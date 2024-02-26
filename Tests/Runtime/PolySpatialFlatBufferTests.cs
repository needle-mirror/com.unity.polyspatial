using System;
using NUnit.Framework;
using UnityEngine;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using System.Reflection;
using FlatSharp.Attributes;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

namespace Tests.Runtime.Functional
{
    [TestFixture]
    public class PolySpatialFlatBufferTests
    {
        /// <summary>
        /// Finds all structs with the FlatBufferStruct attribute in the PSL Core assembly
        /// </summary>
        /// <returns>HashSet of System.Types for the flatbuffer structs</returns>
        static HashSet<Type> GetPolySpatialFlatBufferStructTypes(bool valueTypesOnly)
        {
            HashSet<Type> pslTypes = new();

            var pslCore = Assembly.Load("Unity.PolySpatial.Core");

            foreach (Type type in pslCore.GetTypes())
            {
                // inherit is false since the structReader_* classes inherit from PSL structs
                if (type.GetCustomAttributes(typeof(FlatBufferStructAttribute), inherit: false).Length > 0)
                {
                    if (valueTypesOnly && !type.IsValueType)
                        continue;

                    pslTypes.Add(type);
                }
            }

            return pslTypes;
        }

        /// <summary>
        /// Asserts that a struct type has padding added so that it's size matches it's aligned size
        /// </summary>
        static void ValidateStructPadding<TType>(HashSet<Type> pslTypes) where TType : struct
        {
            var pslType = typeof(TType);

            var typeSize = UnsafeUtility.SizeOf<TType>();
            var typeAlignment = UnsafeUtility.AlignOf<TType>();

            // AlignOf doesn't always return an alignment that is a power of 2, so round up to next power of 2 if necessary
            var roundedAlignment = (int)Math.Pow(2, Math.Ceiling(Math.Log(typeAlignment, 2)));

            // Create AlignedSize in the same way as ChangeList.cs
            var alignedSize = ((typeSize - 1) & ~(roundedAlignment - 1)) + roundedAlignment;

            Assert.AreEqual(alignedSize, typeSize, $"Padding is required for struct {pslType.Name}. Current Size: {typeSize}, Alignment: {roundedAlignment}, AlignedSize: {alignedSize}");

            pslTypes.Remove(pslType);
        }

        /// <summary>
        /// Types with the FlatBufferStruct attribute should be C# structs, if not they are most likely missing the `fs_valueStruct` schema attribute
        /// </summary>
        [Test]
        public void Test_FlatbufferStructs_AreValueTypes()
        {
            foreach (var pslType in GetPolySpatialFlatBufferStructTypes(valueTypesOnly: false))
            {
                Assert.IsTrue(pslType.IsValueType, $"{pslType.Name} is a C# class but a flatbuffer struct. Most likely the `fs_valueStruct` attribute is missing from the schema.");
            }
        }

        /// <summary>
        /// The flatbuffer structs generated for C# don't have padding added to the end, but the structs in swift do.
        /// We have to manually add padding to the schema so that the sizeof() each struct matches between C# and swift.
        /// </summary>
        [Test]
        public unsafe void Test_FlatBufferStruct_Padding()
        {
            // Filter out non-value types s
            var pslTypes = GetPolySpatialFlatBufferStructTypes(valueTypesOnly: true);

            // The UnsafeUtility can provide SizeOf for a System.Type at runtime, but not AlignOf.
            // So instead of iterating though the set of types, pass the type as a generic parameter to a 
            // helper function that is called for each struct and remove the type from the set as 
            // we validate the size. If the set isn't empty at the end of the tests then fail because
            // a new struct was added without updating this test.
            ValidateStructPadding<PolySpatialAssetID>(pslTypes);
            ValidateStructPadding<PolySpatialCameraData>(pslTypes);
            ValidateStructPadding<PolySpatialPose>(pslTypes);
            ValidateStructPadding<PolySpatialInstanceID>(pslTypes);
            ValidateStructPadding<PolySpatialChangeListEntityData>(pslTypes);
            ValidateStructPadding<PolySpatialTextureMipData>(pslTypes);
            ValidateStructPadding<PolySpatialPlatformCapabilities>(pslTypes);
            ValidateStructPadding<PolySpatialInputCapabilities>(pslTypes);
            ValidateStructPadding<PolySpatialOutputCapabilities>(pslTypes);
            ValidateStructPadding<PolySpatialARCapabilities>(pslTypes);
            ValidateStructPadding<PolySpatialEnvironmentCapabilities>(pslTypes);
            ValidateStructPadding<PolySpatialBoneWeight>(pslTypes);
            ValidateStructPadding<PolySpatialColliderData>(pslTypes);
            ValidateStructPadding<PolySpatialColorTextureMapData>(pslTypes);
            ValidateStructPadding<PolySpatialFrameData>(pslTypes);
            ValidateStructPadding<PolySpatialGameObjectData>(pslTypes);
            ValidateStructPadding<PolySpatialHoverEffectData>(pslTypes);
            ValidateStructPadding<PolySpatialImageBasedLightData>(pslTypes);
            ValidateStructPadding<PolySpatialImageBasedLightReceiverData>(pslTypes);
            ValidateStructPadding<PolySpatialLightData>(pslTypes);
            ValidateStructPadding<PolySpatialLightProbeData>(pslTypes);
            ValidateStructPadding<PolySpatialLightmapData>(pslTypes);
            ValidateStructPadding<PolySpatialLitParticleMaterial>(pslTypes);
            ValidateStructPadding<PolySpatialOcclusionMaterial>(pslTypes);
            ValidateStructPadding<PolySpatialPBRMaterial>(pslTypes);
            ValidateStructPadding<PolySpatialParticleBurst>(pslTypes);
            ValidateStructPadding<PolySpatialParticleCurveKey>(pslTypes);
            ValidateStructPadding<PolySpatialParticleGradientAlphaKey>(pslTypes);
            ValidateStructPadding<PolySpatialParticleGradientColorKey>(pslTypes);
            ValidateStructPadding<PolySpatialParticleSubEmitter>(pslTypes);
            ValidateStructPadding<PolySpatialPointerEvent>(pslTypes);
            ValidateStructPadding<PolySpatialReflectionProbeData>(pslTypes);
            ValidateStructPadding<PolySpatialScalarTextureMapData>(pslTypes);
            ValidateStructPadding<PolySpatialSortingOrder>(pslTypes);
            ValidateStructPadding<PolySpatialStereoRendererComponent>(pslTypes);
            ValidateStructPadding<PolySpatialStereoRendererFramebuffer>(pslTypes);
            ValidateStructPadding<PolySpatialSubMesh>(pslTypes);
            ValidateStructPadding<PolySpatialTexture>(pslTypes);
            ValidateStructPadding<PolySpatialTextureColor>(pslTypes);
            ValidateStructPadding<PolySpatialTextureMapData>(pslTypes);
            ValidateStructPadding<PolySpatialTextureScalar>(pslTypes);
            ValidateStructPadding<PolySpatialUIGraphicData>(pslTypes);
            ValidateStructPadding<PolySpatialUnlitMaterial>(pslTypes);
            ValidateStructPadding<PolySpatialUnlitParticleMaterial>(pslTypes);
            ValidateStructPadding<PolySpatialWindowState>(pslTypes);
            ValidateStructPadding<PolySpatialPingData>(pslTypes);

            Assert.AreEqual(0, pslTypes.Count, $"Untested flatbuffer structs: {string.Join(", ", pslTypes)}");
        }
    }
}
