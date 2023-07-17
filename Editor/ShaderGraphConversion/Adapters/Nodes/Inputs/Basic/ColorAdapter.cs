
namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class ColorAdapter : ANodeAdapter<ColorNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is ColorNode cnode)
            {
                var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Constant, node, graph, externals, "Color", outputType: MtlxDataTypes.Color4);

                var c = cnode.color.color;
                var value = new float[] { c.r, c.g, c.b, c.a };

                nodeData.AddPortValue("value", MtlxDataTypes.Color4, value);
            }
        }
    }
}
