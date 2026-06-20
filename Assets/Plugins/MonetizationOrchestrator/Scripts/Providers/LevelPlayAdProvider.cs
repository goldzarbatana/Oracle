#pragma warning disable 67

using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncResultTask = Cysharp.Threading.Tasks.UniTask<bool>;
#else
using AsyncResultTask = System.Threading.Tasks.Task<bool>;
#endif

#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
using Unity.Services.LevelPlay;
#endif

namespace UniversalMonetization
{
    public class LevelPlayAdProvider : IAdProvider
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

#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
        private LevelPlayRewardedAd _rewardedAd;
        private LevelPlayInterstitialAd _interstitialAd;
        private LevelPlayBannerAd _bannerAd;

        private string _rewardedId;
        private string _interstitialId;
        private string _bannerId;
        private bool _initRequested;
        private bool _enableTestSuite;
        private bool _rewardedLoadInFlight;
        private bool _interstitialLoadInFlight;
        private bool _bannerLoadInFlight;
        private bool _bannerAtTop;
#endif

        public void Configure(string rewardedId, string interstitialId, string bannerId, bool enableTestSuite, bool bannerAtTop)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            _rewardedId = rewardedId;
            _interstitialId = interstitialId;
            _bannerId = bannerId;
            _enableTestSuite = enableTestSuite && Debug.isDebugBuild;
            _bannerAtTop = bannerAtTop;

            if (_enableTestSuite)
            {
                LevelPlay.SetMetaData("is_test_suite", "enable"); 
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            LevelPlay.SetMetaData("internal_is_debug_logs", "true");
#endif
#else
            Debug.Log("[LevelPlayAdProvider] Configured in Stub Mode.");
#endif
        }

        public void Initialize(string appKey)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (_initRequested || IsInitialized) return;
            _initRequested = true;

            try 
            {
                LevelPlayPrivacySettings.SetCOPPA(false);
                LevelPlay.SetMetaData("Google_Family_Self_Certified_SDKS", "true");
                LevelPlay.SetMetaData("is_privacy_sandbox_enabled", "true");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LevelPlayAdProvider] Failed to set pre-init metadata: {ex.Message}");
            }

            LevelPlay.OnInitSuccess += HandleInitSuccess;
            LevelPlay.OnInitFailed += HandleInitFailed;
            LevelPlay.OnImpressionDataReady += HandleImpressionDataReady;

            try
            {
                Debug.Log($"[LevelPlayAdProvider] Initializing LevelPlay with appKey: {appKey}");
                LevelPlay.Init(appKey);
            }
            catch (Exception ex)
            {
                OnInitializationFailed?.Invoke(ex.Message);
            }
#else
            Debug.LogWarning("[LevelPlayAdProvider] LevelPlay SDK is disabled or unavailable. Initialization skipped.");
#endif
        }

#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
        private void HandleInitSuccess(LevelPlayConfiguration config)
        {
            IsInitialized = true;
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;

            Debug.Log("[LevelPlayAdProvider] LevelPlay SDK Initialized Successfully!");

            SetupInstances();
            OnProviderInitialized?.Invoke();
        }

        private void HandleInitFailed(LevelPlayInitError error)
        {
            _initRequested = false;
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;
            string err = $"[LevelPlay SDK Error {error.ErrorCode}] {error.ErrorMessage}";
            OnInitializationFailed?.Invoke(err);
        }

        private void SetupInstances()
        {
            // --- Rewarded ---
            _rewardedAd = new LevelPlayRewardedAd(_rewardedId);
            _rewardedAd.OnAdLoaded += info => { _rewardedLoadInFlight = false; OnRewardedAdLoaded?.Invoke(); };
            _rewardedAd.OnAdLoadFailed += err => { _rewardedLoadInFlight = false; OnRewardedAdLoadFailed?.Invoke(err.ErrorMessage); };
            _rewardedAd.OnAdDisplayed += info => OnRewardedAdDisplayed?.Invoke();
            _rewardedAd.OnAdDisplayFailed += (info, err) => OnRewardedAdDisplayFailed?.Invoke(err.ErrorMessage);
            _rewardedAd.OnAdClicked += info => OnRewardedAdClicked?.Invoke();
            _rewardedAd.OnAdRewarded += (info, reward) => OnRewardedAdRewarded?.Invoke();
            _rewardedAd.OnAdClosed += info => OnRewardedAdClosed?.Invoke();

            // --- Interstitial ---
            _interstitialAd = new LevelPlayInterstitialAd(_interstitialId);
            _interstitialAd.OnAdLoaded += info => { _interstitialLoadInFlight = false; OnInterstitialAdLoaded?.Invoke(); };
            _interstitialAd.OnAdLoadFailed += err => { _interstitialLoadInFlight = false; OnInterstitialAdLoadFailed?.Invoke(err.ErrorMessage); };
            _interstitialAd.OnAdDisplayed += info => OnInterstitialAdDisplayed?.Invoke();
            _interstitialAd.OnAdDisplayFailed += (info, err) => OnInterstitialAdDisplayFailed?.Invoke(err.ErrorMessage);
            _interstitialAd.OnAdClicked += info => OnInterstitialAdClicked?.Invoke();
            _interstitialAd.OnAdClosed += info => OnInterstitialAdClosed?.Invoke();

            // --- Banner ---
            if (!string.IsNullOrWhiteSpace(_bannerId))
            {
                var bannerConfig = new LevelPlayBannerAd.Config.Builder()
                    .SetSize(LevelPlayAdSize.CreateAdaptiveAdSize())
                    .SetPosition(_bannerAtTop ? LevelPlayBannerPosition.TopCenter : LevelPlayBannerPosition.BottomCenter)
                    .SetDisplayOnLoad(true)
                    .Build();

                _bannerAd = new LevelPlayBannerAd(_bannerId, bannerConfig);
                _bannerAd.OnAdLoaded += info => { _bannerLoadInFlight = false; OnBannerAdLoaded?.Invoke(); };
                _bannerAd.OnAdLoadFailed += err => { _bannerLoadInFlight = false; OnBannerAdLoadFailed?.Invoke(err.ErrorMessage); };
                _bannerAd.OnAdDisplayed += info => OnBannerAdDisplayed?.Invoke();
            }
        }

        private void HandleImpressionDataReady(LevelPlayImpressionData data)
        {
            if (data == null) return;
            OnImpressionDataReady?.Invoke(new AdImpressionData
            {
                AdNetwork = data.AdNetwork,
                AdUnit = data.AdFormat,
                InstanceName = data.InstanceName,
                Revenue = data.Revenue ?? 0.0,
                Currency = "USD"
            });
        }
#endif

        // ========================
        // IADPROVIDER API
        // ========================
        public bool IsRewardedAdReady()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            return _rewardedAd != null && _rewardedAd.IsAdReady();
#else
            return false;
#endif
        }

        public void LoadRewardedAd()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (_rewardedAd == null || _rewardedLoadInFlight || _rewardedAd.IsAdReady()) return;
            _rewardedLoadInFlight = true;
            _rewardedAd.LoadAd();
#endif
        }

        public void ShowRewardedAd(string placementId = null)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (_rewardedAd == null) return;
            if (string.IsNullOrEmpty(placementId)) _rewardedAd.ShowAd();
            else _rewardedAd.ShowAd(placementId);
#endif
        }

        public bool IsRewardedPlacementCapped(string placementId)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            return LevelPlayRewardedAd.IsPlacementCapped(placementId);
#else
            return false;
#endif
        }

        public async AsyncResultTask ShowRewardedAdAsync(string placementId, CancellationToken ct = default)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (!IsRewardedAdReady()) return false;

            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => tcs.TrySetResult(false)))
            {
                Action<LevelPlayAdInfo, LevelPlayReward> rewardHandler = (info, reward) => tcs.TrySetResult(true);
                Action<LevelPlayAdInfo> closeHandler = info => tcs.TrySetResult(false);

                _rewardedAd.OnAdRewarded += rewardHandler;
                _rewardedAd.OnAdClosed += closeHandler;

                ShowRewardedAd(placementId);

                try
                {
                    return await tcs.Task;
                }
                finally
                {
                    if (_rewardedAd != null)
                    {
                        _rewardedAd.OnAdRewarded -= rewardHandler;
                        _rewardedAd.OnAdClosed -= closeHandler;
                    }
                }
            }
#else
            await Task.Yield();
            return false;
#endif
        }

        public bool IsInterstitialAdReady()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            return _interstitialAd != null && _interstitialAd.IsAdReady();
#else
            return false;
#endif
        }

        public void LoadInterstitialAd()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (_interstitialAd == null || _interstitialLoadInFlight || _interstitialAd.IsAdReady()) return;
            _interstitialLoadInFlight = true;
            _interstitialAd.LoadAd();
#endif
        }

        public void ShowInterstitialAd(string placementId = null)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (_interstitialAd == null) return;
            if (string.IsNullOrEmpty(placementId)) _interstitialAd.ShowAd();
            else _interstitialAd.ShowAd(placementId);
#endif
        }

        public async AsyncResultTask ShowInterstitialAdAsync(string placementId, CancellationToken ct = default)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (!IsInterstitialAdReady()) return false;

            var tcs = new TaskCompletionSource<bool>();
            using (ct.Register(() => tcs.TrySetResult(false)))
            {
                Action<LevelPlayAdInfo> closeHandler = info => tcs.TrySetResult(true);
                Action<LevelPlayAdInfo, LevelPlayAdError> failHandler = (info, err) => tcs.TrySetResult(false);

                _interstitialAd.OnAdClosed += closeHandler;
                _interstitialAd.OnAdDisplayFailed += failHandler;

                ShowInterstitialAd(placementId);

                try
                {
                    return await tcs.Task;
                }
                finally
                {
                    if (_interstitialAd != null)
                    {
                        _interstitialAd.OnAdClosed -= closeHandler;
                        _interstitialAd.OnAdDisplayFailed -= failHandler;
                    }
                }
            }
#else
            await Task.Yield();
            return false;
#endif
        }

        public void LoadBanner(string unitId)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            if (_bannerAd == null || _bannerLoadInFlight) return;
            _bannerLoadInFlight = true;
            _bannerAd.LoadAd();
#endif
        }

        public void ShowBanner()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            _bannerAd?.ShowAd();
#endif
        }

        public void HideBanner()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            _bannerAd?.HideAd();
#endif
        }

        public int GetBannerHeight()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            return 50; // Adaptive default DP height
#else
            return 0;
#endif
        }

        public void LaunchTestSuite()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var testSuiteClass = new AndroidJavaClass("com.ironsource.mediationsdk.testSuite.TestSuiteActivity"))
                using (var explicitIntent = new AndroidJavaObject("android.content.Intent", currentActivity, testSuiteClass))
                {
                    currentActivity.Call("startActivity", explicitIntent);
                }
#else
                LevelPlay.LaunchTestSuite();
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LevelPlayAdProvider] Failed to open Test Suite: {ex.Message}");
            }
#endif
        }

        public void SetConsent(bool consent)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            var consents = new Dictionary<string, bool>
            {
                { "IronSource", consent },
                { "UnityAds", consent }
            };
            LevelPlayPrivacySettings.SetGDPRConsents(consents);
#endif
        }

        public void SetMetaData(string key, string value)
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            LevelPlay.SetMetaData(key, value);
#endif
        }

        public void OnApplicationPause(bool isPaused)
        {
        }

        public void Dispose()
        {
#if MONETIZATION_LEVELPLAY && !UNITY_WEBGL
            LevelPlay.OnInitSuccess -= HandleInitSuccess;
            LevelPlay.OnInitFailed -= HandleInitFailed;
            LevelPlay.OnImpressionDataReady -= HandleImpressionDataReady;

            _rewardedAd?.Dispose();
            _interstitialAd?.Dispose();
            _bannerAd?.Dispose();
#endif
        }
    }
}
