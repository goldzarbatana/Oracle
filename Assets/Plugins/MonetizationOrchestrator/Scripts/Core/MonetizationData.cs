using System;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncTimerTask = Cysharp.Threading.Tasks.UniTask;
#else
using AsyncTimerTask = System.Threading.Tasks.Task;
#endif

namespace UniversalMonetization
{
    /// <summary>
    /// Represents the outcome of an ad presentation request.
    /// </summary>
    public enum AdResult
    {
        Success,
        Failed,
        SDKNotInitialized,
        Cancelled,
        AdShowing,
        AdsRemoved,
        GDPRBlocked,
        NotReady
    }

    /// <summary>
    /// Represents the detailed outcome of a rewarded ad.
    /// </summary>
    [Serializable]
    public struct RewardedAdResult
    {
        public AdResult Result;
        public string FailureReason;
        public int RewardAmount;
        public string PlacementId;

        public bool IsSuccess => Result == AdResult.Success;

        public static RewardedAdResult CreateSuccess(int amount = 0, string placement = null)
            => new() { Result = AdResult.Success, RewardAmount = amount, PlacementId = placement };

        public static RewardedAdResult CreateFailure(AdResult result, string reason)
            => new() { Result = result, FailureReason = reason };
    }

    /// <summary>
    /// Represents the detailed outcome of an interstitial ad.
    /// </summary>
    [Serializable]
    public struct InterstitialAdResult
    {
        public AdResult Result;
        public string FailureReason;
        public string PlacementId;

        public bool IsSuccess => Result == AdResult.Success;

        public static InterstitialAdResult CreateSuccess(string placement = null)
            => new() { Result = AdResult.Success, PlacementId = placement };

        public static InterstitialAdResult CreateFailure(AdResult result, string reason)
            => new() { Result = result, FailureReason = reason };
    }

    /// <summary>
    /// Information about a successfully rendered ad impression (used for custom analytics linking).
    /// </summary>
    [Serializable]
    public struct AdImpressionData
    {
        public string AdNetwork;
        public string AdUnit;      // e.g. "Rewarded", "Interstitial", "Banner"
        public string InstanceName;  // e.g. placements or specific networks
        public double Revenue;     // Impression level revenue
        public string Currency;    // e.g. "USD"
    }

    /// <summary>
    /// Type of IAP product supported by Unity IAP.
    /// </summary>
    public enum IapProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    /// <summary>
    /// Outcome of an in-app purchase request.
    /// </summary>
    public enum IapResult
    {
        Success,
        Failed,
        UserCancelled,
        StoreUnavailable,
        Pending,
        ProductUnavailable
    }

    /// <summary>
    /// Represents the detailed outcome of an IAP purchase process.
    /// </summary>
    [Serializable]
    public struct IapPurchaseResult
    {
        public IapResult Result;
        public string ProductId;
        public string TransactionId;
        public string FailureReason;

        public bool IsSuccess => Result == IapResult.Success;

        public static IapPurchaseResult CreateSuccess(string productId, string transactionId)
            => new() { Result = IapResult.Success, ProductId = productId, TransactionId = transactionId };

        public static IapPurchaseResult CreateFailure(IapResult result, string productId, string reason)
            => new() { Result = result, ProductId = productId, FailureReason = reason };
    }

    /// <summary>
    /// A main-thread-safe delay helper for Unity.
    /// Uses Task.Yield to prevent background thread pool scheduling and ensure 100% WebGL compatibility.
    /// </summary>
    public static class MonetizationTimer
    {
        public static async AsyncTimerTask DelayAsync(float seconds, System.Threading.CancellationToken ct = default)
        {
#if MONETIZATION_UNITASK
            await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(seconds), ignoreTimeScale: false, cancellationToken: ct);
#else
            float start = UnityEngine.Time.unscaledTime;
            while (UnityEngine.Time.unscaledTime - start < seconds)
            {
                if (ct.IsCancellationRequested) throw new System.OperationCanceledException();
                await System.Threading.Tasks.Task.Yield();
            }
#endif
        }
    }
}
