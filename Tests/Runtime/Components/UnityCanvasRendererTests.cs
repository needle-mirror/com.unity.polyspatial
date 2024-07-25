using System.Runtime.InteropServices;
using System.Reflection;
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial.Internals;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnityCanvasRendererTests
    {
        static Texture2D s_TestTexture = new Texture2D(1, 1);

        static readonly string s_DefaultSortingLayerName = "Default";

        GameObject m_Canvas;
        GameObject m_TestGameObject;
        CanvasRenderer m_TestCanvasRenderer;

        PolySpatialUnityTracker Tracker => PolySpatialCore.UnitySimulation?.Tracker;

        (GameObject, CanvasRenderer, Button, Image) CreateAllTestObjects()
        {
            m_TestGameObject = new GameObject("CanvasRenderer Test Object");
            m_TestCanvasRenderer = m_TestGameObject.AddComponent<CanvasRenderer>();
            var button = m_TestGameObject.AddComponent<Button>();
            var image = m_TestGameObject.AddComponent<Image>();
            button.image = image;
            image.sprite = Resources.Load<Sprite>("UISprite");

            return (m_TestGameObject, m_TestCanvasRenderer, button, image);
        }

        (GameObject, CanvasRenderer) CreateTestObjects()
        {
            m_Canvas = new GameObject("Canvas", typeof(Canvas));
            m_TestGameObject = new GameObject("CanvasRenderer Test Object");
            m_TestCanvasRenderer = m_TestGameObject.AddComponent<CanvasRenderer>();
            m_TestGameObject.transform.SetParent(m_Canvas.transform);
            return (m_TestGameObject, m_TestCanvasRenderer);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestCanvasRenderer != null)
                Object.Destroy(m_TestCanvasRenderer);
            if (m_TestGameObject != null)
                Object.Destroy(m_TestGameObject);
            if (m_Canvas != null)
                Object.Destroy(m_Canvas);
            m_TestCanvasRenderer = null;
            m_TestGameObject = null;
            m_Canvas = null;
        }

        [UnityTest]
        public IEnumerator Test_Availability_Checks()
        {
            CreateTestObjects();
            var image = m_TestGameObject.AddComponent<Image>();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestCanvasRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.IsEnabled());
            Assert.IsTrue(data.IsActive());
            Assert.IsTrue(data.IsActiveAndEnabled());

            image.enabled = false;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestCanvasRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsFalse(data.IsEnabled());
            Assert.IsTrue(data.IsActive());
            Assert.IsFalse(data.IsActiveAndEnabled());

            Object.Destroy(image);
        }

        [UnityTest]
        public IEnumerator Test_IsActive_Flag()
        {
            CreateTestObjects();
            var image = m_TestGameObject.AddComponent<Image>();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(m_TestCanvasRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.IsEnabled());
            Assert.IsTrue(data.IsActive());
            Assert.IsTrue(data.IsActiveAndEnabled());

            image.gameObject.SetActive(false);

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(m_TestCanvasRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.IsTrue(data.IsEnabled());
            Assert.IsFalse(data.IsActive());
            Assert.IsFalse(data.IsActiveAndEnabled());

            Object.Destroy(image);
        }

        [UnityTest]
        public IEnumerator Test_CanvasEnable_Disable()
        {
            CreateTestObjects();
            var image = m_TestGameObject.AddComponent<Image>();

            yield return null;

            var backingGO = BackingComponentUtils.GetBackingGameObjectFor(PolySpatialInstanceID.For(m_TestGameObject));
            if (backingGO != null)
            {
                Assert.IsNotNull(backingGO.GetComponent<MeshFilter>());
                Assert.IsNotNull(backingGO.GetComponent<MeshRenderer>());
            }

            m_Canvas.GetComponent<Canvas>().enabled = false;

            yield return null;

            if (backingGO != null)
            {
                Assert.IsNull(backingGO.GetComponent<MeshFilter>());
                Assert.IsNull(backingGO.GetComponent<MeshRenderer>());
            }

            Object.Destroy(image);
        }

        [UnityTest]
        public IEnumerator Test_UnityCanvasRenderer_Create_Destroy_Tracking()
        {
            CreateTestObjects();

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            var data = PolySpatialComponentUtils.GetTrackingData(m_TestCanvasRenderer);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());

            var criid = m_TestCanvasRenderer.GetInstanceID();

            // destroy the component
            Object.Destroy(m_TestCanvasRenderer);

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            data = PolySpatialComponentUtils.GetCanvasRendererTrackingData(criid);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
#endif
        }

        [UnityTest]
        public IEnumerator Test_TextMeshProUGUI_Change_Triggers_CanvasRendererTracker()
        {
            CreateTestObjects();
            var cr = m_TestGameObject.GetComponent<CanvasRenderer>();
            var tmp = m_TestGameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = "Hello";

            // Let a frame be processed and trigger the above assertions
            yield return null;

            // right after the frame, we should no longer be dirty
            {
                var data = PolySpatialComponentUtils.GetTrackingData(cr);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            }

            var criid = cr.GetInstanceID();

            yield return null;

            // Make a change to the TextMeshProUGUI component, this should trigger a CanvasRenderer change
            tmp.color = Color.magenta;

            yield return null;

            {
                var data = PolySpatialComponentUtils.GetTrackingData(cr);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.AreEqual(PolySpatialTrackingFlags.Running, data.GetLifecycleStage());
            }

            // destroy the components
            Object.Destroy(tmp);
            Object.Destroy(cr);

            yield return null;

#if UNITY_EDITOR
            // check that it's destroyed
            {
                var data = PolySpatialComponentUtils.GetCanvasRendererTrackingData(criid);
                Assert.IsTrue(data.ValidateTrackingFlags());
                Assert.AreEqual(PolySpatialTrackingFlags.Destroyed, data.GetLifecycleStage());
            };
#endif
        }


        [UnityTest]
        public IEnumerator Test_CanvasRenderer_SetsMaterialAsset()
        {
            CreateTestObjects();
            var cr = m_TestGameObject.GetComponent<CanvasRenderer>();
            var image = m_TestGameObject.AddComponent<Image>();

            image.sprite = Resources.Load<Sprite>("Sprites/TestSprite");

            Assert.IsNotNull(image.sprite);

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(cr);
            Assert.IsTrue(data.ValidateTrackingFlags());
            Assert.AreNotEqual(PolySpatialAssetID.InvalidAssetID, data.customData.materialAssetId);

            Object.Destroy(image);
            image = null;
        }

        struct SortTestSubTree : IEquatable<SortTestSubTree>, IComparable<SortTestSubTree>
        {
            public Canvas canvas;
            public CanvasRenderer button1;
            public CanvasRenderer label1;
            public CanvasRenderer button2;
            public CanvasRenderer label2;

            internal int SortIndexFor(CanvasRenderer item)
            {
                var data = CanvasRendererTracker.GetTrackingData(item.GetInstanceID());
                return data.customData.globalSortIndex;
            }

            public bool Equals(SortTestSubTree other)
            {
                return SortIndexFor(button1) == SortIndexFor(other.button1) &&
                       SortIndexFor(label1) == SortIndexFor(other.label1) &&
                       SortIndexFor(button2) == SortIndexFor(other.button2) &&
                       SortIndexFor(label2) == SortIndexFor(other.label2);
            }

            public int Compare(CanvasRenderer lhs, CanvasRenderer rhs)
            {
                return SortIndexFor(lhs).CompareTo(SortIndexFor(rhs));
            }

            public int CompareTo(SortTestSubTree other)
            {
                int ret = 0;
                ret = SortIndexFor(label2).CompareTo(SortIndexFor(other.label2));
                if (ret == 0)
                    ret = SortIndexFor(button2).CompareTo(SortIndexFor(other.button2));
                if (ret == 0)
                    ret = SortIndexFor(label1).CompareTo(SortIndexFor(other.label1));
                if (ret == 0)
                    ret = SortIndexFor(button1).CompareTo(SortIndexFor(other.button1));
                return ret;
            }
        }

        SortTestSubTree CreateSortTestSubTree(string name, string layerId, int sortOrder)
        {
            var canvas = new GameObject($"Canvas {name}", typeof(Canvas));
            var button1 = new GameObject("Button 1", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var label1 = new GameObject("Label 1", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var button2 = new GameObject("Button 2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var label2 = new GameObject("Label 2", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

            label1.GetComponent<TextMeshProUGUI>().text = "Label 1";
            label2.GetComponent<TextMeshProUGUI>().text = "Label 2";

            // Parent button2 over button1 to tests multiple layers.
            // Depths are computed from the hierarchy position as well as the sibling index.
            // -> Canvas                   Depth: 0
            //     -> Button 1             Depth: 1
            //          -> Label 1         Depth: 2
            //          -> Button 2        Depth: 3
            //               -> Label 2    Depth: 4
            button1.transform.SetParent(canvas.transform);
            label1.transform.SetParent(button1.transform);
            button2.transform.SetParent(button1.transform);
            label2.transform.SetParent(button2.transform);

            var result = new SortTestSubTree()
            {
                canvas = canvas.GetComponent<Canvas>(),
                button1 = button1.GetComponent<CanvasRenderer>(),
                label1 = label1.GetComponent<CanvasRenderer>(),
                button2 = button2.GetComponent<CanvasRenderer>(),
                label2 = label2.GetComponent<CanvasRenderer>(),
            };

            result.canvas.sortingOrder = sortOrder;
            result.canvas.sortingLayerName = layerId;

            return result;

        }

        void DestroySubtree(SortTestSubTree tree)
        {
            GameObject.Destroy(tree.canvas.gameObject);
            GameObject.Destroy(tree.button1.gameObject);
            GameObject.Destroy(tree.label1.gameObject);
            GameObject.Destroy(tree.button2.gameObject);
            GameObject.Destroy(tree.label2.gameObject);
        }

        [UnityTest]
        public IEnumerator Test_CanvasRenderer_Canvas_SortByLayerName()
        {
            var tree1 = CreateSortTestSubTree("One", s_DefaultSortingLayerName, 0);
            var tree2 = CreateSortTestSubTree("Two", s_DefaultSortingLayerName, 0);

            yield return null;

            Assert.IsTrue(tree1.CompareTo(tree2) < 0, "Expected tree1 to be sorted before tree2 for sibling ordering");

            tree1.canvas.sortingLayerName = PolySpatialUnityBackend.PolySpatialUISortingLayerName;

            yield return null;

            Assert.IsTrue(tree2.CompareTo(tree1) < 0, "Expected tree2 to be sorted before tree1 in layer value ordering");

            tree1.canvas.sortingLayerName = s_DefaultSortingLayerName;
            tree2.canvas.sortingLayerName = PolySpatialUnityBackend.PolySpatialUISortingLayerName;

            yield return null;

            Assert.IsTrue(tree1.CompareTo(tree2) < 0, "Expected tree1 to be sorted before tree2 in layer value ordering");

            DestroySubtree(tree1);
            DestroySubtree(tree2);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CanvasRenderer_Canvas_SortByLayerOrder()
        {
            var tree1 = CreateSortTestSubTree("One", s_DefaultSortingLayerName, 0);
            var tree2 = CreateSortTestSubTree("Two", s_DefaultSortingLayerName, 0);

            yield return null;

            Assert.IsTrue(tree1.CompareTo(tree2) == -1, "Expected tree1 to be sorted before tree2");

            tree1.canvas.sortingOrder = 1;

            yield return null;

            Assert.IsTrue(tree2.CompareTo(tree1) == -1, "Expected tree2 to be sorted before tree1");

            tree1.canvas.sortingOrder = 0;
            tree2.canvas.sortingOrder = 1;

            yield return null;

            Assert.IsTrue(tree1.CompareTo(tree2) == -1, "Expected tree1 to be sorted before tree2");

            DestroySubtree(tree1);
            DestroySubtree(tree2);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CanvasRenderer_SameLevel_SortBySiblingIndex()
        {
            var tree1 = CreateSortTestSubTree("One", s_DefaultSortingLayerName, 0);

            yield return null;

            Assert.IsTrue(tree1.Compare(tree1.label1, tree1.button2) < 0, "Expected label to be sorted before button");

            tree1.button2.transform.SetSiblingIndex(0);
            tree1.label1.transform.SetSiblingIndex(1);

            yield return null;

            Assert.IsTrue(tree1.Compare(tree1.button2, tree1.label1) < 0, "Expected button to be sorted before label");

            tree1.label1.transform.SetSiblingIndex(0);
            tree1.button2.transform.SetSiblingIndex(1);

            yield return null;

            Assert.IsTrue(tree1.Compare(tree1.label1, tree1.button2) < 0, "Expected label to be sorted before button");

            DestroySubtree(tree1);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_CanvasRenderer_DeletingItem_ResetsSortIndices()
        {
            var tree1 = CreateSortTestSubTree("One", s_DefaultSortingLayerName, 0);

            var canvas = tree1.canvas;
            var image1 = new GameObject("Image 1", new[] { typeof(RectTransform), typeof(Image) });
            image1.transform.SetParent(canvas.transform);
            var image2 = new GameObject("Image 2", new[] { typeof(RectTransform), typeof(Image) });
            image2.transform.SetParent(canvas.transform);
            var image3 = new GameObject("Image 3", new[] { typeof(RectTransform), typeof(Image) });
            image3.transform.SetParent(canvas.transform);

            yield return null;

            var i1Sid = tree1.SortIndexFor(image1.GetComponent<CanvasRenderer>());
            var i2Sid = tree1.SortIndexFor(image2.GetComponent<CanvasRenderer>());
            var i3Sid = tree1.SortIndexFor(image3.GetComponent<CanvasRenderer>());
            Assert.IsTrue(i1Sid < i2Sid);
            Assert.IsTrue(i2Sid < i3Sid);

            GameObject.Destroy(image2);

            yield return null;

            var i1NewSid = tree1.SortIndexFor(image1.GetComponent<CanvasRenderer>());
            var i3NewSid = tree1.SortIndexFor(image3.GetComponent<CanvasRenderer>());
            Assert.AreEqual(i1Sid, i1NewSid);
            Assert.AreNotEqual(i3Sid, i3NewSid);
            Assert.IsTrue(i1Sid < i3Sid);

            DestroySubtree(tree1);

            yield return null;
        }


        GameObject CreateMaskedHierarchy<T>() where T : MonoBehaviour
        {
            Assert.IsTrue(typeof(T) == typeof(RectMask2D) || typeof(T) == typeof(Mask));

            CreateTestObjects();
            var canvas = m_TestGameObject.transform.parent.GetComponent<Canvas>();
            var canvsRt = canvas.gameObject.GetComponent<RectTransform>();
            canvsRt.sizeDelta = new Vector2(400, 400);

            var maskObject = new GameObject();
            maskObject.transform.parent = canvas.transform;
            var cr = m_TestGameObject.GetComponent<CanvasRenderer>();
            cr.transform.parent = maskObject.transform;
            var crRt = cr.gameObject.AddComponent<RectTransform>();
            crRt.sizeDelta = new Vector2(200, 200);

            var maskRt = maskObject.AddComponent<RectTransform>();
            if (typeof(T) == typeof(Mask))
            {
                var image = maskObject.gameObject.AddComponent<Image>();
                image.sprite = Resources.Load<Sprite>("UISprite");
            }

            maskObject.gameObject.AddComponent<T>();
            return maskObject;
        }

        [UnityTest]
        public IEnumerator Test_MaskEnable_Disable()
        {
            var maskObject = CreateMaskedHierarchy<Mask>();
            var maskRt = maskObject.GetComponent<RectTransform>();
            var mask = maskObject.GetComponent<Mask>();

            maskRt.sizeDelta = new Vector2(100, 100);

            var cr = m_TestGameObject.GetComponent<CanvasRenderer>();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(cr);
            Assert.IsTrue(data.ValidateTrackingFlags());
            // Mask enabled, assert that the masking transform has changed.
            Assert.IsFalse(MathExtensions.ApproximatelyEqual(MaterialConversionHelpers.s_DefaultMaskTransform, data.customData.appliedMaskUVTransform));

            mask.enabled = false;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(cr);
            Assert.IsTrue(data.ValidateTrackingFlags());
            // Mask disabled, assert that the masking transform is back to default and thus no masking is occuring.
            Assert.IsTrue(MathExtensions.ApproximatelyEqual(MaterialConversionHelpers.s_DefaultMaskTransform, data.customData.appliedMaskUVTransform));

            Object.Destroy(maskObject);
        }

        [UnityTest]
        public IEnumerator Test_RectMask2DEnable_Disable()
        {
            var maskObject = CreateMaskedHierarchy<RectMask2D>();
            var maskRt = maskObject.GetComponent<RectTransform>();
            var mask = maskObject.GetComponent<RectMask2D>();

            maskRt.sizeDelta = new Vector2(100, 100);
            mask.padding = new Vector4(10, 10, 10, 10);
            mask.enabled = true;

            var cr = m_TestGameObject.GetComponent<CanvasRenderer>();

            yield return null;

            var data = PolySpatialComponentUtils.GetTrackingData(cr);
            Assert.IsTrue(data.ValidateTrackingFlags());
            // Mask enabled, assert that the masking transform has changed.
            Assert.IsFalse(MathExtensions.ApproximatelyEqual(MaterialConversionHelpers.s_DefaultMaskTransform, data.customData.appliedMaskUVTransform));

            mask.enabled = false;

            yield return null;

            data = PolySpatialComponentUtils.GetTrackingData(cr);
            Assert.IsTrue(data.ValidateTrackingFlags());
            // Mask disabled, assert that the masking transform is back to default and thus no masking is occuring.
            Assert.IsTrue(MathExtensions.ApproximatelyEqual(MaterialConversionHelpers.s_DefaultMaskTransform, data.customData.appliedMaskUVTransform));

            Object.Destroy(maskObject);
        }

        [UnityTest]
        public IEnumerator Test_RectMask2DEnable_PSLCompoundCollider_NoNullRef()
        {
            var maskObject = CreateMaskedHierarchy<RectMask2D>();
            var boxCollider = maskObject.AddComponent<BoxCollider>();
            boxCollider.size = Vector3.one;

            var image = maskObject.AddComponent<Image>();
            image.raycastTarget = true;

            var maskRt = maskObject.GetComponent<RectTransform>();
            var mask = maskObject.GetComponent<RectMask2D>();

            maskRt.sizeDelta = new Vector2(100, 100);
            mask.padding = new Vector4(10, 10, 10, 10);
            mask.enabled = true;

            yield return null;

            mask.enabled = false;

            yield return null;

            boxCollider.size = Vector3.one * 2;

            yield return null;

            Object.Destroy(maskObject);
        }
    }
}
