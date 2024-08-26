using System;
using UnityEngine;
using Unity.PolySpatial.Internals;

namespace Unity.PolySpatial
{
    /// <summary>
    /// PolySpatialImageBasedLight has been deprecated. Use VisionOSImageBasedLight instead.
    /// </summary>
    [Obsolete("PolySpatialImageBasedLight has been deprecated.  Use VisionOSImageBasedLight instead (UnityUpgradable) -> VisionOSImageBasedLight", true)]
    public class PolySpatialImageBasedLight : MonoBehaviour
    {
        /// <summary>
        /// Use VisionOSImageBasedLight.FirstSource.
        /// </summary>
        public Texture FirstSource { get; set; }

        /// <summary>
        /// Use VisionOSImageBasedLight.SecondSource.
        /// </summary>
        public Texture SecondSource { get; set; }

        /// <summary>
        /// Use VisionOSImageBasedLight.Blend.
        /// </summary>
        public float Blend { get; set; }

        /// <summary>
        /// Use VisionOSImageBasedLight.InheritsRotation.
        /// </summary>
        public bool InheritsRotation { get; set; }

        /// <summary>
        /// Use VisionOSImageBasedLight.IntensityExponent.
        /// </summary>
        public float IntensityExponent { get; set; }
    }
}
