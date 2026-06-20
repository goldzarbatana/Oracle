using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Modern network service for social network app using UniTask.
    /// Handles REST API calls, file uploads, and authentication.
    /// </summary>
    public class NetworkService : INetworkService, IManager
    {
        [Inject] private AppConfig _appConfig;
        private string _authToken;

        [Inject]
        public NetworkService() { }

        public bool IsInitialized { get; private set; }
        public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            // Restore auth token from PlayerPrefs or secure storage
            _authToken = PlayerPrefs.GetString("AuthToken", string.Empty);
            IsInitialized = true;
            await UniTask.Yield(cancellationToken);
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        /// <summary>
        /// Perform GET request with UniTask
        /// </summary>
        public async UniTask<TResponse> GetAsync<TResponse>(
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            var baseUrl = _appConfig != null ? _appConfig.ApiBaseUrl : "https://api.timeaura.com";
            var url = $"{baseUrl}/{endpoint}";
            using var request = UnityWebRequest.Get(url);

            AddAuthHeader(request);

            try
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new NetworkException($"GET {endpoint} failed: {request.error}");
                }

                var json = request.downloadHandler.text;
                return JsonUtility.FromJson<TResponse>(json);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"GET {endpoint} cancelled");
                throw;
            }
        }

        /// <summary>
        /// Perform POST request with JSON body
        /// </summary>
        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            CancellationToken cancellationToken = default)
        {
            var baseUrl = _appConfig != null ? _appConfig.ApiBaseUrl : "https://api.timeaura.com";
            var url = $"{baseUrl}/{endpoint}";
            var json = JsonUtility.ToJson(data);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer()
            };

            request.SetRequestHeader("Content-Type", "application/json");
            AddAuthHeader(request);

            try
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new NetworkException($"POST {endpoint} failed: {request.error}");
                }

                var responseJson = request.downloadHandler.text;
                return JsonUtility.FromJson<TResponse>(responseJson);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"POST {endpoint} cancelled");
                throw;
            }
        }

        /// <summary>
        /// Upload file (avatar, image, etc.) with progress tracking
        /// </summary>
        public async UniTask<string> UploadFileAsync(
            string endpoint,
            byte[] fileData,
            string fileName,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var baseUrl = _appConfig != null ? _appConfig.ApiBaseUrl : "https://api.timeaura.com";
            var url = $"{baseUrl}/{endpoint}";

            var form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", fileData, fileName, "image/jpeg")
            };

            using var request = UnityWebRequest.Post(url, form);
            AddAuthHeader(request);

            try
            {
                var operation = request.SendWebRequest();

                // Track upload progress
                while (!operation.isDone)
                {
                    progress?.Report(operation.progress);
                    await UniTask.Yield(cancellationToken);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new NetworkException($"Upload {fileName} failed: {request.error}");
                }

                var response = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                return response.url;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Upload {fileName} cancelled");
                throw;
            }
        }

        /// <summary>
        /// Download texture from URL (for avatars, images) with Addressables-like pattern
        /// </summary>
        public async UniTask<Texture2D> DownloadTextureAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);

            try
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new NetworkException($"Download texture failed: {request.error}");
                }

                return DownloadHandlerTexture.GetContent(request);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Download texture {url} cancelled");
                throw;
            }
        }

        public void SetAuthToken(string token)
        {
            _authToken = token;
            PlayerPrefs.SetString("AuthToken", token);
            PlayerPrefs.Save();
        }

        public void ClearAuth()
        {
            _authToken = string.Empty;
            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.Save();
        }

        private void AddAuthHeader(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
        }

        [Serializable]
        private class UploadResponse
        {
            public string url;
        }
    }

    public class NetworkException : Exception
    {
        public NetworkException(string message) : base(message) { }
    }
}
