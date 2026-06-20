using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace CommerceOrchestrator.Core
{
    public class StoreManager : MonoBehaviour
#if UNITY_PURCHASING
        , IDetailedStoreListener
#endif
    {
        public static StoreManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableTestMode = false;
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private StoreDatabase fallbackDatabase;

        private IStorePricingProvider _pricingProvider;
        private bool _isInitialized = false;

        // Public events
        public static event Action OnStoreInitialized;
        public static event Action<string> OnStoreInitializationFailed;
        public static event Action<string, List<RewardEntry>> OnPurchaseSuccess;
        public static event Action<string, string> PurchaseFailedEvent;

        public bool IsInitialized => _isInitialized;
        public bool IsTestMode => enableTestMode;
        public IStorePricingProvider PricingProvider => _pricingProvider;

#if UNITY_PURCHASING
        private IStoreController _storeController;
        private IExtensionProvider _storeExtensionProvider;
#endif

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetPricingProvider(IStorePricingProvider provider)
        {
            _pricingProvider = provider;
            if (enableDebugLogs) Debug.Log("[CommerceOrchestrator] Pricing provider registered.");
        }

        public async UniTask InitializeStoreAsync()
        {
            if (_pricingProvider == null)
            {
                // Fallback to local pricing if none provided
                if (fallbackDatabase != null)
                {
                    SetPricingProvider(new LocalPricingProvider(fallbackDatabase));
                }
                else
                {
                    Debug.LogError("[CommerceOrchestrator] Cannot initialize store: No pricing provider or fallback database set.");
                    OnStoreInitializationFailed?.Invoke("Missing pricing configurations.");
                    return;
                }
            }

            // Fetch product configuration
            await _pricingProvider.FetchProductsAsync(forceReload: true);

            if (enableTestMode)
            {
                Debug.LogWarning("[CommerceOrchestrator] Test Mode Active. Purchases will be simulated locally.");
                _isInitialized = true;
                OnStoreInitialized?.Invoke();
                return;
            }

#if UNITY_PURCHASING
            Debug.Log("[CommerceOrchestrator] Initializing Unity IAP...");
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            var configs = _pricingProvider.GetAllProductConfigs();
            foreach (var config in configs.Values)
            {
                var type = ProductType.Consumable;
                if (config.isSubscription)
                {
                    type = ProductType.Subscription;
                }
                else if (fallbackDatabase != null)
                {
                    var info = fallbackDatabase.GetProduct(config.productId);
                    if (info != null && info.isOneTimePurchase)
                    {
                        type = ProductType.NonConsumable;
                    }
                }

                builder.AddProduct(config.productId, type);
                if (enableDebugLogs) Debug.Log($"[CommerceOrchestrator] Registered product: {config.productId} ({type})");
            }

            UnityPurchasing.Initialize(this, builder);
#else
            Debug.LogWarning("[CommerceOrchestrator] UNITY_PURCHASING is not defined. IAP functionality disabled. Please import Unity IAP package.");
            OnStoreInitializationFailed?.Invoke("UNITY_PURCHASING not defined");
#endif
        }

        public void BuyProduct(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;

            if (enableTestMode)
            {
                SimulateTestPurchase(productId);
                return;
            }

#if UNITY_PURCHASING
            if (_storeController != null)
            {
                var product = _storeController.products.WithID(productId);
                if (product != null && product.availableToPurchase)
                {
                    _storeController.InitiatePurchase(product);
                }
                else
                {
                    Debug.LogError($"[CommerceOrchestrator] Product {productId} is unavailable or not found in store.");
                    PurchaseFailedEvent?.Invoke(productId, "Product unavailable");
                }
            }
            else
            {
                Debug.LogError("[CommerceOrchestrator] Store is not initialized.");
                PurchaseFailedEvent?.Invoke(productId, "Store not initialized");
            }
#else
            Debug.LogError("[CommerceOrchestrator] Purchase request failed: UNITY_PURCHASING is disabled.");
            PurchaseFailedEvent?.Invoke(productId, "IAP disabled");
#endif
        }

        public string GetLocalizedPrice(string productId)
        {
#if UNITY_PURCHASING
            if (_storeController != null)
            {
                var p = _storeController.products.WithID(productId);
                if (p != null && p.metadata != null)
                {
                    return p.metadata.localizedPriceString;
                }
            }
#endif
            // Fallback to provider config
            if (_pricingProvider != null)
            {
                var config = _pricingProvider.GetProductConfig(productId);
                if (config != null)
                {
                    return $"${config.priceUsd:0.00}";
                }
            }
            return string.Empty;
        }

        public void RestorePurchases()
        {
#if UNITY_PURCHASING
            if (_storeExtensionProvider != null)
            {
                var apple = _storeExtensionProvider.GetExtension<IAppleExtensions>();
                if (apple != null)
                {
                    apple.RestoreTransactions((result, error) =>
                    {
                        if (enableDebugLogs) Debug.Log($"[CommerceOrchestrator] Restore transactions finished: result={result}, error={error}");
                    });
                }
            }
#endif
        }

        private void SimulateTestPurchase(string productId)
        {
            Debug.LogWarning($"[CommerceOrchestrator] Simulating successful purchase of '{productId}'");
            
            var config = _pricingProvider?.GetProductConfig(productId);
            var rewards = config != null ? config.parsedRewards : new List<RewardEntry>();

            OnPurchaseSuccess?.Invoke(productId, rewards);
        }

#if UNITY_PURCHASING
        // --- IStoreListener & IDetailedStoreListener Implementation ---

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _storeExtensionProvider = extensions;
            _isInitialized = true;

            if (enableDebugLogs)
            {
                Debug.Log("[CommerceOrchestrator] Store successfully initialized.");
                foreach (var product in controller.products.all)
                {
                    Debug.Log($"[CommerceOrchestrator] Product: {product.definition.id} | Price: {product.metadata.localizedPriceString}");
                }
            }
            OnStoreInitialized?.Invoke();
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, null);
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"[CommerceOrchestrator] Store initialization failed: {error}. Message: {message}");
            _isInitialized = false;
            OnStoreInitializationFailed?.Invoke($"{error}: {message}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            var product = args.purchasedProduct;
            var id = product.definition.id;

            if (enableDebugLogs) Debug.Log($"[CommerceOrchestrator] Transaction completed successfully for: {id}");

            // Retrieve rewards configuration from provider
            var config = _pricingProvider?.GetProductConfig(id);
            var rewards = config != null ? config.parsedRewards : new List<RewardEntry>();

            OnPurchaseSuccess?.Invoke(id, rewards);

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogWarning($"[CommerceOrchestrator] Purchase failed: {product.definition.id}. Reason: {failureReason}");
            PurchaseFailedEvent?.Invoke(product.definition.id, failureReason.ToString());
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            var reason = failureDescription?.reason ?? PurchaseFailureReason.Unknown;
            var message = failureDescription?.message ?? string.Empty;

            Debug.LogWarning($"[CommerceOrchestrator] Purchase failed: {product.definition.id}. Reason: {reason}. Details: {message}");
            PurchaseFailedEvent?.Invoke(product.definition.id, $"{reason}: {message}");
        }
#endif
    }
}
