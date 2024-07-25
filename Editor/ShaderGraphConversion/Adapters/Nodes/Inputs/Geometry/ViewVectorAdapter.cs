using System;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ViewVectorAdapter : ANodeAdapter<ViewVectorNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            const string k_WorldViewVector = "mul(unity_ObjectToWorld, float4(polySpatial_ObjectSpaceViewVector, 0))";
            QuickNode.CompoundOp(node, graph, externals, sgContext, "ViewVector", ((ViewVectorNode)node).space switch
            {
                CoordinateSpace.Object => "Out = polySpatial_ObjectSpaceViewVector;",
                CoordinateSpace.View => $"Out = mul(UNITY_MATRIX_V, {k_WorldViewVector}).xyz;",
                CoordinateSpace.World => $"Out = {k_WorldViewVector}.xyz;",
                CoordinateSpace.Tangent => $"Out = mul(polySpatial_WorldToTangent, {k_WorldViewVector}).xyz;",
                var space => throw new NotSupportedException($"Unsupported space: {space}"),
            });
        }
    }
}