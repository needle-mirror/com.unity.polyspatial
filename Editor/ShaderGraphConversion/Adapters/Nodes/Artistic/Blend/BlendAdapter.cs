
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class BlendAdapter : ANodeAdapter<BlendNode>
    {
        static string BlendModeToMtlxNodeType(BlendNode node)
            => node.blendMode switch
            {
                // Blend
                BlendMode.Difference => "difference", // Wrong--
                BlendMode.Subtract => "minus",
                BlendMode.Burn => "burn",
                BlendMode.Dodge => "dodge",
                BlendMode.LinearDodge => "plus",
                BlendMode.Overlay => "overlay",
                BlendMode.Screen => "screen",

                BlendMode.Overwrite => "mix",
                BlendMode.Negation => "screen",
                BlendMode.Multiply => "in",

                // last resort-- most of the others don't have sane mappings, but could be expanded into many nodes.
                // Note that it's probably less desirable to use mix than something else.
                _ => "mix"
            };


        public override string SupportDetails(AbstractMaterialNode node)
        {
            if (node is BlendNode bnode)
            {
                if (BlendModeToMtlxNodeType(bnode) == "mix" && bnode.blendMode != BlendMode.Overwrite)
                {
                    return $"'{bnode.blendMode}' is not supported, defaulting to lerp/mix.";
                }
            }
            return "";
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is BlendNode bnode)
            {
                var blendMode = BlendModeToMtlxNodeType(bnode);
                var portMap = new Dictionary<string, string>();
                portMap.Add("Blend", "fg");
                portMap.Add("Base", "bg");
                portMap.Add("Opacity", "mix");

                var typeOverMap = new Dictionary<string, string>();
                typeOverMap.Add("Blend", MtlxDataTypes.Color3);
                typeOverMap.Add("Base", MtlxDataTypes.Color3);
                typeOverMap.Add("Opacity", MtlxDataTypes.Float);

                var nodeData = QuickNode.NaryOp(blendMode, node, graph, externals, "Blend", portMap, typeOverMap, MtlxDataTypes.Color3);
            }
        }
    }
}
