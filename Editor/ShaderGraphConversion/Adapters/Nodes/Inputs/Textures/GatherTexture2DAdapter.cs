namespace UnityEditor.ShaderGraph.MaterialX
{
    class GatherTexture2DAdapter : AbstractUVNodeAdapter<GatherTexture2DNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Gather-Texture-2D-Node.html
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "GatherTexture2D", @"
RGBA = GATHER_TEXTURE2D(Texture, Sampler, UV, Offset);
R = RGBA.r; G = RGBA.g; B = RGBA.b; A = RGBA.a;");
        }
    }
}