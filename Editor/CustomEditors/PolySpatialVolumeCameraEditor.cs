using System;
using Unity.PolySpatial;
using UnityEditor.PolySpatial.InternalBridge;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    [CustomEditor(typeof(VolumeCamera))]
    class PolySpatialVolumeCameraEditor : Editor
    {
        static class Styles
        {
            public static GUIStyle LinkButton;

            static Styles()
            {
                LinkButton = "FloatFieldLinkButton";
                LinkButton.alignment = TextAnchor.MiddleCenter;
            }
        }

        const int k_MinInspectorWidth = 212;

        SerializedProperty m_ModeProperty;
        SerializedProperty m_DimensionsProperty;
        SerializedProperty m_CullingMaskProperty;
        SerializedProperty m_IsUniformScaleProperty;

        //Linked-scaling button related variables.
        GUIContent m_DimensionsContent = EditorGUIUtility.TrTextContent("Dimensions", "Sets the size of the oriented bounding box dimensions in X, Y, Z.");
        static readonly int k_FoldoutHash = "Foldout".GetHashCode();
        static readonly float[] k_Vector3Floats = {0, 0, 0};
        static readonly Lazy<GUIContent[]> k_XYZLabels = new(() => new[]
            { EditorGUIBridge.TextContent("X"), EditorGUIBridge.TextContent("Y"), EditorGUIBridge.TextContent("Z") });

        Vector3 m_InitialDimension, m_PreviousDimension;

        void OnEnable()
        {
            m_ModeProperty = serializedObject.FindProperty("m_Mode");
            m_DimensionsProperty = serializedObject.FindProperty("m_Dimensions");
            m_IsUniformScaleProperty = serializedObject.FindProperty("m_IsUniformScale");
            m_CullingMaskProperty = serializedObject.FindProperty("CullingMask");

            m_InitialDimension = m_DimensionsProperty.vector3Value;
        }

        public override void OnInspectorGUI()
        {
            using (var changed = new EditorGUI.ChangeCheckScope())
            {

                // Adjust label to avoid content spilling over
                if (!EditorGUIUtility.wideMode)
                {
                    EditorGUIUtility.wideMode = true;
                    EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - k_MinInspectorWidth;
                }

                EditorGUILayout.PropertyField(m_ModeProperty, EditorGUIBridge.TextContent("Mode"));

                var isUniformScale = m_IsUniformScaleProperty.boolValue;
                var wasUniformScale = isUniformScale;
                var axisModified = -1;
                var toggleContent = EditorGUIUtility.TrTextContent("", (isUniformScale ? "Disable" : "Enable") + " constrained proportions");
                var position = EditorGUILayout.GetControlRect(true);
                var dimensions = m_DimensionsProperty.vector3Value;
                m_DimensionsProperty.vector3Value = LinkedVector3Field(position, m_DimensionsContent, toggleContent, dimensions, ref isUniformScale,
                    m_InitialDimension, 0, ref axisModified, m_DimensionsProperty, m_IsUniformScaleProperty);

                dimensions = m_DimensionsProperty.vector3Value;

                if (wasUniformScale != isUniformScale && isUniformScale)
                    m_InitialDimension = dimensions != Vector3.zero ? dimensions : Vector3.one;

                EditorGUILayout.PropertyField(m_CullingMaskProperty, EditorGUIBridge.TextContent("Culling Mask"));

                if (changed.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    if (Application.isPlaying && PolySpatialSettings.instance.EnablePolySpatialRuntime)
                    {
                        var volumeCamera = (VolumeCamera)target;
                        volumeCamera.UpdateBoundedCullingCamera();
                        // unbounded update will happen on the next host camera sync
                    }
                }
            }
        }

        // The following methods are modified/adapted versions of methods from trunk scripts: EditorGUI & ConstrainProportionTransformScale
        // The "linked-scaling" functionality in trunk is hardcoded to work only with rect/transform "Scale" property.
        // The comments inside of these methods are copied from the original scripts.
        static Vector3 DoScaleProportions(Vector3 value, Vector3 previousValue, Vector3 initialValue, ref int axisModified)
        {
            float ratio = 1;
            var ratioChanged = false;

            if (previousValue != value)
            {
                // Check which axis was modified and set locked fields and ratio
                //AxisModified values [-1;2] : [none, x, y, z]
                // X axis
                ratio = SetRatio(value.x, previousValue.x, initialValue.x);
                axisModified = ratio != 1 || !Mathf.Approximately(value.x, previousValue.x) ? 0 : -1;
                // Y axis
                if (axisModified == -1)
                {
                    ratio = SetRatio(value.y, previousValue.y, initialValue.y);
                    axisModified = ratio != 1 || !Mathf.Approximately(value.y, previousValue.y) ? 1 : -1;
                }
                // Z axis
                if (axisModified == -1)
                {
                    ratio = SetRatio(value.z, previousValue.z, initialValue.z);
                    axisModified = ratio != 1 || !Mathf.Approximately(value.z, previousValue.z) ? 2 : -1;
                }

                ratioChanged = true;
            }

            return ratioChanged ? GetVector3WithRatio(initialValue, ratio) : value;
        }

        static float SetRatio(float value, float previousValue, float initialValue)
        {
            return Mathf.Approximately(value, previousValue) ? 1 : Mathf.Approximately(initialValue, 0) ? 0 : value / initialValue;
        }

        static Vector3 GetVector3WithRatio(Vector3 vector, float ratio)
        {
            //If there are any fields with the same values, use already precalculated values.
            var xValue = vector.x * ratio;
            var yValue = vector.y * ratio;

            return new Vector3(
                xValue,
                Mathf.Approximately(vector.y, vector.x) ? xValue : yValue,
                Mathf.Approximately(vector.z, vector.x) ? xValue : Mathf.Approximately(vector.z, vector.y) ? yValue : vector.z * ratio
            );
        }

        static Vector3 LinkedVector3Field(Rect position, GUIContent label,  GUIContent toggleContent, Vector3 value, ref bool isUniformScale,
            Vector3 initialScale, uint mixedValues, ref int axisModified, SerializedProperty vectorProperty = null, SerializedProperty isUniformScaleProperty = null)
        {
            var fullLabelRect = position;

            EditorGUIBridge.BeginChangeCheck();

            if (isUniformScaleProperty != null)
                EditorGUIBridge.BeginPropertyInternal(fullLabelRect, label, isUniformScaleProperty);

            var scalePropertyId = -1;
            if (vectorProperty != null)
            {
                label = EditorGUIBridge.BeginPropertyInternal(position, label, vectorProperty);
                scalePropertyId = GUIUtility.keyboardControl;
            }

            var copiedProperty = vectorProperty?.Copy();
            var toggle = EditorStyles.toggle.CalcSize(GUIContent.none);
            var id = GUIUtility.GetControlID(k_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUIBridge.MultiFieldPrefixLabel(position, id, label, 3, toggle.x + EditorGUIBridge.DefaultSpacing, false);
            var toggleRect = position;
            toggleRect.width = toggle.x;
            var toggleOffset = toggleRect.width + EditorGUIBridge.DefaultSpacing;
            toggleRect.x -= toggleOffset;
            toggle.x -= toggleOffset;

            // In case we have a background overlay, make sure constrain proportions toggle won't be affected
            var currentColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            var previousProportionalScale = isUniformScale;
            isUniformScale = GUI.Toggle(toggleRect, isUniformScale, toggleContent, Styles.LinkButton);

            if (isUniformScaleProperty != null && previousProportionalScale != isUniformScale)
                isUniformScaleProperty.boolValue = isUniformScale;

            GUI.backgroundColor = currentColor;

            position.x += toggle.x + EditorGUIBridge.DefaultSpacing;
            position.width -= toggle.x + EditorGUIBridge.DefaultSpacing;
            position.height = EditorGUIBridge.SingleLineHeight;

            if (isUniformScaleProperty != null)
                EditorGUIBridge.EndProperty();

            if (vectorProperty != null)
            {
                // Note: due to how both the scale + constrainScale property drawn and handled in a custom fashion, the lastcontrolId never correspond
                // to the scaleProperty. Also s_PendingPropertyKeyboardHandling is nullifed by the constrainScale property.
                // Make it work for now but I feel this whole system is super brittle.
                // This will be hopefully fixed up when we use uitk to create these editors.

                var lastId = EditorGUIBridge.LastControlID;
                EditorGUIBridge.LastControlID = scalePropertyId;
                EditorGUIBridge.EndProperty();
                EditorGUIBridge.LastControlID = lastId;
            }

            var newValue = LinkedVector3Field(position, value, isUniformScale, mixedValues, initialScale, ref axisModified, copiedProperty);
            return newValue;
        }

        // Make an X, Y & Z field for entering a [[Vector3]].
        static Vector3 LinkedVector3Field(Rect position, Vector3 value, bool isUniformScale, uint mixedValues, Vector3 initialValue, ref int axisModified, SerializedProperty property = null)
        {
            var valueAfterChangeCheck = value;
            k_Vector3Floats[0] = value.x;
            k_Vector3Floats[1] = value.y;
            k_Vector3Floats[2] = value.z;
            position.height = EditorGUIBridge.SingleLineHeight;
            const float kPrefixWidthOffset = 3.65f;

            var labels = k_XYZLabels.Value;
            LockingMultiFloatFieldInternal(position, isUniformScale, mixedValues, labels, k_Vector3Floats,
                new [] {initialValue.x, initialValue.y, initialValue.z}, property,
                EditorGUIBridge.CalcPrefixLabelWidth(labels[0]) + kPrefixWidthOffset);

            if (EditorGUIBridge.EndChangeCheck())
            {
                valueAfterChangeCheck.x = k_Vector3Floats[0];
                valueAfterChangeCheck.y = k_Vector3Floats[1];
                valueAfterChangeCheck.z = k_Vector3Floats[2];
            }

            return isUniformScale? DoScaleProportions(valueAfterChangeCheck, value, initialValue, ref axisModified) : valueAfterChangeCheck;
        }

        static void LockingMultiFloatFieldInternal(Rect position, bool locked, uint mixedValues, GUIContent[] subLabels, float[] values,
            float[] initialValues = null, SerializedProperty property = null, float prefixLabelWidth = -1)
        {
            var eCount = values.Length;
            var w = (position.width - (eCount - 1) * EditorGUIBridge.SpacingSubLabel) / eCount;
            var nr = new Rect(position) {width = w};
            var t = EditorGUIUtility.labelWidth;
            var l = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var guiEnabledState = GUI.enabled;
            var hasMixedValues = mixedValues != 0;

            initialValues ??= values;

            var mixedValueState = EditorGUI.showMixedValue;

            for (int i = 0; i < initialValues.Length; i++)
            {
                if (property != null)
                    property.Next(true);

                EditorGUIUtility.labelWidth = prefixLabelWidth > 0 ? prefixLabelWidth : EditorGUIBridge.CalcPrefixLabelWidth(subLabels[i]);

                if (guiEnabledState)
                {
                    if (locked)
                    {
                        // If initial field value is 0, it must be locked not to break proportions
                        GUI.enabled = !Mathf.Approximately(initialValues[i], 0) && property != null;
                    }
                    else
                    {
                        GUI.enabled = true;
                    }
                }

                if (property != null)
                {
                    EditorGUI.PropertyField(nr, property, subLabels[i]);
                    values[i] = property.floatValue;
                }
                else
                {
                    values[i] = EditorGUI.FloatField(nr, subLabels[i], values[i]);
                }

                if (hasMixedValues)
                    EditorGUI.showMixedValue = false;

                nr.x += w + EditorGUIBridge.SpacingSubLabel;
            }
            GUI.enabled = guiEnabledState;
            EditorGUI.showMixedValue = mixedValueState;
            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;
        }
    }
}
