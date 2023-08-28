using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class UsdPreviewSurfaceAdapter : ISurfaceAdapter
    {
        public void BuildContextInstance(GraphData graphData, MtlxGraphData graph, ExternalEdgeMap externals)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            UsdPreviewSurfaceUtils.BuildContextInstance(graphData, graph, externals, true);
#else
            UsdPreviewSurfaceUtils.BuildContextInstance(graphData, graph, externals, false);
#endif
        }

        public bool IsBlockSupported(BlockNode node)
            => UsdPreviewSurfaceUtils.HasAdapter(node);
    }


    internal class UsdPreviewSurfaceUtils
    {
        private const string GeometryModification = "GeometryModification";

        internal static void BuildContextInstance(GraphData graphData, MtlxGraphData graph, ExternalEdgeMap externals, bool ignoreVertexStage = false)
        {
            Init();

            var blocks = graphData.GetNodes<BlockNode>();

#if DISABLE_MATERIALX_EXTENSIONS
            var fragmentNodeType = MtlxNodeTypes.UsdPreviewSurface;
#else
            // Use the presence of lighting parameter blocks as an indicator of whether the material is lit.
            var fragmentNodeType = MtlxNodeTypes.RealityKitUnlit;
            foreach (var block in blocks)
            {
                if (block.descriptor.shaderStage == ShaderStage.Fragment &&
                    block.descriptor.name != "BaseColor" &&
                    block.descriptor.name != "Alpha" &&
                    block.descriptor.name != "AlphaClipThreshold")
                {
                    fragmentNodeType = MtlxNodeTypes.RealityKitPbr;
                    break;
                }
            }
#endif

            // setup the surface shader node.
            var fragmentNodeName = "SR_" + graph.name;
            var vertexNodeName = fragmentNodeName + "Vertex";
            var materialNodeName = "USD_" + graph.name;
            var vertexNode = ignoreVertexStage ? null : graph.AddNode(vertexNodeName, GeometryModification, MtlxDataTypes.Vertex);
            var fragmentNode = graph.AddNode(fragmentNodeName, fragmentNodeType, MtlxDataTypes.Surface);

            // Disable tone mapping for unlit surfaces to match Unity's behavior.
            if (fragmentNodeType == MtlxNodeTypes.RealityKitUnlit)
                fragmentNode.AddPortValue("applyPostProcessToneMap", MtlxDataTypes.Boolean, new[] { 0.0f });

            var isTransparent = false;
            var alphaIsAdditive = false;
            var alphaClipEnabled = false;
            foreach (var target in graphData.activeTargets)
            {
                var surfaceType = "Opaque";
                var alphaMode = "Alpha";
                switch (target)
                {
                    case BuiltInTarget builtInTarget:
                        surfaceType = builtInTarget.surfaceType.ToString();
                        alphaMode = builtInTarget.alphaMode.ToString();
                        alphaClipEnabled = builtInTarget.alphaClip;
                        break;
                    
                    // UniversalTarget cannot be accessed directly due to its protection level.  Instead, we use
                    // reflection to access its properties (identical to those in BuiltInTarget).
                    default:
                        var type = target.GetType();
                        if (type.FullName == "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget")
                        {
                            surfaceType = type.GetProperty("surfaceType").GetValue(target).ToString();
                            alphaMode = type.GetProperty("alphaMode").GetValue(target).ToString();
                            alphaClipEnabled = (bool)type.GetProperty("alphaClip").GetValue(target);
                        }
                        else
                        {
                            Debug.LogWarning($"Unsupported target type: {type}");
                        }
                        break;
                }
                switch (surfaceType)
                {
                    case "Opaque":
                        break;
                    
                    case "Transparent":
                        isTransparent = true;
                        break;
                    
                    default:
                        Debug.LogWarning($"Unsupported surface type: {surfaceType}");
                        break;
                }
                if (isTransparent && fragmentNodeType != MtlxNodeTypes.UsdPreviewSurface)
                {
                    switch (alphaMode)
                    {
                        case "Alpha":
                            break;
                        
                        case "Premultiply":
                            fragmentNode.AddPortValue("hasPremultipliedAlpha", MtlxDataTypes.Boolean, new[] { 1.0f });
                            break;

                        case "Additive":
                            fragmentNode.AddPortValue("hasPremultipliedAlpha", MtlxDataTypes.Boolean, new[] { 1.0f });
                            alphaIsAdditive = true;
                            break;

                        default:
                            Debug.LogWarning($"Unsupported alpha mode: {alphaMode}");
                            break;
                    }
                }
            }

            // initialize the material node boiler plate.
            graph.AddNode(materialNodeName, MtlxNodeTypes.Material, MtlxDataTypes.Material);
            if (!ignoreVertexStage)
                graph.AddPortAndEdge(vertexNodeName, materialNodeName, "vertexshader", MtlxDataTypes.Vertex);
            graph.AddPortAndEdge(fragmentNodeName, materialNodeName, "surfaceshader", MtlxDataTypes.Surface);

            // each relevant block should map to a surfaceshader input in UsdPreviewSurface.
            foreach (var block in blocks)
            {
                if (!HasAdapter(block))
                    continue;

                var shaderNode = block.descriptor.shaderStage switch
                {
                    ShaderStage.Fragment => fragmentNode,
                    ShaderStage.Vertex => vertexNode,
                    _ => throw new NotSupportedException($"Unsupported shader stage {block.descriptor.shaderStage}")
                };
                if (shaderNode == null)
                    continue; // Ignore vertex stage if unsupported.

                var thisSlot    = block.FindInputSlot<MaterialSlot>(0);
                var srcSlot     = SlotUtils.GetSourceConnectionSlot(thisSlot);
                var portType    = SlotUtils.GetDataTypeName(thisSlot);
                var fileValue   = SlotUtils.GetDefaultFilename(thisSlot);
                var floatValue  = SlotUtils.GetDefaultValue(thisSlot);
                var portName    = blockMap[block.descriptor.name];

                string externalNodeName = shaderNode.name;
                string externalPortName = portName;
                var externalNode = shaderNode;
                bool ignoreIfNotConnected = false;

                if (rulesMap.TryGetValue(block.descriptor.name, out SpecialRules rule))
                {
                    switch(rule)
                    {
                        // This special case adds a new port for specular workflow activation, but doesn't otherwise
                        // impact how the blockNode would be processed.
                        case SpecialRules.EnablesSpecularWorkflow:
                            if (block.owner.GetActiveBlocksForAllActiveTargets().Contains(block.descriptor))
                                shaderNode.AddPortValue("useSpecularWorkflow", MtlxDataTypes.Integer, new float[] { 1 });
                            break;

                        // Create a "dotproduct" node to convert to grayscale for RealityKit specular level.
                        case SpecialRules.SpecularGrayscale:
                            externalNodeName = $"{shaderNode.name}_{portName}_Grayscale";
                            externalPortName = "in2";
                            externalNode = graph.AddNode(
                                externalNodeName, MtlxNodeTypes.DotProduct, MtlxDataTypes.Float);
                            
                            // Convert specular color to grayscale according to the Unity reference conversion:
                            // https://docs.unity3d.com/ScriptReference/Color-grayscale.html
                            portType = MtlxDataTypes.Vector3;
                            externalNode.AddPortValue("in1", portType, new[] { 0.299f, 0.587f, 0.114f });
                            graph.AddEdge(externalNodeName, shaderNode.name, portName);
                            break;

                        // we create a 'subtract' node to perform the one's complement,
                        // "in2" of this node now needs to be responsible for accepting external edges.
                        case SpecialRules.OnesComplement:

                            // A new subtract node becomes associated with the edge, as does the second input.
                            externalNodeName = $"{shaderNode.name}_{portName}_OnesComplement";
                            externalPortName = "in2";
                            externalNode = graph.AddNode(externalNodeName, "subtract", portType);

                            externalNode.AddPortValue("in1", portType, new float[3] { 1.0f, 1.0f, 1.0f });
                            graph.AddEdge(externalNodeName, shaderNode.name, portName);
                            break;

                        case SpecialRules.DefaultIsTangent:
                            // Actually-- we should not be adding a node if this is disconnected.
                            portType = MtlxDataTypes.Vector3;
                            ignoreIfNotConnected = true;
                            break;

                        case SpecialRules.SubtractPosition:
                            if (ignoreVertexStage || srcSlot == null)
                                continue;

                            portType = MtlxDataTypes.Vector3;
                            ignoreIfNotConnected = true;

                            // Subtract the model space position to get the offset.
                            externalNodeName = $"{vertexNodeName}_{portName}_SubtractPosition";
                            externalPortName = "in1";
                            externalNode = graph.AddNode(externalNodeName, "subtract", portType);
                            graph.AddPortAndEdge(externalNodeName, vertexNodeName, portName, portType);

                            var positionNodeName = $"{vertexNodeName}_{portName}_Position";
                            var positionNode = graph.AddNode(
                                positionNodeName, MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3);
                            positionNode.AddPortString("space", MtlxDataTypes.String, "object");
                            graph.AddPortAndEdge(positionNodeName, externalNodeName, "in2", portType);

                            break;
                        
                        case SpecialRules.VertexStage:
                            if (ignoreVertexStage)
                                continue;

                            if (!typeMap.TryGetValue(block.descriptor.name, out portType))
                                portType = MtlxDataTypes.Vector3;

                            ignoreIfNotConnected = true;
                            break;
                        
                        case SpecialRules.AdditiveColor:
                            if (fragmentNodeType == MtlxNodeTypes.RealityKitUnlit)
                                externalPortName = portName = "color";

                            if (alphaIsAdditive)
                            {
                                // If alpha is additive, scale the color by what would be the alpha.
                                externalNodeName = $"{fragmentNodeName}_{portName}_MultiplyAlpha";
                                externalPortName = "in1";
                                externalNode = graph.AddNode(
                                    externalNodeName, MtlxNodeTypes.Multiply, MtlxDataTypes.Color3);
                                graph.AddPortAndEdge(
                                    externalNodeName, fragmentNodeName, portName, MtlxDataTypes.Color3);

                                var alphaBlock = blocks.FirstOrDefault(block => block.descriptor.name == "Alpha");
                                if (alphaBlock != null)
                                {
                                    QuickNode.AddInputPortAndEdge(
                                        externals, externalNode, alphaBlock.FindInputSlot<MaterialSlot>(0),
                                        "in2", MtlxDataTypes.Float);
                                }
                            }
                            break;
                        
                        case SpecialRules.AdditiveAlpha:
                            if (!isTransparent)
                            {
                                // If opaque, omit opacity entirely.
                                srcSlot = null;
                                ignoreIfNotConnected = true;
                            }
                            else if (alphaIsAdditive)
                            {
                                // If alpha is additive, opacity should be zero (because the
                                // destination color will be multiplied by (1 - alpha)).
                                srcSlot = null;
                                floatValue = new[] { 0.0f };
                            }
                            break;
                        
                        case SpecialRules.EnableAlphaClip:
                            if (!alphaClipEnabled)
                            {
                                // If alpha clip is not enabled, omit opacity threshold entirely.
                                srcSlot = null;
                                ignoreIfNotConnected = true;
                            }
                            break;
                    }
                }


                if (srcSlot != null && AdapterMap.IsNodeSupported(srcSlot.owner))
                {
                    externalNode.AddPort(externalPortName, portType);
                    externals.AddExternalPort(thisSlot.slotReference, externalNodeName, externalPortName);
                    externals.AddExternalEdge(srcSlot.slotReference, thisSlot.slotReference);
                }
                else if (srcSlot == null && portType == MtlxDataTypes.Filename && !ignoreIfNotConnected)
                {
                    externalNode.AddPortString(externalPortName, portType, fileValue);
                }
                else if (srcSlot == null && !ignoreIfNotConnected)
                {
                    externalNode.AddPortValue(externalPortName, portType, floatValue);
                }
            }

            // We then need to setup Unity defaults to override UsdPreviewSurface defaults for any input not used.
            var surfaceInputs = (fragmentNodeType == MtlxNodeTypes.RealityKitUnlit) ?
                unlitSurfaceInputs : litSurfaceInputs;
            foreach (var defaultPorts in surfaceInputs.Values)
            {
                if (!fragmentNode.HasPort(defaultPorts.name))
                {
                    // As a special case, if alpha clipping is disabled, we need to omit opacityThreshold entirely.
                    // Including it in any form causes the surface to be treated as opaque, causing a "cut-out"
                    // effect on visionOS.
                    if (defaultPorts.name != "opacityThreshold" || alphaClipEnabled)
                        fragmentNode.AddPortValue(defaultPorts.name, defaultPorts.datatype, defaultPorts.value);
                }
            }
        }

        internal static bool HasAdapter(BlockNode node)
        {
            Init();
            return blockMap.ContainsKey(node.descriptor.name);
        }


        #region PrivateStatic
        private static void Init()
        {
            if (isInit)
                return;

            InitBlockMap();
            InitSurfaceDescription();
            isInit = true;
        }

        private static Dictionary<string, string> blockMap = new();
        private static Dictionary<string, SpecialRules> rulesMap = new();
        private static Dictionary<string, string> typeMap = new();
        private static Dictionary<string, MtlxPortData> litSurfaceInputs = new();
        private static Dictionary<string, MtlxPortData> unlitSurfaceInputs = new();
        private static bool isInit = false;

        private enum SpecialRules
        {
            OnesComplement,
            DefaultIsTangent,
            WorldToTangent,
            ObjectToTangent,
            EnablesSpecularWorkflow,
            SpecularGrayscale,
            SubtractPosition,
            VertexStage,
            AdditiveColor,
            AdditiveAlpha,
            EnableAlphaClip,
        }

        private static void InitBlockMap()
        {
            // TODO: I think unity rebalances inputs to avoid blowing out the color range,
            // this may be a platform dependent deviation that we can't really control for though.
            blockMap.Add("Position", "modelPositionOffset");
            rulesMap.Add("Position", SpecialRules.SubtractPosition);
            blockMap.Add("Normal", "normal");
            rulesMap.Add("Normal", SpecialRules.VertexStage);
            blockMap.Add("Tangent", "bitangent");
            rulesMap.Add("Tangent", SpecialRules.VertexStage);

            blockMap.Add("Color", "color");
            rulesMap.Add("Color", SpecialRules.VertexStage);
            typeMap.Add("Color", MtlxDataTypes.Color4);
            blockMap.Add("UV0", "uv0");
            rulesMap.Add("UV0", SpecialRules.VertexStage);
            typeMap.Add("UV0", MtlxDataTypes.Vector2);
            blockMap.Add("UV1", "uv1");
            rulesMap.Add("UV1", SpecialRules.VertexStage);
            typeMap.Add("UV1", MtlxDataTypes.Vector2);
            blockMap.Add("UserAttribute", "userAttribute");
            rulesMap.Add("UserAttribute", SpecialRules.VertexStage);
            typeMap.Add("UserAttribute", MtlxDataTypes.Vector4);

#if DISABLE_MATERIALX_EXTENSIONS
            blockMap.Add("BaseColor", "diffuseColor");
            blockMap.Add("Specular", "specularColor");
            rulesMap.Add("Specular", SpecialRules.EnablesSpecularWorkflow);
            blockMap.Add("Occlusion", "occlusion");
            blockMap.Add("IOR", "ior");
            blockMap.Add("RefractionIndex", "ior");
            blockMap.Add("TessellationDisplacement", "displacement");
#else
            blockMap.Add("BaseColor", "baseColor");
            blockMap.Add("Specular", "specular");
            rulesMap.Add("Specular", SpecialRules.SpecularGrayscale);
            blockMap.Add("Occlusion", "ambientOcclusion");

#endif
            rulesMap.Add("BaseColor", SpecialRules.AdditiveColor);
            blockMap.Add("Emission", "emissiveColor");
            blockMap.Add("Metallic", "metallic");
            blockMap.Add("NormalTS", "normal");
            rulesMap.Add("NormalTS", SpecialRules.DefaultIsTangent);
            blockMap.Add("NormalWS", "normal");
            rulesMap.Add("NormalWS", SpecialRules.WorldToTangent);
            blockMap.Add("NormalOS", "normal");
            rulesMap.Add("NormalOS", SpecialRules.ObjectToTangent);
            blockMap.Add("Smoothness", "roughness");
            rulesMap.Add("Smoothness", SpecialRules.OnesComplement);
            
            blockMap.Add("Alpha", "opacity");
            rulesMap.Add("Alpha", SpecialRules.AdditiveAlpha);
            blockMap.Add("AlphaClipThreshold", "opacityThreshold");
            rulesMap.Add("AlphaClipThreshold", SpecialRules.EnableAlphaClip);
            blockMap.Add("CoatMask", "clearcoat");
            blockMap.Add("CoatSmoothness", "clearcoatRoughness");
            rulesMap.Add("CoatSmoothness", SpecialRules.OnesComplement);
        }

        private static void InitSurfaceDescription()
        {
#if DISABLE_MATERIALX_EXTENSIONS
            SetupLitInput("diffuseColor",         MtlxDataTypes.Color3,   new float[] { .5f, .5f, .5f });
            SetupLitInput("specularColor",        MtlxDataTypes.Color3,   new float[] { .5f, .5f, .5f });
            SetupLitInput("useSpecularWorkflow",  MtlxDataTypes.Integer,  new float[] { 0 });
            SetupLitInput("ior",                  MtlxDataTypes.Float,    new float[] { 1.4f });
            SetupLitInput("occlusion",            MtlxDataTypes.Float,    new float[] { 1 });
#else
            SetupLitInput("baseColor",            MtlxDataTypes.Color3,   new float[] { .5f, .5f, .5f });
            SetupLitInput("specular",             MtlxDataTypes.Float,    new float[] { .5f });
            SetupLitInput("ambientOcclusion",     MtlxDataTypes.Float,    new float[] { 1 });
#endif
            SetupLitInput("emissiveColor",        MtlxDataTypes.Color3,   new float[] { 0, 0, 0 });
            SetupLitInput("metallic",             MtlxDataTypes.Float,    new float[] { 0 });
            SetupLitInput("roughness",            MtlxDataTypes.Float,    new float[] { 0.5f });
            SetupLitInput("clearcoat",            MtlxDataTypes.Float,    new float[] { 0 });
            SetupLitInput("clearcoatRoughness",   MtlxDataTypes.Float,    new float[] { 0.01f });
            SetupLitInput("opacity",              MtlxDataTypes.Float,    new float[] { 1 });
            SetupLitInput("opacityThreshold",     MtlxDataTypes.Float,    new float[] { 0 });
            
            SetupUnlitInput("color",              MtlxDataTypes.Color3,   new float[] { .5f, .5f, .5f });
            SetupUnlitInput("opacity",            MtlxDataTypes.Float,    new float[] { 1 });
            SetupUnlitInput("opacityThreshold",   MtlxDataTypes.Float,    new float[] { 0 });
        }

        private static void SetupLitInput(string name, string type, float[] value)
            => litSurfaceInputs.Add(name, new MtlxPortData(name, type, value));
        
        private static void SetupUnlitInput(string name, string type, float[] value)
            => unlitSurfaceInputs.Add(name, new MtlxPortData(name, type, value));
        #endregion
    }
}
