namespace UnityEditor.ShaderGraph.MaterialX
{
    class EyeIndexAdapter : ANodeAdapter<EyeIndexNode>
    {
        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            QuickNode.CompoundOp(node, graph, externals, "EyeIndex", new()
            {
                ["Out"] = new(MtlxNodeTypes.RealityKitGeometrySwitchCameraIndex, MtlxDataTypes.Float, new()
                {
                    ["right"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
            });
        }
    }
}