using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEditor.PolySpatial.Utilities;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PolySpatial
{
    [CustomEditor(typeof(PolySpatialSettings))]
    class PolySpatialSettingsEditor : Editor
    {
        static readonly GUIContent k_ColliderSyncLayerMaskContent = new GUIContent("PolySpatial Collider Layer Mask");
        static readonly GUIContent k_HidePolySpatialPreviewObjectsInSceneContent = new GUIContent("Hide PolySpatial Preview Objects In Scene");
        
        static readonly HashSet<Type> k_TrackerTypes = new();
        static readonly string[] k_TrackerTypeNames;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<string> k_InactiveTrackers = new();

        SerializedProperty m_EnablePolySpatialRuntimeProperty;
        GUIContent m_EnablePolySpatialRuntimeContent;

        SerializedProperty m_DefaultVolumeCameraConfigurationProperty;
        SerializedProperty m_AutoCreateVolumeCameraProperty;
        GUIContent m_AutoCreateVolumeCameraContent;
        SerializedProperty m_ColliderSyncLayerMaskProperty;
        SerializedProperty m_ParticleModeProperty;

        SerializedProperty m_EnableStatisticsProperty;
        SerializedProperty m_TransmitDebugInfoProperty;
        SerializedProperty m_HidePolySpatialPreviewObjectsInScene;

        SerializedProperty m_DisableTrackingMaskProperty;
        SerializedProperty m_DisabledTrackersProperty;
        ReorderableList m_DisabledTrackersList;
        SavedBool m_TrackersFoldoutState;

        SerializedProperty m_ForceValidationForCurrentBuildTargetProperty;

        static PolySpatialSettingsEditor()
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

        void OnEnable()
        {
            m_EnablePolySpatialRuntimeProperty = serializedObject.FindProperty("m_EnablePolySpatialRuntime");
            m_EnablePolySpatialRuntimeContent = new GUIContent("Enable PolySpatial Runtime");

            m_DefaultVolumeCameraConfigurationProperty = serializedObject.FindProperty("m_DefaultVolumeCameraConfiguration");
            m_AutoCreateVolumeCameraProperty = serializedObject.FindProperty("m_AutoCreateVolumeCamera");
            m_AutoCreateVolumeCameraContent = new GUIContent("Auto-Create Volume Camera", m_AutoCreateVolumeCameraProperty.tooltip);
            m_ColliderSyncLayerMaskProperty = serializedObject.FindProperty("m_ColliderSyncLayerMask");
            m_ParticleModeProperty = serializedObject.FindProperty("m_ParticleMode");

            m_EnableStatisticsProperty = serializedObject.FindProperty("m_EnableStatistics");
            m_TransmitDebugInfoProperty = serializedObject.FindProperty("m_TransmitDebugInfo");
            m_HidePolySpatialPreviewObjectsInScene = serializedObject.FindProperty("m_HidePolySpatialPreviewObjectsInScene");

            m_DisableTrackingMaskProperty = serializedObject.FindProperty("m_DisableTrackingMask");
            m_DisabledTrackersProperty = serializedObject.FindProperty("m_DisabledTrackers");
            m_DisabledTrackersList = new ReorderableList(serializedObject, m_DisabledTrackersProperty, true, true, true, true)
            {
                drawHeaderCallback = DrawDisabledTrackersHeader,
                drawElementCallback = DrawDisabledTrackersItem,
                onAddCallback = OnAddDisabledTrackers
            };
            m_TrackersFoldoutState = new SavedBool("PolySpatialSettingsEditor.TrackersFoldoutState", true);

            m_ForceValidationForCurrentBuildTargetProperty = serializedObject.FindProperty("m_ForceValidationForCurrentBuildTarget");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_EnablePolySpatialRuntimeProperty.boolValue = EditorGUILayout.BeginToggleGroup(m_EnablePolySpatialRuntimeContent, m_EnablePolySpatialRuntimeProperty.boolValue);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_DefaultVolumeCameraConfigurationProperty);
                if (m_DefaultVolumeCameraConfigurationProperty.objectReferenceValue == null)
                {
                    m_DefaultVolumeCameraConfigurationProperty.objectReferenceValue =
                        Resources.Load<VolumeCameraConfiguration>("Default Unbounded Configuration");
                }
                EditorGUILayout.PropertyField(m_AutoCreateVolumeCameraProperty, m_AutoCreateVolumeCameraContent);
                EditorGUILayout.PropertyField(m_ColliderSyncLayerMaskProperty, k_ColliderSyncLayerMaskContent);
                EditorGUILayout.PropertyField(m_ParticleModeProperty);

                EditorGUILayout.PropertyField(m_EnableStatisticsProperty);
                EditorGUILayout.PropertyField(m_TransmitDebugInfoProperty);
                // Due to CamelCase drawing in the UI for serialized properties we have to manually override the property label to write PolySpatial instead of "Poly Spatial"
                EditorGUILayout.PropertyField(m_HidePolySpatialPreviewObjectsInScene, k_HidePolySpatialPreviewObjectsInSceneContent);
                EditorGUILayout.PropertyField(m_DisableTrackingMaskProperty);
            }

            EditorGUILayout.Space();
            m_DisabledTrackersList.DoLayoutList();
            DrawRuntimeTrackers();
            EditorGUILayout.Space();

            EditorGUILayout.EndToggleGroup();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_ForceValidationForCurrentBuildTargetProperty);

                if (check.changed)
                {
                    EditorUtility.RequestScriptReload();
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);
        }

        void DrawRuntimeTrackers()
        {
            m_TrackersFoldoutState.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_TrackersFoldoutState.value, "Runtime Trackers");
            if (!m_TrackersFoldoutState.value)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                if (Application.isPlaying)
                {
                    var trackers = PolySpatialCore.UnitySimulation?.Tracker?.Trackers;
                    if (trackers == null)
                    {
                        EditorGUILayout.HelpBox("No Active Trackers.", MessageType.Info);
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
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField($"Inactive Trackers ({inactiveTrackersCount})", EditorStyles.boldLabel);
                            foreach (var inactiveTracker in k_InactiveTrackers)
                            {
                                EditorGUILayout.LabelField(inactiveTracker);
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Trackers are only available at runtime.", MessageType.Info);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawDisabledTrackersHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, m_DisabledTrackersProperty.displayName);
        }

        void DrawDisabledTrackersItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = m_DisabledTrackersProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, GUIContent.none);
        }

        void OnAddDisabledTrackers(ReorderableList list)
        {
            var disabledTrackers = PolySpatialSettings.instance.DisabledTrackers;
            var menu = new GenericMenu();
            foreach (var trackerTypeName in k_TrackerTypeNames)
            {
                if (disabledTrackers.Contains(trackerTypeName))
                    menu.AddDisabledItem(new GUIContent(trackerTypeName));
                else
                    menu.AddItem(new GUIContent(trackerTypeName), false, () => AddDisabledTracker(list, trackerTypeName));
            }
            menu.ShowAsContext();
        }

        void AddDisabledTracker(ReorderableList list, string trackerTypeName)
        {
            list.serializedProperty.arraySize++;
            var newElement = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
            newElement.stringValue = trackerTypeName;

            list.serializedProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(PolySpatialSettings.instance);
        }
    }
}
