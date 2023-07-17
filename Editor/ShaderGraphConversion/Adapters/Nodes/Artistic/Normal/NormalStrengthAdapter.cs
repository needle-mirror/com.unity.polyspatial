namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalStrengthAdapter : ANodeAdapter<NormalStrengthNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Normal-Strength-Node.html
            QuickNode.CompoundOp(node, graph, externals, "NormalStrength", new()
            {
                ["ScaledIn"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new ExternalInputDef("In"),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                    {
                        ["in"] = new ExternalInputDef("Strength"),
                        ["channels"] = new StringInputDef("xxx"),
                    }),
                }),
                // float3(In.xy * Strength, lerp(1, In.z, saturate(Strength)))
                ["Out"] = new(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("ScaledIn"),
                        ["channels"] = new StringInputDef("x"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("ScaledIn"),
                        ["channels"] = new StringInputDef("y"),
                    }),
                    ["in3"] = new InlineInputDef(MtlxNodeTypes.Mix, MtlxDataTypes.Float, new()
                    {
                        ["bg"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["fg"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new ExternalInputDef("In"),
                            ["channels"] = new StringInputDef("z"),
                        }),
                        ["mix"] = new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new ExternalInputDef("Strength"),
                        }),
                    }),
                }),
            });
        }
    }
}