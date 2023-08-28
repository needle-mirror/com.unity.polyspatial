using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    using Operator = CompoundOpParser.Operator;
    using ParseException = CompoundOpParser.ParseException;
    using ParserInput = CompoundOpParser.ParserInput;
    using SamplerStateInput = CompoundOpParser.SamplerStateInput;
    using Symbol = CompoundOpParser.Symbol;
    using SyntaxNode = CompoundOpParser.SyntaxNode;

    // Contains the implementations of the functions and operators that
    // expressions parsed by CompoundOpParser may reference.
    internal static class CompoundOpFunctions
    {
        // The "compiler" delegate type.  Compilation in this usage refers to processing a node in the
        // abstract syntax tree in order to convert it to a node definition.
        delegate InputDef Compiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output);
        
        static Dictionary<(string, Operator.VariantType), Compiler> s_OperatorCompilers = new()
        {
            [(";", Operator.VariantType.Default)] = SemicolonCompiler,
            [("=", Operator.VariantType.Default)] = AssignmentCompiler,
            [("+", Operator.VariantType.Prefix)] = IdentityCompiler,
            [("-", Operator.VariantType.Prefix)] = CreateUnaryOpCompiler(MtlxNodeTypes.Subtract, "in2"),
            [("+", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Add, true),
            [("-", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Subtract, true),
            [("*", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Multiply, true),
            [("/", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Divide, true),
            [("%", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Modulo, true),
            [(".", Operator.VariantType.Default)] = SwizzleCompiler,
            [("abs", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Absolute),
            [("acos", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Arccosine),
            [("asin", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Arcsine),
            [("atan", Operator.VariantType.FunctionCall)] = AtanCompiler,
            [("atan2", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.Arctangent2, "iny", "inx"),
            [("ceil", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Ceil),
            [("clamp", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.Clamp, "in", "low", "high"),
            [("cos", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Cosine),
            [("cross", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.CrossProduct),
            [("degrees", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Multiply, Mathf.Rad2Deg),
            [("distance", Operator.VariantType.FunctionCall)] = DistanceCompiler,
            [("dot", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(
                MtlxNodeTypes.DotProduct, false, MtlxDataTypes.Float),
            [("exp", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Exponential),
            [("float2", Operator.VariantType.FunctionCall)] = CreateVectorConstructorCompiler(MtlxDataTypes.Vector2),
            [("float2x2", Operator.VariantType.FunctionCall)] = CreateMatrixConstructorCompiler(
                MtlxDataTypes.Matrix22),
            [("float3", Operator.VariantType.FunctionCall)] = CreateVectorConstructorCompiler(MtlxDataTypes.Vector3),
            [("float3x3", Operator.VariantType.FunctionCall)] = CreateMatrixConstructorCompiler(
                MtlxDataTypes.Matrix33),
            [("float4", Operator.VariantType.FunctionCall)] = CreateVectorConstructorCompiler(MtlxDataTypes.Vector4),
            [("float4x4", Operator.VariantType.FunctionCall)] = CreateMatrixConstructorCompiler(
                MtlxDataTypes.Matrix44),
            [("floor", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Floor),
            [("fmod", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Modulo, true),
            [("frac", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.RealityKitFractional),
            [("length", Operator.VariantType.FunctionCall)] = LengthCompiler,
            [("lerp", Operator.VariantType.FunctionCall)] = LerpCompiler,
            [("log", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.NaturalLog),
            [("max", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Maximum, true),
            [("min", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Minimum, true),
            [("mul", Operator.VariantType.FunctionCall)] = MulCompiler,
            [("normalize", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Normalize),
            [("pow", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Power, true),
            [("radians", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Multiply, Mathf.Deg2Rad),
            [("rcp", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(MtlxNodeTypes.Power, -1.0f),
            [("reflect", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.RealityKitReflect, "in", "normal"),
            [("round", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Round),
            [("rsqrt", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Power, -0.5f),
            [("saturate", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Clamp),
            [("sign", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Sign),
            [("sin", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Sine),
            [("smoothstep", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.SmoothStep, "low", "high", "in"),
            [("splitlr", Operator.VariantType.FunctionCall)] = SplitLRCompiler,
            [("sqrt", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.SquareRoot),
            [("step", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.RealityKitStep, "edge", "in"),
            [("tan", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Tangent),
            [("transpose", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Transpose),
            [("SAMPLE_TEXTURE2D", Operator.VariantType.FunctionCall)] = SampleTexture2DCompiler,
            [("SAMPLE_TEXTURE2D_LOD", Operator.VariantType.FunctionCall)] = SampleTexture2DLodCompiler,
        };

        static Dictionary<string, Compiler> s_SymbolCompilers = new()
        {
            [PolySpatialShaderGlobals.Time] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.SinTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.CosTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.DeltaTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.GlossyEnvironmentColor] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.VolumeToWorld] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            [PolySpatialShaderProperties.Lightmap] = CreateImplicitCompiler(MtlxDataTypes.Filename),
            [$"sampler{PolySpatialShaderProperties.Lightmap}"] = CreateSamplerCompiler(
                new() { wrap = TextureSamplerState.WrapMode.Clamp }),
            [PolySpatialShaderProperties.LightmapInd] = CreateImplicitCompiler(MtlxDataTypes.Filename),
            [$"sampler{PolySpatialShaderProperties.LightmapInd}"] = CreateSamplerCompiler(
                new() { wrap = TextureSamplerState.WrapMode.Clamp }),
            [PolySpatialShaderProperties.LightmapST] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHAr] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHAg] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHAb] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHBr] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHBg] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHBb] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHC] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
        };

        static CompoundOpFunctions()
        {
            for (var i = 0; i < PolySpatialShaderGlobals.LightCount; ++i)
            {
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.LightColorPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.LightPositionPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.SpotDirectionPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.LightAttenPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
            }
        }

        internal static InputDef CompileOperator(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            var op = node.Lexeme as Operator;
            if (s_OperatorCompilers.TryGetValue((op.Span.contents, op.Variant), out var compiler))
                return compiler(node, inputs, output);
            
            throw new ParseException($"Unknown operator {op.Span.contents}", op.Span);
        }

        internal static InputDef CompileSymbol(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            var symbol = node.Lexeme as Symbol;
            if (inputs.ContainsKey(symbol.Span.contents))
                return new ExternalInputDef(symbol.Span.contents);
            
            if (output.ContainsKey(symbol.Span.contents))
                return new InternalInputDef(symbol.Span.contents);

            if (s_SymbolCompilers.TryGetValue(symbol.Span.contents, out var compiler))
                return compiler(node, inputs, output);
            
            throw new ParseException($"Unknown symbol {symbol.Span.contents}", symbol.Span);
        }

        static InputDef SemicolonCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.Children.ForEach(child => child.Compile(inputs, output));
            return null;
        }

        static InputDef AssignmentCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);
            InputDef inputDef = node.Children[1].Compile(inputs, output);

            string symbol;
            var leftChild = node.Children[0];
            if (leftChild.Lexeme is Symbol)
            {
                symbol = leftChild.Lexeme.Span.contents;
            }
            else if (leftChild.Lexeme is Operator op &&
                op.Variant == Operator.VariantType.VariableDefinition &&
                leftChild.Children.Count == 1 &&
                leftChild.Children[0].Lexeme is Symbol leftGrandchildSymbol)
            {
                symbol = leftGrandchildSymbol.Span.contents;

                var expectedType = op.Span.contents switch
                {
                    "float" => MtlxDataTypes.Float,
                    "float2" => MtlxDataTypes.Vector2,
                    "float3" => MtlxDataTypes.Vector3,
                    "float4" => MtlxDataTypes.Vector4,
                    "float2x2" => MtlxDataTypes.Matrix22,
                    "float3x3" => MtlxDataTypes.Matrix33,
                    "float4x4" => MtlxDataTypes.Matrix44,
                    _ => throw new ParseException("Unknown type", op.Span),
                };
                if (!TryCoerce(ref inputDef, inputs, output, expectedType))
                    throw new ParseException($"Expected {op.Span.contents} rvalue", node.Lexeme.Span);
            }
            else
            {
                throw new ParseException("Invalid lvalue for assignment", node.Lexeme.Span);
            }

            switch (inputDef)
            {
                case InlineInputDef inlineInputDef:
                    output[symbol] = inlineInputDef.Source;
                    break;
                
                default:
                    output[symbol] = new(MtlxNodeTypes.Dot, GetOutputType(inputDef, inputs, output), new()
                    {
                        ["in"] = inputDef,
                    });
                    break;
            }
            
            return new InternalInputDef(symbol);
        }

        static InputDef IdentityCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);
            return node.Children[0].Compile(inputs, output);
        }

        static InputDef AtanCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            // Note: If the atan2 node definitions had reasonable defaults (i.e., defaulting inx to a vector
            // of 1.0), we could just rely on that and use CreateUnaryOpCompiler.  However, the current node
            // definitions have 1.0 as the default inx for the float variant but zero vectors as the defaults for
            // the vector variants (and one vectors for the iny defaults, which makes me think it's probably
            // an oversight).
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["iny"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "iny");

            var ones = new float[MtlxDataTypes.GetLength(matchedType)];
            Array.Fill(ones, 1.0f);
            inputDefs.Add("inx", new FloatInputDef(matchedType, ones));

            return new InlineInputDef(MtlxNodeTypes.Arctangent2, matchedType, inputDefs);
        }

        static InputDef SwizzleCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);
            var leftInputDef = node.Children[0].Compile(inputs, output);
            var leftInputType = GetOutputType(leftInputDef, inputs, output);

            var leftLength = MtlxDataTypes.GetLength(leftInputType);
            if (leftLength > 4)
                throw new ParseException("Left side of . cannot be swizzled", node.Lexeme.Span);
            if (node.Children[1].Lexeme is not Symbol swizzle)
                throw new ParseException("Right side of . is not a swizzle", node.Lexeme.Span);

            var containsXYZW = false;
            var containsRGBA = false;
            var containsOther = false;
            foreach (var ch in swizzle.Span.contents)
            {
                if ("xyzw".Contains(ch))
                    containsXYZW = true;
                else if ("rgba".Contains(ch))
                    containsRGBA = true;
                else
                    containsOther = true;
            }
            if (!(containsXYZW ^ containsRGBA) || containsOther || swizzle.Span.contents.Length > 4)
                throw new ParseException("Invalid swizzle", swizzle.Span);

            var outputType = MtlxDataTypes.GetTypeOfLength(swizzle.Span.contents.Length);

            var channels = swizzle.Span.contents;
            if (containsRGBA)
            {
                StringBuilder builder = new();
                foreach (var ch in channels)
                {
                    builder.Append(ch switch
                    {
                        'r' => 'x',
                        'g' => 'y',
                        'b' => 'z',
                        _ => 'w',
                    });
                }
                channels = builder.ToString();
            }

            return new InlineInputDef(MtlxNodeTypes.Swizzle, outputType, new()
            {
                ["in"] = leftInputDef,
                ["channels"] = new StringInputDef(channels),
            });
        }

        static InputDef DistanceCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(inputs, output),
                ["in2"] = node.Children[1].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");

            var lengthNodeType = (matchedType == MtlxDataTypes.Float) ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            return new InlineInputDef(lengthNodeType, MtlxDataTypes.Float, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, inputDefs),
            });
        }

        static InputDef LengthCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);
            
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");

            var nodeType = (matchedType == MtlxDataTypes.Float) ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            return new InlineInputDef(nodeType, MtlxDataTypes.Float, inputDefs);
        }

        static InputDef LerpCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(3);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["bg"] = node.Children[0].Compile(inputs, output),
                ["fg"] = node.Children[1].Compile(inputs, output),
                ["mix"] = node.Children[2].Compile(inputs, output),
            };
            
            // MaterialX's mix type requires a scalar parameter.  So, we use that for scalar parameters only
            // and use X + (Y - X)*S for vector parameters.
            if (GetOutputType(inputDefs["mix"], inputs, output) == MtlxDataTypes.Float)
            {
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "bg", "fg");
                return new InlineInputDef(MtlxNodeTypes.Mix, matchedType, inputDefs);
            }
            else
            {
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "bg", "fg", "mix");
                var sharedXInput = GetSharedInput(inputDefs["bg"], output);
                return new InlineInputDef(MtlxNodeTypes.Add, matchedType, new()
                {
                    ["in1"] = sharedXInput,
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, new()
                        {
                            ["in1"] = inputDefs["fg"],
                            ["in2"] = sharedXInput,
                        }),
                        ["in2"] = inputDefs["mix"],
                    }),
                });
            }
        }

        static InputDef MulCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(inputs, output),
                ["in2"] = node.Children[1].Compile(inputs, output),
            };
            var leftType = GetOutputType(inputDefs["in1"], inputs, output);
            var rightType = GetOutputType(inputDefs["in2"], inputs, output);

            // Two vectors -> dot product.
            var leftIsVector = MtlxDataTypes.IsVector(leftType);
            var rightIsVector = MtlxDataTypes.IsVector(rightType);
            if (leftIsVector && rightIsVector)
            {
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");
                return new InlineInputDef(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, inputDefs);
            }

            // One vector, one matrix -> transform vector.
            var leftIsMatrix = MtlxDataTypes.IsMatrix(leftType);
            var rightIsMatrix = MtlxDataTypes.IsMatrix(rightType);
            if (leftIsMatrix && rightIsVector)
            {
                var elementLength = MtlxDataTypes.GetElementLength(leftType);
                var vectorType = MtlxDataTypes.GetTypeOfLength(elementLength);

                inputDefs["in"] = inputDefs["in2"];
                inputDefs["mat"] = inputDefs["in1"];
                inputDefs.Remove("in1");
                inputDefs.Remove("in2");

                CoerceToType(node, inputs, output, inputDefs, "in", vectorType);
                return new InlineInputDef(MtlxNodeTypes.TransformMatrix, vectorType, inputDefs);
            }
            else if (leftIsVector && rightIsMatrix)
            {
                var elementLength = MtlxDataTypes.GetElementLength(rightType);
                var vectorType = MtlxDataTypes.GetTypeOfLength(elementLength);

                inputDefs["in"] = inputDefs["in1"];
                inputDefs["mat"] = new InlineInputDef(MtlxNodeTypes.Transpose, rightType, new()
                {
                    ["in"] = inputDefs["in2"],
                });
                inputDefs.Remove("in1");
                inputDefs.Remove("in2");

                CoerceToType(node, inputs, output, inputDefs, "in", vectorType);
                return new InlineInputDef(MtlxNodeTypes.TransformMatrix, vectorType, inputDefs);
            }
            else
            {
                // Anything else -> multiply
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");
                return new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, inputDefs);
            }
        }

        static InputDef SplitLRCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(4);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["valuel"] = node.Children[0].Compile(inputs, output),
                ["valuer"] = node.Children[1].Compile(inputs, output),
                ["center"] = node.Children[2].Compile(inputs, output),
                ["texcoord"] = node.Children[3].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "valuel", "valuer");
            CoerceToType(node, inputs, output, inputDefs, "center", MtlxDataTypes.Float);
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector2);

            return new InlineInputDef(MtlxNodeTypes.SplitLR, matchedType, inputDefs);
        }

        static InputDef SampleTexture2DCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(3);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = node.Children[0].Compile(inputs, output),
                ["texcoord"] = node.Children[2].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector2);

            FlipTexCoordInput(inputDefs);

            var samplerInputDef = node.Children[1].Compile(inputs, output);
            var samplerState = RequireTextureSampler(node, inputs, samplerInputDef);
            var addressMode = new StringInputDef(SampleTexture2DAdapter.GetAddressMode(samplerState));

            inputDefs.Add("filtertype", new StringInputDef(SampleTexture2DAdapter.GetFilterType(samplerState)));
            inputDefs.Add("uaddressmode", addressMode);
            inputDefs.Add("vaddressmode", addressMode);

            return new InlineInputDef(MtlxNodeTypes.Image, MtlxDataTypes.Vector4, inputDefs);
        }

        static InputDef SampleTexture2DLodCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(4);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = node.Children[0].Compile(inputs, output),
                ["texcoord"] = node.Children[2].Compile(inputs, output),
                ["level"] = node.Children[3].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector2);
            CoerceToType(node, inputs, output, inputDefs, "level", MtlxDataTypes.Float);

            FlipTexCoordInput(inputDefs);

            var samplerInputDef = node.Children[1].Compile(inputs, output);
            var samplerState = RequireTextureSampler(node, inputs, samplerInputDef);
            var filterType = new StringInputDef(SampleTexture2DLODAdapter.GetFilterType(samplerState));
            var addressMode = new StringInputDef(SampleTexture2DLODAdapter.GetAddressMode(samplerState));

            inputDefs.Add("mag_filter", filterType);
            inputDefs.Add("min_filter", filterType);
            inputDefs.Add("mip_filter", filterType);
            inputDefs.Add("s_address", addressMode);
            inputDefs.Add("t_address", addressMode);

            return new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Vector4, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.RealityKitImageLod, MtlxDataTypes.Color4, inputDefs),
            });
        }

        static Compiler CreateUnaryOpCompiler(string nodeType, string inputPort = "in", string outputType = null)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(1);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    [inputPort] = node.Children[0].Compile(inputs, output),
                };
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, inputPort);

                return new InlineInputDef(nodeType, outputType ?? matchedType, inputDefs);
            };
        }

        static Compiler CreateBinaryOpCompiler(string nodeType, bool allowFloatRight = false, string outputType = null)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(2);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(inputs, output),
                    ["in2"] = node.Children[1].Compile(inputs, output),
                };

                // We allow the right hand side to be a float for the FA node variants, like vector * scalar.
                string matchedType;
                if (allowFloatRight && GetOutputType(inputDefs["in2"], inputs, output) == MtlxDataTypes.Float)
                    matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1");
                else
                    matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");
                    
                return new InlineInputDef(nodeType, outputType ?? matchedType, inputDefs);
            };
        }

        static Compiler CreateBinaryConstantOpCompiler(string nodeType, float constant)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(1);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(inputs, output),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, constant),
                };
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1");
                
                return new InlineInputDef(nodeType, matchedType, inputDefs);
            };
        }

        static Compiler CreateNaryOpCompiler(string nodeType, params string[] inputPorts)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(inputPorts.Length);

                Dictionary<string, InputDef> inputDefs = new();
                for (var i = 0; i < inputPorts.Length; ++i)
                {
                    inputDefs.Add(inputPorts[i], node.Children[i].Compile(inputs, output));
                }
                var outputType = CoerceToMatchedType(node, inputs, output, inputDefs, inputPorts);

                return new InlineInputDef(nodeType, outputType, inputDefs);
            };
        }

        static TextureSamplerState RequireTextureSampler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, InputDef inputDef)
        {
            if (inputDef is ExternalInputDef externalInputDef)
            {
                if (inputs.TryGetValue(externalInputDef.Source, out var input) &&
                    input is SamplerStateInput samplerStateInput)
                {
                    return samplerStateInput.SamplerState ?? new();
                }
            }
            throw new ParseException($"Expected texture sampler argument", node.Lexeme.Span);
        }

        /// <summary>
        /// Attempts to coerce the inputs identified by inputNames within the inputDefs map (which represents the inputs
        /// to a node in a compound op) so that they are the same type, which may involve promotion (float to vector)
        /// or conversion (color to vector).
        /// </summary>
        /// <param name="node">The abstract syntax node being for which the coercion is being undertaken.</param>
        /// <param name="inputs">The map of all inputs to the parsed expression.</param>
        /// <param name="output">The map that will contain the generated node definitions for the parsed
        /// expression.</param>
        /// <param name="inputDefs">The mapping of input names to their definitions, which may be modified to include
        /// type conversions.</param>
        /// <param name="inputNames">The names of the inputs to be coerced within the inputDefs map.</param>
        /// <returns>The common type to which all named inputs were coerced.</returns>
        static string CoerceToMatchedType(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output,
            Dictionary<string, InputDef> inputDefs, params string[] inputNames)
        {
            var maxLength = 1;
            var maxElementLength = 1;
            foreach (var inputName in inputNames)
            {
                var inputDef = inputDefs[inputName];
                var inputType = GetOutputType(inputDef, inputs, output);
                maxLength = Math.Max(maxLength, MtlxDataTypes.GetLength(inputType));
                maxElementLength = Math.Max(maxElementLength, MtlxDataTypes.GetElementLength(inputType));
            }

            var matchedType = maxElementLength switch
            {
                2 => MtlxDataTypes.Matrix22,
                3 => MtlxDataTypes.Matrix33,
                4 => MtlxDataTypes.Matrix44,
                _ => MtlxDataTypes.GetTypeOfLength(maxLength),
            };

            foreach (var inputName in inputNames)
            {
                CoerceToType(node, inputs, output, inputDefs, inputName, matchedType);
            }

            return matchedType;
        }

        static void CoerceToType(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output,
            Dictionary<string, InputDef> inputDefs, string inputName, string expectedType)
        {
            var inputDef = inputDefs[inputName];
            if (!TryCoerce(ref inputDef, inputs, output, expectedType))
            {
                var inputType = GetOutputType(inputDef, inputs, output);
                throw new ParseException(
                    $"Mismatched argument type ({inputType} vs. {expectedType})", node.Lexeme.Span);
            }
            inputDefs[inputName] = inputDef;
        }

        static bool TryCoerce(
            ref InputDef inputDef, Dictionary<string, ParserInput> inputs,
            Dictionary<string, NodeDef> output, string expectedType)
        {
            var outputType = GetOutputType(inputDef, inputs, output);
            if (outputType == expectedType)
                return true;
            
            var outputLength = MtlxDataTypes.GetLength(outputType);
            var expectedLength = MtlxDataTypes.GetLength(expectedType);
            if (inputDef is FloatInputDef floatInputDef)
            {
                var newValues = new float[expectedLength];
                if (outputLength == 1)
                    Array.Fill(newValues, floatInputDef.Values[0]);
                else
                    Array.Copy(floatInputDef.Values, newValues, Math.Min(outputLength, expectedLength));
                inputDef = new FloatInputDef(expectedType, newValues);
                return true;
            }

            // The conversion nodes can convert a float to anything and
            // can convert between colors and vectors of the same size.
            if (outputLength == 1 || outputLength == expectedLength)
            {
                inputDef = new InlineInputDef(MtlxNodeTypes.Convert, expectedType, new()
                {
                    ["in"] = inputDef,
                });
                return true;
            }

            return false;
        }

        static void FlipTexCoordInput(Dictionary<string, InputDef> inputDefs)
        {
            inputDefs["texcoord"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = inputDefs["texcoord"],
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, new[] { 1.0f, -1.0f }),
                }),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, new[] { 0.0f, 1.0f }),
            });
        }

        static Compiler CreateVectorConstructorCompiler(string dataType)
        {
            var length = MtlxDataTypes.GetLength(dataType);
            return (node, inputs, output) =>
            {
                List<InputDef> inputDefs = new();
                List<string> outputTypes = new();
                var allFloatInputDefs = true;
                foreach (var child in node.Children)
                {
                    var inputDef = child.Compile(inputs, output);
                    if (inputDef is not FloatInputDef)
                        allFloatInputDefs = false;

                    inputDefs.Add(inputDef);
                    outputTypes.Add(GetOutputType(inputDef, inputs, output));
                }
                var totalLength = outputTypes.Select(MtlxDataTypes.GetLength).Sum();
                if (totalLength != length)
                    throw new ParseException($"Expected {length} components, found {totalLength}", node.Lexeme.Span);

                if (inputDefs.Count == 1)
                {
                    return inputDefs[0];
                }
                else if (allFloatInputDefs)
                {
                    var values = inputDefs.SelectMany(inputDef => ((FloatInputDef)inputDef).Values).ToArray();
                    return new FloatInputDef(dataType, values);
                }

                Dictionary<string, InputDef> inputDefsMap = new();
                var inIndex = 1;
                for (var i = 0; i < inputDefs.Count; ++i)
                {
                    var inputLength = MtlxDataTypes.GetLength(outputTypes[i]);
                    if (inputLength == 1)
                    {
                        inputDefsMap[$"in{inIndex++}"] = inputDefs[i];
                        continue;
                    }
                    var sharedInput = GetSharedInput(inputDefs[i], output);
                    for (var j = 0; j < inputLength; ++j)
                    {
                        inputDefsMap[$"in{inIndex++}"] = new InlineInputDef(
                            MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = sharedInput,
                                ["channels"] = new StringInputDef("xyzw".Substring(j, 1)),
                            });
                    }
                }

                return new InlineInputDef($"combine{length}", dataType, inputDefsMap);
            };
        }

        static Compiler CreateMatrixConstructorCompiler(string dataType)
        {
            var length = MtlxDataTypes.GetLength(dataType);
            var elementLength = MtlxDataTypes.GetElementLength(dataType);
            return (node, inputs, output) =>
            {
                List<InputDef> inputDefs = new();
                List<string> outputTypes = new();
                var allFloatInputDefs = true;
                foreach (var child in node.Children)
                {
                    var inputDef = child.Compile(inputs, output);
                    if (inputDef is not FloatInputDef)
                        allFloatInputDefs = false;

                    inputDefs.Add(inputDef);
                    outputTypes.Add(GetOutputType(inputDef, inputs, output));
                }
                var totalLength = outputTypes.Select(MtlxDataTypes.GetLength).Sum();
                if (totalLength != length)
                    throw new ParseException($"Expected {length} components, found {totalLength}", node.Lexeme.Span);

                if (inputDefs.Count == 1)
                {
                    return inputDefs[0];
                }
                else if (allFloatInputDefs)
                {
                    var values = inputDefs.SelectMany(inputDef => ((FloatInputDef)inputDef).Values).ToArray();
                    return new FloatInputDef(dataType, values);
                }

                Dictionary<string, InputDef> inputDefsMap = new();
                var inIndex = 1;
                var vectorType = MtlxDataTypes.GetTypeOfLength(elementLength);
                Dictionary<string, InputDef> vectorInputDefsMap = null;
                var vectorInIndex = 1;

                void AddScalar(InputDef inputDef)
                {
                    if (vectorInputDefsMap == null)
                    {
                        inputDefsMap[$"in{inIndex++}"] = new InlineInputDef(
                            $"combine{elementLength}", vectorType, vectorInputDefsMap = new());
                    }
                    vectorInputDefsMap[$"in{vectorInIndex++}"] = inputDef;
                    if (vectorInIndex > elementLength)
                    {
                        vectorInputDefsMap = null;
                        vectorInIndex = 1;
                    }
                }

                for (var i = 0; i < inputDefs.Count; ++i)
                {
                    var outputType = outputTypes[i];
                    if (outputType == vectorType && vectorInputDefsMap == null)
                    {
                        inputDefsMap[$"in{inIndex++}"] = inputDefs[i];
                        continue;
                    }
                    var outputLength = MtlxDataTypes.GetLength(outputType);
                    if (outputLength == 1)
                    {
                        AddScalar(inputDefs[i]);
                        continue;
                    }
                    var sharedInput = GetSharedInput(inputDefs[i], output);
                    for (var j = 0; j < outputLength; ++j)
                    {
                        AddScalar(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = sharedInput,
                            ["channels"] = new StringInputDef("xyzw".Substring(j, 1)),
                        }));
                    }
                }

                return new InlineInputDef(MtlxNodeTypes.Transpose, dataType, new()
                {
                    ["in"] = new InlineInputDef($"realitykit_combine{elementLength}", dataType, inputDefsMap),
                });
            };
        }

        static InputDef GetSharedInput(InputDef inputDef, Dictionary<string, NodeDef> output)
        {
            if (inputDef is not InlineInputDef inlineInputDef)
                return inputDef;
            
            var temporaryName = $"__Tmp{output.Count}";
            output.Add(temporaryName, inlineInputDef.Source);
            return new InternalInputDef(temporaryName);
        }

        static string GetOutputType(
            InputDef inputDef, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            return inputDef switch
            {
                FloatInputDef floatInputDef => floatInputDef.PortType,
                InternalInputDef internalInputDef => output[internalInputDef.Source].OutputType,
                ExternalInputDef externalInputDef => inputs[externalInputDef.Source].InputType,
                InlineInputDef inlineInputDef => inlineInputDef.Source.OutputType,
                ImplicitInputDef implicitInputDef => implicitInputDef.DataType,
                _ => MtlxDataTypes.String,
            };
        }

        static Compiler CreateImplicitCompiler(string dataType)
        {
            return (node, inputs, output) => new ImplicitInputDef(node.Lexeme.Span.contents, dataType);
        }

        static Compiler CreateSamplerCompiler(TextureSamplerState samplerState)
        {
            return (node, inputs, output) =>
            {
                var name = node.Lexeme.Span.contents;
                inputs[name] = new SamplerStateInput(samplerState);
                return new ExternalInputDef(name);
            };
        }
    }
}