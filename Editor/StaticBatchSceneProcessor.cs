using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;

namespace UnityEditor.PolySpatial.Internals
{
    /// <summary>
    /// Processes scenes for builds and play mode to remove Unity's static batching and replace it with batching
    /// driven by PolySpatialStaticBatchElement.
    /// </summary>
    internal class StaticBatchSceneProcessor : IProcessSceneWithReport
    {
        /// <summary>
        /// The order in which this processor is called relative to other scene processors.
        /// </summary>
        /// <seealso cref="IOrderedCallback.callbackOrder"/>
        public int callbackOrder => 0;

        /// <summary>
        /// Called when each scene is built.
        /// </summary>
        /// <param name="scene">The scene to process.</param>
        /// <param name="report">The build report.</param>
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // We only need to process the scene if PolySpatial is in use and player static batching is enabled.
            var enablePolySpatialRuntimeInEditor = false;
            PolySpatialCore.GetShouldEnablePolySpatialRuntimeInEditor(ref enablePolySpatialRuntimeInEditor);
            if (report != null ?
                PolySpatialBuildProvider.ShouldProcessBuild(report.summary.platform) &&
                    PlayerSettingsBridge.GetStaticBatchingForPlatform(report.summary.platform) :
                enablePolySpatialRuntimeInEditor &&
                    PlayerSettingsBridge.GetStaticBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget))
            {
                foreach (var gameObject in scene.GetRootGameObjects())
                {
                    ReplaceBatchingStaticFlagsWithComponent(gameObject);
                }
            }
        }

        void ReplaceBatchingStaticFlagsWithComponent(GameObject gameObject)
        {
            var staticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            if (staticFlags.HasFlag(StaticEditorFlags.BatchingStatic))
            {
                GameObjectUtility.SetStaticEditorFlags(gameObject, staticFlags & (~StaticEditorFlags.BatchingStatic));
                if (!gameObject.TryGetComponent<PolySpatialStaticBatchElement>(out _))
                    gameObject.AddComponent<PolySpatialStaticBatchElement>();
            }

            for (var i = 0; i < gameObject.transform.childCount; ++i)
            {
                ReplaceBatchingStaticFlagsWithComponent(gameObject.transform.GetChild(i).gameObject);
            }
        }
    }
}