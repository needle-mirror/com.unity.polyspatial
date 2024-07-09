using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MultiplyAdapter : ANodeAdapter<MultiplyNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var inputSlots = new List<MaterialSlot>();
            node.GetInputSlots<MaterialSlot>(inputSlots);

            var aType = SlotUtils.GetDataTypeName(inputSlots[0]);
            var bType = SlotUtils.GetDataTypeName(inputSlots[1]);

            if (aType == bType)
                QuickNode.BinaryOp(MtlxNodeTypes.Multiply, node, graph, externals, "Multiply");
            else
                QuickNode.CompoundOp(node, graph, externals, sgContext, "Multiply", "Out = mul(A, B);");
        }
    }
}
