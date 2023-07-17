
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TransformAdapter : ANodeAdapter<TransformNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is TransformNode tnode)
            {
                var nodeType = tnode.conversionType switch
                {
                    ConversionType.Direction => MtlxNodeTypes.TransformVector,
                    ConversionType.Normal => MtlxNodeTypes.TransformNormal,
                    ConversionType.Position => MtlxNodeTypes.TransformPoint,
                    _ => null
                };

                string from = PositionAdapter.SpaceToMtlxString(tnode.spaceTransform.from);
                string to = PositionAdapter.SpaceToMtlxString(tnode.spaceTransform.to);

                var outputSlot = NodeUtils.GetPrimaryOutput(tnode);
                var inputSlot = NodeUtils.GetSlotByName(tnode, "In");

                string nodeName = NodeUtils.GetNodeName(node);
                var nodeData = graph.AddNode(nodeName, nodeType, MtlxDataTypes.Vector3);
                nodeData.AddPortValue("in", MtlxDataTypes.Vector3, SlotUtils.GetDefaultValue(inputSlot));
                nodeData.AddPortString("fromspace", MtlxDataTypes.String, from);
                nodeData.AddPortString("tospace", MtlxDataTypes.String, to);

                externals.AddExternalPort(outputSlot.slotReference, nodeName);
                externals.AddExternalPortAndEdge(inputSlot, nodeName, "in");
            }
        }
    }
}
