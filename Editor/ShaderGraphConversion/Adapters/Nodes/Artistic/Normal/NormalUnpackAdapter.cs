namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalUnpackAdapter : ANodeAdapter<NormalUnpackNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is not NormalUnpackNode normalUnpackNode)
                return;

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Normal-Unpack-Node.html
            // (although it just uses predefined functions: UnpackNormalmapRGorAG or UnpackNormalmapRGB)
            switch (normalUnpackNode.normalMapSpace)
            {
                case NormalMapSpace.Tangent:
                    QuickNode.CompoundOp(node, graph, externals, "NormalUnpack", new()
                    {
                        // packedNormal.ag * 2.0 - 1.0;
                        ["XY"] = new(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector2, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                            {
                                ["in1"] = new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                                {
                                    // packedNormal.a *= packedNormal.r;
                                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                                    {
                                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                        {
                                            ["in"] = new ExternalInputDef("In"),
                                            ["channels"] = new StringInputDef("w"),
                                        }),
                                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                        {
                                            ["in"] = new ExternalInputDef("In"),
                                            ["channels"] = new StringInputDef("x"),
                                        }),
                                    }),
                                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                    {
                                        ["in"] = new ExternalInputDef("In"),
                                        ["channels"] = new StringInputDef("y"),
                                    }),
                                }),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                            }),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        }),
                        // UnpackNormalmapRGorAG(In);
                        ["Out"] = new(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new InternalInputDef("XY"),
                                ["channels"] = new StringInputDef("x"),
                            }),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new InternalInputDef("XY"),
                                ["channels"] = new StringInputDef("y"),
                            }),
                            // max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
                            ["in3"] = new InlineInputDef(MtlxNodeTypes.Maximum, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0e-16f),
                                ["in2"] = new InlineInputDef(MtlxNodeTypes.SquareRoot, MtlxDataTypes.Float, new()
                                {
                                    ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                                    {
                                        ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
                                        {
                                            ["in"] = new InlineInputDef(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, new()
                                            {
                                                ["in1"] = new InternalInputDef("XY"),
                                                ["in2"] = new InternalInputDef("XY"),
                                            }),
                                        }),
                                    }),
                                }),
                            }),
                        }),
                    });
                    break;
                
                case NormalMapSpace.Object:
                    QuickNode.CompoundOp(node, graph, externals, "NormalUnpack", new()
                    {
                        // UnpackNormalmapRGB(In);
                        // packedNormal.rgb * 2.0 - 1.0;
                        ["Out"] = new(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                            {
                                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                                {
                                    ["in"] = new ExternalInputDef("In"),
                                    ["channels"] = new StringInputDef("xyz"),
                                }),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                            }),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        }),
                    });
                    break;
            }
        }
    }
}