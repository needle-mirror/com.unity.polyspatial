using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class AmbientAdapter : ANodeAdapter<AmbientNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Ambient-Node.html
            QuickNode.CompoundOp(node, graph, externals, sgContext, "Ambient", new()
            {
                ["Color/Sky"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new ImplicitInputDef(PolySpatialShaderGlobals.k_AmbientSkyColor, MtlxDataTypes.Vector4),
                }),
                ["Equator"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new ImplicitInputDef(PolySpatialShaderGlobals.k_AmbientEquatorColor, MtlxDataTypes.Vector4),
                }),
                ["Ground"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector4, new()
                {
                    ["in"] = new ImplicitInputDef(PolySpatialShaderGlobals.k_AmbientGroundColor, MtlxDataTypes.Vector4),
                }),
            });
        }
    }
}