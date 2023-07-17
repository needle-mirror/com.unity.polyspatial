using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.PolySpatial.Internals;
using UnityEditor.Build;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.PolySpatial.Internals
{
    // PolySpatialAssetProcessor will find all assets used in the Build (using ContentBuildInterface APIs and
    // Resources.LoadAll) which have PolySpatialAssetData, and will tell the build pipeline to add the PolySpatialAssetData
    // to StreamingAssets (using BuildPlayerContext.AddAdditionalPathToStreamingAssets).
    //
    // Also, it will create a mapping table for each scene which maps object references to PolySpatial Asset paths, so
    // we can map objects to their PolySpatialAssetData at runtime. This mapping table will be added to a new GameObject in each
    // serialized scene during the build.
    class PolySpatialAssetBuildProcessor : BuildPlayerProcessor, IProcessSceneWithReport
    {
        public override int callbackOrder => 0;

        internal struct AssetGUIDMapEntryEditor
        {
            internal PolySpatialSceneAssetMap.AssetGUIDMapEntry asset;
            internal string guid;
        }

        static private Dictionary<string, List<AssetGUIDMapEntryEditor>> s_SceneAssets;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    throw new Exception("User aborted.");
            }

            var openedScenePath = SceneManager.GetActiveScene().path;
            try
            {
                PrepareForBuildInner(buildPlayerContext);
            }
            finally
            {
                if (!string.IsNullOrEmpty(openedScenePath))
                {
                    EditorSceneManager.OpenScene(openedScenePath);
                }
            }
        }

        void PrepareForBuildInner(BuildPlayerContext buildPlayerContext)
        {
            s_SceneAssets = new();
            bool firstScene = true;
            foreach (var scene in buildPlayerContext.BuildPlayerOptions.scenes)
            {
                var guids = new List<AssetGUIDMapEntryEditor>();
                var sceneDeps = ContentBuildInterface.CalculatePlayerDependenciesForScene(scene, new(), new());
                foreach (var obj in sceneDeps.referencedObjects)
                    CollectAssetReference(buildPlayerContext, obj.guid.ToString(), guids);

                if (firstScene)
                {
                    // Add manager and resource dependencies to the first scene, so that the object mapping for those
                    // becomes available right from the beginning.
                    var managerDeps = ContentBuildInterface.CalculatePlayerDependenciesForGameManagers(new(),
                        new(), new());
                    foreach (var obj in managerDeps.referencedObjects)
                        CollectAssetReference(buildPlayerContext, obj.guid.ToString(), guids);

                    foreach (var resourcePath in new[] { "", "Packages/com.unity.polyspatial/Resources" })
                    {
                        var resources = Resources.LoadAll(resourcePath);
                        foreach (var obj in resources)
                        {
                            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _))
                                CollectAssetReference(buildPlayerContext, guid, guids);
                        }
                    }

                    firstScene = false;
                }

                s_SceneAssets[scene] = guids;
            }

            // Ensure that the build contains the placeholder referenced by and necessary for loading USD materials.
            if (!File.Exists("Assets/StreamingAssets/placeholder.png"))
            {
                buildPlayerContext.AddAdditionalPathToStreamingAssets(
                    "Packages/com.unity.polyspatial/Textures/placeholder.png");
            }
        }

        private static void CollectAssetReference(BuildPlayerContext buildPlayerContext, string guid,
            List<AssetGUIDMapEntryEditor> guids)
        {
            var paths = EditorPolySpatialAssetProvider.GetPathsForAsset(guid);
            if (paths.Length == 0)
                return;
            guids.Add(new()
            {
                guid = guid,
                // Unity stores secondary artifacts for Assets in the Library folder. All secondary artifacts for an asset
                // are stored next to each other, with the artifact key as an extension. So, we just store the base path
                // without the extension here, which will let us find all the artifacts with a wildcard search. We don't use
                // Path.GetFileNameWithoutExtension, as there are usually multiple extensions
                // (ie: [file hash].[key].polyspatialasset).
                asset = { locator = paths[0].Substring(0, paths[0].IndexOf('.')) }
            });

            foreach (var p in paths)
            {
                buildPlayerContext.AddAdditionalPathToStreamingAssets(Path.GetFullPath(p),
                    Path.Combine("PolySpatialAssets", p));
            }
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                var guids = s_SceneAssets[scene.path];
                var qam = new GameObject().AddComponent<PolySpatialSceneAssetMap>();
                qam.m_SerializedAssetGUIDMap = new List<PolySpatialSceneAssetMap.AssetGUIDMapEntry>(guids.Select(agme =>
                    new PolySpatialSceneAssetMap.AssetGUIDMapEntry
                    {
                        locator = agme.asset.locator,
                        obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(agme.guid))
                    }
                ).Where(agme => agme.obj != null));

            }
        }
    }
}
