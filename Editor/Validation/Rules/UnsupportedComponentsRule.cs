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
    /// Class that creates validation rules for unsupported components.
    /// </summary>
    sealed class UnsupportedComponentsRule : IComponentRuleCreator, IPropertyValidator
    {
        const string k_MessageFormat = "The <b>{0}</b> component is not supported by the <b>{1}</b> profile(s).";

        // EditorUtility.GetObjectEnabled returns -1 for objects that don't show the enabled toggle in the InspectorTitleBar
        const int k_HiddenEnabledToggle = -1;

        /// <inheritdoc />
        public void CreateRules(Component component, List<BuildValidationRule> createdRules)
        {
            var componentType = component.GetType();
            var instanceID = component.GetInstanceID();

            var rule = new BuildValidationRule
            {
                IsRuleEnabled = () => CapabilityProfileSelection.Selected.Any(c => c is PolySpatialCapabilityProfile),
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, $"{componentType.Name} Unsupported"),
                Message = string.Format(k_MessageFormat, componentType.Name, PolySpatialSceneValidator.CachedCapabilityProfileNames),
                CheckPredicate = () =>
                {
                    var componentToCheck = EditorUtility.InstanceIDToObject(instanceID) as Component;
                    return componentToCheck == null || EditorUtility.GetObjectEnabled(componentToCheck) == 0;
                },
                SceneOnlyValidation = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            createdRules.Add(rule);
        }

        /// <inheritdoc />
        public void GetTypesToTrack(Component component, List<Type> types)
        {
            if (EditorUtility.GetObjectEnabled(component) != k_HiddenEnabledToggle)
                types.Add(component.GetType());
        }
    }
}
