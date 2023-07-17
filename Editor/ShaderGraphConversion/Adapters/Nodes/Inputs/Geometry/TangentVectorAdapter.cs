
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TangentAdapter : ANodeAdapter<TangentVectorNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.GeomTangent, node, graph, externals, "Tangent");
            PositionAdapter.SetupSpacePort(nodeData, node);
        }
    }
}
