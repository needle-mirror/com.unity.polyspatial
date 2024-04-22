using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace Unity.PolySpatial.Internals
{
    internal static class ObjectBridge
    {
        internal static UnityEngine.Object FindObjectFromInstanceID(int iid)
        {
            return UnityEngine.Object.FindObjectFromInstanceID(iid);
        }

        internal static UnityEngine.Object FindObjectFromInstanceID(long iid)
        {
            return UnityEngine.Object.FindObjectFromInstanceID((int)iid);
        }

        internal static T FindFromInstanceID<T>(int iid)
            where T : UnityEngine.Object
        {
            return FindObjectFromInstanceID(iid) as T;
        }

        internal static T FindFromInstanceID<T>(long iid)
            where T : UnityEngine.Object
        {
            return FindObjectFromInstanceID(iid) as T;
        }

        internal static void MarkDirty(this UnityEngine.Object obj)
        {
            obj.MarkDirty();
        }
    }


    internal class ObjectDispatcherProxy : IDisposable
    {
        internal enum TransformTrackingType
        {
            GlobalTRS = ObjectDispatcher.TransformTrackingType.GlobalTRS,
            LocalTRS = ObjectDispatcher.TransformTrackingType.LocalTRS,
            Hierarchy = ObjectDispatcher.TransformTrackingType.Hierarchy,
        }

        internal enum TypeTrackingFlags
        {
            // All the objects that are instantiated in the scene.
            // For example: GameObjects, Components or dynamically created Meshes or Materials.
            SceneObjects = ObjectDispatcher.TypeTrackingFlags.SceneObjects,
            // All the persistent objects that are either assets or resources.
            // For example: Mesh or Material assets references by MeshRenderer or MeshFilter components.
            // Or a resource object loaded through Resources.Load method.
            Assets = ObjectDispatcher.TypeTrackingFlags.Assets,
            // All the objects that are used by Editor internally.
            // For example: preview scene objects.
            EditorOnlyObjects = ObjectDispatcher.TypeTrackingFlags.EditorOnlyObjects,

            Default = ObjectDispatcher.TypeTrackingFlags.Default,
            All = ObjectDispatcher.TypeTrackingFlags.All
        }

        internal struct TypeDispatchData : IDisposable
        {
            UnityEngine.TypeDispatchData m_Real;

            public UnityEngine.Object[] changed => m_Real.changed;
            public NativeArray<int> changedID => m_Real.changedID;
            public NativeArray<int> destroyedID => m_Real.destroyedID;

            internal TypeDispatchData(UnityEngine.TypeDispatchData real)
            {
                m_Real = real;
            }

            public void Dispose()
            {
                m_Real.Dispose();
            }
        }

        internal struct TransformDispatchData : IDisposable
        {
            UnityEngine.TransformDispatchData m_Real;

            public NativeArray<int> transformedID => m_Real.transformedID;
            public NativeArray<int> parentID => m_Real.parentID;
            public NativeArray<Matrix4x4> localToWorldMatrices => m_Real.localToWorldMatrices;
            public NativeArray<Vector3> positions => m_Real.positions;
            public NativeArray<Quaternion> rotations => m_Real.rotations;
            public NativeArray<Vector3> scales => m_Real.scales;

            internal TransformDispatchData(UnityEngine.TransformDispatchData real)
            {
                m_Real = real;
            }

            public void Dispose()
            {
                m_Real.Dispose();
            }
        }

        readonly ObjectDispatcher m_Real;

        // Cache callbacks to avoid boxing captured types and GC creation per cycle
        readonly Action<UnityEngine.TypeDispatchData> m_FetchTypeCallback;
        readonly Action<UnityEngine.TransformDispatchData> m_TransformHierarchyCallback;
        readonly Action<UnityEngine.TransformDispatchData> m_TransformLocalTRSCallback;

        readonly Action<UnityEngine.TypeDispatchData> m_InternalTempFetchTypeCallback;
        Action<ObjectDispatcherProxy.TypeDispatchData> m_TempFetchTypeCallback;

        readonly Action<UnityEngine.TransformDispatchData> m_InternalTempFetchTransformCallback;
        Action<ObjectDispatcherProxy.TransformDispatchData> m_TempFetchTransformCallback;

        internal ObjectDispatcherProxy()
        {
            m_Real = new ObjectDispatcher();
            m_InternalTempFetchTypeCallback = real =>
            {
                m_TempFetchTypeCallback(new TypeDispatchData(real));
            };
            m_InternalTempFetchTransformCallback = real =>
            {
                m_TempFetchTransformCallback(new TransformDispatchData(real));
            };
        }

        internal ObjectDispatcherProxy(Action<ObjectDispatcherProxy.TypeDispatchData> fetchTypeCallback)
        {
            m_Real = new ObjectDispatcher();
            m_FetchTypeCallback = (real) =>
            {
                fetchTypeCallback(new TypeDispatchData(real));
            };
        }

        internal ObjectDispatcherProxy(Action<ObjectDispatcherProxy.TransformDispatchData> transformHierarchyCallback, Action<TransformDispatchData> transformLocalTRSCallback)
        {
            m_Real = new ObjectDispatcher();
            m_TransformHierarchyCallback = (real) =>
            {
                transformHierarchyCallback(new TransformDispatchData(real));
            };
            m_TransformLocalTRSCallback = (real) =>
            {
                transformLocalTRSCallback(new TransformDispatchData(real));
            };
        }

        public void Dispose()
        {
            m_Real.Dispose();
        }

        public void EnableTypeTracking(TypeTrackingFlags flags, params Type[] types)
        {
            m_Real.EnableTypeTracking((ObjectDispatcher.TypeTrackingFlags)flags, types);
        }
        public void DisableTypeTracking(params Type[] types)
        {
            m_Real.DisableTypeTracking(types);
        }

        public void EnableTransformTracking(TransformTrackingType trackingType, params Type[] types)
        {
            m_Real.EnableTransformTracking((ObjectDispatcher.TransformTrackingType)trackingType, types);
        }

        public void DisableTransformTracking(TransformTrackingType trackingType, params Type[] types)
        {
            m_Real.DisableTransformTracking((ObjectDispatcher.TransformTrackingType)trackingType, types);
        }

        /// <summary>
        /// Fetch types into specified callback method.
        /// </summary>
        public void DispatchTypeChangesAndClear(Type type, Action<ObjectDispatcherProxy.TypeDispatchData> fetchProxyTypeCallback)
        {
            Debug.Assert(fetchProxyTypeCallback != null);
            // Store passed in callback in class field to evade GC from capturing in lambda
            m_TempFetchTypeCallback = fetchProxyTypeCallback;
            m_Real.DispatchTypeChangesAndClear(type, m_InternalTempFetchTypeCallback);
            m_TempFetchTypeCallback = null;
        }

        /// <summary>
        /// Fetch transforms into specified callback method.
        /// </summary>
        public void DispatchTransformChangesAndClear(
            Type type, TransformTrackingType trackingType,
            Action<ObjectDispatcherProxy.TransformDispatchData> fetchProxyTransformCallback)
        {
            Debug.Assert(fetchProxyTransformCallback != null);
            // Store passed in callback in class field to evade GC from capturing in lambda
            m_TempFetchTransformCallback = fetchProxyTransformCallback;
            m_Real.DispatchTransformChangesAndClear(
                type, (ObjectDispatcher.TransformTrackingType)trackingType, m_InternalTempFetchTransformCallback);
            m_TempFetchTransformCallback = null;
        }

        /// <summary>
        /// Fetch types into fetchTypeCallback specified in constructor.
        /// </summary>
        public void DispatchTypeChangesAndClear(Type type)
        {
            Debug.Assert(m_FetchTypeCallback != null);
            m_Real.DispatchTypeChangesAndClear(type, m_FetchTypeCallback);
        }

        /// <summary>
        /// Fetch types into transformHierarchyCallback specified in constructor.
        /// </summary>
        public void DispatchTransformHierarchyChangesAndClear(Type type)
        {
            Debug.Assert(m_TransformHierarchyCallback != null);
            m_Real.DispatchTransformChangesAndClear(type, ObjectDispatcher.TransformTrackingType.Hierarchy, m_TransformHierarchyCallback);
        }

        /// <summary>
        /// Fetch types into transformLocalTRSCallback specified in constructor.
        /// </summary>
        public void DispatchTransformLocalTRSChangesAndClear(Type type)
        {
            Debug.Assert(m_TransformLocalTRSCallback != null);
            m_Real.DispatchTransformChangesAndClear(type, ObjectDispatcher.TransformTrackingType.LocalTRS, m_TransformLocalTRSCallback);
        }
    }

    internal static class TextureBridge
    {
        public static void SetAllowReadingInEditor(this Texture tex, bool allow)
        {
#if UNITY_EDITOR
            tex.allowReadingInEditor = allow;
#endif
        }

        public static bool GetAllowReadingInEditor(this Texture tex)
        {
#if UNITY_EDITOR
            return tex.allowReadingInEditor;
#else
            return false;
#endif
        }
    }

#if UNITY_EDITOR
    internal static class PlayerSettingsBridge
    {
        public static void SetRequiresReadableAssets(bool requires)
        {
            PlayerSettings.platformRequiresReadableAssets = requires;
        }

        public static bool GetRequiresReadableAssets()
        {
            return PlayerSettings.platformRequiresReadableAssets;
        }

        public static bool GetSymlinkTrampolineBuildSetting()
        {
            return EditorUserBuildSettings.symlinkTrampoline;
        }
    }
#endif

    internal static class RendererBridge
    {
        public static uint GetSortingKey(this Renderer renderer)
        {
            return renderer.sortingKey;
        }

        public static int GetSortingGroupId(this Renderer renderer)
        {
            return renderer.sortingGroupID;
        }

        public static int GetSortingGroupOrder(this Renderer renderer)
        {
            return renderer.sortingGroupOrder;
        }

        public static uint GetSortingGroupKey(this Renderer renderer)
        {
            return renderer.sortingGroupKey;
        }
    }

    internal static class SortingGroupBridge
    {
        public static int GetSortingGroupId(this SortingGroup sortingGroup)
        {
            return sortingGroup.sortingGroupID;
        }

        public static int GetSortingGroupOrder(this SortingGroup sortingGroup)
        {
            return sortingGroup.sortingGroupOrder;
        }

        public static int GetIndex(this SortingGroup sortingGroup)
        {
            return sortingGroup.index;
        }

        public static uint GetSortingKey(this SortingGroup sortingGroup)
        {
            return sortingGroup.sortingKey;
        }
    }

    internal static class SpriteRendererBridge
    {
        public static Mesh.MeshDataArray GetCurrentMeshData(this SpriteRenderer sr)
        {
            return sr.GetCurrentMeshData();
        }
    }

    // Dependent implementation in Unity is not available till 2022.3.12f1
#if !UNITY_2022_3_11
    internal static class MathBridge
    {
        public static bool ApproximatelyEqual(this Matrix4x4 lhs, Matrix4x4 rhs, float epsilon = 0.000001f)
        {
            return Matrix4x4.CompareApproximately(lhs, rhs, epsilon);
        }
    }
#endif
}

