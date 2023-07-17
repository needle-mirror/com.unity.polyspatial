
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RotateAdapter : AbstractUVNodeAdapter<RotateNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var centerSlot = NodeUtils.GetSlotByName(node, "Center");
            var uvSlot = NodeUtils.GetSlotByName(node, "UV");
            var rotationSlot = NodeUtils.GetSlotByName(node, "Rotation");
            var outputSlot = NodeUtils.GetPrimaryOutput(node);


            // setup the center input.
            var center = graph.AddNode(NodeUtils.GetNodeName(node, "RotateCenter"), MtlxNodeTypes.Constant, MtlxDataTypes.Vector2);
            center.AddPortValue("value", MtlxDataTypes.Vector2, SlotUtils.GetDefaultValue(centerSlot));
            externals.AddExternalPortAndEdge(centerSlot, center.name, "value");

            // apply the offset
            var offset = graph.AddNode(NodeUtils.GetNodeName(node, "RotateOffset"), MtlxNodeTypes.Subtract, MtlxDataTypes.Vector2);
            QuickNode.HandleUVSlot((UVMaterialSlot)uvSlot, NodeUtils.GetNodeName(node, "RotateUV"), offset.name, "in1", graph, externals);
            graph.AddPortAndEdge(center.name, offset.name, "in2", MtlxDataTypes.Vector2);

            // do the actual rotation.
            var rotate = graph.AddNode(NodeUtils.GetNodeName(node, "Rotate"), MtlxNodeTypes.Rotate2d, MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(offset.name, rotate.name, "in", MtlxDataTypes.Vector2);
            rotate.AddPortValue("amount", MtlxDataTypes.Float, SlotUtils.GetDefaultValue(rotationSlot));
            externals.AddExternalPortAndEdge(rotationSlot, rotate.name, "amount");

            // add the offset back in.
            var output = graph.AddNode(NodeUtils.GetNodeName(node, "RotateOutput"), MtlxNodeTypes.Add, MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(center.name, output.name, "in1", MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(rotate.name, output.name, "in2", MtlxDataTypes.Vector2);

            externals.AddExternalPort(outputSlot.slotReference, output.name);
        }
    }
}
