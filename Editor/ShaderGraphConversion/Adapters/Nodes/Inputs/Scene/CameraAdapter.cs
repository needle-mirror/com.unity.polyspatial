
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class CameraAdapter : ANodeAdapter<CameraNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            QuickNode.AddImplicitPropertyFromNode(PolySpatialShaderGlobals.WorldSpaceCameraPos, MtlxDataTypes.Vector3, node, graph, externals, "Position");
            QuickNode.AddImplicitPropertyFromNode(PolySpatialShaderGlobals.WorldSpaceCameraDir, MtlxDataTypes.Vector3, node, graph, externals, "Direction");
#else
            var positionNode = graph.AddNode(NodeUtils.GetNodeName(node, "Position"), MtlxNodeTypes.RealityKitCameraPosition, MtlxDataTypes.Vector3);
            externals.AddExternalPort(NodeUtils.GetOutputByName(node, "Position").slotReference, positionNode.name);

            var directionNode = graph.AddNode(NodeUtils.GetNodeName(node, "Direction"), MtlxNodeTypes.RealityKitViewDirection, MtlxDataTypes.Vector3);
            externals.AddExternalPort(NodeUtils.GetOutputByName(node, "Direction").slotReference, directionNode.name);
#endif

            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.OrthoParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Orthographic", MtlxDataTypes.Float, "w");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ProjectionParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Near Plane", MtlxDataTypes.Float, "y");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ProjectionParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Far Plane", MtlxDataTypes.Float, "z");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.ProjectionParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Z Buffer Sign", MtlxDataTypes.Float, "x");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.OrthoParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Width", MtlxDataTypes.Float, "x");
            QuickNode.AddImplicitPropertyFromNode(
                PolySpatialShaderGlobals.OrthoParams, MtlxDataTypes.Vector4, node,
                graph, externals, "Height", MtlxDataTypes.Float, "y");
        }
    }
}
