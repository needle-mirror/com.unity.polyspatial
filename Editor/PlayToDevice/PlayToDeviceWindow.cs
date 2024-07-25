using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PolySpatial;
using Unity.PolySpatial.Networking;
using UnityEditor.PolySpatial.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Connection = Unity.PolySpatial.PolySpatialUserSettings.Connection;
using ConnectionCandidate = Unity.PolySpatial.PolySpatialUserSettings.ConnectionCandidate;

#if UNITY_HAS_XR_VISIONOS
using UnityEditor.XR.VisionOS;
#endif

namespace UnityEditor.PolySpatial.PlayToDevice
{
    class PlayToDeviceWindow : EditorWindow
    {
        const string k_InfoHelpBoxTextFormat = "Refer to <a href=\"{0}\">this post</a> or the <a href=\"{1}\">package documentation</a> for more info about the Play to Device for PolySpatial.";

        const string k_DiscussionsURL = "https://discussions.unity.com/t/play-to-device/309359";
        const string k_PlayToDeviceDocsURL = "https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@latest/index.html?subfolder=/manual/PlayToDevice.html";

        const string k_MismatchedVersionHelpBoxTextFormat = "The device(s) named {0} have an app version that is not compatible with the installed version of " +
                                                            "PolySpatial v{1} ({2}).";

        const string k_ConnectOnPlayToggleTooltip = "When enabled, your content will be synced to the Play to Device Host each time you enter Play mode. The Play To Device Host must be installed and running within the visionOS or your Vision Pro device.";
        const string k_ConnectionTimeoutFieldTooltip = "How long (in seconds) to try connecting to a remote host before timing out.";

        const string k_DirectConnectionName = "<Direct Connection>";
        const string k_InvalidNameHelpBoxText = "Invalid Name";
        const string k_InvalidIPHelpBoxText = "Invalid IP Address";
        const string k_DuplicateConnectionHelpBoxText = "Device IP address and port conflicts with already available connection.";
        const string k_InvalidPortHelpBoxText = "Invalid Port";
        const string k_DiscoveryPortHelpBoxText = "Port number <b>{0}</b> is being used by the broadcast manager and cannot be used by Play To Device.";

        const string k_PlayToDeviceWindowTitle = "Play To Device";
        const string k_PlayToDeviceWindowMenuPath = "Window/PolySpatial/" + k_PlayToDeviceWindowTitle;
        const string k_PlayToDeviceWindowIconPath = "Packages/com.unity.polySpatial/Assets/Textures/Icons/ARVR@4x.png";
        const string k_PlayToDeviceWindowTreeAssetPath = "Packages/com.unity.polyspatial/Editor/PlayToDevice/PlayToDeviceWindow.uxml";
        const string m_ConnectionListEntryTreeAssetPath = "Packages/com.unity.polyspatial/Editor/PlayToDevice/ConnectionListEntry.uxml";

        const string K_InfoFoldoutName = "InfoFoldout";
        const string k_InfoHelpBox = "InfoHelpBox";
        const string k_ConnectOnPlayDropdown = "ConnectOnPlayDropdown";
        const string k_ConnectionTimeoutField = "ConnectionTimeoutField";
        const string k_AvailableConnectionsFoldoutName = "AvailableConnectionsFoldout";
        const string k_ConnectionList = "ConnectionList";
        const string k_MismatchVersionHelpBox = "MismatchVersionHelpBox";

        const string K_AdvancedSettingsFoldoutName = "AdvancedSettingsFoldout";
        const string k_HostNameField = "HostNameField";
        const string k_InvalidHostNameHelpBox = "InvalidHostNameHelpBox";
        const string k_HostIPField = "HostIPField";
        const string k_InvalidIPHelpBox = "InvalidIPHelpBox";
        const string k_HostPortField = "HostPortField";
        const string k_InvalidPortHelpBox = "InvalidPortHelpBox";
        const string K_AddConnectionButton = "AddConnectionButton";
        const string K_DuplicateConnectionHelpBox = "DuplicateConnectionHelpBox";

        const string k_NoConnectionsSelectedMessage = "<b>Connect on Play</b> is enabled, but no connections have been selected. For Play To Device to work, please select a connection from the list below or add a new connection.";
        const string k_PlayToDeviceNotEnabled = "A connection is selected but <b>Connect on Play</b> is disabled. Enable <b>Connect on Play</b> for Play To Device to work.";
        const string k_BuildNotVisionOSMessage = "The build target is set to <b>{0}</b>. For Play To Device to work you must set the build target to <b>visionOS</b>.";
        const string k_AppModeNotMRMessage = "The current app mode is not compatible with Play To Device. For Play To Device to work go to <b>Project Settings</b> > <b>XR Plug-in Management</b> > <b>Apple visionOS</b> and change the App Mode to <b>Mixed Reality</b>.";

        const string k_SetupErrorsHelpBoxName = "SetupErrors";
#if UNITY_HAS_XR_VISIONOS
        VisionOSSettings.AppMode m_PreviousAppMode;
#endif

        internal const int DirectConnectionMagicCookie = 0;

        [MenuItem(k_PlayToDeviceWindowMenuPath)]
        static void LoadPlayToDeviceWindow()
        {
            var window = GetWindow<PlayToDeviceWindow>();
            window.titleContent = new GUIContent(k_PlayToDeviceWindowTitle, AssetDatabase.LoadAssetAtPath<Texture2D>(k_PlayToDeviceWindowIconPath));
        }

        static bool IsValidHostName(string name)
        {
            return !string.IsNullOrWhiteSpace(name);
        }

        static bool IsValidIPAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            var octets = ipAddress.Split('.');
            if (octets.Length != 4)
                return false;

            return octets.All(o => byte.TryParse(o, out _));
        }

        static bool IsValidPort(int port)
        {
            return port > 1 && port < 65535 && port != PolySpatialSettings.instance.ConnectionDiscoveryPort;
        }

        static int CompareConnectionsByStatus(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.Status == b.Status)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return b.Status.CompareTo(a.Status);
        }

        static int CompareConnectionsByName(ConnectionCandidate a, ConnectionCandidate b)
        {
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        }

        static int CompareConnectionsByAppVersion(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.PlayToDeviceHostVersion == b.PlayToDeviceHostVersion)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return string.Compare(b.PlayToDeviceHostVersion, a.PlayToDeviceHostVersion, StringComparison.Ordinal);
        }

        static int CompareConnectionsByIP(ConnectionCandidate a, ConnectionCandidate b)
        {
            return string.Compare(a.IP, b.IP, StringComparison.Ordinal);
        }

        static int CompareConnectionsByPort(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.ServerPort == b.ServerPort)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return a.ServerPort.CompareTo(b.ServerPort);
        }

        static int CompareConnectionsByIsSelected(ConnectionCandidate a, ConnectionCandidate b)
        {
            if (a.IsSelected == b.IsSelected)
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);

            return b.IsSelected.CompareTo(a.IsSelected);
        }

        static void SortConnections(List<ConnectionCandidate> connectionCandidates, ConnectionsSortOption sortOption)
        {
            switch (sortOption)
            {
                case ConnectionsSortOption.Status:
                    connectionCandidates.Sort(CompareConnectionsByStatus);
                    break;
                case ConnectionsSortOption.Name:
                    connectionCandidates.Sort(CompareConnectionsByName);
                    break;
                case ConnectionsSortOption.PlayToDeviceHostVersion:
                    connectionCandidates.Sort(CompareConnectionsByAppVersion);
                    break;
                case ConnectionsSortOption.IP:
                    connectionCandidates.Sort(CompareConnectionsByIP);
                    break;
                case ConnectionsSortOption.ServerPort:
                    connectionCandidates.Sort(CompareConnectionsByPort);
                    break;
                case ConnectionsSortOption.IsSelected:
                    connectionCandidates.Sort(CompareConnectionsByIsSelected);
                    break;
            }
        }

        [SerializeField]
        VisualTreeAsset m_PlayToDeviceWindowTreeAsset;

        [SerializeField]
        VisualTreeAsset m_ConnectionListEntryTreeAsset;

        [SerializeField]
        string m_HostName = k_DirectConnectionName;

        [SerializeField]
        string m_HostIPAddress = PolySpatialSettings.DefaultServerAddress;

        [SerializeField]
        int m_HostPort = PolySpatialSettings.DefaultServerPort;

        [NonSerialized]
        List<ConnectionCandidate> m_ConnectionCandidates = new List<ConnectionCandidate>();

        ListView m_ConnectionCandidatesListView;
        DropdownField m_ConnectOnPlayToggle;
        Foldout m_AvailableConnectionsFoldout;
        Foldout m_AdvancedSettingsFoldout;
        HelpBox m_MismatchedVersionsHelpBox;
        HelpBox m_DuplicateConnectionHelpBox;

        TextField m_HostNameField;
        TextField m_HostIPField;
        IntegerField m_HostPortField;
        HelpBox m_InvalidHostNameHelpBox;
        HelpBox m_InvalidIPHelpBox;
        HelpBox m_InvalidPortHelpBox;

        SavedBool m_InfoFoldoutState;
        SavedBool m_AvailableConnectionsFoldoutState;
        SavedBool m_AdvancedSettingsFoldoutState;
        string m_MismatchedVersionNames;

        HelpBox m_SetupErrorsHelpBox;

        void OnEnable()
        {
            minSize = new Vector2(380, 400);
            m_InfoFoldoutState = new SavedBool("PolySpatial.PlayToDeviceWindow.InfoFoldoutState", false);
            m_AvailableConnectionsFoldoutState = new SavedBool("PolySpatial.PlayToDeviceWindow.AvailableConnectionsFoldoutState", true);
            m_AdvancedSettingsFoldoutState = new SavedBool("PolySpatial.PlayToDeviceWindow.AdvancedSettingsFoldoutState", false);

            if (m_PlayToDeviceWindowTreeAsset == null)
                m_PlayToDeviceWindowTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_PlayToDeviceWindowTreeAssetPath);

            if (m_ConnectionListEntryTreeAsset == null)
                m_ConnectionListEntryTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(m_ConnectionListEntryTreeAssetPath);

            Refresh();
            ConnectionDiscoveryManager.instance.OnConnectionsChanges += Refresh;
            if (!ConnectionDiscoveryManager.instance.IsListening)
                ConnectionDiscoveryManager.instance.StartListening();

#if UNITY_HAS_XR_VISIONOS
            m_PreviousAppMode = VisionOSSettings.currentSettings == null ? VisionOSSettings.AppMode.MR : VisionOSSettings.currentSettings.appMode;
#endif
        }

        void OnDisable()
        {
            ConnectionDiscoveryManager.instance.OnConnectionsChanges -= Refresh;
            if (ConnectionDiscoveryManager.instance.IsListening)
                ConnectionDiscoveryManager.instance.StopListening();
        }

        internal void Refresh()
        {
            m_ConnectionCandidates.Clear();
            m_ConnectionCandidates.AddRange(PolySpatialUserSettings.instance.ConnectionCandidates.Values);
            SortConnections(m_ConnectionCandidates, PolySpatialEditorUserSettings.instance.ConnectionsSortOption);

            // Reserves the first list element for the header
            m_ConnectionCandidates.Insert(0, null);

            // Reserves the second list element for the empty list message
            if (PolySpatialUserSettings.instance.ConnectionCandidates.Values.Count == 0)
                m_ConnectionCandidates.Add(null);

            if (rootVisualElement.childCount != 0)
                RefreshViews();
        }

        void RefreshViews()
        {
            m_ConnectionCandidatesListView.Rebuild();

            m_MismatchedVersionNames = GetMisMatchedConnectionNames();
            if (string.IsNullOrEmpty(m_MismatchedVersionNames))
            {
                m_MismatchedVersionsHelpBox.style.display = DisplayStyle.None;
            }
            else
            {
                m_MismatchedVersionsHelpBox.text = string.Format(k_MismatchedVersionHelpBoxTextFormat, m_MismatchedVersionNames,
                    PolySpatialSettings.instance.PackageVersion, ((long)PolySpatialMagicCookie.Value).ToString().Substring(0,5));
                m_MismatchedVersionsHelpBox.style.display = DisplayStyle.Flex;
            }

            UpdateSetupErrors();
        }

        string GetMisMatchedConnectionNames()
        {
            return string.Join(", ",
                m_ConnectionCandidates
                    .Where(c =>
                        c != null
                        && c.PlayToDeviceHostMagicCookie != DirectConnectionMagicCookie
                        && c.PlayToDeviceHostMagicCookie != (long)PolySpatialMagicCookie.Value)
                    .Select(c => $"\'{c.Name}\'")
                    .Distinct());
        }

#if UNITY_HAS_XR_VISIONOS
        void Update()
        {
            var currentAppMode = VisionOSSettings.currentSettings == null ? m_PreviousAppMode : VisionOSSettings.currentSettings.appMode;
            if (currentAppMode != m_PreviousAppMode)
            {
                m_PreviousAppMode = currentAppMode;
                UpdateSetupErrors();
            }
        }
#endif

        void UpdateSetupErrors()
        {
            var errorMessage = "";

            var connectionIsSelected = false;
            foreach (var candidate in PolySpatialUserSettings.instance.ConnectionCandidates.Values)
            {
                if (candidate != null && candidate.IsSelected)
                {
                    connectionIsSelected = true;
                    break;
                }
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.VisionOS)
            {
                errorMessage = String.Format(k_BuildNotVisionOSMessage, EditorUserBuildSettings.activeBuildTarget);
                SetEnable(false);
            }
#if UNITY_HAS_XR_VISIONOS
            else if (GetAppMode() != VisionOSSettings.AppMode.MR)
            {
                errorMessage = k_AppModeNotMRMessage;
                SetEnable(false);
            }
#endif
            else if (PolySpatialUserSettings.instance.ConnectToPlayToDevice)
            {
                if (!connectionIsSelected)
                {
                    errorMessage = k_NoConnectionsSelectedMessage;
                    SetEnable(true);
                }
            }
            else if (connectionIsSelected)
            {
                errorMessage = k_PlayToDeviceNotEnabled;
                SetEnable(true);
            }

            if (string.IsNullOrEmpty(errorMessage))
            {
                m_SetupErrorsHelpBox.style.display = DisplayStyle.None;
                SetEnable(true);
            }
            else
            {
                m_SetupErrorsHelpBox.text = errorMessage;
                m_SetupErrorsHelpBox.style.display = DisplayStyle.Flex;
            }
        }

#if UNITY_HAS_XR_VISIONOS
        VisionOSSettings.AppMode GetAppMode()
        {
            var visionOSSettings = VisionOSSettings.currentSettings;
            return visionOSSettings != null ? visionOSSettings.appMode : VisionOSSettings.AppMode.MR;
        }
#endif

        void SetEnable(bool isEnabled)
        {
            m_ConnectOnPlayToggle.SetEnabled(isEnabled);
            m_AvailableConnectionsFoldout.SetEnabled(isEnabled);
            m_AdvancedSettingsFoldout.SetEnabled(isEnabled);
        }

        void CreateGUI()
        {
            VisualElement uxmlElements = m_PlayToDeviceWindowTreeAsset.Instantiate();

            uxmlElements.Q<HelpBox>(k_InfoHelpBox).text = string.Format(k_InfoHelpBoxTextFormat, k_DiscussionsURL, k_PlayToDeviceDocsURL);

            var infoFoldout = uxmlElements.Q<Foldout>(K_InfoFoldoutName);
            infoFoldout.value = m_InfoFoldoutState.Value;
            infoFoldout.RegisterValueChangedCallback(evt => m_InfoFoldoutState.Value = evt.newValue);

            m_ConnectOnPlayToggle = uxmlElements.Q<DropdownField>(k_ConnectOnPlayDropdown);
            m_ConnectOnPlayToggle.index = PolySpatialUserSettings.instance.ConnectToPlayToDevice ? 1 : 0;
            m_ConnectOnPlayToggle.tooltip = k_ConnectOnPlayToggleTooltip;
            m_ConnectOnPlayToggle.RegisterValueChangedCallback(evt =>
            {
                var isEnabled = evt.newValue == PlayToDeviceConfiguration.Enabled.ToString();
                PolySpatialUserSettings.instance.ConnectToPlayToDevice = isEnabled;
                UpdateSetupErrors();
            });

            m_SetupErrorsHelpBox = uxmlElements.Q<HelpBox>(k_SetupErrorsHelpBoxName);

            m_AvailableConnectionsFoldout = uxmlElements.Q<Foldout>(k_AvailableConnectionsFoldoutName);
            m_AvailableConnectionsFoldout.value = m_AvailableConnectionsFoldoutState.Value;
            m_AvailableConnectionsFoldout.RegisterValueChangedCallback(evt => m_AvailableConnectionsFoldoutState.Value = evt.newValue);

            m_ConnectionCandidatesListView = uxmlElements.Q<ListView>(k_ConnectionList);
            m_ConnectionCandidatesListView.makeItem = () =>
            {
                var newListEntry = m_ConnectionListEntryTreeAsset.Instantiate();
                var newListEntryController= new ConnectionListEntryController();
                newListEntry.userData = newListEntryController;
                newListEntryController.SetVisualElement(newListEntry);
                return newListEntry;
            };
            m_ConnectionCandidatesListView.bindItem = (item, index) =>
            {
                (item.userData as ConnectionListEntryController)?.BindData(this, m_ConnectionCandidatesListView, index);
            };
            m_ConnectionCandidatesListView.itemsSource = m_ConnectionCandidates;
            m_MismatchedVersionsHelpBox = uxmlElements.Q<HelpBox>(k_MismatchVersionHelpBox);

            m_DuplicateConnectionHelpBox = uxmlElements.Q<HelpBox>(K_DuplicateConnectionHelpBox);
            m_DuplicateConnectionHelpBox.text = k_DuplicateConnectionHelpBoxText;
            m_DuplicateConnectionHelpBox.style.display = DisplayStyle.None;

            CreateConnectionFields(uxmlElements);

            var addConnectionButton = uxmlElements.Q<Button>(K_AddConnectionButton);
            addConnectionButton.clicked += () =>
            {
                if (ValidateConnectionFields())
                    AddConnection();
            };

            RefreshViews();
            rootVisualElement.Add(uxmlElements);
        }

        void CreateConnectionFields(VisualElement uxmlElements)
        {
            m_AdvancedSettingsFoldout = uxmlElements.Q<Foldout>(K_AdvancedSettingsFoldoutName);
            m_AdvancedSettingsFoldout.value = m_AdvancedSettingsFoldoutState.Value;
            m_AdvancedSettingsFoldout.RegisterValueChangedCallback(evt => m_AdvancedSettingsFoldoutState.Value = evt.newValue);

            var connectionTimeoutField = uxmlElements.Q<UnsignedIntegerField>(k_ConnectionTimeoutField);
            connectionTimeoutField.value = PolySpatialUserSettings.instance.ConnectionTimeout;
            connectionTimeoutField.tooltip = k_ConnectionTimeoutFieldTooltip;
            connectionTimeoutField.RegisterValueChangedCallback(evt => PolySpatialUserSettings.instance.ConnectionTimeout = evt.newValue);

            m_InvalidHostNameHelpBox = uxmlElements.Q<HelpBox>(k_InvalidHostNameHelpBox);
            m_InvalidHostNameHelpBox.text = k_InvalidNameHelpBoxText;
            m_InvalidHostNameHelpBox.style.display = DisplayStyle.None;

            m_InvalidIPHelpBox = uxmlElements.Q<HelpBox>(k_InvalidIPHelpBox);
            m_InvalidIPHelpBox.text = k_InvalidIPHelpBoxText;
            m_InvalidIPHelpBox.style.display = DisplayStyle.None;

            m_InvalidPortHelpBox = uxmlElements.Q<HelpBox>(k_InvalidPortHelpBox);
            m_InvalidPortHelpBox.text = k_InvalidPortHelpBoxText;
            m_InvalidPortHelpBox.style.display = DisplayStyle.None;

            m_HostNameField = uxmlElements.Q<TextField>(k_HostNameField);
            m_HostNameField.value = m_HostName;
            m_HostNameField.RegisterValueChangedCallback(evt =>
            {
                m_HostName = evt.newValue;
                m_InvalidHostNameHelpBox.style.display = DisplayStyle.None;
                m_DuplicateConnectionHelpBox.style.display = DisplayStyle.None;
            });

            m_HostIPField = uxmlElements.Q<TextField>(k_HostIPField);
            m_HostIPField.value = m_HostIPAddress;
            m_HostIPField.RegisterValueChangedCallback(evt =>
            {
                m_HostIPAddress = evt.newValue;
                m_InvalidIPHelpBox.style.display = DisplayStyle.None;
                m_DuplicateConnectionHelpBox.style.display = DisplayStyle.None;
            });

            m_HostPortField = uxmlElements.Q<IntegerField>(k_HostPortField);
            m_HostPortField.value = m_HostPort;
            m_HostPortField.RegisterValueChangedCallback(evt =>
            {
                m_HostPort = evt.newValue;
                m_InvalidPortHelpBox.style.display = DisplayStyle.None;
                m_DuplicateConnectionHelpBox.style.display = DisplayStyle.None;
            });
        }

        bool ValidateConnectionFields()
        {
            var isValidHostname = IsValidHostName(m_HostName);
            var isValidIPAddress = IsValidIPAddress(m_HostIPAddress);
            var isValidPort = IsValidPort(m_HostPort);

            m_InvalidHostNameHelpBox.style.display = isValidHostname ? DisplayStyle.None : DisplayStyle.Flex;
            m_InvalidIPHelpBox.style.display = isValidIPAddress ? DisplayStyle.None : DisplayStyle.Flex;

            if (isValidPort)
            {
                m_InvalidPortHelpBox.style.display = DisplayStyle.None;
            }
            else
            {
                m_InvalidPortHelpBox.style.display = DisplayStyle.Flex;
                m_InvalidPortHelpBox.text = m_HostPort == PolySpatialSettings.instance.ConnectionDiscoveryPort ?
                    string.Format(k_DiscoveryPortHelpBoxText, PolySpatialSettings.instance.ConnectionDiscoveryPort):
                    k_InvalidPortHelpBoxText;
            }

            return isValidHostname && isValidIPAddress && isValidPort;
        }

        void ClearConnectionFields()
        {
            m_HostNameField.value = "";
            m_HostIPField.value = "";
            m_HostPortField.value = PolySpatialSettings.DefaultServerPort;
        }

        void AddConnection()
        {
            var newConnection = new Connection(m_HostIPAddress, m_HostPort);

            if (PolySpatialUserSettings.instance.ConnectionCandidates.ContainsKey(newConnection))
            {
                m_DuplicateConnectionHelpBox.style.display = m_ConnectionCandidates.Any(c =>
                    c != null && c.IP == m_HostIPAddress && c.ServerPort == m_HostPort)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                return;
            }

            var newConnectionCandidate = new ConnectionCandidate()
            {
                IP = m_HostIPAddress,
                Name = m_HostName,
                ServerPort = m_HostPort,
                Status = ConnectionDiscoveryStatus.Lost,
                PlayToDeviceHostVersion = string.Empty,
                LastContact = DateTime.Now
            };

            PolySpatialUserSettings.instance.ConnectionCandidates.Add(newConnection, newConnectionCandidate);

            ClearConnectionFields();
            Refresh();
        }
    }
}
