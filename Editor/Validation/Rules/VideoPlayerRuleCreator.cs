using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals.Capabilities;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using UnityEngine;
using UnityEngine.Video;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that validates if a <see cref="VideoPlayer"/>'s render mode is supported by PolySpatial.
    /// </summary>
    class VideoPlayerRuleCreator : IComponentRuleCreator, IPropertyValidator
    {
        IPropertyValidator m_PropertyValidatorImplementation;

        const string k_MessageRenderTex = "The <b>{0}</b> profile(s) do not support the following render mode for the <b>Video Player</b>: {1}.";
        const string k_MessageMatOverride = "The <b>Material Override</b> render mode is not supported for the <b>Video Player</b>. Please use the <b>VisionOS Video Component</b> instead.";
        const string k_FixItMsgRenderTex = "Set render mode to <b>Render Texture</b>.";
        const string k_FixItMsgMatOverride = "Add a <b>PolySpatial Video Component</b> to this GameObject.";

        /// <inheritdoc />
        public void GetTypesToTrack(Component component, List<Type> types)
        {
            types.Add(typeof(VideoPlayer));
        }

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var instanceID = component.GetInstanceID();
            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                FixItAutomatic = true,
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            rule.CheckPredicate = () =>
            {
                var videoPlayer = EditorUtility.InstanceIDToObject(instanceID) as VideoPlayer;
                if (videoPlayer == null || !videoPlayer.enabled)
                    return true;

                var renderMode = videoPlayer.renderMode;

                if (renderMode == VideoRenderMode.MaterialOverride)
                {
                    rule.Message = k_MessageMatOverride;
                    rule.FixItMessage = k_FixItMsgMatOverride;
                }
                else
                {
                    rule.Message = string.Format(k_MessageRenderTex, PolySpatialSceneValidator.CachedCapabilityProfileNames, renderMode.ToString());
                    rule.FixItMessage = k_FixItMsgRenderTex;
                }

                return renderMode == VideoRenderMode.RenderTexture;
            };

            rule.FixIt = () =>
            {
                var videoPlayer = EditorUtility.InstanceIDToObject(instanceID) as VideoPlayer;
                if (videoPlayer == null)
                    return;

                if (videoPlayer.renderMode == VideoRenderMode.MaterialOverride)
                {
                    Undo.SetCurrentGroupName("Fix Component");
                    var groupIndex = Undo.GetCurrentGroup();

                    if (videoPlayer.gameObject.GetComponent<VisionOSVideoComponent>() == null)
                        Undo.AddComponent<VisionOSVideoComponent>(videoPlayer.gameObject);

                    Undo.RecordObject(videoPlayer, "Fix Component");
                    videoPlayer.enabled = false;

                    Undo.CollapseUndoOperations(groupIndex);
                    return;
                }

                Undo.RecordObject(videoPlayer, "Fix Component");
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            };

            createdRules.Add(rule);
        }
    }
}
