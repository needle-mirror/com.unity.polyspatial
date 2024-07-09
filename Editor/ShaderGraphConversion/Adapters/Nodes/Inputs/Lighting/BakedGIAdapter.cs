using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class BakedGIAdapter : ANodeAdapter<BakedGINode>
    {
        internal static string GetLightmapContributionExpr(string uv, bool transformUV, string normalWS, string output)
        {
            // Note: this only supports the dLDR encoding (which appears to be the default).
            const float kLightmapHdrMultiplier = 4.59f;

            // https://github.cds.internal.unity3d.com/unity/unity/blob/1ade3ed2dfc1932d8c8427253060d0edaa1663d3/Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl#L251
            return $@"
float2 uv = {(transformUV ?
    $"{uv} * {PolySpatialShaderProperties.LightmapST}.xy + {PolySpatialShaderProperties.LightmapST}.zw" : uv)};
float4 direction = SAMPLE_TEXTURE2D(
    {PolySpatialShaderProperties.LightmapInd}, sampler{PolySpatialShaderProperties.LightmapInd}, uv);
float4 encodedIlluminance = SAMPLE_TEXTURE2D(
    {PolySpatialShaderProperties.Lightmap}, sampler{PolySpatialShaderProperties.Lightmap}, uv);
float3 illuminance = encodedIlluminance.rgb * {kLightmapHdrMultiplier};
float halfLambert = dot({normalWS}, direction.xyz - 0.5) + 0.5;
float3 {output} = illuminance * halfLambert / max(1e-4, direction.w);";
        }

        internal static string GetLightProbeContributionExpr(string normalWS, string output)
        {
            // https://github.cds.internal.unity3d.com/unity/unity/blob/f5741f9e623093a5514dc13a534a4044e0d7e0ec/Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl#L116
            return $@"
float4 vA = float4({normalWS}, 1.0);
float4 vB = {normalWS}.xyzz * {normalWS}.yzzx;
float vC = {normalWS}.x * {normalWS}.x - {normalWS}.y * {normalWS}.y;
float3 x1 = float3(
    dot({PolySpatialShaderProperties.SHAr}, vA),
    dot({PolySpatialShaderProperties.SHAg}, vA),
    dot({PolySpatialShaderProperties.SHAb}, vA));
float3 x2 = float3(
    dot({PolySpatialShaderProperties.SHBr}, vB),
    dot({PolySpatialShaderProperties.SHBg}, vB),
    dot({PolySpatialShaderProperties.SHBb}, vB));
float3 x3 = {PolySpatialShaderProperties.SHC}.rgb * vC;
float3 {output} = max(float3(0, 0, 0), x1 + x2 + x3);";
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Baked-GI-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "BakedGI", $@"
{GetLightmapContributionExpr("StaticUV", ((BakedGINode)node).applyScaling.isOn, "Normal", "lightmapGI")}
{GetLightProbeContributionExpr("Normal", "lightProbeGI")}
Out = LIGHTMAP_ON ? lightmapGI : lightProbeGI;");
        }
    }
}