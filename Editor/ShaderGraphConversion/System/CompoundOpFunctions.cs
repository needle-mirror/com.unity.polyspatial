using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    using CompilationContext = CompoundOpParser.CompilationContext;
    using ExternalInput = CompoundOpParser.ExternalInput;
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
        delegate InputDef Compiler(CompilationContext ctx, SyntaxNode node);
            
        static Dictionary<(string, Operator.VariantType), Compiler> s_OperatorCompilers = new()
        {
            [(";", Operator.VariantType.Default)] = CompileChildren,
            [(";", Operator.VariantType.Unary)] = CompileChildren,
            [("return", Operator.VariantType.Default)] = CompileChildren,
            [("return", Operator.VariantType.Unary)] = CompileChildren,
            [("=", Operator.VariantType.Default)] = AssignmentCompiler,
            [("+=", Operator.VariantType.Default)] = CreateBinaryAssignmentCompiler(MtlxNodeTypes.Add),
            [("-=", Operator.VariantType.Default)] = CreateBinaryAssignmentCompiler(MtlxNodeTypes.Subtract),
            [("*=", Operator.VariantType.Default)] = CreateBinaryAssignmentCompiler(MtlxNodeTypes.Multiply),
            [("/=", Operator.VariantType.Default)] = CreateBinaryAssignmentCompiler(MtlxNodeTypes.Divide),
            [("%=", Operator.VariantType.Default)] = CreateBinaryAssignmentCompiler(MtlxNodeTypes.Modulo),
            [("++", Operator.VariantType.Default)] = CreateUnaryAssignmentCompiler(MtlxNodeTypes.Add, true),
            [("++", Operator.VariantType.Prefix)] = CreateUnaryAssignmentCompiler(MtlxNodeTypes.Add, false),
            [("--", Operator.VariantType.Default)] = CreateUnaryAssignmentCompiler(MtlxNodeTypes.Subtract, true),
            [("--", Operator.VariantType.Prefix)] = CreateUnaryAssignmentCompiler(MtlxNodeTypes.Subtract, false),
            [("+", Operator.VariantType.Prefix)] = IdentityCompiler,
            [("-", Operator.VariantType.Prefix)] = CreateUnaryOpCompiler(MtlxNodeTypes.Subtract, "in2"),
            [("+", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Add, true),
            [("-", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Subtract, true),
            [("*", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Multiply, true),
            [("/", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Divide, true),
            [("%", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Modulo, true),
            [(".", Operator.VariantType.Default)] = SwizzleCompiler,
            [("!", Operator.VariantType.Default)] = NotCompiler,
            [("==", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfEqual, 1.0f),
            [("!=", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfEqual, 0.0f),
            [("<=", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreater, 0.0f),
            [(">=", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreaterOrEqual, 1.0f),
            [(">", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreater, 1.0f),
            [("<", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreaterOrEqual, 0.0f),
            [("&&", Operator.VariantType.Default)] = AndCompiler,
            [("||", Operator.VariantType.Default)] = OrCompiler,
            [("?", Operator.VariantType.Default)] = ConditionalCompiler,
            [("{", Operator.VariantType.Default)] = CreateConstructorCompiler(),
            [("abs", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Absolute),
            [("acos", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Arccosine),
            [("all", Operator.VariantType.FunctionCall)] = AllCompiler,
            [("any", Operator.VariantType.FunctionCall)] = AnyCompiler,
            [("asin", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Arcsine),
            [("atan", Operator.VariantType.FunctionCall)] = AtanCompiler,
            [("atan2", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.Arctangent2, "iny", "inx"),
            [("ceil", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Ceil),
            [("clamp", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.Clamp, "in", "low", "high"),
            [("cos", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Cosine),
            [("cosh", Operator.VariantType.FunctionCall)] = HyperbolicCosineCompiler,
            [("cross", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.CrossProduct),
            [("degrees", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Multiply, Mathf.Rad2Deg),
            [("distance", Operator.VariantType.FunctionCall)] = DistanceCompiler,
            [("dot", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(
                MtlxNodeTypes.DotProduct, false, MtlxDataTypes.Float),
            [("exp", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Exponential),
            [("exp2", Operator.VariantType.FunctionCall)] = Exp2Compiler,
            [("float", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Float),
            [("float2", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Vector2),
            [("float2x2", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Matrix22),
            [("float3", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Vector3),
            [("float3x3", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Matrix33),
            [("float4", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Vector4),
            [("float4x4", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Matrix44),
            [("float", Operator.VariantType.DefinitionType)] = CreateDefinitionCompiler(MtlxDataTypes.Float),
            [("float2", Operator.VariantType.DefinitionType)] = CreateDefinitionCompiler(MtlxDataTypes.Vector2),
            [("float2x2", Operator.VariantType.DefinitionType)] = CreateDefinitionCompiler(MtlxDataTypes.Matrix22),
            [("float3", Operator.VariantType.DefinitionType)] = CreateDefinitionCompiler(MtlxDataTypes.Vector3),
            [("float3x3", Operator.VariantType.DefinitionType)] = CreateDefinitionCompiler(MtlxDataTypes.Matrix33),
            [("float4", Operator.VariantType.DefinitionType)] = CreateDefinitionCompiler(MtlxDataTypes.Vector4),
            [("float4x4", Operator.VariantType.DefinitionType)] = CreateDefinitionCompiler(MtlxDataTypes.Matrix44),            
            [("floor", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Floor),
            [("fmod", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Modulo, true),
            [("frac", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.RealityKitFractional),
            [("isinf", Operator.VariantType.FunctionCall)] = IsInfCompiler,
            [("isnan", Operator.VariantType.FunctionCall)] = IsNanCompiler,
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
            [("refract", Operator.VariantType.FunctionCall)] = RefractCompiler,
            [("round", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Round),
            [("rsqrt", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Power, -0.5f),
            [("saturate", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Clamp),
            [("sign", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Sign),
            [("sin", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Sine),
            [("sinh", Operator.VariantType.FunctionCall)] = HyperbolicSineCompiler,
            [("smoothstep", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.SmoothStep, "low", "high", "in"),
            [("splitlr", Operator.VariantType.FunctionCall)] = SplitLRCompiler,
            [("sqrt", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.SquareRoot),
            [("step", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.RealityKitStep, "edge", "in"),
            [("tan", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Tangent),
            [("tanh", Operator.VariantType.FunctionCall)] = HyperbolicTangentCompiler,
            [("transpose", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Transpose),
            [("trunc", Operator.VariantType.FunctionCall)] = TruncCompiler,
            [("SAMPLE_TEXTURE2D", Operator.VariantType.FunctionCall)] = SampleTexture2DCompiler,
            [("SAMPLE_TEXTURE2D_LOD", Operator.VariantType.FunctionCall)] = SampleTexture2DLodCompiler,
            [("SAMPLE_TEXTURE3D", Operator.VariantType.FunctionCall)] = SampleTexture3DCompiler,
            [("SAMPLE_TEXTURE3D_LOD", Operator.VariantType.FunctionCall)] = SampleTexture3DLodCompiler,
            [("SAMPLE_TEXTURECUBE_LOD", Operator.VariantType.FunctionCall)] = SampleTextureCubeLodCompiler,
            [("GATHER_TEXTURE2D", Operator.VariantType.FunctionCall)] = GatherTexture2DCompiler,
        };

        static FloatInputDef s_ZFlipMatrix = new(
            MtlxDataTypes.Matrix44,
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);

        static InlineInputDef s_GeometryModifierFlippedWorldToModel = new(
            MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44, new()
        {
            ["in1"] = new InlineInputDef(
                MtlxNodeTypes.RealityKitGeometryModifierWorldToModel, MtlxDataTypes.Matrix44, new(), "worldToModel"),
            ["in2"] = s_ZFlipMatrix,
        });

        static InlineInputDef s_GeometryModifierViewToProjection = new(
            MtlxNodeTypes.RealityKitGeometryModifierViewToProjection,
            MtlxDataTypes.Matrix44, new(), "viewToProjection");

        static InlineInputDef s_SurfaceViewToProjection = new(
            MtlxNodeTypes.RealityKitSurfaceViewToProjection,
            MtlxDataTypes.Matrix44, new(), "viewToProjection");

        static Dictionary<string, Compiler> s_SymbolCompilers = new()
        {
            ["PI"] = CreateConstantCompiler(Mathf.PI),
            ["HALF_PI"] = CreateConstantCompiler(Mathf.PI * 0.5f),
            ["FOG_EXP"] = CreateImplicitCompiler(MtlxDataTypes.Boolean),
            ["FOG_EXP2"] = CreateImplicitCompiler(MtlxDataTypes.Boolean),
            ["FOG_LINEAR"] = CreateImplicitCompiler(MtlxDataTypes.Boolean),
            ["LIGHTMAP_ON"] = CreateImplicitCompiler(MtlxDataTypes.Boolean),
            [PolySpatialShaderGlobals.Time] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.SinTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.CosTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.DeltaTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.FogColor] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.FogParams] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.GlossyEnvironmentColor] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.VolumeToWorld] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            [PolySpatialShaderProperties.ObjectBoundsCenter] = CreateImplicitCompiler(MtlxDataTypes.Vector3),
            [PolySpatialShaderProperties.ObjectBoundsExtents] = CreateImplicitCompiler(MtlxDataTypes.Vector3),
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

#if DISABLE_MATERIALX_EXTENSIONS
            ["unity_ObjectToWorld"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["unity_WorldToObject"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_V"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_I_V"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_P"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_I_P"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_VP"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_I_VP"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
#else
            ["unity_ObjectToWorld"] = CreateMatrixCompiler(
                s_ZFlipMatrix, MtlxNodeTypes.RealityKitGeometryModifierModelToWorld,
                MtlxNodeTypes.RealityKitSurfaceModelToWorld, "modelToWorld", s_ZFlipMatrix, false),
            ["unity_WorldToObject"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    s_ZFlipMatrix, MtlxNodeTypes.RealityKitGeometryModifierWorldToModel,
                    "worldToModel", s_ZFlipMatrix, false),
                CreateMatrixCompiler(
                    s_ZFlipMatrix, MtlxNodeTypes.RealityKitSurfaceModelToWorld,
                    "modelToWorld", s_ZFlipMatrix, true)),
            ["UNITY_MATRIX_V"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, false),
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, false)),
            ["UNITY_MATRIX_I_V"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, true),
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, true)),
            ["UNITY_MATRIX_P"] = CreateMatrixCompiler(
                null, MtlxNodeTypes.RealityKitGeometryModifierViewToProjection,
                MtlxNodeTypes.RealityKitSurfaceViewToProjection, "viewToProjection", null, false),
            ["UNITY_MATRIX_I_P"] = CreateMatrixCompiler(
                null, MtlxNodeTypes.RealityKitGeometryModifierProjectionToView,
                MtlxNodeTypes.RealityKitSurfaceProjectionToView, "projectionToView", null, false),
            ["UNITY_MATRIX_VP"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    s_GeometryModifierViewToProjection, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, false),
                CreateMatrixCompiler(
                    s_SurfaceViewToProjection, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, false)),
            ["UNITY_MATRIX_I_VP"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    s_GeometryModifierViewToProjection, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, true),
                CreateMatrixCompiler(
                    s_SurfaceViewToProjection, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, true)),
            ["polySpatial_TangentToWorld"] = CreateTangentMatrixCompiler(false),
            ["polySpatial_WorldToTangent"] = CreateTangentMatrixCompiler(true),
            ["polySpatial_WorldSpaceViewDirection"] = WorldSpaceViewDirectionCompiler,
            ["polySpatial_ObjectSpaceViewVector"] = ObjectSpaceViewVectorCompiler,
#endif
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
            for (var i = 0; i < PolySpatialShaderProperties.ReflectionProbeCount; ++i)
            {
                var reflectionProbeTextureProperty = PolySpatialShaderProperties.ReflectionProbeTexturePrefix + i;
                s_SymbolCompilers.Add(reflectionProbeTextureProperty, CreateImplicitCompiler(MtlxDataTypes.Filename));
                s_SymbolCompilers.Add(
                    $"sampler{reflectionProbeTextureProperty}",
                    CreateSamplerCompiler(new() { wrap = TextureSamplerState.WrapMode.Clamp }));
                s_SymbolCompilers.Add(
                    PolySpatialShaderProperties.ReflectionProbeWeightPrefix + i,
                    CreateImplicitCompiler(MtlxDataTypes.Float));
            }
        }

        internal static InputDef CompileOperator(CompilationContext ctx, SyntaxNode node)
        {
            var op = node.Lexeme as Operator;
            if (s_OperatorCompilers.TryGetValue((op.Span.contents, op.Variant), out var compiler))
                return compiler(ctx, node);
            
            if (op.Variant == Operator.VariantType.FunctionCall &&
                ctx.functions.TryGetValue(op.Span.contents, out var functionNode))
            {
                CompilationContext subCtx = new(ctx);

                // The children of the definition (aside from the last) are the parameters.
                var functionDefinition = functionNode.Children[0];
                node.RequireChildCount(functionDefinition.Children.Count - 1);
                for (var i = 0; i < node.Children.Count; ++i)
                {
                    var parameterNode = functionDefinition.Children[i];
                    var isOut = (parameterNode.Lexeme.Span.contents == "out");
                    if (isOut || parameterNode.Lexeme.Span.contents is "inout" or "in")
                    {
                        // Skip over in/out/inout qualifier, which should be followed by type.
                        parameterNode.RequireChildCount(1);
                        parameterNode = parameterNode.Children[0];
                    }
                    // Type is followed by symbol.
                    parameterNode.RequireChildCount(1);
                    var symbol = parameterNode.Children[0].Lexeme.Span.contents;

                    var hlslType = parameterNode.Lexeme.Span.contents;
                    if (hlslType == "UnitySamplerState")
                    {
                        // Hack for handling sampler states, which don't correspond to MaterialX data types:
                        // remap input from outer context to subcontext.
                        var argumentNode = node.Children[i];
                        var inputDef = argumentNode.Compile(ctx);
                        var samplerState = RequireTextureSampler(ctx, argumentNode, inputDef);
                        subCtx.inputs.Add(subCtx.variablePrefix + symbol, new SamplerStateInput(samplerState));
                        continue;
                    }
                    var dataType = MtlxDataTypes.GetTypeForHlsl(hlslType);
                    if (dataType == null)
                        throw new ParseException("Expected data type", parameterNode.Lexeme.Span);

                    // Use the subcontext to get the scoped variable definition.
                    var (_, newVersionedDef) = IncrementVersionedVariableDef(subCtx, symbol);
                    if (isOut)
                    {
                        // For "out" parameters, just initialize to the default value.
                        ctx.output[newVersionedDef.Source] = new(MtlxNodeTypes.Constant, dataType, new());
                    }
                    else
                    {
                        // For normal and "inout," evaluate and coerce the argument.
                        var argumentNode = node.Children[i];
                        var inputDef = argumentNode.Compile(ctx);
                        if (!TryCoerce(ctx, ref inputDef, dataType))
                            throw new ParseException($"Expected {dataType}", argumentNode.Lexeme.Span);

                        ctx.output[newVersionedDef.Source] = new(MtlxNodeTypes.Dot, dataType, new()
                        {
                            ["in"] = inputDef,
                        });
                    }
                    ctx.output[subCtx.variablePrefix + symbol] = new(MtlxNodeTypes.Dot, dataType, new()
                    {
                        ["in"] = newVersionedDef,
                    });
                }
                var returnDef = CompileFunction(subCtx, functionNode);
                var returnType = MtlxDataTypes.GetTypeForHlsl(functionNode.Lexeme.Span.contents);
                if (returnType != null && !TryCoerce(ctx, ref returnDef, returnType))
                    throw new ParseException($"Expected return type {returnType}", functionNode.Lexeme.Span);

                // Extract the "out"/"inout" arguments.
                for (var i = 0; i < node.Children.Count; ++i)
                {
                    var parameterNode = functionDefinition.Children[i];
                    if (parameterNode.Lexeme.Span.contents is not ("out" or "inout"))
                        continue;
                    
                    // We verified before compiling the function that the grandchild contains the symbol.
                    var symbol = parameterNode.Children[0].Children[0].Lexeme.Span.contents;

                    // Similarly, we know that there will be a version of the variable available.
                    Assert.IsTrue(TryGetVersionedVariableDef(subCtx, symbol, out var versionedVariableDef));

                    // Handle the argument extraction as an assignment so that we can swizzle.
                    var argumentNode = node.Children[i];
                    CompileAssignment(ctx, argumentNode, argumentNode, versionedVariableDef);
                }

                return returnDef;
            }

            throw new ParseException($"Unknown operator {op.Span.contents}", op.Span);
        }

        internal static InputDef CompileFunction(CompilationContext ctx, SyntaxNode node)
        {
            // The final child of the definition contains the body of the function ({).
            var functionDefinition = node.Children[0];
            var functionBody = functionDefinition.Children[^1];
            return CompileChildren(ctx, functionBody);
        }

        internal static InputDef CompileSymbol(CompilationContext ctx, SyntaxNode node)
        {
            // Outputs and temporary variables map to versioned values and take precedence over
            // inputs (in case we redefined an input).
            var symbol = node.Lexeme as Symbol;
            if (TryGetVersionedVariableDef(ctx, symbol.Span.contents, out var versionedVariableDef))
                return versionedVariableDef;

            // We use the variable prefix with inputs so that we can use the same input map across contexts:
            // The top level context (with an empty prefix) will have access to the actual node inputs,
            // and subcontexts will have access (only) to remapped or synthesized texture sampler inputs.
            var prefixedInput = ctx.variablePrefix + symbol.Span.contents;
            if (ctx.inputs.ContainsKey(prefixedInput))
                return new ExternalInputDef(prefixedInput);

            if (s_SymbolCompilers.TryGetValue(symbol.Span.contents, out var compiler))
                return compiler(ctx, node);
            
            throw new ParseException($"Unknown symbol {symbol.Span.contents}", symbol.Span);
        }

        // Helper function that compiles all the children of a node and returns the last result, if any.
        static InputDef CompileChildren(CompilationContext ctx, SyntaxNode node)
        {
            InputDef lastResult = null;
            foreach (var child in node.Children)
            {
                lastResult = child.Compile(ctx);
            }
            return lastResult;
        }

        static InputDef AssignmentCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(2);
            
            return CompileAssignment(ctx, node, node.Children[0], node.Children[1].Compile(ctx));
        }

        static Compiler CreateBinaryAssignmentCompiler(string nodeType)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(2);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(ctx),
                    ["in2"] = node.Children[1].Compile(ctx),
                };

                // Match LHS type (but accept float variants).
                var outputType = GetOutputType(ctx, inputDefs["in1"]);
                if (GetOutputType(ctx, inputDefs["in2"]) != MtlxDataTypes.Float)
                    CoerceToType(ctx, node, inputDefs, "in2", outputType);

                return CompileAssignment(
                    ctx, node, node.Children[0], new InlineInputDef(nodeType, outputType, inputDefs));
            };
        }

        static Compiler CreateUnaryAssignmentCompiler(string nodeType, bool postfixForm)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(1);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(ctx),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                };

                // LHS determines type.
                var outputType = GetOutputType(ctx, inputDefs["in1"]);
                return CompileAssignment(
                    ctx, node, node.Children[0], new InlineInputDef(nodeType, outputType, inputDefs), postfixForm);
            };
        }

        /// <summary>
        /// Compiles an assignment expression: either a direct assignment (=) or a compound assignment
        /// (+=, -=, *=, /=, %=, ++, --).  Direct assignments (but not compound assignments) may be part of
        /// initial variable definitions (e.g., "float a = 1;").  Also used to assign out/inout function arguments.
        /// </summary>
        /// <param name="ctx">The context shared by all nodes in the current scope.</param>
        /// <param name="node">The abstract syntax node representing the top level of the assignment
        /// (used as a location for errors).</param>
        /// <param name="lvalue">The abstract syntax node representing the target of the assignment.</param>
        /// <param name="rvalue">The expression to be assigned to the target.</param>
        /// <param name="postfixForm">If true, the operator (either ++ or --) is in its postfix form, meaning that
        /// the expression returned should represent the value *before* the assignment is performed.</param>
        /// <returns>The compiled expression representing the value (either the current value of the variable,
        /// or the previous value if postfixForm is true).</returns>
        static InputDef CompileAssignment(
            CompilationContext ctx, SyntaxNode node, SyntaxNode lvalue, InputDef rvalue, bool postfixForm = false)
        {
            string symbol;
            string variableType = null;
            string swizzle = null;
            if (lvalue.Lexeme is Symbol)
            {
                symbol = lvalue.Lexeme.Span.contents;
            }
            else if (lvalue.Lexeme is Operator &&
                lvalue.Lexeme.Span.contents == "." &&
                lvalue.Children.Count == 2 &&
                lvalue.Children[0].Lexeme is Symbol swizzleLeftSymbol &&
                lvalue.Children[1].Lexeme is Symbol swizzleRightSymbol)
            {
                symbol = swizzleLeftSymbol.Span.contents;
                swizzle = swizzleRightSymbol.Span.contents;
            }
            else if (lvalue.Lexeme is Operator op &&
                op.Variant == Operator.VariantType.DefinitionType &&
                lvalue.Children.Count == 1 &&
                lvalue.Children[0].Lexeme is Symbol leftGrandchildSymbol)
            {
                symbol = leftGrandchildSymbol.Span.contents;

                variableType = MtlxDataTypes.GetTypeForHlsl(op.Span.contents);
                if (variableType == null)
                    throw new ParseException("Unknown type", op.Span);
                    
                if (!TryCoerce(ctx, ref rvalue, variableType))
                    throw new ParseException($"Expected {op.Span.contents} rvalue", node.Lexeme.Span);
            }
            else
            {
                throw new ParseException("Invalid lvalue for assignment", node.Lexeme.Span);
            }

            // Increment the version.
            var (oldVersionedDef, newVersionedDef) = IncrementVersionedVariableDef(ctx, symbol);

            // Get the type from the old version, if any.
            if (oldVersionedDef != null)
                variableType = GetOutputType(ctx, oldVersionedDef);
            else if (postfixForm)
                throw new ParseException($"Variable {symbol} not initialized", node.Lexeme.Span);
            
            // Coerce the rvalue into the expected type, if any.
            var expectedType = (swizzle != null) ? MtlxDataTypes.GetTypeOfLength(swizzle.Length) : variableType;
            if (expectedType != null && !TryCoerce(ctx, ref rvalue, expectedType))
                throw new ParseException($"Expected {expectedType} rvalue", node.Lexeme.Span);
            
            // Handle swizzle, if any, by combining old version with new values.
            if (swizzle != null)
            {
                var sharedRvalue = GetSharedInput(ctx, rvalue);
                
                var length = MtlxDataTypes.GetLength(variableType);
                Dictionary<string, InputDef> inputDefs = new();
                for (var i = 0; i < length; ++i)
                {
                    InputDef inputDef;
                    var vectorElement = "xyzw".Substring(i, 1);
                    var colorElement = "rgba".Substring(i, 1);
                    var vectorIndex = swizzle.IndexOf(vectorElement);
                    var swizzleIndex = (vectorIndex == -1) ? swizzle.IndexOf(colorElement) : vectorIndex;

                    if (swizzleIndex != -1)
                    {
                        if (swizzle.Length == 1)
                        {
                            inputDef = sharedRvalue;
                        }
                        else
                        {
                            inputDef = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = sharedRvalue,
                                ["channels"] = new StringInputDef("xyzw".Substring(swizzleIndex, 1)),
                            });
                        }
                    }
                    else
                    {
                        if (oldVersionedDef == null)
                            throw new ParseException($"Variable {symbol} not initialized", node.Lexeme.Span);

                        inputDef = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = oldVersionedDef,
                            ["channels"] = new StringInputDef(vectorElement),
                        });
                    }

                    inputDefs[$"in{i + 1}"] = inputDef;
                }

                rvalue = new InlineInputDef($"combine{length}", variableType, inputDefs);
            }

            // The versioned symbol contains the actual value.
            var outputType = GetOutputType(ctx, rvalue);
            ctx.output[newVersionedDef.Source] = rvalue switch
            {
                InlineInputDef inlineInputDef => inlineInputDef.Source,
                _ => new(MtlxNodeTypes.Dot, outputType, new()
                {
                    ["in"] = rvalue,
                }),
            };

            // The prefixed symbol points to the versioned symbol.
            ctx.output[ctx.variablePrefix + symbol] = new(MtlxNodeTypes.Dot, outputType, new()
            {
                ["in"] = newVersionedDef,
            });

            // The evaluated value refers to the previous or current versioned symbol.
            return postfixForm ? oldVersionedDef : newVersionedDef;
        }

        /// <summary>
        /// Obtains the current versioned reference for the named variable and generates a new reference with an
        /// incremented version number.  Versioned references are of the form #_[name], starting with 0_[name]
        /// and increasing by one with each revision.  The names start with integers to avoid collisions with
        /// unversioned symbols (which must be valid identifiers, and thus can't start with numbers).
        /// </summary>
        /// <param name="ctx">The context shared by all nodes in the current scope.</param>
        /// <param name="symbol">The symbol representing the (unversioned) variable to obtain.</param>
        /// <returns>A tuple containing the old versioned reference for the variable (or null for none) and the
        /// new versioned referenced.</returns>
        static (InternalInputDef oldVersionedDef, InternalInputDef newVersionedDef) IncrementVersionedVariableDef(
            CompilationContext ctx, string symbol)
        {
            var version = 0;
            if (TryGetVersionedVariableDef(ctx, symbol, out var oldVersionedDef))
            {
                var oldVersionedSymbol = oldVersionedDef.Source;
                version = int.Parse(oldVersionedSymbol.Remove(
                    oldVersionedSymbol.Length - 1 - ctx.variablePrefix.Length - symbol.Length)) + 1;
            }
            return (oldVersionedDef, new InternalInputDef($"{version}_{ctx.variablePrefix}{symbol}"));
        }

        /// <summary>
        /// Retrieves the most recent versioned reference to a variable value by looking up the symbol in the output.
        /// </summary>
        /// <param name="output">The map that will contain the generated node definitions for the parsed
        /// expression.</param>
        /// <param name="symbol">The name of the variable to look up.</param>
        /// <param name="versionedVariableDef">The definition that will hold the versioned reference.</param>
        /// <returns>True if the variable was found, false if not.</returns>
        static bool TryGetVersionedVariableDef(
            CompilationContext ctx, string symbol, out InternalInputDef versionedVariableDef)
        {
            if (ctx.output.TryGetValue(ctx.variablePrefix + symbol, out var nodeDef) &&
                nodeDef.NodeType == MtlxNodeTypes.Dot &&
                nodeDef.Inputs.TryGetValue("in", out var inputDef) &&
                inputDef is InternalInputDef internalInputDef)
            {
                versionedVariableDef = internalInputDef;
                return true;
            }
            versionedVariableDef = null;
            return false;
        }

        static InputDef IdentityCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);
            return node.Children[0].Compile(ctx);
        }

        static InputDef AtanCompiler(CompilationContext ctx, SyntaxNode node)
        {
            // Note: If the atan2 node definitions had reasonable defaults (i.e., defaulting inx to a vector
            // of 1.0), we could just rely on that and use CreateUnaryOpCompiler.  However, the current node
            // definitions have 1.0 as the default inx for the float variant but zero vectors as the defaults for
            // the vector variants (and one vectors for the iny defaults, which makes me think it's probably
            // an oversight).
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["iny"] = node.Children[0].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "iny");

            var ones = new float[MtlxDataTypes.GetLength(matchedType)];
            Array.Fill(ones, 1.0f);
            inputDefs.Add("inx", new FloatInputDef(matchedType, ones));

            return new InlineInputDef(MtlxNodeTypes.Arctangent2, matchedType, inputDefs);
        }

        static InputDef SwizzleCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(2);
            var leftInputDef = node.Children[0].Compile(ctx);
            var leftInputType = GetOutputType(ctx, leftInputDef);

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

        static InputDef NotCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["value1"] = node.Children[0].Compile(ctx),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
            };
            CoerceToType(ctx, node, inputDefs, "value1", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, inputDefs);
        }

        static InputDef AndCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(ctx),
                ["in2"] = node.Children[1].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "in1", MtlxDataTypes.Float);
            CoerceToType(ctx, node, inputDefs, "in2", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, inputDefs),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef OrCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> leftInputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            CoerceToType(ctx, node, leftInputDefs, "in", MtlxDataTypes.Float);

            Dictionary<string, InputDef> rightInputDefs = new()
            {
                ["in"] = node.Children[1].Compile(ctx),
            };
            CoerceToType(ctx, node, rightInputDefs, "in", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, leftInputDefs),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, rightInputDefs),
                }),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef ConditionalCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(2);

            var rightChild = node.Children[1];
            if (rightChild.Lexeme is not Operator rightOp || rightOp.Span.contents != ":")
                throw new ParseException("Expected ':'", node.Lexeme.Span);
            
            rightChild.RequireChildCount(2);
            
            var valueDef = node.Children[0].Compile(ctx);
            var trueDef = rightChild.Children[0].Compile(ctx);
            var falseDef = rightChild.Children[1].Compile(ctx);
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["value1"] = valueDef,
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = falseDef,
                ["in2"] = trueDef,
            };
            CoerceToType(ctx, node, inputDefs, "value1", MtlxDataTypes.Float);
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1", "in2");

            return new InlineInputDef(MtlxNodeTypes.IfEqual, matchedType, inputDefs);
        }

        static InputDef AllCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            var inputType = CoerceToMatchedType(ctx, node, inputDefs, "in");

            if (inputType != MtlxDataTypes.Float)
            {
                var sharedInput = GetSharedInput(ctx, inputDefs["in"]);
                
                var inputLength = MtlxDataTypes.GetLength(inputType);
                for (var i = 0; i < inputLength; ++i)
                {
                    var componentDef = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedInput,
                        ["channels"] = new StringInputDef("xyzw".Substring(i, 1)),
                    });
                    inputDefs["in"] = (i == 0) ? 
                        componentDef : new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = inputDefs["in"],
                        ["in2"] = componentDef,
                    });
                }
            }

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = inputDefs["in"],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef AnyCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            var inputType = CoerceToMatchedType(ctx, node, inputDefs, "in");

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = inputType == MtlxDataTypes.Float ?
                    inputDefs["in"] : new InlineInputDef(MtlxNodeTypes.Length, MtlxDataTypes.Float, inputDefs),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef DistanceCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(ctx),
                ["in2"] = node.Children[1].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1", "in2");

            var lengthNodeType = (matchedType == MtlxDataTypes.Float) ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            return new InlineInputDef(lengthNodeType, MtlxDataTypes.Float, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, inputDefs),
            });
        }

        static InputDef Exp2Compiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                ["in2"] = node.Children[0].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1", "in2");

            return new InlineInputDef(MtlxNodeTypes.Power, matchedType, inputDefs);
        }

        static InputDef IsInfCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);
            
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "in", MtlxDataTypes.Float);

            // 1/0 == float.PositiveInfinity
            // (see https://www.gnu.org/software/libc/manual/html_node/Infinity-and-NaN.html)
            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, inputDefs),
                ["value2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                }),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
            });
        }

        static InputDef IsNanCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);
            
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "in", MtlxDataTypes.Float);

            // If the input is NaN, then both step(in, 0) and step(0, in) will return 0; otherwise, one
            // or both will return 1.  So, we add them together and compare the result to zero.
            // I arrived at this approach after failing to get other methods to work in visionOS, perhaps
            // because of NaN-ignoring optimizations in the Metal shader compiler.  For instance, in == in
            // should be false for NaN, but it evaluates to true in visionOS (and may simply be optimized out).
            var sharedIn = GetSharedInput(ctx, inputDefs["in"]);
            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.RealityKitStep, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedIn,
                        ["edge"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.RealityKitStep, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["edge"] = sharedIn,
                    }),
                }),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
            });
        }

        static InputDef LengthCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);
            
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in");

            var nodeType = (matchedType == MtlxDataTypes.Float) ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            return new InlineInputDef(nodeType, MtlxDataTypes.Float, inputDefs);
        }

        static InputDef LerpCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(3);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["bg"] = node.Children[0].Compile(ctx),
                ["fg"] = node.Children[1].Compile(ctx),
                ["mix"] = node.Children[2].Compile(ctx),
            };
            
            // MaterialX's mix type requires a scalar parameter.  So, we use that for scalar parameters only
            // and use X + (Y - X)*S for vector parameters.
            if (GetOutputType(ctx, inputDefs["mix"]) == MtlxDataTypes.Float)
            {
                var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "bg", "fg");
                return new InlineInputDef(MtlxNodeTypes.Mix, matchedType, inputDefs);
            }
            else
            {
                var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "bg", "fg", "mix");
                var sharedXInput = GetSharedInput(ctx, inputDefs["bg"]);
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

        static InputDef MulCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(ctx),
                ["in2"] = node.Children[1].Compile(ctx),
            };
            var leftType = GetOutputType(ctx, inputDefs["in1"]);
            var rightType = GetOutputType(ctx, inputDefs["in2"]);

            // Two vectors -> dot product.
            var leftIsVector = MtlxDataTypes.IsVector(leftType);
            var rightIsVector = MtlxDataTypes.IsVector(rightType);
            if (leftIsVector && rightIsVector)
            {
                var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1", "in2");
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

                CoerceToType(ctx, node, inputDefs, "in", vectorType);
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

                CoerceToType(ctx, node, inputDefs, "in", vectorType);
                return new InlineInputDef(MtlxNodeTypes.TransformMatrix, vectorType, inputDefs);
            }
            else
            {
                // Anything else -> multiply
                var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1", "in2");
                return new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, inputDefs);
            }
        }

        static InputDef SplitLRCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(4);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["valuel"] = node.Children[0].Compile(ctx),
                ["valuer"] = node.Children[1].Compile(ctx),
                ["center"] = node.Children[2].Compile(ctx),
                ["texcoord"] = node.Children[3].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "valuel", "valuer");
            CoerceToType(ctx, node, inputDefs, "center", MtlxDataTypes.Float);
            CoerceToType(ctx, node, inputDefs, "texcoord", MtlxDataTypes.Vector2);

            return new InlineInputDef(MtlxNodeTypes.SplitLR, matchedType, inputDefs);
        }

        static InputDef TruncCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in");
            
            return CompileTrunc(ctx, matchedType, inputDefs["in"]);
        }

        static InputDef CompileTrunc(CompilationContext ctx, string type, InputDef inputDef)
        {
            var sharedIn = GetSharedInput(ctx, inputDef);

            // trunc(in) = floor(abs(in)) * sign(in)
            return new InlineInputDef(MtlxNodeTypes.Multiply, type, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Floor, type, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Absolute, type, new()
                    {
                        ["in"] = sharedIn,
                    }),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Sign, type, new()
                {
                    ["in"] = sharedIn,
                }),
            });
        }

        static InputDef SampleTexture2DCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(3);

            var fileInputDef = node.Children[0].Compile(ctx);
            var externalFile = GetExternalTexture(ctx, fileInputDef);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = fileInputDef,
                ["texcoord"] = node.Children[2].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(ctx, node, inputDefs, "texcoord", MtlxDataTypes.Vector2);

            var samplerInputDef = node.Children[1].Compile(ctx);
            var samplerState = RequireTextureSampler(ctx, node, samplerInputDef);

            if (externalFile != null)
                ApplyTextureTransform(inputDefs, externalFile);

#if DISABLE_MATERIALX_EXTENSIONS
            AddImageSamplerState(inputDefs, samplerState);
            return new InlineInputDef(MtlxNodeTypes.Image, MtlxDataTypes.Vector4, inputDefs);
#else
            AddTexture2DSamplerState(inputDefs, samplerState);
            return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2D, MtlxDataTypes.Vector4, inputDefs);
#endif
        }

        static InputDef SampleTexture2DLodCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(4);

            var fileInputDef = node.Children[0].Compile(ctx);
            var externalFile = GetExternalTexture(ctx, fileInputDef);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = fileInputDef,
                ["texcoord"] = node.Children[2].Compile(ctx),
                ["lod"] = node.Children[3].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(ctx, node, inputDefs, "texcoord", MtlxDataTypes.Vector2);
            CoerceToType(ctx, node, inputDefs, "lod", MtlxDataTypes.Float);

            var samplerInputDef = node.Children[1].Compile(ctx);
            var samplerState = RequireTextureSampler(ctx, node, samplerInputDef);
            AddTexture2DSamplerState(inputDefs, samplerState);
            
            if (externalFile != null)
                ApplyTextureTransform(inputDefs, externalFile);

            return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, inputDefs);
        }

        static void ApplyTextureTransform(Dictionary<string, InputDef> inputDefs, ExternalInputDef externalFile)
        {
            inputDefs["texcoord"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = inputDefs["texcoord"],
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new TextureTransformInputDef(externalFile.Source),
                        ["channels"] = new StringInputDef("xy"),
                    }),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new TextureTransformInputDef(externalFile.Source),
                    ["channels"] = new StringInputDef("zw"),
                }), 
            });
        }

        static InputDef SampleTexture3DCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(3);

            var fileInputDef = node.Children[0].Compile(ctx);
            var file = RequireExternalTexture(ctx, node, fileInputDef);

            var samplerInputDef = node.Children[1].Compile(ctx);
            var samplerState = RequireTextureSampler(ctx, node, samplerInputDef);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["texcoord"] = node.Children[2].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "texcoord", MtlxDataTypes.Vector3);

            var texCoords3D = CompileWrappedTexCoords3D(ctx, inputDefs["texcoord"], samplerState);
            
            return CompileSampleTexture3D(ctx, texCoords3D, file, samplerState, texCoords2D =>
            {
                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["file"] = file,
                    ["texcoord"] = texCoords2D,
                };
#if DISABLE_MATERIALX_EXTENSIONS
                AddImageSamplerState(inputDefs, samplerState);
                return new InlineInputDef(MtlxNodeTypes.Image, MtlxDataTypes.Vector4, inputDefs);
#else
                AddTexture2DSamplerState(inputDefs, samplerState);
                return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2D, MtlxDataTypes.Vector4, inputDefs);
#endif
            });
        }

        static InputDef SampleTexture3DLodCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(4);

            var fileInputDef = node.Children[0].Compile(ctx);
            var file = RequireExternalTexture(ctx, node, fileInputDef);

            var samplerInputDef = node.Children[1].Compile(ctx);
            var samplerState = RequireTextureSampler(ctx, node, samplerInputDef);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["texcoord"] = node.Children[2].Compile(ctx),
                ["lod"] = node.Children[3].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "texcoord", MtlxDataTypes.Vector3);
            CoerceToType(ctx, node, inputDefs, "lod", MtlxDataTypes.Float);
            
            var texCoords3D = CompileWrappedTexCoords3D(ctx, inputDefs["texcoord"], samplerState);
            var lod = inputDefs["lod"];
            
            return CompileSampleTexture3D(ctx, texCoords3D, file, samplerState, texCoords2D =>
            {
                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["file"] = file,
                    ["texcoord"] = texCoords2D,
                    ["lod"] = lod,
                };
                AddTexture2DSamplerState(inputDefs, samplerState);
                return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, inputDefs);
            });
        }

        // As a helper for SampleTexture3DCompiler and SampleTexture3DLodCompiler, this method converts raw 3D texture
        // coordinates to wrapped coordinates in [0, 1) according to the wrap mode.  Keeping the range open-ended helps
        // to avoid exceeding the boundaries of each slice later on, when we map 3D coordinates to 2D ones.  
        static InputDef CompileWrappedTexCoords3D(
            CompilationContext ctx, InputDef texCoords3D, TextureSamplerState samplerState)
        {
            const float kOneMinusEpsilon = 1.0f - Vector3.kEpsilon;
            const float kTwoMinusEpsilon = 2.0f - Vector3.kEpsilon;
            switch (samplerState.wrap)
            {
                case TextureSamplerState.WrapMode.Repeat:
                    // For repeat mode, we can simply take the fraction, which will be in [0, 1).
                    return new InlineInputDef(MtlxNodeTypes.RealityKitFractional, MtlxDataTypes.Vector3, new()
                    {
                        ["in"] = texCoords3D,
                    });

                case TextureSamplerState.WrapMode.Clamp:
                    // Clamp to [0, 1) to prevent overflow into next slice.
                    return new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Vector3, new()
                    {
                        ["in"] = texCoords3D,
                        ["low"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["high"] = new FloatInputDef(MtlxDataTypes.Float, kOneMinusEpsilon),
                    });

                case TextureSamplerState.WrapMode.Mirror:
                    // Start with 2 * fract(uvw / 2), which repeats 0 -> 1.999... every two units.
                    var sharedFraction = GetSharedInput(
                        ctx, new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.RealityKitFractional, MtlxDataTypes.Vector3, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector3, new()
                            {
                                ["in1"] = texCoords3D,
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                            }),
                        }),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                    }));
                    // Then take the smaller of the result and (1.999... - result),
                    // which repeats 0 -> 0.999... -> 0 every two units.
                    return new InlineInputDef(MtlxNodeTypes.Minimum, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = sharedFraction,
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new FloatInputDef(
                                MtlxDataTypes.Vector3, kTwoMinusEpsilon, kTwoMinusEpsilon, kTwoMinusEpsilon),
                            ["in2"] = sharedFraction,
                        }),
                    });

                case TextureSamplerState.WrapMode.MirrorOnce:
                    // Taking the absolute value mirrors the function; clamp the result to [0, 1).
                    return new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Vector3, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Vector3, new()
                        {
                            ["in"] = texCoords3D,
                        }),
                        ["low"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["high"] = new FloatInputDef(MtlxDataTypes.Float, kOneMinusEpsilon),
                    });
                
                default:
                    throw new NotSupportedException($"Unknown wrap mode {samplerState.wrap}");
            }
        }

        delegate InputDef CompileSampleTexture2D(InputDef texCoords2D);

        // As a helper for SampleTexture3DCompiler and SampleTexture3DLodCompiler, this method simulates a 3D texture
        // sample by sampling a 2D texture containing vertically stacked slices once (for point sampling) or twice
        // (for linear sampling, with the two samples being blended according to the fraction).  
        static InputDef CompileSampleTexture3D(
            CompilationContext ctx, InputDef texCoords3D, ExternalInputDef file,
            TextureSamplerState samplerState, CompileSampleTexture2D compileSampleTexture2D)
        {
            var sharedDepth = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = new TextureSizeInputDef(file.Source),
                ["channels"] = new StringInputDef("z"),
            }));
            var sharedTexCoords3D = GetSharedInput(ctx, texCoords3D);

            if (samplerState.filter == TextureSamplerState.FilterMode.Point)
            {
                return compileSampleTexture2D(new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                {
                    // U' = U
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedTexCoords3D,
                        ["channels"] = new StringInputDef("x"),
                    }),
                    // V' = (floor(W * depth) + V) / depth
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Floor, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                                {
                                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                    {
                                        ["in"] = sharedTexCoords3D,
                                        ["channels"] = new StringInputDef("z"),
                                    }),
                                    ["in2"] = sharedDepth,
                                }),
                            }),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = sharedTexCoords3D,
                                ["channels"] = new StringInputDef("y"),
                            }),
                        }),
                        ["in2"] = sharedDepth,
                    }),
                }));
            }
            var sharedU = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedTexCoords3D,
                ["channels"] = new StringInputDef("x"),
            }));

            // To prevent bleeding between slices at their tops and bottoms, when we use linear filtering,
            // we need to clamp V to [0.5/height, 1 - 0.5/height].
            var sharedHalfTexelHeight = GetSharedInput(
                ctx, new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
            {
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = new TextureSizeInputDef(file.Source),
                    ["channels"] = new StringInputDef("y"),
                }),
            }));
            var sharedV = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = sharedTexCoords3D,
                    ["channels"] = new StringInputDef("y"),
                }),
                ["low"] = sharedHalfTexelHeight,
                ["high"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = sharedHalfTexelHeight,
                }),
            }));

            var sharedWD = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedTexCoords3D,
                        ["channels"] = new StringInputDef("z"),
                    }),
                    ["in2"] = sharedDepth,
                }),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
            }));

            var sharedFloorWD = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Floor, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedWD,
            }));

            var sharedCeilWD = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Ceil, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedWD,
            }));

            var depthMinusOne = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
            {
                ["in1"] = sharedDepth,
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });

            return new InlineInputDef(MtlxNodeTypes.Mix, MtlxDataTypes.Vector4, new()
            {
                ["bg"] = compileSampleTexture2D(new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                {
                    // U' = U
                    ["in1"] = sharedU,
                    // V' = (floor(W * depth - 0.5) + V) / depth
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            // If floor == -1, we either wrap around to depth - 1 or clamp to zero. 
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreaterOrEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = sharedFloorWD,
                                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in1"] = sharedFloorWD,
                                ["in2"] = samplerState.wrap switch
                                {
                                    TextureSamplerState.WrapMode.Repeat => depthMinusOne,
                                    _ => new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                },
                            }),
                            ["in2"] = sharedV,
                        }),
                        ["in2"] = sharedDepth,
                    }),
                })),
                ["fg"] = compileSampleTexture2D(new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                {
                    // U' = U
                    ["in1"] = sharedU,
                    // V' = (ceil(W * depth - 0.5) + V) / depth
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            // If ceil == depth, we either wrap around to zero or clamp to depth - 1. 
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreaterOrEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = sharedCeilWD,
                                ["value2"] = sharedDepth,
                                ["in1"] = samplerState.wrap switch
                                {
                                    TextureSamplerState.WrapMode.Repeat => new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                    _ => depthMinusOne,
                                },
                                ["in2"] = sharedCeilWD,
                            }),
                            ["in2"] = sharedV,
                        }),
                        ["in2"] = sharedDepth,
                    }),
                })),
                // Final color = lerp(BG, FG, fract(W * depth - 0.5))
                ["mix"] = new InlineInputDef(MtlxNodeTypes.RealityKitFractional, MtlxDataTypes.Float, new()
                {
                    ["in"] = sharedWD,
                }),
            });
        }

        static InputDef SampleTextureCubeLodCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(4);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = node.Children[0].Compile(ctx),
                ["texcoord"] = node.Children[2].Compile(ctx),
                ["lod"] = node.Children[3].Compile(ctx),
                ["u_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                ["v_wrap_mode"] = new StringInputDef("clamp_to_edge"),
            };
            CoerceToType(ctx, node, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(ctx, node, inputDefs, "texcoord", MtlxDataTypes.Vector3);
            CoerceToType(ctx, node, inputDefs, "lod", MtlxDataTypes.Float);

            // Create shared intermediate values for the texcoord direction and its
            // components so that we can reference them in multiple places.
            var sharedDir = GetSharedInput(ctx, inputDefs["texcoord"]);
            var sharedDirComps = "xyz".Select(comp =>
                GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedDir,
                ["channels"] = new StringInputDef(comp.ToString()),
            }))).ToArray();
            
            // Likewise, create shared values for the absolute value of the direction and its components.
            var sharedAbsDir = GetSharedInput(
                ctx, new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Vector3, new()
            {
                ["in"] = sharedDir,
            }));
            var sharedAbsDirComps = "xyz".Select(comp =>
                GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedAbsDir,
                ["channels"] = new StringInputDef(comp.ToString()),
            }))).ToArray();

            // Create a shared vector2 for the ZY/X component pair (with range [-1, 1]), used for the +X/-X faces.
            var sharedZY = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = sharedDir,
                    ["channels"] = new StringInputDef("zy"),
                }),
                ["in2"] = sharedAbsDirComps[0]
            }));

            // Create a shared vector2 for the XZ/Y component pair (with range [-1, 1]), used for the +Y/-Y faces.
            var sharedXZ = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = sharedDir,
                    ["channels"] = new StringInputDef("xz"),
                }),
                ["in2"] = sharedAbsDirComps[1]
            }));

            // Create a shared vector2 for the XY/Z component pair (with range [-1, 1]), used for the +Z/-Z faces.
            var sharedXY = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = sharedDir,
                    ["channels"] = new StringInputDef("xy"),
                }),
                ["in2"] = sharedAbsDirComps[2]
            }));

            // Create the value for the +X/-X faces.
            var valueX = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedDirComps[0],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                // dir.x > 0: +X
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedZY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, -0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of +X face: (0.5, 0.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 0.5f / 6.0f),
                }),
                // dir.x <= 0: -X
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedZY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of -X face: (0.5, 1.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 1.5f / 6.0f),
                }),
            });

            // Create the value for the +Y/-Y faces.
            var valueY = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedDirComps[1],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                // dir.y > 0: +Y
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXZ,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 0.5f / 6.0f),
                    }),
                    // Translate to center of +Y face: (0.5, 2.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 2.5f / 6.0f),
                }),
                // dir.y <= 0: -Y 
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXZ,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of -Y face: (0.5, 3.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 3.5f / 6.0f),
                }),
            });

            // Create the shared value for the +Z/-Z faces (shared because used in two places).
            var sharedValueZ = GetSharedInput(
                ctx, new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedDirComps[2],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                // dir.z > 0: +Z
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of +Z face: (0.5, 4.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 4.5f / 6.0f),
                }),
                // dir.z <= 0: -Z
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, -0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of -Z face: (0.5, 5.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 5.5f / 6.0f),
                }),
            }));

            // Choose the final texcoord value based on the component with the greatest absolute value.
            inputDefs["texcoord"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedAbsDirComps[0],
                ["value2"] = sharedAbsDirComps[1],
                // dir.x > dir.y
                ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
                {
                    ["value1"] = sharedAbsDirComps[0],
                    ["value2"] = sharedAbsDirComps[2],
                    // dir.x > dir.z: +X/-X
                    ["in1"] = valueX,
                    // dir.x <= dir.z: +Z/-Z
                    ["in2"] = sharedValueZ,
                }),
                // dir.x <= dir.y
                ["in2"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
                {
                    ["value1"] = sharedAbsDirComps[1],
                    ["value2"] = sharedAbsDirComps[2],
                    // dir.y > dir.z: +Y/-Y
                    ["in1"] = valueY,
                    // dir.y <= dir.z: +Z/-Z
                    ["in2"] = sharedValueZ,
                }),
            });

            var samplerInputDef = node.Children[1].Compile(ctx);
            var samplerState = RequireTextureSampler(ctx, node, samplerInputDef);
            
            StringInputDef minMagFilter = new(GetMinMagFilter(samplerState));
            inputDefs.Add("min_filter", minMagFilter);
            inputDefs.Add("mag_filter", minMagFilter);
            inputDefs.Add("mip_filter", new StringInputDef(GetMipFilter(samplerState)));

            inputDefs.Add("max_anisotropy", new FloatInputDef(MtlxDataTypes.Integer, GetMaxAnisotropy(samplerState)));

            return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, inputDefs);
        }

        static InputDef GatherTexture2DCompiler(CompilationContext ctx, SyntaxNode node)
        {
            // Accept, but don't require, the pixel offset argument.
            var hasOffset = (node.Children.Count == 4);
            if (!hasOffset)
                node.RequireChildCount(3);

            var fileInputDef = node.Children[0].Compile(ctx);
            var externalFile = RequireExternalTexture(ctx, node, fileInputDef);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = fileInputDef,
                ["texcoord"] = node.Children[2].Compile(ctx),
            };
            CoerceToType(ctx, node, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(ctx, node, inputDefs, "texcoord", MtlxDataTypes.Vector2);

            var samplerInputDef = node.Children[1].Compile(ctx);
            var samplerState = RequireTextureSampler(ctx, node, samplerInputDef);

            if (hasOffset)
            {
                inputDefs["offset"] = node.Children[3].Compile(ctx);
                CoerceToType(ctx, node, inputDefs, "offset", MtlxDataTypes.Vector2);
            }
            else
            {
                inputDefs["offset"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.0f, 0.0f);
            }
            
            ApplyTextureTransform(inputDefs, externalFile);

            // Refer to the implementation of GatherTexture2DNode:
            // https://github.cds.internal.unity3d.com/unity/unity/blob/93a364f095f55c0e7616dc8d1638d6c6c37b5ad5/Packages/com.unity.shadergraph/Editor/Data/Nodes/Input/Texture/GatherTexture2DNode.cs#L91
            var textureSize = GetSharedInput(
                ctx, new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
            {
                ["in"] = new TextureSizeInputDef(externalFile.Source),
                ["channels"] = new StringInputDef("xy"),
            }));
            var pixelTexCoords = GetSharedInput(
                ctx, new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = inputDefs["texcoord"],
                ["in2"] = textureSize,
            }));
            var offsetPlusHalf = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = CompileTrunc(ctx, MtlxDataTypes.Vector2, inputDefs["offset"]),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 0.5f),
            }));
            
            InlineInputDef CreateQuadrantDef(float quadrantX, float quadrantY)
            {
                Dictionary<string, InputDef> textureInputDefs = new()
                {
                    ["file"] = inputDefs["file"],
                    ["texcoord"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Floor, MtlxDataTypes.Vector2, new()
                            {
                                ["in"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                                {
                                    ["in1"] = pixelTexCoords,
                                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, quadrantX, quadrantY),
                                }),
                            }),
                            ["in2"] = offsetPlusHalf,
                        }),
                        ["in2"] = textureSize,
                    }),
                };
                AddTexture2DSamplerState(textureInputDefs, samplerState);
                return new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(
                        MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, textureInputDefs),
                    ["channels"] = new StringInputDef("x"),
                });
            }

            return new InlineInputDef(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
            {
                ["in1"] = CreateQuadrantDef(-0.5f, 0.5f),
                ["in2"] = CreateQuadrantDef(0.5f, 0.5f),
                ["in3"] = CreateQuadrantDef(0.5f, -0.5f),
                ["in4"] = CreateQuadrantDef(-0.5f, -0.5f),
            });
        }

        static InputDef RefractCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(3);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
                ["normal"] = node.Children[1].Compile(ctx),
                ["eta"] = node.Children[2].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in", "normal");
            CoerceToType(ctx, node, inputDefs, "eta", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.RealityKitRefract, matchedType, inputDefs);
        }

        static InputDef HyperbolicSineCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in");
            var sharedInput = GetSharedInput(ctx, inputDefs["in"]);

            // See https://en.wikipedia.org/wiki/Hyperbolic_functions#Exponential_definitions
            return new InlineInputDef(MtlxNodeTypes.Divide, matchedType, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                        {
                            ["in1"] = sharedInput,
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                        }),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = sharedInput,
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                }),
            });
        }

        static InputDef HyperbolicCosineCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in");
            var sharedInput = GetSharedInput(ctx, inputDefs["in"]);

            // See https://en.wikipedia.org/wiki/Hyperbolic_functions#Exponential_definitions
            return new InlineInputDef(MtlxNodeTypes.Divide, matchedType, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                        {
                            ["in1"] = sharedInput,
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                        }),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = sharedInput,
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                }),
            });
        }

        static InputDef HyperbolicTangentCompiler(CompilationContext ctx, SyntaxNode node)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(ctx),
            };
            var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in");

            // See https://en.wikipedia.org/wiki/Hyperbolic_functions#Exponential_definitions
            var sharedExpInput2 = GetSharedInput(ctx, new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                {
                    ["in1"] = inputDefs["in"],
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                }),
            }));

            return new InlineInputDef(MtlxNodeTypes.Divide, matchedType, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, new()
                {
                    ["in1"] = sharedExpInput2,
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, matchedType, new()
                {
                    ["in1"] = sharedExpInput2,
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
            });
        }

        static Compiler CreateUnaryOpCompiler(string nodeType, string inputPort = "in", string outputType = null)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(1);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    [inputPort] = node.Children[0].Compile(ctx),
                };
                var matchedType = CoerceToMatchedType(ctx, node, inputDefs, inputPort);

                return new InlineInputDef(nodeType, outputType ?? matchedType, inputDefs);
            };
        }

        static Compiler CreateBinaryOpCompiler(string nodeType, bool allowFloatRight = false, string outputType = null)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(2);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(ctx),
                    ["in2"] = node.Children[1].Compile(ctx),
                };

                // We allow the right hand side to be a float for the FA node variants, like vector * scalar.
                string matchedType;
                if (allowFloatRight && GetOutputType(ctx, inputDefs["in2"]) == MtlxDataTypes.Float)
                    matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1");
                else
                    matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1", "in2");
                    
                return new InlineInputDef(nodeType, outputType ?? matchedType, inputDefs);
            };
        }

        static Compiler CreateBinaryConstantOpCompiler(string nodeType, float constant)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(1);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(ctx),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, constant),
                };
                var matchedType = CoerceToMatchedType(ctx, node, inputDefs, "in1");
                
                return new InlineInputDef(nodeType, matchedType, inputDefs);
            };
        }

        static Compiler CreateNaryOpCompiler(string nodeType, params string[] inputPorts)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(inputPorts.Length);

                Dictionary<string, InputDef> inputDefs = new();
                for (var i = 0; i < inputPorts.Length; ++i)
                {
                    inputDefs.Add(inputPorts[i], node.Children[i].Compile(ctx));
                }
                var outputType = CoerceToMatchedType(ctx, node, inputDefs, inputPorts);

                return new InlineInputDef(nodeType, outputType, inputDefs);
            };
        }

        static Compiler CreateComparisonCompiler(string nodeType, float trueValue)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(2);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["value1"] = node.Children[0].Compile(ctx),
                    ["value2"] = node.Children[1].Compile(ctx),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, trueValue),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f - trueValue),
                };
                CoerceToType(ctx, node, inputDefs, "value1", MtlxDataTypes.Float);
                CoerceToType(ctx, node, inputDefs, "value2", MtlxDataTypes.Float);

                return new InlineInputDef(nodeType, MtlxDataTypes.Float, inputDefs);
            };
        }

        static TextureSamplerState RequireTextureSampler(CompilationContext ctx, SyntaxNode node, InputDef inputDef)
        {
            if (inputDef is ExternalInputDef externalInputDef)
            {
                if (ctx.inputs.TryGetValue(externalInputDef.Source, out var input) &&
                    input is SamplerStateInput samplerStateInput)
                {
                    return samplerStateInput.SamplerState ?? new();
                }
            }
            throw new ParseException($"Expected texture sampler argument", node.Lexeme.Span);
        }

        static ExternalInputDef RequireExternalTexture(CompilationContext ctx, SyntaxNode node, InputDef inputDef)
        {
            var externalInputDef = GetExternalTexture(ctx, inputDef);
            if (externalInputDef != null)
                return externalInputDef;

            throw new ParseException($"Expected texture argument", node.Lexeme.Span);
        }

        static ExternalInputDef GetExternalTexture(CompilationContext ctx, InputDef inputDef)
        {
            var externalInputDef = GetExternalInputDef(ctx, inputDef);
            if (externalInputDef != null &&
                ctx.inputs.TryGetValue(externalInputDef.Source, out var input) &&
                input is ExternalInput externalInput &&
                externalInput.InputType == MtlxDataTypes.Filename)
            {
                return externalInputDef;
            }
            return null;
        }

        // Follows the chain of references (if any) starting at the specified input def in order to reach the
        // final, external input reference.
        static ExternalInputDef GetExternalInputDef(CompilationContext ctx, InputDef inputDef)
        {
            switch (inputDef)
            {
                case ExternalInputDef externalInputDef:
                    return externalInputDef;

                case InternalInputDef internalInputDef:
                    if (ctx.output.TryGetValue(internalInputDef.Source, out var nodeDef) &&
                        nodeDef.NodeType == MtlxNodeTypes.Dot &&
                        nodeDef.Inputs.TryGetValue("in", out var nextInputDef))
                    {
                        return GetExternalInputDef(ctx, nextInputDef);
                    }
                    break;
            }
            return null;
        }

        /// <summary>
        /// Attempts to coerce the inputs identified by inputNames within the inputDefs map (which represents the inputs
        /// to a node in a compound op) so that they are the same type, which may involve promotion (float to vector)
        /// or conversion (color to vector).
        /// </summary>
        /// <param name="ctx">The context shared by all nodes in the current scope.</param>
        /// <param name="node">The abstract syntax node being for which the coercion is being undertaken.</param>
        /// <param name="inputDefs">The mapping of input names to their definitions, which may be modified to include
        /// type conversions.</param>
        /// <param name="inputNames">The names of the inputs to be coerced within the inputDefs map.</param>
        /// <returns>The common type to which all named inputs were coerced.</returns>
        static string CoerceToMatchedType(
            CompilationContext ctx, SyntaxNode node, Dictionary<string, InputDef> inputDefs,
            params string[] inputNames)
        {
            var maxLength = 1;
            var maxElementLength = 1;
            foreach (var inputName in inputNames)
            {
                var inputDef = inputDefs[inputName];
                var inputType = GetOutputType(ctx, inputDef);
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
                CoerceToType(ctx, node, inputDefs, inputName, matchedType);
            }

            return matchedType;
        }

        static void CoerceToType(
            CompilationContext ctx, SyntaxNode node, Dictionary<string, InputDef> inputDefs,
            string inputName, string expectedType)
        {
            var inputDef = inputDefs[inputName];
            if (!TryCoerce(ctx, ref inputDef, expectedType))
            {
                var inputType = GetOutputType(ctx, inputDef);
                throw new ParseException(
                    $"Mismatched argument type ({inputType} vs. {expectedType})", node.Lexeme.Span);
            }
            inputDefs[inputName] = inputDef;
        }

        static bool TryCoerce(CompilationContext ctx, ref InputDef inputDef, string expectedType)
        {
            var outputType = GetOutputType(ctx, inputDef);
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

            // Handle the conversion between vector4s and matrix22s as a special case; it's the one
            // case where we can't distinguish between types by their length.
            // This is specifically for the case of compiling "float2x2 foo = {<four scalars or two vector2s>};",
            // since the right side will be initially compiled as a vector4 (simply because it has four elements).
            if (outputType == MtlxDataTypes.Vector4 && expectedType == MtlxDataTypes.Matrix22)
            {
                var sharedInputDef = GetSharedInput(ctx, inputDef);
                inputDef = new InlineInputDef(MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = sharedInputDef,
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = sharedInputDef,
                        ["channels"] = new StringInputDef("zw"),
                    }),
                });
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

            // We can reduce the number of components of vectors by swizzling.
            var isColor = MtlxDataTypes.IsColor(outputType);
            var isVector = MtlxDataTypes.IsVector(outputType);
            if ((isColor || isVector) && expectedLength < outputLength)
            {
                inputDef = new InlineInputDef(MtlxNodeTypes.Swizzle, expectedType, new()
                {
                    ["in"] = inputDef,
                    ["channels"] = new StringInputDef((isColor ? "rgb" : "xyz").Substring(0, expectedLength)),
                });
                return true;
            }

            return false;
        }

        static void AddImageSamplerState(Dictionary<string, InputDef> inputDefs, TextureSamplerState samplerState)
        {
            inputDefs.Add("filtertype", new StringInputDef(GetFilterType(samplerState)));

            StringInputDef addressMode = new(GetAddressMode(samplerState));
            inputDefs.Add("uaddressmode", addressMode);
            inputDefs.Add("vaddressmode", addressMode);
        }

        static string GetAddressMode(TextureSamplerState samplerState)
        {
            return samplerState.wrap switch
            {
                TextureSamplerState.WrapMode.Clamp => "clamp",
                TextureSamplerState.WrapMode.Mirror => "mirror",
                _ => "periodic",
            };
        }

        static string GetFilterType(TextureSamplerState samplerState)
        {
            return samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Point => "closest",
                _ => "linear",
            };
        }

        static void AddTexture2DSamplerState(Dictionary<string, InputDef> inputDefs, TextureSamplerState samplerState)
        {
            StringInputDef minMagFilter = new(GetMinMagFilter(samplerState));
            inputDefs.Add("min_filter", minMagFilter);
            inputDefs.Add("mag_filter", minMagFilter);
            inputDefs.Add("mip_filter", new StringInputDef(GetMipFilter(samplerState)));

            StringInputDef wrapMode = new(GetWrapMode(samplerState));
            inputDefs.Add("u_wrap_mode", wrapMode);
            inputDefs.Add("v_wrap_mode", wrapMode);

            inputDefs.Add("max_anisotropy", new FloatInputDef(MtlxDataTypes.Integer, GetMaxAnisotropy(samplerState)));
        }

        static string GetMinMagFilter(TextureSamplerState samplerState)
        {
            return samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Point => "nearest",
                _ => "linear",
            };
        }

        static string GetMipFilter(TextureSamplerState samplerState)
        {
            return samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Trilinear => "linear",
                _ => "nearest",
            };
        }

        static string GetWrapMode(TextureSamplerState samplerState)
        {
            return samplerState.wrap switch
            {
                TextureSamplerState.WrapMode.Clamp => "clamp_to_edge",
                TextureSamplerState.WrapMode.Mirror => "mirrored_repeat",
                _ => "repeat",
            };
        }

        static int GetMaxAnisotropy(TextureSamplerState samplerState)
        {
            return samplerState.anisotropic switch
            {
                TextureSamplerState.Anisotropic.x2 => 2,
                TextureSamplerState.Anisotropic.x4 => 4,
                TextureSamplerState.Anisotropic.x8 => 8,
                TextureSamplerState.Anisotropic.x16 => 16,
                _ => 1,
            };
        }

        // Creates a compiler to construct the specified data type
        // (or null to infer the type from the number of elements).
        static Compiler CreateConstructorCompiler(string fixedDataType = null)
        {
            return (ctx, node) =>
            {
                List<InputDef> inputDefs = new();
                List<string> outputTypes = new();
                var allFloatInputDefs = true;
                foreach (var child in node.Children)
                {
                    var inputDef = child.Compile(ctx);
                    if (inputDef is not FloatInputDef)
                        allFloatInputDefs = false;

                    inputDefs.Add(inputDef);
                    outputTypes.Add(GetOutputType(ctx, inputDef));
                }
                var totalLength = outputTypes.Select(MtlxDataTypes.GetLength).Sum();
                string dataType;
                if (fixedDataType == null)
                {
                    dataType = MtlxDataTypes.GetTypeOfLength(totalLength);
                    if (dataType == null)
                        throw new ParseException($"No type known of length {totalLength}", node.Lexeme.Span);
                }
                else 
                {
                    var expectedLength = MtlxDataTypes.GetLength(fixedDataType);
                    if (totalLength != expectedLength)
                    {
                        throw new ParseException(
                            $"Expected {expectedLength} components, found {totalLength}", node.Lexeme.Span);
                    }
                    dataType = fixedDataType;
                }

                if (inputDefs.Count == 1)
                {
                    return inputDefs[0];
                }
                else if (allFloatInputDefs)
                {
                    var values = inputDefs.SelectMany(inputDef => ((FloatInputDef)inputDef).Values).ToArray();
                    return new FloatInputDef(dataType, values);
                }

                if (MtlxDataTypes.IsVector(dataType))
                    return CompileVectorConstructor(ctx, dataType, inputDefs, outputTypes);
                else if (MtlxDataTypes.IsMatrix(dataType))
                    return CompileMatrixConstructor(ctx, dataType, inputDefs, outputTypes);
                else
                    throw new ParseException($"Cannot construct type {dataType}", node.Lexeme.Span);
            };
        }
        
        /// <summary>
        /// Creates a compiler to define a variable of the specified type without initializing it
        /// (for example, "float foo;")  This is useful for out parameters of functions, for example.
        /// </summary>
        /// <param name="dataType">The type of variable to define.</param>
        /// <returns>A compiler that will generate a definition of the specified type.</returns>
        static Compiler CreateDefinitionCompiler(string dataType)
        {
            return (ctx, node) =>
            {
                node.RequireChildCount(1);

                var symbol = node.Children[0].Lexeme.Span.contents;
                var (oldVersionedDef, newVersionedDef) = IncrementVersionedVariableDef(ctx, symbol);
                if (oldVersionedDef != null)
                    throw new ParseException($"Variable {symbol} already defined", node.Lexeme.Span);

                // Map both the versioned symbol (to the value) and the unversioned (to the versioned).
                ctx.output[newVersionedDef.Source] = new(MtlxNodeTypes.Constant, dataType, new());
                ctx.output[ctx.variablePrefix + symbol] = new(MtlxNodeTypes.Dot, dataType, new()
                {
                    ["in"] = newVersionedDef,
                });

                return newVersionedDef;
            };
        }

        static InputDef CompileVectorConstructor(
            CompilationContext ctx, string dataType, List<InputDef> inputDefs, List<string> outputTypes)
        {
            Assert.IsTrue(MtlxDataTypes.IsVector(dataType));
            var dataTypeLength = MtlxDataTypes.GetLength(dataType);

            Dictionary<string, InputDef> inputDefsMap = new();
            var inIndex = 1;

            void AddScalar(InputDef inputDef)
            {
                Assert.IsTrue(inIndex <= dataTypeLength);
                inputDefsMap[$"in{inIndex++}"] = inputDef;
            }

            for (var i = 0; i < inputDefs.Count; ++i)
            {
                var inputLength = MtlxDataTypes.GetLength(outputTypes[i]);
                Assert.AreNotEqual(inputLength, 0);
                if (inputLength == 1)
                {
                    AddScalar(inputDefs[i]);
                    continue;
                }
                var inputDef = inputDefs[i];
                if (inputDef is FloatInputDef floatInputDef)
                {
                    foreach (var value in floatInputDef.Values)
                    {
                        AddScalar(new FloatInputDef(MtlxDataTypes.Float, value));
                    }
                }
                else
                {
                    var sharedInput = GetSharedInput(ctx, inputDef);
                    for (var j = 0; j < inputLength; ++j)
                    {
                        AddScalar(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = sharedInput,
                            ["channels"] = new StringInputDef("xyzw".Substring(j, 1)),
                        }));
                    }
                }
            }

            return new InlineInputDef($"combine{dataTypeLength}", dataType, inputDefsMap);
        }

        static InputDef CompileMatrixConstructor(
            CompilationContext ctx, string dataType, List<InputDef> inputDefs, List<string> outputTypes)
        {
            Assert.IsTrue(MtlxDataTypes.IsMatrix(dataType));

            Dictionary<string, InputDef> inputDefsMap = new();
            var inIndex = 1;
            var elementLength = MtlxDataTypes.GetElementLength(dataType);
            var vectorType = MtlxDataTypes.GetTypeOfLength(elementLength);
            Dictionary<string, InputDef> vectorInputDefsMap = null;
            var vectorInIndex = 1;

            void AddVector(InputDef inputDef)
            {
                Assert.IsTrue(inIndex <= elementLength);
                inputDefsMap[$"in{inIndex++}"] = inputDef;
            }

            void AddScalar(InputDef inputDef)
            {
                if (vectorInputDefsMap == null)
                    AddVector(new InlineInputDef($"combine{elementLength}", vectorType, vectorInputDefsMap = new()));
                
                Assert.IsTrue(vectorInIndex <= elementLength);
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
                    AddVector(inputDefs[i]);
                    continue;
                }
                var outputLength = MtlxDataTypes.GetLength(outputType);
                Assert.AreNotEqual(outputLength, 0);
                if (outputLength == 1)
                {
                    AddScalar(inputDefs[i]);
                    continue;
                }
                var inputDef = inputDefs[i];
                if (inputDef is FloatInputDef floatInputDef)
                {
                    foreach (var value in floatInputDef.Values)
                    {
                        AddScalar(new FloatInputDef(MtlxDataTypes.Float, value));
                    }
                }
                else
                {
                    var sharedInput = GetSharedInput(ctx, inputDef);
                    for (var j = 0; j < outputLength; ++j)
                    {
                        AddScalar(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = sharedInput,
                            ["channels"] = new StringInputDef("xyzw".Substring(j, 1)),
                        }));
                    }
                }
            }

            return new InlineInputDef(MtlxNodeTypes.Transpose, dataType, new()
            {
                ["in"] = new InlineInputDef($"realitykit_combine{elementLength}", dataType, inputDefsMap),
            });
        }

        static InputDef GetSharedInput(CompilationContext ctx, InputDef inputDef)
        {
            if (inputDef is not InlineInputDef inlineInputDef)
                return inputDef;
            
            var temporaryName = $"__Tmp{ctx.output.Count}";
            ctx.output.Add(temporaryName, inlineInputDef.Source);
            return new InternalInputDef(temporaryName);
        }

        static string GetOutputType(CompilationContext ctx, InputDef inputDef)
        {
            if (inputDef is PerStageInputDef perStageInputDef)
            {
                // Ensure that both stages have the same type.
                var vertexType = GetOutputType(ctx, perStageInputDef.Vertex);
                var fragmentType = GetOutputType(ctx, perStageInputDef.Fragment);
                Assert.AreEqual(vertexType, fragmentType);
                return vertexType;
            }
            return inputDef switch
            {
                FloatInputDef floatInputDef => floatInputDef.PortType,
                InternalInputDef internalInputDef => ctx.output[internalInputDef.Source].OutputType,
                ExternalInputDef externalInputDef => ctx.inputs[externalInputDef.Source].InputType,
                InlineInputDef inlineInputDef => inlineInputDef.Source.OutputType,
                ImplicitInputDef implicitInputDef => implicitInputDef.DataType,
                TextureSizeInputDef => MtlxDataTypes.Vector3,
                _ => MtlxDataTypes.String,
            };
        }

        static Compiler CreateConstantCompiler(float value)
        {
            return (ctx, node) => new FloatInputDef(MtlxDataTypes.Float, value);
        }

        static Compiler CreateImplicitCompiler(string dataType)
        {
            return (ctx, node) => new ImplicitInputDef(node.Lexeme.Span.contents, dataType);
        }

        static Compiler CreateSamplerCompiler(TextureSamplerState samplerState)
        {
            return (ctx, node) =>
            {
                var name = ctx.variablePrefix + node.Lexeme.Span.contents;
                ctx.inputs[name] = new SamplerStateInput(samplerState);
                return new ExternalInputDef(name);
            };
        }

        // Convenience function to create space transforms more conveniently. 
        // Returns (preMult * input * postMult) ^ (invert ? -1 : 1), using either the vertex node type
        // or the fragment node type for the input depending on the active stage.
        static Compiler CreateMatrixCompiler(
            InputDef preMult, string vertexNodeType, string fragmentNodeType,
            string outputName, InputDef postMult, bool invert)
        {
            return CreatePerStageCompiler(
                CreateMatrixCompiler(preMult, vertexNodeType, outputName, postMult, invert),
                CreateMatrixCompiler(preMult, fragmentNodeType, outputName, postMult, invert));
        }

        // Convenience function to create separate compilers for each shader stage.
        static Compiler CreatePerStageCompiler(Compiler vertexCompiler, Compiler fragmentCompiler)
        {
            return (ctx, node) => new PerStageInputDef(vertexCompiler(ctx, node), fragmentCompiler(ctx, node));
        }

        // Convenience function to create space transforms more conveniently. 
        // Returns (preMult * input * postMult) ^ (invert ? -1 : 1).
        static Compiler CreateMatrixCompiler(
            InputDef preMult, string nodeType, string outputName, InputDef postMult, bool invert)
        {
            return (ctx, node) =>
            {
                var inputDef = new InlineInputDef(nodeType, MtlxDataTypes.Matrix44, new(), outputName);

                if (preMult != null)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44, new()
                    {
                        ["in1"] = preMult,
                        ["in2"] = inputDef,
                    });
                }
                if (postMult != null)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44, new()
                    {
                        ["in1"] = inputDef,
                        ["in2"] = postMult,
                    });
                }
                if (invert)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Inverse, MtlxDataTypes.Matrix44, new()
                    {
                        ["in"] = inputDef,
                    });
                }

                return inputDef;
            };
        }

        static Compiler CreateTangentMatrixCompiler(bool invert)
        {
            return (ctx, node) =>
            {
                // Local function to create column from geometry position/vector.
                InlineInputDef CreateColumn(string nodeType, bool position = false)
                {
                    // Start with the object space geometry converted to a vector4: (x, y, z, 1)
                    var columnDef = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Vector4, new()
                    {
                        ["in"] = new InlineInputDef(nodeType, MtlxDataTypes.Vector3, new()
                        {
                            ["space"] = new StringInputDef("object"),
                        }),
                    });

                    // For vectors, clear the w component before transforming.
                    if (!position)
                    {
                        columnDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4, new()
                        {
                            ["in1"] = columnDef,
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 1.0f, 1.0f, 0.0f),
                        });
                    }

                    // Transform by the model-to-world matrix and invert the resulting z to convert to Unity space.
                    columnDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, new()
                        {
                            ["in"] = columnDef,
                            ["mat"] = new PerStageInputDef(
                                new InlineInputDef(
                                    MtlxNodeTypes.RealityKitGeometryModifierModelToWorld,
                                    MtlxDataTypes.Matrix44, new(), "modelToWorld"),
                                new InlineInputDef(
                                    MtlxNodeTypes.RealityKitSurfaceModelToWorld,
                                    MtlxDataTypes.Matrix44, new(), "modelToWorld")),
                        }),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 1.0f, -1.0f, 1.0f),
                    });

                    if (position)
                        return columnDef;
                    
                    // Normalize vectors after transformation.
                    return new InlineInputDef(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector4, new()
                    {
                        ["in"] = columnDef,
                    });
                }

                var inputDef = new InlineInputDef(MtlxNodeTypes.RealityKitCombine4, MtlxDataTypes.Matrix44, new()
                {
                    ["in1"] = CreateColumn(MtlxNodeTypes.GeomTangent),
                    ["in2"] = CreateColumn(MtlxNodeTypes.GeomBitangent),
                    ["in3"] = CreateColumn(MtlxNodeTypes.GeomNormal),
                    ["in4"] = CreateColumn(MtlxNodeTypes.GeomPosition, true),
                });

                if (invert)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Inverse, MtlxDataTypes.Matrix44, new()
                    {
                        ["in"] = inputDef,
                    });
                }

                return inputDef;
            };
        }

        static InputDef WorldSpaceViewDirectionCompiler(CompilationContext ctx, SyntaxNode node)
        {
            // Flip z coordinate to convert RealityKit space to Unity space.
            return new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.RealityKitViewDirection, MtlxDataTypes.Vector3, new()
                {
                    ["space"] = new StringInputDef("world"),
                }),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, -1.0f),
            });
        }

        static InputDef ObjectSpaceViewVectorCompiler(CompilationContext ctx, SyntaxNode node)
        {
            // Flip z coordinate to convert RealityKit space to Unity space.
            return new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.RealityKitCameraPosition, MtlxDataTypes.Vector3, new()
                    {
                        ["space"] = new StringInputDef("object"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3, new()
                    {
                        ["space"] = new StringInputDef("object"),
                    }),
                }),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, -1.0f),
            });
        }
    }
}