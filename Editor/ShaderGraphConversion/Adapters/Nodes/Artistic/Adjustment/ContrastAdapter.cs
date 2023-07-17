
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ContrastAdapter : ANodeAdapter<ContrastNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var portMap = new Dictionary<string, string>();
            portMap.Add("In", "in");
            portMap.Add("Contrast", "amount");

            var typeMap = new Dictionary<string, string>();
            typeMap.Add("In", MtlxDataTypes.Color3);
            typeMap.Add("Contrast", MtlxDataTypes.Float);

            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Contrast, node, graph, externals, "Contrast", portMap, typeMap, MtlxDataTypes.Color3);
            nodeData.AddPortValue("pivot", MtlxDataTypes.Float, new float[] { 0.21763764082403103478406750436994f });
        }
    }
}
