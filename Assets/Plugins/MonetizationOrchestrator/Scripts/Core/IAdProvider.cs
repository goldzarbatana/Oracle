using System;
using System.Threading;
using System.Threading.Tasks;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncResultTask = Cysharp.Threading.Tasks.UniTask<bool>;
#else
using AsyncResultTask = System.Threading.Tasks.Task<bool>;
#endif

namespace UniversalMonetization
{
    public interface IAdProvider : IDisposable
    {
        bool IsInitialized { get; }
        
        void Configure(
            string rewardedId, 
            string interstitialId, 
            string bannerId, 
            bool enableTestSuite,
            bool bannerAtTop
        );
        
        void Initialize(string appKey);
        
        // Lifecycle Events
        event Action OnProviderInitialized;
        event Action<string> OnInitializationFailed;

        // Rewarded Events & Methods
        event Action OnRewardedAdLoaded;
        event Action<string> OnRewardedAdLoadFailed;
        event Action OnRewardedAdDisplayed;
        event Action<string> OnRewardedAdDisplayFailed;
        event Action OnRewardedAdClicked;
        event Action OnRewardedAdRewarded;
        event Action OnRewardedAdClosed;

        bool IsRewardedAdReady();
        void LoadRewardedAd();
        void ShowRewardedAd(string placementId = null);
        bool IsRewardedPlacementCapped(string placementId);
        AsyncResultTask ShowRewardedAdAsync(string placementId, CancellationToken ct = default);
        
        // Interstitial Events & Methods
        event Action OnInterstitialAdLoaded;
        event Action<string> OnInterstitialAdLoadFailed;
        event Action OnInterstitialAdDisplayed;
        event Action<string> OnInterstitialAdDisplayFailed;
        event Action OnInterstitialAdClicked;
        event Action OnInterstitialAdClosed;

        bool IsInterstitialAdReady();
        void LoadInterstitialAd();
        void ShowInterstitialAd(string placementId = null);
        AsyncResultTask ShowInterstitialAdAsync(string placementId, CancellationToken ct = default);

        // Banner Events & Methods
        event Action OnBannerAdLoaded;
        event Action<string> OnBannerAdLoadFailed;
        event Action OnBannerAdDisplayed;

        void LoadBanner(string unitId);
        void ShowBanner();
        void HideBanner();
        int GetBannerHeight();
        
        // Impression Data callback for custom analytics tracking
        event Action<AdImpressionData> OnImpressionDataReady;
        
        // Debugging
        void LaunchTestSuite();
        
        // Regulatory & Compliance (GDPR, COPPA, CCPA)
        void SetConsent(bool consent);
        void SetMetaData(string key, string value);
        
        // Application State
        void OnApplicationPause(bool isPaused);
    }
}
