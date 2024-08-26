using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Rendering.BuiltIn.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class UsdPreviewSurfaceAdapter : ISurfaceAdapter
    {
        private MaterialSlot m_DebugOutputSlot;

        public UsdPreviewSurfaceAdapter(MaterialSlot debugOutputSlot = null)
        {
            m_DebugOutputSlot = debugOutputSlot;
        }

        public void BuildContextInstance(GraphData graphData, MtlxGraphData graph, ExternalEdgeMap externals)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            UsdPreviewSurfaceUtils.BuildContextInstance(graphData, graph, externals, m_DebugOutputSlot, true);
#else
            // We disable the vertex stage iff the debug output slot connects to it.  The idea is that if you're
            // debugging the fragment stage, you probably want to be able to visualize the vertex stage's
            // interpolants (position, normal, custom interpolators, etc.)  If you're debugging the vertex stage,
            // however, we don't want to set a possibly invalid position that might make the debug output invisible
            // (and there may be issues with using the same nodes to output to both the vertex and fragment stages).
            UsdPreviewSurfaceUtils.BuildContextInstance(
                graphData, graph, externals, m_DebugOutputSlot,
                m_DebugOutputSlot != null && UnityEditor.Graphing.NodeUtils.GetEffectiveShaderStage(
                    m_DebugOutputSlot, false) == ShaderStage.Vertex);
#endif
        }

        public bool IsBlockSupported(BlockNode node)
            => UsdPreviewSurfaceUtils.HasAdapter(node);
    }


    internal class UsdPreviewSurfaceUtils
    {
        private const string GeometryModification = "GeometryModification";

        struct TargetInfo
        {
            internal bool isTransparent;
            internal bool alphaIsPremultiplied;
            internal bool alphaIsAdditive;
            internal bool alphaClipEnabled;
            internal bool isUnlit;
            internal bool enableUnlitToneMapping;
        }

        static TargetInfo GetTargetInfo(GraphData graphData)
        {
            TargetInfo targetInfo = default;
            var foundValidTarget = false;
            foreach (var target in graphData.activeTargets)
            {
                var surfaceType = "Opaque";
                var alphaMode = "Alpha";
                var subTargetName = "Lit";
                switch (target)
                {
                    case BuiltInTarget builtInTarget:
                        surfaceType = builtInTarget.surfaceType.ToString();
                        alphaMode = builtInTarget.alphaMode.ToString();
                        targetInfo.alphaClipEnabled = builtInTarget.alphaClip;
                        subTargetName = builtInTarget.activeSubTarget.displayName;
                        foundValidTarget = true;
                        break;
                    
                    case MultiJsonInternal.UnknownTargetType:
                        // This is generated when we don't have the target pipeline (URP/HDRP) installed.  Ignore.
                        break;

                    // UniversalTarget cannot be accessed directly due to its protection level.  Instead, we use
                    // reflection to access its properties (identical to those in BuiltInTarget).
                    default:
                        var type = target.GetType();
                        if (type.FullName == "UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget")
                        {
                            surfaceType = type.GetProperty("surfaceType").GetValue(target).ToString();
                            alphaMode = type.GetProperty("alphaMode").GetValue(target).ToString();
                            targetInfo.alphaClipEnabled = (bool)type.GetProperty("alphaClip").GetValue(target);
                            var subTarget = (SubTarget)type.GetProperty("activeSubTarget").GetValue(target);
                            subTargetName = subTarget.displayName;
                            foundValidTarget = true;
                        }
                        else
                        {
                            MtlxPostProcessor.LogWarningForGraph(graphData, $"Unsupported target type: {type}");
                        }
                        break;
                }
                switch (surfaceType)
                {
                    case "Opaque":
                        break;
                    
                    case "Transparent":
                        targetInfo.isTransparent = true;
                        break;
                    
                    default:
                        MtlxPostProcessor.LogWarningForGraph(graphData, $"Unsupported surface type: {surfaceType}");
                        break;
                }
                if (targetInfo.isTransparent)
                {
                    switch (alphaMode)
                    {
                        case "Alpha":
                            break;
                        
                        case "Premultiply":
                            targetInfo.alphaIsPremultiplied = true;
                            break;

                        case "Additive":
                            targetInfo.alphaIsAdditive = true;
                            break;

                        default:
                            MtlxPostProcessor.LogWarningForGraph(graphData, $"Unsupported alpha mode: {alphaMode}");
                            break;
                    }
                }

                switch (subTargetName)
                {
                    case "Lit":
                        break;

                    case "Unlit" or "Sprite Unlit":
                        targetInfo.isUnlit = true;
                        break;
                    
                    default:
                        MtlxPostProcessor.LogWarningForGraph(graphData, $"Unsupported subtarget: {subTargetName}");
                        break;
                }
            }
            if (!foundValidTarget)
                MtlxPostProcessor.LogWarningForGraph(graphData, "No supported (built-in/URP) target found.");

            foreach (var subData in graphData.SubDatas)
            {
                if (subData is MtlxDataExtension mtlxData)
                    targetInfo.enableUnlitToneMapping = mtlxData.EnableUnlitToneMapping;
            }
            return targetInfo;
        }

        internal static void BuildContextInstance(
            GraphData graphData, MtlxGraphData graph, ExternalEdgeMap externals,
            MaterialSlot debugOutputSlot, bool ignoreVertexStage = false)
        {
            Init();

            // Debug output is always unlit/opaque.
            var targetInfo = (debugOutputSlot == null) ? GetTargetInfo(graphData) : new() { isUnlit = true };
            
#if DISABLE_MATERIALX_EXTENSIONS
            var fragmentNodeType = MtlxNodeTypes.UsdPreviewSurface;
#else
            var fragmentNodeType = targetInfo.isUnlit ? MtlxNodeTypes.RealityKitUnlit : MtlxNodeTypes.RealityKitPbr;
#endif

            // setup the surface shader node.
            var fragmentNodeName = "SR_" + graph.name;
            var vertexNodeName = fragmentNodeName + "Vertex";
            var materialNodeName = "USD_" + graph.name;
            var vertexNode = ignoreVertexStage ? null : graph.AddNode(vertexNodeName, GeometryModification, MtlxDataTypes.Vertex);
            var fragmentNode = graph.AddNode(fragmentNodeName, fragmentNodeType, MtlxDataTypes.Surface);

            // Disable tone mapping for unlit surfaces to match Unity's behavior.
            if (fragmentNodeType == MtlxNodeTypes.RealityKitUnlit)
            {
                fragmentNode.AddPortValue(
                    "applyPostProcessToneMap", MtlxDataTypes.Boolean,
                    new[] { targetInfo.enableUnlitToneMapping ? 1.0f : 0.0f });
            }

            // Premultiplied and additive modes use premultiplied alpha.
            if (fragmentNodeType != MtlxNodeTypes.UsdPreviewSurface &&
                (targetInfo.alphaIsPremultiplied || targetInfo.alphaIsAdditive))
            {
                fragmentNode.AddPortValue("hasPremultipliedAlpha", MtlxDataTypes.Boolean, new[] { 1.0f });
            }

            // initialize the material node boiler plate.
            graph.AddNode(materialNodeName, MtlxNodeTypes.Material, MtlxDataTypes.Material);
            if (!ignoreVertexStage)
                graph.AddPortAndEdge(vertexNodeName, materialNodeName, "vertexshader", MtlxDataTypes.Vertex);
            graph.AddPortAndEdge(fragmentNodeName, materialNodeName, "surfaceshader", MtlxDataTypes.Surface);

            // each relevant block should map to a surfaceshader input in UsdPreviewSurface.
            var blocks = graphData.GetNodes<BlockNode>();
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

                if (fragmentNodeType == MtlxNodeTypes.RealityKitUnlit)
                {
                    // As a special case, convert the "baseColor" mapping to RealityKitUnlit's "color".
                    // TODO (LXR-2242): Refactor the way we handle these mappings and rules, since they're full
                    // of special cases like this.
                    if (portName == "baseColor")
                        portName = "color";
                    
                    // For Unlit surfaces, ignore anything that isn't in the input map (we don't need to do this
                    // for Lit surfaces, since all Unlit inputs are also Lit inputs)
                    if (shaderNode == fragmentNode && !unlitSurfaceInputs.ContainsKey(portName))
                        continue;
                }

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

                            // Flip the Z coordinate to convert from Unity to RealityKit space.
                            externalNodeName = $"{vertexNodeName}_{portName}_FlipZ";
                            externalPortName = "in1";
                            externalNode = graph.AddNode(externalNodeName, "multiply", portType);
                            externalNode.AddPortValue("in2", portType, new[] { 1.0f, 1.0f, -1.0f });

                            // Subtract the model space position to get the offset.
                            var subtractNodeName = $"{vertexNodeName}_{portName}_SubtractPosition";
                            graph.AddNode(subtractNodeName, "subtract", portType);
                            graph.AddPortAndEdge(externalNodeName, subtractNodeName, "in1", portType);
                            graph.AddPortAndEdge(subtractNodeName, vertexNodeName, portName, portType);

                            var positionNodeName = $"{vertexNodeName}_{portName}_Position";
                            var positionNode = graph.AddNode(positionNodeName, MtlxNodeTypes.GeomPosition, portType);
                            positionNode.AddPortString("space", MtlxDataTypes.String, "object");
                            graph.AddPortAndEdge(positionNodeName, subtractNodeName, "in2", portType);
                            break;
                        
                        case SpecialRules.FlipZ:
                            if (ignoreVertexStage  || srcSlot == null)
                                continue;
                            
                            portType = MtlxDataTypes.Vector3;
                            ignoreIfNotConnected = true;

                            // Flip the Z coordinate to convert from Unity to RealityKit space.
                            externalNodeName = $"{vertexNodeName}_{portName}_FlipZ";
                            externalPortName = "in1";
                            externalNode = graph.AddNode(externalNodeName, "multiply", portType);
                            graph.AddPortAndEdge(externalNodeName, vertexNodeName, portName, portType);
                            externalNode.AddPortValue("in2", portType, new[] { 1.0f, 1.0f, -1.0f });
                            break;

                        case SpecialRules.VertexStage:
                            if (ignoreVertexStage)
                                continue;

                            if (!typeMap.TryGetValue(block.descriptor.name, out portType))
                                portType = MtlxDataTypes.Vector3;
                            break;
                        
                        case SpecialRules.AdditiveColor:
                            // If we have a debug output slot, that slot becomes the color source.
                            if (debugOutputSlot != null)
                            {
                                if (debugOutputSlot.isInputSlot)
                                {
                                    srcSlot = SlotUtils.GetSourceConnectionSlot(debugOutputSlot);
                                    floatValue = SlotUtils.GetDefaultValue(debugOutputSlot);
                                }
                                else
                                {
                                    srcSlot = debugOutputSlot;
                                }
                            }

                            if (targetInfo.alphaIsAdditive)
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
                            if (!(targetInfo.isTransparent || targetInfo.alphaClipEnabled))
                            {
                                // If opaque and alpha clipping is disabled, omit opacity entirely.
                                srcSlot = null;
                                ignoreIfNotConnected = true;
                            }
                            else if (targetInfo.alphaIsAdditive)
                            {
                                // If alpha is additive, opacity should be zero (because the
                                // destination color will be multiplied by (1 - alpha)).
                                srcSlot = null;
                                floatValue = new[] { 0.0f };
                            }
                            break;
                        
                        case SpecialRules.EnableAlphaClip:
                            if (!targetInfo.alphaClipEnabled)
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
                    // effect on visionOS.  Similarly, we omit opacity if the surface isn't transparent/alpha-clipped;
                    // including it causes the object to be sorted as transparent.
                    if (!(defaultPorts.name == "opacityThreshold" && !targetInfo.alphaClipEnabled ||
                        defaultPorts.name == "opacity" && !(targetInfo.isTransparent || targetInfo.alphaClipEnabled)))
                    {
                        fragmentNode.AddPortValue(defaultPorts.name, defaultPorts.datatype, defaultPorts.value);
                    }
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
            FlipZ,
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
            rulesMap.Add("Normal", SpecialRules.FlipZ);
            blockMap.Add("Tangent", "bitangent");
            rulesMap.Add("Tangent", SpecialRules.FlipZ);

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
            // Color values are (0.5, 0.5, 0.5) converted to linear (raised to the power of 2.2)
            SetupLitInput("diffuseColor",         MtlxDataTypes.Color3,   new float[] { .218f, .218f, .218f });
            SetupLitInput("specularColor",        MtlxDataTypes.Color3,   new float[] { .218f, .218f, .218f });
            SetupLitInput("useSpecularWorkflow",  MtlxDataTypes.Integer,  new float[] { 0 });
            SetupLitInput("ior",                  MtlxDataTypes.Float,    new float[] { 1.4f });
            SetupLitInput("occlusion",            MtlxDataTypes.Float,    new float[] { 1 });
#else
            SetupLitInput("baseColor",            MtlxDataTypes.Color3,   new float[] { .218f, .218f, .218f });
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
            
            SetupUnlitInput("color",              MtlxDataTypes.Color3,   new float[] { .218f, .218f, .218f });
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
