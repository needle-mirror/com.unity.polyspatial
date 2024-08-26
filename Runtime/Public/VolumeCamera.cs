using System;
using System.Diagnostics;
using System.Linq;
#if UNITY_EDITOR && ENABLE_MULTIPLE_DISPLAYS
using Unity.PolySpatial.Internals.InternalBridge;
#endif
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Unity.PolySpatial
{
    // In bounded mode, a "volume camera" captures content within an oriented bounding box (OBB) and transforms this
    // content to a "canonical volume," similar to the canonical view volume of a regular camera: a unit box centered
    // at the origin. Typically, this content is then displayed on a host by a corresponding "volume renderer",
    // by mapping this canonical volume out to the host volume renderer's own, distinct OBB. The effect is that
    // 3D content within the volume camera's bounds is transformed, rotated, stretched and/or squashed to fill the
    // volume renderer's bounds.
    //
    // In unbounded mode, everything works similar, except that the volume camera and volume renderer each define an
    // unbounded 3-space rather than a bounded 3-space volume.

    /// <summary>
    /// Specifies the portion of the scene to render in a volume window. A volume window is
    /// similar to a standard computer window with the addition of a third dimension.
    /// Every volume camera has an associated volume window.
    /// </summary>
    /// <remarks>
    /// In metal mode, most of the properties of the VolumeCamera are not used. Instead,
    /// the scene is rendered by the main scene camera (in stereo).
    ///
    /// You can only have one volume camera in unbounded or metal mode at the same time.
    /// You can have multiple bounded volume cameras in addition to a volume camera in
    /// unbounded or metal mode.
    /// </remarks>
    [Icon("Camera Gizmo")]
    public class VolumeCamera : MonoBehaviour
    {
        internal static string PolySpatialLayerName => "PolySpatial";

        internal const PolySpatialVolumeCameraMode k_DefaultMode = PolySpatialVolumeCameraMode.Bounded;

        internal static readonly Vector3 k_DefaultDimensions = new Vector3(0.25f, 0.25f, 0.25f);

        [SerializeField]
        [Tooltip("The dimensions in Unity's world space of the volume camera's bounding box. Ignored if the volume camera is displayed in an Unbounded output.")]
        Vector3 m_Dimensions = k_DefaultDimensions;

        // This will lock the current ratio and scale the camera uniformly.
        [SerializeField]
        // ReSharper disable once NotAccessedField
#pragma warning disable CS0414 // Field is assigned but its value is never used
        bool m_IsUniformScale;
#pragma warning restore CS0414

        [SerializeField]
        [Tooltip("The output Volume Camera Window Configuration object for this volume camera, or None for default. Create new volume camera configurations via the Asset Create menu.")]
        VolumeCameraWindowConfiguration m_OutputConfiguration = null;

        [SerializeField]
        [Tooltip("Only objects in the selected layers will be visible inside this Volume Camera.")]
        LayerMask m_CullingMask = ~0x0;

        /// <summary>
        /// If true, a window is automatically opened for this volume camera when loaded. If false, the window must be opened manually via OpenWindow().
        /// </summary>
        [SerializeField]
        [Tooltip("If true, a window is automatically opened for this volume camera when loaded. If false, the window must be opened manually via OpenWindow().")]
        public bool OpenWindowOnLoad = true;

        bool m_MatricesValid = false;

        Matrix4x4 m_VolumeToWorld;

        Matrix4x4 m_WorldToVolume;

        // The backing camera provides culling information to the simulation. Its parameters are generated
        // from the volume camera if the volume camera is Bounded, otherwise from the host camera.
        GameObject m_BackingCameraGO;
        Camera m_BackingCamera;

        internal Camera BackingCamera => m_BackingCamera;

        internal int m_PolySpatialLayerIndex;
        internal int m_PolySpatialLayerMask;

        // these change when we are notified by the host
        bool m_WindowOpen = false;

        bool m_WindowFocused = false;

        // this is set by OpenWindow/CloseWindow, which the VC tracker looks at
        internal bool m_RequestedWindowOpenState = false;

        #if UNITY_EDITOR
        // fields used in the editor that wont carry on to runtime
        [SerializeField]
#pragma warning disable CS0414 // Field is assigned but its value is never used
        bool m_ShowVolumeCameraEventsFoldout;
#pragma warning restore CS0414
        #endif

        /// <summary>
        /// Only objects in the selected layers will be visible inside this Volume Camera.
        /// </summary>
        public LayerMask CullingMask
        {
            get => m_CullingMask;
            set
            {
                m_CullingMask = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        /// <summary>
        /// Enum to define the mode of the volume camera, Bounded or Unbounded.
        /// </summary>
        public enum PolySpatialVolumeCameraMode : int
        {
            /// <summary>
            /// Mode where only content contained within the camera's bounding box appears inside your Volume Window.
            /// </summary>
            Bounded = 0,
            /// <summary>
            /// Mode where the entire Unity scene is shown in a visionOS Immersive Space.
            /// </summary>
            Unbounded = 1,
        }

        /// <summary>
        /// Enum for all possible window events that can occur on a volume camera.
        /// </summary>
        public enum WindowEvent : int
        {
            /// <summary>
            ///  The volume camera window was opened.
            /// </summary>
            Opened,

            /// <summary>
            /// The volume camera window was resized. See the OutputDimensions and ContentDimensions to
            /// figure out what the volume camera window was resized to.
            /// </summary>
            Resized,

            /// <summary>
            /// The volume camera window either received focus or lost focus.
            /// </summary>
            Focused,

            /// <summary>
            ///  The volume camera window was closed due to being backgrounded.
            /// </summary>
            Backgrounded,

            /// <summary>
            ///  The volume camera window was closed due to being dismissed.
            /// </summary>
            Closed,
        }


        /// <summary>
        /// Struct to encapsulate a change in window state.
        /// </summary>
        public struct WindowState
        {
            /// <summary>
            /// The change in state that just occurred for this window. Change of states can mean actions such as the window
            /// was opened or the window was backgrounded.
            /// </summary>
            public WindowEvent WindowEvent;

            /// <summary>
            /// The actual dimensions of the window in world space, or `Vector3.zero` if the volume is unbounded.
            /// </summary>
            public Vector3 OutputDimensions;

            /// <summary>
            /// The dimensions that your Volume Camera's dimensions are mapped to, in Unity's coordinate units.
            /// (On visionOS, these will typically be the same, but they may not be on other platforms.)
            /// </summary>
            public Vector3 ContentDimensions;

            /// <summary>
            /// The mode this volume camera will display its content in, Bounded or Unbounded.
            /// </summary>
            public PolySpatialVolumeCameraMode Mode;

            /// <summary>
            /// When windowEvent is set to WindowEvent.Focused, this will indicate whether it has received focus or lost it.
            /// </summary>
            public bool IsFocused;
        }

        /// <summary>
        /// The output Volume Camera Window Configuration object for this volume camera, or None for default.
        /// Create new volume camera configurations via the Asset Create menu.
        /// </summary>
        public VolumeCameraWindowConfiguration WindowConfiguration
        {
            get => m_OutputConfiguration;
            set
            {
                m_OutputConfiguration = value;
                SetDirty();
            }
        }

        VolumeCameraWindowConfiguration ResolvedWindowConfiguration {
            get
            {
                if (m_OutputConfiguration != null)
                    return m_OutputConfiguration;
                return PolySpatialSettings.instance.DefaultVolumeCameraWindowConfiguration;
            }
        }

        /// <summary>
        /// The mode this volume camera will display its content in, Bounded or Unbounded.
        /// </summary>
        public PolySpatialVolumeCameraMode WindowMode => ResolvedWindowConfiguration?.Mode ?? PolySpatialVolumeCameraMode.Unbounded;

        /// <summary>
        /// The dimensions in meters of the actual output size of the volume camera. May be different than Dimensions,
        /// in which case the space described by Dimensions is scaled to fit the OutputDimensions.
        /// </summary>
        public Vector3 OutputDimensions => ResolvedWindowConfiguration?.Dimensions ?? Vector3.one;

        /// <summary>
        /// Defines the (unscaled) size of the camera's bounding box. The box is centered at the position of the
        /// **VolumeCamera**’s transform.
        /// </summary>
        /// <remarks>
        /// The effective, world space dimensions of the bounding box are calculated by
        /// multiplying the Dimensions by the transform's scale.
        ///
        /// When you set the volume camera **Mode** to **Bounded**, the camera only displays GameObjects
        /// within the scaled bounding box.  A bounding box is not used when you set the **Mode** to **Unbounded**.
        /// </remarks>
        public Vector3 Dimensions
        {
            get
            {
                if (WindowMode == PolySpatialVolumeCameraMode.Unbounded)
                    return Vector3.one;
                return m_Dimensions;
            }
            set
            {
                if (!ValidateDimensions(value, out var errorMsg))
                {
                    Debug.LogError(errorMsg);
                }
                else
                {
                    m_Dimensions = value;
                }

                UpdateMatrices();
                UpdateConfiguration();
            }
        }

        /// <summary>
        /// A matrix that translates from the unit cube at origin canonical volume space back into the world space of the volume camera.
        /// </summary>
        public Matrix4x4 VolumeSpaceToWorldSpaceMatrix
        {
            get
            {
                UpdateMatrices();
                return m_VolumeToWorld;
            }
        }

        /// <summary>
        /// A matrix that translates from world space into the unit cube at origin canonical volume space.
        /// </summary>
        public Matrix4x4 WorldSpaceToVolumeSpaceMatrix
        {
            get
            {
                UpdateMatrices();
                return m_WorldToVolume;
            }
        }

        /// <summary>
        /// Returns true if a window is open and showing the contents of this volume camera.
        /// </summary>
        public bool WindowOpen => m_WindowOpen;

        /// <summary>
        /// Returns true if a window that is showing the contents of this volume camera is focused.
        /// </summary>
        public bool WindowFocused => m_WindowFocused;

        /// <summary>
        /// An event that is triggered when this volume camera's window changes state. Changing states can mean window actions such as
        /// the window opening or the window becoming unfocused.
        /// </summary>
        [SerializeField]
        [Tooltip("An event that is triggered when this volume camera's window changes state.")]
        public UnityEvent<WindowState> OnWindowEvent = new();

        /// <summary>
        /// Request that a window is opened to show the contents of this volume camera. Does nothing
        /// if the window is already open.
        /// </summary>
        public void OpenWindow()
        {
            if (WindowOpen)
                return;

            m_RequestedWindowOpenState = true;
            SetDirty();
        }

        /// <summary>
        /// Request that the OS close the window that is showing the contents of this volume camera. Does nothing
        /// if the window is not open.
        /// </summary>
        public void CloseWindow()
        {
            if (!WindowOpen)
                return;

            m_RequestedWindowOpenState = false;
            SetDirty();
        }

        internal void CalculateWorldToVolumeTRS(out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            CalculateWorldToVolumeTRS(transform.localPosition, transform.localRotation, transform.localScale, Dimensions,
                out pos, out rot, out scale);
        }

        //
        // For a given volume camera position, rotation, and scale in Unity space, calculate the TRS that will transform
        // that into a unit cube at the origin.
        //
        internal static void CalculateWorldToVolumeTRS(Vector3 camPos, Quaternion camRot, Vector3 camScale, Vector3 camDim,
            out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            var totalScale = Vector3.Scale(camScale, camDim);

            if (totalScale.x == 0.0f || totalScale.y == 0.0f || totalScale.z == 0.0f)
            {
                throw new InvalidOperationException($"VolumeCamera: totalScale has invalid component. cam: {camScale} dim: {camDim}");
            }

            var invScale = new Vector3(1.0f / totalScale.x, 1.0f / totalScale.y, 1.0f / totalScale.z);
            var invRot = Quaternion.Inverse(camRot);

            var posWithScale = Vector3.Scale(invScale, -camPos);
            var posWithScaleAndRot = invRot * posWithScale;

            pos = posWithScaleAndRot;
            rot = invRot;
            scale = invScale;
        }

        void UpdateMatrices()
        {
            if (m_MatricesValid)
                return;

            CalculateWorldToVolumeTRS(transform.position, transform.rotation, transform.lossyScale,
                Dimensions, out var pos, out var rot, out var scale);

            m_WorldToVolume = Matrix4x4.TRS(pos, rot, scale);
            m_VolumeToWorld = m_WorldToVolume.inverse;
            m_MatricesValid = true;
        }

        void Awake()
        {
            if (!PolySpatialBridge.RuntimeEnabled)
            {
                this.enabled = false;
                return;
            }

            if (OpenWindowOnLoad)
            {
                OpenWindow();
            }
        }

        void OnEnable()
        {
            m_PolySpatialLayerIndex = LayerMask.NameToLayer(PolySpatialLayerName);
            m_PolySpatialLayerMask = m_PolySpatialLayerIndex != -1 ? 1 << m_PolySpatialLayerIndex : 0;

            UpdateBoundedSimulationCamera();
        }

        void OnDisable()
        {
            Object.Destroy(m_BackingCameraGO);
        }

        void SetDirty()
        {
            m_MatricesValid = false;
            this.MarkDirty();
        }

        void Update()
        {
            // PolySpatialVolumeCameras are innately tied to their
            // transforms and can not really be separated. This
            // means that transform changes need to also trigger
            // component changes so that the PolySpatial tracking system
            // can pick them up and handle them appropriately.
            if (transform.hasChanged)
            {
                transform.hasChanged = false;
                UpdateBoundedSimulationCamera();
                SetDirty();
            }
        }

        internal void UpdateConfiguration()
        {
            UpdateBoundedSimulationCamera();
            SetDirty();
        }

        // TODO -- save all these values, so that we stop re-setting the camera values every frame!

        internal void UpdateBoundedSimulationCamera()
        {
            if (WindowMode != PolySpatialVolumeCameraMode.Bounded)
                return;

            if (m_BackingCamera == null)
            {
                m_BackingCameraGO = new GameObject("PolySpatial Simulation Support");
                m_BackingCameraGO.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                m_BackingCameraGO.transform.SetParent(transform);
                m_BackingCameraGO.layer = m_PolySpatialLayerIndex != -1 ? m_PolySpatialLayerIndex : transform.gameObject.layer;

                m_BackingCamera = m_BackingCameraGO.AddComponent<Camera>();

#if ENABLE_MULTIPLE_DISPLAYS
#if UNITY_EDITOR
                m_BackingCamera.targetDisplay =
                    DisplayUtilityBridge.GetDisplayIndices().Max() + 1; // something high, so it doesn't conflict with users configured displays
#else
                m_BackingCamera.targetDisplay = Display.displays.Length;
#endif
#endif
                // TODO [LXR-3611]: What is an appropriate depth value. Can it match the depth of the cameras created in UnityBackend?
                m_BackingCamera.depth = 500;
            }

            m_BackingCamera.orthographic = true;

            // Compute the actual dimensions of the volume by scaling the dimensions by the volume camera's transform
            // scale
            var worldSpaceDimensions = Vector3.Scale(gameObject.transform.lossyScale, Dimensions);

            // Position the camera so that its view volume matches the volume camera's volume. This consists of
            // several parts:
            // 1. -worldSpaceDimensions.z / 2.0f: The volume camera is centered relative to its GO, but a
            // camera's view volume is in the positive z direction relative to GO. Thus, we need to offset the camera
            // GO by half the bound's dimensions
            // 2. -1: The nearClip plane is set to 1 below (because it can't be zero), so the camera position needs to
            //  be offset by a corresponding amount to align the nearClipPlane with the zMin plane of the bounds
            // 3. Vector3.Scale(unscaledPosition, m_BackingCameraGO.transform.localScale): The backing camera's scale
            //  is the inverted scale of its parent; apply this here so that the offset is appropriately scaled, too.
            var unscaledPosition = new Vector3(0, 0, -1 - worldSpaceDimensions.z / 2.0f);
            m_BackingCameraGO.transform.localPosition = Vector3.Scale(unscaledPosition, m_BackingCameraGO.transform.localScale);
            m_BackingCameraGO.transform.localRotation = Quaternion.identity;

            m_BackingCamera.aspect = worldSpaceDimensions.x / worldSpaceDimensions.y;
            m_BackingCamera.orthographicSize = worldSpaceDimensions.y / 2;
            m_BackingCamera.nearClipPlane = 1;
            m_BackingCamera.farClipPlane = 1 + worldSpaceDimensions.z;
            m_BackingCamera.cullingMask = CullingMask & ~m_PolySpatialLayerMask;

            m_BackingCamera.enabled = true;
        }

        // We need a copy because we can't reference the type inside PolySpatial.Core
        internal struct PolySpatialCameraDataExternal
        {
            public UnityEngine.Vector3 worldPosition;
            public UnityEngine.Quaternion worldRotation;
            public bool isOrthographic;
            public float orthographicHalfSize;
            public float aspectRatio;
            public float fieldOfViewY;
            public float focalLength;
            public float nearClip;
            public float farClip;
            public int cullingMask;
            public Color backgroundColor;
        }

        Vector3 m_LastOutputDimensions;
        Vector3 m_LastContentDimensions;

        internal void UpdateWindowState(WindowState state)
        {
            OnWindowEvent.Invoke(state);
        }

        internal static bool ValidateDimensions(Vector3 dim, out string errorMsg)
        {
            var ok = true;
            errorMsg = "";

            ok = dim.x > 0.0f && dim.y > 0.0f && dim.z > 0.0f;
            if (!ok)
            {
                errorMsg = $"Dimensions must be greater than 0.";
                return ok;
            }

            return ok;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (WindowMode == PolySpatialVolumeCameraMode.Bounded)
            {
                Gizmos.matrix = gameObject.transform.localToWorldMatrix;
                Gizmos.color = new Color(1, 1, 1, 0.1f);
                Gizmos.DrawCube(Vector3.zero, Dimensions);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "Camera Gizmo");
            DrawVolumeEdges(Dimensions);
        }

        void DrawVolumeEdges(Vector3 dimensions)
        {
            if (WindowMode != PolySpatialVolumeCameraMode.Bounded || WindowConfiguration == null)
                return;

            Gizmos.matrix = gameObject.transform.localToWorldMatrix;
            Gizmos.color = new Color(1, 1, 1, 0.7f);
            const float edgeSize = 0.2f;//as a percentage

            var halfDimX = dimensions.x / 2;
            var halfDimY = dimensions.y / 2;
            var halfDimZ = dimensions.z / 2;

            var corner1 = new Vector3(halfDimX, halfDimY, halfDimZ);
            var corner2 = new Vector3(halfDimX, halfDimY, -halfDimZ);
            var corner3 = new Vector3(halfDimX, -halfDimY, halfDimZ);
            var corner4 = new Vector3(halfDimX, -halfDimY, -halfDimZ);
            var corner5 = new Vector3(-halfDimX, halfDimY, halfDimZ);
            var corner6 = new Vector3(-halfDimX, halfDimY, -halfDimZ);
            var corner7 = new Vector3(-halfDimX, -halfDimY, halfDimZ);
            var corner8 = new Vector3(-halfDimX, -halfDimY, -halfDimZ);

            DrawSegment(corner1, corner2, edgeSize);
            DrawSegment(corner1, corner3, edgeSize);
            DrawSegment(corner1, corner5, edgeSize);
            DrawSegment(corner2, corner4, edgeSize);
            DrawSegment(corner2, corner6, edgeSize);
            DrawSegment(corner3, corner4, edgeSize);
            DrawSegment(corner3, corner7, edgeSize);
            DrawSegment(corner4, corner8, edgeSize);
            DrawSegment(corner5, corner6, edgeSize);
            DrawSegment(corner5, corner7, edgeSize);
            DrawSegment(corner6, corner8, edgeSize);
            DrawSegment(corner7, corner8, edgeSize);
        }

        void DrawSegment(Vector3 a, Vector3 b, float edgeSizePct)
        {
            var ab = new Vector3(b.x - a.x, b.y - a.y, b.z - a.z);
            var sqrLengthAB =  Vector3.SqrMagnitude(ab);

            Vector3 edgeVec = ab / sqrLengthAB * sqrLengthAB * edgeSizePct/2;
            Vector3 segmentPosA = new Vector3(a.x + edgeVec.x, a.y + edgeVec.y, a.z + edgeVec.z);
            Vector3 segmentPosB = new Vector3(b.x - edgeVec.x, b.y - edgeVec.y, b.z - edgeVec.z);

            Gizmos.DrawLine(a, segmentPosA);
            Gizmos.DrawLine(b, segmentPosB);
        }
#endif
    }
}
