#pragma warning disable 67

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncResultTask = Cysharp.Threading.Tasks.UniTask<bool>;
#else
using AsyncResultTask = System.Threading.Tasks.Task<bool>;
#endif

#if MONETIZATION_UNITYADS && !UNITY_WEBGL
using UnityEngine.Advertisement;
#endif

namespace UniversalMonetization
{
    public class UnityAdsProvider : IAdProvider
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
        , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
#endif
    {
        public bool IsInitialized { get; private set; }

        // Lifecycle Events
        public event Action OnProviderInitialized;
        public event Action<string> OnInitializationFailed;

        // Rewarded Events
        public event Action OnRewardedAdLoaded;
        public event Action<string> OnRewardedAdLoadFailed;
        public event Action OnRewardedAdDisplayed;
        public event Action<string> OnRewardedAdDisplayFailed;
        public event Action OnRewardedAdClicked;
        public event Action OnRewardedAdRewarded;
        public event Action OnRewardedAdClosed;

        // Interstitial Events
        public event Action OnInterstitialAdLoaded;
        public event Action<string> OnInterstitialAdLoadFailed;
        public event Action OnInterstitialAdDisplayed;
        public event Action<string> OnInterstitialAdDisplayFailed;
        public event Action OnInterstitialAdClicked;
        public event Action OnInterstitialAdClosed;

        // Banner Events
        public event Action OnBannerAdLoaded;
        public event Action<string> OnBannerAdLoadFailed;
        public event Action OnBannerAdDisplayed;

        public event Action<AdImpressionData> OnImpressionDataReady;

#if MONETIZATION_UNITYADS && !UNITY_WEBGL
        private string _rewardedId;
        private string _interstitialId;
        private string _bannerId;
        private bool _testMode;
        private bool _bannerAtTop;
        private bool _isRewardedLoaded;
        private bool _isInterstitialLoaded;
        private bool _isBannerLoaded;
        private bool _bannerVisible;
#endif

        public void Configure(string rewardedId, string interstitialId, string bannerId, bool enableTestSuite, bool bannerAtTop)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            _rewardedId = rewardedId;
            _interstitialId = interstitialId;
            _bannerId = bannerId;
            _testMode = enableTestSuite;
            _bannerAtTop = bannerAtTop;
#else
            Debug.Log("[UnityAdsProvider] Configured in Stub Mode.");
#endif
        }

        public void Initialize(string appKey)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            if (Advertisement.isInitialized || !Advertisement.isSupported) return;
            Debug.Log($"[UnityAdsProvider] Initializing Unity Ads with appKey: {appKey}");
            Advertisement.Initialize(appKey, _testMode, this);
#else
            Debug.LogWarning("[UnityAdsProvider] Unity Ads SDK is disabled or unavailable. Initialization skipped.");
#endif
        }

        // ========================
        // UNITY ADS CALLBACKS
        // ========================
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
        public void OnInitializationComplete()
        {
            IsInitialized = true;
            Debug.Log("[UnityAdsProvider] Unity Ads SDK Initialized Successfully!");
            OnProviderInitialized?.Invoke();
        }

        void IUnityAdsInitializationListener.OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            string err = $"[UnityAds SDK InitError {error}] {message}";
            OnInitializationFailed?.Invoke(err);
        }

        // --- Load Callbacks ---
        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (placementId == _rewardedId)
            {
                _isRewardedLoaded = true;
                OnRewardedAdLoaded?.Invoke();
            }
            else if (placementId == _interstitialId)
            {
                _isInterstitialLoaded = true;
                OnInterstitialAdLoaded?.Invoke();
            }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            string err = $"Load failed for placement '{placementId}' ({error}): {message}";
            if (placementId == _rewardedId)
            {
                _isRewardedLoaded = false;
                OnRewardedAdLoadFailed?.Invoke(err);
            }
            else if (placementId == _interstitialId)
            {
                _isInterstitialLoaded = false;
                OnInterstitialAdLoadFailed?.Invoke(err);
            }
        }

        // --- Show Callbacks ---
        public void OnUnityAdsShowStart(string placementId)
        {
            if (placementId == _rewardedId)
            {
                OnRewardedAdDisplayed?.Invoke();
            }
            else if (placementId == _interstitialId)
            {
                OnInterstitialAdDisplayed?.Invoke();
            }
        }

        public void OnUnityAdsShowClick(string placementId)
        {
            if (placementId == _rewardedId)
            {
                OnRewardedAdClicked?.Invoke();
            }
            else if (placementId == _interstitialId)
            {
                OnInterstitialAdClicked?.Invoke();
            }
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            if (placementId == _rewardedId)
            {
                if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
                {
                    OnRewardedAdRewarded?.Invoke();
                }
                OnRewardedAdClosed?.Invoke();
            }
            else if (placementId == _interstitialId)
            {
                OnInterstitialAdClosed?.Invoke();
            }
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            string err = $"Show failed for placement '{placementId}' ({error}): {message}";
            if (placementId == _rewardedId)
            {
                OnRewardedAdDisplayFailed?.Invoke(err);
            }
            else if (placementId == _interstitialId)
            {
                OnInterstitialAdDisplayFailed?.Invoke(err);
            }
        }
#endif

        // ========================
        // IADPROVIDER API
        // ========================
        public bool IsRewardedAdReady()
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            return IsInitialized && _isRewardedLoaded;
#else
            return false;
#endif
        }

        public void LoadRewardedAd()
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            if (!IsInitialized || _isRewardedLoaded) return;
            Advertisement.Load(_rewardedId, this);
#endif
        }

        public void ShowRewardedAd(string placementId = null)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            string id = string.IsNullOrEmpty(placementId) ? _rewardedId : placementId;
            _isRewardedLoaded = false;
            Advertisement.Show(id, this);
#endif
        }

        public bool IsRewardedPlacementCapped(string placementId) => false;

        public async AsyncResultTask ShowRewardedAdAsync(string placementId, CancellationToken ct = default)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            if (!IsRewardedAdReady()) return false;

            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => tcs.TrySetResult(false)))
            {
                Action rewardHandler = () => tcs.TrySetResult(true);
                Action closeHandler = () => tcs.TrySetResult(false);

                OnRewardedAdRewarded += rewardHandler;
                OnRewardedAdClosed += closeHandler;

                ShowRewardedAd(placementId);

                try
                {
                    return await tcs.Task;
                }
                finally
                {
                    OnRewardedAdRewarded -= rewardHandler;
                    OnRewardedAdClosed -= closeHandler;
                }
            }
#else
            await Task.Yield();
            return false;
#endif
        }

        public bool IsInterstitialAdReady()
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            return IsInitialized && _isInterstitialLoaded;
#else
            return false;
#endif
        }

        public void LoadInterstitialAd()
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            if (!IsInitialized || _isInterstitialLoaded) return;
            Advertisement.Load(_interstitialId, this);
#endif
        }

        public void ShowInterstitialAd(string placementId = null)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            string id = string.IsNullOrEmpty(placementId) ? _interstitialId : placementId;
            _isInterstitialLoaded = false;
            Advertisement.Show(id, this);
#endif
        }

        public async AsyncResultTask ShowInterstitialAdAsync(string placementId, CancellationToken ct = default)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            if (!IsInterstitialAdReady()) return false;

            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => tcs.TrySetResult(false)))
            {
                Action closeHandler = () => tcs.TrySetResult(true);
                OnInterstitialAdClosed += closeHandler;

                ShowInterstitialAd(placementId);

                try
                {
                    return await tcs.Task;
                }
                finally
                {
                    OnInterstitialAdClosed -= closeHandler;
                }
            }
#else
            await Task.Yield();
            return false;
#endif
        }

        // --- Banners ---
        public void LoadBanner(string unitId)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            if (_isBannerLoaded) return;
            
            var options = new BannerLoadOptions
            {
                loadCallback = () => { _isBannerLoaded = true; OnBannerAdLoaded?.Invoke(); },
                errorCallback = msg => { _isBannerLoaded = false; OnBannerAdLoadFailed?.Invoke(msg); }
            };
            
            var pos = _bannerAtTop ? BannerPosition.TOP_CENTER : BannerPosition.BOTTOM_CENTER;
            Advertisement.Banner.SetPosition(pos);
            Advertisement.Banner.Load(_bannerId, options);
#endif
        }

        public void ShowBanner()
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            var options = new BannerOptions
            {
                showCallback = () => { _bannerVisible = true; OnBannerAdDisplayed?.Invoke(); }
            };
            Advertisement.Banner.Show(_bannerId, options);
#endif
        }

        public void HideBanner()
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            _bannerVisible = false;
            Advertisement.Banner.Hide();
#endif
        }

        public int GetBannerHeight()
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            return _bannerVisible ? 50 : 0;
#else
            return 0;
#endif
        }

        // --- Misc ---
        public void LaunchTestSuite()
        {
            Debug.Log("[UnityAdsProvider] LaunchTestSuite: Not supported on Unity Ads.");
        }

        public void SetConsent(bool consent)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            // Set user consent (CCPA / GDPR)
            MetaData consentMetadata = new MetaData("gdpr");
            consentMetadata.Set("consent", consent ? "true" : "false");
            Advertisement.SetMetaData(consentMetadata);
#endif
        }

        public void SetMetaData(string key, string value)
        {
#if MONETIZATION_UNITYADS && !UNITY_WEBGL
            MetaData md = new MetaData(key);
            md.Set(key, value);
            Advertisement.SetMetaData(md);
#endif
        }

        public void OnApplicationPause(bool isPaused)
        {
        }

        public void Dispose()
        {
        }
    }
}
