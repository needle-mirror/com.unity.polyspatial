
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TimeAdapter : ANodeAdapter<TimeNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.k_Time, MtlxDataTypes.Vector4, node,
                graph, externals, "Time", MtlxDataTypes.Float, "y");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.k_SinTime, MtlxDataTypes.Vector4, node,
                graph, externals, "Sine Time", MtlxDataTypes.Float, "w");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.k_CosTime, MtlxDataTypes.Vector4, node,
                graph, externals, "Cosine Time", MtlxDataTypes.Float, "w");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.k_DeltaTime, MtlxDataTypes.Vector4, node,
                graph, externals, "Delta Time", MtlxDataTypes.Float, "x");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.k_DeltaTime, MtlxDataTypes.Vector4, node,
                graph, externals, "Smooth Delta", MtlxDataTypes.Float, "z");
        }
    }
}
