using System;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal class MtlxDataExtension : AbstractShaderGraphDataExtension
    {
        [SerializeField]
        bool m_EnableUnlitToneMapping;

        internal bool EnableUnlitToneMapping => m_EnableUnlitToneMapping;

        internal override string displayName => "MaterialX";

        internal override void OnPropertiesGUI(
            VisualElement ctx, Action onChange, Action<string> registerUndo, GraphData owner)
        {
            AddProperty(
                ctx, "Unlit Tone Mapping", "If true, enable tone mapping for unlit targets.", 1,
                new Toggle() { value = m_EnableUnlitToneMapping }, evt =>
            {
                if (m_EnableUnlitToneMapping == evt.newValue)
                    return;

                registerUndo("Change Unlit Tone Mapping");
                m_EnableUnlitToneMapping = evt.newValue;

                onChange();
            });
        }

        void AddProperty<T>(
            VisualElement element, string label, string tooltip, int indentLevel,
            BaseField<T> field, EventCallback<ChangeEvent<T>> evt)
        {
            if (field is INotifyValueChanged<T> notifyValueChanged)
                notifyValueChanged.RegisterValueChangedCallback(evt);

            var propertyLabel = new Label(label);
            propertyLabel.tooltip = tooltip;
            var propertyRow = new PropertyRow(propertyLabel);

            ApplyPadding(propertyRow, indentLevel);
            propertyRow.Add(field);
            element.hierarchy.Add(propertyRow);
        }

        void ApplyPadding(PropertyRow row, int indentLevel)
        {
            row.Q(className: "unity-label").style.marginLeft = (indentLevel) * paddingIdentationFactor;
        }
    }
}