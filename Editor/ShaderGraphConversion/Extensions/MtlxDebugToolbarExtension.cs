#if POLYSPATIAL_EXPERIMENTAL

using System.Collections.Generic;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using Unity.PolySpatial.Internals;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class MtlxDebugToolbarExtension : IShaderGraphToolbarExtension
    {
        static class Styles
        {
            internal static GUIContent k_DebugOutput = new(
                "Preview Selection on Device", "Replace shader with output of current selection in Play to Device.");
        }

        MaterialGraphView m_DebugOutputGraphView;
        PolySpatialShaderGraph m_DebugOutputShaderGraph;

        MaterialGraphView DebugOutputGraphView
        {
            get => m_DebugOutputGraphView;
            set
            {
                if (m_DebugOutputGraphView == value)
                    return;

                if (m_DebugOutputGraphView != null)
                {
                    m_DebugOutputGraphView.OnSelectionChange -= UpdateDebugOutput;
                    UpdateDebugOutput(new());
                }
                
                m_DebugOutputGraphView = value;
                if (m_DebugOutputGraphView != null)
                {
                    m_DebugOutputGraphView.OnSelectionChange += UpdateDebugOutput;
                    UpdateDebugOutput(m_DebugOutputGraphView.selection);
                }
            }
        }

        void UpdateDebugOutput(List<ISelectable> selection)
        {
            MaterialSlot debugOutputSlot = null;
            if (selection.Count == 1)
            {
                switch (selection[0])
                {
                    case Edge edge:
                    {
                        if (edge.output is ShaderPort shaderPort)
                            debugOutputSlot = shaderPort.slot;
                        break;
                    }
                    case IShaderNodeView nodeView:
                        debugOutputSlot = NodeUtils.GetPrimaryOutput(nodeView.node) ??
                            NodeUtils.GetPrimaryInput(nodeView.node);
                        break;
                }
            }

            // Replace the default surface adapter with one that renders the debug output slot (if any).
            var previousSurfaceAdapter = AdapterMap.surfaceAdapter;
            try
            {
                AdapterMap.SetSurfaceAdapter(new UsdPreviewSurfaceAdapter(debugOutputSlot));
                var path = AssetDatabase.GUIDToAssetPath(m_DebugOutputGraphView.graph.assetGuid);
                var mtlxGraphData = MtlxPostProcessor.ProcessGraph(
                    m_DebugOutputGraphView.graph, Path.GetFileNameWithoutExtension(path));
                var mtlxEncoding = new UsdGraphProcessor().ProcessGraph(mtlxGraphData);
                if (m_DebugOutputShaderGraph.materialXEncoding != mtlxEncoding)
                    m_DebugOutputShaderGraph.OverrideMaterialXEncoding(mtlxEncoding);
            }
            finally
            {
                AdapterMap.SetSurfaceAdapter(previousSurfaceAdapter);
            }
        }

        public void OnGUI(MaterialGraphView graphView)
        {
            if (!PolySpatialCore.HostConnectionManager.HasActiveSessions())
            {
                DebugOutputGraphView = null;
                return;
            }

            var path = AssetDatabase.GUIDToAssetPath(graphView.graph.assetGuid);
            var shader = AssetDatabase.LoadAssetAtPath(path, typeof(Shader));
            if (shader == null || !PolySpatialCore.LocalAssetManager.TryGetShaderGraphByShaderInstanceID(
                shader.GetInstanceID(), out m_DebugOutputShaderGraph))
            {
                DebugOutputGraphView = null;
                return;
            }
            
            DebugOutputGraphView =
                GUILayout.Toggle(DebugOutputGraphView == graphView, Styles.k_DebugOutput, EditorStyles.toolbarButton) ?
                graphView : null;
        }
    }
}

#endif