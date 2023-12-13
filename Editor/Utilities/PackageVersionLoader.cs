using Unity.PolySpatial;

namespace UnityEditor.PolySpatial.Utilities
{
    /// <summary>
    /// Static class to load the PolySpatial package version into PolySpatialSettings
    /// </summary>
    static class PackageVersionLoader
    {
        [InitializeOnLoadMethod]
        static void LoadPackageVersion()
        {
            // Make sure everything has finished updating before loading package version
            EditorApplication.delayCall += () =>
            {
                PolySpatialSettings.instance.LoadPackageVersion();
            };
        }
    }
}
