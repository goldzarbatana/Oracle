using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Localization;
using CommerceOrchestrator.Core;

namespace TimeAura.Features.UI.Nexus
{
    public class MysticalShopController : MonoBehaviour
    {
        [Inject] private LocalizationManager _localization;
        [Inject] private AudioService _audioService;

        private VisualElement _shopOverlay;
        private VisualElement _itemsContainer;
        private IVisualElementScheduledItem _pulseTask;

        public void Initialize(VisualElement root)
        {
            Debug.Log("[MysticalShop] 🔮 Initializing Mystical Shop UI Toolkit Overlay.");

            // 1. Locate or dynamically clone the ShopPanel overlay into the root UI layout
            _shopOverlay = root.Q<VisualElement>("ShopPanel");
            if (_shopOverlay == null)
            {
                var shopPanelUxml = Resources.Load<VisualTreeAsset>("UI/Nexus/ShopPanel");
                if (shopPanelUxml != null)
                {
                    var spawned = shopPanelUxml.CloneTree().Q("ShopPanel");
                    if (spawned != null)
                    {
                        root.Add(spawned);
                        _shopOverlay = spawned;
                    }
                }
            }

            if (_shopOverlay == null)
            {
                Debug.LogError("[MysticalShop] ❌ Failed to instantiate ShopPanel overlay!");
                return;
            }

            // Explicitly attach the stylesheet to the panel (since template container is discarded on Q query)
            var styleSheet = Resources.Load<StyleSheet>("UI/Nexus/ShopStyles");
            if (styleSheet != null)
            {
                if (!_shopOverlay.styleSheets.Contains(styleSheet))
                {
                    _shopOverlay.styleSheets.Add(styleSheet);
                    Debug.Log("[MysticalShop] 🎨 Attached ShopStyles.uss programmatically to ShopPanel.");
                }
            }
            else
            {
                Debug.LogError("[MysticalShop] ❌ ShopStyles.uss NOT found in Resources/UI/Nexus/ShopStyles!");
            }

            // 2. Locate inner container where product cards will be spawned
            _itemsContainer = _shopOverlay.Q<VisualElement>("ShopItemsContainer");

            // 3. Bind open button from VaultPanel
            var btnOpenShop = root.Q<Button>("BtnOpenShop");
            if (btnOpenShop != null)
            {
                btnOpenShop.clicked += () => OpenShop(true);
            }

            // 4. Bind close button inside ShopPanel
            var btnCloseShop = _shopOverlay.Q<Button>("BtnCloseShop");
            if (btnCloseShop != null)
            {
                btnCloseShop.clicked += () => OpenShop(false);
            }

            // 5. Populate products if StoreManager is already initialized
            if (StoreManager.Instance != null && StoreManager.Instance.IsInitialized)
            {
                OnStoreInitialized();
            }
        }

        private void Awake()
        {
            StoreManager.OnStoreInitialized += OnStoreInitialized;
            StoreManager.OnPurchaseSuccess += OnPurchaseSuccess;
        }

        private void OnDestroy()
        {
            _pulseTask?.Pause();
            StoreManager.OnStoreInitialized -= OnStoreInitialized;
            StoreManager.OnPurchaseSuccess -= OnPurchaseSuccess;
            if (StoreManager.Instance != null && StoreManager.Instance.PricingProvider != null)
            {
                StoreManager.Instance.PricingProvider.OnPricingUpdated -= PopulateProducts;
            }
        }

        private void OnStoreInitialized()
        {
            if (StoreManager.Instance != null && StoreManager.Instance.PricingProvider != null)
            {
                StoreManager.Instance.PricingProvider.OnPricingUpdated -= PopulateProducts;
                StoreManager.Instance.PricingProvider.OnPricingUpdated += PopulateProducts;
            }
            PopulateProducts();
        }

        private void OnPurchaseSuccess(string productId, List<RewardEntry> rewards)
        {
            // Delay slightly so that MonetizationEconomyAdapter processes, saves and syncs balance first
            UniTask.Delay(300).ContinueWith(() =>
            {
                var vault = FindAnyObjectByType<VaultController>();
                if (vault != null)
                {
                    vault.RefreshUI();
                }
            }).Forget();
        }

        public void OpenShop(bool open)
        {
            if (_shopOverlay == null) return;

            _audioService?.PlaySFX("CrystalClick");

            if (open)
            {
                _shopOverlay.RemoveFromClassList("shop-overlay--hidden");
                _shopOverlay.style.display = DisplayStyle.Flex;
                _shopOverlay.style.opacity = 1f;
                _shopOverlay.BringToFront();
                PopulateProducts();
            }
            else
            {
                _pulseTask?.Pause();
                _shopOverlay.AddToClassList("shop-overlay--hidden");
                _shopOverlay.style.display = DisplayStyle.None;
                _shopOverlay.style.opacity = 0f;
            }
        }

        private void PopulateProducts()
        {
            if (_itemsContainer == null) return;
            _itemsContainer.Clear();

            var storeManager = StoreManager.Instance;
            if (storeManager == null || storeManager.PricingProvider == null)
            {
                Debug.LogWarning("[MysticalShop] StoreManager or PricingProvider not ready.");
                return;
            }

            var configs = storeManager.PricingProvider.GetAllProductConfigs();
            var storeDb = Resources.Load<StoreDatabase>("Settings/StoreDatabase");
            var shopItemCardUxml = Resources.Load<VisualTreeAsset>("UI/Nexus/ShopItemCard");

            if (shopItemCardUxml == null)
            {
                Debug.LogError("[MysticalShop] ShopItemCard UXML template not found in Resources.");
                return;
            }

            foreach (var kvp in configs)
            {
                var config = kvp.Value;
                var card = shopItemCardUxml.CloneTree().Q("ShopItemCard");
                if (card == null) continue;

                // Query UI Elements
                var lblName = card.Q<Label>("ShopItemName");
                var lblPrice = card.Q<Label>("ShopItemPrice");
                var btnBuy = card.Q<Button>("BtnBuyItem");
                var iconEl = card.Q<VisualElement>("ShopItemIcon");

                // Get metadata from local fallback database
                var localInfo = storeDb != null ? storeDb.GetProduct(config.productId) : null;
                
                string displayName = config.productId;
                if (localInfo != null && !string.IsNullOrEmpty(localInfo.itemNameKey))
                {
                    displayName = _localization != null ? _localization.Get(localInfo.itemNameKey, localInfo.productId) : localInfo.productId;
                }
                
                if (lblName != null) lblName.text = displayName;

                // Get price from IAP metadata (or fallback)
                string localizedPrice = storeManager.GetLocalizedPrice(config.productId);
                if (string.IsNullOrEmpty(localizedPrice))
                {
                    localizedPrice = $"${config.priceUsd:F2}";
                }
                if (lblPrice != null) lblPrice.text = localizedPrice;

                // Bind custom product icon
                if (iconEl != null && localInfo != null && localInfo.itemIcon != null)
                {
                    iconEl.style.backgroundImage = new StyleBackground(localInfo.itemIcon);
                }

                // Style subscriptions with custom styling class
                if (config.isSubscription || (localInfo != null && localInfo.isSubscription))
                {
                    card.AddToClassList("shop-item--enlightened");
                }

                // Buy button trigger
                if (btnBuy != null)
                {
                    btnBuy.clicked += () =>
                    {
                        _audioService?.PlaySFX("CrystalClick");
                        storeManager.BuyProduct(config.productId);
                    };
                }

                _itemsContainer.Add(card);
            }

            // Start pulsing animation on the Enlightened subscription card (if present)
            var subCard = _itemsContainer.Q<VisualElement>(className: "shop-item--enlightened");
            if (subCard != null)
            {
                _pulseTask?.Pause();
                _pulseTask = subCard.schedule.Execute(() =>
                {
                    float scaleVal = 1.0f + Mathf.PingPong(Time.time * 1.5f, 0.02f);
                    subCard.style.scale = new StyleScale(new Scale(new Vector3(scaleVal, scaleVal, 1f)));

                    float alpha = 0.4f + Mathf.PingPong(Time.time * 1.5f, 0.6f);
                    var glowColor = new Color(0.83f, 0.69f, 0.22f, alpha);
                    subCard.style.borderTopColor = new StyleColor(glowColor);
                    subCard.style.borderBottomColor = new StyleColor(glowColor);
                    subCard.style.borderLeftColor = new StyleColor(glowColor);
                    subCard.style.borderRightColor = new StyleColor(glowColor);
                }).Every(33);
            }
        }
    }
}
