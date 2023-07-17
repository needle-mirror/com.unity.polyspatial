using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.PolySpatial.Internals;

namespace Tests.Runtime.Functional.Components
{
    [TestFixture]
    public class UnityCanvasClipperTests
    {
        [Test]
        public void Triangle_Inside_Rect_Is_Not_Clipped()
        {
            var verts = new Vector3[] { new Vector3(1, 1, 0), new Vector3(9, 1, 0), new Vector3(5, 9, 0) };
            var uvs = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero };
            var colors32 = new Color32[] { Color.black, Color.black, Color.black };
            var inds = new int[] { 0, 1, 2 };
            var rect = new Rect(0.0f, 0.0f, 10.0f, 10.0f);

            Vector3[] newVerts;
            Vector2[] newUVs;
            Color32[] newColors32;
            int[] newInds;
            CanvasRendererClipper.ClipMeshInRectangle(
                verts, uvs, colors32, inds, rect, out newVerts, out newUVs, out newColors32, out newInds);

            Assert.AreEqual(3, newVerts.Length);
            Assert.AreEqual(3, newInds.Length);

            Assert.AreEqual(verts[0], newVerts[0]);
            Assert.AreEqual(verts[1], newVerts[1]);
            Assert.AreEqual(verts[2], newVerts[2]);
        }

        [Test]
        public void Triangle_Large_Side_Outside_Rect_Is_Clipped()
        {
            var verts = new Vector3[] { new Vector3(-1, 1, 0), new Vector3(-1, 9, 0), new Vector3(5, 5, 0) };
            var uvs = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero };
            var colors32 = new Color32[] { Color.black, Color.black, Color.black };
            var inds = new int[] { 0, 1, 2 };
            var rect = new Rect(0.0f, 0.0f, 10.0f, 10.0f);

            Vector3[] newVerts;
            Vector2[] newUVs;
            Color32[] newColors32;
            int[] newInds;
            CanvasRendererClipper.ClipMeshInRectangle(
                verts, uvs, colors32, inds, rect, out newVerts, out newUVs, out newColors32, out newInds);

            Assert.AreEqual(3, newVerts.Length);
            Assert.AreEqual(3, newInds.Length);

            // The clipping may shuffle the input vertices, but we should have two vertices with x=0
            int count = 0;
            foreach (var v in newVerts)
            {
                if (Mathf.Approximately(0.0f, v.x))
                    ++count;
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Triangle_Large_Side_Inside_Makes_Quad()
        {
            var verts = new Vector3[] { new Vector3(5, 1, 0), new Vector3(5, 9, 0), new Vector3(-5, 5, 0) };
            var uvs = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero };
            var colors32 = new Color32[] { Color.black, Color.black, Color.black };
            var inds = new int[] { 0, 1, 2 };
            var rect = new Rect(0.0f, 0.0f, 10.0f, 10.0f);

            Vector3[] newVerts;
            Vector2[] newUVs;
            Color32[] newColors32;
            int[] newInds;
            CanvasRendererClipper.ClipMeshInRectangle(
                verts, uvs, colors32, inds, rect, out newVerts, out newUVs, out newColors32, out newInds);

            Assert.AreEqual(6, newVerts.Length);
            Assert.AreEqual(6, newInds.Length);
        }

        [Test]
        public void Large_Triangle_Get_Clipped_Inside_Rect()
        {
            // Triangle large enough to cover the whole rect
            var verts = new Vector3[] { new Vector3(-100, -1000, 0), new Vector3(-100, 100, 0), new Vector3(1000, 100, 0) };
            var uvs = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero };
            var colors32 = new Color32[] { Color.black, Color.black, Color.black };
            var inds = new int[] { 0, 1, 2 };
            var rect = new Rect(0.0f, 0.0f, 10.0f, 10.0f);

            Vector3[] newVerts;
            Vector2[] newUVs;
            Color32[] newColors32;
            int[] newInds;
            CanvasRendererClipper.ClipMeshInRectangle(
                verts, uvs, colors32, inds, rect, out newVerts, out newUVs, out newColors32, out newInds);

            Assert.GreaterOrEqual(newVerts.Length, 6);
            Assert.GreaterOrEqual(newInds.Length, 6);
        }

        [Test]
        public void Triangle_UVs_Are_Interpolated()
        {
            var verts = new Vector3[] { new Vector3(-5, 4.9f, 0), new Vector3(5, 5, 0), new Vector3(-5, 5.1f, 0) };
            var uvs = new Vector2[] { new Vector2(0, 0), new Vector2(1.0f, 1.0f), new Vector2(0, 0) };
            var colors32 = new Color32[] { Color.black, Color.white, Color.black };
            var inds = new int[] { 0, 1, 2 };
            var rect = new Rect(0.0f, 0.0f, 10.0f, 10.0f);

            Vector3[] newVerts;
            Vector2[] newUVs;
            Color32[] newColors32;
            int[] newInds;

            CanvasRendererClipper.ClipMeshInRectangle(
                verts, uvs, colors32, inds, rect, out newVerts, out newUVs, out newColors32, out newInds);

            Assert.AreEqual(3, newVerts.Length);
            Assert.AreEqual(3, newInds.Length);

            // The edges should be clipped at x=0, where the UVs should be close to 0.5 and color should be gray
            int count = 0;
            for (int i = 0; i < newVerts.Length; ++i)
            {
                var v = newVerts[i];
                if (Mathf.Approximately(0.0f, v.x))
                {
                    var uv = newUVs[i];
                    Color color = newColors32[i];
                    Assert.AreEqual(0.5f, uv.x, 0.1f);
                    Assert.AreEqual(0.5f, color.r, 0.1f);
                    ++count;
                }
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Quad_In_Rect_Is_Not_Clipped()
        {
            var verts = new Vector3[] { new Vector3(1, 1, 0), new Vector3(9, 1, 0), new Vector3(9, 9, 0), new Vector3(1, 9, 0) };
            var uvs = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            var colors32 = new Color32[] { Color.black, Color.black, Color.black, Color.black };
            var inds = new int[] { 0, 1, 2, 0, 2, 3 };
            var rect = new Rect(0.0f, 0.0f, 10.0f, 10.0f);

            Vector3[] newVerts;
            Vector2[] newUVs;
            Color32[] newColors32;
            int[] newInds;
            CanvasRendererClipper.ClipMeshInRectangle(
                verts, uvs, colors32, inds, rect, out newVerts, out newUVs, out newColors32, out newInds);

            Assert.AreEqual(6, newVerts.Length);
            Assert.AreEqual(6, newInds.Length);
        }

        [Test]
        public void Clipped_Triangle_And_Quad_In_Rect_Is_Not_Clipped()
        {
            var verts = new Vector3[] { new Vector3(-1, 1, 0), new Vector3(-1, 2, 0), new Vector3(1, 1, 0), new Vector3(9, 1, 0), new Vector3(9, 9, 0), new Vector3(1, 9, 0) };
            var uvs = new Vector2[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            var colors32 = new Color32[] { Color.black, Color.black, Color.black, Color.black, Color.black, Color.black };
            var inds = new int[] { 0, 1, 2, 2, 3, 4, 2, 4, 5 };
            var rect = new Rect(0.0f, 0.0f, 10.0f, 10.0f);

            Vector3[] newVerts;
            Vector2[] newUVs;
            Color32[] newColors32;
            int[] newInds;
            CanvasRendererClipper.ClipMeshInRectangle(
                verts, uvs, colors32, inds, rect, out newVerts, out newUVs, out newColors32, out newInds);

            Assert.AreEqual(9, newVerts.Length);
            Assert.AreEqual(9, newInds.Length);
        }
    }
}
