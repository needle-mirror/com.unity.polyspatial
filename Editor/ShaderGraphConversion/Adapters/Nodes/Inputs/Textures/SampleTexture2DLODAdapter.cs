using System.Text;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SampleTexture2DLODAdapter : AbstractSampleTexture2DAdapter<SampleTexture2DLODNode>
    {
        internal static string GetFilterType(TextureSamplerState samplerState)
        {
            return samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Point => "nearest",
                _ => "linear",
            };
        }

        internal static string GetAddressMode(TextureSamplerState samplerState)
        {
            return samplerState.wrap switch
            {
                TextureSamplerState.WrapMode.Clamp => "clamp_to_edge",
                TextureSamplerState.WrapMode.Mirror => "mirrored_repeat",
                _ => "repeat",
            };
        }

        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
#if DISABLE_MATERIALX_EXTENSIONS
            return false;
#else
            return true;
#endif
        }

        protected override string NodeType => MtlxNodeTypes.RealityKitImageLod;

        protected override TextureType GetTextureType(SampleTexture2DLODNode node)
        {
            return node.textureType;
        }

        protected override void AddSamplerState(MtlxNodeData nodeData, TextureSamplerState samplerState)
        {
            var filterType = GetFilterType(samplerState);
            nodeData.AddPortString("mag_filter", MtlxDataTypes.String, filterType);
            nodeData.AddPortString("min_filter", MtlxDataTypes.String, filterType);
            nodeData.AddPortString("mip_filter", MtlxDataTypes.String, filterType);

            var addressMode = GetAddressMode(samplerState);
            nodeData.AddPortString("s_address", MtlxDataTypes.String, addressMode);
            nodeData.AddPortString("t_address", MtlxDataTypes.String, addressMode);
        }

        protected override void AddInputSlot(MtlxNodeData nodeData, MaterialSlot slot, ref string externalPortName)
        {
            if (slot.RawDisplayName() == "LOD")
                nodeData.AddPortValue(externalPortName = "level", MtlxDataTypes.Float, SlotUtils.GetDefaultValue(slot));
            else
                base.AddInputSlot(nodeData, slot, ref externalPortName);
        }
    }
}