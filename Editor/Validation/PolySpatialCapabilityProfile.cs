using Unity.XR.CoreUtils.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Capabilities
{
    /// <summary>
    /// Class that represents a PolySpatial capability profile.
    /// These profiles are used to enable PolySpatial validation rules for scene objects.
    /// </summary>
    /// <seealso cref="CapabilityDictionary"/>
#if POLYSPATIAL_INTERNAL
    [UnityEngine.CreateAssetMenu(menuName = "PolySpatial/PolySpatialCapabilityProfileTest")]
#endif
    public class PolySpatialCapabilityProfile : CapabilityProfile, ICapabilityModifier
    {
        [SerializeField]
        CapabilityDictionary m_Capabilities;

        public bool TryGetCapabilityValue(string capabilityKey, out bool capabilityValue) =>
            m_Capabilities.TryGetValue(capabilityKey, out capabilityValue);
    }
}
