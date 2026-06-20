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

#if MONETIZATION_ADMOB
using GoogleMobileAds.Api;
#endif

namespace UniversalMonetization
{
    public class AdMobProvider : IAdProvider
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

#if MONETIZATION_ADMOB
        private string _rewardedId;
        private string _interstitialId;
        private string _bannerId;
        private bool _bannerAtTop;
        
        private RewardedAd _rewardedAd;
        private InterstitialAd _interstitialAd;
        private BannerView _bannerView;
#endif

        public void Configure(string rewardedId, string interstitialId, string bannerId, bool enableTestSuite, bool bannerAtTop)
        {
#if MONETIZATION_ADMOB
            _rewardedId = rewardedId;
            _interstitialId = interstitialId;
            _bannerId = bannerId;
            _bannerAtTop = bannerAtTop;
#else
            Debug.Log("[AdMobProvider] Configured in Stub Mode. Define MONETIZATION_ADMOB to enable AdMob SDK.");
#endif
        }

        public void Initialize(string appKey)
        {
#if MONETIZATION_ADMOB
            if (IsInitialized) return;
            Debug.Log($"[AdMobProvider] Initializing Google Mobile Ads SDK...");
            MobileAds.Initialize(initStatus =>
            {
                IsInitialized = true;
                Debug.Log("[AdMobProvider] AdMob SDK Initialized Successfully!");
                
                // Ensure callbacks run on main thread
                // Fortunately, GoogleMobileAds now routes callbacks to the main thread automatically in newer versions
                OnProviderInitialized?.Invoke();
            });
#else
            Debug.LogWarning("[AdMobProvider] AdMob SDK is disabled or unavailable. Initialization skipped.");
#endif
        }

        // ========================
        // REWARDED ADS
        // ========================
        public bool IsRewardedAdReady()
        {
#if MONETIZATION_ADMOB
            return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
            return false;
#endif
        }

        public void LoadRewardedAd()
        {
#if MONETIZATION_ADMOB
            if (!IsInitialized) return;
            
            // Clean up old ad
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            var request = new AdRequest();
            RewardedAd.Load(_rewardedId, request, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    OnRewardedAdLoadFailed?.Invoke(error?.GetMessage() ?? "Unknown Load Error");
                    return;
                }

                _rewardedAd = ad;
                RegisterRewardedAdCallbacks(_rewardedAd);
                OnRewardedAdLoaded?.Invoke();
                
                // Track ILRD
                _rewardedAd.OnAdPaid += (AdValue adValue) => TrackImpression(adValue, "Rewarded", _rewardedId);
            });
#endif
        }

        public void ShowRewardedAd(string placementId = null)
        {
#if MONETIZATION_ADMOB
            if (_rewardedAd != null && _rewardedAd.CanShowAd())
            {
                _rewardedAd.Show((Reward reward) =>
                {
                    OnRewardedAdRewarded?.Invoke();
                });
            }
            else
            {
                OnRewardedAdDisplayFailed?.Invoke("Ad is not ready to be shown.");
            }
#endif
        }

        public bool IsRewardedPlacementCapped(string placementId) => false;

        public async AsyncResultTask ShowRewardedAdAsync(string placementId, CancellationToken ct = default)
        {
#if MONETIZATION_ADMOB
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

#if MONETIZATION_ADMOB
        private void RegisterRewardedAdCallbacks(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () => OnRewardedAdDisplayed?.Invoke();
            ad.OnAdFullScreenContentClosed += () => OnRewardedAdClosed?.Invoke();
            ad.OnAdFullScreenContentFailed += (AdError error) => OnRewardedAdDisplayFailed?.Invoke(error.GetMessage());
            ad.OnAdClicked += () => OnRewardedAdClicked?.Invoke();
        }
#endif

        // ========================
        // INTERSTITIAL ADS
        // ========================
        public bool IsInterstitialAdReady()
        {
#if MONETIZATION_ADMOB
            return _interstitialAd != null && _interstitialAd.CanShowAd();
#else
            return false;
#endif
        }

        public void LoadInterstitialAd()
        {
#if MONETIZATION_ADMOB
            if (!IsInitialized) return;
            
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            var request = new AdRequest();
            InterstitialAd.Load(_interstitialId, request, (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    OnInterstitialAdLoadFailed?.Invoke(error?.GetMessage() ?? "Unknown Load Error");
                    return;
                }

                _interstitialAd = ad;
                RegisterInterstitialAdCallbacks(_interstitialAd);
                OnInterstitialAdLoaded?.Invoke();
                
                // Track ILRD
                _interstitialAd.OnAdPaid += (AdValue adValue) => TrackImpression(adValue, "Interstitial", _interstitialId);
            });
#endif
        }

        public void ShowInterstitialAd(string placementId = null)
        {
#if MONETIZATION_ADMOB
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                _interstitialAd.Show();
            }
            else
            {
                OnInterstitialAdDisplayFailed?.Invoke("Ad is not ready to be shown.");
            }
#endif
        }

        public async AsyncResultTask ShowInterstitialAdAsync(string placementId, CancellationToken ct = default)
        {
#if MONETIZATION_ADMOB
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

#if MONETIZATION_ADMOB
        private void RegisterInterstitialAdCallbacks(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () => OnInterstitialAdDisplayed?.Invoke();
            ad.OnAdFullScreenContentClosed += () => OnInterstitialAdClosed?.Invoke();
            ad.OnAdFullScreenContentFailed += (AdError error) => OnInterstitialAdDisplayFailed?.Invoke(error.GetMessage());
            ad.OnAdClicked += () => OnInterstitialAdClicked?.Invoke();
        }
#endif

        // ========================
        // BANNERS
        // ========================
        public void LoadBanner(string unitId)
        {
#if MONETIZATION_ADMOB
            if (_bannerView != null) return;
            
            AdPosition pos = _bannerAtTop ? AdPosition.Top : AdPosition.Bottom;
            _bannerView = new BannerView(_bannerId, AdSize.Banner, pos);

            _bannerView.OnBannerAdLoaded += () => OnBannerAdLoaded?.Invoke();
            _bannerView.OnBannerAdLoadFailed += (LoadAdError error) => OnBannerAdLoadFailed?.Invoke(error.GetMessage());
            _bannerView.OnAdFullScreenContentOpened += () => OnBannerAdDisplayed?.Invoke();
            
            _bannerView.OnAdPaid += (AdValue adValue) => TrackImpression(adValue, "Banner", _bannerId);

            var request = new AdRequest();
            _bannerView.LoadAd(request);
#endif
        }

        public void ShowBanner()
        {
#if MONETIZATION_ADMOB
            _bannerView?.Show();
#endif
        }

        public void HideBanner()
        {
#if MONETIZATION_ADMOB
            _bannerView?.Hide();
#endif
        }

        public int GetBannerHeight()
        {
#if MONETIZATION_ADMOB
            return _bannerView != null ? (int)_bannerView.GetHeightInPixels() : 0;
#else
            return 0;
#endif
        }

        // ========================
        // MISC
        // ========================
#if MONETIZATION_ADMOB
        private void TrackImpression(AdValue adValue, string adUnit, string instanceName)
        {
            var data = new AdImpressionData
            {
                AdNetwork = "GoogleAdMob",
                AdUnit = adUnit,
                InstanceName = instanceName,
                Revenue = adValue.Value / 1000000f, // AdMob returns micro-currency
                Currency = adValue.CurrencyCode
            };
            OnImpressionDataReady?.Invoke(data);
        }
#endif

        public void LaunchTestSuite()
        {
#if MONETIZATION_ADMOB
            Debug.Log("[AdMobProvider] LaunchTestSuite: Use AdMob Inspector instead.");
            MobileAds.OpenAdInspector((AdInspectorError error) => {
                if (error != null) Debug.LogError("Ad Inspector failed to open: " + error.GetMessage());
            });
#endif
        }

        public void SetConsent(bool consent)
        {
#if MONETIZATION_ADMOB
            // In AdMob, UMP (User Messaging Platform) is highly recommended for GDPR
            // This is a placeholder for integrating ConsentInformation
            Debug.Log($"[AdMobProvider] Consent set to: {consent}");
#endif
        }

        public void SetMetaData(string key, string value)
        {
            // AdMob uses AdRequest extras or UMP for metadata
        }

        public void OnApplicationPause(bool isPaused)
        {
        }

        public void Dispose()
        {
#if MONETIZATION_ADMOB
            if (_rewardedAd != null) _rewardedAd.Destroy();
            if (_interstitialAd != null) _interstitialAd.Destroy();
            if (_bannerView != null) _bannerView.Destroy();
#endif
        }
    }
}
