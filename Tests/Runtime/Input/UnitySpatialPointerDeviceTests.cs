#if POLYSPATIAL_INPUT_TESTS
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial;
using Unity.PolySpatial.InputDevices;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityObject = UnityEngine.Object;

namespace Tests.Runtime.Functional.Input
{
    [Obsolete]
    [TestFixture]
    public class UnitySpatialPointerDeviceTests : InputTestFixture
    {
        public override void Setup()
        {
            base.Setup();

            InputSystem.RegisterLayout<PolySpatialTouchscreen>();
            InputSystem.RegisterLayout<SpatialPointerControl>(name: SpatialPointerState.LayoutName);
            InputSystem.RegisterLayout<SpatialPointerDevice>();
        }


        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            // Fully reset unitySim per each test in case left over state causes problems
            PolySpatialSimulationHostImpl.ResetInstance();
        }

        [Test]
#if UNITY_IOS
        [Ignore("Disabling as it currently crashes/fails on iOS.")]
#endif
        public void Test_CheckPhaseState()
        {
            SpatialPointerState? state = null;
            void OnActionChange(InputAction.CallbackContext context)
            {
                state = context.ReadValue<SpatialPointerState>();
            }

            var primaryAction = new InputAction(binding: "<SpatialPointerDevice>/primarySpatialPointer");
            primaryAction.performed += OnActionChange;
            primaryAction.Enable();

            SendEvents(1, SpatialPointerPhase.Began, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsNotNull(state, "Expected to receive a SpatialPointerState.");
            Assert.IsTrue(state.Value.phase == SpatialPointerPhase.Began, "Expected Phase.Began");

            SendEvents(1, SpatialPointerPhase.Moved, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsTrue(state.Value.phase == SpatialPointerPhase.Moved, "Expected Phase.Moved");

            SendEvents(1, SpatialPointerPhase.Ended, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsTrue(state.Value.phase == SpatialPointerPhase.Ended, "Expected Phase.Ended");

            primaryAction.Disable();
        }

        [Test]
#if UNITY_IOS
        [Ignore("Disabling as it currently crashes/fails on iOS.")]
#endif
        public void Test_CheckPositionState()
        {
            SpatialPointerState? state = null;
            void OnActionChange(InputAction.CallbackContext context)
            {
                state = context.ReadValue<SpatialPointerState>();
            }

            var primaryAction = new InputAction(binding: "<SpatialPointerDevice>/primarySpatialPointer");
            primaryAction.performed += OnActionChange;
            primaryAction.Enable();

            Vector3 testPosition = Vector3.one * 10.0f;
            int testColliderId = 10;

            SendEvents(1, SpatialPointerPhase.Began, testPosition, testColliderId);
            InputSystem.Update();

            // TODO -- this test depends on whatever the current opened scene is and its volume camera.
            // It should ensure a specific scene is open!
            var curVolCam = UnityObject.FindObjectOfType<VolumeCamera>();
            var expectedPosition = curVolCam.VolumeSpaceToWorldSpaceMatrix.MultiplyPoint3x4(testPosition);

            Assert.IsNotNull(state, "Expected to receive a SpatialPointerState.");
            Assert.AreEqual(expectedPosition, state.Value.interactionPosition, "Expected correct interactionPosition");
            Assert.AreEqual(testColliderId, state.Value.targetId, "Expected correct targetId");

            primaryAction.Disable();
        }

        unsafe void SendEvents(int pointerId, SpatialPointerPhase phase, Vector3 position, int colliderId)
        {
            NativeArray<PolySpatialPointerEvent> events = new(1, Allocator.Temp);
            events[0] = new PolySpatialPointerEvent()
            {
                targetId = new PolySpatialInstanceID {id = colliderId},
                interactionId = pointerId,
                interactionPosition = position,
                phase = (Unity.PolySpatial.Internals.PolySpatialPointerPhase)phase
            };
            PolySpatialSimulationHostImpl.SimHostOnInputEvent(PolySpatialInputType.Pointer, events.Length, events.GetUnsafePtr());
            events.Dispose();
        }
    }
}
#endif
