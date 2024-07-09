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
        /// <summary>
        /// The root object relative to which the attached GameObject's transform will always stay fixed.
        /// </summary>
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

        /// <summary>
        /// If true, all descendants of the attached GameObject will also be considered static.
        /// </summary>
        [SerializeField]
        [Tooltip("If true, all descendants of this object will also be considered static.")]
        bool m_ApplyToDescendants;
        public bool ApplyToDescendants
        {
            get => m_ApplyToDescendants;
            set
            {
                m_ApplyToDescendants = value;
                ObjectBridge.MarkDirty(this);
            }
        }
    }
}