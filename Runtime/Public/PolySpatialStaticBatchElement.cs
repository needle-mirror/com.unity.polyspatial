using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Provides a hint to the host that the containing GameObject will never move relative to a root ancestor
    /// (or the scene root), and can thus be merged with other batch elements to improve performance.
    /// </summary>
    public class PolySpatialStaticBatchElement : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The root object relative to which this object's transform will always stay fixed.")]
        GameObject m_Root;
        public GameObject Root
        {
            get => m_Root;
            set
            {
                m_Root = value;
                ObjectBridge.MarkDirty(this);
            }
        }
    }
}