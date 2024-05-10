using System.IO;
using UnityEngine;

namespace BA2LW.Utils
{
    public static class Utility
    {
        /// <summary>
        /// Get current application/project root directory.
        /// </summary>
        /// <returns>Application/project root full path.</returns>
        public static string GetApplicationPath()
        {
#if UNITY_ANDROID || UNITY_WEBGl && !UNITY_EDITOR
            return $"file:///{Directory.GetParent(Application.persistentDataPath)!.ToString()}";
#elif UNITY_STANDALONE_OSX
            return $"file://{Directory.GetParent(Application.dataPath)!.ToString()}";
#else
            return Directory.GetParent(Application.dataPath)!.ToString();
#endif
        }
    }
}
