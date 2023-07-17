
using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class KeywordAdapter : ANodeAdapter<KeywordNode>
    {
        internal const string k_MaterialXKeywordReferenceName = "MATERIAL_X";

        [BuiltinKeyword]
        static KeywordDescriptor MaterialXKeyword()
        {
            return new KeywordDescriptor()
            {
                displayName = "MaterialX",
                referenceName = k_MaterialXKeywordReferenceName,
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
                scope = KeywordScope.Global,
                value = 0,
                entries = new KeywordEntry[0],
                stages = KeywordShaderStage.All,
            };
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            if (node is not KeywordNode knode)
                return;

            var keywordName = knode.keyword.referenceName;
            var keywordValue = (keywordName == k_MaterialXKeywordReferenceName) ? 1 : knode.keyword.value;

            string inputName;
            if (knode.keyword.keywordType == KeywordType.Boolean)
            {
                inputName = (keywordValue == 0) ? "Off" : "On";
            }
            else
            {
                List<MaterialSlot> inputs = new();
                node.GetInputSlots(inputs);
                inputName = inputs[keywordValue].RawDisplayName();
            }

            QuickNode.UnaryOp(MtlxNodeTypes.Dot, node, graph, externals, $"Keyword{keywordName}", "in", "", inputName);
        }
    }
}
