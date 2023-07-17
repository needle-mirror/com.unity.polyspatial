
using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class PositionAdapter : ANodeAdapter<PositionNode>
    {
        static internal string SpaceToMtlxString(Internal.CoordinateSpace space)
            => space switch
            {
                Internal.CoordinateSpace.AbsoluteWorld => "world",
                Internal.CoordinateSpace.World => "world",
                Internal.CoordinateSpace.Object => "object",
                Internal.CoordinateSpace.Tangent => "tangent",
                Internal.CoordinateSpace.View => "view",
                Internal.CoordinateSpace.Screen => "screen",
                _ => ""
            };

        // TODO: Move to TransformAdapter probably.
        static internal void SetupSpacePort(MtlxNodeData nodeData, AbstractMaterialNode node)
        {
            var gnode = node as GeometryNode;

            string space = gnode.space switch
            {
                Internal.CoordinateSpace.AbsoluteWorld => "world",
                Internal.CoordinateSpace.World => "world",
                Internal.CoordinateSpace.Object => "object",
                Internal.CoordinateSpace.Tangent => "tangent",
                Internal.CoordinateSpace.View => "view",
                Internal.CoordinateSpace.Screen => "screen",
                _ => ""
            };

            if (!string.IsNullOrEmpty(space))
                nodeData.AddPortString("space", MtlxDataTypes.String, space);
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var positionNode = graph.AddNode(NodeUtils.GetNodeName(node, "Position"),
                MtlxNodeTypes.GeomPosition, MtlxDataTypes.Vector3);
            SetupSpacePort(positionNode, node);

            // If we relied on the default conversion to promote the vec3 position to a vec4, we would
            // get w = 0.  That makes sense for vectors, but for points (like position), we want w = 1.
            // So, promote the value here with a fixed w = 1.
            var xNode = graph.AddNode(NodeUtils.GetNodeName(node, "X"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            xNode.AddPortString("channels", MtlxDataTypes.String, "x");
            graph.AddPortAndEdge(positionNode.name, xNode.name, "in", MtlxDataTypes.Vector3);

            var yNode = graph.AddNode(NodeUtils.GetNodeName(node, "Y"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            yNode.AddPortString("channels", MtlxDataTypes.String, "y");
            graph.AddPortAndEdge(positionNode.name, yNode.name, "in", MtlxDataTypes.Vector3);

            var zNode = graph.AddNode(NodeUtils.GetNodeName(node, "Z"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
            zNode.AddPortString("channels", MtlxDataTypes.String, "z");
            graph.AddPortAndEdge(positionNode.name, zNode.name, "in", MtlxDataTypes.Vector3);

            var nodeData = QuickNode.NaryOp(MtlxNodeTypes.Combine4, node, graph, externals, "Combine");
            nodeData.datatype = MtlxDataTypes.Vector4;
            graph.AddPortAndEdge(xNode.name, nodeData.name, "in1", MtlxDataTypes.Float);
            graph.AddPortAndEdge(yNode.name, nodeData.name, "in2", MtlxDataTypes.Float);
            graph.AddPortAndEdge(zNode.name, nodeData.name, "in3", MtlxDataTypes.Float);
            nodeData.AddPortValue("in4", MtlxDataTypes.Float, new[] {1.0f});
        }
    }
}
