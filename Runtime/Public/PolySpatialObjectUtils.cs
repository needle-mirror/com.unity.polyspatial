using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    public static class PolySpatialObjectUtils
    {
        // <summary>
        // Marks the specified render texture as changed so that it will be updated over PolySpatial.
        // </summary>
        // <param name="renderTexture">The render texture to mark as changed.</param>
        public static void MarkDirty(RenderTexture renderTexture)
        {
            ObjectBridge.MarkDirty(renderTexture);
        }
    }
}