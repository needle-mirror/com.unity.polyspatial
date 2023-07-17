namespace UnityEditor.ShaderGraph.MaterialX
{
    class InverseLerpAdapter : ANodeAdapter<InverseLerpNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Inverse-Lerp-Node.html
            var outputType = NodeUtils.GetDataTypeName(node);
            QuickNode.CompoundOp(node, graph, externals, "InverseLerp", new()
            {
                // (T - A)/(B - A);
                ["Out"] = new(MtlxNodeTypes.Divide, outputType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, outputType, new()
                    {
                        ["in1"] = new ExternalInputDef("T"),
                        ["in2"] = new ExternalInputDef("A"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Subtract, outputType, new()
                    {
                        ["in1"] = new ExternalInputDef("B"),
                        ["in2"] = new ExternalInputDef("A"),
                    }),
                }),
            });
        }
    }
}