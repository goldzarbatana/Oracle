using System;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Localization;
using TimeAura.Core.Services;
using TimeAura.Features.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TimeAura.Features.UI.Initiation
{
    /// <summary>
    /// Handles the mystical Oracle prophecy, typing effects, and logo pulsation.
    /// Extracted from the InitiationScreen monolith.
    /// </summary>
    public class InitiationOracleController : MonoBehaviour
    {
        public void SetActive(bool active) => enabled = active;

        [Inject] private IOracleService _oracle;
        [Inject] private LocalizationManager _localization;

        private Label _prophecyLabel;
        private VisualElement _logo;

        public void Initialize(VisualElement root)
        {
            _prophecyLabel = root.Q<Label>("ProphecyLabel");
            _logo = root.Q<VisualElement>("LogoMain");
            
            _ = PulsateLogoAsync();
        }

        public async UniTask RequestProphecy()
        {
            if (_prophecyLabel != null && _oracle != null)
            {
                try {
                    string greeting = MysticalTerms.Greetings.GetTimeBasedGreeting();
                    string prophecy = await _oracle.RequestOracle("The Adept is at the gateway. Give them a cryptic, welcoming one-sentence prophecy.", greeting);
                    await TypeOracleText(prophecy);
                } catch (Exception ex) {
                    Debug.LogWarning($"[InitiationOracle] Oracle silent: {ex.Message}");
                    _prophecyLabel.text = _localization?.Get(AuraTerms.INIT_ORACLE_SILENT, "The Aura is silent today.") ?? "The Aura is silent today.";
                }
            }
        }

        public async UniTask TypeOracleText(string text)
        {
            if (_prophecyLabel == null) return;
            _prophecyLabel.text = "";
            foreach (char c in text)
            {
                _prophecyLabel.text += c;
                await UniTask.Delay(30);
            }
        }

        public void ShowFeedback(string message)
        {
            if (_prophecyLabel != null) _prophecyLabel.text = message;
            Debug.LogWarning($"[InitiationOracle] Feedback: {message}");
        }

        private async UniTask PulsateLogoAsync()
        {
            if (_logo == null) return;

            float baseScale = 1.0f;
            float pulseIntensity = 0.05f;
            float pulseSpeed = 1.2f;

            while (this != null && _logo != null)
            {
                // Sacred Sine Wave of Aura
                float wave = Mathf.Sin(Time.time * pulseSpeed);
                float s = baseScale + (wave * pulseIntensity);
                float opacity = 0.8f + (wave * 0.2f);

                // Apply Scale & Opacity
                _logo.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
                _logo.style.opacity = opacity;

                if (_prophecyLabel != null)
                {
                    float pOpacity = 0.6f + (wave * 0.4f);
                    _prophecyLabel.style.opacity = pOpacity;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
    }
}
