using System;

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncIapResult = Cysharp.Threading.Tasks.UniTask<UniversalMonetization.IapPurchaseResult>;
#else
using System.Threading.Tasks;
using AsyncIapResult = System.Threading.Tasks.Task<UniversalMonetization.IapPurchaseResult>;
#endif

namespace UniversalMonetization
{
    /// <summary>
    /// Dependency Injection interface for IAPManager.
    /// Can be registered in VContainer/Zenject: builder.RegisterComponent(IAPManager.Instance).As<IIAPManagerService>();
    /// </summary>
    public interface IIAPManagerService
    {
        bool IsInitialized { get; }
        
        void BuyProduct(string productId);
        AsyncIapResult BuyProductAsync(string productId);
        void RestorePurchases();
        string GetLocalizedPrice(string productId);
    }
}
