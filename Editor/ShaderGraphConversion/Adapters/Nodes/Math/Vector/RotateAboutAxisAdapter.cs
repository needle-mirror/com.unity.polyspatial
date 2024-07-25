
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class RotateAboutAxisAdapter : ANodeAdapter<RotateAboutAxisNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            QuickNode.CompoundOp(node, graph, externals, sgContext, "RotateAboutAxis", new()
            {
                ["Out"] = new(MtlxNodeTypes.Rotate3d, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new ExternalInputDef("In"),
                    ["amount"] = ((RotateAboutAxisNode)node).unit switch
                    {
                        RotationUnit.Radians => new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new ExternalInputDef("Rotation"),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, Mathf.Rad2Deg),
                        }),
                        RotationUnit.Degrees => new ExternalInputDef("Rotation"),
                        var unit => throw new NotSupportedException($"Unknown rotation unit: {unit}"), 
                    },
                    ["axis"] = new ExternalInputDef("Axis"),
                }),
            });
        }
    }
}
