
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class DitherAdapter : ANodeAdapter<DitherNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {

            if (node is DitherNode dnode)
            {
                //[Slot(0, Binding.None)] DynamicDimensionVector In,
                //[Slot(1, Binding.ScreenPosition)] Vector2 ScreenPosition,
                //[Slot(2, Binding.None)] out DynamicDimensionVector Out)
                //$precision2 uv = ScreenPosition.xy * _ScreenParams.xy;
                //$precision DITHER_THRESHOLDS[16] =
                //{
                //    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                //    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                //    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                //    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                //};
                //uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                //Out = In - DITHER_THRESHOLDS[index];

                var screenPosSlot = (ScreenPositionMaterialSlot)NodeUtils.GetSlotByName(node, "Screen Position");
                var screenSpaceNode = ScreenPositionAdapter.SetupScreenSpace(screenPosSlot.screenSpaceType, graph);
                QuickNode.EnsureImplicitProperty(PolySpatialShaderGlobals.ScreenParams, MtlxDataTypes.Vector4, graph);
                externals.AddExternalPortAndEdge(screenPosSlot, screenSpaceNode.name, "Screen Position");

                // $precision2 uv = ScreenPosition.xy * _ScreenParams.xy; => keeping x/y separate.
                var x = graph.AddNode(NodeUtils.GetNodeName(node, "DitherScreenPositionX"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                x.AddPortString("channels", MtlxDataTypes.String, "x");
                graph.AddPortAndEdge(screenSpaceNode.name, x.name, "in", MtlxDataTypes.Vector4);

                var y = graph.AddNode(NodeUtils.GetNodeName(node, "DitherScreenPositionY"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                y.AddPortString("channels", MtlxDataTypes.String, "y");
                graph.AddPortAndEdge(screenSpaceNode.name, y.name, "in", MtlxDataTypes.Vector4);

                var screenWidth = graph.AddNode(NodeUtils.GetNodeName(node, "ScreenWidth"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                screenWidth.AddPortString("channels", MtlxDataTypes.String, "x");
                graph.AddPortAndEdge(PolySpatialShaderGlobals.ScreenParams, screenWidth.name, "in", MtlxDataTypes.Vector4);

                var u = graph.AddNode(NodeUtils.GetNodeName(node, "DitherU"), MtlxNodeTypes.Multiply, MtlxDataTypes.Float);
                graph.AddPortAndEdge(x.name, u.name, "in1", MtlxDataTypes.Float);
                graph.AddPortAndEdge(screenWidth.name, u.name, "in2", MtlxDataTypes.Float);

                var screenHeight = graph.AddNode(NodeUtils.GetNodeName(node, "ScreenHeight"), MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                screenHeight.AddPortString("channels", MtlxDataTypes.String, "y");
                graph.AddPortAndEdge(PolySpatialShaderGlobals.ScreenParams, screenHeight.name, "in", MtlxDataTypes.Vector4);

                var v = graph.AddNode(NodeUtils.GetNodeName(node, "DitherV"), MtlxNodeTypes.Multiply, MtlxDataTypes.Float);
                graph.AddPortAndEdge(y.name, v.name, "in1", MtlxDataTypes.Float);
                graph.AddPortAndEdge(screenHeight.name, v.name, "in2", MtlxDataTypes.Float);

                // (uint(uv.x) % 4) * 4 => x component
                var floorU = graph.AddNode(NodeUtils.GetNodeName(node, "DitherFloorU"), MtlxNodeTypes.Floor, MtlxDataTypes.Float);
                graph.AddPortAndEdge(u.name, floorU.name, "in", MtlxDataTypes.Float);
                var modU = graph.AddNode(NodeUtils.GetNodeName(node, "DitherModU"), MtlxNodeTypes.Modulo, MtlxDataTypes.Float);
                graph.AddPortAndEdge(floorU.name, modU.name, "in1", MtlxDataTypes.Float);
                modU.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 4 });
                var mulU = graph.AddNode(NodeUtils.GetNodeName(node, "DitherMulU"), MtlxNodeTypes.Multiply, MtlxDataTypes.Float);
                graph.AddPortAndEdge(modU.name, mulU.name, "in1", MtlxDataTypes.Float);
                mulU.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 4 });

                // uint(uv.y) % 4 => y component
                var floorV = graph.AddNode(NodeUtils.GetNodeName(node, "DitherFloorV"), MtlxNodeTypes.Floor, MtlxDataTypes.Float);
                graph.AddPortAndEdge(v.name, floorV.name, "in", MtlxDataTypes.Float);
                var modV = graph.AddNode(NodeUtils.GetNodeName(node, "DitherModV"), MtlxNodeTypes.Modulo, MtlxDataTypes.Float);
                graph.AddPortAndEdge(floorV.name, modV.name, "in1", MtlxDataTypes.Float);
                modV.AddPortValue("in2", MtlxDataTypes.Float, new float[] { 4 });

                // uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
                var di = graph.AddNode(NodeUtils.GetNodeName(node, "DitherIndex"), MtlxNodeTypes.Add, MtlxDataTypes.Float);
                graph.AddPortAndEdge(mulU.name, di.name, "in1", MtlxDataTypes.Float);
                graph.AddPortAndEdge(modV.name, di.name, "in2", MtlxDataTypes.Float);


                //$precision DITHER_THRESHOLDS[16] =
                //{
                //    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                //    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                //    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                //    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                //};
                var ditherValues = new float[16]  { 1.0f  / 17.0f,  9.0f / 17.0f,  3.0f / 17.0f, 11.0f / 17.0f,
                                                    13.0f / 17.0f,  5.0f / 17.0f, 15.0f / 17.0f,  7.0f / 17.0f,
                                                    4.0f  / 17.0f, 12.0f / 17.0f,  2.0f / 17.0f, 10.0f / 17.0f,
                                                    16.0f / 17.0f,  8.0f / 17.0f, 14.0f / 17.0f,  6.0f / 17.0f };
                var ditherIndices = new float[16] { 0f, 1f, 2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f, 11f, 12f, 13f, 14f, 15f };

                // don't have a better way to index an array w/out some potentially unsupported features.
                var dLookup = graph.AddNode(NodeUtils.GetNodeName(node, "DitherLookup"), MtlxNodeTypes.CurveLookup, MtlxDataTypes.Float);
                graph.AddPortAndEdge(di.name, dLookup.name, "in", MtlxDataTypes.Float);
                dLookup.AddPortValue("knots", MtlxDataTypes.FloatArray, ditherIndices);
                dLookup.AddPortValue("knotvalues", MtlxDataTypes.FloatArray, ditherValues);

                //        Out = In - DITHER_THRESHOLDS[index];
                var outputNode = graph.AddNode(NodeUtils.GetNodeName(node, "Dither"), MtlxNodeTypes.Subtract, MtlxDataTypes.Float);
                outputNode.AddPortValue("in1", MtlxDataTypes.Float, new float[] { 1.0f });
                graph.AddPortAndEdge(dLookup.name, outputNode.name, "in2", MtlxDataTypes.Float);

                externals.AddExternalPortAndEdge(NodeUtils.GetSlotByName(node, "In"), outputNode.name, "in1");
                externals.AddExternalPort(NodeUtils.GetPrimaryOutput(node).slotReference, outputNode.name);
            }
        }
    }
}
