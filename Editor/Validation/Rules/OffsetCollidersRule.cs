using System;
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
    /// Class that validates the 'center' property of a <see cref="Collider"/> if that collider has a center property.
    /// </summary>
    class OffsetCollidersRule : IComponentRuleCreator, IPropertyValidator
    {
        const string k_Message = "The target platform does not support offsets to the <b>Center</b> parameter of <b>Collider</b> components.";

        const string k_FixItMessage = "Change the <b>Center</b> property values to zero on all axis.";

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
                Category = string.Format(PolySpatialSceneValidator.RuleCategoryFormat, $"{component.GetType().Name} Center"),
                Message = k_Message,
                FixItAutomatic = true,
                CheckPredicate = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return true;

                    var center = GetCenter(collider);

                    return center.Equals(Vector3.zero);
                },
                FixIt = () =>
                {
                    var collider = EditorUtility.InstanceIDToObject(instanceID) as Collider;
                    if (collider == null)
                        return;

                    RecenterCollider(collider);
                },
                FixItMessage = k_FixItMessage,
                SceneOnlyValidation = true,
                Error = true,
                OnClick = () => BuildValidator.SelectObject(instanceID),
            };

            createdRules.Add(rule);
        }

        static void RecenterCollider(Collider collider)
        {
            if (collider is BoxCollider boxCollider)
            {
                Undo.RecordObject(boxCollider, "Recenter Collider");
                boxCollider.center = Vector3.zero;
            }
            else if (collider is CapsuleCollider capsuleCollider)
            {
                Undo.RecordObject(capsuleCollider, "Recenter Collider");
                capsuleCollider.center = Vector3.zero;
            }
            else if (collider is SphereCollider sphereCollider)
            {
                Undo.RecordObject(sphereCollider, "Recenter Collider");
                sphereCollider.center = Vector3.zero;
            }
            else if (collider is WheelCollider wheelCollider)
            {
                Undo.RecordObject(wheelCollider, "Recenter Collider");
                wheelCollider.center = Vector3.zero;
            }
        }

        static Vector3 GetCenter(Collider collider)
        {
            Vector3 center;

            if (collider is BoxCollider boxCollider)
                center = boxCollider.center;
            else if (collider is CapsuleCollider capsuleCollider)
                center = capsuleCollider.center;
            else if (collider is SphereCollider sphereCollider)
                center = sphereCollider.center;
            else if (collider is WheelCollider wheelCollider)
                center = wheelCollider.center;
            else
                center = Vector3.zero;

            return center;
        }
    }
}
