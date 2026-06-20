using System;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Auth;
using TimeAura.Features.UI;
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

            if (panelId == "sanctuary")
            {
                if (PlayerPrefs.GetInt(PREF_NEXUS, 0) == 0)
                {
                    await ShowStep("\ud83c\udf0c Ласкаво просимо в NEXUS", "NEXUS — твій центр обміну часом і навичками.\n\nТут ти знайдеш людей, які пропонують те, що тобі потрібно, і навпаки.", PREF_NEXUS);
                }
                else if (PlayerPrefs.GetInt(PREF_HORAS, 0) == 0)
                {
                    await ShowStep("\u29c6 ХОРИ (HORAS)", "ХОРИ — валюта Нексусу.\n\n1 ХОРА = 1 година праці.\n\nТи отримуєш Хори, коли хтось цінує твій час. Витрачаєш — коли сам замовляєш послугу.", PREF_HORAS);
                }
            }
            else if (panelId == "feed")
            {
                if (PlayerPrefs.GetInt(PREF_FEED, 0) == 0)
                {
                    await ShowStep("\u2728 СТРІЧКА (FEED)", "Тут живуть Адепти — люди з навичками, які тобі потрібні.\n\nПрогортай картки, натисни на профіль — і розпочни Симетрію (угоду).", PREF_FEED);
                }
            }
            else if (panelId == "aura")
            {
                if (PlayerPrefs.GetInt(PREF_AURA, 0) == 0)
                {
                    await ShowStep("\ud83c\udf00 АУРА (AURA)", "Тут ти налаштовуєш своє резюме Нексусу:\n\n\ud83c\udf81 MANIFEST = що ти вмієш і пропонуєш\n\ud83d\udd0d INTENT = що шукаєш\n\nЧим точніше — тим краще збіги!", PREF_AURA);
                }
            }
        }

        private async UniTask ShowStep(string title, string body, string prefKey)
        {
            var tcs = new UniTaskCompletionSource();
            Debug.Log($"[Onboarding] ⏳ Step {prefKey} showing...");
            _uiManager?.ShowOnboardingStep(
                title,
                body,
                () => {
                    Debug.Log($"[Onboarding] ⚡ Step {prefKey} callback onConfirm invoked!");
                    tcs.TrySetResult();
                }
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
