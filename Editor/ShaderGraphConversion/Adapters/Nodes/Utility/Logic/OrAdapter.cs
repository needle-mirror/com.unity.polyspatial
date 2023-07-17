
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class OrAdapter : ANodeAdapter<OrNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // To achieve an 'or' operation with the nodes provided by Mtlx, we can do something like this:
            // (abs(A) + abs(B)) > 0 ? 1 : 0 -- haven't done boolean algebra in a bit-- this is fiiine.

            var aSlot = NodeUtils.GetSlotByName(node, "A");
            var bSlot = NodeUtils.GetSlotByName(node, "B");
            var outputSlot = NodeUtils.GetPrimaryOutput(node);

            var aValue = SlotUtils.GetDefaultValue(aSlot);
            var bValue = SlotUtils.GetDefaultValue(bSlot);

            // abs(a)
            var aNode = graph.AddNode(NodeUtils.GetNodeName(node, "OrA"), MtlxNodeTypes.Absolute, MtlxDataTypes.Float);
            aNode.AddPortValue("in", MtlxDataTypes.Float, aValue);
            externals.AddExternalPortAndEdge(aSlot, aNode.name, "in"); // input a

            // abs(b)
            var bNode = graph.AddNode(NodeUtils.GetNodeName(node, "OrB"), MtlxNodeTypes.Absolute, MtlxDataTypes.Float);
            bNode.AddPortValue("in", MtlxDataTypes.Float, bValue);
            externals.AddExternalPortAndEdge(bSlot, bNode.name, "in"); // input b

            // abs(a) + abs(b)
            var addNode = graph.AddNode(NodeUtils.GetNodeName(node, "OrSum"), MtlxNodeTypes.Add, MtlxDataTypes.Float);
            graph.AddPortAndEdge(aNode.name, addNode.name, "in1", MtlxDataTypes.Float);
            graph.AddPortAndEdge(bNode.name, addNode.name, "in2", MtlxDataTypes.Float);

            // abs(a) + abs(b) > 0 ? 1 : 0
            var compNode = graph.AddNode(NodeUtils.GetNodeName(node, "OrComp"), MtlxNodeTypes.IfGreater, MtlxDataTypes.Float);
            graph.AddPortAndEdge(addNode.name, compNode.name, "value1", MtlxDataTypes.Float);
            compNode.AddPortValue("value2", MtlxDataTypes.Float, new float[] { 0 });
            compNode.AddPortValue("in1", MtlxDataTypes.Float, new float[] { 1 });
            compNode.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 0 });

            externals.AddExternalPort(outputSlot.slotReference, compNode.name);
        }
    }
}
