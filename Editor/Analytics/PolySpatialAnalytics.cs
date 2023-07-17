#if ENABLE_CLOUD_SERVICES_ANALYTICS
namespace UnityEditor.PolySpatial.Analytics
{
    /// <summary>
    /// The entry point class to send PolySpatial analytics data.
    /// </summary>
    [InitializeOnLoad]
    static class PolySpatialAnalytics
    {
        internal const string VendorKey = "unity.quantum";

        static PolySpatialPlaymodeUsageEvent PlaymodeUsageEvent { get; } = new();

        static PolySpatialAnalytics()
        {
            PlaymodeUsageEvent.Register();
        }
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS
