using System.Linq;
using Unity.PolySpatial;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PolySpatial.UniversalPlayer
{
    class UniversalPlayerWindow : EditorWindow
    {
        const string k_DownloadURL = "https://unity.com/";
        const string k_VisionOSSimulatorURL = "https://developer.apple.com/documentation/visionOS/interacting-with-your-app-in-the-visionos-simulator";

        const string k_InfoTextFormat = "The Universal Player allows you to play your Apple's <a href=\"{0}\">visionOS Simulator</a> and Vision Pro device " +
                                        "without the need to build and deploy. Information regarding installation can be found below.";

        const string k_DownloadTextFormat = "<a href=\"{0}\">Download the TestFlight App</a>";
        const string k_ConnectionHelpBoxText = "In order to stream your content, the Universal Player app needs to be open in the visionOS simulator or on " +
                                               "your visionPro device.";

        const string k_InvalidIPHelpBoxText = "Invalid IP Address";

        const string k_UniversalPlayerWindowTitle = "Universal Player";
        const string k_UniversalPlayerWindowMenuPath = "Window/PolySpatial/" + k_UniversalPlayerWindowTitle;
        const string k_UniversalPlayerWindowIconPath = "Packages/com.unity.polySpatial/Assets/Textures/Icons/ARVR@4x.png";

        const string k_InfoBox = "InfoBox";
        const string k_InfoTextLabel = "InfoTextLabel";
        const string k_DownloadLinkLabel = "DownloadLinkLabel";
        const string k_InfoBoxSeparator = "InfoBoxSeparator";
        const string k_ConnectionHelpBox = "ConnectionHelpBox";
        const string k_ConnectToPlayerToggle = "ConnectToPlayerToggle";
        const string k_PlayerIPField = "PlayerIPField";
        const string k_InvalidIPHelpBox = "InvalidIPHelpBox";

        static readonly Color k_InfoBoxDarkColor = new Color(0.248f, 0.248f, 0.248f, 1f);
        static readonly Color k_InfoBoxLightColor = new Color(0.87f, 0.87f, 0.87f, 1f);

        static readonly Color k_InfoBoxSeparatorDarkColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        static readonly Color k_InfoBoxSeparatorLightColor = new Color(0.33f, 0.33f, 0.33f, 1f);

        [MenuItem(k_UniversalPlayerWindowMenuPath)]
        static void LoadUniversalPlayerWindow()
        {
            var window = GetWindow<UniversalPlayerWindow>();
            window.titleContent = new GUIContent(k_UniversalPlayerWindowTitle, AssetDatabase.LoadAssetAtPath<Texture2D>(k_UniversalPlayerWindowIconPath));
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

        [SerializeField]
        VisualTreeAsset m_VisualTreeAsset = default;

        void OnEnable()
        {
            minSize = new Vector2(380, 200);
        }

        void CreateGUI()
        {
            VisualElement uxmlElements = m_VisualTreeAsset.Instantiate();

            uxmlElements.Q<Box>(k_InfoBox).style.backgroundColor = EditorGUIUtility.isProSkin ? k_InfoBoxDarkColor : k_InfoBoxLightColor;
            uxmlElements.Query<VisualElement>(k_InfoBoxSeparator).
                ForEach(s => s.style.backgroundColor = EditorGUIUtility.isProSkin ? k_InfoBoxSeparatorDarkColor : k_InfoBoxSeparatorLightColor);
            uxmlElements.Q<Label>(k_InfoTextLabel).text = string.Format(k_InfoTextFormat, k_VisionOSSimulatorURL);
            uxmlElements.Q<Label>(k_DownloadLinkLabel).text = string.Format(k_DownloadTextFormat, k_DownloadURL);
            uxmlElements.Q<HelpBox>(k_ConnectionHelpBox).text = k_ConnectionHelpBoxText;

            var connectToPlayerToggle = uxmlElements.Q<Toggle>(k_ConnectToPlayerToggle);
            connectToPlayerToggle.value = PolySpatialUserSettings.instance.ConnectToUniversalPlayer;
            connectToPlayerToggle.RegisterValueChangedCallback(evt => PolySpatialUserSettings.instance.ConnectToUniversalPlayer = evt.newValue);

            var invalidIPHelpBox = uxmlElements.Q<HelpBox>(k_InvalidIPHelpBox);
            invalidIPHelpBox.text = k_InvalidIPHelpBoxText;
            invalidIPHelpBox.style.display = IsValidIPAddress(PolySpatialUserSettings.instance.UniversalPlayerIP) ? DisplayStyle.None : DisplayStyle.Flex;

            var playerIPField = uxmlElements.Q<TextField>(k_PlayerIPField);
            playerIPField.value = PolySpatialUserSettings.instance.UniversalPlayerIP;
            playerIPField.RegisterValueChangedCallback(evt =>
            {
                PolySpatialUserSettings.instance.UniversalPlayerIP = evt.newValue;
                invalidIPHelpBox.style.display = IsValidIPAddress(evt.newValue) ? DisplayStyle.None : DisplayStyle.Flex;
            });

            rootVisualElement.Add(uxmlElements);
        }
    }
}
