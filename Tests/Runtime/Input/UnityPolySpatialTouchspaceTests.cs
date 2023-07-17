#if POLYSPATIAL_INPUT_TESTS
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.PolySpatial.InputDevices;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityObject = UnityEngine.Object;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Tests.Runtime.Functional.Input
{
    [TestFixture]
    public class UnityPolySpatialTouchspaceTests : InputTestFixture
    {
        GameObject m_CameraGameObject;

        public override void Setup()
        {
            base.Setup();
            InputSystem.RegisterLayout<WorldTouchControl>(name: WorldTouchState.LayoutName);
            InputSystem.RegisterLayout<PolySpatialTouchspace>();
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
        public void Test_CheckTouchState()
        {
            TouchState? touchState = null;
            void OnActionChange(InputAction.CallbackContext context)
            {
                touchState = context.ReadValue<TouchState>();
            }

            var primaryTouchAction = new InputAction(binding: "<PolySpatialTouchspace>/primaryTouch");
            primaryTouchAction.performed += OnActionChange;
            primaryTouchAction.Enable();

            SendEvents(1, PolySpatialPointerState.Began, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsNotNull(touchState, "Expected to receive a TouchState.");
            Assert.IsTrue(touchState.Value.phase == TouchPhase.Began, "Expected Touchphase.Began");

            SendEvents(1, PolySpatialPointerState.Moved, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsTrue(touchState.Value.phase == TouchPhase.Moved, "Expected Touchphase.Began");

            SendEvents(1, PolySpatialPointerState.Ended, Vector3.one, 1);
            InputSystem.Update();

            Assert.IsTrue(touchState.Value.phase == TouchPhase.Ended, "Expected Touchphase.Ended");

            primaryTouchAction.Disable();
        }

        [Test]
#if UNITY_IOS
        [Ignore("Disabling as it currently crashes/fails on iOS.")]
#endif
        public void Test_CheckWorldTouchState()
        {
            WorldTouchState? worldTouchState = null;
            void OnActionChange(InputAction.CallbackContext context)
            {
                worldTouchState = context.ReadValue<WorldTouchState>();
            }

            var primaryWorldTouchAction = new InputAction(binding: "<PolySpatialTouchspace>/primaryWorldTouch");
            primaryWorldTouchAction.performed += OnActionChange;
            primaryWorldTouchAction.Enable();

            SendEvents(1, PolySpatialPointerState.Began, Vector3.one * 10, 10);
            InputSystem.Update();

            Assert.IsNotNull(worldTouchState, "Expected to receive a WorldTouchState.");
            Assert.AreEqual(worldTouchState.Value.worldPosition, Vector3.one * 10, "Expected worldPosition to be Vector3.one * 10");
            Assert.AreEqual(worldTouchState.Value.colliderId, 10 , "Expected worldPosition to be 10");

            primaryWorldTouchAction.Disable();
        }

        unsafe void SendEvents(int pointerId, PolySpatialPointerState pointerState, Vector3 position, int colliderId)
        {
            NativeArray<PolySpatialPointerEvent> events = new(1, Allocator.Temp);
            events[0] = new PolySpatialPointerEvent
            {
                colliderId = new PolySpatialInstanceID {id = colliderId},
                pointerIndex = pointerId,
                position = position,
                state = pointerState
            };
            PolySpatialSimulationHostImpl.SimHostOnInputEvent(PolySpatialInputType.Pointer, events.Length, events.GetUnsafePtr());
            events.Dispose();
        }
    }
}
#endif
