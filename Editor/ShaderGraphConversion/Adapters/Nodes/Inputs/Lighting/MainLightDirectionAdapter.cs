
using System;
using System.Collections.Generic;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class MainLightDirectionAdapter : ANodeAdapter<MainLightDirectionNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Note that the direction of the light is the opposite of its vector.  Refer to:
            // https://github.cds.internal.unity3d.com/unity/quantum/commit/78925a799fd2002969d7326d3176d50a9414e597#diff-8cafc4e487dec6ada0b282887ae545dc4852ae4737345a00362d15b7dea5f8b0R129
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "MainLightDirection",
                $"Direction = -{PolySpatialShaderGlobals.k_LightPositionPrefix}0.xyz;");
        }
    }
}
