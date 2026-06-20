using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TimeAura.Core.Services
{
    public interface INetworkService
    {
        bool IsAuthenticated { get; }
        UniTask<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
        UniTask<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken cancellationToken = default);
        UniTask<string> UploadFileAsync(string endpoint, byte[] fileData, string fileName, IProgress<float> progress = null, CancellationToken cancellationToken = default);
        UniTask<Texture2D> DownloadTextureAsync(string url, CancellationToken cancellationToken = default);
        void SetAuthToken(string token);
        void ClearAuth();
    }
}
