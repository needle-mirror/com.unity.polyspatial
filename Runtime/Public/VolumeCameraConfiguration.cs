using UnityEngine;

namespace Unity.PolySpatial
{
    [CreateAssetMenu(fileName = "VolumeCameraConfiguration", menuName = "PolySpatial/Volume Camera Configuration")]
    public class VolumeCameraConfiguration : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Whether the camera should restrict the rendered content to objects within its bounding box or be unbounded.")]
        VolumeCamera.PolySpatialVolumeCameraMode m_Mode = VolumeCamera.PolySpatialVolumeCameraMode.Bounded;

        [SerializeField]
        [Tooltip("The dimensions that are mapped to a unit cube in the destination view. Only available when the mode is set to Bounded.")]
        Vector3 m_OutputDimensions = Vector3.one;

        // This will lock the current ratio and scale the camera uniformly.
        // ReSharper disable once NotAccessedField.Local
        [SerializeField]
        bool m_IsUniformScale;

        public VolumeCamera.PolySpatialVolumeCameraMode Mode
        {
            get => m_Mode;
        }

        public Vector3 Dimensions
        {
            get => m_OutputDimensions;
        }

        internal VolumeCamera.PolySpatialVolumeCameraMode PrivateMode
        {
            get => m_Mode;
            set => m_Mode = value;
        }

        internal Vector3 PrivateDimensions
        {
            get => m_OutputDimensions;
            set => m_OutputDimensions = value;
        }
    }
}
