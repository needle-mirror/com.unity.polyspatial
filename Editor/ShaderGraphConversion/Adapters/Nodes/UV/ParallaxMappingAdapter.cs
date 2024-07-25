using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ParallaxMappingAdapter : ANodeAdapter<ParallaxMappingNode>
    {
        public override string SupportDetails(AbstractMaterialNode node)
        {
            return QuickNode.GetUVSupportDetails((UVMaterialSlot)NodeUtils.GetSlotByName(node, "UVs"));
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var component = ((ParallaxMappingNode)node).channel switch
            {
                Channel.Red => 'r',
                Channel.Green => 'g',
                Channel.Blue => 'b',
                Channel.Alpha => 'a',
                var channel => throw new NotSupportedException($"Unknown color channel: {channel}"),
            };

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Parallax-Mapping-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "ParallaxMapping", $@"
// Refer to
// https://github.cds.internal.unity3d.com/unity/unity/blob/93a364f095f55c0e7616dc8d1638d6c6c37b5ad5/Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl#L46
float height = Amplitude * 0.01 * (SAMPLE_TEXTURE2D(Heightmap, HeightmapSampler, UVs).{component} - 0.5);
float3 v = normalize(mul(polySpatial_WorldToTangent, float4(polySpatial_WorldSpaceViewDirection, 0)).xyz);
v.z += 0.42;
ParallaxUVs = UVs + height * (v.xy / v.z);");
        }
    }
}