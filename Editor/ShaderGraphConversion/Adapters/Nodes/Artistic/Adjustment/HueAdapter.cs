
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class HueAdapter : ANodeAdapter<HueNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.UnaryOp(MtlxNodeTypes.HsvAdjust, node, graph, externals, "Hue", coerceType:MtlxDataTypes.Color3);

            var inputName = NodeUtils.GetNodeName(node, "HueOffset");
            var inputSlot = NodeUtils.GetSlotByName(node, "Offset");
            var inputValue = SlotUtils.GetDefaultValue(inputSlot);

            var inputNodeData = graph.AddNode(inputName, MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3);
            inputNodeData.AddPortValue("in1", MtlxDataTypes.Float, inputValue);        // shift on hue
            inputNodeData.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 1 }); // noop on saturation
            inputNodeData.AddPortValue("in3", MtlxDataTypes.Float, new float[] { 1 }); // noop on value
            externals.AddExternalPortAndEdge(inputSlot, inputNodeData.name, "in1");

            graph.AddPortAndEdge(inputNodeData.name, nodeData.name, "amount", MtlxDataTypes.Vector3);
        }
    }
}
