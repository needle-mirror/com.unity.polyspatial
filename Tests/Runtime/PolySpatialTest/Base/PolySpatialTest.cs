using System.Collections;
using Tests.Runtime.PolySpatialTest.Utils;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Runtime.PolySpatialTest.Base
{
    /// <summary>
    /// An abstract PolySpatial test class with common attributes and methods. This is an implicit TestFixture class
    /// since it contains a UnityTearDown method.
    /// </summary>
    public abstract class PolySpatialTest
    {
        protected GameObject m_TestGameObject;
        protected Light m_TestLight;
        protected MeshRenderer m_TestPolySpatialMeshRenderer;
        protected MeshFilter m_TestPolySpatialMeshFilter;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log("Called TearDown from PolySpatialTest");
            if (m_TestGameObject)
            {
                // skip a frame in case there is any pending action on the GameObject
                yield return null;
                Debug.Log("Destroying Test GameObject from PolySpatialTest");
                Object.Destroy(m_TestGameObject);
            }

            yield return new WaitForDone(2f, () => m_TestGameObject == null);

            if (m_TestPolySpatialMeshRenderer)
            {
                Object.Destroy(m_TestPolySpatialMeshRenderer);
                yield return new WaitForDone(2f, () => m_TestPolySpatialMeshRenderer == null);
            }

            if (m_TestLight)
            {
                Object.Destroy(m_TestLight);
                yield return new WaitForDone(2f, () => m_TestLight == null);
            }

            yield return null;
        }

        /// <summary>
        /// This method disables test failures caused by logging error|exception messages from the executing code.
        /// PolySpatial may log these types of messages without throwing an exception, which will cause tests to fail.
        /// Note that this function needs to be called from test methods individually; it cannot be set globally
        /// from pre/post-test methods, like SetUp or TearDown; it only applies to that method's scope.
        /// See Class documentation for more information:
        /// https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/api/UnityEngine.TestTools.LogAssert.html
        /// TODO: remove this workaround if JIRA addressed: https://jira.unity3d.com/browse/LXR-250
        /// </summary>
        protected void DisableFailOnErrorMessages()
        {
            LogAssert.ignoreFailingMessages = true;
        }
    }
}
