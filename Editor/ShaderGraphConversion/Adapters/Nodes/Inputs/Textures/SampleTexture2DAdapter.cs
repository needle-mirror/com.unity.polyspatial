namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleTexture2DAdapter : AbstractSampleTexture2DAdapter<SampleTexture2DNode>
    {
        protected override string NodeType => MtlxNodeTypes.Image;

        protected override TextureType GetTextureType(SampleTexture2DNode node)
        {
            return node.textureType;
        }

        protected override void AddSamplerState(MtlxNodeData nodeData, TextureSamplerState samplerState)
        {
            nodeData.AddPortString("filtertype", MtlxDataTypes.String, samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Point => "closest",
                _ => "linear",
            });
            var addressMode = samplerState.wrap switch
            {
                TextureSamplerState.WrapMode.Clamp => "clamp",
                TextureSamplerState.WrapMode.Mirror => "mirror",
                _ => "periodic",
            };
            nodeData.AddPortString("uaddressmode", MtlxDataTypes.String, addressMode);
            nodeData.AddPortString("vaddressmode", MtlxDataTypes.String, addressMode);
        }
    }
}
