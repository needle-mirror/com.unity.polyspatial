#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Unity.PolySpatial
{
    /// <summary>
    /// Class that holds the PolySpatial user settings values per project.
    /// This class is serialized in the project UserSettings folder.
    /// </summary>
    [FilePath("UserSettings/PolySpatialUserSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    class PolySpatialUserSettings : ScriptableSingleton<PolySpatialUserSettings>
    {
        [SerializeField]
        bool m_ConnectToUniversalPlayer;

        internal bool ConnectToUniversalPlayer
        {
            get => m_ConnectToUniversalPlayer;
            set
            {
                m_ConnectToUniversalPlayer = value;
                Save(true);
            }
        }

        [SerializeField]
        string m_UniversalPlayerIP = PolySpatialSettings.DefaultServerAddress;

        internal string UniversalPlayerIP
        {
            get => m_UniversalPlayerIP;
            set
            {
                m_UniversalPlayerIP = value;
                Save(true);
            }
        }
    }
}
#endif
