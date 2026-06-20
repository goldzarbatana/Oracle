using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

namespace TimeAura.Core.Utils
{
    public static class ImageLoader
    {
        public static async UniTask<Texture2D> LoadTextureAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ImageLoader] ❌ Failed to load texture: {request.error}");
                    return null;
                }

                return DownloadHandlerTexture.GetContent(request);
            }
        }
    }
}
