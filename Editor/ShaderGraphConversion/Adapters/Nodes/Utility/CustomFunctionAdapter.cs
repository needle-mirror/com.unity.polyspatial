using System;
using System.IO;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class CustomFunctionAdapter : ANodeAdapter<CustomFunctionNode>
    {
        public override string SupportDetails(AbstractMaterialNode node)
        {
            var customFunctionNode = (CustomFunctionNode)node;
            if (customFunctionNode.sourceType != HlslSourceType.String)
            {
                // It seems like overkill to load the file source every time we want to validate the node.  Let's
                // assume for now that the reporting we do on conversion will be sufficient.
                return "";
            }

            // Parse the expression just to see if it throws an exception.
            try
            {
                CompoundOpParser.Parse(node, null, customFunctionNode.functionBody);
            }
            catch (CompoundOpParser.ParseException e)
            {
                return e.ShortMessage;
            }

            return "";
        }

        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            var customFunctionNode = (CustomFunctionNode)node;
            switch (customFunctionNode.sourceType)
            {
                case HlslSourceType.String:
                    try
                    {
                        QuickNode.CompoundOp(
                            node, graph, externals, sgContext, customFunctionNode.functionName,
                            customFunctionNode.functionBody);
                    }
                    catch (CompoundOpParser.ParseException e)
                    {
                        MtlxPostProcessor.LogWarningForGraph(node.owner, $"Couldn't parse custom function: {e}");
                    }
                    break;
                
                case HlslSourceType.File:
                    if (string.IsNullOrEmpty(customFunctionNode.functionSource))
                    {
                        // The source file hasn't yet been populated.  If this is an error (for example, because
                        // an output is specified), the shader graph editor will report it.
                        return;
                    }
                    var path = AssetDatabase.GUIDToAssetPath(customFunctionNode.functionSource);
                    try
                    {
                        QuickNode.CompoundOp(
                            node, graph, externals, sgContext, customFunctionNode.functionName,
                            File.ReadAllText(path), $"{customFunctionNode.functionName}_float");
                    }
                    catch (CompoundOpParser.ParseException e)
                    {
                        MtlxPostProcessor.LogWarningForGraph(
                            node.owner, $"Couldn't parse custom function in {path}: {e}");
                    }
                    catch (IOException e)
                    {
                        MtlxPostProcessor.LogWarningForGraph(node.owner, $"Error reading {path}: {e}");
                    }
                    break;
                
                default:
                    throw new NotSupportedException($"Unrecognized source type: {customFunctionNode.sourceType}");
            }            
        }
    }
}