using System;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Auth;
using TimeAura.Features.UI;
using TimeAura.Features.Localization;
using TimeAura.Core.Localization;
using TimeAura.Core.Data.SO;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// OnboardingController — UX Audit #14: 4-step interactive tutorial for new users.
    /// Shows once per device (tracked via PlayerPrefs).
    /// Explains Nexus, Feed, Aura, and Horas in plain language with mystical framing.
    /// </summary>
    public class OnboardingController : MonoBehaviour
    {
        private const string PREF_NEXUS = "onboarding_nexus_v2_done";
        private const string PREF_FEED = "onboarding_feed_v2_done";
        private const string PREF_AURA = "onboarding_aura_v2_done";
        private const string PREF_HORAS = "onboarding_horas_v2_done";

        [Inject] private UIManager _uiManager;
        [Inject] private AuthManager _authManager;
        [Inject] private LocalizationManager _localization;

        private void Start()
        {
            var nav = UnityEngine.Object.FindAnyObjectByType<NexusNavigationManager>(FindObjectsInactive.Include);
            if (nav != null)
            {
                nav.OnPanelSwitched += HandlePanelSwitched;
            }
        }

        private void OnDestroy()
        {
            var nav = UnityEngine.Object.FindAnyObjectByType<NexusNavigationManager>(FindObjectsInactive.Include);
            if (nav != null)
            {
                nav.OnPanelSwitched -= HandlePanelSwitched;
            }
        }

        private void HandlePanelSwitched(string panelId)
        {
            TryShowOnboardingAsync(panelId).Forget();
        }

        public async UniTask TryShowOnboardingAsync(string panelId = null)
        {
            var profile = _authManager?.CurrentProfile;
            if (profile == null) return;

            await UniTask.Delay(800); // Wait for animations to settle
            
            // If called without a panel ID, find current
            if (string.IsNullOrEmpty(panelId))
            {
                var nav = UnityEngine.Object.FindAnyObjectByType<NexusNavigationManager>(FindObjectsInactive.Include);
                panelId = nav != null ? nav.ActivePanelId : "sanctuary";
            }

            var tone = profile.OracleTone;

            if (panelId == "sanctuary")
            {
                if (PlayerPrefs.GetInt(PREF_NEXUS, 0) == 0)
                {
                    string title = _localization?.GetPersonaString(AuraTerms.ONBOARDING_NEXUS_TITLE, tone, "🌌 Ласкаво просимо в NEXUS") ?? "🌌 Ласкаво просимо в NEXUS";
                    string body = _localization?.GetPersonaString(AuraTerms.ONBOARDING_NEXUS_BODY, tone, "NEXUS — твій центр обміну часом і навичками.\n\nТут ти знайдеш людей, які пропонують те, що тобі потрібно, і навпаки.") ?? "NEXUS — твій центр обміну часом і навичками.\n\nТут ти знайдеш людей, які пропонують те, що тобі потрібно, і навпаки.";
                    await ShowStep(title, body, PREF_NEXUS);
                }
                else if (PlayerPrefs.GetInt(PREF_HORAS, 0) == 0)
                {
                    string title = _localization?.GetPersonaString(AuraTerms.ONBOARDING_HORAS_TITLE, tone, "⧖ ХОРИ (HORAS)") ?? "⧖ ХОРИ (HORAS)";
                    string body = _localization?.GetPersonaString(AuraTerms.ONBOARDING_HORAS_BODY, tone, "ХОРИ — валюта Нексусу.\n\n1 ХОРА = 1 година праці.\n\nТи отримуєш Хори, коли хтось цінує твій час. Витрачаєш — коли сам замовляєш послугу.") ?? "ХОРИ — валюта Нексусу.\n\n1 ХОРА = 1 година праці.\n\nТи отримуєш Хори, коли хтось цінує твій час. Витрачаєш — коли сам замовляєш послугу.";
                    await ShowStep(title, body, PREF_HORAS);
                }
            }
            else if (panelId == "feed")
            {
                if (PlayerPrefs.GetInt(PREF_FEED, 0) == 0)
                {
                    string title = _localization?.GetPersonaString(AuraTerms.ONBOARDING_FEED_TITLE, tone, "✨ СТРІЧКА (FEED)") ?? "✨ СТРІЧКА (FEED)";
                    string body = _localization?.GetPersonaString(AuraTerms.ONBOARDING_FEED_BODY, tone, "Тут живуть Адепти — люди з навичками, які тобі потрібні.\n\nПрогортай картки, натисни на профіль — і розпочни Симетрію (угоду).") ?? "Тут живуть Адепти — люди з навичками, які тобі потрібні.\n\nПрогортай картки, натисни на профіль — і розпочни Симетрію (угоду).";
                    await ShowStep(title, body, PREF_FEED);
                }
            }
            else if (panelId == "aura")
            {
                if (PlayerPrefs.GetInt(PREF_AURA, 0) == 0)
                {
                    string title = _localization?.GetPersonaString(AuraTerms.ONBOARDING_AURA_TITLE, tone, "🌀 АУРА (AURA)") ?? "🌀 АУРА (AURA)";
                    string body = _localization?.GetPersonaString(AuraTerms.ONBOARDING_AURA_BODY, tone, "Тут ти налаштовуєш своє резюме Нексусу:\n\n✨ MANIFEST = що ти вмієш і пропонуєш\n🔍 INTENT = що шукаєш\n\nЧим точніше — тим краще збіги!") ?? "Тут ти налаштовуєш своє резюме Нексусу:\n\n✨ MANIFEST = що ти вмієш і пропонуєш\n🔍 INTENT = що шукаєш\n\nЧим точніше — тим краще збіги!";
                    await ShowStep(title, body, PREF_AURA);
                }
            }
        }

        private async UniTask ShowStep(string title, string body, string prefKey)
        {
            var tcs = new UniTaskCompletionSource();
            Debug.Log($"[Onboarding] ⏳ Step {prefKey} showing...");
            var tone = _authManager?.CurrentProfile?.OracleTone ?? OracleTone.Business;
            string confirmText = _localization?.GetPersonaString(AuraTerms.BTN_UNDERSTOOD, tone, "ЗРОЗУМІЛО ✦") ?? "ЗРОЗУМІЛО ✦";
            _uiManager?.ShowOnboardingStep(
                title,
                body,
                () => {
                    Debug.Log($"[Onboarding] ⚡ Step {prefKey} callback onConfirm invoked!");
                    tcs.TrySetResult();
                },
                confirmText
            );
            await tcs.Task;
            PlayerPrefs.SetInt(prefKey, 1);
            PlayerPrefs.Save();
            Debug.Log($"[Onboarding] ✓ Step {prefKey} completed and saved.");
        }

        [ContextMenu("Reset Onboarding")]
        public void ResetOnboarding()
        {
            PlayerPrefs.DeleteKey(PREF_NEXUS);
            PlayerPrefs.DeleteKey(PREF_FEED);
            PlayerPrefs.DeleteKey(PREF_AURA);
            PlayerPrefs.DeleteKey(PREF_HORAS);
            PlayerPrefs.DeleteKey("hint_horas_shown");
            Debug.Log("[Onboarding] Reset. Will show again on next play.");
        }
    }
}
