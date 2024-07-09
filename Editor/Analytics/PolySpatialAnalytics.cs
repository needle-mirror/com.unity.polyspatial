#if ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER

namespace UnityEditor.PolySpatial.Analytics
{
    /// <summary>
    /// The entry point class to send PolySpatial analytics data.
    /// </summary>
    [InitializeOnLoad]
    static class PolySpatialAnalytics
    {
        internal const string VendorKey = "unity.polyspatial";

#if UNITY_2023_2_OR_NEWER
        internal const string PackageName = "com.unity.polyspatial";
        internal static readonly string PackageVersion = PackageManager.PackageInfo.FindForPackageName(PackageName).version;
#endif

        static PolySpatialFeatureUsageEvent FeatureUsageEvent { get; } = new();
        internal static PolySpatialPlaymodeUsageEvent PlaymodeUsageEvent { get; } = new();

        /// <summary>
        /// Sends the given feature usage to the analytics server.
        /// </summary>
        /// <param name="featureName">The feature name to send.</param>
        /// <param name="featureConfiguration">The feature configuration to send. It should be well formatted.</param>
        /// <returns>Returns whether the event was successfully sent.</returns>
        /// <remarks>
        /// You should surround your calls to this method between the directives
        /// <code>UNITY_EDITOR && (ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER)</code>.
        /// </remarks>
        internal static bool Send(FeatureName featureName, string featureConfiguration = "")
        {
            return FeatureUsageEvent.Send(featureName, featureConfiguration);
        }

#if !UNITY_2023_2_OR_NEWER
        static PolySpatialAnalytics()
        {
            PlaymodeUsageEvent.Register();
            FeatureUsageEvent.Register();
        }
#endif
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER
