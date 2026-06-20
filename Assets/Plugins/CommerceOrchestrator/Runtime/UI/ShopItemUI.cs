using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CommerceOrchestrator.Core;

namespace CommerceOrchestrator.UI
{
    public class ShopItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;

        private ProductConfig _config;
        private Action<string> _onBuyRequested;

        public void Setup(ProductConfig config, ProductLocalInfo localInfo, Action<string> onBuyRequested)
        {
            _config = config;
            _onBuyRequested = onBuyRequested;

            if (iconImage != null && localInfo != null)
            {
                iconImage.sprite = localInfo.itemIcon;
                iconImage.enabled = localInfo.itemIcon != null;
            }

            if (titleText != null && localInfo != null)
            {
                titleText.text = localInfo.itemNameKey;
            }

            if (descriptionText != null && localInfo != null)
            {
                descriptionText.text = localInfo.itemDescriptionKey;
            }

            UpdatePriceDisplay();

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyClicked);
            }
        }

        public void UpdatePriceDisplay()
        {
            if (_config == null || priceText == null) return;

            string localizedPrice = StoreManager.Instance.GetLocalizedPrice(_config.productId);
            if (!string.IsNullOrEmpty(localizedPrice))
            {
                priceText.text = localizedPrice;
            }
            else
            {
                priceText.text = $"${_config.priceUsd:0.00}";
            }
        }

        private void OnBuyClicked()
        {
            if (_config == null) return;
            _onBuyRequested?.Invoke(_config.productId);
        }
    }
}
