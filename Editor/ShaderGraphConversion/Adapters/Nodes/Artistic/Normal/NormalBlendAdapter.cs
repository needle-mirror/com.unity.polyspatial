using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalBlendAdapter : ANodeAdapter<NormalBlendNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (!(node is NormalBlendNode normalBlendNode))
                return;
            
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@14.0/manual/Normal-Blend-Node.html
            switch (normalBlendNode.blendMode)
            {
                case NormalBlendMode.Default:
                    QuickNode.CompoundOp(node, graph, externals, "NormalBlend", new()
                    {
                        ["Sum"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new ExternalInputDef("A"),
                            ["in2"] = new ExternalInputDef("B"),
                        }),
                        // normalize(float3(A.xy + B.xy, A.z * B.z))
                        ["Out"] = new(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                            {
                                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                {
                                    ["in"] = new InternalInputDef("Sum"),
                                    ["channels"] = new StringInputDef("x"),
                                }),
                                ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                {
                                    ["in"] = new InternalInputDef("Sum"),
                                    ["channels"] = new StringInputDef("y"),
                                }),
                                ["in3"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                                {
                                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                    {
                                        ["in"] = new ExternalInputDef("A"),
                                        ["channels"] = new StringInputDef("z"),
                                    }),
                                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                    {
                                        ["in"] = new ExternalInputDef("B"),
                                        ["channels"] = new StringInputDef("z"),
                                    }),
                                }),
                            }),
                        }),
                    });
                    break;
                
                case NormalBlendMode.Reoriented:
                    QuickNode.CompoundOp(node, graph, externals, "NormalBlend", new()
                    {
                        // A.xyz + float3(0.0, 0.0, 1.0)
                        ["T"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new ExternalInputDef("A"),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 0.0f, 0.0f, 1.0f),
                        }),
                        // B.xyz * float3(-1.0, -1.0, 1.0)
                        ["U"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new ExternalInputDef("B"),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, -1.0f, -1.0f, 1.0f),
                        }),
                        // (t / t.z) * dot(t, u) - u
                        ["Out"] = new(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                            {
                                ["in1"] = new InternalInputDef("T"),
                                ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                                {
                                    ["in"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                                    {
                                        ["in1"] = new InlineInputDef(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, new()
                                        {
                                            ["in1"] = new InternalInputDef("T"),
                                            ["in2"] = new InternalInputDef("U"),
                                        }),
                                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                        {
                                            ["in"] = new InternalInputDef("T"),
                                            ["channels"] = new StringInputDef("z"),
                                        }),
                                    }),
                                }),
                            }),
                            ["in2"] = new InternalInputDef("U"),
                        }),
                    });
                    break;
                
                default:
                    throw new NotSupportedException($"Unrecognized blend mode: {normalBlendNode.blendMode}");
            }
        }
    }
}