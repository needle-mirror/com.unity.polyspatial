using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Unity.PolySpatial.Networking;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityObject = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.PolySpatial
{
    public static class PolySpatialSettingsExtensions
    {
        public static bool ShouldDisableParticleRendering(this PolySpatialSettings.ParticleReplicationMode mode)
        {
            return (mode != PolySpatialSettings.ParticleReplicationMode.ReplicateProperties);
        }
    }

    /// <summary>
    /// Class containing the PolySpatial settings asset.
    /// </summary>
    public class PolySpatialSettings : ScriptableObject
#if UNITY_EDITOR
        ,ISerializationCallbackReceiver
#endif
    {
        public enum PolySpatialTextureCompressionFormat
        {
            Unknown = 0,
            ETC = 1,
            ETC2 = 2,
            ASTC = 3,
            PVRTC = 4,
            DXTC = 5,
            BPTC = 6,
            DXTC_RGTC = 7
        }

        public enum NetworkingMode
        {
            Local,
            LocalAndClient,
        };

        public enum RecordingMode
        {
            None,
            Record,
            Playback
        };

        public enum ParticleReplicationMode
        {
            ReplicateProperties,
            BakeToMesh,
#if POLYSPATIAL_INTERNAL
            ExperimentalBakeToTexture
#endif
        };

        [Serializable]
        public struct ProjectionHalfAngles
        {
            public float left;
            public float right;
            public float top;
            public float bottom;
        }

        [Serializable]
        public struct DisplayProviderParameters
        {
            public int framebufferWidth;
            public int framebufferHeight;
            public Pose leftEyePose;
            public Pose rightEyePose;
            public ProjectionHalfAngles leftProjectionHalfAngles;
            public ProjectionHalfAngles rightProjectionHalfAngles;
        }

#if UNITY_EDITOR
        const string k_DefaultAssetPath = "Assets/Resources/PolySpatialSettings.asset";
        const string k_SessionRecordingModeKey = "PolySpatial.Session.Recording.Mode";
        const string k_SessionRecordingFilePathKey = "PolySpatial.Session.Recording.FilePath";
        const string k_SessionRecordingFrameRateKey = "PolySpatial.Session.Recording.FrameRate";
        const string k_SessionRecordingFrameLimit = "PolySpatial.Session.Recording.FrameLimit";
#endif

        const int k_DefaultMaxMipByteSizePerCycle = 128000;

        static PolySpatialSettings s_Instance;

        /// <summary>
        /// Gets a reference for an instance of the PolySpatial settings asset in the project.
        /// </summary>
        public static PolySpatialSettings instance {
            get {
                InitializeInstance();
                return s_Instance;
            }
        }

        static void InitializeInstance()
        {
            if (s_Instance != null)
                return;
#if UNITY_EDITOR
            s_Instance = AssetDatabase.LoadAssetAtPath<PolySpatialSettings>(k_DefaultAssetPath);
            if (s_Instance == null)
            {
                s_Instance = CreateInstance<PolySpatialSettings>();

                // Dispatch asset creation on main thread in case instance is created on worker thread
                EditorApplication.delayCall += () =>
                {
                    // Don't overwrite an existing asset if one already exists
                    if (File.Exists(k_DefaultAssetPath))
                    {
                        Debug.LogError($"A new {nameof(PolySpatialSettings)} asset was initialized when its asset already exists. " +
                            $"Was {nameof(PolySpatialSettings)}.{nameof(instance)} used by an asset importer?");

                        return;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(k_DefaultAssetPath));
                    AssetDatabase.CreateAsset(s_Instance, k_DefaultAssetPath);
                    EditorUtility.SetDirty(s_Instance);
                    AssetDatabase.SaveAssetIfDirty(s_Instance);
                };
            }
#else
            s_Instance = Resources.Load<PolySpatialSettings>("PolySpatialSettings");
            if (s_Instance == null)
            {
                Debug.LogWarning("PolySpatialSettings not found in Resources folder. Using default settings.");
                s_Instance = CreateInstance<PolySpatialSettings>();
            }

            RuntimeOverrideFromEnvironment(s_Instance);
#endif
        }

#if UNITY_EDITOR
        // what to do when we enter play mode
        internal static RecordingMode SessionRecordingMode
        {
            get => (RecordingMode)SessionState.GetInt(k_SessionRecordingModeKey, (int)RecordingMode.None);
            set => SessionState.SetInt(k_SessionRecordingModeKey, (int)value);
        }

        //The FrameRate to use when recording
        internal static int SessionRecordingFrameRate
        {
            get => SessionState.GetInt(k_SessionRecordingFrameRateKey, 0);
            set => SessionState.SetInt(k_SessionRecordingFrameRateKey, value);
        }

        internal static bool SessionLimitFramerateWhenRecording
        {
            get => SessionState.GetBool(k_SessionRecordingFrameLimit, false);
            set => SessionState.SetBool(k_SessionRecordingFrameLimit, value);
        }

        // the target file path for recording or playback
        internal static string SessionRecordingFilePath
        {
            get => SessionState.GetString(k_SessionRecordingFilePathKey, null);
            set => SessionState.SetString(k_SessionRecordingFilePathKey, value);
        }

        internal static void EraseSessionRecordingMode()
        {
            SessionState.EraseInt(k_SessionRecordingModeKey);
        }

        internal static void EraseSessionRecordingFilePath()
        {
            SessionState.EraseString(k_SessionRecordingFilePathKey);
        }
#endif

        internal static string FindCmdArg(string arg)
        {
            var cmdargs = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < cmdargs.Length; i++)
            {
                if (cmdargs[i].ToLowerInvariant() == arg)
                {
                    if (i + 1 < cmdargs.Length)
                        return cmdargs[i + 1];
                    return null;
                }
            }

            return null;
        }

        [Conditional("POLYSPATIAL_INTERNAL")]
        internal static void RuntimeOverrideFromEnvironment(PolySpatialSettings inst)
        {
            // Environment overrides command line variables

            var netmode =
                Environment.GetEnvironmentVariable("POLYSPATIAL_NETMODE") ??
                FindCmdArg("-qnetmode");
            var server =
                Environment.GetEnvironmentVariable("POLYSPATIAL_HOST") ??
                FindCmdArg("-qhost");
            var launchscene =
                Environment.GetEnvironmentVariable("POLYSPATIAL_SCENE") ??
                FindCmdArg("-qscene");

            #if POLYSPATIAL_INTERNAL
            if (!string.IsNullOrEmpty(netmode))
            {
                if (Enum.TryParse(netmode, true, out NetworkingMode mode))
                {
                    inst.m_PolySpatialNetworkingMode = mode;
                }
                else
                {
                    Debug.LogWarning($"Invalid POLYSPATIAL_NETMODE value: {netmode}");
                }
            }

            if (!string.IsNullOrEmpty(server))
            {
                inst.m_SerializedServerAddresses = new List<string> { server };
            }

            inst.m_DemoLaunchSceneName = launchscene;
            #endif
        }

        public const int DefaultServerPort = 9876;
        public const string DefaultServerAddress = "127.0.0.1";
        public const uint DefaultConnectionTimeOut = 5;

        [SerializeField] int m_ConnectionDiscoveryPort = 9877;

        /// <summary>Default port for auto connection discovery</summary>
        public int ConnectionDiscoveryPort => m_ConnectionDiscoveryPort;

        [SerializeField] float m_ConnectionDiscoverySendInterval = 1.0f;

        /// <summary>Default interval between UDP broadcast for auto connection discovery app host</summary>
        public float ConnectionDiscoverySendInterval => m_ConnectionDiscoverySendInterval;

        [SerializeField] float m_ConnectionDiscoveryTimeOutDuration = 5.0f;

        /// <summary>The timeout duration in seconds to mark a connection as Lost for auto connection discovery</summary>
        public float ConnectionDiscoveryTimeOutDuration => m_ConnectionDiscoveryTimeOutDuration;

        [SerializeField] string m_PackageVersion;

        /// <summary>The version of the PolySpatial package</summary>
        public string PackageVersion => m_PackageVersion;

        [SerializeField]
        bool m_EnablePolySpatialRuntime;

        /// <summary>
        /// whether PolySpatial is enabled in this project.
        /// </summary>
        public bool EnablePolySpatialRuntime
        {
            get => m_EnablePolySpatialRuntime;
            set => m_EnablePolySpatialRuntime = value;
        }

        [SerializeField]
        [Tooltip("When enabled, PolySpatial will collect information about its tracked objects. You can see this data in the PolySpatial Statistics windows.")]
        bool m_EnableStatistics;

        internal bool EnableStatistics => m_EnableStatistics;

        [SerializeField]
        [Tooltip("Only colliders in these layers are tracked by PolySpatial.")]
        LayerMask m_ColliderSyncLayerMask = 1;

        internal LayerMask ColliderSyncLayerMask
        {
            get => m_ColliderSyncLayerMask;
            set => m_ColliderSyncLayerMask = value;
        }

        [SerializeField]
        [Tooltip("The technique used to translate particle data to Reality Kit. Replicate Properties: Unity Particle System Properties will be mapped to " +
                 "RealityKit Particle System Properties. BakeToMesh: Particle Systems will be baked to a mesh every frame and rendered in RealityKit.")]
        ParticleReplicationMode m_ParticleMode;

        internal ParticleReplicationMode ParticleMode
        {
            get => m_ParticleMode;
            set => m_ParticleMode = value;
        }

        [SerializeField]
        [Tooltip("Whether or not to track light and reflection probes for PolySpatial Lighting node.")]
        bool m_TrackLightAndReflectionProbes = true;

        internal bool TrackLightAndReflectionProbes => m_TrackLightAndReflectionProbes;

        [SerializeField]
        [Tooltip("GameObjects created in these layers will have tracking completely disabled.")]
        LayerMask m_DisableTrackingMask;

        internal LayerMask DisableTrackingMask
        {
            get => m_DisableTrackingMask;
            set => m_DisableTrackingMask = value;
        }

        [SerializeField]
        [Tooltip("PolySpatial tracker type names to disable. When a tracker is disabled, PolySpatial won't track their respective Unity object types.")]
        string[] m_DisabledTrackers;

        internal string[] DisabledTrackers => m_DisabledTrackers;

        // Additional texture formats to be produced by the PolySpatialTextureImporter.
        // This cannot be a simple serialized field on the PolySpatialSettings ScriptableObject,
        // because the TextureImporter may run before the PolySpatialSettings object is imported - so we need to be
        // able to access these even if the PolySpatialSettings object does not exist. So, instead, the static
        // AdditionalTextureFormats property will read and write from a file next to the ScriptableObjects.
        //
        // So, why have a serialized field here at all then? Because it makes the settings UI in the
        // PolySpatialSettingsProvider "just work". So we have a field backed by the static property using
        // ISerializationCallbackReceiver.
        [SerializeField]
        PolySpatialTextureCompressionFormat[] m_AdditionalTextureFormats;

#if UNITY_EDITOR
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_AdditionalTextureFormats = AdditionalTextureFormats;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            AdditionalTextureFormats = m_AdditionalTextureFormats;
        }
#endif

        public static PolySpatialTextureCompressionFormat[] AdditionalTextureFormats
        {
            get
            {
#if UNITY_EDITOR
                var path = Path.ChangeExtension(k_DefaultAssetPath, "textureFormats");
                if (File.Exists(path))
                    return File.ReadAllLines(path).Select(l => Enum.Parse<PolySpatialTextureCompressionFormat>(l)).ToArray();
#endif
                return null;
            }
            set
            {
#if UNITY_EDITOR
                var path = Path.ChangeExtension(k_DefaultAssetPath, "textureFormats");
                if (value != null)
                    File.WriteAllLines(path, value.Select(f => f.ToString()).ToArray());
                else
                    File.Delete(path);
#endif
            }
        }

        [SerializeField]
        [Tooltip("Default Volume Camera Window configuration, if none is specified on a Volume Camera component. If null, unbounded is assumed.")]
        VolumeCameraWindowConfiguration m_DefaultVolumeCameraWindowConfiguration;

        /// <summary>
        /// Default Volume Camera camera configuration, if none is specified on a Volume Camera component. If null, unbounded is assumed.
        /// </summary>
        public VolumeCameraWindowConfiguration DefaultVolumeCameraWindowConfiguration
        {
            get => m_DefaultVolumeCameraWindowConfiguration;
            set => m_DefaultVolumeCameraWindowConfiguration = value;
        }

        [Obsolete("Renamed to DefaultVolumeCameraWindowConfiguration (UnityUpgradable) -> DefaultVolumeCameraWindowConfiguration")]
        public VolumeCameraWindowConfiguration DefaultVolumeCameraConfiguration
        {
            get => m_DefaultVolumeCameraWindowConfiguration;
            set => m_DefaultVolumeCameraWindowConfiguration = value;
        }

        [SerializeField]
        [Tooltip("When enabled, if there is no Volume Camera after scene load, one will be automatically created using the default settings. Disable this to be able to create the initial Volume Camera from script.")]
        bool m_AutoCreateVolumeCamera = true;

        internal bool AutoCreateVolumeCamera
        {
            get => m_AutoCreateVolumeCamera;
            set => m_AutoCreateVolumeCamera = value;
        }

#if UNITY_EDITOR
        [SerializeField]
        [Tooltip("When enabled, GameObjects created to support PolySpatial preview will be hidden in the Scene view.")]
        bool m_HidePolySpatialPreviewObjectsInScene = true;

        internal bool HidePolySpatialPreviewObjectsInScene => m_HidePolySpatialPreviewObjectsInScene;
#endif

        [SerializeField]
        [Tooltip("When enabled, PolySpatial tracks preview object names and display them in the Hierarchy view.")]
        bool m_TransmitDebugInfo;

        internal bool TransmitDebugInfo
        {
            get => m_TransmitDebugInfo;
            set => m_TransmitDebugInfo = value;
        }

#if UNITY_EDITOR
        [Tooltip("Validations are always enabled when targeting PolySpatial platforms, but turning this option will enable validation on the current build " +
                 "target. This is useful to run validation when developing on Windows, for example.")]
        [SerializeField]
        bool m_ForceValidationForCurrentBuildTarget = true;

        internal bool ForceValidationForCurrentBuildTarget => m_ForceValidationForCurrentBuildTarget;

        [Tooltip("Whether or not to show conversion warnings for shader graphs loaded from packages.")]
        [SerializeField]
        bool m_ShowWarningsForShaderGraphsInPackages = true;

        public bool ShowWarningsForShaderGraphsInPackages => m_ShowWarningsForShaderGraphsInPackages;
#endif

        [Tooltip("Convert unsupported shaders at runtime to a best guess.")]
        [SerializeField]
        bool m_EnableFallbackShaderConversion = true;

        /// <summary>
        /// Convert unsupported shaders at runtime to a best guess.
        /// </summary>
        public bool EnableFallbackShaderConversion => m_EnableFallbackShaderConversion;

#if POLYSPATIAL_INTERNAL
        [SerializeField]
        bool m_EnableTransformVerification;

        internal bool EnableTransformVerification => m_EnableTransformVerification;

        [SerializeField]
        bool m_EnableProgressiveMipStreaming;

        internal bool EnableProgressiveMipStreaming => m_EnableProgressiveMipStreaming;

        [SerializeField]
        long m_MaxMipByteSizePerCycle = k_DefaultMaxMipByteSizePerCycle;

        internal long MaxMipByteSizePerCycle => m_MaxMipByteSizePerCycle;

        [SerializeField]
        bool m_EnableMacRealityKitPreviewInPlayMode;

        internal bool EnableMacRealityKitPreviewInPlayMode => m_EnableMacRealityKitPreviewInPlayMode;

        [SerializeField]
        ulong m_RuntimeFlags = 0;

        internal ulong RuntimeFlags
        {
            get => m_RuntimeFlags;
            set => m_RuntimeFlags = value;
        }

        [Tooltip("PolySpatial creates a mirrored version of your scene on the target runtime. When previewing in the Unity Editor, " +
                 "this option will enable or disable creating these preview clones within Unity.  Changing this setting does not affect behavior of a build.")]
        [SerializeField]
        bool m_EnableInEditorPreview = true;

        internal bool EnableInEditorPreview => m_EnableInEditorPreview;

        [SerializeField]
        [Tooltip("Always links the PolySpatial runtime when making a build.")]
        bool m_AlwaysLinkPolySpatialRuntime;

        public bool AlwaysLinkPolySpatialRuntime => m_AlwaysLinkPolySpatialRuntime;

        [SerializeField]
        NetworkingMode m_PolySpatialNetworkingMode;

        public NetworkingMode PolySpatialNetworkingMode
        {
            get
            {
#if POLYSPATIAL_FORCE_CLIENT
                // POLYSPATIAL_FORCE_CLIENT used for internal testing+dev when mirroring a project to be the client.
                // See Projects/PolySpatialShell/README.md for project mirroring instructions.
                Logging.Log(Unity.PolySpatial.Internals.LogCategory.Debug,
                    "POLYSPATIAL_FORCE_CLIENT #define set, project forced to always be network client.",
                    LogType.Warning);
                return NetworkingMode.Client;
#endif

#if UNITY_EDITOR
                if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
                {
                    return NetworkingMode.LocalAndClient;
                }
#endif

                return m_PolySpatialNetworkingMode;
            }
            set => m_PolySpatialNetworkingMode = value;
        }

        [SerializeField]
        [Tooltip("How long (in seconds) to try connecting to a remote host before timing out.")]
        uint m_ConnectionTimeOut = DefaultConnectionTimeOut;
        internal uint ConnectionTimeOut
        {
            get
            {
#if UNITY_EDITOR
                if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
                    return PolySpatialUserSettings.instance.ConnectionTimeout;
#endif
                return m_ConnectionTimeOut;
            }
            set => m_ConnectionTimeOut = value;
        }

        [SerializeField]
        bool m_EnableHostCameraControl;

        internal bool EnableHostCameraControl
        {
            get => m_EnableHostCameraControl;
            set => m_EnableHostCameraControl = value;
        }

        [SerializeField]
        bool m_EnableClipping;

        public bool EnableClipping => m_EnableClipping;

        public List<SocketAddress> ServerAddresses => m_ServerAddresses.Value;

        readonly Lazy<List<SocketAddress>> m_ServerAddresses = new(() =>
        {
            var results = new List<SocketAddress>();
            if (instance.m_PolySpatialNetworkingMode == NetworkingMode.LocalAndClient)
            {
                foreach (var address in instance.m_SerializedServerAddresses)
                {
                    if (SocketAddress.ParseAddress(address, DefaultServerPort, out var socketAddress))
                    {
                        results.Add(socketAddress);
                    }
                }
            }

#if UNITY_EDITOR
            if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
            {
                foreach (var candidate in PolySpatialUserSettings.instance.ConnectionCandidates)
                {
                    if (candidate.Value.IsSelected && results.All(s => s.Host != candidate.Key.IP && s.Port != candidate.Key.ServerPort)
                        && SocketAddress.ParseAddress(candidate.Key.IP, candidate.Key.ServerPort, out var socketAddress))
                    {
                        results.Add(socketAddress);
                    }
                }
            }
#endif

            return results;
        });

        [SerializeField]
        List<string> m_SerializedServerAddresses = new() { DefaultServerAddress };

        public HashSet<string> IgnoredScenePaths => m_IgnoredScenePaths.Value;

        readonly Lazy<HashSet<string>> m_IgnoredScenePaths = new(() => new HashSet<string>(instance.m_SerializedIgnoredScenePaths));

        [SerializeField]
        List<string> m_SerializedIgnoredScenePaths;

        // Non-serialized value to store the launch scene that comes from
        // the command line or environment.  Used for sample bootstrapping.
        string m_DemoLaunchSceneName = null;
        public static string DemoLaunchSceneName => instance.m_DemoLaunchSceneName;

        // Temporarily defaults to the simulator parameters until HW ship.
        [SerializeField]
        DisplayProviderParameters m_DeviceDisplayProviderParameters = new()
        {
            framebufferWidth = 1920,
            framebufferHeight = 1080,
            leftEyePose =
            {
                position = Vector3.zero,
                rotation = Quaternion.identity
            },
            rightEyePose =
            {
                position = Vector3.zero,
                rotation = Quaternion.identity
            },
            leftProjectionHalfAngles = new()
            {
                left = -1.0f,
                right = 1.0f,
                top = 1.0f,
                bottom = -1.0f
            },
            rightProjectionHalfAngles = new()
            {
                left = -1.0f,
                right = 1.0f,
                top = 1.0f,
                bottom = -1.0f
            },
        };

        public DisplayProviderParameters DeviceDisplayProviderParameters => m_DeviceDisplayProviderParameters;

        [SerializeField]
        DisplayProviderParameters m_SimulatorDisplayProviderParameters = new()
        {
            framebufferWidth = 1920,
            framebufferHeight = 1080,
            leftEyePose =
            {
                position = Vector3.zero,
                rotation = Quaternion.identity
            },
            rightEyePose =
            {
                position = Vector3.zero,
                rotation = Quaternion.identity
            },
            leftProjectionHalfAngles = new()
            {
                left = -1.0f,
                right = 1.0f,
                top = 1.0f,
                bottom = -1.0f
            },
            rightProjectionHalfAngles = new()
            {
                left = -1.0f,
                right = 1.0f,
                top = 1.0f,
                bottom = -1.0f
            },
        };

        public DisplayProviderParameters SimulatorDisplayProviderParameters => m_SimulatorDisplayProviderParameters;

        [SerializeField]
        private bool m_MockBackend = false;

        public bool MockBackend => m_MockBackend;
#else
        internal bool EnableTransformVerification => false;
        internal bool EnableProgressiveMipStreaming => false;
        internal long MaxMipByteSizePerCycle => k_DefaultMaxMipByteSizePerCycle;
        internal bool EnableMacRealityKitPreviewInPlayMode => false;
        internal ulong RuntimeFlags => 0;
        internal bool EnableInEditorPreview => true;

        public NetworkingMode PolySpatialNetworkingMode
        {
            get
            {
#if UNITY_EDITOR
                if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
                {
                    return NetworkingMode.LocalAndClient;
                }
#endif
                return NetworkingMode.Local;
            }
        }

        public uint ConnectionTimeOut
        {
            get
            {
#if UNITY_EDITOR
                if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
                {
                    return PolySpatialUserSettings.instance.ConnectionTimeout;
                }
#endif
                return DefaultConnectionTimeOut;
            }
        }

        internal bool EnableHostCameraControl => false;
        static readonly HashSet<string> s_EmptyStringHash = new();
        public HashSet<string> IgnoredScenePaths => s_EmptyStringHash;
        public int ServerPort => DefaultServerPort;
        public List<SocketAddress> ServerAddresses
        {
            get
            {
                var results = new List<SocketAddress>();
#if UNITY_EDITOR
                if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
                {
                    foreach (var candidate in PolySpatialUserSettings.instance.ConnectionCandidates.Values)
                    {
                        if (candidate.IsSelected && results.All(s => s.Host != candidate.IP && s.Port != candidate.ServerPort)
                                                 && SocketAddress.ParseAddress(candidate.IP, candidate.ServerPort, out var socketAddress))
                        {
                            results.Add(socketAddress);
                        }
                    }
                    return results;
                }
#endif

                results.Add(new SocketAddress() {Host = DefaultServerAddress, Port = DefaultServerPort});
                return results;
            }
        }

        public bool EnableClipping => false;
        public bool EnableServerCameraControl => false;
        public DisplayProviderParameters DeviceDisplayProviderParameters => default;
        public DisplayProviderParameters SimulatorDisplayProviderParameters => default;
        public bool MockBackend => false;
#endif

        void Awake()
        {
            if (m_DefaultVolumeCameraWindowConfiguration == null)
                m_DefaultVolumeCameraWindowConfiguration = Resources.Load<VolumeCameraWindowConfiguration>("Default Unbounded Configuration");
        }

#if UNITY_EDITOR
        internal void LoadPackageVersion()
        {
            var assembly = typeof(PolySpatialSettings).Assembly;
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);

            if (m_PackageVersion == packageInfo.version) return;

            // Only set PolySpatialSettings dirty if the version is different
            m_PackageVersion = packageInfo.version;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}
