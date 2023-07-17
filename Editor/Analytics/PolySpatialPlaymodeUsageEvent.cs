#if ENABLE_CLOUD_SERVICES_ANALYTICS
using System;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;
using UnityEngine;

namespace UnityEditor.PolySpatial.Analytics
{
    /// <summary>
    /// Editor event used to send editor usage <see cref="PolySpatialAnalytics"/> data.
    /// Only accepts <see cref="PolySpatialPlaymodeUsageEvent.Payload"/> parameters.
    /// </summary>
    class PolySpatialPlaymodeUsageEvent : PolySpatialEditorAnalyticsEvent<PolySpatialPlaymodeUsageEvent.Payload>
    {
        const string k_EventName = "quantum_playmode_usage";
        const int k_EventVersion = 3;

        /// <summary>
        /// The event parameter.
        /// Do not rename any field, the field names are used the identify the table/event column of this event payload.
        /// </summary>
        [Serializable]
        internal struct Payload
        {
            internal const string EnteredPlaymodeCategory = "EnteredPlaymode";
            internal const string PolySpatialScenePlayedName = "PolySpatialScenePlayed";
            internal const string NonPolySpatialScenePlayedName = "NonPolySpatialScenePlayed";

            [SerializeField]
            internal string Name;

            [SerializeField]
            string Category;

            [SerializeField]
            internal int BoundedVolumes;

            [SerializeField]
            internal int UnboundedVolumes;

            internal Payload(string name, string category, int boundedVolumes = 0, int unboundedVolumes = 0)
            {
                Name = name;
                Category = category;
                BoundedVolumes = boundedVolumes;
                UnboundedVolumes = unboundedVolumes;
            }
        }

        internal PolySpatialPlaymodeUsageEvent() : base(k_EventName, k_EventVersion)
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnPlayModeChanged(PlayModeStateChange newState)
        {
            if (newState != PlayModeStateChange.EnteredPlayMode)
                return;

            if (PolySpatialSettings.instance.EnablePolySpatialRuntime)
            {
                var boundedVolumes = 0;
                var unboundedVolumes = 0;

                if (PolySpatialSettings.instance.EnablePolySpatialRuntime && PolySpatialCore.UnitySimulation != null)
                {
                    var volumeCamera = PolySpatialCore.UnitySimulation?.Camera;
                    if (volumeCamera != null)
                    {
                        switch (volumeCamera.Mode)
                        {
                            case VolumeCamera.PolySpatialVolumeCameraMode.Bounded:
                                boundedVolumes++;
                                break;
                            case VolumeCamera.PolySpatialVolumeCameraMode.Unbounded:
                                unboundedVolumes++;
                                break;
                        }
                    }
                }

                Send(new Payload(Payload.PolySpatialScenePlayedName, Payload.EnteredPlaymodeCategory, boundedVolumes, unboundedVolumes));
            }
            else
            {
                Send(new Payload(Payload.NonPolySpatialScenePlayedName, Payload.EnteredPlaymodeCategory));
            }
        }
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS
