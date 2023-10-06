using System;
using System.Diagnostics;
using System.Linq;
#if UNITY_EDITOR && ENABLE_MULTIPLE_DISPLAYS
using Unity.PolySpatial.InternalBridge;
#endif
using Unity.PolySpatial.Internals;
using UnityEngine;
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
    public class VolumeCamera : MonoBehaviour
    {
        internal static string PolySpatialLayerName => "PolySpatial";

        internal const PolySpatialVolumeCameraMode k_DefaultMode = PolySpatialVolumeCameraMode.Bounded;

        internal static readonly Vector3 k_DefaultDimensions = new Vector3(0.25f, 0.25f, 0.25f);

        [SerializeField]
        [Tooltip("The dimensions in Unity's world space of the volume camera's bounding box. Ignored if the volume camera is displayed in an Unbounded output.")]
        Vector3 m_Dimensions = k_DefaultDimensions;

        // This will lock the current ratio and scale the camera uniformly.
        // ReSharper disable once NotAccessedField.Local
        [SerializeField]
        bool m_IsUniformScale;

        [SerializeField]
        [Tooltip("The output configuration object for this volume camera, or None for default. Create new output configurations via VolumeCameraConfiguration scriptable objects.")]
        VolumeCameraConfiguration m_OutputConfiguration = null;

        [Tooltip("Only objects in the selected layers will be visible inside this Volume Camera.")]
        public LayerMask CullingMask = ~0x0;

        bool m_MatricesValid = false;

        Matrix4x4 m_VolumeToWorld;

        Matrix4x4 m_WorldToVolume;

        // The backing camera provides culling information to the simulation. Its parameters are generated
        // from the volume camera if the volume camera is Bounded, otherwise from the host camera.
        GameObject m_BackingCameraGO;
        Camera m_BackingCamera;

        internal int m_PolySpatialLayerMask;

        // Due to environment limitations, the host camera may not be available (returns null) in all modes, but
        // when present, the host camera serves as a kind of volume-specific "main camera." It reflects the position,
        // orientation, and other properties of the camera actually used to render the scene on the host, but
        // transformed into the simulation client's canonical space. It can be used to construct camera rays from the
        // user's perspective, oriented geometry can be rotated to face it, and so forth.
        //
        // Any time the mode of a volume camera changes, its host camera is assumed to be disabled until the host
        // supplies updated camera information by calling OnHostCameraChanged(). A host camera
        bool m_IsHostCameraAvailable;

        public enum PolySpatialVolumeCameraMode : int
        {
            Bounded = 0,
            Unbounded = 1,
        }

        public VolumeCameraConfiguration OutputConfiguration
        {
            get => m_OutputConfiguration;
            set
            {
                m_OutputConfiguration = value;
                SetDirty();
            }
        }

        VolumeCameraConfiguration ResolvedOutputConfiguration {
            get
            {
                if (m_OutputConfiguration != null)
                    return m_OutputConfiguration;
                return PolySpatialSettings.instance.DefaultVolumeCameraConfiguration;
            }
        }

        /// <summary>
        /// The mode this volume camera will display its content in, Bounded or Unbounded.
        /// </summary>
        public PolySpatialVolumeCameraMode OutputMode => ResolvedOutputConfiguration.Mode;

        /// <summary>
        /// The dimensions in meters of the actual output size of the volume camera. May be different than Dimensions,
        /// in which case the space described by Dimensions is scaled to fit the OutputDimensions.
        /// </summary>
        public Vector3 OutputDimensions => ResolvedOutputConfiguration.Dimensions;

        public Vector3 Dimensions
        {
            get
            {
                if (OutputMode == PolySpatialVolumeCameraMode.Unbounded)
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

        public Camera BackingCamera => m_BackingCamera.enabled ? m_BackingCamera : null;

        public bool IsHostCameraAvailable => m_IsHostCameraAvailable;

        // A matrix that translates from the unit cube at origin canonical volume space
        // back into the world space of the volume camera.
        public Matrix4x4 VolumeSpaceToWorldSpaceMatrix
        {
            get
            {
                UpdateMatrices();
                return m_VolumeToWorld;
            }
        }

        // A matrix that translates from world space into the unit cube at origin canonical volume space.
        public Matrix4x4 WorldSpaceToVolumeSpaceMatrix
        {
            get
            {
                UpdateMatrices();
                return m_WorldToVolume;
            }
        }

        internal void CalculateWorldToVolumeTRS(out Vector3 pos, out Quaternion rot, out Vector3 scale)
        {
            CalculateWorldToVolumeTRS(transform.localPosition, transform.localRotation, transform.localScale, Dimensions,
                out pos, out rot, out scale);
        }

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
            if (!PolySpatialSettings.instance.EnablePolySpatialRuntime)
            {
                this.enabled = false;
            }
        }

        void OnEnable()
        {
            m_PolySpatialLayerMask = LayerMask.NameToLayer(PolySpatialLayerName);

            m_BackingCameraGO = new GameObject("Culling Camera");
            m_BackingCameraGO.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            m_BackingCameraGO.layer = m_PolySpatialLayerMask;
            m_BackingCameraGO.transform.SetParent(transform);

            m_BackingCamera = m_BackingCameraGO.AddComponent<Camera>();
#if ENABLE_MULTIPLE_DISPLAYS
#if UNITY_EDITOR
            m_BackingCamera.targetDisplay = DisplayUtilityBridge.GetDisplayIndices().Max() + 1; // something high, so it doesn't conflict with users configured displays
#else
            m_BackingCamera.targetDisplay = Display.displays.Length;
#endif
#endif
            m_BackingCamera.depth = 500;

            m_IsHostCameraAvailable = false;
        }

        void OnDisable()
        {
            Object.Destroy(m_BackingCameraGO);

            m_IsHostCameraAvailable = false;
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
                SetDirty();
                transform.hasChanged = false;
            }
        }

        internal void UpdateConfiguration()
        {
            UpdateBoundedCullingCamera();
            SetDirty();
        }

        // TODO -- save all these values, so that we stop re-setting the camera values every frame!

        internal void UpdateBoundedCullingCamera()
        {
            if (OutputMode != PolySpatialVolumeCameraMode.Bounded)
                return;

            if (m_BackingCamera == null)
                return;

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
            public UnityEngine.Vector3 position;
            public UnityEngine.Quaternion rotation;
            public UnityEngine.Vector3 scale;
            public bool isOrthographic;
            public float orthographicHalfSize;
            public float aspectRatio;
            public float fieldOfViewY;
            public float focalLength;
            public float nearClip;
            public float farClip;
            public int cullingMask;
        }

        // Keep the HostCamera in sync with the "main camera" of the host app
        internal void UpdateUnboundedCullingCamera(PolySpatialCameraDataExternal hostData)
        {
            // hostData's TRS should be in world coordinates
            var camx = m_BackingCameraGO.transform;
            var cam = m_BackingCamera;

            var lossyScale = transform.lossyScale;

            camx.position = hostData.position;
            camx.rotation = hostData.rotation;
            camx.localScale = Vector3.Scale(hostData.scale, new Vector3(1.0f / lossyScale.x, 1.0f / lossyScale.y, 1.0f / lossyScale.z));

            cam.orthographic = hostData.isOrthographic;
            cam.orthographicSize = hostData.orthographicHalfSize;
            cam.aspect = hostData.aspectRatio;
            cam.fieldOfView = hostData.fieldOfViewY;
            cam.focalLength = hostData.focalLength;
            cam.nearClipPlane = hostData.nearClip;
            cam.farClipPlane = hostData.farClip;
            cam.cullingMask = hostData.cullingMask & ~m_PolySpatialLayerMask;

            // The host is not required to invoke this function, but once it has done so, the host camera is assumed to
            // be in sync until the volume camera's mode changes.
            m_IsHostCameraAvailable = true;

            cam.enabled = true;
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

        [Conditional("UNITY_EDITOR")]
        internal void OnDrawGizmosSelected()
        {
            // Draw a green box that shows what will be included in 1x1x1 units of canonical space
            if (OutputMode == PolySpatialVolumeCameraMode.Bounded)
            {
                Gizmos.matrix = gameObject.transform.localToWorldMatrix;
                Gizmos.color = new Color(0, 0.5f, 0, 0.2f);
                Gizmos.DrawCube(Vector3.zero, Dimensions);
            }
        }
    }
}
