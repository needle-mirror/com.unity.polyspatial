
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class NormalAdapter : ANodeAdapter<NormalVectorNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.GeomNormal, node, graph, externals, "Normal");
            PositionAdapter.SetupSpacePort(nodeData, node);
        }
    }
}
