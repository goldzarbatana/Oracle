using System;
using TimeAura.Core.Services;
using TimeAura.Features.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TimeAura.Features.UI.Initiation
{
    /// <summary>
    /// Handles the language carousel and switching logic for the Initiation scene.
    /// Extracted from the InitiationScreen monolith.
    /// </summary>
    public class InitiationLanguageController : MonoBehaviour
    {
        public void SetActive(bool active) => enabled = active;

        [Inject] private LocalizationManager _localization;

        private ScrollView _langCarousel;
        private VisualElement _root;

        public void Initialize(VisualElement root)
        {
            _root = root;
            _langCarousel = root.Q<ScrollView>("LangCarousel");

            // Map buttons to languages
            BindLang("BtnUK", SystemLanguage.Ukrainian);
            BindLang("BtnEN", SystemLanguage.English);
            BindLang("BtnES", SystemLanguage.Spanish);
            BindLang("BtnFR", SystemLanguage.French);
            BindLang("BtnDE", SystemLanguage.German);
            BindLang("BtnIT", SystemLanguage.Italian);
            BindLang("BtnPL", SystemLanguage.Polish);
            BindLang("BtnRU", SystemLanguage.Russian);
            BindLang("BtnTR", SystemLanguage.Turkish);
            BindLang("BtnHI", SystemLanguage.Hindi);

            // Carousel Arrows
            var arrowLeft = root.Q<Label>("ArrowLeft");
            var arrowRight = root.Q<Label>("ArrowRight");
            if (arrowLeft != null) arrowLeft.RegisterCallback<ClickEvent>(e => ScrollCarousel(-300));
            if (arrowRight != null) arrowRight.RegisterCallback<ClickEvent>(e => ScrollCarousel(300));
        }

        private void BindLang(string btnName, SystemLanguage lang)
        {
            var btn = _root.Q<Button>(btnName);
            if (btn != null)
            {
                btn.clicked += () => {
                    Debug.Log($"[InitiationLang] 🌐 Switch to: {lang}");
                    _localization?.SetLanguage(lang);
                    UpdateLanguageSelectionUI(btn);
                };
                
                // Set initial active state if matches current lang
                if (_localization != null && _localization.CurrentLanguage == lang)
                    btn.AddToClassList("lang-btn--active");
            }
        }

        private void UpdateLanguageSelectionUI(Button selectedBtn)
        {
            _root.Query<Button>(className: "lang-btn").ForEach(b => b.RemoveFromClassList("lang-btn--active"));
            selectedBtn.AddToClassList("lang-btn--active");
        }

        private void ScrollCarousel(float offset)
        {
            if (_langCarousel == null) return;
            var target = _langCarousel.scrollOffset;
            target.x += offset;
            _langCarousel.scrollOffset = target;
        }
    }
}
