using TimeAura.Features.Aura;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using TimeAura.Features.Matching;
using TimeAura.Features.Security;
using UnityEngine;
using VContainer;

namespace TimeAura.Core
{
    [DisallowMultipleComponent]
    public sealed class BootstrapValidator : MonoBehaviour
    {
        [Inject] private AppConfig appConfig;
        [Inject] private IDataService dataService;
        [Inject] private ISmsGateway smsGateway;
        [Inject] private AuthManager authManager;
        [Inject] private GameManager gameManager;
        [Inject] private LocalizationManager localizationManager;
        [Inject] private AuraEffectManager auraEffectManager;
        [Inject] private SecurityHub securityHub;
        [Inject] private MatchingManager matchingManager;

        private async void Start()
        {
            // Wait for one frame to ensure DI injection from Bootstrapper has finished
            await Cysharp.Threading.Tasks.UniTask.NextFrame();

            var missing = new System.Collections.Generic.List<string>();

            if (appConfig == null) missing.Add(nameof(appConfig));
            if (dataService == null) missing.Add(nameof(dataService));
            if (smsGateway == null) missing.Add(nameof(smsGateway));
            if (authManager == null) missing.Add(nameof(authManager));
            if (gameManager == null) missing.Add(nameof(gameManager));
            if (localizationManager == null) missing.Add(nameof(localizationManager));
            if (auraEffectManager == null) missing.Add(nameof(auraEffectManager));
            if (securityHub == null) missing.Add(nameof(securityHub));
            if (matchingManager == null) missing.Add(nameof(matchingManager));

            if (missing.Count == 0)
            {
                Debug.Log("[BootstrapValidator] All critical DI bindings are present.");
            }
            else
            {
                Debug.LogWarning($"[BootstrapValidator] Missing DI bindings: {string.Join(", ", missing)}");
            }
        }
    }
}
