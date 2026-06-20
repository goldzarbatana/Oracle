using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CommerceOrchestrator.Core;

namespace CommerceOrchestrator.UI
{
    public class ShopController : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private Transform powerUpContainer;
        [SerializeField] private Transform offerContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject shopItemUIPrefab;
        [SerializeField] private GameObject shopOfferItemUIPrefab;

        [Header("Database Settings")]
        [SerializeField] private StoreDatabase localDatabase;

        [Header("UI Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button restoreButton;

        private readonly List<GameObject> _instantiatedUI = new List<GameObject>();

        private void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
            if (restoreButton != null) restoreButton.onClick.AddListener(OnRestoreClicked);

            StoreManager.OnStoreInitialized += PopulateShop;
            if (StoreManager.Instance != null && StoreManager.Instance.PricingProvider != null)
            {
                StoreManager.Instance.PricingProvider.OnPricingUpdated += PopulateShop;
            }

            PopulateShop();
        }

        private void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
            if (restoreButton != null) restoreButton.onClick.RemoveListener(OnRestoreClicked);

            StoreManager.OnStoreInitialized -= PopulateShop;
            if (StoreManager.Instance != null && StoreManager.Instance.PricingProvider != null)
            {
                StoreManager.Instance.PricingProvider.OnPricingUpdated -= PopulateShop;
            }
        }

        public void PopulateShop()
        {
            ClearShopUI();

            if (StoreManager.Instance == null || StoreManager.Instance.PricingProvider == null || localDatabase == null)
            {
                Debug.LogWarning("[CommerceOrchestrator] StoreManager or PricingProvider is not initialized yet.");
                return;
            }

            var configs = StoreManager.Instance.PricingProvider.GetAllProductConfigs();

            foreach (var kvp in configs)
            {
                string productId = kvp.Key;
                ProductConfig config = kvp.Value;
                ProductLocalInfo localInfo = localDatabase.GetProduct(productId);

                if (localInfo == null)
                {
                    Debug.LogWarning($"[CommerceOrchestrator] Product ID '{productId}' not found in local database. Skipping visual generation.");
                    continue;
                }

                bool isBundle = config.parsedRewards != null && config.parsedRewards.Count > 1;

                if (isBundle && offerContainer != null && shopOfferItemUIPrefab != null)
                {
                    var go = Instantiate(shopOfferItemUIPrefab, offerContainer);
                    _instantiatedUI.Add(go);

                    var ui = go.GetComponent<ShopOfferItemUI>();
                    if (ui != null)
                    {
                        ui.Setup(config, localInfo, BuyRequested);
                    }
                }
                else if (powerUpContainer != null && shopItemUIPrefab != null)
                {
                    var go = Instantiate(shopItemUIPrefab, powerUpContainer);
                    _instantiatedUI.Add(go);

                    var ui = go.GetComponent<ShopItemUI>();
                    if (ui != null)
                    {
                        ui.Setup(config, localInfo, BuyRequested);
                    }
                }
            }
        }

        private void ClearShopUI()
        {
            foreach (var go in _instantiatedUI)
            {
                if (go != null) Destroy(go);
            }
            _instantiatedUI.Clear();
        }

        private void BuyRequested(string productId)
        {
            if (StoreManager.Instance != null)
            {
                StoreManager.Instance.BuyProduct(productId);
            }
        }

        private void OnBackClicked()
        {
            gameObject.SetActive(false);
        }

        private void OnRestoreClicked()
        {
            if (StoreManager.Instance != null)
            {
                StoreManager.Instance.RestorePurchases();
            }
        }
    }
}
