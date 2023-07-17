using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class TextureSizeAdapter : ANodeAdapter<Texture2DPropertiesNode>
    {
        internal static string EnsureTextureSizeProperty(MaterialSlot slot, MtlxGraphData graph)
        {
            string textureSizeNodeName;
            var isSystemInput = true;
            var srcSlot = SlotUtils.GetRedirectedSourceConnectionSlot(slot);
            if (srcSlot == null)
            {
                textureSizeNodeName = GetTextureSizeNodeName(slot.owner.GetVariableNameForSlot(slot.id));
            }
            else if (srcSlot.owner is PropertyNode propertyNode)
            {
                textureSizeNodeName = GetTextureSizeNodeName(propertyNode.property.referenceName);
                isSystemInput = false;
            }
            else
            {
                textureSizeNodeName = GetTextureSizeNodeName(Texture2DAssetAdapter.GetVariableNameForSlot(srcSlot));
            }
            if (!graph.HasNode(textureSizeNodeName))
            {
                var nodeData = graph.AddNode(
                    textureSizeNodeName, MtlxNodeTypes.Constant, MtlxDataTypes.Vector4, !isSystemInput, isSystemInput);
                nodeData.AddPortValue("value", MtlxDataTypes.Vector4, new float[4]);
            }
            return textureSizeNodeName;
        }

        internal static string GetTextureSizeNodeName(string texturePropertyName)
        {
            return $"TextureSize{texturePropertyName}";
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            // Unconnected texture slots correspond to implicit properties.
            var slot = NodeUtils.GetPrimaryInput(node);
            if (!slot.isConnected)
                QuickNode.EnsureImplicitProperty(node.GetVariableNameForSlot(slot.id), MtlxDataTypes.Filename, graph);
    
            var sizeNodeName = EnsureTextureSizeProperty(slot, graph);

            var channelNames = new[] { ("x", "Width"), ("y", "Height"), ("z", "Texel Width"), ("w", "Texel Height") };
            foreach (var (channel, name) in channelNames) {
                var nodeName = NodeUtils.GetNodeName(node, name);
                var nodeData = graph.AddNode(nodeName, MtlxNodeTypes.Swizzle, MtlxDataTypes.Float);
                graph.AddPortAndEdge(sizeNodeName, nodeName, "in", MtlxDataTypes.Vector4);
                nodeData.AddPortString("channels", MtlxDataTypes.String, channel);
                externals.AddExternalPort(NodeUtils.GetOutputByName(node, name).slotReference, nodeName);
            }
        }
    }
}