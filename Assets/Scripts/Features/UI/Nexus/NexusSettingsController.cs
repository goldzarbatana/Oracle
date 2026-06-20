using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Core.Services;
using TimeAura.Features.Localization;
using TimeAura.Core.Localization;
using TimeAura.Core.Data.SO;
using System;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Economy;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// Manages the Settings panel UI and audio sliders.
    /// Extracted from the NexusController monolith.
    /// </summary>
    public class NexusSettingsController : MonoBehaviour
    {
        public void SetActive(bool active) => enabled = active;

        [Inject] private LocalizationManager _localization;
        [Inject] private AudioService _audioService;
        [Inject] private UIManager _uiManager;
        [Inject] private RemoteConfigService _remoteConfig;
        [Inject] private IStripeService _stripeService;

        private VisualElement _settingsPanel;
        public event Action OnClose;

        public void Initialize(VisualElement root)
        {
            _settingsPanel = root.Q("SettingsPanel");
            if (_settingsPanel == null) return;

            var sMaster  = _settingsPanel.Q<Slider>("SliderMasterVol");
            var sMusic   = _settingsPanel.Q<Slider>("SliderMusicVol");
            var sSFX     = _settingsPanel.Q<Slider>("SliderSFXVol");
            var sVoice   = _settingsPanel.Q<Slider>("SliderAmbienceVol");
            var sScale   = _settingsPanel.Q<Slider>("SliderUIScale");
            var tMute    = _settingsPanel.Q<Toggle>("ToggleMute");
            var btnClose = _settingsPanel.Q<Button>("BtnCloseSettings");

            if (_audioService != null)
            {
                // Master Channel
                if (sMaster != null) { 
                    sMaster.value = _audioService.GetVolume(AudioChannel.Master); 
                    sMaster.RegisterValueChangedCallback(v => _audioService.SetVolume(AudioChannel.Master, v.newValue)); 
                }
                
                // Music Channel
                if (sMusic != null) { 
                    sMusic.value = _audioService.GetVolume(AudioChannel.Music);  
                    sMusic.RegisterValueChangedCallback(v => _audioService.SetVolume(AudioChannel.Music, v.newValue)); 
                }
                
                // SFX Channel
                if (sSFX != null) { 
                    sSFX.value = _audioService.GetVolume(AudioChannel.SFX);    
                    sSFX.RegisterValueChangedCallback(v => _audioService.SetVolume(AudioChannel.SFX, v.newValue)); 
                }
                
                // Oracle Voice (using Ambience Channel)
                if (sVoice != null) { 
                    sVoice.value = _audioService.GetVolume(AudioChannel.Ambience); 
                    sVoice.RegisterValueChangedCallback(v => _audioService.SetVolume(AudioChannel.Ambience, v.newValue)); 
                }
                
                // Global Silence (Mute)
                if (tMute != null) { 
                    tMute.value = PlayerPrefs.GetInt("Audio_Muted", 0) == 1;     
                    tMute.RegisterValueChangedCallback(v => {
                        _audioService.ToggleMute(v.newValue);
                        _audioService.PlaySFX("ButtonClick", 0.5f); // Mystical feedback
                    }); 
                }
            }

            if (sScale != null && _uiManager != null)
            {
                sScale.value = _uiManager.GlobalScale;
                sScale.RegisterValueChangedCallback(v => _uiManager.SetGlobalScale(v.newValue));
            }

            var btnCloseTop = _settingsPanel.Q<Button>("BtnCloseSettingsTop");
            if (btnCloseTop != null)
            {
                btnCloseTop.clicked += () => {
                    _audioService?.PlaySFX("ButtonClick", 0.4f);
                    OnClose?.Invoke();
                };
            }

            if (btnClose != null)
            {
                btnClose.clicked += () => {
                    _audioService?.PlaySFX("ButtonClick", 0.4f);
                    OnClose?.Invoke();
                };
            }

            var lblVersion = _settingsPanel.Q<Label>("LblAppVersion");
            if (lblVersion != null)
            {
                lblVersion.text = $"v{Application.version} (Remote Config)";
                int clickCount = 0;
                float lastClickTime = 0f;
                lblVersion.RegisterCallback<ClickEvent>(evt =>
                {
                    float currentTime = Time.time;
                    if (currentTime - lastClickTime > 2.0f)
                    {
                        clickCount = 0;
                    }
                    clickCount++;
                    lastClickTime = currentTime;
                    if (clickCount >= 5)
                    {
                        clickCount = 0;
                        OpenAdminPanel(root);
                    }
                });
            }
            
            UpdateLocalization();
        }

        public void UpdateLocalization()
        {
            if (_settingsPanel == null || _localization == null) return;

            var tone = _localization.CurrentTone;
            var header = _settingsPanel.Q<Label>(null, "essence-header");
            if (header != null) header.text = _localization.GetPersonaString(AuraTerms.SETTINGS_HEADER, tone, "NEXUS SETTINGS");

            // Slider Labels
            UpdateSliderLabel("SliderMasterVol", AuraTerms.LBL_MASTER_VOL, "MASTER AUDIO");
            UpdateSliderLabel("SliderMusicVol", AuraTerms.LBL_MUSIC_VOL, "BACKGROUND MUSIC");
            UpdateSliderLabel("SliderSFXVol", AuraTerms.LBL_SFX_VOL, "SOUND EFFECTS");
            UpdateSliderLabel("SliderAmbienceVol", AuraTerms.LBL_VOICE_VOL, "ORACLE VOICE");
            UpdateSliderLabel("SliderUIScale", AuraTerms.LBL_UI_SCALE, "INTERFACE SCALE");

            var tMute = _settingsPanel.Q<Toggle>("ToggleMute");
            if (tMute != null) tMute.label = _localization.GetPersonaString(AuraTerms.LBL_MUTE, tone, "SILENCE MODE (MUTE)");

            var btnClose = _settingsPanel.Q<Button>("BtnCloseSettings");
            if (btnClose != null) btnClose.text = _localization.GetPersonaString(AuraTerms.SUCCESS, tone, "CLOSE").ToUpper();
        }


        private void OpenAdminPanel(VisualElement root)
        {
            var adminPanel = root.Q("DeveloperAdminPanel");
            if (adminPanel == null) return;

            adminPanel.style.display = DisplayStyle.Flex;

            var btnReload = adminPanel.Q<Button>("BtnAdminReload");
            var btnClose = adminPanel.Q<Button>("BtnAdminClose");
            var configList = adminPanel.Q<ScrollView>("AdminConfigList");

            // Додаємо кнопку для тестування Stripe Checkout
            var stripeTestButton = adminPanel.Q<Button>("BtnTestStripe");
            if (stripeTestButton == null)
            {
                // Створюємо нову кнопку, якщо ще не існує
                stripeTestButton = new Button();
                stripeTestButton.name = "BtnTestStripe";
                stripeTestButton.text = "TEST STRIPE CHECKOUT ($30)";
                stripeTestButton.style.marginTop = 10;
                stripeTestButton.style.backgroundColor = new Color(0.83f, 0.2f, 0.2f); // Червоний для позначення платіжної функції
                
                // Додаємо кнопку до панелі (перед іншими елементами)
                adminPanel.Insert(0, stripeTestButton);
            }

            if (btnReload != null)
            {
                btnReload.clicked -= HandleAdminReload;
                btnReload.clicked += HandleAdminReload;
            }

            if (btnClose != null)
            {
                btnClose.clicked -= HandleAdminClose;
                btnClose.clicked += HandleAdminClose;
            }

            if (stripeTestButton != null)
            {
                stripeTestButton.clicked -= HandleStripeTest;
                stripeTestButton.clicked += HandleStripeTest;
            }

            void HandleAdminReload()
            {
                ReloadAdminConfigAsync(configList).Forget();
            }

            void HandleAdminClose()
            {
                adminPanel.style.display = DisplayStyle.None;
            }

            void HandleStripeTest()
            {
                TestStripeCheckoutAsync().Forget();
            }

            PopulateAdminConfigList(configList);
        }

        private async UniTask TestStripeCheckoutAsync()
        {
            if (_stripeService == null)
            {
                Debug.LogError("[StripeTest] Stripe service is not available");
                return;
            }

            Debug.Log("[StripeTest] Initiating Stripe Checkout test for $30.00 USD");

            // Викликаємо Stripe сервіс з тестовими параметрами
            string result = await _stripeService.CreateEscrowAsync(
                clientId: "test_client_" + System.DateTime.Now.Ticks.ToString(),  // Унікальний ID клієнта
                freelancerId: "test_freelancer_" + System.DateTime.Now.Ticks.ToString(),  // Унікальний ID фрілансера
                amountCents: 3000           // 30.00 USD
            );

            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log("[StripeTest] ✅ Stripe Checkout session created successfully");
            }
            else
            {
                Debug.LogError("[StripeTest] ❌ Failed to create Stripe Checkout session");
            }
        }

        private void PopulateAdminConfigList(ScrollView configList)
        {
            if (configList == null || _remoteConfig == null) return;

            configList.Clear();

            var allValues = _remoteConfig.Source?.GetAllValues();
            if (allValues == null)
            {
                var label = new Label("No keys loaded.");
                label.style.color = Color.white;
                configList.Add(label);
                return;
            }

            foreach (var kvp in allValues)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.paddingBottom = 4;
                row.style.borderBottomWidth = 1;
                row.style.borderBottomColor = new Color(1f, 1f, 1f, 0.1f);

                var keyLabel = new Label(kvp.Key);
                keyLabel.style.color = new Color(0.83f, 0.69f, 0.22f); // gold
                keyLabel.style.fontSize = 12;
                keyLabel.style.width = Length.Percent(45);

                var valLabel = new Label(kvp.Value);
                valLabel.style.color = Color.white;
                valLabel.style.fontSize = 12;
                valLabel.style.width = Length.Percent(50);
                valLabel.style.whiteSpace = WhiteSpace.Normal;

                row.Add(keyLabel);
                row.Add(valLabel);
                configList.Add(row);
            }
        }

        private async UniTaskVoid ReloadAdminConfigAsync(ScrollView configList)
        {
            if (_remoteConfig == null) return;

            var reloadBtn = _settingsPanel?.parent?.Q("DeveloperAdminPanel")?.Q<Button>("BtnAdminReload");
            if (reloadBtn != null) reloadBtn.SetEnabled(false);

            try
            {
                await _remoteConfig.ReloadAsync();
                PopulateAdminConfigList(configList);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AdminPanel] Failed to reload configuration: {ex.Message}");
            }
            finally
            {
                if (reloadBtn != null) reloadBtn.SetEnabled(true);
            }
        }

        private void UpdateSliderLabel(string sliderName, string key, string fallback)
        {
            var slider = _settingsPanel.Q<Slider>(sliderName);
            if (slider != null && slider.parent != null)
            {
                var label = slider.parent.Q<Label>();
                if (label != null)
                {
                    var tone = _localization != null ? _localization.CurrentTone : OracleTone.Business;
                    label.text = _localization.GetPersonaString(key, tone, fallback);
                }
            }
        }

    }
}