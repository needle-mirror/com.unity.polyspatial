using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Capabilities.Editor;
using Unity.XR.CoreUtils.Editor;
using Unity.PolySpatial.Internals.Capabilities;
using UnityEngine;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// Class that validates if a <see cref="Transform"/>'s scale has all positive values.
    /// </summary>
    class TransformScaleRuleCreator : IComponentRuleCreator, IPropertyValidator
    {
        const string k_Message = "The <b>Scale</b> property cannot have negative values.";
        const string k_FixItMessage = "Switch negative values to positive.";

        /// <inheritdoc />
        public void GetTypesToTrack(Component component, List<Type> types)
        {
            types.Add(component.GetType());
        }

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var instanceID = component.GetInstanceID();
            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, component.GetType().Name),
                Message = k_Message,
                FixItAutomatic = true,
                CheckPredicate = () =>
                {
                    var transform = EditorUtility.InstanceIDToObject(instanceID) as Transform;
                    if (transform == null)
                        return true;

                    var scale = transform.localScale;
                    return (scale.x >= 0 && scale.y >= 0 && scale.z >= 0);
                },
                FixIt = () =>
                {
                    var transform = EditorUtility.InstanceIDToObject(instanceID) as Transform;
                    if (transform == null)
                        return;

                    var scale = transform.localScale;
                    Undo.RecordObject(transform,"Make Scale Positive");
                    transform.localScale = new Vector3(Math.Abs(scale.x), Math.Abs(scale.y), Math.Abs(scale.z));
                    EditorUtility.SetDirty(transform);
                },
                FixItMessage = k_FixItMessage,
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            createdRules.Add(rule);
        }
    }
}
