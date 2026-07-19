using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Services;
using TimeAura.Features.Aura;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using TimeAura.Features.Matching;
using TimeAura.Features.Security;
using TimeAura.Features.Social;
using TimeAura.Features.Harmony;
using TimeAura.Features.UI;
using TimeAura.Features.UI.Initiation;
using TimeAura.Features.UI.Nexus;
using TimeAura.Features.Economy;
using TimeAura.Core.Data.SO;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using System.Linq;

namespace TimeAura.Core.Infrastructure
{
    public class TimeAuraLifetimeScope : LifetimeScope
    {
        [Header("Configurations")]
        [SerializeField] private AppConfig appConfig;

        [Header("Global Managers Prefab")]
        [SerializeField] private GameObject managersPrefab;

        protected override void Awake()
        {
            var allScopes = UnityEngine.Object.FindObjectsByType<TimeAuraLifetimeScope>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var scope in allScopes)
            {
                if (scope != this && scope.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    Debug.Log("[TimeAuraLifetimeScope] ℹ️ Persistent root already exists. Removing local duplicate.");
                    Destroy(this);
                    return;
                }
            }

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("[TimeAuraLifetimeScope] ✦ Established Global Persistent Root.");

            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            Debug.Log("[TimeAuraLifetimeScope] Configure — registering all services...");

            EnsureManagersSpawned();

            if (appConfig == null)
            {
                appConfig = Resources.Load<AppConfig>("Settings/AppConfig");
            }

            if (appConfig != null)
            {
                appConfig.LoadLocalOverrides();
                builder.RegisterInstance(appConfig).AsSelf();
            }
            else
            {
                var fallback = ScriptableObject.CreateInstance<AppConfig>();
                fallback.LoadLocalOverrides();
                builder.RegisterInstance(fallback).AsSelf();
            }

            // Data registration
            var pillars = Resources.LoadAll<AuraPillarSO>("Settings/Pillars");
            builder.RegisterInstance(pillars).AsSelf();

            var oracles = Resources.LoadAll<OracleSO>("Settings/Oracles");
            builder.RegisterInstance(oracles).AsSelf();

            var economy = Resources.Load<ResonanceEconomySO>("Settings/Economy_Standard");
            if (economy != null) builder.RegisterInstance(economy).AsSelf();

            // Personas are now loaded dynamically in GeminiOracleService

            var tiers = Resources.LoadAll<LegacyTierSO>("Settings/Tiers");
            builder.RegisterInstance(tiers).AsSelf();

            // Services
            builder.Register(c => new NetworkService(), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new MediaService(), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<GoogleSheetsConfigSource>(Lifetime.Singleton).As<IRemoteConfigSource>();
            builder.Register<RemoteConfigService>(Lifetime.Singleton).AsSelf();
            builder.Register<TelemetryService>(Lifetime.Singleton).AsSelf();
            builder.Register<FirebaseBackendService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            
            // Oracle Providers
            builder.Register(c => new GeminiOracleProvider(c.Resolve<AppConfig>()), Lifetime.Singleton).AsSelf();
            builder.Register(c => new QwenOracleProvider(c.Resolve<AppConfig>()), Lifetime.Singleton).AsSelf();
            
            // Oracle Router (Main IOracleService entry point)
            builder.Register(c => new OracleServiceProvider(
                c.Resolve<GeminiOracleProvider>(), 
                c.Resolve<QwenOracleProvider>(), 
                c.Resolve<LocalizationManager>(),
                c.Resolve<RemoteConfigService>(),
                c.Resolve<TelemetryService>()
            ), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            
            // Business Logic Oracle
            builder.Register(c => new GeminiOracleService(c.Resolve<IOracleService>(), c.Resolve<AuthManager>(), c.Resolve<LocalizationManager>(), c.Resolve<AuraPillarSO[]>()), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new AddressableAssetService(), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new GeocodingService(), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new HapticService(), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new TimeAura.Features.UI.Oracle.OracleWhisperManager(c.Resolve<HapticService>(), c.Resolve<AudioService>(), c.Resolve<IAuraOracleService>()), Lifetime.Singleton).AsSelf();
            builder.Register(c => new PushNotificationService(
                c.Resolve<IDataService>(),
                c.Resolve<AuthManager>()
            ), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new OraclePromptFactory(c.Resolve<IOracleService>(), c.Resolve<AuthManager>(), c.Resolve<IDataService>()), Lifetime.Singleton).AsSelf();
            builder.Register(c => new AuraValueCalculator(
                c.Resolve<AuraPillarSO[]>(),
                UnityEngine.Object.FindAnyObjectByType<TimeAura.Core.Services.LocationService>(FindObjectsInactive.Include)
            ), Lifetime.Singleton).AsSelf();

            // Event Orchestration Bridge & Executor
            builder.Register(c => new EventCloudBridge(c.Resolve<AuthManager>()), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new ActionExecutor(
                c.Resolve<TimeAura.Features.UI.Oracle.OracleWhisperManager>(),
                c.Resolve<AudioService>(),
                c.Resolve<AuthManager>(),
                c.Resolve<IOracleService>(),
                c.Resolve<HorasEconomyService>(),
                c.Resolve<IDataService>()
            ), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            // Global Manager
            var globalManager = UnityEngine.Object.FindAnyObjectByType<GlobalManager>(FindObjectsInactive.Include);
            if (globalManager != null) builder.RegisterComponent(globalManager).AsSelf();

            // Managers
            RegisterManagerIfPresent<AudioService>(builder);
            RegisterManagerIfPresent<GameManager>(builder);
            RegisterManagerIfPresent<AuthManager>(builder);
            RegisterManagerIfPresent<LocalizationManager>(builder);
            RegisterManagerIfPresent<TimeAura.Core.Services.LocationService>(builder);
            RegisterManagerIfPresent<UIManager>(builder);
            RegisterManagerIfPresent<SocialManager>(builder);
            RegisterManagerIfPresent<AuraEffectManager>(builder);
            RegisterManagerIfPresent<InitiationProcessor>(builder);

            builder.Register(c => new AuraPresenter(c.Resolve<IDataService>(), c.Resolve<AuthManager>(), c.Resolve<IAuraOracleService>(), c.Resolve<AuraPillarSO[]>()), Lifetime.Singleton).AsSelf();
            builder.Register(c => new HarmonyManager(c.Resolve<IDataService>(), c.Resolve<SocialManager>()), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new HorasEconomyService(c.Resolve<IDataService>(), c.Resolve<ResonanceEconomySO>(), c.Resolve<AuraValueCalculator>()), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<QuantumWalletService>(Lifetime.Singleton).AsSelf();
            builder.Register<TimeWalletService>(Lifetime.Singleton).AsSelf();
            
            // Stripe Service Registration - Use Real or Mock based on config
            if (appConfig != null && appConfig.UseRealStripeTestMode)
            {
                builder.Register(c => new RealStripeService(), Lifetime.Singleton).As<IStripeService>().AsSelf();
                Debug.Log("[TimeAuraLifetimeScope] 💳 Registered RealStripeService (Test Mode)");
            }
            else
            {
                builder.Register<MockStripeService>(Lifetime.Singleton).As<IStripeService>().AsSelf();
                Debug.Log("[TimeAuraLifetimeScope] 💳 Registered MockStripeService");
            }
            
            builder.Register<OracleIntentParser>(Lifetime.Singleton).AsSelf();
            builder.Register<TimeAura.Features.Economy.MonetizationEconomyAdapter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new MatchingManager(c.Resolve<IDataService>()), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register(c => new MatchmakingManager(
                c.Resolve<IDataService>(), 
                c.Resolve<AppConfig>(),
                c.Resolve<AuthManager>(),
                c.Resolve<HorasEconomyService>(),
                c.Resolve<LocalizationManager>()), Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            
            RegisterManagerIfPresent<SecurityHub>(builder);
            RegisterComponentIfPresent<AppOrchestrator>(builder);

            // Interface Services
            RegisterServiceIfPresent<FirebaseDataService, IDataService>(builder, () => builder.Register(c => new NullDataService(), Lifetime.Singleton).As<IDataService>());
            RegisterServiceIfPresent<TwilioSmsGateway, ISmsGateway>(builder, () => builder.Register(c => new NullSmsGateway(), Lifetime.Singleton).As<ISmsGateway>());

            // Scene Components
            RegisterComponentIfPresent<InitiationScreen>(builder);
            RegisterComponentIfPresent<TimeAura.Features.UI.Nexus.NexusController>(builder);
            RegisterComponentIfPresent<TimeAura.Features.UI.Nexus.ChamberController>(builder);
            RegisterComponentIfPresent<TimeAura.Features.UI.Harmony.HarmonyWorkspaceController>(builder);

            // Bootstrapper
            builder.Register(c => new TimeAuraBootstrapper(c, c.Resolve<GameManager>(), c.Resolve<UIManager>()), Lifetime.Singleton).AsImplementedInterfaces();
        }

        private void EnsureManagersSpawned()
        {
            var globalRoot = UnityEngine.Object.FindAnyObjectByType<GlobalManager>(FindObjectsInactive.Include);
            if (globalRoot == null && managersPrefab != null)
            {
                var instance = Instantiate(managersPrefab);
                instance.name = managersPrefab.name;
                DontDestroyOnLoad(instance);
            }
        }

        private void RegisterComponentIfPresent<T>(IContainerBuilder builder) where T : Component
        {
            var found = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (found != null) builder.RegisterComponent(found).AsSelf();
        }

        private void RegisterManagerIfPresent<T>(IContainerBuilder builder) where T : Component, IManager
        {
            var found = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (found != null) builder.RegisterComponent(found).AsImplementedInterfaces().AsSelf();
        }

        private void RegisterServiceIfPresent<TComponent, TInterface>(IContainerBuilder builder, System.Action fallback) where TComponent : Component
        {
            var found = UnityEngine.Object.FindAnyObjectByType<TComponent>(FindObjectsInactive.Include);
            if (found != null) builder.RegisterComponent(found).As<TInterface>().AsSelf();
            else fallback?.Invoke();
        }
    }

    internal sealed class NullDataService : IDataService
    {
        public UniTask<TimeAura.Features.Data.UserProfile> GetUserProfileAsync(string userId, System.Threading.CancellationToken ct) => UniTask.FromResult<TimeAura.Features.Data.UserProfile>(null);
        public UniTask<System.Collections.Generic.List<TimeAura.Features.Data.UserProfile>> GetAllProfilesAsync(System.Threading.CancellationToken ct) => UniTask.FromResult(new System.Collections.Generic.List<TimeAura.Features.Data.UserProfile>());
        public UniTask SaveUserProfileAsync(TimeAura.Features.Data.UserProfile profile, System.Threading.CancellationToken ct) => UniTask.CompletedTask;
        public UniTask SendHarmonyMessageAsync(string sessionId, ChatMessage message) => UniTask.CompletedTask;
        public System.IDisposable ListenToHarmonyMessages(string sessionId, System.Action<System.Collections.Generic.List<ChatMessage>> onMessagesChanged) => null;
    }

    internal sealed class NullSmsGateway : ISmsGateway
    {
        public System.Threading.Tasks.Task SendSmsAsync(string phoneNumber, string message, System.Threading.CancellationToken ct) => System.Threading.Tasks.Task.CompletedTask;
    }

    public class TimeAuraBootstrapper : IStartable, System.IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly GameManager _gameManager;
        private readonly UIManager _uiManager;

        public TimeAuraBootstrapper(IObjectResolver resolver, GameManager gameManager = null, UIManager uiManager = null)
        {
            _resolver = resolver;
            _gameManager = gameManager;
            _uiManager = uiManager;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void Dispose() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => InjectSceneObjects();

        private void InjectSceneObjects()
        {
            var initScreen = UnityEngine.Object.FindAnyObjectByType<InitiationScreen>(FindObjectsInactive.Include);
            if (initScreen != null)
            {
                _resolver.Inject(initScreen);
                foreach (var c in initScreen.GetComponents<MonoBehaviour>()) _resolver.Inject(c);
            }

            var nexus = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Nexus.NexusController>(FindObjectsInactive.Include);
            if (nexus != null)
            {
                _resolver.Inject(nexus);
                foreach (var c in nexus.GetComponents<MonoBehaviour>()) _resolver.Inject(c);
            }
            
            var oracleWidget = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Oracle.OracleWidgetController>(FindObjectsInactive.Include);
            if (oracleWidget != null) _resolver.Inject(oracleWidget);

            var workspace = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Harmony.HarmonyWorkspaceController>(FindObjectsInactive.Include);
            if (workspace != null) _resolver.Inject(workspace);
        }

        public async void Start()
        {
            InjectSceneObjects();
            if (_gameManager != null) await _gameManager.InitializeAsync(default);
        }
    }
}