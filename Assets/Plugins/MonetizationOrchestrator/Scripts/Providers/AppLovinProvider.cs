#if MONETIZATION_APPLOVIN_ENABLED
using System;
using UnityEngine;
using System.Threading;
#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncResultTask = Cysharp.Threading.Tasks.UniTask<bool>;
#else
using System.Threading.Tasks;
using AsyncResultTask = System.Threading.Tasks.Task<bool>;
#endif

namespace UniversalMonetization
{
    public class AppLovinProvider : IAdProvider
    {
        public bool IsInitialized { get; private set; }

        public event Action OnProviderInitialized;
        public event Action<string> OnInitializationFailed;

        public event Action OnRewardedAdLoaded;
        public event Action<string> OnRewardedAdLoadFailed;
        public event Action OnRewardedAdDisplayed;
        public event Action<string> OnRewardedAdDisplayFailed;
        public event Action OnRewardedAdClicked;
        public event Action OnRewardedAdRewarded;
        public event Action OnRewardedAdClosed;

        public event Action OnInterstitialAdLoaded;
        public event Action<string> OnInterstitialAdLoadFailed;
        public event Action OnInterstitialAdDisplayed;
        public event Action<string> OnInterstitialAdDisplayFailed;
        public event Action OnInterstitialAdClicked;
        public event Action OnInterstitialAdClosed;

        public event Action OnBannerAdLoaded;
        public event Action<string> OnBannerAdLoadFailed;
        public event Action OnBannerAdDisplayed;
        
        public event Action<AdImpressionData> OnImpressionDataReady;

        private string _rewardedId;
        private string _interstitialId;
        private string _bannerId;
        private bool _bannerAtTop;
        private bool _enableTestSuite;

        public void Configure(string rewardedId, string interstitialId, string bannerId, bool enableTestSuite, bool bannerAtTop)
        {
            _rewardedId = rewardedId;
            _interstitialId = interstitialId;
            _bannerId = bannerId;
            _enableTestSuite = enableTestSuite;
            _bannerAtTop = bannerAtTop;
        }

        public void Initialize(string appKey)
        {
            if (string.IsNullOrEmpty(appKey))
            {
                OnInitializationFailed?.Invoke("AppLovin AppKey is empty. Initialization aborted.");
                return;
            }

            MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            {
                IsInitialized = true;
                OnProviderInitialized?.Invoke();
            };

            MaxSdk.SetSdkKey(appKey);
            MaxSdk.InitializeSdk();

            RegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            // Rewarded
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (adUnitId, adInfo) => OnRewardedAdLoaded?.Invoke();
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (adUnitId, errorInfo) => OnRewardedAdLoadFailed?.Invoke(errorInfo.Message);
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (adUnitId, adInfo) => OnRewardedAdDisplayed?.Invoke();
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) => OnRewardedAdDisplayFailed?.Invoke(errorInfo.Message);
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += (adUnitId, adInfo) => OnRewardedAdClicked?.Invoke();
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (adUnitId, reward, adInfo) => OnRewardedAdRewarded?.Invoke();
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (adUnitId, adInfo) => OnRewardedAdClosed?.Invoke();

            // Interstitial
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (adUnitId, adInfo) => OnInterstitialAdLoaded?.Invoke();
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (adUnitId, errorInfo) => OnInterstitialAdLoadFailed?.Invoke(errorInfo.Message);
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (adUnitId, adInfo) => OnInterstitialAdDisplayed?.Invoke();
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (adUnitId, errorInfo, adInfo) => OnInterstitialAdDisplayFailed?.Invoke(errorInfo.Message);
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += (adUnitId, adInfo) => OnInterstitialAdClicked?.Invoke();
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (adUnitId, adInfo) => OnInterstitialAdClosed?.Invoke();

            // Banner
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += (adUnitId, adInfo) => OnBannerAdLoaded?.Invoke();
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (adUnitId, errorInfo) => OnBannerAdLoadFailed?.Invoke(errorInfo.Message);
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += (adUnitId, adInfo) => OnBannerAdDisplayed?.Invoke();

            // Revenue
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += HandleAdRevenuePaid;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += HandleAdRevenuePaid;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += HandleAdRevenuePaid;
        }

        private void HandleAdRevenuePaid(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adInfo == null) return;
            var data = new AdImpressionData
            {
                AdNetwork = adInfo.NetworkName,
                AdUnit = adUnitId,
                InstanceName = adInfo.Placement,
                Revenue = adInfo.Revenue,
                Currency = "USD"
            };
            OnImpressionDataReady?.Invoke(data);
        }

        public bool IsRewardedAdReady() => MaxSdk.IsRewardedAdReady(_rewardedId);
        public void LoadRewardedAd() => MaxSdk.LoadRewardedAd(_rewardedId);
        public async AsyncResultTask ShowRewardedAdAsync(string placementId, CancellationToken ct = default)
        {
            MaxSdk.ShowRewardedAd(_rewardedId, placementId);
            return true;
        }
        public bool IsRewardedPlacementCapped(string placementId) => false; 

        public bool IsInterstitialAdReady() => MaxSdk.IsInterstitialAdReady(_interstitialId);
        public void LoadInterstitialAd() => MaxSdk.LoadInterstitialAd(_interstitialId);
        public async AsyncResultTask ShowInterstitialAdAsync(string placementId, CancellationToken ct = default)
        {
            MaxSdk.ShowInterstitialAd(_interstitialId, placementId);
            return true;
        }

        public void LoadBanner(string unitId)
        {
            MaxSdkBase.BannerPosition position = _bannerAtTop ? MaxSdkBase.BannerPosition.TopCenter : MaxSdkBase.BannerPosition.BottomCenter;
            MaxSdk.CreateBanner(_bannerId, position);
        }
        
        public void ShowBanner() => MaxSdk.ShowBanner(_bannerId);
        public void HideBanner() => MaxSdk.HideBanner(_bannerId);
        public int GetBannerHeight() => 50;

        public void LaunchTestSuite()
        {
            if (_enableTestSuite)
                MaxSdk.ShowMediationDebugger();
        }

        public void SetConsent(bool consent)
        {
            MaxSdk.SetHasUserConsent(consent);
        }

        public void SetMetaData(string key, string value)
        {
            // Handled via MaxSdk API if needed
        }

        public void OnApplicationPause(bool isPaused)
        {
            // AppLovin handles pauses internally on iOS/Android
        }

        public void Dispose()
        {
            // Cleanup if necessary
        }
    }
}
#endif
