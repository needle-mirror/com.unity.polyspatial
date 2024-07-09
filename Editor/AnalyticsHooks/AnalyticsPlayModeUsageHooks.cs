using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.PolySpatial;
using Unity.PolySpatial.Internals;

#if UNITY_HAS_XR_MANAGEMENT
using UnityEditor.XR.Management;
#endif

#if UNITY_HAS_XR_VISIONOS
using UnityEditor.XR.VisionOS;
#endif

namespace UnityEditor.PolySpatial.Analytics
{
    /// <summary>
    /// Class that contains the PolySpatial analytics PlayMode usage hooks.
    /// This class listen to PlayMode changes, build the PlayMode payload and send it to the analytics server.
    /// </summary>
    [InitializeOnLoad]
    class AnalyticsPlayModeUsageHooks
    {
        static AnalyticsPlayModeUsageHooks()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange != PlayModeStateChange.EnteredPlayMode)
                return;

            var payload = GetPlayModeUsagePayload();
            PolySpatialAnalytics.PlaymodeUsageEvent.Send(payload);
        }

        static PolySpatialPlaymodeUsageEvent.Payload GetPlayModeUsagePayload()
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var payload = new PolySpatialPlaymodeUsageEvent.Payload()
            {
                PlaymodeState = PolySpatialPlaymodeUsageEvent.Payload.EnteredPlaymodeState,
                ActiveBuildTarget = activeBuildTarget.ToString(),
                PolySpatialRuntimeState = PolySpatialPlaymodeUsageEvent.Payload.DeactivatedState,
                XRManagementState = PolySpatialPlaymodeUsageEvent.Payload.NotInstalledState,
                ActiveXRLoaders = new string[0],
                ConfiguredMode = PolySpatialPlaymodeUsageEvent.Payload.UndefinedMode,
#if UNITY_2023_2_OR_NEWER
                package = PolySpatialAnalytics.PackageName,
                package_ver = PolySpatialAnalytics.PackageVersion
#endif
            };

            if (PolySpatialRuntime.Enabled)
            {
                payload.PolySpatialRuntimeState = PolySpatialPlaymodeUsageEvent.Payload.ActivatedState;
                if (PolySpatialCore.UnitySimulation != null)
                {
                    foreach (var volumeCamera in PolySpatialCore.UnitySimulation.VolumeCameras)
                    {
                        switch (volumeCamera.WindowMode)
                        {
                            case VolumeCamera.PolySpatialVolumeCameraMode.Bounded:
                                payload.BoundedVolumes++;
                                break;
                            case VolumeCamera.PolySpatialVolumeCameraMode.Unbounded:
                                payload.UnboundedVolumes++;
                                break;
                        }
                    }
                }
            }

#if UNITY_HAS_XR_MANAGEMENT
            var group = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            var isXRManagementActive = generalSettings != null && generalSettings.InitManagerOnStart;
            if (isXRManagementActive)
            {
                payload.XRManagementState = PolySpatialPlaymodeUsageEvent.Payload.ActivatedState;
                if (generalSettings.Manager != null)
                {
                    payload.ActiveXRLoaders = generalSettings.Manager.activeLoaders
                        .Where(l => l != null)
                        .Select(l => l.GetType().Name)
                        .ToArray();
                }
            }
            else
            {
                payload.XRManagementState = PolySpatialPlaymodeUsageEvent.Payload.DeactivatedState;
            }
#endif

            if (activeBuildTarget == BuildTarget.VisionOS)
            {
#if UNITY_HAS_XR_VISIONOS
                var visionOSSettings = VisionOSSettings.currentSettings;
                if (visionOSSettings != null)
                {
                    payload.ConfiguredMode = visionOSSettings.appMode.ToString();
                }
                else
#endif
                {
                    payload.ConfiguredMode = PolySpatialPlaymodeUsageEvent.Payload.WindowedMode;
                }
            }

            payload.AppNetworkConnections = GetLocalAppNetworkConnections();

            return payload;
        }

        static List<PolySpatialPlaymodeUsageEvent.AppNetworkPayload> GetLocalAppNetworkConnections()
        {
            var localAppNetwork = new List<PolySpatialPlaymodeUsageEvent.AppNetworkPayload>();
            if (!PolySpatialRuntime.Enabled)
                return localAppNetwork;

            var selectedCandidate = PolySpatialUserSettings.instance.ConnectionCandidates.Values.FirstOrDefault(c => c.IsSelected);
            var playToDeviceIP = selectedCandidate != null && IPAddress.TryParse(selectedCandidate.IP, out var ipAddress) ? ipAddress : null;

            // Will be marked true if one or more connections are valid
            var connected = false;

            foreach (var connection in PolySpatialCore.HostConnectionManager.Connections)
            {
                if (connection.Connected)
                {
                    connected = true;

                    localAppNetwork.Add(new PolySpatialPlaymodeUsageEvent.AppNetworkPayload()
                    {
                        IsConnected = true,
                        AppName = connection.Address.Equals(playToDeviceIP)
                            ? PolySpatialPlaymodeUsageEvent.AppNetworkPayload.UnityPlayToDeviceName
                            : PolySpatialPlaymodeUsageEvent.AppNetworkPayload.UnknownAppName
                    });
                }
            }

            if (!connected && PolySpatialUserSettings.instance.ConnectToPlayToDevice && playToDeviceIP != null)
            {
                localAppNetwork.Add(new PolySpatialPlaymodeUsageEvent.AppNetworkPayload()
                {
                    IsConnected = false,
                    AppName = PolySpatialPlaymodeUsageEvent.AppNetworkPayload.UnityPlayToDeviceName
                });
            }

            return localAppNetwork;
        }
    }
}
