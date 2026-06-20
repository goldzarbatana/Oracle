#pragma warning disable 67

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

#if MONETIZATION_IAP
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

#if MONETIZATION_UNITASK
using Cysharp.Threading.Tasks;
using AsyncIapResult = Cysharp.Threading.Tasks.UniTask<UniversalMonetization.IapPurchaseResult>;
#else
using AsyncIapResult = System.Threading.Tasks.Task<UniversalMonetization.IapPurchaseResult>;
#endif

namespace UniversalMonetization
{
    public class IAPManager : MonoBehaviour, IIAPManagerService
#if MONETIZATION_IAP
        , IDetailedStoreListener
#endif
    {
        public static IAPManager Instance { get; private set; }

        [Header("Shop Configurations")]
        [SerializeField] private List<ShopOfferSO> registeredOffers = new();

        [Header("Simulated Testing")]
        [SerializeField] private bool enableMockModeInEditor = true;
        [SerializeField] private bool enableDebugLogs = true;

        // --- Core States ---
        public bool IsInitialized { get; private set; }
        public bool InitializationFailed { get; private set; }
        public string InitializationError { get; private set; }

        // --- C# Event Callbacks ---
        public static event Action OnStoreInitialized;
        public static event Action<string> OnStoreInitializationFailed;
        public static event Action<IapPurchaseResult> OnPurchaseCompleted;
        public static event Action<string, string> OnPurchaseError; // productId, error details

#if MONETIZATION_IAP
        private IStoreController _storeController;
        private IExtensionProvider _storeExtensionProvider;
#endif
        private TaskCompletionSource<IapPurchaseResult> _purchaseCompletionSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeIAP();
        }

        private void InitializeIAP()
        {
            Log("🚀 Initializing IAP Orchestrator...");

#if UNITY_EDITOR
            if (enableMockModeInEditor)
            {
                LogWarning("⚠️ Mock Simulation Mode Active. Purchases will be simulated locally.");
                IsInitialized = true;
                OnStoreInitialized?.Invoke();
                return;
            }
#endif

#if MONETIZATION_IAP
            Log("✅ MONETIZATION_IAP scripting define active. Initializing Unity Purchasing module...");
            try
            {
                var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

                foreach (var offer in registeredOffers)
                {
                    if (offer == null || string.IsNullOrEmpty(offer.iapProductId)) continue;

                    var type = offer.productType == IapProductType.Consumable ? ProductType.Consumable : 
                               offer.productType == IapProductType.NonConsumable ? ProductType.NonConsumable : ProductType.Subscription;

                    builder.AddProduct(offer.iapProductId, type);
                    Log($"Registered Product: {offer.iapProductId} ({type})");
                }

                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception ex)
            {
                MarkInitFailed("store_init_crashed", ex.Message);
            }
#else
            LogWarning("⚠️ MONETIZATION_IAP define is missing. Initializing in Fallback Mock Mode.");
            IsInitialized = true;
            OnStoreInitialized?.Invoke();
#endif
        }

        // ========================
        // PUBLIC BILLING FLOWS
        // ========================
        public void BuyProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;

            Log($"Process billing request initiated for: {productId}");

            // 1. Check Editor / Define Mock Simulation
            bool runMock = false;
#if UNITY_EDITOR
            runMock = enableMockModeInEditor;
#endif
#if !MONETIZATION_IAP
            runMock = true; // Fallback to mock if SDK is not imported
#endif

            if (runMock)
            {
                SimulatePurchase(productId);
                return;
            }

#pragma warning disable CS0162 // Unreachable code warning (guarded by preprocessor defines)
#if MONETIZATION_IAP
            if (_storeController != null)
            {
                var product = _storeController.products.WithID(productId);
                if (product != null && product.availableToPurchase)
                {
                    Log($"Initiating native Unity IAP purchase for product '{productId}'");
                    _storeController.InitiatePurchase(product);
                }
                else
                {
                    TriggerPurchaseFailed(productId, "Product not available in store configuration.");
                }
            }
            else
            {
                TriggerPurchaseFailed(productId, "IAP Store is not initialized.");
            }
#endif
#pragma warning restore CS0162
        }

        public async AsyncIapResult BuyProductAsync(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return IapPurchaseResult.CreateFailure(IapResult.ProductUnavailable, productId, "Product ID is empty.");

            if (_purchaseCompletionSource != null && !_purchaseCompletionSource.Task.IsCompleted)
            {
                return IapPurchaseResult.CreateFailure(IapResult.Failed, productId, "Another purchase is already in progress.");
            }

            _purchaseCompletionSource = new TaskCompletionSource<IapPurchaseResult>();

            BuyProduct(productId);

            try
            {
                return await _purchaseCompletionSource.Task;
            }
            finally
            {
                _purchaseCompletionSource = null;
            }
        }

        public void RestorePurchases()
        {
            Log("Processing transaction restore request...");
#if MONETIZATION_IAP
            if (_storeExtensionProvider != null)
            {
                var apple = _storeExtensionProvider.GetExtension<IAppleExtensions>();
                if (apple != null)
                {
                    apple.RestoreTransactions((result, error) =>
                    {
                        if (result)
                        {
                            Log("Purchase transactions restored successfully!");
                        }
                        else
                        {
                            LogError($"Purchase transactions restore failed: {error}");
                        }
                    });
                }
                else
                {
                    Log("Transaction restoration skipped: Device platform is not iOS.");
                }
            }
#else
            Log("Transaction restoration skipped: Platform is mock mode.");
#endif
        }

        /// <summary>
        /// Retrieves the localized price string (e.g. "$1.99", "£0.79") from the active store.
        /// Falls back to the developer-configured inspectable price if the store is not initialized.
        /// </summary>
        public string GetLocalizedPrice(string productId)
        {
#if MONETIZATION_IAP
            if (_storeController != null)
            {
                var p = _storeController.products.WithID(productId);
                if (p != null && p.metadata != null)
                {
                    return p.metadata.localizedPriceString;
                }
            }
#endif
            var offer = registeredOffers.Find(o => o.iapProductId == productId);
            return offer != null ? offer.fallbackPriceDisplay : "$0.00";
        }

        // ========================
        // MOCK SIMULATOR ENGINE
        // ========================
        private void SimulatePurchase(string productId)
        {
            // Simulate billing delay
            _ = SimulatePurchaseAsync(productId);
        }

        private async AsyncIapResult SimulatePurchaseAsync(string productId)
        {
            await MonetizationTimer.DelayAsync(1.5f);
            
            // Randomly succeed for mock testing or always succeed
            bool mockSuccess = true; 
            
            if (mockSuccess)
            {
                string txnId = $"MOCK_TXN_{UnityEngine.Random.Range(100000, 999999)}";
                Log($"🏆 Simulated purchase success for product: '{productId}' (Txn: {txnId})");
                
                var result = IapPurchaseResult.CreateSuccess(productId, txnId);
                _purchaseCompletionSource?.TrySetResult(result);
                OnPurchaseCompleted?.Invoke(result);
                return result;
            }
            else
            {
                TriggerPurchaseFailed(productId, "Simulated user cancel / network error");
                return IapPurchaseResult.CreateFailure(IapResult.Failed, productId, "Simulated user cancel / network error");
            }
        }

        private void TriggerPurchaseFailed(string productId, string reason)
        {
            LogError($"Purchase failed for '{productId}': {reason}");
            var failResult = IapPurchaseResult.CreateFailure(IapResult.Failed, productId, reason);
            _purchaseCompletionSource?.TrySetResult(failResult);
            OnPurchaseError?.Invoke(productId, reason);
        }

        // ========================
        // UNITY IAP CALLBAKS
        // ========================
#if MONETIZATION_IAP
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _storeExtensionProvider = extensions;
            IsInitialized = true;
            InitializationFailed = false;

            Log("✅ Store Controller successfully initialized! Loaded active products catalog.");
            OnStoreInitialized?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            string details = string.IsNullOrEmpty(message) ? error.ToString() : $"{error}: {message}";
            MarkInitFailed(error.ToString(), details);
        }

        private void MarkInitFailed(string errorKey, string details)
        {
            InitializationFailed = true;
            InitializationError = errorKey;
            IsInitialized = false;
            LogError($"❌ Store Initialization failed: {details}");
            OnStoreInitializationFailed?.Invoke(errorKey);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;
            var productId = product.definition.id;
            var transactionId = product.transactionID;

            Log($"💰 Purchase processing completed: '{productId}' (Txn: {transactionId})");

            var result = IapPurchaseResult.CreateSuccess(productId, transactionId);
            _purchaseCompletionSource?.TrySetResult(result);
            OnPurchaseCompleted?.Invoke(result);

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            TriggerPurchaseFailed(product.definition.id, failureReason.ToString());
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            var reason = failureDescription?.reason ?? PurchaseFailureReason.Unknown;
            var msg = failureDescription?.message ?? "Unknown billing error";
            TriggerPurchaseFailed(product.definition.id, $"{reason}: {msg}");
        }
#endif

        // ========================
        // LOGGER UTILITY
        // ========================
        private void Log(string msg)
        {
            if (enableDebugLogs) Debug.Log($"<color=#ffd238>[IAPManager]</color> {msg}");
        }

        private void LogWarning(string msg)
        {
            if (enableDebugLogs) Debug.LogWarning($"<color=yellow>[IAPManager] ⚠️</color> {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[IAPManager] ❌ {msg}");
        }
    }
}
