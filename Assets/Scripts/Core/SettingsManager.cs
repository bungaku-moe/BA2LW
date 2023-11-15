using System;
using System.IO;
using BA2LW.Serialization;
using BA2LW.Utils;
using UnityEngine;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Settings Manager")]
    public class SettingsManager : Singleton<SettingsManager>
    {
        const string DATA_DIRECTORY = "Data";
        const string CONFIG_FILE = "config.json";
        const string SETTINGS_FILE = "settings.json";

        /// <summary>
        /// Root path of Spine data.
        /// </summary>
        /// <value></value>
        public string dataPath { get; private set; }

        /// <summary>
        /// Global configuration file path.
        /// </summary>
        /// <value></value>
        public string configPath { get; private set; }

        /// <summary>
        /// Spine settings path.
        /// </summary>
        /// <value></value>
        public string settingsPath { get; private set; }

        /// <summary>
        /// The root directory of currently  active wallpaper.
        /// </summary>
        /// <value></value>
        public string currentWallpaperPath { get; private set; }

        /// <summary>
        /// Global configuration data.
        /// </summary>
        /// <value></value>
        public GlobalConfig config { get; private set; }

        /// <summary>
        /// Spine settings data.
        /// </summary>
        /// <value></value>
        public SpineSettings settings { get; private set; }

        void Awake()
        {
            dataPath = Path.Combine(GetRootPath(), DATA_DIRECTORY);
            configPath = Path.Combine(dataPath, CONFIG_FILE);

            GetGlobalConfig();
        }

        void Initialize()
        {
            currentWallpaperPath = Path.Combine(dataPath, config.wallpaper);
            GetSpineSettings();
            SetFrameRate(config.fps);
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

        async void GetGlobalConfig()
        {
            try
            {
                Debug.Log($"[GET] Get global configuration: {configPath}");

                string conf = await WebRequestHelper.GetTextData(configPath);
                config = JsonUtility.FromJson<GlobalConfig>(conf);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR] Failed to get global configuration! {ex.Message}");
            }
            finally
            {
                Debug.Log("[OK] Successfully get global configuration.");
                Initialize();
            }
        }

        async void GetSpineSettings()
        {
            settingsPath = Path.Combine(currentWallpaperPath, SETTINGS_FILE);

            try
            {
                Debug.Log($"[GET] Get Spine settings: {settingsPath}...");

                string setting = await WebRequestHelper.GetTextData(settingsPath);
                settings = JsonUtility.FromJson<SpineSettings>(setting);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ERROR] Failed to get Spine settings! {ex.Message}");
            }
            finally
            {
                Debug.Log("[OK] Successfully get Spine settings.");
            }
        }

        void SetFrameRate(int fps)
        {
            Application.targetFrameRate = fps;
        }
    }
}
