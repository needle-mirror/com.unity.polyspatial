using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalReconstructZAdapter : ANodeAdapter<NormalReconstructZNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            //Normal Reconstruct Z:
            //$precision reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
            //$precision3 normalVector = $precision3(In.x, In.y, reconstructZ);
            //Out = normalize(normalVector);

            var slot = NodeUtils.GetSlotByName(node, "In");

            var inputNode = graph.AddNode(NodeUtils.GetNodeName(node, "ReconstructZIn"), MtlxNodeTypes.Constant, MtlxDataTypes.Vector2);
            inputNode.AddPortValue("value", MtlxDataTypes.Vector2, SlotUtils.GetDefaultValue(slot));
            externals.AddExternalPortAndEdge(slot, inputNode.name, "value");

            var dotNode = graph.AddNode(NodeUtils.GetNodeName(node, "ReconstructZDot"), MtlxNodeTypes.DotProduct, MtlxDataTypes.Float);
            graph.AddPortAndEdge(inputNode.name, dotNode.name, "in1", MtlxDataTypes.Vector2);
            graph.AddPortAndEdge(inputNode.name, dotNode.name, "in2", MtlxDataTypes.Vector2);

            var satNode = graph.AddNode(NodeUtils.GetNodeName(node, "ReconstructZSaturate"), MtlxNodeTypes.Clamp, MtlxDataTypes.Float);
            graph.AddPortAndEdge(dotNode.name, satNode.name, "in", MtlxDataTypes.Float);

            var minusNode = graph.AddNode(NodeUtils.GetNodeName(node, "ReconstructZOnesComplement"), MtlxNodeTypes.Subtract, MtlxDataTypes.Float);
            minusNode.AddPortValue("in1", MtlxDataTypes.Float, new float[] { 1f });
            graph.AddPortAndEdge(satNode.name, minusNode.name, "in2", MtlxDataTypes.Float);

            var sqrNode = graph.AddNode(NodeUtils.GetNodeName(node, "ReconstructZSquareRoot"), MtlxNodeTypes.SquareRoot, MtlxDataTypes.Float);
            graph.AddPortAndEdge(minusNode.name, sqrNode.name, "in", MtlxDataTypes.Float);

            var xNode = graph.AddNode(NodeUtils.GetNodeName(node, "XReconstructZ"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            xNode.AddPortString("channels", MtlxDataTypes.String, "x");
            graph.AddPortAndEdge(inputNode.name, xNode.name, "in", MtlxDataTypes.Vector2);

            var yNode = graph.AddNode(NodeUtils.GetNodeName(node, "YReconstructZ"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            yNode.AddPortString("channels", MtlxDataTypes.String, "y");
            graph.AddPortAndEdge(inputNode.name, yNode.name, "in", MtlxDataTypes.Vector2);

            var cNode = graph.AddNode(NodeUtils.GetNodeName(node, "ReconstructZCombine"), MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3);
            graph.AddPortAndEdge(xNode.name, cNode.name, "in1", MtlxDataTypes.Float);
            graph.AddPortAndEdge(yNode.name, cNode.name, "in2", MtlxDataTypes.Float);
            graph.AddPortAndEdge(sqrNode.name, cNode.name, "in3", MtlxDataTypes.Float);

            var outNode = graph.AddNode(NodeUtils.GetNodeName(node, "ReconstructZ"), MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3);
            graph.AddPortAndEdge(cNode.name, outNode.name, "in", MtlxDataTypes.Vector3);

            externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, outNode.name);
        }
    }
}
