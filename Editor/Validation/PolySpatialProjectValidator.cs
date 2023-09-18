using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that creates validation rules for the PolySpatial project.
    /// </summary>
    [InitializeOnLoad]
    static class PolySpatialProjectValidator
    {
        static PolySpatialProjectValidator()
        {
            // Delay the initialization to allow AssetDatabase.FindAssets to work properly in a clean checkout (LXR-2335)
            EditorApplication.delayCall += Initialize;
        }

        static void Initialize()
        {
            var rules = new List<BuildValidationRule>();
            rules.Add(CreatePolySpatialLayerExistRule());
            rules.Add(CreateCollisionMatrixRule());

#if POLYSPATIAL_INTERNAL
            BuildValidator.AddRules(BuildTargetGroup.Standalone, rules);
            BuildValidator.AddRules(BuildTargetGroup.Android, rules);
#endif

            BuildValidator.AddRules(BuildTargetGroup.VisionOS, rules);
        }

        static BuildValidationRule CreatePolySpatialLayerExistRule()
        {
            return new BuildValidationRule()
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, "LayerExist"),
                Message = $"The <b>{VolumeCamera.PolySpatialLayerName}</b> physics layer does not exist.",
                CheckPredicate = () => LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName) != -1,
                FixIt = () =>
                {
                    var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                    var quantumLayerIndex= PolySpatialUnityBackend.AddLayer(tagManager, PolySpatialCore.PolySpatialLayerName);
                    tagManager.ApplyModifiedProperties();

                    if (quantumLayerIndex == -1)
                    {
                        DisplayNoLayersAvailableDialog();
                    }
                    else
                    {
                        DisplayAddPolySpatialLayerDialog(quantumLayerIndex);
                        PolySpatialUnityBackend.IgnoreLayerCollision(quantumLayerIndex);
                    }
                },
                FixItMessage = $"Add a <b>{VolumeCamera.PolySpatialLayerName}</b> physics layer to the project."
            };
        }

        static void DisplayNoLayersAvailableDialog()
        {
            if (EditorUtility.DisplayDialog(
                $"{VolumeCamera.PolySpatialLayerName} Layer Error",
                $"There were no available layer slots. The {VolumeCamera.PolySpatialLayerName} layer was not added. " +
                $"Delete or rename an existing layer to `{VolumeCamera.PolySpatialLayerName}`.",
                "Ok",
                "Cancel"))
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
        }

        static void DisplayAddPolySpatialLayerDialog(int layerIndex)
        {
            if (EditorUtility.DisplayDialog(
                $"{VolumeCamera.PolySpatialLayerName} Layer Added",
                $"The {VolumeCamera.PolySpatialLayerName} layer was added on 'User Layer {layerIndex}' (the first available physics layer index).",
                "Go to Layer Settings",
                "Ok"))
                SettingsService.OpenProjectSettings("Project/Tags and Layers");
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
                FixIt = () =>
                {
                    var quantumLayer = LayerMask.NameToLayer(VolumeCamera.PolySpatialLayerName);
                    if (quantumLayer == -1)
                        return;

                    PolySpatialUnityBackend.IgnoreLayerCollision(quantumLayer);
                },
                FixItMessage = $"Disable any physics layer collision with the <b>{VolumeCamera.PolySpatialLayerName}</b> layer."
            };
        }
    }
}
