using System;
using System.Collections;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnitySkinnedMeshRendererTests : ComponentTestBase
    {
        GameObject m_TestGameObject;
        Transform[] m_TestSkeleton;
        SkinnedMeshRenderer m_SkinnedMeshRenderer;

        const string k_TestMaterialName = "SkinnedMeshRendererTestMaterial";
        const string k_TestMeshName = "SkinnedMeshRendererTestMesh";
        const string k_TestGameObjectName = "SkinnedMeshRenderer Test Object";

        private Mesh CreateTestSkinnedMesh()
        {
            Assert.IsNotNull(m_TestSkeleton, "Test skeleton was null. Make sure to call CreateTestObjects() before attempting to create a mesh.");
            Assert.IsTrue(m_TestSkeleton.Length > 0, "Skeleton bone length was 0. Make sure to call CreateTestObjects() before attempting to create a mesh.");

            var mesh = new Mesh();
            // a mesh must have some vertices and at least 1 submesh in order to be sent
            mesh.vertices = new[] { Vector3.up, Vector3.left, Vector3.down };
            mesh.SetIndices(new[] { 0, 1, 2}, MeshTopology.Triangles, 0);
            mesh.name = k_TestMeshName;

            // Set bone weights. The size of the boneweights array must match the size of the mesh vertices array.
            BoneWeight[] weights = new BoneWeight[3];
            weights[0].boneIndex0 = 0;
            weights[0].weight0 = 1;
            weights[1].boneIndex0 = 0;
            weights[1].weight0 = 1;
            weights[2].boneIndex0 = 1;
            weights[2].weight0 = 1;
            mesh.boneWeights = weights;

            // Set bind poses. Skip the first one in the m_TestSkeleton array since that one is the root game object for
            // the other bones and has no parent, as set up in CreateTestObjects. Note the length of the bindposes has to be equal to the skeleton array length regardless,
            // so the first bindpose will just be identity.
            var bindPoses = new Matrix4x4[m_TestSkeleton.Length];
            bindPoses[0] = Matrix4x4.identity;
            for (int i = 1; i < m_TestSkeleton.Length; i++)
            {
                bindPoses[i] = m_TestSkeleton[i].worldToLocalMatrix * m_TestSkeleton[i].parent.localToWorldMatrix;
            }

            mesh.bindposes = bindPoses;

            return mesh;
        }

        void CreateTestObjects()
        {
            m_TestGameObject = new GameObject(k_TestGameObjectName);
            m_SkinnedMeshRenderer = m_TestGameObject.AddComponent<SkinnedMeshRenderer>();
            m_TestSkeleton = new Transform[4];

            var boneIndex = 0;
            for (int i = 0; i < m_TestSkeleton.Length; i++)
            {
                var childName = "ChildBones: " + boneIndex++;
                var newGameObjects = new GameObject(childName);
                m_TestSkeleton[i] = newGameObjects.transform;
            }

            // Arbitrarily create an hierarchy.
            m_TestSkeleton[1].parent = m_TestSkeleton[0];
            m_TestSkeleton[2].parent = m_TestSkeleton[0];
            m_TestSkeleton[3].parent = m_TestSkeleton[1];

            m_TestSkeleton[1].localPosition = new Vector3(0, 5, 0);
            m_TestSkeleton[2].localPosition = new Vector3(5, 0, 0);
            m_TestSkeleton[3].localPosition = new Vector3(0, 0, 5);

            // Set defaults for the skinned mesh renderer. It has to have bones and a rootBone to work, and the quality
            // is set to default.
            m_SkinnedMeshRenderer.bones = m_TestSkeleton;
            m_SkinnedMeshRenderer.quality = SkinQuality.Auto;
            m_SkinnedMeshRenderer.rootBone = m_TestGameObject.transform;
        }

        internal override IEnumerator InternalUnityTearDown()
        {
            if (m_SkinnedMeshRenderer != null)
                Object.Destroy(m_SkinnedMeshRenderer);
            if (m_TestGameObject != null)
                Object.Destroy(m_TestGameObject);
            foreach (var bone in m_TestSkeleton)
            {
                if (bone != null)
                {
                    Object.Destroy(bone.gameObject);
                }
            }
            m_TestGameObject = null;
            m_SkinnedMeshRenderer = null;
            m_TestSkeleton = null;

            yield return base.InternalUnityTearDown();
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_Create_Destroy_Tracking()
        {
            CreateTestObjects();

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            PolySpatialComponentUtils.ValidateTrackingData(data, PolySpatialTrackingFlags.Running);

            var criid = m_SkinnedMeshRenderer.GetInstanceID();

            // destroy the component
            Object.Destroy(m_SkinnedMeshRenderer);

            yield return null;

#if UNITY_EDITOR
            // Check to see if data stays deleted. We no longer clear the dirty flag, as we will not touch this data any more.
            var deletedData = PolySpatialComponentUtils.GetSkinnedMeshRendererTrackingData(criid);
            Assert.IsTrue(deletedData.ValidateTrackingFlags());
            PolySpatialComponentUtils.ValidateTrackingData(deletedData, PolySpatialTrackingFlags.Destroyed);
#endif

            yield return null;

            // After another frame, the tracking data is gone, and GetSkinnedMeshRendererTrackingData throws.
            Assert.Throws<InvalidOperationException>(() => PolySpatialComponentUtils.GetSkinnedMeshRendererTrackingData(criid));
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_Disable_Tracking()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_SkinnedMeshRenderer.gameObject));
            if (backingGO != null)
                Assert.IsTrue(backingGO.GetComponent<SkinnedMeshRenderer>().enabled);

            m_SkinnedMeshRenderer.enabled = false;

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.TrackingFlags.HasFlag(PolySpatialTrackingFlags.Disabled));

            if (backingGO != null)
                Assert.IsFalse(backingGO.GetComponent<SkinnedMeshRenderer>() != null);
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_Set_Mesh_Updates_MeshRendererData()
        {
            CreateTestObjects();

            // skip one frame, so processing happens and dirty bit gets cleared
            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var oldSkinMeshAssetId = data.customData.meshRendererTrackingData.meshId;

            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            // let the changes happen
            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreNotEqual(oldSkinMeshAssetId, data.customData.meshRendererTrackingData.meshId);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_MaterialChanges()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            // When the skinned mesh renderer has its material changed/set, it creates a new instance of the original material
            // and uses that instead, so we have to track the material in the skinned mesh renderer, not the original.
            m_SkinnedMeshRenderer.materials = new Material[] {PolySpatialComponentUtils.CreateUnlitMaterial(Color.red)};
            m_SkinnedMeshRenderer.material.name = k_TestMaterialName;
            var material1 = m_SkinnedMeshRenderer.material;

            yield return null;

            var data1 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data1.ValidateTrackingFlags());
            var materialIds1 = data1.customData.meshRendererTrackingData.materialIds;
            Assert.AreEqual(1, materialIds1.Length);
            Assert.IsTrue(materialIds1[0].IsValid(), "Expected first material to be valid.");
            Assert.AreEqual(materialIds1[0], PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(material1));
            Assert.AreEqual(material1, PolySpatialCore.LocalAssetManager.GetRegisteredResource(materialIds1[0]));

            PolySpatialCore.LocalAssetManager.GetPolySpatialMaterialDataForRegisteredMaterial(materialIds1[0], out var materialData1);
            // this isn't a complete verification of material conversion, that needs a separate suite
            Assert.AreEqual(PolySpatialMaterialType.Unlit, materialData1.materialType);
            Assert.IsTrue(materialData1.baseColorMap.isEnabled, "Base color map was not enabled.");
            Assert.AreEqual(Color.red, materialData1.baseColorMap.color);

            // now modify the material's color.  We should get a material change notification and the material data
            // should get updated
            material1.color = Color.blue;

            yield return null;

            // The AssetID shouldn't change
            var data2 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data2.ValidateTrackingFlags());
            var materialIds2 = data1.customData.meshRendererTrackingData.materialIds;
            Assert.AreEqual(materialIds1[0], PolySpatialCore.LocalAssetManager.GetRegisteredAssetID(material1));
            Assert.AreEqual(materialIds1[0], materialIds2[0]);

            // but the data should
            PolySpatialCore.LocalAssetManager.GetPolySpatialMaterialDataForRegisteredMaterial(materialIds1[0], out var materialData2);
            Assert.AreEqual(Color.blue, materialData2.baseColorMap.color);
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_CreateAndDeletePolySpatialAnimationRig()
        {
            // Create rig.
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            var cachedData = data.customData;
            var rig = PolySpatialCore.LocalAssetManager.GetRegisteredResource<PolySpatialAnimationRig>(cachedData.rigId);

            Assert.IsNotNull(rig, "Rig was null.");

            // Test to see if it's got the correct data.
            Assert.AreEqual(m_SkinnedMeshRenderer.rootBone.gameObject.GetInstanceID(), rig.m_SkeletonHierarchy.RootBone.id);
            Assert.AreEqual(m_SkinnedMeshRenderer.quality.ToPlatform(), rig.m_SkinWeight);

            for (int i = 0; i < rig.m_SkeletonHierarchy.AsPolySpatialIDs.Length; i++)
            {
                Assert.IsTrue(rig.m_SkeletonHierarchy.AsPolySpatialIDs[i].Equals(PolySpatialInstanceID.For(m_TestSkeleton[i].gameObject)));
            }

            // Change the skinning quality to one bone and check again.
            m_SkinnedMeshRenderer.quality = SkinQuality.Bone1;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            cachedData = data.customData;
            rig = PolySpatialCore.LocalAssetManager.GetRegisteredResource<PolySpatialAnimationRig>(cachedData.rigId);
            Assert.AreEqual(1, rig.m_SkinWeight);

            // Destroy the skinned mesh renderer and ensure proper cleanup happens. Cache the ids so we can attempt to access them later for testing.
            var smriid = m_SkinnedMeshRenderer.GetInstanceID();
            var mesh = m_SkinnedMeshRenderer.sharedMesh;
            var material = m_SkinnedMeshRenderer.material;
            m_SkinnedMeshRenderer.DestroyAppropriately();

            yield return null;

#if UNITY_EDITOR
            data = PolySpatialComponentUtils.GetSkinnedMeshRendererTrackingData(smriid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.customData.rigId.IsValid());
            Assert.IsFalse(data.customData.meshRendererTrackingData.meshId.IsValid());
            foreach (var materialId in data.customData.meshRendererTrackingData.materialIds)
                Assert.IsFalse(materialId.IsValid());
#endif

            mesh.DestroyAppropriately();
            material.DestroyAppropriately();

            void ClearIfDeleted(IAssetManager mgr, ref PolySpatialAssetID aid) {
                if (aid != PolySpatialAssetID.InvalidAssetID && mgr.GetRegisteredResource(aid) == null)
                {
                    aid = PolySpatialAssetID.InvalidAssetID;
                }
            }

            Assert.AreEqual(1, cachedData.meshRendererTrackingData.materialIds.Length);

            var cachedDataBackend = cachedData;

            m_TestPlatformWrapper.OnAfterAssetsDeletedCalled = (assetIds) =>
            {
                ClearIfDeleted(PolySpatialCore.LocalAssetManager, ref cachedData.meshRendererTrackingData.meshId);
                ClearIfDeleted(PolySpatialCore.LocalAssetManager, ref cachedData.rigId);
                ClearIfDeleted(PolySpatialCore.LocalAssetManager, ref cachedData.meshRendererTrackingData.materialIds.ElementAt(0));

                // Assert that meshes, materials, and rigs have been properly removed from the Platform Asset Manager.
                ClearIfDeleted(UnitySceneGraphAssetManager.Shared, ref cachedDataBackend.meshRendererTrackingData.meshId);
                ClearIfDeleted(UnitySceneGraphAssetManager.Shared, ref cachedDataBackend.rigId);
                ClearIfDeleted(UnitySceneGraphAssetManager.Shared, ref cachedDataBackend.meshRendererTrackingData.materialIds.ElementAt(0));
            };

            yield return null;

            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedData.meshRendererTrackingData.meshId);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedData.rigId);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedData.meshRendererTrackingData.materialIds[0]);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedDataBackend.meshRendererTrackingData.meshId);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedDataBackend.rigId);
            Assert.AreEqual(PolySpatialAssetID.InvalidAssetID, cachedDataBackend.meshRendererTrackingData.materialIds[0]);
        }

        [UnityTest]
#if UNITY_IOS
        [Ignore("Disabling as it currently crashes/fails on iOS.")]
#endif
        public IEnumerator Test_AnimationRig_Equality()
        {
            CreateTestObjects();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            var firstRig = PolySpatialCore.LocalAssetManager.GetRegisteredResource<PolySpatialAnimationRig>(data.customData.rigId);

            // Create another animation rig.
            var secondRig = PolySpatialAnimationRig.CreateAnimationRig(m_SkinnedMeshRenderer);

            bool isHierarchyEqual;
            Assert.IsTrue(firstRig.Compare(m_SkinnedMeshRenderer, out isHierarchyEqual));
            Assert.IsTrue(firstRig.Compare(secondRig, out isHierarchyEqual));

            // Modify the skeleton transforms for the first rig.
            var newBone = new GameObject("New Bone");
            m_TestSkeleton[^1] = newBone.transform;
            m_SkinnedMeshRenderer.bones = m_TestSkeleton;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            firstRig = PolySpatialCore.LocalAssetManager.GetRegisteredResource<PolySpatialAnimationRig>(data.customData.rigId);

            // First rig will have a new skeleton, but second rig should still have original skeleton transforms.
            Assert.IsFalse(firstRig.Compare(secondRig, out isHierarchyEqual));
            Assert.IsFalse(isHierarchyEqual);

            secondRig.UpdateFrom(m_SkinnedMeshRenderer);
            Assert.IsTrue(firstRig.Compare(secondRig, out isHierarchyEqual));
            Assert.IsTrue(isHierarchyEqual);

            m_SkinnedMeshRenderer.quality = SkinQuality.Bone2;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            firstRig = PolySpatialCore.LocalAssetManager.GetRegisteredResource<PolySpatialAnimationRig>(data.customData.rigId);

            // Shouldn't need to update the skeleton here.
            Assert.IsFalse(firstRig.Compare(secondRig, out isHierarchyEqual));
            Assert.IsTrue(isHierarchyEqual);

            // Cleanup locally created resources.
            if (secondRig != null)
            {
                Object.Destroy(secondRig);
            }
        }

        [UnityTest]
        public IEnumerator Test_UnitySkinnedMeshRenderer_MultipleMaterials()
        {
            CreateTestObjects();
            m_SkinnedMeshRenderer.sharedMesh = CreateTestSkinnedMesh();
            m_SkinnedMeshRenderer.sharedMaterials = Array.Empty<Material>();

            yield return null;

            var data1 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data1.ValidateTrackingFlags());
            Assert.AreEqual(0, data1.customData.meshRendererTrackingData.materialIds.Length);
            Assert.IsFalse(data1.customData.meshRendererTrackingData.hasExternalMaterials);

            int maxMaterialsInFixedBuffer = data1.customData.meshRendererTrackingData.materialIds.Capacity;
            var materials = new Material[maxMaterialsInFixedBuffer + 1];
            for (int i=0; i<maxMaterialsInFixedBuffer + 1; i++)
                materials[i] = PolySpatialComponentUtils.CreateUnlitMaterial(Color.red);

            m_SkinnedMeshRenderer.sharedMaterial = materials[0];

            yield return null;

            var data2 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data2.ValidateTrackingFlags());
            Assert.AreEqual(1, data2.customData.meshRendererTrackingData.materialIds.Length);
            Assert.IsFalse(data2.customData.meshRendererTrackingData.hasExternalMaterials);

            m_SkinnedMeshRenderer.sharedMaterials = materials;

            yield return null;

            var data3 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data3.ValidateTrackingFlags());
            Assert.AreEqual(0, data3.customData.meshRendererTrackingData.materialIds.Length);

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_SkinnedMeshRenderer.gameObject));
            if (backingGO != null)
                Assert.AreEqual(materials.Length, backingGO.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length);

            Assert.IsTrue(data3.customData.meshRendererTrackingData.hasExternalMaterials);

            m_SkinnedMeshRenderer.sharedMaterials = new[] {materials[0]};

            yield return null;

            var data4 = PolySpatialComponentUtils.GetTrackingData(m_SkinnedMeshRenderer);
            Assert.IsTrue(data4.ValidateTrackingFlags());
            Assert.AreEqual(1, data4.customData.meshRendererTrackingData.materialIds.Length);
            Assert.IsFalse(data4.customData.meshRendererTrackingData.hasExternalMaterials);
        }
    }
}
