using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Localization;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Core;
using TimeAura.Core.Services;
using TimeAura.Features.Economy;
using TimeAura.Features.Harmony;
using TimeAura.Features.Social;
using TimeAura.Features.Aura;
using TimeAura.Features.UI.Oracle;
using TimeAura.Features.UI.Social;
using TimeAura.Core.Data.SO;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// The Nexus Orchestrator. Coordinates specialized sub-controllers.
    /// No longer a monolith.
    /// </summary>
    public class NexusController : MonoBehaviour
    {
        private static NexusController _instance;
        public static NexusController Instance => _instance;

        [Header("Persistence")]
        [SerializeField] private bool _makePersistent = true;
        [Header("UI Bridge")]
        [SerializeField] private UIDocument uiDocument;
        [Header("Styles & Themes")]
        [SerializeField] private List<StyleSheet> _nexusStyleSheets;

        [Inject] private LocalizationManager _localization;
        [Inject] private AuthManager _authManager;
        [Inject] private IDataService _dataService;
        [Inject] private IOracleService _gemini;
        [Inject] private IAuraOracleService _oracleService;
        [Inject] private Matching.MatchmakingManager _matchmakingManager;
        [Inject] private HorasEconomyService _economy;
        [Inject] private MediaService _media;
        [Inject] private AudioService _audioService;
        [Inject] private UIManager _uiManager;
        [Inject] private HapticService _hapticService;
        [Inject] private AuraPresenter _auraPresenter;
        [Inject] private OracleWhisperManager _whisperManager;
        [Inject] private IObjectResolver _resolver;
        [Inject] private OraclePromptFactory _oraclePromptFactory;
        [Inject] private QuantumWalletService _walletService;
        [Inject] private OracleSO[] _oracles;

        [Header("Sub-Controllers")]
        [SerializeField] private TimeAura.Features.UI.Oracle.OracleWidgetController _oracleWidget;
        [SerializeField] private RadarController _radar;
        [SerializeField] private NexusNavigationManager _navigation;
        [SerializeField] private NexusSettingsController _settings;
        [SerializeField] private VisualTreeAsset _fateCardTemplate;
        [SerializeField] private NexusContentController _content;
        [SerializeField] private NexusMenuController _menu;
        [SerializeField] private VaultController _vault;
        [SerializeField] private AuraVFXController _vfx;
        [SerializeField] private ConvergenceFeedController _feed;

        private ChamberController _chamber;
        private HarmonyChannelController _harmonyChannel;
        private MasterDossierController _masterDossier;
        private SymmetryRiteController _symmetryRite;
        private OracleHorasEvaluatorController _horasEvaluator;
        private string _pendingChatHistory;
        private int _pendingFinalEscrowAmount;
        private UserProfile _activePartner;

        public HarmonyChannelController HarmonyChannel => _harmonyChannel;
        public UserProfile ActivePartner => _activePartner;

        private VisualElement _screenRoot, _panelVault, _panelFeed, _auraPanel, _discoveryPanel, _welcomeRite, _settingsPanel;
        private ScrollView _discoveryFeed;
        private List<Button> _discoveryTabs = new();
        private Label _lblCurrentLangHUD;
        
        private AuraView _auraView;
        private IDisposable _langSubscription;
        private IDisposable _autonomousMatchSubscription;
        private VoiceCaptureService _voiceCapture;
        private OnboardingController _onboarding;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log($"[NexusController] 🧬 Duplicate detected on {gameObject.scene.name}. Dissolving.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                // Hide immediately to prevent unlocalized text flash during async initialization
                uiDocument.rootVisualElement.style.opacity = 0f;
            }

            if (_makePersistent)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }

            // Auto-find if not assigned
            if (_radar == null) _radar = GetComponentInChildren<RadarController>();
            if (_navigation == null) _navigation = GetComponentInChildren<NexusNavigationManager>();
            if (_settings == null) _settings = GetComponentInChildren<NexusSettingsController>();
            if (_content == null) _content = GetComponentInChildren<NexusContentController>();
            if (_menu == null) _menu = GetComponentInChildren<NexusMenuController>();
            if (_menu == null)
            {
                _menu = gameObject.AddComponent<NexusMenuController>();
                Debug.Log("[NexusController] 🛠️ NexusMenuController missing - automatically synthesized.");
            }
            if (_vault == null) _vault = GetComponentInChildren<VaultController>();
            if (_vault == null)
            {
                _vault = gameObject.AddComponent<VaultController>();
                Debug.Log("[NexusController] 🛠️ VaultController missing - automatically synthesized.");
            }
            // Ensure Oracle Controllers are active
            if (_oracleWidget == null) _oracleWidget = FindAnyObjectByType<TimeAura.Features.UI.Oracle.OracleWidgetController>(FindObjectsInactive.Include);
            
            if (gameObject.GetComponent<SanctuaryUIController>() == null)
                gameObject.AddComponent<SanctuaryUIController>();

            if (gameObject.GetComponent<GeminiChatController>() == null)
                gameObject.AddComponent<GeminiChatController>();

            if (gameObject.GetComponent<MysticalShopController>() == null)
                gameObject.AddComponent<MysticalShopController>();

            if (_feed == null) _feed = GetComponentInChildren<ConvergenceFeedController>();
            if (_feed == null) _feed = gameObject.AddComponent<ConvergenceFeedController>();

            if (_chamber == null) _chamber = GetComponentInChildren<ChamberController>();
            if (_chamber == null) _chamber = gameObject.AddComponent<ChamberController>();
            if (_oracleWidget != null)
            {
                Debug.Log("[NexusController] 👁️ Linked to Global OracleWidgetController.");
            }

            if (_vfx == null) _vfx = FindAnyObjectByType<AuraVFXController>();
            if (_vfx == null)
            {
                var prefab = Resources.Load<GameObject>("Prefabs/AuraVFX_Core");
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    _vfx = instance.GetComponent<AuraVFXController>();
                    Debug.Log("[NexusController] ✧ Spawned AuraVFX_Core from Resources.");
                }
            }

            // Ensure UI Safe Area is handled
            if (gameObject.GetComponent<TimeAura.Core.UI.UISafeAreaHandler>() == null)
                gameObject.AddComponent<TimeAura.Core.UI.UISafeAreaHandler>();

            if (_voiceCapture == null)
                _voiceCapture = gameObject.AddComponent<VoiceCaptureService>();
        }

        private bool _isRitualExecuted = false;

        private async UniTaskVoid Start()
        {
            // ═══ STEP 1: Ensure UIDocument exists ═══
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("[NexusController] ❌ No UIDocument component found!");
                return;
            }

            // ═══ STEP 2: Diagnose the document state ═══
            Debug.Log($"[NexusController] 🔍 DIAGNOSTICS:" +
                $"\n  UIDocument: {uiDocument.name}" +
                $"\n  VisualTreeAsset: {(uiDocument.visualTreeAsset != null ? uiDocument.visualTreeAsset.name : "NULL")}" +
                $"\n  PanelSettings: {(uiDocument.panelSettings != null ? uiDocument.panelSettings.name : "⚠️ NULL")}" +
                $"\n  rootVisualElement: {(uiDocument.rootVisualElement != null ? $"EXISTS (children: {uiDocument.rootVisualElement.childCount})" : "NULL")}" +
                $"\n  Enabled: {uiDocument.enabled}" +
                $"\n  GameObject Active: {uiDocument.gameObject.activeInHierarchy}");

            // ═══ STEP 3: Fix missing PanelSettings ═══
            if (uiDocument.panelSettings == null)
            {
                Debug.LogWarning("[NexusController] ⚡ PanelSettings is NULL! Searching for one...");
                
                // Try to find PanelSettings from another UIDocument in the scene
                var otherDocs = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var doc in otherDocs)
                {
                    if (doc != uiDocument && doc.panelSettings != null)
                    {
                        uiDocument.panelSettings = doc.panelSettings;
                        Debug.Log($"[NexusController] ✅ Borrowed PanelSettings from: {doc.name} → {doc.panelSettings.name}");
                        break;
                    }
                }

                // If still null, try loading from Resources
                if (uiDocument.panelSettings == null)
                {
                    var ps = Resources.Load<PanelSettings>("Settings/NexusPanelSettings");
                    if (ps != null)
                    {
                        uiDocument.panelSettings = ps;
                        Debug.Log("[NexusController] ✅ Loaded PanelSettings from Resources.");
                    }
                }
                
                // Final: try loading the known asset
                if (uiDocument.panelSettings == null)
                {
                    Debug.LogError("[NexusController] 💀 Cannot find ANY PanelSettings. UI will NOT render.");
                }
            }

            // ═══ STEP 4: Force document reload via disable/enable cycle ═══
            if (uiDocument.rootVisualElement == null || uiDocument.rootVisualElement.childCount == 0)
            {
                Debug.Log("[NexusController] 🔄 Forcing UIDocument reload cycle...");
                var savedAsset = uiDocument.visualTreeAsset;
                var savedPanel = uiDocument.panelSettings;
                
                uiDocument.enabled = false;
                await UniTask.DelayFrame(2);
                
                // Re-assign to ensure they're fresh
                uiDocument.visualTreeAsset = savedAsset;
                uiDocument.panelSettings = savedPanel;
                uiDocument.enabled = true;
                
                await UniTask.DelayFrame(3);
                Debug.Log($"[NexusController] 🔄 After reload: children = {(uiDocument.rootVisualElement != null ? uiDocument.rootVisualElement.childCount : -1)}");
            }

            // ═══ STEP 5: Wait for ScreenRoot with extended patience ═══
            int retry = 0;
            while (retry < 30)
            {
                if (uiDocument.rootVisualElement != null && uiDocument.rootVisualElement.childCount > 0)
                {
                    if (uiDocument.rootVisualElement.Q("ScreenRoot") != null) break;
                }
                
                await UniTask.Delay(TimeSpan.FromSeconds(0.15f));
                retry++;
            }

            // ═══ STEP 6: Final verification ═══
            var screenRoot = uiDocument.rootVisualElement?.Q("ScreenRoot");
            if (screenRoot == null)
            {
                Debug.LogError($"[NexusController] 💀 FATAL: ScreenRoot not found after full recovery." +
                    $"\n  Final children: {(uiDocument.rootVisualElement != null ? uiDocument.rootVisualElement.childCount : -1)}" +
                    $"\n  PanelSettings: {(uiDocument.panelSettings != null ? uiDocument.panelSettings.name : "NULL")}" +
                    $"\n  Asset: {(uiDocument.visualTreeAsset != null ? uiDocument.visualTreeAsset.name : "NULL")}");

                if (uiDocument.rootVisualElement != null && uiDocument.rootVisualElement.childCount > 0)
                {
                    Debug.Log("[NexusController] 📂 Hierarchy Dump:");
                    foreach (var c in uiDocument.rootVisualElement.Children())
                        Debug.Log($"  - {c.name} [{c.GetType().Name}]");
                }
                return;
            }

            Debug.Log($"[NexusController] ✅ UI Awakened! ScreenRoot found with {screenRoot.childCount} children.");
            Debug.Log($"[NexusController] 🖥️ Successfully bound to: {uiDocument.name} (Asset: {uiDocument.visualTreeAsset.name})");

            // ═══ STEP 7: Wait for Injection (VContainer synchronization) ═══
            int injectRetry = 0;
            while (_localization == null && injectRetry < 20)
            {
                await UniTask.DelayFrame(1);
                injectRetry++;
            }

            if (_localization == null)
            {
                Debug.LogError("[NexusController] 💀 FATAL: Dependency Injection failed after 20 frames. Interface will be non-functional.");
                ShowFatalErrorOverlay("Помилка ініціалізації системи (DI Failed).\nБудь ласка, перезапустіть додаток.");
                
                // Restore visibility so the error is visible
                if (uiDocument != null && uiDocument.rootVisualElement != null)
                {
                    uiDocument.rootVisualElement.style.opacity = 1f;
                }
                return; // Stop further initialization
            }
            else
            {
                Debug.Log($"[NexusController] 💉 Dependencies solidified after {injectRetry} frames.");
            }

            InitializeWiring();
            UpdateLocalization();

            // Restore visibility now that everything is wired and localized
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                uiDocument.rootVisualElement.style.opacity = 1f;
            }
            
            if (_navigation != null)
            {
                _navigation.OnPanelSwitched += OnPanelSwitched;
            }

            if (!_isRitualExecuted)
            {
                await ExecuteIntroRitual();
                _isRitualExecuted = true;
            }
            
            // Check for Quick Start Destination first
            if (UnityEngine.PlayerPrefs.HasKey("QuickStart_Destination"))
            {
                string quickStartChoice = UnityEngine.PlayerPrefs.GetString("QuickStart_Destination");
                UnityEngine.PlayerPrefs.DeleteKey("QuickStart_Destination");
                UnityEngine.PlayerPrefs.Save();

                string destination = "radar";
                if (quickStartChoice == "AskOracle")
                {
                    destination = "sanctuary";
                }
                else if (quickStartChoice == "FindMaster")
                {
                    destination = "radar";
                }

                Debug.Log($"[Nexus] ⚡ Quick Start destination detected: {quickStartChoice} -> Navigating to {destination}");
                _navigation?.SwitchTo(destination);
            }
            else
            {
                // Task: Land on Radar if at least one pillar exists, else Aura for setup
                bool hasPillars = _authManager != null && _authManager.CurrentProfile != null && 
                                 (_authManager.CurrentProfile.AuraGifts.Count > 0 || _authManager.CurrentProfile.AuraSeeks.Count > 0);

                if (!hasPillars)
                {
                    Debug.Log("[Nexus] 🧭 Fresh Initiate detected. Navigating to Vault and opening Aura Rite.");
                    _navigation?.SwitchTo("vault");
                    if (_auraPanel != null)
                    {
                        _auraPanel.RemoveFromClassList("panel--hidden");
                        _auraPanel.style.display = DisplayStyle.Flex;
                        _auraPanel.style.visibility = Visibility.Visible;
                        _auraPanel.style.opacity = 1f;
                        _auraPanel.pickingMode = PickingMode.Position;
                        _auraPanel.BringToFront();
                    }
                }
                else if (_navigation != null && _resolver != null)
                {
                    var harmonyManager = _resolver.Resolve<TimeAura.Features.Harmony.HarmonyManager>();
                    var stateRestorer = new NexusStateRestorer(harmonyManager);
                    string entryPoint = stateRestorer.DetermineEntryPoint();
                    if (entryPoint == "radar" || entryPoint == "aura") entryPoint = "feed";
                    
                    Debug.Log($"[Nexus] 🧠 State Restorer determined entry point: {entryPoint}");
                    _navigation.SwitchTo(entryPoint);
                }
                else if (_navigation != null)
                {
                    _navigation.SwitchTo("feed"); // Fallback
                }
            }

            _audioService?.PlayMusic("TibetanBowls");
            
            // Start the Hidden Ping background loop
            _whisperManager?.StartWhisperLoop();

            // UX Audit #14: Try to show onboarding now that the scene has stabilized and entry point is loaded
            if (_onboarding != null)
            {
                _ = _onboarding.TryShowOnboardingAsync();
            }
        }

        private void OnDisable() {
            _langSubscription?.Dispose();
            _autonomousMatchSubscription?.Dispose();
        }
        private void OnDestroy()
        {
            _langSubscription?.Dispose();
            _autonomousMatchSubscription?.Dispose();
            if (_navigation != null) _navigation.OnPanelSwitched -= OnPanelSwitched;
            _whisperManager?.StopWhisperLoop();
        }

        private void InitializeWiring()
        {
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            Debug.Log($"[NexusController] 🛠️ WIRING DIAGNOSTICS:" +
                $"\n  Localization: {(_localization != null ? "OK" : "MISSING")}" +
                $"\n  Auth: {(_authManager != null ? "OK" : "MISSING")}" +
                $"\n  Data: {(_dataService != null ? "OK" : "MISSING")}" +
                $"\n  Gemini: {(_gemini != null ? "OK" : "MISSING")}" +
                $"\n  OracleService: {(_oracleService != null ? "OK" : "MISSING")}" +
                $"\n  AuraPresenter: {(_auraPresenter != null ? "OK" : "MISSING")}");

            foreach (var child in root.Children())
            {
                Debug.Log($" - Child: {child.name} (Class: {child.GetClasses().FirstOrDefault()})");
            }
            
            // --- Inject StyleSheets from Inspector or Resources ---
            if (_nexusStyleSheets == null || _nexusStyleSheets.Count == 0)
            {
                // Fallback to Resources
                var loadedSheets = Resources.LoadAll<StyleSheet>("UI/Styles/Nexus");
                if (loadedSheets != null && loadedSheets.Length > 0)
                {
                    Debug.Log($"[NexusController] 📦 Auto-loaded {loadedSheets.Length} StyleSheets from Resources/UI/Styles/Nexus");
                    foreach (var sheet in loadedSheets)
                    {
                        if (sheet != null && !root.styleSheets.Contains(sheet))
                            root.styleSheets.Add(sheet);
                    }
                }
            }
            else
            {
                foreach (var sheet in _nexusStyleSheets)
                {
                    if (sheet != null && !root.styleSheets.Contains(sheet))
                    {
                        root.styleSheets.Add(sheet);
                    }
                }
            }
            
            // Link to Oracle Eye
            if (OracleWidgetController.Instance != null)
            {
                Debug.Log("[NexusController] 👁️ Linking to Global OracleWidgetController...");
                OracleWidgetController.Instance.Refresh();
            }

            _screenRoot = root.Q("ScreenRoot");
            _panelVault = root.Q("VaultPanel");
            _panelFeed = root.Q("FeedPanel");
            _discoveryPanel = root.Q("DiscoveryPanel");
            _discoveryFeed = root.Q<ScrollView>("DiscoveryFeed");
            _auraPanel = root.Q("AuraPanel");
            _settingsPanel = root.Q("SettingsPanel");
            _welcomeRite = root.Q("WelcomeRite");
            _lblCurrentLangHUD = root.Q<Label>("LblCurrentLangHUD");

            // --- Sub-Controller Initialization ---
            _radar?.Initialize(root);
            if (_radar != null) 
            {
                _radar.OnSearchCompleted += OnSearchCompleted;
            }

            var panels = new List<VisualElement> { _panelVault, _panelFeed, _settingsPanel, _discoveryPanel };
            var chamberPanel = _screenRoot?.Q("ChamberPanel");
            if (chamberPanel != null) panels.Add(chamberPanel);
            
            var sanctuaryPanel = _screenRoot?.Q("SanctuaryPanel");
            if (sanctuaryPanel != null) panels.Add(sanctuaryPanel);

            // ── Orders of Chronos Panel ──
            var ordersPanel = _screenRoot?.Q("OrdersPanel");
            if (ordersPanel == null && _screenRoot != null)
            {
                ordersPanel = new VisualElement { name = "OrdersPanel" };
                ordersPanel.style.display = DisplayStyle.None;
                ordersPanel.style.flexGrow = 1;
                ordersPanel.style.backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.1f, 1f));
                
                var title = new Label("⚔️ ОРДЕНИ ХРОНОСУ");
                title.style.color = new StyleColor(new Color(0.83f, 0.69f, 0.22f));
                title.style.fontSize = 24;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                title.style.marginTop = 50;
                ordersPanel.Add(title);

                var desc = new Label("Шукайте однодумців, створюйте спільні Ескроу-рахунки та долучайтесь до Великої Мети.");
                desc.style.color = Color.white;
                desc.style.whiteSpace = WhiteSpace.Normal;
                desc.style.unityTextAlign = TextAnchor.MiddleCenter;
                desc.style.marginTop = 20;
                desc.style.paddingLeft = 20;
                desc.style.paddingRight = 20;
                ordersPanel.Add(desc);
                
                var btnJoin = new Button { text = "СТВОРИТИ АБО ЗНАЙТИ ОРДЕН" };
                btnJoin.style.marginTop = 40;
                btnJoin.style.height = 50;
                btnJoin.style.backgroundColor = new StyleColor(new Color(0.83f, 0.69f, 0.22f));
                btnJoin.style.color = Color.black;
                btnJoin.style.unityFontStyleAndWeight = FontStyle.Bold;
                ordersPanel.Add(btnJoin);
                _screenRoot.Add(ordersPanel);
            }
            if (ordersPanel != null) panels.Add(ordersPanel);

            // Add a floating AI button to ScreenRoot so it's always visible
            if (_screenRoot != null && _screenRoot.Q("BtnFloatingAi") == null)
            {
                var btnFloatingAI = new Button { text = "🤖 AI", name = "BtnFloatingAi" };
                btnFloatingAI.style.position = Position.Absolute;
                btnFloatingAI.style.right = 20;
                btnFloatingAI.style.bottom = 120;
                btnFloatingAI.style.width = 60;
                btnFloatingAI.style.height = 60;
                btnFloatingAI.style.borderTopLeftRadius = 30;
                btnFloatingAI.style.borderTopRightRadius = 30;
                btnFloatingAI.style.borderBottomLeftRadius = 30;
                btnFloatingAI.style.borderBottomRightRadius = 30;
                btnFloatingAI.style.backgroundColor = new StyleColor(new Color(0.85f, 0.1f, 0.85f));
                btnFloatingAI.style.color = Color.white;
                btnFloatingAI.style.unityFontStyleAndWeight = FontStyle.Bold;
                btnFloatingAI.style.fontSize = 20;
                btnFloatingAI.clicked += () => 
                {
                    Debug.Log("[Nexus] Opening AI Masters Guild...");
                    var aiMasters = TimeAura.Features.Data.AIMasterFactory.GetAIMasters();
                    if (_masterDossier != null && aiMasters.Count > 0)
                    {
                        // Cycle through them or show a random one?
                        int idx = UnityEngine.Random.Range(0, aiMasters.Count);
                        _masterDossier.Show(aiMasters[idx]);
                    }
                };
                _screenRoot.Add(btnFloatingAI);
            }

            _navigation?.Initialize(root, panels);
            if (_navigation != null) _navigation.OnPanelSwitched += OnPanelSwitched;

            if (_chamber != null)
            {
                _resolver?.Inject(_chamber);
                _chamber.Initialize(root, _navigation);
                _chamber.OnSessionEntered -= OnChamberSessionEntered;
                _chamber.OnSessionEntered += OnChamberSessionEntered;
            }

            _settings?.Initialize(root);
            if (_settings != null) _settings.OnClose += () => _navigation?.ReturnToPrevious();
            
            _content?.Initialize(root);
            if (_content != null) _content.OnProfileAligned += target =>
            {
                _hapticService?.MediumTap();
                Debug.Log($"[Nexus] Aligned with profile: {target.Nickname}. Opening Symmetry Rite.");

                // Show Master Dossier first
                _masterDossier?.Show(target);

                // Pre-initialize Harmony Channel once
                if (_harmonyChannel == null)
                {
                    var harmonyRoot = root.Q("HarmonyPanel");
                    if (harmonyRoot != null)
                    {
                        _harmonyChannel = new HarmonyChannelController(
                            harmonyRoot,
                            _authManager,
                            _dataService,
                            _localization,
                            _economy,
                            _media,
                            _oracleService,
                            _audioService);

                        // BtnSealPact inside HarmonyPanel fires this —
                        // route through Scales of Chronos, then EscrowModal
                        _harmonyChannel.OnInitiateRiteRequested += (partner, chatHistory) =>
                        {
                            Debug.Log($"[Nexus] ⚖️ Seal Pact requested for {partner.DisplayName}. Opening Scales of Chronos.");
                            _pendingChatHistory = chatHistory;
                            _activePartner = partner;
                            if (_horasEvaluator != null)
                            {
                                _horasEvaluator.Show(partner, _authManager.CurrentProfile, chatHistory);
                            }
                            else
                            {
                                OpenEscrowModal(root, -1, -1);
                            }
                        };
                    }
                }
            };

            var dossierEl = root.Q("MasterDossierPanel");
            Debug.Log($"[NexusController] 📂 MasterDossierPanel element found: {dossierEl != null}");
            if (dossierEl != null)
            {
                _masterDossier = new MasterDossierController(dossierEl, _localization, _audioService, _hapticService, _oracleService, _authManager);
                
                _masterDossier.OnDossierClosed += OnSymmetryDeclined;


                _masterDossier.OnQuickChatRequested += (targetUser) =>
                {
                    if (_hapticService != null) _hapticService.MediumTap();
                    _audioService?.PlaySFX("MessageSent2");

                    // Lazily create HarmonyChannel if not yet initialized
                    // (e.g. user came straight to Dossier without going through Radar Aligned flow)
                    if (_harmonyChannel == null)
                    {
                        var harmonyRoot = root.Q("HarmonyPanel");
                        if (harmonyRoot != null)
                        {
                            _harmonyChannel = new HarmonyChannelController(
                                harmonyRoot, _authManager, _dataService, _localization,
                                _economy, _media, _oracleService, _audioService);

                            _harmonyChannel.OnInitiateRiteRequested += (partner, chatHistory) =>
                            {
                                Debug.Log($"[Nexus] ⚖️ Seal Pact requested for {partner.DisplayName}. Opening Scales of Chronos.");
                                _pendingChatHistory = chatHistory;
                                _activePartner = partner;
                                if (_horasEvaluator != null)
                                    _horasEvaluator.Show(partner, _authManager.CurrentProfile, chatHistory);
                                else
                                    OpenEscrowModal(root, -1, -1);
                            };

                            _harmonyChannel.OnClosed += () => 
                            {
                                Debug.Log("[Nexus] Harmony Channel closed. Switching to Feed.");
                                _navigation?.SwitchTo("feed");
                            };
                        }
                    }

                    var session = new HarmonySession(_authManager.CurrentProfile.UserId, targetUser.UserId, 0);
                    session.sessionId = "demo_session";

                    _harmonyChannel?.Open(session, new TimeAura.Features.Harmony.HarmonyContext(targetUser));
                    _navigation?.SwitchTo("harmony");
                };

            }

            var riteEl = root.Q("SymmetryRiteModal");
            Debug.Log($"[NexusController] ⚡ SymmetryRiteModal element found: {riteEl != null}");
            if (riteEl != null)
            {
                _symmetryRite = new SymmetryRiteController(riteEl, _audioService, _hapticService, _localization);
                _symmetryRite.OnHarmonyAchieved += OnHarmonyAchieved;
                _symmetryRite.OnSymmetryDeclined += OnSymmetryDeclined;
            }

            // ── Ваги Хроноса — Horas Evaluator ────────────────────────
            var scalesEl = root.Q("HorasScalesPanel");
            Debug.Log($"[NexusController] ⚖️ HorasScalesPanel element found: {scalesEl != null}");
            if (scalesEl != null)
            {
                _horasEvaluator = new OracleHorasEvaluatorController(
                    scalesEl, _oracleService, _audioService, _hapticService);

                _horasEvaluator.OnHorasAccepted += (horas, minutes) =>
                {
                    // Open EscrowModal, pre-filling with Oracle's verdict (or empty for manual)
                    OpenEscrowModal(root, horas, minutes);
                };

                _horasEvaluator.OnCancelled += () =>
                {
                    Debug.Log("[NexusController] ⚖️ Horas evaluation cancelled.");
                    _radar?.Resume();
                };
            }

            _menu?.Initialize(root);
            _vault?.Initialize(root, _authManager, _localization, _media, _audioService, _dataService, _walletService);
            
            var supportUi = GetComponent<SupportUIController>();
            if (supportUi != null) supportUi.Initialize(root);

            var sanctuaryUi = GetComponent<SanctuaryUIController>();
            if (sanctuaryUi != null)
            {
                _resolver?.Inject(sanctuaryUi);
                sanctuaryUi.Initialize(root);
                sanctuaryUi.OnExitRequested += () => _navigation?.SwitchTo("feed");
            }

            var shopUi = GetComponent<MysticalShopController>();
            if (shopUi != null)
            {
                _resolver?.Inject(shopUi);
                shopUi.Initialize(root);
            }

            if (_feed == null) _feed = GetComponentInChildren<ConvergenceFeedController>();
            if (_feed == null) _feed = gameObject.AddComponent<ConvergenceFeedController>();
            
            _resolver?.Inject(_feed);
            if (_feed != null)
            {
                _feed.Initialize(root, _fateCardTemplate);

                // Wire feed card actions → Dossier and Sanctuary
                _feed.OnOpenDossierForUser = (userId) =>
                {
                    if (string.IsNullOrEmpty(userId)) return;
                    ShowDossierForUserIdAsync(userId).Forget();
                };
                
                _feed.OnOpenChatForPost = (post) =>
                {
                    if (post == null || string.IsNullOrEmpty(post.userId)) return;
                    OpenChatForPostAsync(post).Forget();
                };
                
                _feed.OnOpenSanctuary = () => _navigation?.SwitchTo("sanctuary");
            }

            var geminiChat = GetComponent<GeminiChatController>();
            // GeminiChat will be initialized by OracleWidgetController, 
            // but we ensure it's present here.

            if (_oracleWidget != null)
            {
                _oracleWidget.gameObject.SetActive(true);
                _oracleWidget.SetDocument(uiDocument);
                _oracleWidget.Refresh();
            }

            var btnMenuSupport = root.Q<Button>("BtnMenuSupport");
            if (btnMenuSupport != null) btnMenuSupport.clicked += () => 
            {
                _menu?.ToggleMenu(false);
                supportUi?.Show();
            };



            var btnMenuSanctuary = root.Q<Button>("BtnMenuSanctuary");
            if (btnMenuSanctuary != null) btnMenuSanctuary.clicked += () =>
            {
                _menu?.ToggleMenu(false);
                _navigation?.SwitchTo("sanctuary");
            };

            var btnProfileSupport = root.Q<Button>("BtnProfileSupport");
            if (btnProfileSupport != null) 
            {
                btnProfileSupport.clicked += () => 
                {
                    if (supportUi != null && root.Q("SupportModal") != null) 
                        supportUi.Show();
                    else 
                        _uiManager?.ShowErrorPopup("В РОЗРОБЦІ", "Портал пожертв розробникам ще будується. Дякуємо за вашу підтримку!");
                };
            }

            _menu?.SetActive(true);
            if (_menu != null) _menu.OnMenuItemSelected += id => _navigation?.SwitchTo(id);

            _navigation?.SetActive(true);
            _settings?.SetActive(true);
            _content?.SetActive(true);

            if (_auraPanel != null && _auraPresenter != null)
            {
                _auraView = new AuraView(_auraPanel, _auraPresenter, _localization, _oraclePromptFactory, _oracles, _audioService, _hapticService, _whisperManager);
                _auraPresenter.InitializeFromProfile(_authManager?.CurrentProfile);
                _auraPresenter.OnAuraColorsUpdated += (colors) => { _vfx?.UpdateAuraColors(colors); UpdateDynamicUiAndSound(colors); };
                
                _auraView.OnLaunchResonance += () =>
                {
                    _radar?.StartRadarSearch();
                };
                
                // 🎭 Sync Tone on Start
                if (_authManager != null && _authManager.CurrentProfile != null)
                    _localization?.SetTone(_authManager.CurrentProfile.OracleTone);
            }
            else if (_auraPanel != null)
            {
                Debug.LogWarning("[NexusController] ⚠️ Cannot initialize AuraView: AuraPresenter is NULL.");
            }

            // Subscribe to localization
            _langSubscription = EventBus.Subscribe<LanguageChangedEvent>(e => UpdateLocalization());
            _autonomousMatchSubscription = EventBus.Subscribe<TimeAura.Core.AutonomousMatchFoundEvent>(OnAutonomousMatchFound);
            UpdateLocalization();

            // UX Audit #14: Start onboarding for new users
            if (_onboarding == null)
            {
                _onboarding = gameObject.AddComponent<OnboardingController>();
                _resolver?.Inject(_onboarding);
            }
        }

        private void OnChamberSessionEntered(TimeAura.Features.Data.UserProfile partner, TimeAura.Features.Social.Post post)
        {
            OpenChatForPostAsync(post).Forget();
        }

        private async UniTaskVoid OpenChatForPostAsync(TimeAura.Features.Social.Post post)
        {
            if (_hapticService != null) _hapticService.MediumTap();
            _audioService?.PlaySFX("MessageSent2");

            var targetUser = await _dataService.GetUserProfileAsync(post.userId, default);
            if (targetUser == null) 
            {
                Debug.LogWarning($"[Nexus] User {post.userId} not found in DB. Generating mock profile to allow chat.");
                targetUser = new TimeAura.Features.Data.UserProfile(post.userId, "", post.username ?? "Unknown Adept", 0f, 0);
            }

            if (_harmonyChannel == null)
            {
                var harmonyRoot = uiDocument.rootVisualElement.Q("HarmonyPanel");
                if (harmonyRoot == null)
                {
                    var harmonyAsset = UnityEngine.Resources.Load<UnityEngine.UIElements.VisualTreeAsset>("UI/Harmony/HarmonyChannel");
                    if (harmonyAsset != null)
                    {
                        harmonyRoot = harmonyAsset.Instantiate();
                        harmonyRoot.name = "HarmonyPanel";
                        harmonyRoot.AddToClassList("panel");
                        harmonyRoot.AddToClassList("panel--hidden");
                        harmonyRoot.style.position = UnityEngine.UIElements.Position.Absolute;
                        harmonyRoot.style.width = UnityEngine.UIElements.Length.Percent(100);
                        harmonyRoot.style.height = UnityEngine.UIElements.Length.Percent(100);
                        
                        var panelContent = uiDocument.rootVisualElement.Q("PanelContent");
                        if (panelContent != null)
                        {
                            panelContent.Add(harmonyRoot);
                        }
                        else 
                        {
                            uiDocument.rootVisualElement.Add(harmonyRoot);
                        }
                        Debug.Log("[Nexus] Dynamically instantiated HarmonyChannel UI.");
                    }
                    else
                    {
                        Debug.LogError("[Nexus] Failed to load HarmonyChannel UXML from Resources!");
                    }
                }

                if (harmonyRoot != null)
                {
                    _harmonyChannel = new HarmonyChannelController(
                        harmonyRoot, _authManager, _dataService, _localization,
                        _economy, _media, _oracleService, _audioService);

                    _harmonyChannel.OnInitiateRiteRequested += (partner, chatHistory) =>
                    {
                        Debug.Log($"[Nexus] ⚖️ Seal Pact requested for {partner.DisplayName}. Opening Scales of Chronos.");
                        _pendingChatHistory = chatHistory;
                        _activePartner = partner;
                        if (_horasEvaluator != null)
                            _horasEvaluator.Show(partner, _authManager.CurrentProfile, chatHistory);
                        else
                            OpenEscrowModal(uiDocument.rootVisualElement, -1, -1);
                    };

                    _harmonyChannel.OnClosed += () => 
                    {
                        Debug.Log("[Nexus] Harmony Channel closed. Switching to Feed.");
                        _navigation?.SwitchTo("feed");
                    };
                }
            }

            var session = new HarmonySession(_authManager.CurrentProfile.UserId, targetUser.UserId, 0);
            session.sessionId = "demo_session_" + post.postId;

            var context = new TimeAura.Features.Harmony.HarmonyContext(targetUser, post);
            _harmonyChannel?.Open(session, context);
            _navigation?.SwitchTo("harmony");
        }

        private void OnAutonomousMatchFound(TimeAura.Core.AutonomousMatchFoundEvent evt)
        {
            Debug.Log("[NexusController] 🔮 Received Autonomous Match! Showing prophecy...");
            _hapticService?.MediumTap();
            ShowAutonomousMatchModal(evt);
        }

        private void ShowAutonomousMatchModal(TimeAura.Core.AutonomousMatchFoundEvent evt)
        {
            var root = uiDocument.rootVisualElement;
            if (root == null) return;

            var existing = root.Q("AutonomousMatchModal");
            if (existing != null) existing.RemoveFromHierarchy();

            var modal = new VisualElement { name = "AutonomousMatchModal" };
            modal.style.position = Position.Absolute;
            modal.style.left = 0; modal.style.right = 0; modal.style.top = 0; modal.style.bottom = 0;
            modal.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.85f));
            modal.style.justifyContent = Justify.Center;
            modal.style.alignItems = Align.Center;

            var container = new VisualElement();
            container.style.backgroundColor = new StyleColor(new Color(0.08f, 0.04f, 0.12f, 0.98f));
            container.style.paddingLeft = 20; container.style.paddingRight = 20; container.style.paddingTop = 30; container.style.paddingBottom = 30;
            container.style.borderTopLeftRadius = 24; container.style.borderTopRightRadius = 24; container.style.borderBottomLeftRadius = 24; container.style.borderBottomRightRadius = 24;
            container.style.borderTopColor = new StyleColor(new Color(0.85f, 0.65f, 0.15f)); // Luxurious Golden Aura
            container.style.borderBottomColor = new StyleColor(new Color(0.85f, 0.65f, 0.15f));
            container.style.borderLeftColor = new StyleColor(new Color(0.85f, 0.65f, 0.15f));
            container.style.borderRightColor = new StyleColor(new Color(0.85f, 0.65f, 0.15f));
            container.style.borderTopWidth = 2; container.style.borderBottomWidth = 2; container.style.borderLeftWidth = 2; container.style.borderRightWidth = 2;
            container.style.width = Length.Percent(90);

            var title = new Label("🔮 КОСМІЧНЕ КІЛЬЦЕ СИМЕТРІЇ");
            title.style.color = new StyleColor(new Color(1f, 0.84f, 0f));
            title.style.fontSize = 20;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;

            // Triple-Agent Circle Container
            var ringContainer = new VisualElement();
            ringContainer.style.flexDirection = FlexDirection.Row;
            ringContainer.style.justifyContent = Justify.SpaceAround;
            ringContainer.style.alignItems = Align.Center;
            ringContainer.style.marginBottom = 25;
            ringContainer.style.paddingTop = 15;
            ringContainer.style.paddingBottom = 15;

            // Resolve Agent Info or use fallbacks
            string avatarA = !string.IsNullOrEmpty(evt.UserAAvatar) ? evt.UserAAvatar : "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=120";
            string avatarB = !string.IsNullOrEmpty(evt.UserBAvatar) ? evt.UserBAvatar : "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=120";
            string avatarC = !string.IsNullOrEmpty(evt.UserCAvatar) ? evt.UserCAvatar : "https://images.unsplash.com/photo-1614850523459-c2f4c699c52e?w=120";

            string nameA = !string.IsNullOrEmpty(evt.UserANickname) ? evt.UserANickname : "Ти";
            string nameB = !string.IsNullOrEmpty(evt.UserBNickname) ? evt.UserBNickname : "Моделлер";
            string nameC = !string.IsNullOrEmpty(evt.UserCNickname) ? evt.UserCNickname : "Садівник";

            string roleA = !string.IsNullOrEmpty(evt.RoleA) ? evt.RoleA : "Скрипт";
            string roleB = !string.IsNullOrEmpty(evt.RoleB) ? evt.RoleB : "Концепт";
            string roleC = !string.IsNullOrEmpty(evt.RoleC) ? evt.RoleC : "Газон";

            var nodeA = CreateVisageNode(nameA, avatarA, roleA);
            var nodeB = CreateVisageNode(nameB, avatarB, roleB);
            var nodeC = CreateVisageNode(nameC, avatarC, roleC);

            // Connectors
            var arrow1 = new Label("➔");
            arrow1.style.color = new StyleColor(new Color(1f, 0.84f, 0f, 0.6f));
            arrow1.style.fontSize = 24;
            arrow1.style.unityFontStyleAndWeight = FontStyle.Bold;

            var arrow2 = new Label("➔");
            arrow2.style.color = new StyleColor(new Color(1f, 0.84f, 0f, 0.6f));
            arrow2.style.fontSize = 24;
            arrow2.style.unityFontStyleAndWeight = FontStyle.Bold;

            ringContainer.Add(nodeA);
            ringContainer.Add(arrow1);
            ringContainer.Add(nodeB);
            ringContainer.Add(arrow2);
            ringContainer.Add(nodeC);

            // Circular closing arrow label to make it explicitly closed-loop
            var returnPathLabel = new Label(_localization?.Get("term.slide_to_harmony", "✦ Кільце Симетрії замикається назад на тебе ✦") ?? "✦ Кільце Симетрії замикається назад на тебе ✦");
            returnPathLabel.style.color = new StyleColor(new Color(0.8f, 0.7f, 1f));
            returnPathLabel.style.fontSize = 12;
            returnPathLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            returnPathLabel.style.marginBottom = 15;
            returnPathLabel.style.unityFontStyleAndWeight = FontStyle.Italic;

            var lblDesc = new Label(evt.MatchDescription);
            lblDesc.style.color = new StyleColor(Color.white);
            lblDesc.style.fontSize = 15;
            lblDesc.style.whiteSpace = WhiteSpace.Normal;
            lblDesc.style.marginBottom = 15;
            lblDesc.style.unityTextAlign = TextAnchor.MiddleCenter;

            var lblOracle = new Label($"«{evt.OracleMessage}»");
            lblOracle.style.color = new StyleColor(new Color(0.6f, 0.8f, 1f));
            lblOracle.style.fontSize = 14;
            lblOracle.style.whiteSpace = WhiteSpace.Normal;
            lblOracle.style.unityFontStyleAndWeight = FontStyle.Italic;
            lblOracle.style.marginBottom = 20;
            lblOracle.style.unityTextAlign = TextAnchor.MiddleCenter;

            // Pulse micro-animation using root.schedule
            var pulseTime = 0f;
            var pulseReg = root.schedule.Execute(() =>
            {
                pulseTime += 0.1f;
                float pulse = 0.5f + Mathf.Sin(pulseTime * 2.5f) * 0.5f;
                var pulseColor = new Color(1f, 0.84f, 0f, 0.3f + pulse * 0.7f);
                arrow1.style.color = new StyleColor(pulseColor);
                arrow2.style.color = new StyleColor(pulseColor);
                title.style.color = new StyleColor(pulseColor);
            }).Every(80);

            var btnClose = new Button();
            string btnManifestText = _localization?.Get(AuraTerms.MSG_SEEKING_CONVERGENCE, "МАНІФЕСТУВАТИ СОЮЗ") ?? "МАНІФЕСТУВАТИ СОЮЗ";
            btnClose.text = _localization != null
                ? _localization.GetPersonaString(AuraTerms.MSG_SEEKING_CONVERGENCE, _authManager?.CurrentProfile?.OracleTone ?? OracleTone.Business, "МАНІФЕСТУВАТИ СОЮЗ").ToUpper()
                : "МАНІФЕСТУВАТИ СОЮЗ";
            btnClose.style.backgroundColor = new StyleColor(new Color(0.45f, 0.1f, 0.65f));
            btnClose.style.color = new StyleColor(Color.white);
            btnClose.style.paddingTop = 12; btnClose.style.paddingBottom = 12;
            btnClose.style.borderTopLeftRadius = 12; btnClose.style.borderTopRightRadius = 12; btnClose.style.borderBottomLeftRadius = 12; btnClose.style.borderBottomRightRadius = 12;
            btnClose.style.fontSize = 14;
            btnClose.style.unityFontStyleAndWeight = FontStyle.Bold;

            btnClose.clicked += () =>
            {
                _hapticService?.MediumTap();
                _audioService?.PlaySFX("RitualSeal");
                pulseReg.Pause();
                modal.RemoveFromHierarchy();
            };

            container.Add(title);
            container.Add(ringContainer);
            container.Add(returnPathLabel);
            container.Add(lblDesc);
            container.Add(lblOracle);
            container.Add(btnClose);
            modal.Add(container);

            root.Add(modal);

            _audioService?.PlaySFX("AuraResonance");
        }

        private VisualElement CreateVisageNode(string nickname, string avatarUrl, string roleText)
        {
            var node = new VisualElement();
            node.style.alignItems = Align.Center;

            var avatarFrame = new VisualElement();
            avatarFrame.style.width = 64;
            avatarFrame.style.height = 64;
            avatarFrame.style.borderTopLeftRadius = 32;
            avatarFrame.style.borderTopRightRadius = 32;
            avatarFrame.style.borderBottomLeftRadius = 32;
            avatarFrame.style.borderBottomRightRadius = 32;
            avatarFrame.style.borderTopWidth = 2;
            avatarFrame.style.borderBottomWidth = 2;
            avatarFrame.style.borderLeftWidth = 2;
            avatarFrame.style.borderRightWidth = 2;
            avatarFrame.style.borderTopColor = new StyleColor(new Color(1f, 0.84f, 0f));
            avatarFrame.style.borderBottomColor = new StyleColor(new Color(1f, 0.84f, 0f));
            avatarFrame.style.borderLeftColor = new StyleColor(new Color(1f, 0.84f, 0f));
            avatarFrame.style.borderRightColor = new StyleColor(new Color(1f, 0.84f, 0f));
            avatarFrame.style.overflow = Overflow.Hidden;
            avatarFrame.style.marginBottom = 8;

            var img = new Image();
            img.style.width = Length.Percent(100);
            img.style.height = Length.Percent(100);
            
            LoadRemoteImage(img, avatarUrl).Forget();
            avatarFrame.Add(img);

            var lblName = new Label(nickname);
            lblName.style.color = new StyleColor(Color.white);
            lblName.style.fontSize = 12;
            lblName.style.unityFontStyleAndWeight = FontStyle.Bold;
            lblName.style.marginBottom = 2;

            var lblRole = new Label(roleText);
            lblRole.style.color = new StyleColor(new Color(0.85f, 0.65f, 1f));
            lblRole.style.fontSize = 10;

            node.Add(avatarFrame);
            node.Add(lblName);
            node.Add(lblRole);

            return node;
        }

        private async UniTaskVoid LoadRemoteImage(Image imageElement, string url)
        {
            try
            {
                using (var webRequest = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
                {
                    await webRequest.SendWebRequest();
                    if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(webRequest);
                        if (texture != null && imageElement != null)
                        {
                            imageElement.image = texture;
                        }
                    }
                }
            }
            catch
            {
                // Ignore fallback
            }
        }

        private void UpdateLocalization()
        {
            if (_localization == null) return;

            Debug.Log("[Nexus] 🌐 Syncing Divine Tongues...");
            
            _radar?.UpdateLocalization();
            _navigation?.UpdateLocalization();
            _settings?.UpdateLocalization();
            _content?.UpdateLocalization();
            _feed?.UpdateLocalization();
            _menu?.UpdateLocalization();
            _vault?.UpdateLocalization();
            _auraView?.UpdateLocalization();
            _masterDossier?.UpdateLocalization();
            GetComponent<SupportUIController>()?.UpdateLocalization();
            TimeAura.Features.UI.Oracle.SanctuaryUIController.Instance?.UpdateLocalization();

            if (_lblCurrentLangHUD != null)
            {
                string code = _localization.CurrentLanguage switch
                {
                    SystemLanguage.Ukrainian => "UA",
                    SystemLanguage.English => "EN",
                    SystemLanguage.Spanish => "ES",
                    SystemLanguage.French => "FR",
                    SystemLanguage.German => "DE",
                    SystemLanguage.Italian => "IT",
                    SystemLanguage.Polish => "PL",
                    SystemLanguage.Russian => "RU",
                    SystemLanguage.Turkish => "TR",
                    SystemLanguage.Hindi => "HI",
                    _ => "EN"
                };
                _lblCurrentLangHUD.text = code;
            }

            var btnProfile = uiDocument?.rootVisualElement?.Q<Button>("BtnOpenProfile");
            if (btnProfile != null)
            {
                btnProfile.text = _localization.GetPersonaString("nav_me", _authManager?.CurrentProfile?.OracleTone ?? OracleTone.Business, "ME").ToUpper();
            }
        }

        private async UniTask ExecuteIntroRitual()
        {
            if (_welcomeRite == null) return;
            
            var lblWelcome = _welcomeRite.Q<Label>("LblWelcomeText");
            if (lblWelcome != null && _oracleService != null && _authManager?.CurrentProfile != null)
            {
                var profile = _authManager.CurrentProfile;
                string name = !string.IsNullOrEmpty(profile.DisplayName) ? profile.DisplayName : "Seeker";
                string location = !string.IsNullOrEmpty(profile.LocationZone) ? profile.LocationZone : "";
                
                var prompts = TimeAura.Core.Data.SO.OracleCorePromptsSO.GetInstance();
                string template = prompts != null ? prompts.SanctuaryWelcomePromptTemplate : "Привітай Майстра {0} у Нексусі. {1}Зроби це містично, максимум 20 слів.";
                
                string locationContext = !string.IsNullOrEmpty(location) ? $"Згадай його місцезнаходження {location}, якщо воно є в профілі. " : "";
                string prompt = "";
                try
                {
                    prompt = string.Format(template, name, locationContext);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NexusController] Welcome prompt formatting failed: {ex.Message}");
                    prompt = $"Привітай Майстра {name} у Нексусі. {locationContext}Зроби це містично, максимум 20 слів.";
                }

                string guidance = await _oracleService.GetPhilosophicalGuidanceAsync(prompt, "NexusWelcome");
                lblWelcome.text = guidance.ToUpper();
            }

            _welcomeRite.style.display = DisplayStyle.Flex;
            _welcomeRite.style.opacity = 0;
            
            float elapsed = 0;
            while (elapsed < 1.0f)
            {
                elapsed += Time.deltaTime;
                _welcomeRite.style.opacity = elapsed;
                await UniTask.Yield();
            }

            await UniTask.Delay(3000); // Increased delay for reading AI text
            
            while (elapsed > 0)
            {
                elapsed -= Time.deltaTime;
                _welcomeRite.style.opacity = elapsed;
                await UniTask.Yield();
            }
            _welcomeRite.style.display = DisplayStyle.None;
        }

        private void OnPanelSwitched(string panelId)
        {
            _hapticService?.LightTap();

            if (panelId != "harmony" && _harmonyChannel != null && _harmonyChannel.IsVisible)
            {
                _harmonyChannel.HideSilently();
            }
            
            // Manage logic lifecycle based on panel
            if (panelId == "feed" || panelId == "discovery")
            {
                if (_content != null) _content.Refresh();
                if (panelId == "feed" && _feed != null) _feed.RefreshAsync().Forget();
            }

            // Sync Oracle Context
            UIContext context = panelId switch
            {
                "radar" => UIContext.Radar,
                "vault" => UIContext.Vault,
                "aura" => UIContext.Aura,
                "harmony" => UIContext.Harmony,
                "discovery" => UIContext.Radar,
                "sanctuary" => UIContext.Sanctuary,
                _ => UIContext.Nexus
            };
            OracleContextManager.SetContext(context);
            
            if (_content != null) _content.SetActive(panelId == "feed" || panelId == "vault" || panelId == "discovery");
            if (panelId == "vault" && _vault != null) _vault.RefreshUI();
            
            if (panelId == "radar") 
            {
                if (_radar != null) _radar.Resume();
            }
            else 
            {
                if (_radar != null) _radar.Pause();
            }
        }

        private void OnSearchCompleted(List<UserProfile> matches)
        {
            _hapticService?.MediumTap();
            Debug.Log($"[Nexus] Radar search completed with {matches.Count} matches. Switching to Feed.");
            
            // Switch to Feed Panel (Discovery grid of Masters)
            _navigation?.SwitchTo("feed");
            
            // Task 3: Display results in Discovery Feed
            _content?.DisplaySymmetryResults(matches);
        }

        private void OnSymmetryDeclined()
        {
            _radar?.Resume();
        }

        /// <summary>Load a user profile by userId and show the Master Dossier overlay.</summary>
        private async UniTaskVoid ShowDossierForUserIdAsync(string userId)
        {
            if (_masterDossier == null) return;
            try
            {
                // Try to load via SocialManager
                var socialManager = GetComponent<TimeAura.Features.Social.SocialManager>();
                if (socialManager == null)
                    socialManager = FindAnyObjectByType<TimeAura.Features.Social.SocialManager>();

                UserProfile profile = null;
                if (socialManager != null)
                    profile = await socialManager.GetUserProfileAsync(userId);

                if (profile == null)
                {
                    // Fallback: create a stub so dossier still opens
                    profile = new UserProfile(userId, "", "Адепт", 0f, 0);
                }

                _masterDossier.Show(profile);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Nexus] ShowDossierForUserIdAsync failed: {ex.Message}");
            }
        }

        private void OnHarmonyAchieved()
        {
            if (_symmetryRite?.TargetUser == null) return;

            Debug.Log($"[Nexus] Harmony Achieved! Finalizing contract for {_pendingFinalEscrowAmount} minutes...");

            var whisper = _resolver?.Resolve<TimeAura.Features.UI.Oracle.OracleWhisperManager>();
            whisper?.ShowWhisper("Контракт скріплено печаткою!", TimeAura.Features.UI.Oracle.WhisperColor.Cyan);

            _harmonyChannel?.ConfirmHarmonySeal(_pendingFinalEscrowAmount).Forget();
            _navigation?.SwitchTo("harmony");
        }

        private void OpenEscrowModal(VisualElement root, int preFillHoras, int preFillMinutes)
        {
            // ── Show EscrowModal overlay ───────────────────────────────────
            var escrowEl = root?.Q("EscrowModal");
            if (escrowEl == null)
            {
                Debug.LogWarning("[NexusController] ⚠️ EscrowModal not found — executing final seal directly.");
                int totalMins = preFillHoras > 0 ? (preFillHoras * 60) + Mathf.Max(0, preFillMinutes) : 60;
                _pendingFinalEscrowAmount = totalMins;
                if (_symmetryRite != null && _activePartner != null)
                {
                    _symmetryRite.Show(_activePartner);
                }
                else
                {
                    _harmonyChannel?.ConfirmHarmonySeal(_pendingFinalEscrowAmount).Forget();
                    _navigation?.SwitchTo("harmony");
                }
                return;
            }

            // Pre-fill inputs with Oracle verdict (or leave empty for manual)
            var inputHours = escrowEl.Q<TextField>("InputEscrowHours");
            var inputMinutes = escrowEl.Q<TextField>("InputEscrowMinutes");

            if (inputHours != null)
                inputHours.value = preFillHoras >= 0 ? preFillHoras.ToString() : "";
            if (inputMinutes != null)
                inputMinutes.value = preFillMinutes >= 0 ? preFillMinutes.ToString() : "";

            escrowEl.RemoveFromClassList("modal--hidden");
            escrowEl.style.display = DisplayStyle.Flex;
            escrowEl.pickingMode   = PickingMode.Position;
            Debug.Log("[Popup] Opened: EscrowModal (Ритуал ескроу)");

            var btnConfirm = escrowEl.Q<Button>("BtnConfirmEscrow");
            var btnCancel  = escrowEl.Q<Button>("BtnCancelEscrow");

            void ConfirmEscrow()
            {
                escrowEl.AddToClassList("modal--hidden");
                escrowEl.style.display = StyleKeyword.Null;
                escrowEl.pickingMode   = PickingMode.Ignore;
                if (btnConfirm != null) btnConfirm.clicked -= ConfirmEscrow;
                if (btnCancel  != null) btnCancel.clicked  -= CancelEscrow;

                // Parse the (possibly user-edited) hours and minutes
                int parsedHours = 1;
                int parsedMinutes = 0;

                if (inputHours != null && int.TryParse(inputHours.value, out int h) && h >= 0)
                    parsedHours = h;
                if (inputMinutes != null && int.TryParse(inputMinutes.value, out int m) && m >= 0)
                    parsedMinutes = m;

                int totalMinutes = (parsedHours * 60) + parsedMinutes;
                if (totalMinutes < 5) totalMinutes = 5; // Safe minimum limits

                _pendingFinalEscrowAmount = totalMinutes;

                // Open Symmetry Rite modal as the ultimate sealing ritual
                if (_symmetryRite != null && _activePartner != null)
                {
                    _symmetryRite.Show(_activePartner);
                }
                else
                {
                    Debug.LogWarning("[NexusController] Symmetry Rite or partner is null, confirming seal directly.");
                    _harmonyChannel?.ConfirmHarmonySeal(_pendingFinalEscrowAmount).Forget();
                    _navigation?.SwitchTo("harmony");
                }
            }

            void CancelEscrow()
            {
                escrowEl.AddToClassList("modal--hidden");
                escrowEl.style.display = StyleKeyword.Null;
                escrowEl.pickingMode   = PickingMode.Ignore;
                if (btnConfirm != null) btnConfirm.clicked -= ConfirmEscrow;
                if (btnCancel  != null) btnCancel.clicked  -= CancelEscrow;
            }

            if (btnConfirm != null) btnConfirm.clicked += ConfirmEscrow;
            if (btnCancel  != null) btnCancel.clicked  -= CancelEscrow;
        }

        private void UpdateDynamicUiAndSound(List<Color> colors)
        {
            if (colors == null || colors.Count == 0) return;

            Debug.Log($"[NexusController] 🎨 Адаптація Aura Symphony: {colors.Count} активних кольорів.");

            // UI Toolkit USS dynamic variable updates on the root element
            if (uiDocument != null && uiDocument.rootVisualElement != null)
            {
                var root = uiDocument.rootVisualElement;

                // Simple mixing for primary background/accent
                Color primaryColor = colors[0];
                Color secondaryColor = colors.Count > 1 ? colors[1] : colors[0];
                
                // Add soft ambient background styling to main HUD panel if present
                var hudBackground = root.Q("HudPanel") ?? root.Q("NexusMenuPanel") ?? root.Q("RadarPanel");
                if (hudBackground != null)
                {
                    hudBackground.style.backgroundColor = new StyleColor(new Color(primaryColor.r * 0.1f, primaryColor.g * 0.1f, primaryColor.b * 0.15f, 0.95f));
                    hudBackground.style.borderLeftColor = new StyleColor(primaryColor);
                    hudBackground.style.borderRightColor = new StyleColor(secondaryColor);
                }
            }

            // Audio loop adaptation
            if (_audioService != null)
            {
                float warmWeight = 0;
                float coolWeight = 0;

                foreach (var c in colors)
                {
                    float r = c.r;
                    float g = c.g;
                    float b = c.b;

                    if (r > b) warmWeight += 1f;
                    else coolWeight += 1f;
                }

                if (coolWeight > warmWeight)
                {
                    _audioService.PlayMusic("TransformationAmbience", true);
                    Debug.Log("[NexusController] 🎵 Звуковий ландшафт: Космічний містичний амбієнт (TransformationAmbience).");
                }
                else
                {
                    _audioService.PlayMusic("TechAuraMusic", true);
                    Debug.Log("[NexusController] 🎵 Звуковий ландшафт: Ритмічний технологічний амбієнт (TechAuraMusic).");
                }
            }
        }
        private void ShowFatalErrorOverlay(string message)
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;
            
            var overlay = new UnityEngine.UIElements.VisualElement();
            overlay.style.position = UnityEngine.UIElements.Position.Absolute;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new Color(0.1f, 0f, 0f, 0.95f));
            overlay.style.alignItems = UnityEngine.UIElements.Align.Center;
            overlay.style.justifyContent = UnityEngine.UIElements.Justify.Center;

            var label = new UnityEngine.UIElements.Label(message);
            label.style.color = new UnityEngine.UIElements.StyleColor(Color.white);
            label.style.fontSize = 42;
            label.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            label.style.whiteSpace = UnityEngine.UIElements.WhiteSpace.Normal;
            
            var btn = new UnityEngine.UIElements.Button(() => UnityEngine.Application.Quit());
            btn.text = _localization != null ? _localization.Get(TimeAura.Core.Localization.AuraTerms.BTN_EXIT, "Вийти").ToUpper() : "ВИЙТИ";
            btn.style.marginTop = 40;
            btn.style.paddingTop = 15;
            btn.style.paddingBottom = 15;
            btn.style.paddingLeft = 40;
            btn.style.paddingRight = 40;
            btn.style.fontSize = 32;
            btn.style.backgroundColor = new UnityEngine.UIElements.StyleColor(new Color(0.8f, 0.1f, 0.1f));
            btn.style.color = new UnityEngine.UIElements.StyleColor(Color.white);

            overlay.Add(label);
            overlay.Add(btn);
            
            uiDocument.rootVisualElement.Add(overlay);
        }
    }
}
