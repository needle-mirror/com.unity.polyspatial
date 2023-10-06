using Unity.PolySpatial;
using UnityEditor.IMGUI.Controls;
using UnityEditor.PolySpatial.InternalBridge;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Internals
{
    [CustomEditor(typeof(VolumeCamera))]
    class VolumeCameraEditor : Editor
    {
        const int k_MinInspectorWidth = 212;
        static readonly Color k_BoundsHandleColor = new Color(1, 1, 1, 0.7f);

        SerializedProperty m_IsUniformScaleProperty;
        SerializedProperty m_DimensionsProperty;
        SerializedProperty m_CullingMaskProperty;
        SerializedProperty m_ConfigurationProperty;

        BoxBoundsHandle m_BoundsHandle = new();
        GUIContent m_DimensionsContent;
        Vector3 m_InitialDimension, m_PreviousDimension;
        Editor m_VolumeCameraConfigEditor;

        void OnEnable()
        {
            m_IsUniformScaleProperty = serializedObject.FindProperty("m_IsUniformScale");
            m_DimensionsProperty = serializedObject.FindProperty("m_Dimensions");
            m_CullingMaskProperty = serializedObject.FindProperty("CullingMask");
            m_ConfigurationProperty = serializedObject.FindProperty("m_OutputConfiguration");

            m_InitialDimension = m_DimensionsProperty.vector3Value;

            m_BoundsHandle.wireframeColor = k_BoundsHandleColor;
            m_BoundsHandle.handleColor = k_BoundsHandleColor;

            m_DimensionsContent = new GUIContent(m_DimensionsProperty.displayName, m_DimensionsProperty.tooltip);
        }

        void OnDisable()
        {
            if (m_VolumeCameraConfigEditor != null)
            {
                DestroyImmediate(m_VolumeCameraConfigEditor);
                m_VolumeCameraConfigEditor = null;
            }
        }

        void OnSceneGUI()
        {
            var volumeCamera = (VolumeCamera)target;
            if (volumeCamera == null || volumeCamera.OutputMode != VolumeCamera.PolySpatialVolumeCameraMode.Bounded)
                return;

            var initialDimensions = m_DimensionsProperty.vector3Value == Vector3.zero ? Vector3.one : m_DimensionsProperty.vector3Value;
            m_BoundsHandle.size = initialDimensions;
            m_BoundsHandle.center = volumeCamera.transform.position;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                m_BoundsHandle.DrawHandle();

                if (check.changed)
                {
                    if (m_IsUniformScaleProperty.boolValue)
                    {
                        // If uniform scale is enabled, we need to make sure that the user is only able to scale the volume uniformly.
                        var scale = 1f;
                        var deltaSize = m_BoundsHandle.size - initialDimensions;
                        if (deltaSize.x != 0 && initialDimensions.x != 0)
                            scale = m_BoundsHandle.size.x / initialDimensions.x;
                        else if (deltaSize.y != 0 && initialDimensions.y != 0)
                            scale = m_BoundsHandle.size.y / initialDimensions.y;
                        else if (deltaSize.z != 0 && initialDimensions.z != 0)
                            scale = m_BoundsHandle.size.z / initialDimensions.z;

                        m_BoundsHandle.size = initialDimensions * scale;
                    }

                    Undo.RecordObject(target, "Changed Volume Dimensions");
                    m_DimensionsProperty.vector3Value = m_BoundsHandle.size;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                // Adjust label to avoid content spilling over
                if (!EditorGUIUtility.wideMode)
                {
                    EditorGUIUtility.wideMode = true;
                    EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - k_MinInspectorWidth;
                }

                EditorGUILayout.PropertyField(m_CullingMaskProperty, EditorGUIBridge.TextContent("Culling Mask"));

                var isUniformScale = m_IsUniformScaleProperty.boolValue;
                var wasUniformScale = isUniformScale;
                var axisModified = -1;
                var toggleContent = EditorGUIUtility.TrTextContent("", (isUniformScale ? "Disable" : "Enable") + " constrained proportions");
                var position = EditorGUILayout.GetControlRect(true);

                var isUnboundedCameraMode = m_ConfigurationProperty.objectReferenceValue is VolumeCameraConfiguration volumeConfiguration && volumeConfiguration != null &&
                                          volumeConfiguration.Mode == VolumeCamera.PolySpatialVolumeCameraMode.Unbounded;
                Vector3 dimensions;
                using (new EditorGUI.DisabledScope(isUnboundedCameraMode))
                {
                    dimensions = PolySpatialEditorGUIUtils.LinkedVector3Field(position, m_DimensionsContent,
                        toggleContent, m_DimensionsProperty.vector3Value, ref isUniformScale, m_InitialDimension, 0,
                        ref axisModified, m_DimensionsProperty, m_IsUniformScaleProperty);
                }

                if (wasUniformScale != isUniformScale && isUniformScale)
                    m_InitialDimension = dimensions != Vector3.zero ? dimensions : Vector3.one;

                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.ObjectField(m_ConfigurationProperty, typeof(VolumeCameraConfiguration),
                        EditorGUIBridge.TextContent("Output Configuration"));

                    if (m_ConfigurationProperty.objectReferenceValue != null)
                    {
                        var config = (VolumeCameraConfiguration) m_ConfigurationProperty.objectReferenceValue;
                        CreateCachedEditor(config, null, ref m_VolumeCameraConfigEditor);
                        if (m_VolumeCameraConfigEditor is VolumeCameraConfigurationEditor configEditor)
                            configEditor.ShowValidationMessage = false;

                        using (new EditorGUI.IndentLevelScope())
                        {
                            m_VolumeCameraConfigEditor.OnInspectorGUI();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("This volume camera does not have a configuration assigned. " +
                                                "The initial configuration specified in XR Plug-in Management settings for the platform will be used.",
                            MessageType.Info);
                    }
                }

                if (changed.changed)
                {
                    m_DimensionsProperty.vector3Value = dimensions;
                    serializedObject.ApplyModifiedProperties();
                    if (Application.isPlaying && PolySpatialSettings.instance.EnablePolySpatialRuntime)
                    {
                        var volumeCamera = (VolumeCamera)target;
                        volumeCamera.UpdateConfiguration();
                    }
                }
            }
        }
    }
}
