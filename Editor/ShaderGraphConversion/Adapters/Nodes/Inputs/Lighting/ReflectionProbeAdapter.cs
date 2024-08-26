using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ReflectionProbeAdapter : ANodeAdapter<ReflectionProbeNode>
    {
        internal static string GetProbeContributionExpr(int probeIndex, string reflectVector, string lod)
        {
            // Note: this only supports the dLDR encoding (which appears to be the default).
            const float kReflectionProbeHdrMultiplier = 4.59f;
            var textureProperty = PolySpatialShaderPropertiesInternal.ReflectionProbeTexturePrefix + probeIndex;
            return $@"
SAMPLE_TEXTURECUBE_LOD({textureProperty}, sampler{textureProperty}, {reflectVector}, {lod}).rgb *
{kReflectionProbeHdrMultiplier}";
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Refer to:
            // https://github.cds.internal.unity3d.com/unity/unity/blob/e837d3a2b4d8abf7605e479584d89ca0722967b5/Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl#L85
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "ReflectionProbe",
                $"Out = {GetProbeContributionExpr(0, "reflect(-ViewDir, Normal)", "LOD")};");
        }
    }
}
