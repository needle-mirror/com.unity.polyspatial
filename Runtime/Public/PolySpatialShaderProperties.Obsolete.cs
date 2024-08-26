using System;
using UnityEngine;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Shader properties specific to custom PolySpatial ShaderGraph nodes.
    /// </summary>
    [Obsolete("This class will be removed in PolySpatial 2.x onwards.")]
    public static class PolySpatialShaderProperties
    {
        /// <summary>
        /// Property name for the PolySpatial Volume To World matrix.
        /// </summary>
        [Obsolete("This property will be removed in 2.x onwards.")]
        public const string VolumeToWorld = "polySpatial_VolumeToWorld";

        /// <summary>
        /// Property name for the center of the object's bounds.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string ObjectBoundsCenter = "polySpatial_ObjectBoundsCenter";
        /// <summary>
        /// Property name for the extents of the object's bounds.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string ObjectBoundsExtents = "polySpatial_ObjectBoundsExtents";

        /// <summary>
        /// Property name for the directional lightmap texture.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string Lightmap = "polySpatial_Lightmap";
        /// <summary>
        /// Property name for the lightmap index
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string LightmapInd = "polySpatial_LightmapInd";
        /// <summary>
        /// Property name for the lightmap texture scale and offset.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string LightmapST = "polySpatial_LightmapST";

        /// <summary>
        /// SHAr component of the baked light probes.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string SHAr = "polySpatial_SHAr";

        /// <summary>
        /// SHAg component of the baked light probes.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string SHAg = "polySpatial_SHAg";

        /// <summary>
        /// SHAb component of the baked light probes.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string SHAb = "polySpatial_SHAb";

        /// <summary>
        /// SHBr component of the baked light probes.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string SHBr = "polySpatial_SHBr";

        /// <summary>
        /// SHBg component of the baked light probes.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string SHBg = "polySpatial_SHBg";

        /// <summary>
        /// SHBb component of the baked light probes.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string SHBb = "polySpatial_SHBb";

        /// <summary>
        /// SHC component of the baked light probes.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string SHC = "polySpatial_SHC";

        /// <summary>
        /// Max number of reflection probes supported by PolySpatial.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const int ReflectionProbeCount = 2;

        /// <summary>
        /// Prefix for reflection probe texture property names.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string ReflectionProbeTexturePrefix = "polySpatial_SpecCube";

        /// <summary>
        /// Prefix for reflection probe weight property names.
        /// </summary>
        [Obsolete("This property will be removed in PolySpatial 2.x onwards.")]
        public const string ReflectionProbeWeightPrefix = "polySpatial_SpecCubeWeight";

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
