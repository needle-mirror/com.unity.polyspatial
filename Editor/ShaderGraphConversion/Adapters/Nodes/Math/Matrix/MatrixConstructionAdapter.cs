using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MatrixConstructionAdapter : ANodeAdapter<MatrixConstructionNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            return false;
#else
            return true;
#endif
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var output44 = NodeUtils.GetOutputByName(node, "4x4");
            if (output44.isConnected)
            {
                var nodeData = graph.AddNode(NodeUtils.GetNodeName(node, "Construction4x4"),
                    MtlxNodeTypes.RealityKitCombine4, MtlxDataTypes.Matrix44);
                AddPortsAndEdges(node, graph, externals, output44, nodeData, 4, MtlxDataTypes.Vector4);
            }

            var output33 = NodeUtils.GetOutputByName(node, "3x3");
            if (output33.isConnected)
            {
                var nodeData = graph.AddNode(NodeUtils.GetNodeName(node, "Construction3x3"),
                    MtlxNodeTypes.RealityKitCombine3, MtlxDataTypes.Matrix33);
                AddPortsAndEdges(node, graph, externals, output33, nodeData, 3, MtlxDataTypes.Vector3);
            }

            var output22 = NodeUtils.GetOutputByName(node, "2x2");
            if (output22.isConnected)
            {
                var nodeData = graph.AddNode(NodeUtils.GetNodeName(node, "Construction2x2"),
                    MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22);
                AddPortsAndEdges(node, graph, externals, output22, nodeData, 2, MtlxDataTypes.Vector2);
            }
        }

        void AddPortsAndEdges(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals,
            MaterialSlot outputSlot, MtlxNodeData nodeData, int inputCount, string portType)
        {
            var transposeNode = graph.AddNode(
                NodeUtils.GetNodeName(node, "Transpose"), MtlxNodeTypes.Transpose, nodeData.datatype);
            graph.AddPortAndEdge(nodeData.name, transposeNode.name, "in", nodeData.datatype);

            externals.AddExternalPort(outputSlot.slotReference, transposeNode.name);

            for (var i = 0; i < inputCount; ++i)
            {
                var portName = $"in{i + 1}";
                var inputSlot = NodeUtils.GetInputByName(node, $"M{i}");
                nodeData.AddPortValue(portName, portType, SlotUtils.GetDefaultValue(inputSlot));
                externals.AddExternalPortAndEdge(inputSlot, nodeData.name, portName);
            }
        }
    }
}
