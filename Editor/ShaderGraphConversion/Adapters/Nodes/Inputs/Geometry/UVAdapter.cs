
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class UVAdapter : ANodeAdapter<UVNode>
    {
        public override string SupportDetails(AbstractMaterialNode node)
        {
            return node is UVNode uvNode ? QuickNode.GetUVSupportDetails((int)uvNode.uvChannel) : "";
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var uvNode = graph.AddNode(NodeUtils.GetNodeName(node, "UV"),
                MtlxNodeTypes.GeomTexCoord, MtlxDataTypes.Vector2);

            var channel = (int)(node as UVNode).uvChannel;
            uvNode.AddPortValue("index", MtlxDataTypes.Integer, new float[] { channel });

            var multiplyNode = graph.AddNode(NodeUtils.GetNodeName(node, "Multiply"),
                MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2);
            multiplyNode.AddPortValue("in1", MtlxDataTypes.Vector2, new[] {1.0f, -1.0f});
            graph.AddPortAndEdge(uvNode.name, multiplyNode.name, "in2", MtlxDataTypes.Vector2);

            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Add, node, graph, externals, "Add");

            // mtlx doesn't support 4-channel UVs?
            nodeData.datatype = MtlxDataTypes.Vector2;

            nodeData.AddPortValue("in1", MtlxDataTypes.Vector2, new[] {0.0f, 1.0f});
            graph.AddPortAndEdge(multiplyNode.name, nodeData.name, "in2", MtlxDataTypes.Vector2);
        }
    }
}
