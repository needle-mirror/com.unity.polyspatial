
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ColorspaceConversionAdapter : ANodeAdapter<ColorspaceConversionNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is ColorspaceConversionNode cnode)
            {
                var from = cnode.conversion.from;
                var to = cnode.conversion.to;

                if (from == Colorspace.HSV && to == Colorspace.RGB)
                {
                    QuickNode.UnaryOp(MtlxNodeTypes.HsvToRgb, node, graph, externals, $"ColorSpace{from}{to}", coerceType: MtlxDataTypes.Color3);
                }
                else if (from == Colorspace.RGB && to == Colorspace.HSV)
                {
                    QuickNode.UnaryOp(MtlxNodeTypes.RgbToHsv, node, graph, externals, $"ColorSpace{from}{to}", coerceType: MtlxDataTypes.Color3);
                }
                else
                {
                    // TODO: I think there is a way to indicate we want linear colorspace in a convert or dot node,
                    // but overall, the handling of colors between USG and mtlx is not going to be consistent.
                    // some really thorough testing/tweaking needs to be done to get things consistent.
                    QuickNode.UnaryOp(MtlxNodeTypes.Dot, node, graph, externals, $"ColorSpace{from}{to}", coerceType: MtlxDataTypes.Color3);
                }
            }
        }
    }
}
