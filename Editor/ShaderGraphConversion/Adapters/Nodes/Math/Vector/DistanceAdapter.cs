namespace UnityEditor.ShaderGraph.MaterialX
{
    class DistanceAdapter : ANodeAdapter<DistanceNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Distance-Node.html
            // (but it uses the distance function, whereas we use length(a - b))
            var inputType = SlotUtils.GetDataTypeName(NodeUtils.GetPrimaryInput(node));
            var lengthNodeType = (inputType == MtlxDataTypes.Float) ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            QuickNode.CompoundOp(node, graph, externals, "Distance", new()
            {
                // Out = length(A - B);
                ["Out"] = new(lengthNodeType, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, inputType, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"), 
                    }),
                }),
            });
        }
    }
}