using System;
using System.IO;
using BA2LW.Serialization;
using BA2LW.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Logging;
using Unity.Logging.Sinks;
using Logger = Unity.Logging.Logger;

namespace BA2LW.Core
{
    [AddComponentMenu("BA2LW/Core/Settings Manager")]
    public class SettingsManager : MonoBehaviour
    {
        [Header("Data & Settings")]
        [SerializeField]
        [Tooltip("Wallpaper data directory name at the application root directory")]
        string m_DataDirectory = "Data";

        [SerializeField]
        [Tooltip("Wallpaper global configuration file name")]
        string m_ConfigFile = "config.json";

        [SerializeField]
        [Tooltip("Spine settings file name")]
        string m_SettingsFile = "settings.json";

        [SerializeField]
        [Tooltip("Log file name")]
        string m_LogFile = "console.log";

        /// <summary>
        /// Root path of the Spine data.
        /// </summary>
        /// <value></value>
        public string DataPath { get; private set; }

        /// <summary>
        /// Global configuration file path.
        /// </summary>
        /// <value></value>
        public string ConfigPath { get; private set; }

        /// <summary>
        /// Spine settings path.
        /// </summary>
        /// <value></value>
        public string SettingsPath { get; private set; }

        /// <summary>
        /// The root directory of currently active wallpaper.
        /// </summary>
        /// <value></value>
        public string CurrentWallpaperPath { get; private set; }

        /// <summary>
        /// Global configuration data.
        /// </summary>
        /// <value></value>
        public GlobalConfig GlobalConfig { get; private set; }

        /// <summary>
        /// Spine settings data.
        /// </summary>
        /// <value></value>
        public SpineSettings Settings { get; private set; }

        async UniTaskVoid Awake()
        {
            SetLoggerConfig();

            DataPath = Path.Combine(Utility.GetApplicationPath(), m_DataDirectory);
            ConfigPath = Path.Combine(DataPath, m_ConfigFile);
            GlobalConfig = await GetGlobalConfig(ConfigPath);

            CurrentWallpaperPath = Path.Combine(DataPath, GlobalConfig.wallpaper);
            SettingsPath = Path.Combine(CurrentWallpaperPath, m_SettingsFile);
            Settings = await GetSpineSettings(SettingsPath);

            SetFrameRate(GlobalConfig.fps);
        }

        /// <summary>
        /// Get wallpaper global configuration.
        /// </summary>
        /// <returns></returns>
        async UniTask<GlobalConfig> GetGlobalConfig(string configPath)
        {
            try
            {
                Log.Info($"Using global configuration: {ConfigPath}");
                return JsonUtility.FromJson<GlobalConfig>(
                    await WebRequestHelper.GetTextData(configPath)
                );
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get global configuration! {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get Spine related settings.
        /// </summary>
        /// <returns></returns>
        async UniTask<SpineSettings> GetSpineSettings(string settingPath)
        {
            try
            {
                Log.Info($"Using Spine settings: {settingPath}...");
                return JsonUtility.FromJson<SpineSettings>(
                    await WebRequestHelper.GetTextData(settingPath)
                );
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get Spine settings! {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Set application frame rate.
        /// </summary>
        /// <param name="fps"></param>
        /// <returns><paramref name="fps"/></returns>
        int SetFrameRate(int fps)
        {
            Application.targetFrameRate = fps;
            return fps;
        }

        /// <summary>
        /// Setup logger configurations.
        /// </summary>
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        void SetLoggerConfig()
        {
            Log.Logger = new Logger(new LoggerConfig()
                .MinimumLevel.Debug()
                .CaptureStacktrace()
                .RedirectUnityLogs()
                .OutputTemplate("[{Timestamp}] <b>[{Level}]</b> <b>{Message}</b>{NewLine}<i>{Stacktrace}</i>")
                .WriteTo.File($"{Path.Combine(Utility.GetApplicationPath(), m_LogFile)}", minLevel: LogLevel.Verbose)
                .WriteTo.UnityEditorConsole());
        }
    }
}
