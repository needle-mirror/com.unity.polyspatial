using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

#if HAS_URP_14_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.PolySpatial
{
    internal static class UniversalRenderPipelineUtils
    {
        internal static void ApplyToUnsupportedURPTypes(Action<Type> action)
        {
#if HAS_URP_14_OR_NEWER
            action(typeof(DecalProjector));
#endif
        }

        internal static void ConfigureUniversalAdditionalLightData(this Light light, uint renderingLayers)
        {
#if HAS_URP_14_OR_NEWER
            light.GetUniversalAdditionalLightData().renderingLayers = renderingLayers;
#endif
        }

        internal static void DestroyUniversalAdditionalLightData(this GameObject gameObject)
        {
#if HAS_URP_14_OR_NEWER
            // We can't use GameObjectExtensions.DestroyBackingComponent/ObjectExtensions.DestroyAppropriately,
            // as that would cause a circular dependency.
            if (!gameObject.TryGetComponent<UniversalAdditionalLightData>(out var lightData))
                return;

    #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityObject.DestroyImmediate(lightData);
                return;
            }
    #endif
            UnityObject.Destroy(lightData);
#endif
        }
    }
}