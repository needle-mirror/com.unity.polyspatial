using System;
using System.Collections;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;
using Random = UnityEngine.Random;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public unsafe class UnityLightTests : PolySpatialTest.Base.PolySpatialTest
    {
        #if false
        [SetUp]
        public void SetUp()
        {
            m_TestGameObject = new GameObject("Light Test Object");
            m_TestLight = m_TestGameObject.AddComponent<Light>();
        }

        [Test]
        public void Test_PolySpatial_Light_Created()
        {
            Assert.IsNotNull(m_TestLight);
        }

        [UnityTest]
        public IEnumerator Test_PolySpatial_Light_Sets_LightType_And_Marks_Dirty()
        {
            // We do this to make sure that one frame of change processing has happened and the
            // data is no longer dirty due to creating the component on the first frame;
            yield return null;
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestLight);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.trackingData.dirty, "Expected data.dirty to be false");

            m_TestLight.type = LightType.Point;
        }

        [UnityTest]
        public IEnumerator Test_PolySpatial_Light_Sets_Same_Light_Type_And_Does_Not_Mark_Dirty()
        {
            // We do this to make sure that one frame of change processing has happened and the
            // data is no longer dirty due to creating the component on the first frame;
            yield return null;
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestLight);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.dirty, "Expected data.dirty to be false");
            data = PolySpatialComponentUtils.GetTrackingData(m_TestLight);
            Assert.IsTrue(data.ValidateTrackingFlags());
            m_TestLight.lightType = m_TestLight.lightType;
            Assert.IsFalse(data.dirty, "Expected data.dirty to be false");
        }

        [UnityTest]
        public IEnumerator Test_PolySpatial_Light_Sets_Intensity_And_Marks_Dirty()
        {
            // We do this to make sure that one frame of change processing has happened and the
            // data is no longer dirty due to creating the component on the first frame;
            yield return null;
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestLight);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.dirty);
            // the value should not matter here which is why I chose random.
            m_TestLight.intensity = Random.value;
            data = PolySpatialComponentUtils.GetTrackingData(m_TestLight);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.dirty, "Expected data.dirty to be true");
            var lightIntensity = PolySpatialComponentUtils.GetComponentData(m_TestLight).intensity;
            Assert.IsTrue(Math.Abs(lightIntensity - m_TestLight.intensity) < float.Epsilon,
                "Expected the component data and component intensities to be equal, instead got: " +
                $"GetLightData(m_Light).intensity: {lightIntensity} and m_Light.intensity: {m_TestLight.intensity}");
        }

        [UnityTest]
        public IEnumerator Test_PolySpatial_Light_Sets_Color_And_Marks_Dirty()
        {
            // We do this to make sure that one frame of change processing has happened and the
            // data is no longer dirty due to creating the component on the first frame;
            yield return null;
            m_TestLight.color = Color.black;
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestLight);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.dirty);
            var lightColor = PolySpatialComponentUtils.GetComponentData(m_TestLight).color;
            Assert.AreEqual(lightColor, m_TestLight.color,
                "Expected the component data color and the component color to be equal." +
                $"\nInstead got: GetLightData(m_Light).color: {lightColor} and m_Light.color: {m_TestLight.color}");
        }

        [UnityTest]
        public IEnumerator Test_Light_Destroyed()
        {
            // We do this so that change processing runs and clears the dirty flag
            // from the component being created.
            yield return null;

            Object.Destroy(m_TestLight);
            // We need to wait 1+ frames to make sure that Unity destroyed the component and
            // component data was updated correctly.
            yield return new WaitForDone(2f,
                () => PolySpatialComponentUtils.GetTrackingData(m_TestLight).stage == LifecycleStage.Deallocated);

            // need to use "==" here because of its special purpose within unity for detecting destroyed objects.
            Assert.IsTrue(m_TestLight == null, "Expected m_Light == null to be true (i.e. destroyed)");
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestLight);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(LifecycleStage.Deallocated, data.stage,
                $"trackingData.stage == LifecycleStage.Deallocated, got {data.stage} instead");
        }
        #endif
    }
}
