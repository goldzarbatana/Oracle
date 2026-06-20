using System;
using UnityEngine;
using UnityEngine.UIElements;
using TimeAura.Features.Localization;
using VContainer;

namespace TimeAura.Features.UI.Nexus
{
    public class SupportUIController : MonoBehaviour
    {
        [Inject] private LocalizationManager _localization;

        private VisualElement _supportModal;
        private Button _btnSupportAd;
        private Button _btnSupportDonate;
        private Button _btnSupportClose;

        public event Action OnWatchAdRequested;
        public event Action OnDonateRequested;

        public void Initialize(VisualElement root)
        {
            _supportModal = root.Q<VisualElement>("SupportModal");
            
            if (_supportModal == null)
            {
                Debug.LogWarning("[SupportUIController] SupportModal not found in UXML.");
                return;
            }

            _btnSupportAd = _supportModal.Q<Button>("BtnSupportAd");
            _btnSupportDonate = _supportModal.Q<Button>("BtnSupportDonate");
            _btnSupportClose = _supportModal.Q<Button>("BtnSupportClose");

            if (_btnSupportAd != null)
                _btnSupportAd.clicked += HandleAdClick;

            if (_btnSupportDonate != null)
                _btnSupportDonate.clicked += HandleDonateClick;

            if (_btnSupportClose != null)
                _btnSupportClose.clicked += Hide;

            // Optional: Click outside to close
            _supportModal.RegisterCallback<ClickEvent>(evt => 
            {
                if (evt.target == _supportModal) Hide();
            });

            UpdateLocalization();
        }

        public void Show()
        {
            if (_supportModal != null)
            {
                _supportModal.RemoveFromClassList("modal--hidden");
                Debug.Log("[Popup] Opened: SupportModal (Модальне вікно підтримки)");
            }
        }

        public void Hide()
        {
            if (_supportModal != null)
            {
                _supportModal.AddToClassList("modal--hidden");
            }
        }

        private void HandleAdClick()
        {
            Debug.Log("[SupportUIController] 📺 Watch Ad Requested");
            OnWatchAdRequested?.Invoke();
            // Typically we don't hide immediately until ad finishes, but for UI feedback:
            Hide();
        }

        private void HandleDonateClick()
        {
            Debug.Log("[SupportUIController] 💎 Donate ($1) Requested");
            OnDonateRequested?.Invoke();
            // Typically wait for IAP callback to hide
        }

        public void UpdateLocalization()
        {
            if (_localization == null || _supportModal == null) return;

            var header = _supportModal.Q<Label>("LblSupportHeader");
            if (header != null) header.text = _localization.Get("support.header", "SUPPORT TIME AURA").ToUpper();

            var text = _supportModal.Q<Label>("LblSupportText");
            if (text != null) text.text = _localization.Get("support.text", "Choose how to send energy to the creators.");

            if (_btnSupportAd != null) _btnSupportAd.text = _localization.Get("support.btn.ad", "VIEW A VISION (AD)").ToUpper();
            if (_btnSupportDonate != null) _btnSupportDonate.text = _localization.Get("support.btn.donate", "SHARE RESOURCES ($1)").ToUpper();
            if (_btnSupportClose != null) _btnSupportClose.text = _localization.Get("btn.close", "CLOSE").ToUpper();
        }
    }
}
