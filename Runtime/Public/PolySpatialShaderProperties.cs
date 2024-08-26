using UnityEngine;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Shader properties specific to custom PolySpatial ShaderGraph nodes.
    /// </summary>
    static class PolySpatialShaderPropertiesInternal
    {
        internal const string VolumeToWorld = "polySpatial_VolumeToWorld";

        internal const string ObjectBoundsCenter = "polySpatial_ObjectBoundsCenter";
        internal const string ObjectBoundsExtents = "polySpatial_ObjectBoundsExtents";

        internal const string Lightmap = "polySpatial_Lightmap";
        internal const string LightmapInd = "polySpatial_LightmapInd";
        internal const string LightmapST = "polySpatial_LightmapST";

        internal const string SHAr = "polySpatial_SHAr";
        internal const string SHAg = "polySpatial_SHAg";
        internal const string SHAb = "polySpatial_SHAb";
        internal const string SHBr = "polySpatial_SHBr";
        internal const string SHBg = "polySpatial_SHBg";
        internal const string SHBb = "polySpatial_SHBb";
        internal const string SHC = "polySpatial_SHC";

        internal const int ReflectionProbeCount = 2;

        internal const string ReflectionProbeTexturePrefix = "polySpatial_SpecCube";
        internal const string ReflectionProbeWeightPrefix = "polySpatial_SpecCubeWeight";

        internal static readonly int VolumeToWorldID = Shader.PropertyToID(VolumeToWorld);

        internal static readonly int ObjectBoundsCenterID = Shader.PropertyToID(ObjectBoundsCenter);
        internal static readonly int ObjectBoundsExtentsID = Shader.PropertyToID(ObjectBoundsExtents);

        internal static readonly int LightmapID = Shader.PropertyToID(Lightmap);
        internal static readonly int LightmapIndID = Shader.PropertyToID(LightmapInd);
        internal static readonly int LightmapSTID = Shader.PropertyToID(LightmapST);

        internal static readonly int SHArID = Shader.PropertyToID(SHAr);
        internal static readonly int SHAgID = Shader.PropertyToID(SHAg);
        internal static readonly int SHAbID = Shader.PropertyToID(SHAb);
        internal static readonly int SHBrID = Shader.PropertyToID(SHBr);
        internal static readonly int SHBgID = Shader.PropertyToID(SHBg);
        internal static readonly int SHBbID = Shader.PropertyToID(SHBb);
        internal static readonly int SHCID = Shader.PropertyToID(SHC);

        internal static readonly int[] ReflectionProbeTextureIDs =
            GetReflectionProbePropertyIDs(ReflectionProbeTexturePrefix);
        internal static readonly int[] ReflectionProbeWeightIDs =
            GetReflectionProbePropertyIDs(ReflectionProbeWeightPrefix);

        static int[] GetReflectionProbePropertyIDs(string prefix)
        {
            var propertyIDs = new int[ReflectionProbeCount];
            for (var i = 0; i < ReflectionProbeCount; ++i)
            {
                propertyIDs[i] = Shader.PropertyToID(prefix + i);
            }
            return propertyIDs;
        }
    }
}
