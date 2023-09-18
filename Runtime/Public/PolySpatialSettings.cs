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

#if UNITY_EDITOR
        const string k_DefaultAssetPath = "Assets/Resources/PolySpatialSettings.asset";
        const string k_SessionRecordingModeKey = "PolySpatial.Session.Recording.Mode";
        const string k_SessionRecordingFilePathKey = "PolySpatial.Session.Recording.FilePath";
        const string k_SessionRecordingFrameRateKey = "PolySpatial.Session.Recording.FrameRate";
        const string k_SessionRecordingFrameLimit = "PolySpatial.Session.Recording.FrameLimit";
#endif

        static PolySpatialSettings s_Instance;

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

        [SerializeField] public bool EnablePolySpatialRuntime;
        [SerializeField] public bool EnableStatistics;
        [SerializeField] public bool EnableTransformVerification;
        [SerializeField] public bool EnableMacRealityKitPreviewInPlayMode;
        [SerializeField] public bool EnableProgressiveMipStreaming;
        [SerializeField] public long MaxMipByteSizePerCycle = 128000;
        [SerializeField] public ulong RuntimeFlags = 0;
        [Tooltip("PolySpatial creates a mirrored version of your scene on the target runtime. When previewing in the Unity Editor, " +
            "this option will enable or disable creating these preview clones within Unity.  Changing this setting does not affect behavior of a build.")]
        [SerializeField] public bool EnableInEditorPreview = true;

        [SerializeField] public LayerMask ColliderSyncLayerMask = 1;

        public string[] DisabledTrackers;

        // Additional texture formats to be produced by the PolySpatialTextureImporter.
        // This cannot be a simple serialized field on the PolySpatialSettings ScriptableObject,
        // because the TextureImporter may run before the PolySpatialSettings object is imported - so we need to be
        // able to access these even if the PolySpatialSettings object does not exist. So, instead, the static
        // AdditionalTextureFormats property will read and write from a file next to the ScriptableObjects.
        //
        // So, why have a serialized field here at all then? Because it makes the settings UI in the
        // PolySpatialSettingsProvider "just work". So we have a field backed by the static property using
        // ISerializationCallbackReceiver.
        [SerializeField] public PolySpatialTextureCompressionFormat[] m_AdditionalTextureFormats;

#if UNITY_EDITOR
        public void OnBeforeSerialize()
        {
            m_AdditionalTextureFormats = AdditionalTextureFormats;
        }

        public void OnAfterDeserialize()
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
        [Tooltip("When enabled, a fallback unbounded Volume Camera will be created on startup if none is found in the scene. Disable this to be able to create the initial Volume Camera from script.")]
        public bool EnableDefaultVolumeCamera = true;

#if UNITY_EDITOR
        [SerializeField]
        [Tooltip("When enabled, GameObjects created to support PolySpatial preview will be hidden in the Scene view.")]
        internal bool HidePolySpatialPreviewObjectsInScene = true;
#endif

        [SerializeField] public string MaterialXExtensionNamespace = "mtlxextension";

        [SerializeField]
        bool m_TransmitDebugInfo;

        internal bool TransmitDebugInfo
        {
            get => m_TransmitDebugInfo;
            set => m_TransmitDebugInfo = value;
        }

#if POLYSPATIAL_INTERNAL
        [SerializeField] public bool ForceLinkPolySpatialRuntime;
#endif

#if POLYSPATIAL_INTERNAL
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
                if (PolySpatialUserSettings.instance.ConnectToUniversalPlayer)
                {
                    return NetworkingMode.LocalAndClient;
                }
#endif

                return m_PolySpatialNetworkingMode;
            }
            set => m_PolySpatialNetworkingMode = value;
        }

        [SerializeField]
        NetworkingMode m_PolySpatialNetworkingMode;

        public bool EnableHostCameraControl
        {
            get => m_EnableHostCameraControl;
            internal set => m_EnableHostCameraControl = value;
        }

        [SerializeField]
        bool m_EnableHostCameraControl;

        public bool EnableClipping => m_EnableClipping;

        [SerializeField]
        bool m_EnableClipping;

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
            if (PolySpatialUserSettings.instance.ConnectToUniversalPlayer
                && results.All(s => s.Host != PolySpatialUserSettings.instance.UniversalPlayerIP && s.Port != DefaultServerPort))
            {
                if (SocketAddress.ParseAddress(PolySpatialUserSettings.instance.UniversalPlayerIP, DefaultServerPort, out var socketAddress))
                {
                    results.Add(socketAddress);
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
#else
        public NetworkingMode PolySpatialNetworkingMode
        {
            get
            {
#if UNITY_EDITOR
                if (PolySpatialUserSettings.instance.ConnectToUniversalPlayer)
                {
                    return NetworkingMode.LocalAndClient;
                }
#endif
                return NetworkingMode.Local;
            }
        }

        public bool EnableHostCameraControl;
        static readonly HashSet<string> s_EmptyStringHash = new();
        public HashSet<string> IgnoredScenePaths => s_EmptyStringHash;
        public int ServerPort => DefaultServerPort;
        public List<SocketAddress> ServerAddresses
        {
            get
            {
#if UNITY_EDITOR
                if (PolySpatialUserSettings.instance.ConnectToUniversalPlayer &&
                    SocketAddress.ParseAddress(PolySpatialUserSettings.instance.UniversalPlayerIP, DefaultServerPort, out var socketAddress))
                {
                    return new() {socketAddress};
                }
#endif
                return new() {new SocketAddress() {Host = DefaultServerAddress, Port = DefaultServerPort}};
            }
        }

        public bool EnableClipping => false;
        public bool EnableServerCameraControl => false;
#endif
    }
}
