using System.IO;
using BA2LW.Utils;
using UnityEngine;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Settings Manager")]
    public class SettingsManager : Singleton<SettingsManager>
    {
        const string DATA_DIRECTORY = "Data";
        const string SETTINGS_FILE = "settings.json";

        public string dataPath { get; private set; }
        public string settingsPath { get; private set; }
        public Setting settings { get; private set; }

        void Awake()
        {
            dataPath = Path.Combine(GetRootPath(), DATA_DIRECTORY);
            settingsPath = Path.Combine(dataPath, SETTINGS_FILE);
            Initialize();
        }

        async void Initialize()
        {
            Debug.Log($"Getting {settingsPath}...");
            string setting = await WebRequestHelper.GetTextData(settingsPath);
            settings = JsonUtility.FromJson<Setting>(setting);
            Debug.Log($"Successfully get {settingsPath}.");
        }

        /// <summary>
        /// Get current application/project root directory.
        /// </summary>
        /// <returns>Application/project root path</returns>
        string GetRootPath()
        {
#if UNITY_ANDROID || UNITY_WEBGl && !UNITY_EDITOR
            string path =
                $"file:///{Directory.GetParent(Application.persistentDataPath)!.ToString()}";
#elif UNITY_STANDALONE_OSX
            string path = $"file://{Directory.GetParent(Application.dataPath)!.ToString()}";
#else
            string path = Directory.GetParent(Application.dataPath)!.ToString();
#endif
            return path;
        }
    }
}
