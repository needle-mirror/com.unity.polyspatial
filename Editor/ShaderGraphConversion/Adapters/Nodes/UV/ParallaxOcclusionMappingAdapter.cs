using System;
using System.Text;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ParallaxOcclusionMappingAdapter : ANodeAdapter<ParallaxOcclusionMappingNode>
    {
        public override string SupportDetails(AbstractMaterialNode node)
        {
            var details = QuickNode.GetUVSupportDetails((UVMaterialSlot)NodeUtils.GetSlotByName(node, "UVs"));

            // We require that "Steps" be a constant so that we can unroll the loop.
            if (NodeUtils.GetInputByName(node, "Steps").isConnected)
                details += $"{(details.Length == 0 ? "" : "  ")}Steps input must be constant (unconnected).";

            return details;
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var component = ParallaxMappingAdapter.GetSwizzleComponent(((ParallaxOcclusionMappingNode)node).channel);

            // The ParallaxOcclusionMappingNode implementation clamps Steps to [1, 256] to avoid crashes.
            var numSteps = Mathf.Clamp(
                Mathf.RoundToInt(((Vector1MaterialSlot)NodeUtils.GetInputByName(node, "Steps")).value), 1, 256);
            var stepSize = 1.0f / numSteps;
            
            // For the sake of readability (to make the HLSL source better match what the original looks like),
            // we generate the code to fetch the height inline using this utility function.  In the original
            // code, this is handled by creative use of the preprocessor.
            string ComputePerPixelHeightDisplacement(string offset)
            {
                return $"SAMPLE_TEXTURE2D_LOD(Heightmap, HeightmapSampler, tmpUVs + {offset}, LOD).{component}";
            }

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Parallax-Occlusion-Mapping-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "ParallaxOcclusionMapping", $@"
float3 GetDisplacementObjectScale_float()
{{
    // See https://github.cds.internal.unity3d.com/unity/unity/blob/66c5f7befac7f5061f425973bcb1304c38eadcb6/Packages/com.unity.shadergraph/Editor/Data/Nodes/UV/ParallaxOcclusionMappingNode.cs#L133
    float3 objectScale = float3(1.0, 1.0, 1.0);
    float4x4 worldTransform = unity_WorldToObject;
    objectScale.x = length(mul(float4(1, 0, 0, 0), worldTransform));
    objectScale.z = length(mul(float4(0, 0, 1, 0), worldTransform));
    return objectScale;
}}
void ParallaxOcclusionMapping_float(
    UnityTexture2D Heightmap, UnitySamplerState HeightmapSampler, float Amplitude,
    float2 UVs, float2 Tiling, float2 Offset, float2 PrimitiveSize, float LOD,
    float LODThreshold, out float PixelDepthOffset, out float2 ParallaxUVs)
{{
    // See https://github.cds.internal.unity3d.com/unity/unity/blob/66c5f7befac7f5061f425973bcb1304c38eadcb6/Packages/com.unity.shadergraph/Editor/Data/Nodes/UV/ParallaxOcclusionMappingNode.cs#L194
    float3 viewDirTS = normalize(mul(polySpatial_WorldToTangent, float4(polySpatial_WorldSpaceViewDirection, 0)).xyz);

    float3 tmpViewDir = viewDirTS * GetDisplacementObjectScale_float().xzy;
    float tmpNdotV = tmpViewDir.z;
    float tmpMaxHeight = Amplitude * 0.01;
    tmpMaxHeight *= 2.0 / (abs(Tiling.x) + abs(Tiling.y));

    float2 tmpUVSpaceScale = Tiling * tmpMaxHeight / PrimitiveSize;
    float3 tmpViewDirUV = normalize(float3(tmpViewDir.xy * tmpUVSpaceScale, tmpViewDir.z));

    // See https://github.cds.internal.unity3d.com/unity/unity/blob/66c5f7befac7f5061f425973bcb1304c38eadcb6/Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl#L24
    float2 parallaxMaxOffsetTS = (tmpViewDirUV.xy / -tmpViewDirUV.z);
    float2 texOffsetPerStep = parallaxMaxOffsetTS * {stepSize};

    float2 tmpUVs = UVs * Tiling + Offset;

    float2 texOffsetCurrent = float2(0.0, 0.0);
    float prevHeight = {ComputePerPixelHeightDisplacement("texOffsetCurrent")};
    texOffsetCurrent += texOffsetPerStep;
    float currHeight = {ComputePerPixelHeightDisplacement("texOffsetCurrent")};
    float rayHeight = 1.0 - {stepSize};
    float loopBroken = 0.0;

    {((Func<string>)(() => 
    {
        // No support for loops in the HLSL parser yet; we have to unroll it into source.
        StringBuilder steps = new();
        for (var i = 0; i < numSteps; ++i)
        {
            steps.AppendLine("loopBroken = loopBroken || currHeight > rayHeight;");

            steps.AppendLine("prevHeight = loopBroken ? prevHeight : currHeight;");
            steps.AppendLine($"rayHeight = loopBroken ? rayHeight : rayHeight - {stepSize};");
            steps.AppendLine(
                "texOffsetCurrent = loopBroken ? texOffsetCurrent : texOffsetCurrent + texOffsetPerStep;");
            steps.AppendLine(
                $"currHeight = loopBroken ? currHeight : {ComputePerPixelHeightDisplacement("texOffsetCurrent")};");
        }
        return steps.ToString();
    }))()}

    // This uses the secant method from the original, since it is the active path as determined by the preprocessor.
    float pt0 = rayHeight + {stepSize};
    float pt1 = rayHeight;
    float delta0 = pt0 - prevHeight;
    float delta1 = pt1 - currHeight; 

    float intersectionHeight;
    float2 offset;
    float delta;
    float deltaLessThanThreshold;
    float deltaGreaterThanThreshold;

    {((Func<string>)(() => 
    {
        // No support for loops in the HLSL parser yet; we have to unroll it into source.
        StringBuilder steps = new();
        for (var i = 0; i < 3; ++i)
        {
            steps.AppendLine("intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);");
            steps.AppendLine($"offset = texOffsetPerStep * (1 - intersectionHeight) * {numSteps};");
            steps.AppendLine($"currHeight = {ComputePerPixelHeightDisplacement("offset")};");
            steps.AppendLine($"delta = intersectionHeight - currHeight;");

            steps.AppendLine($"deltaLessThanThreshold = delta < -0.01;");
            steps.AppendLine($"deltaGreaterThanThreshold = delta > 0.01;");
            steps.AppendLine($"delta0 = deltaGreaterThanThreshold ? delta : delta0;");
            steps.AppendLine($"delta1 = deltaLessThanThreshold ? delta : delta1;");
            steps.AppendLine($"pt0 = deltaGreaterThanThreshold ? intersectionHeight : pt0;");
            steps.AppendLine($"pt1 = deltaLessThanThreshold ? intersectionHeight : pt1;");
        }
        return steps.ToString();
    }))()}

    offset *= (1.0 - saturate(LOD - LODThreshold));

    PixelDepthOffset = (tmpMaxHeight - currHeight * tmpMaxHeight) / max(tmpNdotV, 0.0001);
    ParallaxUVs = tmpUVs + offset;
}}", "ParallaxOcclusionMapping_float");
        }
    }
}