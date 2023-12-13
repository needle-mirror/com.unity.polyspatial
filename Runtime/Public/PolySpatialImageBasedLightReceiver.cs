using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    public class PolySpatialImageBasedLightReceiver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The image-based light to apply to this object.")]
        PolySpatialImageBasedLight m_ImageBasedLight;

        /// <summary>
        /// A reference to the image based light to apply to this object and its descendants.
        /// </summary>
        public PolySpatialImageBasedLight ImageBasedLight
        {
            get => m_ImageBasedLight;
            set
            {
                m_ImageBasedLight = value;
                this.MarkDirty();
            }
        }
    }
}