
using NUnit.Framework;
using Unity.PolySpatial;
#if INCLUDE_ISOLATION_TESTS
using Tests.Runtime.PolySpatialTest.Base;
#endif

namespace Tests.Isolation
{
    /// <summary>
    /// These tests are expected to run in new, clean projects that declare PolySpatial package dependencies but have not yet been configured with any additional
    /// define symbol or flags. Some isolation tests are agnostic of any configuration changes and can be included in any test run. However, If adding any test
    /// that depends on having a clean project and default configuration (ex. PolySpatial Runtime disabled by default), gate them behind INCLUDE_ISOLATION_TESTS
    /// so they are not included in other test runs that may modify project configuration.
    /// </summary>
    [TestFixture]
    public class IsolationTests
    {
        [Test]
        public void Test_01_PolySpatial_Settings_Are_Present()
        {
            Assert.IsNotNull(PolySpatialSettings.instance, "PolySpatial Settings are null");
        }

#if INCLUDE_ISOLATION_TESTS
        [Test]
        public void Test_02_PolySpatial_Settings_Have_Default_Values()
        {
            var enablePolySpatialRuntime = PolySpatialSettings.instance.EnablePolySpatialRuntime;
            Assert.IsFalse(enablePolySpatialRuntime, "PolySpatial Settings - Runtime enabled by default");

            var enableClipping = PolySpatialSettings.instance.EnableClipping;
            Assert.IsFalse(enableClipping, "PolySpatial Settings - clipping enabled by default");

            var enableStatistics = PolySpatialSettings.instance.EnableStatistics;
            Assert.IsFalse(enableStatistics, "PolySpatial Settings - statistics enabled by default");

            var enableMacRealityKitPreviewInPlayMode = PolySpatialSettings.instance.EnableMacRealityKitPreviewInPlayMode;
            Assert.IsFalse(enableMacRealityKitPreviewInPlayMode, "PolySpatial Settings - Mac RK preview in PlayMode enabled by default");

            var disabledTrackers = PolySpatialSettings.instance.DisabledTrackers;
            Assert.IsFalse(disabledTrackers != null && disabledTrackers.Length != 0, "PolySpatial Settings - disabled trackers are not empty by default");

            var runtimeFlags = PolySpatialSettings.instance.RuntimeFlags;
            Assert.AreEqual(0, runtimeFlags, $"PolySpatial Settings - Runtime flags are not empty. ulong value: {runtimeFlags}");
        }

        [Test, PolySpatialRuntimeSetup]
        public void Test_03_PolySpatial_Settings_Runtime_Enabled(){
            var enablePolySpatialRuntime = PolySpatialSettings.instance.EnablePolySpatialRuntime;
            Assert.IsTrue(enablePolySpatialRuntime, "PolySpatial Settings - Runtime disabled when expected to be enabled");
        }

        [Test]
        public void Test_04_PolySpatial_Settings_Runtime_Updated(){
            var enablePolySpatialRuntime = PolySpatialSettings.instance.EnablePolySpatialRuntime;
            Assert.IsFalse(enablePolySpatialRuntime, "PolySpatial Settings - Runtime enabled by default");

            PolySpatialSettings.instance.EnablePolySpatialRuntime = true;

            enablePolySpatialRuntime = PolySpatialSettings.instance.EnablePolySpatialRuntime;
            Assert.IsTrue(enablePolySpatialRuntime, "PolySpatial Settings - Runtime disabled when expected to be enabled");

            PolySpatialSettings.instance.EnablePolySpatialRuntime = false;

            enablePolySpatialRuntime = PolySpatialSettings.instance.EnablePolySpatialRuntime;
            Assert.IsFalse(enablePolySpatialRuntime, "PolySpatial Settings - Runtime enabled when expected to be disabled");
        }
#endif
    }
}

