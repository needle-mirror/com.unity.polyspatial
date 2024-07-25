using Unity.PolySpatial;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class ViewDirectionAdapter : GeometryVectorAdapter<ViewDirectionNode>
    {
#if DISABLE_MATERIALX_EXTENSIONS
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is ViewDirectionNode vdNode)
            {
                QuickNode.EnsureImplicitProperty(PolySpatialShaderGlobals.WorldSpaceCameraPos, MtlxDataTypes.Vector3, graph);
                QuickNode.EnsureImplicitProperty(PolySpatialShaderGlobals.WorldSpaceCameraDir, MtlxDataTypes.Vector3, graph);
                QuickNode.EnsureImplicitProperty(PolySpatialShaderGlobals.OrthoParams, MtlxDataTypes.Vector4, graph);

                // create a 'locally' unique/reusable constant.
                var wpName = "ViewDirectionWorldPositionInput";
                if (!graph.HasNode(wpName))
                {
                    var wpNodeData = graph.AddNode(wpName, MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3);
                    wpNodeData.AddPortString("space", MtlxDataTypes.String, "world");
                }

                // CameraPosition - ObjectPosition
                var perspDiffNode = graph.AddNode(NodeUtils.GetNodeName(node, "ViewDirPerspectiveDiff"), MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3);
                graph.AddPortAndEdge(PolySpatialShaderGlobals.WorldSpaceCameraPos, perspDiffNode.name, "in1", MtlxDataTypes.Vector3);
                graph.AddPortAndEdge(wpName, perspDiffNode.name, "in2", MtlxDataTypes.Vector3);

                // |CameraPosition - ObjectPosition|
                var perspNormalNode = graph.AddNode(NodeUtils.GetNodeName(node, "ViewDirPerspectiveNormal"), MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3);
                graph.AddPortAndEdge(perspDiffNode.name, perspNormalNode.name, "in", MtlxDataTypes.Vector3);


                // -1 * CameraDirection
                var orthoEvalNode = graph.AddNode(NodeUtils.GetNodeName(node, "ViewDirOrtho"), MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3);
                orthoEvalNode.AddPortValue("in1", MtlxDataTypes.Vector3, new float[] { -1, -1, -1 });
                graph.AddPortAndEdge(PolySpatialShaderGlobals.WorldSpaceCameraDir, orthoEvalNode.name, "in2", MtlxDataTypes.Vector3);

                // isOrtho = unity_OrthoParams.w
                var isOrthoNode = graph.AddNode(NodeUtils.GetNodeName(node, "IsOrtho"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                isOrthoNode.AddPortString("channels", MtlxDataTypes.String, "w");
                graph.AddPortAndEdge(PolySpatialShaderGlobals.OrthoParams, isOrthoNode.name, "in", MtlxDataTypes.Vector4);

                // isOrtho == 0 ? |CameraPosition - ObjectPosition| : -1 * CameraDirection
                var orthoTestNode = graph.AddNode(NodeUtils.GetNodeName(node, "ViewDirIsOrtho"), MtlxNodeTypes.IfEqual, MtlxDataTypes.Vector3);
                graph.AddPortAndEdge(isOrthoNode.name, orthoTestNode.name, "value1", MtlxDataTypes.Float);
                orthoTestNode.AddPortValue("value2", MtlxDataTypes.Float, new float[] { 0 });
                graph.AddPortAndEdge(perspNormalNode.name, orthoTestNode.name, "in1", MtlxDataTypes.Vector3);
                graph.AddPortAndEdge(orthoEvalNode.name, orthoTestNode.name, "in2", MtlxDataTypes.Vector3);


                var space = PositionAdapter.SpaceToMtlxString(vdNode.space);
                var outputNode = graph.AddNode(NodeUtils.GetNodeName(node, "ViewDirTransform"), MtlxNodeTypes.TransformNormal, MtlxDataTypes.Vector3);
                graph.AddPortAndEdge(orthoTestNode.name, outputNode.name, "in", MtlxDataTypes.Vector3);
                outputNode.AddPortString("fromspace", MtlxDataTypes.String, "world");
                outputNode.AddPortString("tospace", MtlxDataTypes.String, space);


                externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, outputNode.name);
            }
        }
#else
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (((ViewDirectionNode)node).space != CoordinateSpace.Tangent)
            {
                base.BuildInstance(node, graph, externals, sgContext);
                return;
            }
            // Special handling for tangent space view directions, which don't provide what we'd expect on
            // RealityKit (reported to Apple as FB13683686).  We transform the world space view direction by
            // the world-to-tangent matrix.
            QuickNode.CompoundOp(node, graph, externals, sgContext, "ViewDirection", @"
Out = normalize(mul(polySpatial_WorldToTangent, float4(polySpatial_WorldSpaceViewDirection, 0)).xyz);");
        }
#endif

        protected override string Hint => "ViewDirection";
        protected override string NodeType => MtlxNodeTypes.RealityKitViewDirection;
    }
}
