namespace UnityEditor.ShaderGraph.MaterialX
{
    class EllipseAdapter : AbstractUVNodeAdapter<EllipseNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Ellipse-Node.html
            QuickNode.CompoundOp(node, graph, externals, "Ellipse", new()
            {
                ["Out"] = new(MtlxNodeTypes.SplitLR, MtlxDataTypes.Float, new()
                {
                    ["valuel"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["center"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["texcoord"] = new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                    {
                        // float d = length((UV * 2 - 1) / float2(Width, Height));
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Length, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
                            {
                                ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector2, new()
                                {
                                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                                    {
                                        ["in1"] = new ExternalInputDef("UV"),
                                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                                    }),
                                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                                }),
                                ["in2"] = new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                                {
                                    ["in1"] = new ExternalInputDef("Width"),
                                    ["in2"] = new ExternalInputDef("Height"),
                                }),
                            }),
                        }),
                    }),
                }),
            });
        }
    }
}