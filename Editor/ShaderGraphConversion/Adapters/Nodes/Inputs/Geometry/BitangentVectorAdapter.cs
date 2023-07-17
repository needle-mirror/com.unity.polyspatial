
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class BitangentAdapter : ANodeAdapter<BitangentVectorNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.GeomBitangent, node, graph, externals, "Bitangent");
            PositionAdapter.SetupSpacePort(nodeData, node);
        }
    }
}
