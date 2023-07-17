using System;
using Unity.XR.CoreUtils.Editor;

namespace UnityEditor.PolySpatial.Validation
{
    /// <summary>
    /// This attribute is used to tag methods that associate <see cref="IComponentRuleCreator"/> instances with component
    /// types. Unity automatically tracks the registered component types in the opened scenes, when a component has the same
    /// type (or is a derived class) the associated <see cref="IComponentRuleCreator.CreateRules"/> instance method is invoked.
    /// </summary>
    /// <remarks>
    /// The tagged method should have the following signature:
    /// <code>static void Method(List<ValueTuple<Type, IComponentRuleCreator>> ruleCreators)</code>
    /// <br/>
    /// The rule creators are registered in the same order as they are added in the <code>ruleCreators</code> list parameter.
    /// It's possible to associate a component type with a <see lagword="null"/> <see cref="IComponentRuleCreator"/>,
    /// in this case no validation will be performed for the component type. This is useful to override validations for
    /// derived types.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyInvalidComponent : MonoBehaviour
    /// { }
    ///
    /// public class MyValidDerivedComponent : MyInvalidComponent
    /// { }
    ///
    /// public class MyRuleCreatorManager
    /// {
    ///     [RuleCreatorAttribute]
    ///     public static void RegisterRuleCreators(List<ValueTuple<Type, IComponentRuleCreator>> ruleCreators)
    ///     {
    ///         // no validation will be performed for MyValidDerivedComponent
    ///         ruleCreators.Add(new(typeof(MyValidDerivedComponent), null));
    ///         ruleCreators.Add(new(typeof(MyInvalidComponent), new UnsupportedComponentsRule(PolySpatialSceneType.Volume, PolySpatialSceneType.MR, PolySpatialSceneType.VR)));
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="BuildValidationRule"/>
    [AttributeUsage(AttributeTargets.Method)]
    public class RuleCreatorAttribute : Attribute
    {
        internal readonly int Priority;

        /// <summary>
        /// The attribute constructor.
        /// </summary>
        /// <param name="priority">An optional priority value to define the order to register the rule creators. High values are registered later, the default value is <code>0</code>.</param>
        public RuleCreatorAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}
