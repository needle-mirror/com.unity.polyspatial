using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnityCameraTests
    {
        static Camera CreateCamera(string name)
        {
            var gameObject = new GameObject(name);
            return gameObject.AddComponent<Camera>();
        }

        static IEnumerator PrePolySpatialProcessing(Action action)
        {
            PolySpatialCore.UnitySimulation.OnceAfterTrackingBeforeProcessingCallbackForTests = action;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Camera_Tracked()
        {
            var camera = CreateCamera("TestCamera1");

            yield return PrePolySpatialProcessing(() => {
                var data = PolySpatialComponentUtils.GetTrackingData(camera);
                Assert.IsTrue(data.ValidateTrackingFlags());
                // NOTE: This should be .Created! But there's a bug that immediately moves things
                // from Created to Running. If someone fixes that bug, this will fail, and then they
                // can fix this test.
                Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            });

            var data = PolySpatialComponentUtils.GetTrackingData(camera);
            Assert.IsTrue(data.ValidateTrackingFlags());

            camera.fieldOfView = 30f;

            yield return PrePolySpatialProcessing(() => {
                var data = PolySpatialComponentUtils.GetTrackingData(camera);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.IsTrue(data.IsDirty());
            });
        }

        [UnityTest]
        public IEnumerator Test_Camera_UnitySceneGraph_WithVolumeCamera()
        {
            var u = PolySpatialCore.LocalBackend as PolySpatialUnityBackend;
            if (u == null)
            {
                Assert.Ignore("LocalBackend is not Unity");
                yield break;
            }

            var usg = u.SceneGraph;

            var camera = CreateCamera("TestCamera1");
            camera.transform.position = new Vector3(1, 2, 3);
            camera.fieldOfView = 30f;

            yield return null;

            // require the default unbounded volume camera for now
            var vc = GameObject.FindFirstObjectByType<VolumeCamera>();
            Assert.IsNotNull(vc);
            Assert.AreEqual(VolumeCamera.PolySpatialVolumeCameraMode.Unbounded, vc.WindowMode, "This test requires the default unbounded volume camera");

            var backCameraGO = usg.FindUnitySceneGraphGameObjectForId(PolySpatialInstanceID.For(camera.gameObject),
                PolySpatialInstanceID.For(vc.gameObject));
            Assert.IsNotNull(backCameraGO);

            var backCamera = backCameraGO.GetComponent<Camera>();
            Assert.IsNotNull(backCamera);

            Assert.AreEqual(camera.fieldOfView, backCamera.fieldOfView);
            Assert.AreEqual(camera.transform.position, backCamera.transform.position);

            camera.fieldOfView = 60f;
            camera.transform.position = new Vector3(3, 2, 1);

            yield return null;

            Assert.AreEqual(camera.fieldOfView, backCamera.fieldOfView);
            Assert.AreEqual(camera.transform.position, backCamera.transform.position);
        }
    }
}
