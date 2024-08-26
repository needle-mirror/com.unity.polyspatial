using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Sets a GameObject and its children to be illuminated by a designated <see cref="Unity.PolySpatial.PolySpatialImageBasedLight"/>.
    /// </summary>
    /// <remarks>
    /// Add a `VisionOSImageBasedLightReceiver` component to a GameObject and set its
    /// <see cref="Unity.PolySpatial.PolySpatialImageBasedLightReceiver.ImageBasedLight"/>
    /// property to the <see cref="Unity.PolySpatial.PolySpatialImageBasedLight"/> object that
    /// should provide illumination. All <see cref="UnityEngine.MeshRenderer"/> objects on the
    /// same GameObject or its children receive illumination from the designated light source.
    ///
    /// You can prevent a child GameObject from being illuminated by an image-based light source
    /// by assigning the child its own `VisionOSImageBasedLightReceiver` and leaving the
    /// **Image Based Light** property null.
    ///
    /// A `VisionOSImageBasedLightReceiver` object maps directly to a [RealityKit ImageBasedLightReceiverComponent](https://developer.apple.com/documentation/realitykit/imagebasedlightreceivercomponent).
    ///
    /// Refer to [Image Based Lighting](xref:psl-vos-image-based-light) for additional information about
    /// how to set up and use image based lights.
    /// </remarks>
    [Tooltip("Image Based Light Receiver")]
    public class VisionOSImageBasedLightReceiver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The image-based light to apply to this object.")]
        VisionOSImageBasedLight m_ImageBasedLight;

        /// <summary>
        /// A reference to the image based light to apply to this object and its descendants.
        /// </summary>
        public VisionOSImageBasedLight ImageBasedLight
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
