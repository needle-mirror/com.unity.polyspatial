using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX.Tests
{
    using ParserInput = CompoundOpParser.ParserInput;
    using ExternalInput = CompoundOpParser.ExternalInput;
    using SamplerStateInput = CompoundOpParser.SamplerStateInput;
    
    [TestFixture]
    public class CompoundOpParserTests
    {
        [Test]
        public void Test_Empty()
        {
            var nodeDefs = CompoundOpParser.Parse(new(), "");
            Assert.AreEqual(nodeDefs.Count, 0);

            nodeDefs = CompoundOpParser.Parse(new(),
@"
// Comments have no effect.
/* Out = A + B;
*/
");
            Assert.AreEqual(nodeDefs.Count, 0);
        }

        [Test]
        public void Test_Scalar_Arithmetic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["A"] = new ExternalInput(MtlxDataTypes.Float),
                ["B"] = new ExternalInput(MtlxDataTypes.Float),
                ["C"] = new ExternalInput(MtlxDataTypes.Float),
            };
            
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("A"),
                    ["in2"] = new ExternalInputDef("B"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A + B;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("A"),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("B"),
                        ["in2"] = new ExternalInputDef("C"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A + B * C;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                    ["in2"] = new ExternalInputDef("C"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A * B + C;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new ExternalInputDef("B"),
                            ["in2"] = new ExternalInputDef("C"),
                        }),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, -1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = (A * (B + C) + (((-1.0f))));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_D"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                    {
                        ["in2"] = new ExternalInputDef("C")
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 5.0f),
                }),
                ["D"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_D"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                    ["in2"] = new InternalInputDef("0_D"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float D = -C * 5; Out = A + B + D;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Mix, MtlxDataTypes.Float, new()
                {
                    ["bg"] = new ExternalInputDef("A"),
                    ["fg"] = new ExternalInputDef("B"),
                    ["mix"] = new ExternalInputDef("C"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = lerp(A, B, C);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = distance(A, B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = length(A - B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 1.0e-16f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = 1.0e-16;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Power, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, -1.0f),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Power, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("B"),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, -0.5f),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = rcp(A) * rsqrt(B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("A"),
                    ["in2"] = new ExternalInputDef("B"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(A, B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Vector_Arithmetic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["Normal"] = new ExternalInput(MtlxDataTypes.Vector3),
                ["ViewDir"] = new ExternalInput(MtlxDataTypes.Vector3),
                ["Power"] = new ExternalInput(MtlxDataTypes.Float),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Power, MtlxDataTypes.Float, new()
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
                                    ["in"] = new ExternalInputDef("ViewDir"),
                                }),
                            }),
                        }),
                    }),
                    ["in2"] = new ExternalInputDef("Power"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 2.0f, 3.0f, 4.0f),
                    ["channels"] = new StringInputDef("xyy"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = float4(1.0, 2.0, 3.0, 4.0).rgg;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["__Tmp0"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Vector2, 1.0f, 2.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 1.0f, 2.0f),
                }),
                ["__Tmp1"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Vector2, 3.0f, 4.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 3.0f, 4.0f),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("x")
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("y")
                    }),
                    ["in3"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp1"),
                        ["channels"] = new StringInputDef("x")
                    }),
                    ["in4"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp1"),
                        ["channels"] = new StringInputDef("y")
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = float4(float2(1.0, 2.0) + float2(1.0, 2.0), float2(3.0, 4.0) * float2(3.0, 4.0));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Arctangent2, MtlxDataTypes.Vector3, new()
                {
                    ["iny"] = new ExternalInputDef("Normal"),
                    ["inx"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = atan(Normal);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new ExternalInputDef("Normal"),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new ExternalInputDef("ViewDir"),
                            ["in2"] = new ExternalInputDef("Normal"),
                        }),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 0.5f, 0.5f, 0.5f),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = lerp(Normal, ViewDir, float3(0.5, 0.5, 0.5));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.SmoothStep, MtlxDataTypes.Vector2, new()
                {
                    ["low"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("Normal"),
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["high"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("ViewDir"),
                        ["channels"] = new StringInputDef("zw"),
                    }),
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 0.5f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = smoothstep(Normal.xy, ViewDir.zw, float2(0.5, 0.5));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.RealityKitStep, MtlxDataTypes.Vector2, new()
                {
                    ["edge"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("Normal"),
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("ViewDir"),
                        ["channels"] = new StringInputDef("zw"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = step(Normal.rg, ViewDir.ba);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Length, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new ExternalInputDef("Normal"),
                        ["in2"] = new ExternalInputDef("ViewDir"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = distance(Normal, ViewDir);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = length(Normal - ViewDir);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.SplitLR, MtlxDataTypes.Vector3, new()
                {
                    ["valuel"] = new ExternalInputDef("Normal"),
                    ["valuer"] = new ExternalInputDef("ViewDir"),
                    ["center"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                    ["texcoord"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.0f, 0.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = splitlr(Normal, ViewDir, 0.5, float2(0.0, 0.0));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("Normal"),
                    ["in2"] = new ExternalInputDef("ViewDir"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(Normal, ViewDir);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.0f, 1.0f),
                }),
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2 tmp = {0, 1}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_tmp"] = new(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in3"] = new ExternalInputDef("Power"),
                }),
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float3 tmp = {0, 1, Power}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_tmp"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new ExternalInputDef("Power"),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in3"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                    }),
                    ["channels"] = new StringInputDef("x"),
                }),
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["0_tmp3"] = new(MtlxNodeTypes.Convert, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["tmp3"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_tmp3"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_tmp3"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "float tmp = float3(Power, 1, 2); float3 tmp3 = tmp; Out = tmp3;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Matrix_Arithmetic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["M1"] = new ExternalInput(MtlxDataTypes.Matrix22),
                ["M2"] = new ExternalInput(MtlxDataTypes.Matrix22),
                ["A"] = new ExternalInput(MtlxDataTypes.Vector2),
                ["B"] = new ExternalInput(MtlxDataTypes.Vector2),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Matrix22, 1.0f, 0.0f, 0.0f, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = float2x2(1, 0, 0, 1);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Transpose, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = float2x2(A, B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ExternalInputDef("A"),
                    ["mat"] = new ExternalInputDef("M1"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(M1, A);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ExternalInputDef("A"),
                    ["mat"] = new InlineInputDef(MtlxNodeTypes.Transpose, MtlxDataTypes.Matrix22, new()
                    {
                        ["in"] = new ExternalInputDef("M1"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(A, M1);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix22, new()
                {
                    ["in1"] = new ExternalInputDef("M1"),
                    ["in2"] = new ExternalInputDef("M2"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(M1, M2);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Matrix22, 0.0f, 1.0f, 1.0f, 0.0f),
                }),
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2x2 tmp = {0, 1, 1, 0}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Matrix22, 1.0f, 2.0f, 3.0f, 4.0f),
                }),
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2x2 tmp = {{1, 2}, {3, 4}}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["__Tmp0"] = new(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new ExternalInputDef("A"),
                        ["channels"] = new StringInputDef("x"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new ExternalInputDef("A"),
                        ["channels"] = new StringInputDef("y"),
                    }),
                    ["in3"] = new FloatInputDef(MtlxDataTypes.Float, 3.0f),
                    ["in4"] = new FloatInputDef(MtlxDataTypes.Float, 4.0f),
                }),
                ["0_tmp"] = new(MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("zw"),
                    }),
                }),
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2x2 tmp = {A, {3, 4}}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Boolean_Logic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["A"] = new ExternalInput(MtlxDataTypes.Float),
                ["B"] = new ExternalInput(MtlxDataTypes.Float),
                ["C"] = new ExternalInput(MtlxDataTypes.Vector3),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new ExternalInputDef("A"),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = !A;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                                {
                                    ["in1"] = new ExternalInputDef("A"),
                                    ["in2"] = new ExternalInputDef("B"),
                                }),
                                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            }),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = new InlineInputDef(MtlxNodeTypes.Length, MtlxDataTypes.Float, new()
                                {
                                    ["in"] = new ExternalInputDef("C"),
                                }),
                                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            }),
                        }),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A && B || any(C);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new ExternalInputDef("C"),
                                ["channels"] = new StringInputDef("x"),
                            }),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new ExternalInputDef("C"),
                                ["channels"] = new StringInputDef("y"),
                            }),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new ExternalInputDef("C"),
                            ["channels"] = new StringInputDef("z"),
                        }),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = all(C);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Float, new()
                        {
                            ["value1"] = new ExternalInputDef("A"),
                            ["value2"] = new ExternalInputDef("B"),
                            ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Float, new()
                        {
                            ["value1"] = new ExternalInputDef("A"),
                            ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        }),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A > B && A <= 1;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Vector3, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                    {
                        ["value1"] = new ExternalInputDef("A"),
                        ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Vector3, new()
                    {
                        ["value1"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                        {
                            ["value1"] = new ExternalInputDef("A"),
                            ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                            ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        }),
                        ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["in1"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, 1.0f),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 0.0f, 0.0f, 0.0f),
                    }),
                    ["in2"] = new ExternalInputDef("C"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = A == 1 ? C : A != 2 ? float3(0, 0, 0) : float3(1, 1, 1);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Texture_Sampling()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["Texture"] = new ExternalInput(MtlxDataTypes.Filename),
                ["UV"] = new ExternalInput(MtlxDataTypes.Vector2),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.RealityKitTexture2D, MtlxDataTypes.Vector4, new()
                {
                    ["file"] = new ExternalInputDef("Texture"),
                    ["texcoord"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                        {
                            ["in1"] = new ExternalInputDef("UV"),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                            {
                                ["in"] = new TextureTransformInputDef("Texture"),
                                ["channels"] = new StringInputDef("xy"),
                            }),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                        {
                            ["in"] = new TextureTransformInputDef("Texture"),
                            ["channels"] = new StringInputDef("zw"),
                        }),
                    }),
                    ["mag_filter"] = new StringInputDef("linear"),
                    ["min_filter"] = new StringInputDef("linear"),
                    ["mip_filter"] = new StringInputDef("nearest"),
                    ["u_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["v_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["max_anisotropy"] = new FloatInputDef(MtlxDataTypes.Integer, 1),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = SAMPLE_TEXTURE2D(Texture, samplerpolySpatial_Lightmap, UV);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, new()
                {
                    ["file"] = new ExternalInputDef("Texture"),
                    ["texcoord"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                        {
                            ["in1"] = new ExternalInputDef("UV"),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                            {
                                ["in"] = new TextureTransformInputDef("Texture"),
                                ["channels"] = new StringInputDef("xy"),
                            }),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                        {
                            ["in"] = new TextureTransformInputDef("Texture"),
                            ["channels"] = new StringInputDef("zw"),
                        }),
                    }),
                    ["lod"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                    ["mag_filter"] = new StringInputDef("linear"),
                    ["min_filter"] = new StringInputDef("linear"),
                    ["mip_filter"] = new StringInputDef("nearest"),
                    ["u_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["v_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["max_anisotropy"] = new FloatInputDef(MtlxDataTypes.Integer, 1),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = SAMPLE_TEXTURE2D_LOD(Texture, samplerpolySpatial_Lightmap, UV, 0.5);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Variable_Reassignment()
        {
            Dictionary<string, ParserInput> inputs = new();
            
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["0_a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["1_a"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InternalInputDef("0_a"),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_a"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_a"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(inputs, "float a = 1; a = a + 1; Out = a;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                }),
                ["1_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InternalInputDef("0_Out"),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = 2; Out += 1;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["1_a"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InternalInputDef("0_a"),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_a"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_a"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float a = 1; Out = ++a;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["1_a"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InternalInputDef("0_a"),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_a"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_a"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float a = 1; Out = a++;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_a"] = new(MtlxNodeTypes.Constant, MtlxDataTypes.Float, new()),
                ["1_a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["a"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_a"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InternalInputDef("1_a"),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float a; a = 1; Out = a + 1;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, 1.0f),
                }),
                ["1_Out"] = new(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("0_Out"),
                        ["channels"] = new StringInputDef("x"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("0_Out"),
                        ["channels"] = new StringInputDef("y"),
                    }),
                    ["in3"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("1_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = {1, 1, 1}; Out.z = 0;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 1.0f, 1.0f, 1.0f),
                }),
                ["__Tmp2"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new InternalInputDef("0_Out"),
                        ["channels"] = new StringInputDef("yx"),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 2.0f, 3.0f),
                }),
                ["__Tmp4"] = new(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new InternalInputDef("1_Out"),
                        ["channels"] = new StringInputDef("wz"),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 4.0f, 5.0f),
                }),
                ["1_Out"] = new(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp2"),
                        ["channels"] = new StringInputDef("y"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp2"),
                        ["channels"] = new StringInputDef("x"),
                    }),
                    ["in3"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("0_Out"),
                        ["channels"] = new StringInputDef("z"),
                    }),
                    ["in4"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("0_Out"),
                        ["channels"] = new StringInputDef("w"),
                    }),
                }),
                ["2_Out"] = new(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("1_Out"),
                        ["channels"] = new StringInputDef("x"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("1_Out"),
                        ["channels"] = new StringInputDef("y"),
                    }),
                    ["in3"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp4"),
                        ["channels"] = new StringInputDef("y"),
                    }),
                    ["in4"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp4"),
                        ["channels"] = new StringInputDef("x"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new InternalInputDef("2_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = {1, 1, 1, 1}; Out.gr += float2(2, 3); Out.ab -= float2(4, 5);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Functions()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["A"] = new ExternalInput(MtlxDataTypes.Float),
            };

            Dictionary<string, NodeDef> expectedNodeDefs = new();
            var actualNodeDefs = CompoundOpParser.Parse(inputs, @"
void MyFunction_float()
{
}", "MyFunction_float");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, @"
void MyFunction_float(out float Out)
{
    Out = 1;
}", "MyFunction_float");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, @"
float MySecondaryFunction()
{
    return 1 + 1;
}
void MyFunction_float(out float Out)
{
    Out = MySecondaryFunction();
}", "MyFunction_float");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["0_tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                }),
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_tmp"),
                }),

                ["0_2__foo"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new ExternalInputDef("A"),
                }),
                ["2__foo"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_2__foo"),
                }),
                ["0_2__bar"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_tmp"),
                }),
                ["2__bar"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_2__bar"),
                }),
                ["0_2__baz"] = new(MtlxNodeTypes.Constant, MtlxDataTypes.Float, new()),
                ["2__baz"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_2__baz"),
                }),
                ["0_2__tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_2__foo"),
                }),
                ["2__tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_2__tmp"),
                }),
                ["1_2__bar"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InternalInputDef("0_2__bar"),
                    ["in2"] = new InternalInputDef("0_2__tmp"),
                }),
                ["1_2__baz"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_2__bar"),
                }),
                
                ["1_tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_2__bar"),
                }),
                ["0_Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("1_2__baz"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InternalInputDef("0_Out"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, @"
void MySecondaryFunction(float foo, inout float bar, out float baz)
{
    float tmp = foo;
    bar += tmp;
    baz = bar;
}
void MyFunction_float(float A, out float Out)
{
    float tmp = 0;
    MySecondaryFunction(A, tmp, Out);
}", "MyFunction_float");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }
    }
}