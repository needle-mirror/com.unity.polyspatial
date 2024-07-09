#if ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.PolySpatial.Analytics
{
    /// <summary>
    /// The feature name.
    /// Do not rename any entry, the enum entries are used the identify the feature in the analytics dashboard.
    /// </summary>
    internal enum FeatureName
    {
        VisionOSHoverEffect,
        DynamicLights,
        DynamicShadows
    }

    /// <summary>
    /// Editor event used to send frequency of usage of PolySpatial features.
    /// Only accepts <see cref="PolySpatialFeatureUsageEvent.Payload"/> parameters.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [AnalyticInfo(k_EventName, PolySpatialAnalytics.VendorKey, k_EventVersion, k_MaxEventPerHour, k_MaxItems)]
#endif
    sealed class PolySpatialFeatureUsageEvent : PolySpatialEditorAnalyticsEvent<PolySpatialFeatureUsageEvent.Payload>
    {
        const string k_EventName = "polyspatial_feature_usage";
        const int k_EventVersion = 1;

        /// <summary>
        /// The event parameter.
        /// Do not rename any field, the field names are used the identify the table/event column of this event payload.
        /// </summary>
        [Serializable]
        internal struct Payload
#if UNITY_2023_2_OR_NEWER
            : IAnalytic.IData
#endif
        {
#if UNITY_2023_2_OR_NEWER
            [SerializeField]
            internal string package;

            [SerializeField]
            internal string package_ver;
#endif

            [SerializeField]
            internal string feature;

            [SerializeField]
            internal string feature_configuration;
        }

        internal PolySpatialFeatureUsageEvent()
#if !UNITY_2023_2_OR_NEWER
            : base(k_EventName, k_EventVersion)
#endif
        {
        }

        /// <summary>
        /// Sends the given feature usage to the analytics server.
        /// </summary>
        /// <param name="featureName">The feature name to send.</param>
        /// <param name="featureConfiguration">The feature configuration to send. It should be well formatted.</param>
        /// <returns>Returns whether the event was successfully sent.</returns>
        internal bool Send(FeatureName featureName, string featureConfiguration)
        {
            var payload = new Payload
            {
#if UNITY_2023_2_OR_NEWER
                package = PolySpatialAnalytics.PackageName,
                package_ver = PolySpatialAnalytics.PackageVersion,
#endif
                feature = featureName.ToString(),
                feature_configuration = featureConfiguration
            };
            return Send(payload);
        }
    }
}
#endif
