using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class SplitTextureTransformAdapter : ANodeAdapter<SplitTextureTransformNode>
    {
        internal static string EnsureTextureTransformProperty(MaterialSlot slot, MtlxGraphData graph)
        {
            string textureTransformNodeName;
            var isSystemInput = true;
            var srcSlot = SlotUtils.GetRedirectedSourceConnectionSlot(slot);
            if (srcSlot == null)
            {
                textureTransformNodeName = GetTextureTransformNodeName(slot.owner.GetVariableNameForSlot(slot.id));
            }
            else if (srcSlot.owner is PropertyNode propertyNode)
            {
                textureTransformNodeName = GetTextureTransformNodeName(propertyNode.property.referenceName);
                isSystemInput = false;
            }
            else
            {
                textureTransformNodeName = GetTextureTransformNodeName(
                    Texture2DAssetAdapter.GetVariableNameForSlot(srcSlot));
            }
            if (!graph.HasNode(textureTransformNodeName))
            {
                var nodeData = graph.AddNode(
                    textureTransformNodeName, MtlxNodeTypes.Constant,
                    MtlxDataTypes.Vector4, !isSystemInput, isSystemInput);
                nodeData.AddPortValue("value", MtlxDataTypes.Vector4, new[] { 1.0f, 1.0f, 0.0f, 0.0f });
            }
            return textureTransformNodeName;
        }

        internal static string GetTextureTransformNodeName(string texturePropertyName)
        {
            return $"TextureTransform{texturePropertyName}";
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            // Unconnected texture slots correspond to implicit properties.
            var slot = NodeUtils.GetPrimaryInput(node);
            if (!slot.isConnected)
                QuickNode.EnsureImplicitProperty(node.GetVariableNameForSlot(slot.id), MtlxDataTypes.Filename, graph);
            
            var transformNodeName = EnsureTextureTransformProperty(slot, graph);

            QuickNode.CompoundOp(node, graph, externals, sgContext, "TextureTransform", new()
            {
                ["Tiling"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ImplicitInputDef(transformNodeName, MtlxDataTypes.Vector4),
                    ["channels"] = new StringInputDef("xy"),
                }),
                ["Offset"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ImplicitInputDef(transformNodeName, MtlxDataTypes.Vector4),
                    ["channels"] = new StringInputDef("zw"),
                }),
                ["Texture Only"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Filename, new()
                {
                    ["in"] = new ExternalInputDef("In"),
                }),
            });
        }
    }
}