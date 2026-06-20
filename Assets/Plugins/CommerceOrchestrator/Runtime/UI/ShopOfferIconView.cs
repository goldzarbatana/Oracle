using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CommerceOrchestrator.UI
{
    public class ShopOfferIconView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI quantity;

        public void Setup(Sprite sprite, int amount)
        {
            try
            {
                if (icon != null)
                {
                    if (sprite != null)
                    {
                        icon.sprite = sprite;
                        icon.enabled = true;
                        var c = icon.color;
                        c.a = 1f;
                        icon.color = c;
                    }
                    else
                    {
                        icon.enabled = false;
                    }
                }

                if (quantity != null)
                {
                    if (amount > 0)
                    {
                        quantity.text = "x" + amount;
                        quantity.gameObject.SetActive(true);
                    }
                    else
                    {
                        quantity.gameObject.SetActive(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShopOfferIconView] Error in Setup: {ex.Message}");
            }
        }
    }
}
