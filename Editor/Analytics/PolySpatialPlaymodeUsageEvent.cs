#if ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.PolySpatial.Analytics
{
    /// <summary>
    /// Editor event used to send editor usage <see cref="PolySpatialAnalytics"/> data.
    /// Only accepts <see cref="PolySpatialPlaymodeUsageEvent.Payload"/> parameters.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [AnalyticInfo(k_EventName, PolySpatialAnalytics.VendorKey, k_EventVersion, k_MaxEventPerHour, k_MaxItems)]
#endif
    class PolySpatialPlaymodeUsageEvent : PolySpatialEditorAnalyticsEvent<PolySpatialPlaymodeUsageEvent.Payload>
    {
        const string k_EventName = "polyspatial_playmode_usage";
        const int k_EventVersion = 4;

        /// <summary>
        /// The event parameter.
        /// Do not rename any field, the field names are used the identify the table/event column of this event payload.
        /// </summary>
        [Serializable]
        internal struct Payload
#if UNITY_2023_2_OR_NEWER
            : IAnalytic.IData
#endif
        {
            internal const string EnteredPlaymodeState = "EnteredPlaymode";
            internal const string NotInstalledState = "NotInstalled";
            internal const string ActivatedState = "Activated";
            internal const string DeactivatedState = "Deactivated";
            internal const string WindowedMode = "Windowed";
            internal const string UndefinedMode = "Undefined";

            [SerializeField]
            internal string PlaymodeState;

            [SerializeField]
            internal string ActiveBuildTarget;

            [SerializeField]
            internal string PolySpatialRuntimeState;

            [SerializeField]
            internal int BoundedVolumes;

            [SerializeField]
            internal int UnboundedVolumes;

            [SerializeField]
            internal int MetalVolumes;

            [SerializeField]
            internal string XRManagementState;

            [SerializeField]
            internal string[] ActiveXRLoaders;

            [SerializeField]
            internal string ConfiguredMode;

            [SerializeField]
            internal List<AppNetworkPayload> AppNetworkConnections;

#if UNITY_2023_2_OR_NEWER
            [SerializeField]
            internal string package;

            [SerializeField]
            internal string package_ver;
#endif
        }

        [Serializable]
        internal struct AppNetworkPayload
        {
            internal const string UnknownAppName = "Unknown";
            internal const string UnityPlayToDeviceName = "UnityPlayToDevice";

            [SerializeField]
            internal bool IsConnected;

            [SerializeField]
            internal string AppName;
        }

        internal PolySpatialPlaymodeUsageEvent()
#if !UNITY_2023_2_OR_NEWER
            : base(k_EventName, k_EventVersion)
#endif
        {
        }
    }
}
#endif //ENABLE_CLOUD_SERVICES_ANALYTICS || UNITY_2023_2_OR_NEWER
