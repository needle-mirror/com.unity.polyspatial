using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine.InputSystem.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Tests.Runtime.Functional
{
    [TestFixture]
    public class PolySpatialTypeTests
    {
        [Test]
        public void Test_PolySpatialAssetID_Deserializes_To_Same_PolySpatialAssetID()
        {
            PolySpatialAssetID testId = new PolySpatialAssetID(Guid.NewGuid());

            string assetString = JsonUtility.ToJson(testId);
            PolySpatialAssetID newId = JsonUtility.FromJson<PolySpatialAssetID>(assetString);

            Assert.IsTrue(newId.Equals(testId));
        }

        [Test]
        public void Test_PolySpatialAssetID_ToString()
        {
            var guid = Guid.NewGuid();
            var guidString = guid.ToString();
            var testId = (new PolySpatialAssetID(guidString)).ToGuid();
            var idString = testId.ToString();

            Assert.AreEqual(guid, testId);
            Assert.AreEqual(guidString, idString);
        }

        [Test]
        public void Test_PolySpatialInstanceID_ToString()
        {
            var id = 1024;
            PolySpatialHostID hostId = new() { connectionId = 0 };
            uint flags = 0;

            // connectionId and flags are zero
            var psiid = new PolySpatialInstanceID { id = id, hostId = hostId, flags = flags };

            var expectedString = $"{id}";

            Assert.AreEqual(psiid.ToString(), expectedString);

            // flags are zero
            hostId.connectionId = 65535;
            psiid.hostId = hostId;

            expectedString = $"{id}:{hostId}";

            Assert.AreEqual(psiid.ToString(), expectedString);

            // all non-zero
            flags = 128;
            psiid.flags = flags;

            expectedString = $"{id}:{hostId}:{flags}";

            Assert.AreEqual(psiid.ToString(), expectedString);

            // connectionId is zero
            hostId.connectionId = 0;
            psiid.hostId = hostId;

            expectedString = $"{id}:{hostId}:{flags}";

            Assert.AreEqual(psiid.ToString(), expectedString);
        }

        [Test]
        public unsafe void Test_AlignmentUtils()
        {
            Assert.AreEqual(0, PolySpatialUtils.AlignSize(0));
            for (var i = 1; i <= PolySpatialUtils.CommandAlignment; ++i)
                Assert.AreEqual(PolySpatialUtils.CommandAlignment, PolySpatialUtils.AlignSize(i));

            Assert.AreEqual(0, PolySpatialUtils.PaddingNeeded(0));
            for (var i = 1; i <= PolySpatialUtils.CommandAlignment; ++i)
                Assert.AreEqual(PolySpatialUtils.CommandAlignment - i, PolySpatialUtils.PaddingNeeded(i));
        }

        [Test]
        public unsafe void Test_NetworkCommandStreamBase_MsgHeaderAlignment()
        {
            // When a message buffer is allocated, the MsgHeader is written to the start 
            // (except for the totalSize value which was deserialized previously).
            // Verify that the alignment of the header matches that of the args. otherwise padding needs to be added.
            var headerSize = sizeof(NetworkCommandStreamBase.MsgHeader) - sizeof(long);
            var paddingNeeded = PolySpatialUtils.PaddingNeeded(headerSize, PolySpatialUtils.CommandAlignment);

            Assert.AreEqual(0, paddingNeeded);
        }

        [Test]
        public unsafe void Test_Command_Alignment()
        {
            // PolySpatialUtils.CommandAlignment is the alignment of PolySpatialChangeListEntityData.
            // This test verifies that this and related constants are correct, so that we can use const values
            // instead of computing static readonly values at runtime.

            var changeListEntityAlignment = UnsafeUtility.AlignOf<PolySpatialChangeListEntityData>();

            Assert.AreEqual(changeListEntityAlignment, PolySpatialUtils.CommandAlignment);

            var expectedPaddedIntSize = PolySpatialUtils.AlignSize(sizeof(int), PolySpatialUtils.CommandAlignment);

            Assert.AreEqual(expectedPaddedIntSize, PolySpatialUtils.PaddedIntSize);

            Assert.AreEqual(PolySpatialUtils.PaddedIntSize, sizeof(int) + PolySpatialUtils.PaddingNeeded(sizeof(int)));
        }

        [Test]
        public unsafe void Test_RecordingData_Alignment()
        {
            Assert.AreEqual(0, PolySpatialUtils.PaddingNeeded(sizeof(RecordingData.RecordingHeader1)));
            Assert.AreEqual(0, PolySpatialUtils.PaddingNeeded(sizeof(RecordingData.CommandHeader1)));
        }

        [Test]
        public unsafe void Test_SpatialPointerState_Size()
        {
            Assert.AreEqual(sizeof(SpatialPointerState), SpatialPointerState.kSizeInBytes);
        }

        [Test]
        public void Test_ChangeList_Count()
        {
            // Verify that both Read-Only and Writable ChangeLists produce the same value for Count
            // since they store the underlying data differently
            const int k_expectedCount = 3;

            var changeListWritable = new ChangeList<EmptyData>.Writable(Allocator.Temp);
            Assert.AreEqual(0, changeListWritable.Count);

            changeListWritable.Add(new DefaultTrackingData(), default);
            changeListWritable.Add(new DefaultTrackingData(), default);
            changeListWritable.Add(new DefaultTrackingData(), default);

            Assert.AreEqual(k_expectedCount, changeListWritable.Count);

            var changeList = new ChangeList<EmptyData>();

            Assert.AreEqual(0, changeList.Count);

            changeList = new ChangeList<EmptyData>(changeListWritable.RawData);

            Assert.AreEqual(k_expectedCount, changeList.Count);
        }

        [Test]
        public void Test_ChangeList_IsEmpty()
        {
            var changeListWritable = new ChangeList<EmptyData>.Writable(Allocator.Temp);

            Assert.IsTrue(changeListWritable.IsEmpty);

            changeListWritable.Add(new DefaultTrackingData(), default);

            Assert.IsFalse(changeListWritable.IsEmpty);

            var changeList = new ChangeList<EmptyData>();

            Assert.IsTrue(changeList.IsEmpty);

            changeList = new ChangeList<EmptyData>(changeListWritable.RawData);

            Assert.IsFalse(changeList.IsEmpty);
        }

        static void AssertPublicAndInternalEnumTypesAreEqual(Type internalType, Type publicType, bool subsetOk = false)
        {
            Dictionary<string, ulong> ToEnumDict(Type type)
            {
                var internalNames = Enum.GetNames(type);
                var internalValues = Enum.GetValues(type);
                var dict = new Dictionary<string, ulong>();
                for (var i = 0; i < internalNames.Length; i++)
                {
                    var value = (ulong)Convert.ChangeType(internalValues.GetValue(i), typeof(ulong));
                    dict.Add(internalNames[i], value);
                }

                return dict;
            }

            var internalDict = ToEnumDict(internalType);
            var publicDict = ToEnumDict(publicType);

            // everything in the public type should be in the internal type
            foreach (var kvp in publicDict)
            {
                Assert.IsTrue(internalDict.ContainsKey(kvp.Key), $"{publicType.Name} has value {kvp.Key} that is not in {internalType.Name}");
                Assert.AreEqual(kvp.Value, internalDict[kvp.Key], $"{publicType.Name} value for {kvp.Key} does not match {internalType.Name} value");
            }

            if (!subsetOk)
            {
                Assert.AreEqual(internalDict.Count, publicDict.Count, $"{publicType.Name} has a different number of values than {internalType.Name}");
            }
            else
            {
                Assert.LessOrEqual(publicDict.Count, internalDict.Count, $"{publicType.Name} has more values than {internalType.Name}");
            }
        }

        static (Type internalEnum, Type publicEnum, bool isSubsetOk)[] m_EnumsToCompare =
        {
            (typeof(PolySpatialRuntimeFlags), typeof(PolySpatialExperimental.RuntimeFlags), true),
            (typeof(PolySpatialLogCategory), typeof(LogCategory), false),
            (typeof(PolySpatialLogLevel), typeof(LogLevel), false),
            (typeof(WindowEvent), typeof(VolumeCamera.WindowEvent), false),
        };

        [Test]
        public void Test_InternalAndPublic_Enum_Matches([ValueSource("m_EnumsToCompare")]
            (Type internalEnum, Type publicEnum, bool isSubsetOk) testCases)
        {
            AssertPublicAndInternalEnumTypesAreEqual(
                testCases.internalEnum,
                testCases.publicEnum,
                subsetOk: testCases.isSubsetOk);
        }
    }
}
