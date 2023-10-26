
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class GradientNoiseAdapter : AbstractUVNodeAdapter<GradientNoiseNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("Scale", "amplitude");
            var outputNode = QuickNode.NaryOp(MtlxNodeTypes.PerlinNoise2d, node, graph, externals, "GradientNoise", portMap);
            outputNode.AddPort("texcoord", MtlxDataTypes.Vector2);

            var uvSlot = (UVMaterialSlot)NodeUtils.GetSlotByName(node, "UV");
            if (!uvSlot.isConnected)
            {
                var uvNode = QuickNode.CreateUVNode(
                    graph, NodeUtils.GetNodeName(node, "GradientNoiseUV"), (int)uvSlot.channel);
                graph.AddEdge(uvNode.name, outputNode.name, "texcoord");
            }
            else
            {
                externals.AddExternalPortAndEdge(uvSlot, outputNode.name, "texcoord");
            }
        }
    }
}
