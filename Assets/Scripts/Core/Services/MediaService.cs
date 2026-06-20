using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using Firebase.Storage;
#endif

namespace TimeAura.Core.Services
{
    public class MediaService : IManager
    {
        public MediaService() { }
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
        private FirebaseStorage _storage;
        private StorageReference _storageRoot;
#endif

        public bool IsInitialized { get; private set; }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[Media] 📷 Calibrating lens for material preservation...");
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            _storage = FirebaseStorage.DefaultInstance;
            _storageRoot = _storage.RootReference;
#endif
            IsInitialized = true;
            await UniTask.Yield();
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        public async UniTask<byte[]> CaptureMaterialAsync()
        {
            Debug.Log("[Media] 🎞️ Capturing Material from local realm...");
            #if UNITY_EDITOR
            await UniTask.Delay(500);
            var tex = new Texture2D(128, 128);
            for(int y=0; y<128; y++) for(int x=0; x<128; x++) tex.SetPixel(x, y, Color.Lerp(Color.blue, Color.black, (float)y/128));
            tex.Apply();
            return tex.EncodeToJPG();
            #else
            await UniTask.Yield();
            return null;
            #endif
        }

        public async UniTask<byte[]> PickImageAsync()
        {
            Debug.Log("[Media] 🔍 Seeking Visage in the local archives...");
            
#if UNITY_EDITOR
            await UniTask.Delay(1000);
            var tex = new Texture2D(256, 256);
            for(int y=0; y<256; y++) for(int x=0; x<256; x++) 
                tex.SetPixel(x, y, Color.Lerp(new Color(0.8f, 0.7f, 0.2f), Color.black, (float)Vector2.Distance(new Vector2(x,y), new Vector2(128,128))/180f));
            tex.Apply();
            return tex.EncodeToJPG();
#elif UNITY_ANDROID || UNITY_IOS
            var utcs = new UniTaskCompletionSource<byte[]>();
            
            NativeGallery.GetImageFromGallery((path) =>
            {
                if (path == null)
                {
                    utcs.TrySetResult(null);
                    return;
                }

                byte[] data = File.ReadAllBytes(path);
                utcs.TrySetResult(data);
            }, "Select your Visage", "image/*");

            return await utcs.Task;
#else
            await UniTask.Yield();
            return null;
#endif
        }

        public async UniTask<byte[]> TakePhotoAsync()
        {
            Debug.Log("[Media] 📸 Activating the sacred lens...");
            
#if UNITY_EDITOR
            return await PickImageAsync(); // Use same mock for editor
#elif UNITY_ANDROID || UNITY_IOS
            var utcs = new UniTaskCompletionSource<byte[]>();
            
            NativeCamera.TakePicture((path) =>
            {
                if (path == null)
                {
                    utcs.TrySetResult(null);
                    return;
                }

                byte[] data = File.ReadAllBytes(path);
                utcs.TrySetResult(data);
            }, 1024); // Limit to 1024 for profile pic

            return await utcs.Task;
#else
            await UniTask.Yield();
            return null;
#endif
        }

        public async UniTask<string> PreserveMaterialAsync(byte[] data, string sessionId, string mediaId)
        {
            if (data == null || data.Length == 0) return null;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            string path = $"harmony_chats/{sessionId}/{mediaId}.jpg";
            var materialRef = _storageRoot.Child(path);

            Debug.Log($"[Media] 📤 Preserving Material at {path}...");
            try
            {
                await materialRef.PutBytesAsync(data);
                var downloadUrl = await materialRef.GetDownloadUrlAsync();
                return downloadUrl.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Media] ❌ Preservation failed: {e.Message}");
                return null;
            }
#else
            await UniTask.Yield();
            return "mock_material_url";
#endif
        }
    }
}
