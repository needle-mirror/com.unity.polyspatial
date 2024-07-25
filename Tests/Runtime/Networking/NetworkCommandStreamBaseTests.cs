using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.PolySpatial.Internals;
using UnityEngine;
using Assert = NUnit.Framework.Assert;

namespace Tests.Runtime.Functional.Networking
{
    /// <summary>
    /// A NetworkCommandStreamBase implementation that takes a Stream instead of a NetworkStream.
    /// This allows for tests that manually write to and read from the stream without requiring a TCPClient.
    /// Received messages are stored in a List so they can be examined by tests, rather than being
    /// forwarded to a command handler.
    /// </summary>
    internal class TestNetworkCommandStream : NetworkCommandStreamBase
    {
        /// <summary>
        /// Stores contents of a received command
        /// </summary>
        public struct CommandData
        {
            public int Command;
            public string[] Args;
        }

        public List<CommandData> ReceivedCommandList;

        /// <summary>
        /// Always connected for now, but we may want tests in the future that test the not connected case
        /// </summary>
        public override bool Connected => true;

        public TestNetworkCommandStream(Stream stream) : base(stream)
        {
            ReceivedCommandList = new();
        }

        /// <summary>
        /// Stores a received command in the ReceivedCommandList
        /// </summary>
        protected override unsafe void ReceiveCommand(int command, int argCount, int* argSizes, void** argValues)
        {
            var commandData = new CommandData
            {
                Command = command,
                Args = argCount > 0 ? new string[argCount] : null
            };

            for (var i = 0; i < argCount; ++i)
            {
                commandData.Args[i] = Encoding.UTF8.GetString((byte*)argValues[i], argSizes[i]);
            }

            ReceivedCommandList.Add(commandData);
        }

        /// <summary>
        /// Directly receive a message. For cases when the read thread isn't running.
        /// </summary>
        public void ReceiveIncomingMessages(int count = 1)
        {
            for (int i = 0; i < count; ++i)
                ReceiveIncomingMessage();
        }

        /// <summary>
        /// Converts the args to byte arrays and sends the command
        /// </summary>
        /// <returns>expected size of the command on the wire</returns>
        public unsafe int SendCommand(int command, string[] args)
        {
            var expectedSize = sizeof(MsgHeader);

            if (args != null)
            {
                var argCount = args.Length;
                expectedSize += argCount * PolySpatialUtils.PaddedIntSize;

                // Pin the byte arrays so we can use their pointer address as arg values
                var gcHandles = new List<GCHandle>();
                try
                {
                    var argList = new List<byte[]>();

                    foreach (var arg in args)
                    {
                        var argBytes = Encoding.UTF8.GetBytes(arg);
                        argList.Add(argBytes);
                        gcHandles.Add(GCHandle.Alloc(argBytes, GCHandleType.Pinned));
                        expectedSize += PolySpatialUtils.AlignSize(argBytes.Length);
                    }

                    var argSizes = stackalloc int[argCount];
                    var argValues = stackalloc void*[argCount];

                    for (int i = 0; i < argCount; ++i)
                    {
                        argSizes[i] = argList[i].Length;
                        argValues[i] = (void*)gcHandles[i].AddrOfPinnedObject();
                    }

                    SendCommand(command, argCount, argSizes, argValues);
                }
                finally
                {
                    foreach (var handle in gcHandles)
                    {
                        if (handle.IsAllocated)
                            handle.Free();
                    }
                }
            }
            else
            {
                SendCommand(command, 0, null, null);
            }

            return expectedSize;
        }

        public new void SendNextOutgoingMessage() => base.SendNextOutgoingMessage();
    }

    /// <summary>
    /// Unit tests for NetworkCommandStreamBase class. Uses MemoryStream to simulate incoming and outgoing TCP messages.
    /// Read and Write threads are not started, the tests manually call the corresponding methods to process the
    /// incoming and outgoing message queues.
    /// </summary>
    [TestFixture]
    public class NetworkCommandStreamBaseTests
    {
        unsafe int WriteCommandToStream(int command, string[] args, Stream stream, bool resetPosition = true)
        {
            // build up the message to be received by the NetworkCommandStream
            // message size is needed ahead of time
            var expectedSize = sizeof(NetworkCommandStreamBase.MsgHeader);

            var argCount = 0;
            if (args != null)
            {
                argCount = args.Length;

                foreach (var arg in args)
                {
                    expectedSize += PolySpatialUtils.PaddedIntSize + PolySpatialUtils.AlignSize(Encoding.UTF8.GetByteCount(arg));
                }
            }

            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                // total size
                writer.Write((long)expectedSize - sizeof(long));
                // time
                writer.Write(Time.realtimeSinceStartupAsDouble);
                // command
                writer.Write(command);
                // argCount
                writer.Write(argCount);

                // args
                if (args != null)
                {
                    foreach (var arg in args)
                    {
                        var byteCount = Encoding.UTF8.GetByteCount(arg);

                        // argSize
                        writer.Write(byteCount);
                        // padding
                        writer.Write(0);
                        // argValue
                        writer.Write(Encoding.UTF8.GetBytes(arg));
                        // padding
                        for (int i = 0; i < PolySpatialUtils.PaddingNeeded(byteCount); ++i)
                        {
                            writer.Write((byte)0);
                        }
                    }
                }
            }

            // Reset position in case that the command is going to be read by a stream reader
            if (resetPosition)
            {
                stream.Position -= expectedSize;
            }

            return expectedSize;
        }


        /// <summary>
        /// Verifies that the stream written by NetworkCommandStream matches the documented wire format
        /// </summary>
        [TestCase(int.MaxValue, null)]
        [TestCase(1000000, new string[] { "stringArg0", "stringArg1WithDifferentLength" })]
        public unsafe void Test_WireFormat_Send(int command, string[] args)
        {
            var argCount = args?.Length ?? 0;

            var outgoingMemoryStream = new MemoryStream();
            var commandStream = new TestNetworkCommandStream(outgoingMemoryStream);

            // Serialize command and add to outgoing message queue
            var expectedSize = commandStream.SendCommand(command, args);

            // Force the command to be written to the stream on the main thread
            commandStream.SendNextOutgoingMessage();
            commandStream.Flush();

            Assert.AreEqual(expectedSize, outgoingMemoryStream.Length);

            // reset stream position so we can read from beginning
            outgoingMemoryStream.Position = 0;

            using (var reader = new BinaryReader(outgoingMemoryStream, Encoding.UTF8, false))
            {
                // totalSize
                Assert.AreEqual(expectedSize - sizeof(long), reader.ReadInt64());
                // timeElapsed
                reader.ReadDouble();
                // command
                Assert.AreEqual(command, reader.ReadInt32());
                // argCount
                Assert.AreEqual(argCount, reader.ReadInt32());

                for (int i = 0; i < argCount; ++i)
                {
                    var expectedString = args[i];

                    // argSize
                    var argSize = reader.ReadInt32();
                    Assert.AreEqual(expectedString.Length, argSize);

                    // padding
                    reader.ReadInt32();

                    // argValue
                    var actualString = Encoding.UTF8.GetString(reader.ReadBytes(argSize));
                    Assert.AreEqual(expectedString, actualString);

                    // padding
                    var paddingNeeded = PolySpatialUtils.PaddingNeeded(argSize);
                    if (paddingNeeded > 0)
                        reader.ReadBytes(paddingNeeded);
                }

                // All expected data has been read
                Assert.AreEqual(outgoingMemoryStream.Length, outgoingMemoryStream.Position);
            }
        }

        /// <summary>
        /// Verifies that NetworkCommandStream can receive from a stream built using the documented wire format
        /// </summary>
        [TestCase(int.MaxValue, null)]
        [TestCase(1000000, new string[] { "stringArg0", "stringArg1WithDifferentLength" })]
        public unsafe void Test_WireFormat_Receive(int command, string[] args)
        {
            var argCount = args?.Length ?? 0;

            // Write command to memorystream to simulate incoming message on the wire
            var incomingMemoryStream = new MemoryStream();
            var expectedSize = WriteCommandToStream(command, args, incomingMemoryStream);
            Assert.AreEqual(expectedSize, incomingMemoryStream.Length);

            var commandStream = new TestNetworkCommandStream(incomingMemoryStream);

            Assert.AreEqual(0, commandStream.ReceivedCommandList.Count);

            // Receive and process the message immediately on the main thread
            commandStream.ReceiveIncomingMessages();
            commandStream.Update();

            Assert.AreEqual(1, commandStream.ReceivedCommandList.Count);

            // verify received command matches
            var receivedCommand = commandStream.ReceivedCommandList[0];

            Assert.AreEqual(command, receivedCommand.Command);
            Assert.AreEqual(argCount, receivedCommand.Args?.Length ?? 0);

            for (int i = 0; i < argCount; ++i)
            {
                Assert.AreEqual(args[i], receivedCommand.Args[i]);
            }
        }

        /// <summary>
        /// Incoming messages are processed in batches, based on the arrival of
        /// EndAppFrame messages. Tests the various behaviors around processing message batches.
        /// </summary>
        [Test]
        public void Test_Update_EndAppFrame()
        {
            var incomingMemoryStream = new MemoryStream();
            var commandStream = new TestNetworkCommandStream(incomingMemoryStream);
            Assert.AreEqual(0, commandStream.ReceivedCommandList.Count);

            // Test message processed immediately before first EndAppFrame received
            {
                WriteCommandToStream(1000000, null, incomingMemoryStream);

                commandStream.ReceiveIncomingMessages();
                commandStream.Update();
                Assert.AreEqual(1, commandStream.ReceivedCommandList.Count);

                commandStream.ReceivedCommandList.Clear();
                Assert.AreEqual(0, commandStream.ReceivedCommandList.Count);
            }

            // First EndAppFrame should be processed immediately
            {
                WriteCommandToStream((int)PolySpatialCommand.EndAppFrame, null, incomingMemoryStream);

                commandStream.ReceiveIncomingMessages();
                commandStream.Update();
                Assert.AreEqual(1, commandStream.ReceivedCommandList.Count);

                commandStream.ReceivedCommandList.Clear();
                Assert.AreEqual(0, commandStream.ReceivedCommandList.Count);
            }

            // Now that first EndAppFrame received, commands shouldn't be processed until next EndAppFrame
            {
                var totalSize = 0;
                totalSize += WriteCommandToStream(1000000, null, incomingMemoryStream, resetPosition: false);
                totalSize += WriteCommandToStream(1000001, null, incomingMemoryStream, resetPosition: false);
                totalSize += WriteCommandToStream(1000002, null, incomingMemoryStream, resetPosition: false);
                incomingMemoryStream.Position -= totalSize;

                commandStream.ReceiveIncomingMessages(3);
                commandStream.Update();
                Assert.AreEqual(0, commandStream.ReceivedCommandList.Count);

                // All messages up to and including EndAppFrame should be processed. Message after EndAppFrame won't be processed.
                totalSize = 0;
                totalSize += WriteCommandToStream((int)PolySpatialCommand.EndAppFrame, null, incomingMemoryStream, resetPosition: false);
                totalSize += WriteCommandToStream(1000003, null, incomingMemoryStream, resetPosition: false);
                incomingMemoryStream.Position -= totalSize;

                commandStream.ReceiveIncomingMessages(2);
                commandStream.Update();
                Assert.AreEqual(4, commandStream.ReceivedCommandList.Count);

                commandStream.ReceivedCommandList.Clear();
                Assert.AreEqual(0, commandStream.ReceivedCommandList.Count);
            }

            // Remaining message from previous Update() should be processed, as well as all messages up to most recent EndAppFrame
            {
                var totalSize = 0;
                totalSize += WriteCommandToStream((int)PolySpatialCommand.EndAppFrame, null, incomingMemoryStream, resetPosition: false);
                totalSize += WriteCommandToStream(1000003, null, incomingMemoryStream, resetPosition: false);
                totalSize += WriteCommandToStream(1000004, null, incomingMemoryStream, resetPosition: false);
                totalSize += WriteCommandToStream(1000005, null, incomingMemoryStream, resetPosition: false);
                totalSize += WriteCommandToStream((int)PolySpatialCommand.EndAppFrame, null, incomingMemoryStream, resetPosition: false);
                incomingMemoryStream.Position -= totalSize;

                commandStream.ReceiveIncomingMessages(5);
                commandStream.Update();
                Assert.AreEqual(6, commandStream.ReceivedCommandList.Count);
            }
        }
    }
}
