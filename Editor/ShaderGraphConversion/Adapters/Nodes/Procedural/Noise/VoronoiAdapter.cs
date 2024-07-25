using System;
using System.Text;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class VoronoiAdapter : AbstractUVNodeAdapter<VoronoiNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Voronoi-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Voronoi", $@"
inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
{{
    UV = frac(sin(mul(UV, float2x2(15.27, 47.63, 99.41, 89.98))) * 46839.32);
    return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
}}

void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
{{
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);
    float2 lattice;
    float2 offset;
    float d;

    {((Func<string>)(() => 
    {
        // No support for loops in the HLSL parser yet; we have to unroll it into source.
        StringBuilder steps = new();
        for (var y = -1; y <= 1; ++y)
        {
            for (var x = -1; x <= 1; ++x)
            {
                steps.AppendLine($"lattice = float2({x}, {y});");
                steps.AppendLine("offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);");
                steps.AppendLine("d = distance(lattice + offset, f);");
                steps.AppendLine("res = (d < res.x) ? float3(d, offset.x, offset.y) : res;");
            }
        }
        return steps.ToString();
    }))()}

    Out = res.x;
    Cells = res.y;
}}", "Unity_Voronoi_float");
        }
    }
}
