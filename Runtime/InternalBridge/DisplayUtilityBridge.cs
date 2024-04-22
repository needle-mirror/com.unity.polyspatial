#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Unity.PolySpatial.Internals
{
    /// <summary>
    /// Bridge class for accessing the DisplayUtility trunk methods.
    /// </summary>
    static class DisplayUtilityBridge
    {
        internal static int[] GetDisplayIndices()
        {
            return DisplayUtility.GetDisplayIndices();
        }

        internal static GUIContent[] GetDisplayNames()
        {
            return DisplayUtility.GetDisplayNames();
        }
    }
}
#endif
