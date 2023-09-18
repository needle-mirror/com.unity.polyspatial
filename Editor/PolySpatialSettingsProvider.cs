using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PolySpatial
{
    internal class PolySpatialSettingsProvider : SettingsProvider
    {
        static readonly HashSet<Type> k_TrackerTypes = new();
        static readonly string[] k_TrackerTypeNames;

        SerializedObject m_SerializedObject;

        SerializedProperty m_EnablePolySpatialRuntimeProperty;
        SerializedProperty m_ForceLinkPolySpatialRuntimeProperty;
        SerializedProperty m_PolySpatialRecordingModeProperty;
        SerializedProperty m_PolySpatialRecordingPathProperty;
        SerializedProperty m_PolySpatialNetworkingModeProperty;
        SerializedProperty m_ServerAddressesProperty;
        SerializedProperty m_EnableHostCameraControlProperty;
        SerializedProperty m_EnableClippingProperty;

        SerializedProperty m_EnableStatisticsProperty;
        SerializedProperty m_EnableTransformVerificationProperty;
        SerializedProperty m_EnableMacRealityKitPreviewInPlayMode;
        SerializedProperty m_EnableProgressiveMipStreamingProperty;
        SerializedProperty m_MaxMipByteSizePerCycleProperty;
        SerializedProperty m_EnableDefaultVolumeCamera;
        SerializedProperty m_HidePolySpatialPreviewObjectsInScene;
        SerializedProperty m_RuntimeFlags;
        SerializedProperty m_DisabledTrackers;

        SerializedProperty m_ColliderSyncLayerMaskProperty;

        SerializedProperty m_AdditionalTextureFormatsProperty;

        SerializedProperty m_IgnoredScenesListProperty;
        SerializedProperty m_TransmitDebugInfoProperty;
        ReorderableList m_IgnoredScenesReorderableList;
        int m_SelectedTracker;

        SerializedProperty m_EnableInEditorPreviewProperty;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<string> k_InactiveTrackers = new();

        static PolySpatialSettingsProvider()
        {
            var trackerTypeNames = new List<string>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<IUnityObjectTracker>())
            {
                // GameObjectTracker cannot be disabled
                if (type == typeof(GameObjectTracker))
                    continue;

                if (!PolySpatialUnityTracker.IsValidTrackerType(type))
                    continue;

                k_TrackerTypes.Add(type);
                trackerTypeNames.Add(type.FullName);
            }

            k_TrackerTypeNames = trackerTypeNames.ToArray();
        }

        /// <summary>
        /// Settings constructor called when we need a new settings instance.
        /// </summary>
        /// <param name="path">Path of the settings window within the settings UI.</param>
        /// <param name="scopes">The scopes within which the settings are valid.</param>
        /// <param name="keywords">Keywords for search.</param>
        public PolySpatialSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        /// <inheritdoc/>>
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SerializedObject = new SerializedObject(PolySpatialSettings.instance);

            m_EnablePolySpatialRuntimeProperty = m_SerializedObject.FindProperty("EnablePolySpatialRuntime");
            m_EnableStatisticsProperty = m_SerializedObject.FindProperty("EnableStatistics");
            m_EnableTransformVerificationProperty = m_SerializedObject.FindProperty("EnableTransformVerification");
            m_EnableDefaultVolumeCamera = m_SerializedObject.FindProperty("EnableDefaultVolumeCamera");
            m_HidePolySpatialPreviewObjectsInScene = m_SerializedObject.FindProperty("HidePolySpatialPreviewObjectsInScene");

            m_EnableMacRealityKitPreviewInPlayMode = m_SerializedObject.FindProperty("EnableMacRealityKitPreviewInPlayMode");
            m_EnableProgressiveMipStreamingProperty = m_SerializedObject.FindProperty("EnableProgressiveMipStreaming");
            m_MaxMipByteSizePerCycleProperty = m_SerializedObject.FindProperty("MaxMipByteSizePerCycle");
            m_RuntimeFlags = m_SerializedObject.FindProperty("RuntimeFlags");
            m_DisabledTrackers = m_SerializedObject.FindProperty(nameof(PolySpatialSettings.DisabledTrackers));

            m_ColliderSyncLayerMaskProperty = m_SerializedObject.FindProperty("ColliderSyncLayerMask");
            m_AdditionalTextureFormatsProperty = m_SerializedObject.FindProperty("m_AdditionalTextureFormats");

            m_PolySpatialRecordingModeProperty = m_SerializedObject.FindProperty("PolySpatialRecordingMode");
            m_PolySpatialRecordingPathProperty = m_SerializedObject.FindProperty("RecordingPath");
            m_EnableInEditorPreviewProperty = m_SerializedObject.FindProperty("EnableInEditorPreview");

            m_TransmitDebugInfoProperty = m_SerializedObject.FindProperty("m_TransmitDebugInfo");
#if POLYSPATIAL_INTERNAL
            m_ForceLinkPolySpatialRuntimeProperty = m_SerializedObject.FindProperty("ForceLinkPolySpatialRuntime");
            m_PolySpatialNetworkingModeProperty = m_SerializedObject.FindProperty("m_PolySpatialNetworkingMode");
            m_ServerAddressesProperty = m_SerializedObject.FindProperty("m_SerializedServerAddresses");
            m_EnableHostCameraControlProperty = m_SerializedObject.FindProperty("m_EnableHostCameraControl");
            m_EnableClippingProperty = m_SerializedObject.FindProperty("m_EnableClipping");
            m_IgnoredScenesListProperty = m_SerializedObject.FindProperty("m_SerializedIgnoredScenePaths");
            m_IgnoredScenesReorderableList = new ReorderableList(m_SerializedObject, m_IgnoredScenesListProperty, true, true, true, true)
            {
                drawElementCallback = DrawIgnoredSceneItem,
                drawHeaderCallback = DrawIgnoredSceneHeader
            };
#endif
        }

        /// <inheritdoc/>>
        public override void OnGUI(string searchContext)
        {
            bool isMacEditor = false;
#if UNITY_EDITOR_OSX
            isMacEditor = true;
#endif
            using (CreateSettingsWindowGUIScope())
            {
                m_SerializedObject.Update();

                m_EnablePolySpatialRuntimeProperty.boolValue = EditorGUILayout.BeginToggleGroup("Enable PolySpatial Runtime", m_EnablePolySpatialRuntimeProperty.boolValue);
                using (new EditorGUI.IndentLevelScope())
                {
                #if false
                    EditorGUILayout.PropertyField(m_PolySpatialRecordingModeProperty, new GUIContent("Recording Mode"));
                    using (new EditorGUI.IndentLevelScope())
                    {
                        switch ((PolySpatialSettings.RecordingMode)m_PolySpatialRecordingModeProperty.enumValueFlag)
                        {
                            case PolySpatialSettings.RecordingMode.Record:
                                EditorGUILayout.PropertyField(m_PolySpatialRecordingPathProperty, new GUIContent("Record Path"));
                                break;
                            case PolySpatialSettings.RecordingMode.Playback:
                                EditorGUILayout.PropertyField(m_PolySpatialRecordingPathProperty, new GUIContent("Playback Path"));
                                break;
                            default:
                                break;
                        }
                    }
                #endif
#if POLYSPATIAL_INTERNAL
                    EditorGUILayout.PropertyField(m_PolySpatialNetworkingModeProperty, new GUIContent("Networking Mode"));
                    using (new EditorGUI.IndentLevelScope())
                    {
                        switch ((PolySpatialSettings.NetworkingMode)m_PolySpatialNetworkingModeProperty.enumValueFlag)
                        {
                            case PolySpatialSettings.NetworkingMode.LocalAndClient:
                                EditorGUILayout.PropertyField(m_ServerAddressesProperty, new GUIContent("Server Addresses"));
                                break;
                        }

                        EditorGUILayout.PropertyField(m_EnableHostCameraControlProperty,
                            new GUIContent("Enable Host Camera Control",
                                "Host will control camera transform."));
                        EditorGUILayout.PropertyField(m_EnableClippingProperty,
                            new GUIContent("Enable Clipping Buffer",
                                "Clip apps to the bounds of their VolumeRenderer."));
                    }
#endif
                    EditorGUILayout.PropertyField(m_EnableStatisticsProperty);
                    EditorGUILayout.PropertyField(m_EnableTransformVerificationProperty);
                    EditorGUILayout.PropertyField(m_EnableDefaultVolumeCamera);
                    EditorGUILayout.PropertyField(m_HidePolySpatialPreviewObjectsInScene);
                    EditorGUILayout.PropertyField(m_EnableProgressiveMipStreamingProperty);
                    if (m_EnableProgressiveMipStreamingProperty.boolValue)
                        EditorGUILayout.PropertyField(m_MaxMipByteSizePerCycleProperty);

                    EditorGUI.BeginDisabledGroup(!isMacEditor);
                    EditorGUILayout.PropertyField(m_EnableMacRealityKitPreviewInPlayMode);
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.PropertyField(m_TransmitDebugInfoProperty);

                    var flags = (PolySpatialRuntimeFlags)m_RuntimeFlags.ulongValue;
                    flags = (PolySpatialRuntimeFlags)EditorGUILayout.EnumFlagsField("Runtime Flags", flags);
                    if (flags != (PolySpatialRuntimeFlags)m_RuntimeFlags.ulongValue)
                    {
                        m_RuntimeFlags.ulongValue = (ulong)flags;
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(m_ColliderSyncLayerMaskProperty, new GUIContent("PolySpatial Collider Layer Mask"));
                    EditorGUILayout.PropertyField(m_AdditionalTextureFormatsProperty);
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Add/Remove Disabled Trackers", EditorStyles.boldLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        m_SelectedTracker = EditorGUILayout.Popup(m_SelectedTracker, k_TrackerTypeNames);
                        var disabled = true;
                        var disabledTrackers = PolySpatialSettings.instance.DisabledTrackers;
                        if (disabledTrackers != null && m_SelectedTracker >= 0 && m_SelectedTracker <= k_TrackerTypeNames.Length)
                            disabled = disabledTrackers.Contains(k_TrackerTypeNames[m_SelectedTracker]);

                        using (new EditorGUI.DisabledScope(disabled))
                        {
                            if (GUILayout.Button("Add"))
                            {
                                m_DisabledTrackers.InsertArrayElementAtIndex(0);
                                m_DisabledTrackers.GetArrayElementAtIndex(0).stringValue = k_TrackerTypeNames[m_SelectedTracker];
                                m_SerializedObject.ApplyModifiedProperties();

                                // Select the next tracker to make it easier to add a bunch of trackers in a row
                                if (m_SelectedTracker < k_TrackerTypeNames.Length - 1)
                                    m_SelectedTracker++;
                            }
                        }
                    }

                    EditorGUILayout.PropertyField(m_DisabledTrackers);

#if POLYSPATIAL_INTERNAL
                    m_IgnoredScenesReorderableList.DoLayoutList();
#endif
                }

                if (Application.isPlaying)
                {
                    var trackers = PolySpatialCore.UnitySimulation?.Tracker?.Trackers;
                    if (trackers == null)
                    {
                        EditorGUILayout.LabelField("No Active Trackers", EditorStyles.boldLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Active Trackers ({trackers.Count})", EditorStyles.boldLabel);
                        foreach (var tracker in trackers)
                        {
                            EditorGUILayout.LabelField(tracker.GetType().FullName);
                        }

                        k_InactiveTrackers.Clear();
                        foreach (var trackerType in k_TrackerTypes)
                        {
                            var hasActiveTracker = false;
                            foreach (var tracker in trackers)
                            {
                                if (tracker.GetType() == trackerType)
                                {
                                    hasActiveTracker = true;
                                    break;
                                }
                            }

                            if (!hasActiveTracker)
                                k_InactiveTrackers.Add(trackerType.FullName);
                        }

                        var inactiveTrackersCount = k_InactiveTrackers.Count;
                        if (inactiveTrackersCount > 0)
                        {
                            EditorGUILayout.LabelField($"Inactive Trackers ({inactiveTrackersCount})", EditorStyles.boldLabel);
                            foreach (var inactiveTracker in k_InactiveTrackers)
                            {
                                EditorGUILayout.LabelField(inactiveTracker);
                            }
                        }
                    }
                }

                EditorGUILayout.EndToggleGroup();

#if POLYSPATIAL_INTERNAL
                m_ForceLinkPolySpatialRuntimeProperty.boolValue =
                    EditorGUILayout.ToggleLeft("Always Link PolySpatial Runtime", m_ForceLinkPolySpatialRuntimeProperty.boolValue);
#endif

                EditorGUILayout.PropertyField(m_EnableInEditorPreviewProperty);

                m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);
            }
        }

        void DrawIgnoredSceneHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Ignored Scenes");
        }

        void DrawIgnoredSceneItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = m_IgnoredScenesListProperty.GetArrayElementAtIndex(index);
            var oldScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(element.stringValue);

            EditorGUI.BeginChangeCheck();

            var newScene = EditorGUI.ObjectField(rect, oldScene, typeof(SceneAsset), false) as SceneAsset;

            if (EditorGUI.EndChangeCheck())
            {
                var newPath = AssetDatabase.GetAssetPath(newScene);
                element.stringValue = newPath;
            }
        }

        [SettingsProvider]
        static SettingsProvider CreatePolySpatialProjectSettingsProvider()
        {
            return new PolySpatialSettingsProvider("Project/PolySpatial", SettingsScope.Project);
        }

        private IDisposable CreateSettingsWindowGUIScope()
        {
            var unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            var type = unityEditorAssembly.GetType("UnityEditor.SettingsWindow+GUIScope");
            return Activator.CreateInstance(type) as IDisposable;
        }
    }
}
