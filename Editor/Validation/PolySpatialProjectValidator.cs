using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.PolySpatial.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that creates validation rules for the PolySpatial project.
    /// </summary>
    static class PolySpatialProjectValidator
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            var rules = new List<BuildValidationRule>();
            rules.Add(CreatePolySpatialLayerExistRule());
            rules.Add(CreateCollisionMatrixRule());

            BuildValidator.AddRules(BuildTargetGroup.Standalone, rules);
            BuildValidator.AddRules(BuildTargetGroup.iOS, rules);
            BuildValidator.AddRules(BuildTargetGroup.Android, rules);
        }

        static BuildValidationRule CreatePolySpatialLayerExistRule()
        {
            return new BuildValidationRule()
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, "LayerExist"),
                Message = $"The <b>{VolumeCamera.PolySpatialLayerName}</b> physics layer is configured.",
                CheckPredicate = () => LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName) != -1,
                FixIt = () =>
                {
                    var availableLayer = -1;
                    for (var layer = 31; layer > 5; layer--)
                    {
                        var layerName = LayerMask.LayerToName(layer);
                        if (!string.IsNullOrEmpty(layerName))
                            continue;

                        availableLayer = layer;
                        break;
                    }

                    if (availableLayer == -1 || !DisplayAddPolySpatialLayerDialog(availableLayer))
                    {
                        SettingsService.OpenProjectSettings("Project/Tags and Layers");
                        return;
                    }

                    AddOrReplaceLayer(VolumeCamera.PolySpatialLayerName, availableLayer);
                    IgnorePolySpatialLayerCollision();
                },
                FixItMessage = $"Add a <b>{VolumeCamera.PolySpatialLayerName}</b> physics layer to the project."
            };
        }

        static bool DisplayAddPolySpatialLayerDialog(int layerIndex)
        {
            return EditorUtility.DisplayDialog(
                $"Add {VolumeCamera.PolySpatialLayerName} Layer",
                $"The {VolumeCamera.PolySpatialLayerName} layer will be set at index '{layerIndex}' (the last available physics layer index).",
                "Automatically Add",
                "Cancel");
        }

        static void AddOrReplaceLayer(string layerName, int layerIndex)
        {
            if (string.IsNullOrEmpty(layerName) || layerIndex <= 5 || layerIndex > 31)
                return;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
                return;

            var serializedObject = new SerializedObject(assets[0]);
            var layersProperty = serializedObject.FindProperty("layers");
            var layerProperty = layersProperty.GetArrayElementAtIndex(layerIndex);
            layerProperty.stringValue = layerName;
            serializedObject.ApplyModifiedProperties();
        }

        static BuildValidationRule CreateCollisionMatrixRule()
        {
            return new BuildValidationRule()
            {
                IsRuleEnabled = () => LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName) != -1 && CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, "LayerCollision"),
                Message = $"The <b>{VolumeCamera.PolySpatialLayerName}</b> physics layer should not collide with any layer.",
                CheckPredicate = () =>
                {
                    var polyspatialLayer = LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName);
                    if (polyspatialLayer == -1)
                        return false;

                    for (var layer = 0; layer < 32; layer++)
                    {
                        var layerName = LayerMask.LayerToName(layer);
                        if (string.IsNullOrEmpty(layerName))
                            continue;

                        if(!Physics.GetIgnoreLayerCollision(polyspatialLayer, layer))
                            return false;
                    }

                    return true;
                },
                FixIt = IgnorePolySpatialLayerCollision,
                FixItMessage = $"Disable any physics layer collision with the <b>{VolumeCamera.PolySpatialLayerName}</b> layer."
            };
        }

        static void IgnorePolySpatialLayerCollision()
        {
            var polyspatialLayer = LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName);
            if (polyspatialLayer == -1)
                return;

            for (var layer = 0; layer < 32; layer++)
            {
                var layerName = LayerMask.LayerToName(layer);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                if (!Physics.GetIgnoreLayerCollision(polyspatialLayer, layer))
                    Physics.IgnoreLayerCollision(polyspatialLayer, layer, true);
            }
        }
    }
}
