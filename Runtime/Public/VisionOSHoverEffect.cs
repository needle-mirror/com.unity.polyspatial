#if UNITY_EDITOR && (ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER)
#define POLYSPATIAL_ENABLE_ANALYTICS
#endif

using UnityEngine;
using Unity.PolySpatial.Internals;

#if POLYSPATIAL_ENABLE_ANALYTICS
using UnityEditor.PolySpatial.Analytics;
#endif

namespace Unity.PolySpatial
{
    /// <summary>
    /// A "tag" component indicate the corresponding GO should show a hover effect. To function, the GO must also have a
    /// MeshRenderer (to display the hover effect on) and a Collider (against which the view ray can intersect).
    /// </summary>
    public class VisionOSHoverEffect : MonoBehaviour
    {
#if POLYSPATIAL_ENABLE_ANALYTICS
        static bool s_AnalyticsSent;

        void Awake()
        {
            if (!s_AnalyticsSent && Application.isPlaying)
            {
                s_AnalyticsSent = true;
                PolySpatialAnalytics.Send(FeatureName.VisionOSHoverEffect);
            }
        }
#endif
    }
}
