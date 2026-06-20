using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Centralized Addressables asset loading service with caching and UniTask.
    /// Use for all runtime asset loading: UI, avatars, media, prefabs.
    /// </summary>
    public class AddressableAssetService : IManager
    {
        public AddressableAssetService() { }
        private readonly Dictionary<string, object> _cache = new();
        private readonly List<AsyncOperationHandle> _handles = new();

        public bool IsInitialized { get; private set; }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            // Initialize Addressables system
            var initHandle = Addressables.InitializeAsync();
            await initHandle.ToUniTask(cancellationToken: cancellationToken);

            IsInitialized = true;
            Debug.Log("[AddressableAssetService] Initialized successfully");
        }

        public async UniTask ShutdownAsync()
        {
            // Release all loaded assets
            foreach (var handle in _handles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }

            _handles.Clear();
            _cache.Clear();
            IsInitialized = false;

            await UniTask.Yield();
        }

        /// <summary>
        /// Load asset by address/label with caching
        /// </summary>
        public async UniTask<T> LoadAssetAsync<T>(
            string address,
            bool useCache = true,
            CancellationToken cancellationToken = default) where T : class
        {
            // Check cache first
            if (useCache && _cache.TryGetValue(address, out var cached))
            {
                return cached as T;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                _handles.Add(handle);

                var asset = await handle.ToUniTask(cancellationToken: cancellationToken);

                if (asset == null)
                {
                    throw new AssetLoadException($"Failed to load asset: {address}");
                }

                if (useCache)
                {
                    _cache[address] = asset;
                }

                return asset;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[AddressableAssetService] Load cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetService] Load failed: {address}\n{ex}");
                throw new AssetLoadException($"Failed to load {address}", ex);
            }
        }

        /// <summary>
        /// Load multiple assets by label (e.g., "avatar-accessories", "ui-icons")
        /// </summary>
        public async UniTask<IList<T>> LoadAssetsByLabelAsync<T>(
            string label,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                var handle = Addressables.LoadAssetsAsync<T>(label, null);
                _handles.Add(handle);

                var assets = await handle.ToUniTask(cancellationToken: cancellationToken);

                if (assets == null || assets.Count == 0)
                {
                    Debug.LogWarning($"[AddressableAssetService] No assets found for label: {label}");
                }

                return assets;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[AddressableAssetService] Load by label cancelled: {label}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetService] Load by label failed: {label}\n{ex}");
                throw new AssetLoadException($"Failed to load assets with label {label}", ex);
            }
        }

        /// <summary>
        /// Instantiate prefab from Addressables
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(
            string address,
            Transform parent = null,
            bool instantiateInWorldSpace = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var handle = Addressables.InstantiateAsync(address, parent, instantiateInWorldSpace);
                _handles.Add(handle);

                var instance = await handle.ToUniTask(cancellationToken: cancellationToken);

                if (instance == null)
                {
                    throw new AssetLoadException($"Failed to instantiate: {address}");
                }

                return instance;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[AddressableAssetService] Instantiate cancelled: {address}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetService] Instantiate failed: {address}\n{ex}");
                throw new AssetLoadException($"Failed to instantiate {address}", ex);
            }
        }

        /// <summary>
        /// Preload assets for better performance (e.g., load feed items before showing)
        /// </summary>
        public async UniTask PreloadAssetsAsync(
            IEnumerable<string> addresses,
            CancellationToken cancellationToken = default)
        {
            var tasks = new List<UniTask>();

            foreach (var address in addresses)
            {
                tasks.Add(LoadAssetAsync<UnityEngine.Object>(address, true, cancellationToken));
            }

            await UniTask.WhenAll(tasks);
            Debug.Log($"[AddressableAssetService] Preloaded {tasks.Count} assets");
        }

        /// <summary>
        /// Check if asset exists at address (useful before loading)
        /// </summary>
        public async UniTask<bool> AssetExistsAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var locations = await Addressables.LoadResourceLocationsAsync(address)
                    .ToUniTask(cancellationToken: cancellationToken);

                return locations != null && locations.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get download size for remote content (for user feedback)
        /// </summary>
        public async UniTask<long> GetDownloadSizeAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var sizeHandle = Addressables.GetDownloadSizeAsync(address);
                var size = await sizeHandle.ToUniTask(cancellationToken: cancellationToken);
                return size;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableAssetService] Get download size failed: {address}\n{ex}");
                return 0;
            }
        }

        /// <summary>
        /// Download dependencies (for offline mode or preloading)
        /// </summary>
        public async UniTask DownloadDependenciesAsync(
            string address,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var downloadHandle = Addressables.DownloadDependenciesAsync(address);

                while (!downloadHandle.IsDone)
                {
                    progress?.Report(downloadHandle.PercentComplete);
                    await UniTask.Yield(cancellationToken);
                }

                if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    throw new AssetLoadException($"Download failed: {address}");
                }

                Debug.Log($"[AddressableAssetService] Downloaded dependencies: {address}");
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[AddressableAssetService] Download cancelled: {address}");
                throw;
            }
        }

        /// <summary>
        /// Clear cache for specific address
        /// </summary>
        public void ClearCache(string address)
        {
            _cache.Remove(address);
        }

        /// <summary>
        /// Clear all cache
        /// </summary>
        public void ClearAllCache()
        {
            _cache.Clear();
        }
    }

    public class AssetLoadException : Exception
    {
        public AssetLoadException(string message) : base(message) { }
        public AssetLoadException(string message, Exception inner) : base(message, inner) { }
    }
}
