namespace UnityEditor.ShaderGraph.MaterialX
{
    class FresnelEffectAdapter : ANodeAdapter<FresnelNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Fresnel-Effect-Node.html
            QuickNode.CompoundOp(node, graph, externals, "FresnelEffect", new()
            {
                // pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power)
                ["Out"] = new(MtlxNodeTypes.Power, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new InlineInputDef(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3, new()
                                {
                                    ["in"] = new ExternalInputDef("Normal"),
                                }),
                                ["in2"] = new InlineInputDef(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3, new()
                                {
                                    ["in"] = new ExternalInputDef("View Dir"),
                                }),
                            }),
                        }),
                    }),
                    ["in2"] = new ExternalInputDef("Power"),
                }),
            });
        }
    }
}