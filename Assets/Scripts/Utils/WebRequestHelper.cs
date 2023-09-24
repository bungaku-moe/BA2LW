using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace BA2LW.Utils
{
    public static class WebRequestHelper
    {
        /// <summary>
        /// Request file and retrieve the data as text.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Text data.</returns>
        public static async Task<string> GetTextData(string url)
        {
            UnityWebRequest uwr = UnityWebRequest.Get(url);
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            uwr.SendWebRequest().completed += operation =>
            {
                if (uwr.result == UnityWebRequest.Result.Success)
                    tcs.SetResult(uwr.downloadHandler.text);
                else
                    tcs.SetException(new Exception(uwr.error));
                uwr.Dispose(); // Dispose of the UnityWebRequest to free up resources.
            };

            return await tcs.Task;
        }

        /// <summary>
        /// Request file and retrieve the data as bytes.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Array of byte.</returns>
        public static async Task<byte[]> GetBytesData(string url)
        {
            UnityWebRequest uwr = UnityWebRequest.Get(url);
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();

            uwr.SendWebRequest().completed += operation =>
            {
                if (uwr.result == UnityWebRequest.Result.Success)
                    tcs.SetResult(uwr.downloadHandler.data);
                else
                    tcs.SetException(new Exception(uwr.error));
                uwr.Dispose(); // Dispose of the UnityWebRequest to free up resources.
            };

            return await tcs.Task;
        }

        /// <summary>
        /// Get audio clip and automatically assign it's AudioType.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<AudioClip> GetAudioClip(string url)
        {
            UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, GetAudioType(url));
            TaskCompletionSource<AudioClip> tcs = new TaskCompletionSource<AudioClip>();

            uwr.SendWebRequest().completed += operation =>
            {
                if (uwr.result == UnityWebRequest.Result.Success)
                    tcs.SetResult(DownloadHandlerAudioClip.GetContent(uwr));
                else
                    tcs.SetException(new Exception(uwr.error));
                uwr.Dispose(); // Dispose of the UnityWebRequest to free up resources.
            };

            return await tcs.Task;
        }

        /// <summary>
        /// Automatically assign AudioType for *.mp2, *.mp3, *.ogg, *.wav files.
        /// </summary>
        /// <param name="url"></param>
        /// <returns>AudioType</returns>
        public static AudioType GetAudioType(string url)
        {
            switch (Path.GetExtension(url))
            {
                case ".mp2":
                case ".mp3":
                    return AudioType.MPEG;
                case ".ogg":
                    return AudioType.OGGVORBIS;
                case ".wav":
                    return AudioType.WAV;
                default:
                    return AudioType.UNKNOWN;
            }
        }
    }
}
