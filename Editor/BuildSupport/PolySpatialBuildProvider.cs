using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if HAS_SCRIPTABLE_BUILDPIPELINE
using UnityEditor.Build.Pipeline;
#endif

namespace UnityEditor.PolySpatial.Internals
{
    /// <summary>
    /// Performs custom build steps, such as ensuring that the default material will be present in standalone
    /// builds.
    /// </summary>
    public class PolySpatialBuildProvider : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private Shader tempShader;

        /// <summary>
        /// The order in which this provider is called relative to other build providers.
        /// </summary>
        /// <seealso cref="IOrderedCallback.callbackOrder"/>
        public int callbackOrder => 0;

#if HAS_SCRIPTABLE_BUILDPIPELINE
        [InitializeOnLoadMethod]
        static void InitBuildCallbackLogger()
        {
            ContentPipeline.BuildCallbacks.PostDependencyCallback += (parameters, data) =>
            {
                PlayerSettingsBridge.SetRequiresReadableAssets(true);
                return ReturnCode.Success;
            };
            ContentPipeline.BuildCallbacks.PostWritingCallback += (parameters, data, arg3, arg4) =>
            {
                PlayerSettingsBridge.SetRequiresReadableAssets(false);
                return ReturnCode.Success;
            };
        }
#endif

        static bool ShouldProcessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS)
                return false;

            return true;
        }

        /// <summary>
        /// Called before a build is started.
        /// </summary>
        /// <param name="report">The build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            PlayerSettingsBridge.SetRequiresReadableAssets(true);

            if (!ShouldProcessBuild(report))
                return;
        }

        /// <summary>
        /// Called after a build is finished.
        /// </summary>
        /// <param name="report">The build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            PlayerSettingsBridge.SetRequiresReadableAssets(false);

            if (!ShouldProcessBuild(report))
                return;
        }

        private static bool IsPlayerSettingsDirty()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                return EditorUtility.IsDirty(settings[0]);
            return false;
        }

        private static void ClearPlayerSettingsDirtyFlag()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>();
            if (settings != null && settings.Length > 0)
                EditorUtility.ClearDirty(settings[0]);
        }
    }
}
