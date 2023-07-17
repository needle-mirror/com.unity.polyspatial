using Unity.PolySpatial;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;
using UnityEngine;

namespace UnityEditor.PolySpatial.BuildingBlocks
{
    public class VolumeCameraBuildingBlock : IBuildingBlock
    {
        const string k_Id = "Volume Camera";
        const string k_BuildingBlockPath = "GameObject/XR/Setup/" + k_Id;
        const string k_LightIconPath = "Packages/com.unity.polyspatial/Editor/BuildingBlocks/Icons/Blocks/Setup/Light/VolumeCamera.png";
        const string k_DarkIconPath = "Packages/com.unity.polyspatial/Editor/BuildingBlocks/Icons/Blocks/Setup/Dark/VolumeCamera.png";

        const int k_SectionPriority = 10;

        public string Id => k_Id;
        public string IconPath => EditorGUIUtility.isProSkin ? k_DarkIconPath : k_LightIconPath;

        static void InstantiateBuildingBlock()
        {
            var createdInstance = new GameObject("VolumeCamera");
            createdInstance.AddComponent<VolumeCamera>();
            createdInstance.transform.position = SceneView.lastActiveSceneView.pivot;
            Selection.activeGameObject = createdInstance;
            Undo.RegisterCreatedObjectUndo (createdInstance, "Created VolumeCamera");
        }

        public void ExecuteBuildingBlock() => InstantiateBuildingBlock();

        // Each building block should have an accompanying MenuItem, we add them here.
        [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
        public static void ExecuteMenuItem(MenuCommand command) => InstantiateBuildingBlock();
    }
}
