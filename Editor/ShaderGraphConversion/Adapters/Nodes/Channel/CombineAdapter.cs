
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CombineAdapter : ANodeAdapter<CombineNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {


            var nodeName = NodeUtils.GetNodeName(node, "Combine");

            var inputs = new List<MaterialSlot>();
            var outputs = new List<MaterialSlot>();

            node.GetInputSlots(inputs);
            node.GetOutputSlots(outputs);

            var nodeData = graph.AddNode(nodeName, MtlxNodeTypes.Combine4, MtlxDataTypes.Color4);

            // Let's hope they are in the correct order.
            for(int i = 0; i < 4; ++i)
            {
                var portName = $"in{i+1}";
                nodeData.AddPortValue(portName, MtlxDataTypes.Float, new float[] { 0 });
                externals.AddExternalPortAndEdge(inputs[i], nodeName, portName);
            }

            // Due to the way the rest of the systems work, we can rely on concretization to enforce typing,
            // then let type resolution/swizzling handle the rest. So we can promote all output slots to this node.
            foreach (var output in outputs)
                externals.AddExternalPort(output.slotReference, nodeName);
        }
    }
}
