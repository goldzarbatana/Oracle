using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;
using VContainer;

namespace TimeAura.Core.Infrastructure
{
    /// <summary>
    /// AppOrchestrator — The Great Conductor of the Time Aura Boot Sequence.
    /// Manages the strict asynchronous initialization of core systems.
    /// </summary>
    public sealed class AppOrchestrator : MonoBehaviour
    {
        [Header("Manual Assignment (Optional Fallback)")]
        [SerializeField] private List<MonoBehaviour> _manualManagers = new List<MonoBehaviour>();

        private void Awake()
        {
            // If we are part of the global core, we should survive transitions
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private AuthManager _auth;
        private LocalizationManager _loc;
        private IDataService _data;
        private GameManager _game;
        private AudioService _audio;
        private IEnumerable<IManager> _managers;
        private RemoteConfigService _remoteConfig;
        private TelemetryService _telemetry;

        private bool _isInjected = false;

        [Inject]
        public void Construct(
            AuthManager auth, 
            LocalizationManager loc, 
            IDataService data, 
            GameManager game, 
            AudioService audio, 
            IEnumerable<IManager> managers,
            RemoteConfigService remoteConfig,
            TelemetryService telemetry)
        {
            _auth = auth;
            _loc = loc;
            _data = data;
            _game = game;
            _audio = audio;
            _managers = managers;
            _remoteConfig = remoteConfig;
            _telemetry = telemetry;
            _isInjected = true;
            Debug.Log("[Orchestrator] 💉 Dependencies injected via Temple Ritual.");
        }

        public bool IsInitialized { get; private set; }

        private async void Start()
        {
            // Wait a few frames for VContainer to settle
            await UniTask.DelayFrame(5);
            
            if (!_isInjected)
            {
                Debug.LogWarning("[Orchestrator] ⚠️ Not injected by DI. Attempting manual scene recovery...");
                RecoverManagersManually();
            }

            await StartAsync(destroyCancellationToken);
        }

        private void RecoverManagersManually()
        {
            _auth = UnityEngine.Object.FindAnyObjectByType<AuthManager>(FindObjectsInactive.Include);
            _loc = UnityEngine.Object.FindAnyObjectByType<LocalizationManager>(FindObjectsInactive.Include);
            _game = UnityEngine.Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            _audio = UnityEngine.Object.FindAnyObjectByType<AudioService>(FindObjectsInactive.Include);
            _data = UnityEngine.Object.FindAnyObjectByType<FirebaseDataService>(FindObjectsInactive.Include);
            
            if (_data == null)
            {
                var allComp = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                _data = allComp.OfType<IDataService>().FirstOrDefault();
            }
            
            var foundManagers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                .OfType<IManager>()
                                .ToList();
            
            _managers = foundManagers;
            Debug.Log($"[Orchestrator] 🛠️ Manual recovery found {foundManagers.Count} managers and {( _data != null ? "DataService" : "NO DataService")}.");
        }

        /// <summary>
        /// The Boot Sequence: A strict ritual to awaken the application.
        /// </summary>
        public async UniTask StartAsync(CancellationToken ct)
        {
            Debug.Log("[Orchestrator] 🌌 Initiating Great Ritual (Boot Sequence)...");

            try 
            {
                // 1. Firebase Core Check
                Debug.Log("[Orchestrator] 🏗️ Verifying Firebase Ley Lines...");
                var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != Firebase.DependencyStatus.Available)
                {
                    Debug.LogError($"[Orchestrator] ❌ Firebase Ley Lines broken: {status}. The ritual is halted. Error Code: {(int)status}");
                    return;
                }
                
                var app = Firebase.FirebaseApp.DefaultInstance;
                Debug.Log($"[Orchestrator] ✅ Firebase Ley Lines stabilized for App: {app.Name}");

                if (_remoteConfig != null) await _remoteConfig.InitializeAsync();
                if (_telemetry != null) await _telemetry.InitializeAsync();

                // 2. Core Service Initialization
                Debug.Log("[Orchestrator] 💾 Connecting to Akashic Records (Core Services)...");
                
                var allManagers = new List<IManager>();
                if (_managers != null) allManagers.AddRange(_managers);
                
                // Add manual ones if they implement IManager
                foreach (var mb in _manualManagers)
                {
                    if (mb is IManager m && !allManagers.Contains(m))
                    {
                        allManagers.Add(m);
                    }
                }

                if (allManagers.Count == 0)
                {
                    Debug.LogError("[Orchestrator] 💀 No managers found! Check LifetimeScope and Manual Assignment.");
                    return;
                }

                foreach (var manager in allManagers)
                {
                    if (manager == null || manager.IsInitialized) continue;

                    Debug.Log($"[Orchestrator] ✨ Awakening {manager.GetType().Name}...");
                    await manager.InitializeAsync(ct);
                }

                // 4. Session Verification
                Debug.Log("[Orchestrator] 👤 Checking Adept identity...");
                bool isAuthenticated = await _auth.CheckSessionAsync(ct);
                bool hasCompletedInitiation = isAuthenticated && _auth.CurrentProfile != null && _auth.CurrentProfile.HasCompletedInitiation;

                // 5. Transcend to Next Realm
                string targetScene = hasCompletedInitiation ? "Nexus" : "InitiationScene";
                Debug.Log($"[Orchestrator] 🚀 Boot Ritual Complete. Transcending to {targetScene}...");
                
                var handle = Addressables.LoadSceneAsync(targetScene);
                await handle.ToUniTask(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Orchestrator] 💀 Boot Ritual Failed: {ex.Message}");
            }
        }
    }
}
