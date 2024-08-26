using UnityEngine;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Indicates that the parent GameObject should cast a grounding shadow.
    /// </summary>
    /// <remarks>
    /// Add a `VisionOSGroundingShadow` component to a GameObject to provide a hint
    /// that the object should cast a RealityKit grounding shadow. The GameObject must
    /// also have a MeshRenderer (to cast the shadow).
    ///
    /// For additional information, refer to [Grounding Shadows](xref:psl-vos-grounding-shadow).
    /// </remarks>
    public class VisionOSGroundingShadow : MonoBehaviour
    {
    }
}
