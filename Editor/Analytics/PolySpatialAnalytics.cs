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

        static PolySpatialPlaymodeUsageEvent PlaymodeUsageEvent { get; } = new();

#if !UNITY_2023_2_OR_NEWER
        static PolySpatialAnalytics()
        {
            PlaymodeUsageEvent.Register();
        }
#endif
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER
