using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Utility methods for working with PolySpatial objects.
    /// </summary>
    public static class PolySpatialObjectUtils
    {
        /// <summary>
        /// Marks the specified render texture as changed so that it will be updated over PolySpatial.
        /// </summary>
        /// <param name="renderTexture">The render texture to mark as changed.</param>
        public static void MarkDirty(RenderTexture renderTexture)
        {
            ObjectBridge.MarkDirty(renderTexture);
        }

#if POLYSPATIAL_ENABLE_WEBCAM
        /// <summary>
        /// Marks the specified web cam texture as changed so that it will be updated over PolySpatial.
        /// </summary>
        /// <param name="webCamTexture">The web cam texture to mark as changed.</param>
        public static void MarkDirty(WebCamTexture renderTexture)
        {
            ObjectBridge.MarkDirty(renderTexture);
        }
#endif

        /// <summary>
        /// Marks the specified renderer as changed so that it will be updated over PolySpatial.
        /// </summary>
        /// <param name="renderer">The renderer to mark as changed.</param>
        public static void MarkDirty(Renderer renderer)
        {
            // TODO (LXR-3608): Remove this method when #47495 and #47498 have landed on our target versions.
            ObjectBridge.MarkDirty(renderer);
        }

        /// <summary>
        /// Return an identifier usable by PolySpatial backends to obtain platform-specific resources.
        /// </summary>
        /// <param name="go">The GameObject to get the identifier for.</param>
        /// <returns>The identifier for the object.</returns>
        public static ulong GetPolySpatialIdentifier(GameObject go)
        {
            int iid = go.GetInstanceID();
            return (ulong)UnsafeUtility.As<int, uint>(ref iid);
        }

        /// <summary>
        /// Return the GameObject corresponding to the given identifier, if any.
        /// </summary>
        /// <param name="id">The PolySpatial identifier to get the GameObject for.</param>
        /// <returns>The GameObject, or null.</returns>
        public static GameObject GetGameObjectForPolySpatialIdentifier(ulong id)
        {
            uint prefix = (uint)(id >> 32);
            uint iidpart = (uint)(id & 0xffffffff);

            if (prefix != 0)
            {
                return null;
            }

            int iid = (int)UnsafeUtility.As<uint, int>(ref iidpart);
            var obj = ObjectBridge.FindObjectFromInstanceID(iid);
            if (obj is GameObject go)
            {
                return go;
            }

            return null;
        }
    }
}
