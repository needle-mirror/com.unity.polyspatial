
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SaturationAdapter : ANodeAdapter<SaturationNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("In", "in");
            portMap.Add("Saturation", "amount");

            var typeMap = new Dictionary<string, string>();
            typeMap.Add("In", MtlxDataTypes.Color3);
            typeMap.Add("Saturation", MtlxDataTypes.Float);

            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Saturate, node, graph, externals, "Saturation", portMap, typeMap, MtlxDataTypes.Color3);
            nodeData.AddPortValue("lumacoeffs", MtlxDataTypes.Color3, new float[] { 0.2126729f, 0.7151522f, 0.0721750f });
        }
    }
}
