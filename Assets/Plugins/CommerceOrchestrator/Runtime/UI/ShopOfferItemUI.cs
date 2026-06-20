using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CommerceOrchestrator.Core;

namespace CommerceOrchestrator.UI
{
    public class ShopOfferItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button buyButton;

        [Header("Rewards Container")]
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private GameObject rewardIconPrefab;

        private ProductConfig _config;
        private Action<string> _onBuyRequested;
        private ShopItemExpander _expander;

        private void Awake()
        {
            _expander = GetComponent<ShopItemExpander>();
        }

        private void Start()
        {
            var btn = GetComponent<Button>();
            if (btn != null && _expander != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(_expander.Toggle);
            }
        }

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
            PopulateRewards();

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

        private void PopulateRewards()
        {
            if (rewardsContainer == null || rewardIconPrefab == null || _config == null || _config.parsedRewards == null) return;

            for (int i = rewardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(rewardsContainer.GetChild(i).gameObject);
            }

            foreach (var reward in _config.parsedRewards)
            {
                if (reward.amount <= 0) continue;

                var go = Instantiate(rewardIconPrefab, rewardsContainer);
                var view = go.GetComponent<ShopOfferIconView>();
                if (view != null)
                {
                    Sprite sprite = Resources.Load<Sprite>($"Rewards/{reward.resourceId}_Icon");
                    if (sprite == null) sprite = Resources.Load<Sprite>($"PowerUp_Icons/{reward.resourceId}_Icon");
                    if (sprite == null) sprite = Resources.Load<Sprite>($"{reward.resourceId}");
                    
                    view.Setup(sprite, reward.amount);
                }
            }
        }

        private void OnBuyClicked()
        {
            if (_config == null) return;
            _onBuyRequested?.Invoke(_config.productId);
            if (_expander != null && _expander.IsExpanded)
            {
                _expander.Collapse();
            }
        }
    }
}
