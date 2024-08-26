using System;
using System.Collections.Generic;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Defines the drawing and input processing order for a group of GameObjects.
    /// </summary>
    /// <remarks>
    /// <see cref="UnityEngine.Renderer"/> instances on the GameObjects in the group,
    /// and optionally on any child GameObjects, are drawn in the specified order,
    /// starting with the lowest value. Thus objects with a higher order draw on top of
    /// those with a lower order. The sorting occurs in visionOS platform code
    /// and overrides other, Unity-defined renderer sorting methods, such as
    /// <see cref="UnityEngine.Rendering.SortingGroup"/>.
    ///
    /// The specified order also determines the order in which members of the group
    /// capture user input. GameObjects with a higher order value capture input before
    /// any members of the group below them. GameObjects do not need a renderer
    /// object, but do need a <see cref="UnityEngine.Collider"/>. This sorting does not
    /// change physics interactions, just input resolution.
    ///
    /// A GameObject can only be a member of one visionOS sorting group. If you try to add
    /// a GameObject to a second group, the action is ignored.
    ///
    /// Sorting groups have no effect on how renderers are sorted against objects outside the group.
    /// They only affect how objects inside the group are sorted relative to each other.
    ///
    /// You should not change sorting groups frequently at runtime. Such changes can
    /// be expensive, especially if you have enable the **Apply To Descendants** option
    /// within the group members.
    ///
    /// For additional information, refer to [visionOS Sorting Groups](xref:psl-vos-sorting-group).
    /// </remarks>
    [DisallowMultipleComponent]
    public class VisionOSSortingGroup : MonoBehaviour
    {
        /// <summary>
        /// Struct defining a renderer and its sort order.
        /// </summary>
        [Serializable]
        public struct RendererSorting
        {
            /// <summary>
            /// Order within the sort group. Lower values indicate they should be drawn first.
            /// </summary>
            [Tooltip("Order within the sort group. Lower values indicate they should be drawn first.")]
            public int order;

            /// <summary>
            /// The renderer the sort order should apply to.
            /// </summary>
            /// <remarks>
            /// A GameObject can only belong to one sorting group. If you try to add
            /// a GameObject to a second group, the action is ignored.
            ///
            /// If you set <see cref="applyToDescendants"/> to <c>true</c>, then any child
            /// GameObjects are also included in the sorting group at their parent's order.
            /// (The parent does not need to have a renderer or collider in this case.)
            /// </remarks>
            [Tooltip("The renderer the sort order should apply to.")]
            public GameObject renderer;

            /// <summary>
            /// Whether the sort order should also be applied to all descendant renderers.
            /// </summary>
            [Tooltip("Whether the sort order should also be applied to all descendant renderers.")]
            public bool applyToDescendants;
        }

        /// <summary>
        /// Enum defining depth pass types.
        /// </summary>
        public enum DepthPass: int
        {
            /// Draws depth of renderer after drawing all color.
            PostPass,

            /// Draws depth of renderer before drawing any color.
            PrePass,

            /// Draws depth and color at the same time.
            Unseparated,
        }

        [SerializeField]
        [Tooltip("When depth is drawn with respect to color.")]
        DepthPass m_DepthPass;

        /// <summary>
        /// Defines when depth is drawn with respect to color.
        /// </summary>
        public DepthPass depthPass
        {
            get => m_DepthPass;
            set
            {
                m_DepthPass = value;
                ObjectBridge.MarkDirty(this);
            }
        }

        [SerializeField]
        [Tooltip("List of all renderers belonging to this sort group.")]
        List<RendererSorting> m_Renderers = new List<RendererSorting>();

        /// <summary>
        /// The members of this sort group.
        /// </summary>
        /// <remarks>
        /// At runtime, changes to the list are only applied if you set the property to a list
        /// reference. Changes made to the list members using the reference do not
        /// send the update to RealityKit.
        /// </remarks>
        /// <example>
        /// The following example illustrates how to add a new member to a sorting group at runtime
        /// in a way that sends the changes to RealityKit.
        /// <code>
        /// using UnityEngine;
        /// using Unity.PolySpatial;
        /// using System.Collections.Generic;
        ///
        /// public class ChangeSortGroup : MonoBehaviour
        /// {
        ///    public VisionOSSortingGroup vosSortingGroup;
        ///
        ///    public void AddToSortingGroup(GameObject gameObject, int order)
        ///    {
        ///        List<VisionOSSortingGroup.RendererSorting> groupMembers = vosSortingGroup.renderers;
        ///        var newMember = new VisionOSSortingGroup.RendererSorting();
        ///        newMember.renderer = gameObject;
        ///        newMember.order = order;
        ///        newMember.applyToDescendants = false;
        ///
        ///        vosSortingGroup.renderers = groupMembers;
        ///    }
        /// }
        /// </code>
        /// </example>
        public List<RendererSorting> renderers
        {
            get => m_Renderers;
            set
            {
                m_Renderers = value;
                ObjectBridge.MarkDirty(this);
            }
        }
    }
}
