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
        bool connectToPlayToDevice;

        internal bool ConnectToPlayToDevice
        {
            get => connectToPlayToDevice;
            set
            {
                connectToPlayToDevice = value;
                Save(true);
            }
        }

        [SerializeField]
        string playToDeviceIP = PolySpatialSettings.DefaultServerAddress;

        internal string PlayToDeviceIP
        {
            get => playToDeviceIP;
            set
            {
                playToDeviceIP = value;
                Save(true);
            }
        }
    }
}
#endif
