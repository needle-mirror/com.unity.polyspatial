using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using FlatSharp;

namespace Tests.Runtime.Functional.Networking
{
    /// <summary>
    /// Tests behavior of HostConnectionManager when handling HostCommands.
    /// Does not test entire connection or session handshake flow, as those will be covered separately in integration tests between a test sim and app host.
    /// </summary>
    [TestFixture]
    public class HostConnectionManagerTests
    {
        /// <summary>
        /// For manually sending host commands to the HostConnectionManager that would otherwise be sent by 
        /// PolySpatialNetworkAppHostBase as part of the connection and session handshakes.
        /// </summary>
        class TestHost : IDisposable, IPolySpatialCommandHandler
        {
            TcpListener m_TcpListener;
            public int LocalPort { get; }
            public PolySpatialHostNetworkConnection Connection;
            static ushort s_NextConnectionID = 0;

            // None of the tests need to check the command args, just verify that commands were received and what type.
            public int ReceivedCommandCount = 0;
            public PolySpatialCommand LastReceivedCommand = 0;

            public TestHost()
            {
                // Listen on any open port to allow for tests with multiple TestHosts and/or parallel CI runs.
                m_TcpListener = new TcpListener(IPAddress.Loopback, 0);
                m_TcpListener.Start();
                LocalPort = ((IPEndPoint)m_TcpListener.LocalEndpoint).Port;
            }

            public unsafe void HandleCommand(PolySpatialCommand cmd, int argCount, void** argValues, int* argSizes)
            {
                ++ReceivedCommandCount;
                LastReceivedCommand = cmd;
            }

            public void AcceptConnection()
            {
                var tcpClient = m_TcpListener.AcceptTcpClient();
                Connection = new PolySpatialHostNetworkConnection(tcpClient, new PolySpatialHostID(s_NextConnectionID++))
                {
                    NextHandler = this
                };
            }

            public void Dispose()
            {
                m_TcpListener.Stop();
            }
        }

        /// <summary>
        /// TestHost instances created during a test. 
        /// Keep track in a list so we can process all message queues and dispose of the instances.
        /// </summary>
        List<TestHost> m_testHosts = new();

        /// <summary>
        /// THe HostConnectionManager instance to test
        /// </summary>
        HostConnectionManager m_HostConnectionManager;

        TestHost CreateTestHost()
        {
            m_testHosts.Add(new TestHost());

            return m_testHosts.Last();
        }

        void ProcessTestHostMessages()
        {
            foreach (var testHost in m_testHosts)
            {
                testHost.Connection.Update();
            }
        }

        [SetUp]
        public void SetUp()
        {
            m_HostConnectionManager = new HostConnectionManager();
        }

        [TearDown]
        public void TearDown()
        {
            m_HostConnectionManager.Dispose();
            foreach (var testHost in m_testHosts)
            {
                testHost.Dispose();
            }
            m_testHosts.Clear();
        }

        /// <summary>
        /// Creates an AppNetworkConnection to the specified TestHost,
        /// and handle accepting the connection and setting up additional plumping
        /// so that the connection is ready to be used for testing.
        /// </summary>
        PolySpatialAppNetworkConnection CreateConnection(TestHost testHost, bool addToConnectionManager = true)
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Loopback, testHost.LocalPort);

            var connection = new PolySpatialAppNetworkConnection(tcpClient)
            {
                NextHostHandler = m_HostConnectionManager
            };

            if (addToConnectionManager)
                m_HostConnectionManager.AddConnection(connection);

            testHost.AcceptConnection();

            return connection;
        }

        /// <summary>
        /// Uses a SpinWait to wait for a connection to receive a message. Since NetworkCommandStreamBase uses threads and queues
        /// for sending and receiving messages, this verifies that a message has been added to the incoming queue and we can proceed with the test.
        /// Times out after 1000ms and fails an assertion.
        /// </summary>
        /// <param name="connection">The connection expected to receive a message</param>
        /// <param name="expectedMessageIndex">The expected MessageIndex (internal counter in NetworkCommandStreamBase)</param>
        static void WaitForMessageIndex(NetworkCommandStreamBase connection, uint expectedMessageIndex)
        {
            Assert.IsTrue(SpinWait.SpinUntil(() => expectedMessageIndex == connection.NewestMessageIndex, 1000));
        }

        // Helper functions for sending a command and waiting for it to arrive at the expected connection.
        static void SendSerializedEntityHostCommand<T>(
            TestHost sender,
            PolySpatialAppNetworkConnection expectedReceiver,
            PolySpatialHostCommand command,
            PolySpatialInstanceID id,
            T instance) where T : class, IFlatBufferSerializable<T>
        {
            var expectedMessageIdx = expectedReceiver.NewestMessageIndex + 1;

            sender.Connection.SerializedEntityHostCommand(command, id, instance);

            WaitForMessageIndex(expectedReceiver, expectedMessageIdx);
        }

        static unsafe void SendHostCommand<T1, T2>(
            TestHost sender,
            PolySpatialAppNetworkConnection expectedReceiver,
            PolySpatialHostCommand command,
            T1* arg1,
            T2* arg2) where T1 : unmanaged where T2 : unmanaged
        {
            var expectedMessageIdx = expectedReceiver.NewestMessageIndex + 1;

            sender.Connection.HostCommand(command, arg1, arg2);

            WaitForMessageIndex(expectedReceiver, expectedMessageIdx);
        }

        /// <summary>
        /// Test that an active session is registered when BeginHostSession command is received
        /// </summary>
        [Test]
        public void Test_BeginHostSession()
        {
            Assert.AreEqual(0, m_HostConnectionManager.Connections.Count);
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());

            var testHost = CreateTestHost();
            var connection = CreateConnection(testHost);

            // Connection registered but session handshake not complete
            Assert.AreEqual(1, m_HostConnectionManager.Connections.Count);
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection.HostID));

            var hostSessionData = PolySpatialRuntime.GetSessionData();

            SendSerializedEntityHostCommand(testHost, connection,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);

            m_HostConnectionManager.ProcessIncomingMessages();

            // Session handshake complete
            Assert.IsTrue(m_HostConnectionManager.HasActiveSessions());
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection.HostID));

            // Test for UnityEngine.Assertions
#if UNITY_EDITOR
            // Create a connection that isn't registered with the HostConnectionManager
            var testHost1 = CreateTestHost();
            var connection1 = CreateConnection(testHost1, addToConnectionManager: false);

            Assert.AreEqual(1, m_HostConnectionManager.Connections.Count);
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection.HostID));
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection1.HostID));

            // Attempt to begin session from host with unregistered connection
            SendSerializedEntityHostCommand(testHost1, connection1,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);

            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => connection1.Update());

            // Attempt to begin session for session that has already began
            SendSerializedEntityHostCommand(testHost, connection,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);

            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => m_HostConnectionManager.ProcessIncomingMessages());
#endif
        }

        /// <summary>
        /// Test that an active session is unregistered when EndHostSession command is received
        /// </summary>
        [Test]
        public void Test_EndHostSession()
        {
            Assert.AreEqual(0, m_HostConnectionManager.Connections.Count);
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());

            var testHost0 = CreateTestHost();
            var connection0 = CreateConnection(testHost0);

            var testHost1 = CreateTestHost();
            var connection1 = CreateConnection(testHost1);

            // Connection registered but session handshake not complete
            Assert.AreEqual(2, m_HostConnectionManager.Connections.Count);
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection0.HostID));
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection1.HostID));

            var hostSessionData = PolySpatialRuntime.GetSessionData();
            SendSerializedEntityHostCommand(testHost0, connection0,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);
            SendSerializedEntityHostCommand(testHost1, connection1,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);

            m_HostConnectionManager.ProcessIncomingMessages();

            // Session handshakes complete
            Assert.IsTrue(m_HostConnectionManager.HasActiveSessions());
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection0.HostID));
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection1.HostID));

            // End Session for testHost0
            var hostEndSessionData = PolySpatialRuntime.GetEndSessionData(ErrorCode.None);
            SendSerializedEntityHostCommand(testHost0, connection0,
                PolySpatialHostCommand.EndHostSession, PolySpatialInstanceID.None, hostEndSessionData);

            m_HostConnectionManager.ProcessIncomingMessages();

            // Expected session unregistered, and remaining session still active
            Assert.IsTrue(m_HostConnectionManager.HasActiveSessions());
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection0.HostID));
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection1.HostID));

            // End Session for testHost1
            SendSerializedEntityHostCommand(testHost1, connection1,
                PolySpatialHostCommand.EndHostSession, PolySpatialInstanceID.None, hostEndSessionData);

            m_HostConnectionManager.ProcessIncomingMessages();

            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection0.HostID));
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection1.HostID));

            // Test for UnityEngine.Assertions
#if UNITY_EDITOR
            // Test end session for a session that has already ended
            SendSerializedEntityHostCommand(testHost1, connection1,
                PolySpatialHostCommand.EndHostSession, PolySpatialInstanceID.None, hostEndSessionData);

            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => m_HostConnectionManager.ProcessIncomingMessages());
#endif
        }

        /// <summary>
        /// Tests the PingEcho reply to a host when a Ping HostCommand is received
        /// </summary>
        [Test]
        public void Test_Ping()
        {
            Assert.AreEqual(0, m_HostConnectionManager.Connections.Count);
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());

            var testHost0 = CreateTestHost();
            var connection0 = CreateConnection(testHost0);

            var testHost1 = CreateTestHost();
            var connection1 = CreateConnection(testHost1);

            // Connection registered but session handshake not complete
            Assert.AreEqual(2, m_HostConnectionManager.Connections.Count);
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());

            var hostSessionData = PolySpatialRuntime.GetSessionData();
            SendSerializedEntityHostCommand(testHost0, connection0,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);
            SendSerializedEntityHostCommand(testHost1, connection1,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);

            m_HostConnectionManager.ProcessIncomingMessages();

            // Session handshakes complete
            Assert.IsTrue(m_HostConnectionManager.HasActiveSessions());
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection0.HostID));
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection1.HostID));

            Assert.AreEqual(0, testHost0.ReceivedCommandCount);
            Assert.AreNotEqual(PolySpatialCommand.PingEcho, testHost0.LastReceivedCommand);
            Assert.AreEqual(0, testHost1.ReceivedCommandCount);
            Assert.AreNotEqual(PolySpatialCommand.PingEcho, testHost1.LastReceivedCommand);

            // Ping from testHost0
            unsafe
            {
                var requestTimeData = new PolySpatialPingData
                {
                    HostTimeSinceStartup = Time.realtimeSinceStartupAsDouble,
                    SimTimeSinceStartup = -1,
                };

                var testHost0Id = PolySpatialInstanceID.None;
                SendHostCommand(testHost0, connection0,
                    PolySpatialHostCommand.Ping, &testHost0Id, &requestTimeData);
            }

            // HostConnectionManager should handle Ping and send a PingEcho reply
            var expectedTestHost0MessageIdx = testHost0.Connection.NewestMessageIndex + 1;
            m_HostConnectionManager.ProcessIncomingMessages();

            // TestHost0 should receive the PingEcho
            WaitForMessageIndex(testHost0.Connection, expectedTestHost0MessageIdx);
            ProcessTestHostMessages();

            Assert.AreEqual(1, testHost0.ReceivedCommandCount);
            Assert.AreEqual(PolySpatialCommand.PingEcho, testHost0.LastReceivedCommand);

            // TestHost1 should not have received the PingEcho
            Assert.AreEqual(0, testHost1.ReceivedCommandCount);
            Assert.AreNotEqual(PolySpatialCommand.PingEcho, testHost1.LastReceivedCommand);

            // Ping from testHost1 this time, verify testHost0 does not receive the PingEcho
            unsafe
            {
                var requestTimeData = new PolySpatialPingData
                {
                    HostTimeSinceStartup = Time.realtimeSinceStartupAsDouble,
                    SimTimeSinceStartup = -1,
                };

                var testHost1Id = PolySpatialInstanceID.None;
                SendHostCommand(testHost1, connection1,
                    PolySpatialHostCommand.Ping, &testHost1Id, &requestTimeData);
            }

            var expectedTestHost1MessageIdx = testHost1.Connection.NewestMessageIndex + 1;
            m_HostConnectionManager.ProcessIncomingMessages();

            WaitForMessageIndex(testHost1.Connection, expectedTestHost1MessageIdx);
            ProcessTestHostMessages();

            Assert.AreEqual(1, testHost0.ReceivedCommandCount);
            Assert.AreEqual(1, testHost1.ReceivedCommandCount);
            Assert.AreEqual(PolySpatialCommand.PingEcho, testHost1.LastReceivedCommand);
        }

        /// <summary>
        /// Tests that WaitForConnectionsToEnd will force incoming messages to be processed, causing sessions to be unregistered.
        /// </summary>
        [Test]
        public void Test_WaitForConnectionsToEnd()
        {
            // Register an active session
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());

            var testHost = CreateTestHost();
            var connection = CreateConnection(testHost);

            var hostSessionData = PolySpatialRuntime.GetSessionData();
            SendSerializedEntityHostCommand(testHost, connection,
                PolySpatialHostCommand.BeginHostSession, PolySpatialInstanceID.None, hostSessionData);

            m_HostConnectionManager.ProcessIncomingMessages();

            // Session handshake complete
            Assert.IsTrue(m_HostConnectionManager.HasSession(connection.HostID));

            // Enqueue EndHostSession
            var hostEndSessionData = PolySpatialRuntime.GetEndSessionData(ErrorCode.None);
            SendSerializedEntityHostCommand(testHost, connection,
                PolySpatialHostCommand.EndHostSession, PolySpatialInstanceID.None, hostEndSessionData);

            Assert.IsTrue(m_HostConnectionManager.HasActiveSessions());

            m_HostConnectionManager.WaitForConnectionsToEnd();

            // Session no longer active
            Assert.IsFalse(m_HostConnectionManager.HasActiveSessions());
            Assert.IsFalse(m_HostConnectionManager.HasSession(connection.HostID));
        }
    }
}
