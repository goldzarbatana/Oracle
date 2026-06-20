using System;
using System.Threading;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncRewardedResult = Cysharp.Threading.Tasks.UniTask<UniversalMonetization.RewardedAdResult>;
using AsyncInterstitialResult = Cysharp.Threading.Tasks.UniTask<UniversalMonetization.InterstitialAdResult>;
#else
using System.Threading.Tasks;
using AsyncRewardedResult = System.Threading.Tasks.Task<UniversalMonetization.RewardedAdResult>;
using AsyncInterstitialResult = System.Threading.Tasks.Task<UniversalMonetization.InterstitialAdResult>;
#endif

namespace UniversalMonetization
{
    /// <summary>
    /// Dependency Injection interface for AdManager.
    /// Can be registered in VContainer/Zenject: builder.RegisterComponent(AdManager.Instance).As<IAdManagerService>();
    /// </summary>
    public interface IAdManagerService
    {
        bool IsInitialized { get; }
        
        event Action OnSDKInitialized;
        event Action<string> OnSDKInitializationFailed;
        event Action<AdImpressionData> OnImpressionDataReceived;

        bool IsRewardedAdReady(string placement = null);
        AsyncRewardedResult ShowRewardedAdAsync(int coinsRewardAmount = 0, CancellationToken ct = default);

        bool IsInterstitialAdReady(string placement = null);
        AsyncInterstitialResult ShowInterstitialAdAsync(string placement = null, CancellationToken ct = default);

        void ShowBanner();
        void HideBanner();
        int GetBannerHeight();
        
        void LaunchTestSuite();
        void SetConsent(bool consent);
    }
}
