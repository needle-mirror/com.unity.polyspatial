using System;
using UnityEngine;

namespace Unity.PolySpatial
{
    // Utility functions related to WebCamTextures, which can't be referenced in the DLL, because referencing them in
    // any way without the "Camera Usage Description" set generates a build error.  Instead, we require users to define
    // POLYSPATIAL_ENABLE_WEBCAM.  On visionOS, the WebCamTexture shows a feed of the user's Spatial Persona.
    internal static class WebCamTextureUtils
    {
        internal static bool TryGetWebCamTextureType(out Type type)
        {
#if POLYSPATIAL_ENABLE_WEBCAM
            type = typeof(WebCamTexture);
            return true;
#else
            type = null;
            return false;
#endif
        }

        internal static Color32[] GetWebCamTexturePixels32(Texture texture)
        {
#if POLYSPATIAL_ENABLE_WEBCAM
            return ((WebCamTexture)texture).GetPixels32();
#else
            throw new NotSupportedException("POLYSPATIAL_ENABLE_WEBCAM is not set.");
#endif
        }
    }
}