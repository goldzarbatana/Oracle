using System;
using UnityEngine;
using UnityEngine.Events;

namespace UniversalMonetization
{
    [Serializable]
    public class IntUnityEvent : UnityEvent<int> { }

    [Serializable]
    public class StringUnityEvent : UnityEvent<string> { }

    [Serializable]
    public class PurchaseResultUnityEvent : UnityEvent<IapPurchaseResult> { }

    [Serializable]
    public class PurchaseErrorUnityEvent : UnityEvent<string, string> { }

    /// <summary>
    /// A helper component that exposes Monetization Orchestrator events as UnityEvents.
    /// This allows No-Code integration via the Unity Inspector.
    /// </summary>
    [AddComponentMenu("Monetization Orchestrator/Monetization Unity Events")]
    public class MonetizationUnityEvents : MonoBehaviour
    {
        [Header("Ad Events")]
        [Tooltip("Fired when a rewarded ad is fully watched. Returns the reward amount configured in the dashboard/config.")]
        public IntUnityEvent onRewardedAdCompleted;

        [Tooltip("Fired when a rewarded ad fails to load or show. Returns the error message.")]
        public StringUnityEvent onRewardedAdFailed;

        [Tooltip("Fired when an interstitial ad is successfully shown to the user.")]
        public UnityEvent onInterstitialAdShown;

        [Header("IAP Events")]
        [Tooltip("Fired when an in-app purchase is successfully completed.")]
        public PurchaseResultUnityEvent onPurchaseCompleted;

        [Tooltip("Fired when an in-app purchase fails. Returns Product ID and Error Message.")]
        public PurchaseErrorUnityEvent onPurchaseError;

        private void OnEnable()
        {
            AdManager.OnRewardedAdCompleted += HandleRewardedAdCompleted;
            AdManager.OnRewardedAdFailed += HandleRewardedAdFailed;
            AdManager.OnInterstitialAdShown += HandleInterstitialAdShown;
            
            IAPManager.OnPurchaseCompleted += HandlePurchaseCompleted;
            IAPManager.OnPurchaseError += HandlePurchaseError;
        }

        private void OnDisable()
        {
            AdManager.OnRewardedAdCompleted -= HandleRewardedAdCompleted;
            AdManager.OnRewardedAdFailed -= HandleRewardedAdFailed;
            AdManager.OnInterstitialAdShown -= HandleInterstitialAdShown;
            
            IAPManager.OnPurchaseCompleted -= HandlePurchaseCompleted;
            IAPManager.OnPurchaseError -= HandlePurchaseError;
        }

        private void HandleRewardedAdCompleted(int rewardAmount)
        {
            onRewardedAdCompleted?.Invoke(rewardAmount);
        }

        private void HandleRewardedAdFailed(string error)
        {
            onRewardedAdFailed?.Invoke(error);
        }

        private void HandleInterstitialAdShown()
        {
            onInterstitialAdShown?.Invoke();
        }

        private void HandlePurchaseCompleted(IapPurchaseResult result)
        {
            onPurchaseCompleted?.Invoke(result);
        }
        
        private void HandlePurchaseError(string productId, string error)
        {
            onPurchaseError?.Invoke(productId, error);
        }
    }
}
