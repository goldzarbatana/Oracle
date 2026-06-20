using TimeAura.Features.Aura;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using TimeAura.Features.Matching;
using TimeAura.Features.Security;
using TimeAura.Features.UI;
using TimeAura.Features.Social;
using TimeAura.Core.Services;
using UnityEngine;
using VContainer;

namespace TimeAura.Core
{
    [DisallowMultipleComponent]
    public sealed class GlobalManager : MonoBehaviour, IService
    {
        [Header("Configuration (assign in Inspector)")]
        [SerializeField] private AppConfig appConfig;

        [Header("Services")]
        [SerializeField] private FirebaseDataService firebaseDataService;
        [SerializeField] private TwilioSmsGateway twilioSmsGateway;

        [Header("Managers")]
        [SerializeField] private AuthManager authManager;
        [SerializeField] private InitiationProcessor initiationProcessor;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private LocalizationManager localizationManager;
        [SerializeField] private AuraEffectManager auraEffectManager;
        [SerializeField] private SecurityHub securityHub;
        [SerializeField] private SocialManager socialManager;
        [SerializeField] private AudioService audioService;
        [SerializeField] private TimeAura.Core.Services.LocationService locationService;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            // We no longer call EnsureManagersExist() at runtime to avoid race conditions with VContainer.
            // All managers should be either present on the prefab or spawned by TimeAuraLifetimeScope.
        }

        public void ValidateAndLog()
        {
            var missing = new System.Collections.Generic.List<string>();

            if (appConfig == null) missing.Add(nameof(appConfig));
            if (firebaseDataService == null) missing.Add(nameof(firebaseDataService));
            if (twilioSmsGateway == null) missing.Add(nameof(twilioSmsGateway));
            if (authManager == null) missing.Add(nameof(authManager));
            if (initiationProcessor == null) missing.Add(nameof(initiationProcessor));
            if (gameManager == null) missing.Add(nameof(gameManager));
            if (uiManager == null) missing.Add(nameof(uiManager));
            if (localizationManager == null) missing.Add(nameof(localizationManager));
            if (auraEffectManager == null) missing.Add(nameof(auraEffectManager));
            if (securityHub == null) missing.Add(nameof(securityHub));
            if (socialManager == null) missing.Add(nameof(socialManager));
            if (audioService == null) missing.Add(nameof(audioService));
            if (locationService == null) missing.Add(nameof(locationService));

            if (missing.Count == 0)
            {
                Debug.Log("[GlobalManager] All inspector bindings present.");
            }
            else
            {
                Debug.LogWarning($"[GlobalManager] Missing inspector bindings: {string.Join(", ", missing)}");
            }
        }

        /// <summary>
        /// Ensure that all manager/service components exist on this GameObject.
        /// Primarily used in the Editor to setup the managers prefab.
        /// </summary>
        [ContextMenu("Ensure Managers Exist")]
        public void EnsureManagersExist()
        {
            firebaseDataService = GetComponent<FirebaseDataService>() ?? gameObject.AddComponent<FirebaseDataService>();
            twilioSmsGateway = GetComponent<TwilioSmsGateway>() ?? gameObject.AddComponent<TwilioSmsGateway>();
            authManager = GetComponent<AuthManager>() ?? gameObject.AddComponent<AuthManager>();
            initiationProcessor = GetComponent<InitiationProcessor>() ?? gameObject.AddComponent<InitiationProcessor>();
            gameManager = GetComponent<GameManager>() ?? gameObject.AddComponent<GameManager>();
            uiManager = GetComponent<UIManager>() ?? gameObject.AddComponent<UIManager>();
            localizationManager = GetComponent<LocalizationManager>() ?? gameObject.AddComponent<LocalizationManager>();
            auraEffectManager = GetComponent<AuraEffectManager>() ?? gameObject.AddComponent<AuraEffectManager>();
            securityHub = GetComponent<SecurityHub>() ?? gameObject.AddComponent<SecurityHub>();
            socialManager = GetComponent<SocialManager>() ?? gameObject.AddComponent<SocialManager>();
            audioService = GetComponent<AudioService>() ?? gameObject.AddComponent<AudioService>();
            locationService = GetComponent<TimeAura.Core.Services.LocationService>() ?? gameObject.AddComponent<TimeAura.Core.Services.LocationService>();
            
            Debug.Log("[GlobalManager] 🛠️ Ensure Managers Exist: All components verified/added.");
        }

        // Expose properties for other scripts or installers
        public AppConfig AppConfig => appConfig;
        public FirebaseDataService FirebaseDataService => firebaseDataService;
        public TwilioSmsGateway TwilioSmsGateway => twilioSmsGateway;
        public AuthManager AuthManager => authManager;
        public InitiationProcessor InitiationProcessor => initiationProcessor;
        public GameManager GameManager => gameManager;
        public UIManager UIManager => uiManager;
        public LocalizationManager LocalizationManager => localizationManager;
        public AuraEffectManager AuraEffectManager => auraEffectManager;
        public SecurityHub SecurityHub => securityHub;
        public SocialManager SocialManager => socialManager;
        public AudioService AudioService => audioService;
        public TimeAura.Core.Services.LocationService LocationService => locationService;
    }
}
