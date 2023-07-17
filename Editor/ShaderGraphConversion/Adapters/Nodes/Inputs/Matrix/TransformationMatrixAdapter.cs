
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TransformationMatrixAdapter : ANodeAdapter<TransformationMatrixNode>
    {
        const string k_ViewProjectionNodeName = "TransformViewProjection";
        const string k_ViewProjectionInverseNodeName = "TransformViewProjectionInverse";

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is TransformationMatrixNode tnode)
            {
                switch (tnode.matrixType)
                {
                    case UnityMatrixType.Model:
                        AddTransformNode(
                            node, graph, externals, MtlxImplicitProperties.TransformModel,
                            MtlxNodeTypes.RealityKitSurfaceModelToWorld, "modelToWorld", "TransformModel");
                        break;
                    case UnityMatrixType.View:
                        AddTransformNode(
                            node, graph, externals, PolySpatialShaderGlobals.ViewMatrix,
                            MtlxNodeTypes.RealityKitSurfaceWorldToView, "worldToView", "TransformView");
                        break;
                    case UnityMatrixType.Projection:
                        AddTransformNode(
                            node, graph, externals, PolySpatialShaderGlobals.ProjectionMatrix,
                            MtlxNodeTypes.RealityKitSurfaceViewToProjection, "viewToProjection", "TransformProjection");
                        break;

                    case UnityMatrixType.InverseModel:
                        AddInverseTransformNode(
                            node, graph, externals, MtlxImplicitProperties.TransformModel,
                            MtlxNodeTypes.RealityKitSurfaceModelToWorld, "modelToWorld", "TransformModelInverse");
                        break;
                    case UnityMatrixType.InverseView:
                        AddInverseTransformNode(
                            node, graph, externals, PolySpatialShaderGlobals.ViewMatrix,
                            MtlxNodeTypes.RealityKitSurfaceWorldToView, "worldToView", "TransformViewInverse");
                        break;
                    case UnityMatrixType.InverseProjection:
#if DISABLE_MATERIALX_EXTENSIONS
                        AddInverseTransformNode(
                            node, graph, externals, PolySpatialShaderGlobals.ProjectionMatrix,
                            MtlxNodeTypes.RealityKitSurfaceViewToProjection, "viewToProjection",
                            "TransformProjectionInverse");
#else
                        var nodeData = QuickNode.NaryOp(
                            MtlxNodeTypes.RealityKitSurfaceProjectionToView, node, graph, externals,
                            "TransformProjectionInverse");
                        nodeData.outputName = "projectionToView";
#endif
                        break;

                    case UnityMatrixType.ViewProjection:
                        EnsureViewProjectionNodeExists(node, graph, externals);
                        externals.AddExternalPort(
                            NodeUtils.GetPrimaryOutput(node).slotReference, k_ViewProjectionNodeName);
                        break;

                    case UnityMatrixType.InverseViewProjection:
                        if (!graph.HasNode(k_ViewProjectionInverseNodeName))
                        {
                            EnsureViewProjectionNodeExists(node, graph, externals);

                            graph.AddNode(
                                k_ViewProjectionInverseNodeName, MtlxNodeTypes.Inverse, MtlxDataTypes.Matrix44);
                            graph.AddPortAndEdge(
                                k_ViewProjectionNodeName, k_ViewProjectionInverseNodeName,
                                "in", MtlxDataTypes.Matrix44);
                        }
                        externals.AddExternalPort(
                            NodeUtils.GetPrimaryOutput(node).slotReference, k_ViewProjectionInverseNodeName);
                        break;
                }
            }
        }

        void AddTransformNode(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, string implicitProperty,
            string extensionNode, string extensionOutputName, string outNodeNameHint)
        {

#if DISABLE_MATERIALX_EXTENSIONS
            QuickNode.AddImplicitPropertyFromNode(
                implicitProperty, MtlxDataTypes.Matrix44, node, graph, externals, "Out");
#else
            var nodeData = QuickNode.NaryOp(extensionNode, node, graph, externals, outNodeNameHint);
            nodeData.outputName = extensionOutputName;
#endif
        }

        void AddInverseTransformNode(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, string implicitProperty,
            string extensionNode, string extensionOutputName, string outNodeName)
        {
            if (!graph.HasNode(outNodeName))
            {
                string forwardNodeName;

#if DISABLE_MATERIALX_EXTENSIONS
                QuickNode.AddImplicitPropertyFromNode(
                    forwardNodeName = implicitProperty, MtlxDataTypes.Matrix44, node, graph, null, "Out");
#else
                var forwardNode = QuickNode.NaryOp(extensionNode, node, graph, null, outNodeName);
                forwardNode.outputName = extensionOutputName;
                forwardNodeName = forwardNode.name;
#endif
                var nodeData = graph.AddNode(outNodeName, MtlxNodeTypes.Inverse, MtlxDataTypes.Matrix44);
                graph.AddPortAndEdge(forwardNodeName, nodeData.name, "in", MtlxDataTypes.Matrix44);
            }
            externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, outNodeName);
        }

        void EnsureViewProjectionNodeExists(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (graph.HasNode(k_ViewProjectionNodeName))
                return;

            string viewNodeName, projectionNodeName;

#if DISABLE_MATERIALX_EXTENSIONS
            QuickNode.AddImplicitPropertyFromNode(
                viewNodeName = PolySpatialShaderGlobals.ViewMatrix,
                MtlxDataTypes.Matrix44, node, graph, null, "Out");
            QuickNode.AddImplicitPropertyFromNode(
                projectionNodeName = PolySpatialShaderGlobals.ProjectionMatrix,
                MtlxDataTypes.Matrix44, node, graph, null, "Out");
#else
            var viewNode = QuickNode.NaryOp(
                MtlxNodeTypes.RealityKitSurfaceWorldToView, node, graph, null, "TransformView");
            viewNode.outputName = "worldToView";
            viewNodeName = viewNode.name;
            var projectionNode = QuickNode.NaryOp(
                MtlxNodeTypes.RealityKitSurfaceViewToProjection, node, graph, null, "TransformProjection");
            projectionNode.outputName = "viewToProjection";
            projectionNodeName = projectionNode.name;

#endif

            var vpNode = graph.AddNode(k_ViewProjectionNodeName, MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44);
            graph.AddPortAndEdge(viewNodeName, vpNode.name, "in1", MtlxDataTypes.Matrix44);
            graph.AddPortAndEdge(projectionNodeName, vpNode.name, "in2", MtlxDataTypes.Matrix44);
        }
    }
}
