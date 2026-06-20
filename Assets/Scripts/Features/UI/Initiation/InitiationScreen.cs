using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Auth;
using TimeAura.Core.Services;
using TimeAura.Features.Localization;
using TimeAura.Core.Localization;
using TimeAura.Features.Aura;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AddressableAssets;
using TimeAura.Features.Data;
using VContainer;

namespace TimeAura.Features.UI.Initiation
{
    /// <summary>
    /// Initiation Orchestrator. Coordinates specialized sub-controllers for the gateway scene.
    /// No longer a monolith.
    /// </summary>
    public class InitiationScreen : MonoBehaviour, IScreen
    {
        [Header("UI Toolkit Setup")]
        [SerializeField] private UIDocument uiDocument;
        
        [Header("Transition Settings")]
        [SerializeField] private float fadeInDuration = 1.5f;
        [SerializeField] private float fadeOutDuration = 1.0f;

        [Inject] private AuthManager _authManager;
        [Inject] private UIManager _uiManager;
        [Inject] private LocalizationManager _localization;
        [Inject] private AudioService _audioService;
        [Inject] private IDataService _dataService;

        [Header("Sub-Controllers")]
        [SerializeField] private InitiationRitualController _ritual;
        [SerializeField] private InitiationOracleController _oracle;
        [SerializeField] private InitiationLanguageController _language;
        [SerializeField] private AuraVFXController _vfx;

        private VisualElement _root;
        private VisualElement _introRoot;
        private TextField _inputPhone, _inputOTP;
        private Button _btnInitiate, _btnGoogleAuth, _btnAppleAuth;
        private Label _placeholder, _otpPlaceholder;
        private VisualElement _inputGroup, _otpGroup, _oauthGroup;

        private CancellationTokenSource _cts;
        private bool _isInitiating;

        public void Show() 
        {
            gameObject.SetActive(true);
            Debug.Log("<color=#FFD700><b>[PULSE]</b></color> Entering Panel: <color=#00FFFF><b>INITIATION GATEWAY</b></color>");
        }
        public void Hide() => gameObject.SetActive(false);

        private void Awake()
        {
            // Auto-find sub-controllers if not assigned
            if (_ritual == null) _ritual = GetComponent<InitiationRitualController>();
            if (_oracle == null) _oracle = GetComponent<InitiationOracleController>();
            if (_language == null) _language = GetComponent<InitiationLanguageController>();
            
            if (_vfx == null) _vfx = FindAnyObjectByType<AuraVFXController>();
            if (_vfx == null)
            {
                var prefab = Resources.Load<GameObject>("Prefabs/AuraVFX_Core");
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    _vfx = instance.GetComponent<AuraVFXController>();
                    Debug.Log("[InitiationScreen] ✧ Spawned AuraVFX_Core from Resources.");
                }
            }

            if (gameObject.GetComponent<TimeAura.Core.UI.UISafeAreaHandler>() == null)
                gameObject.AddComponent<TimeAura.Core.UI.UISafeAreaHandler>();

            var docs = GetComponents<UIDocument>();
            if (docs.Length > 1) Debug.LogWarning($"[InitiationScreen] ⚠️ Found {docs.Length} UIDocuments on this object!");
            
            if (uiDocument == null) uiDocument = docs.Length > 0 ? docs[0] : null;
            if (uiDocument != null && uiDocument.visualTreeAsset != null)
                Debug.Log($"[InitiationScreen] 🖥️ UIDocument found with asset: {uiDocument.visualTreeAsset.name}");
        }

        private async void Start()
        {
            try
            {
                // 1. Cleanup Scene Duplicates
                // There is often a legacy 'IntroUI' object in the scene that conflicts with us.
                var legacyIntro = GameObject.Find("IntroUI");
                if (legacyIntro != null && legacyIntro != gameObject)
                {
                    Debug.LogWarning($"[InitiationScreen] 🗑️ Found legacy 'IntroUI' object. Deactivating to prevent style conflicts.");
                    legacyIntro.SetActive(false);
                }

                // 2. Ensure UIDocument presence
                if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
                if (uiDocument == null)
                {
                    Debug.LogError("[InitiationScreen] ❌ UIDocument NOT FOUND on this GameObject!");
                    return;
                }

                // 3. Force sorting priority
                // Oracle is usually at -1 or 100. We want to be definitely on top of background but below system popups.
                uiDocument.sortingOrder = 50; 

                // 4. Wait for UI Toolkit Layout Engine
                await UniTask.NextFrame();

                var root = uiDocument.rootVisualElement;
                if (root == null)
                {
                    Debug.LogError("[InitiationScreen] ❌ Root visual element is NULL after wait!");
                    return;
                }

                // 5. Deep Diagnostics
                int sheetCount = root.styleSheets.count;
                var panelSettingsName = uiDocument.panelSettings != null ? uiDocument.panelSettings.name : "NULL";
                
                Debug.Log($"[InitiationScreen] 🛠️ UI DIAGNOSTICS:");
                Debug.Log($" - Panel Settings: {panelSettingsName}");
                Debug.Log($" - Stylesheets: {sheetCount}");
                Debug.Log($" - Root Size: {root.layout.width}x{root.layout.height}");
                Debug.Log($" - Sorting Order: {uiDocument.sortingOrder}");

                if (panelSettingsName.Contains("Nexus") || panelSettingsName.Contains("Menu"))
                {
                    Debug.LogWarning($"[InitiationScreen] ⚠️ Using '{panelSettingsName}'. If styles are missing, check if this panel has a Theme StyleSheet.");
                }

                // 6. Bind Elements and Initialize
                InitializeWiring();
                UpdateLocalization();
                
                // --- Task: Ensure visibility of core elements ---
                var mainContent = _root.Q("MainContent");
                if (mainContent != null) mainContent.style.display = DisplayStyle.Flex;
                
                var logo = _root.Q("LogoMain");
                if (logo != null) logo.style.display = DisplayStyle.Flex;
                
                // Start Sequence
                await FadeInAsync();
                _audioService?.PlayMusic("SacredCompass");

                // Auto-resume check
                bool isAuth = await _authManager.CheckSessionAsync();
                if (isAuth && _authManager.CurrentProfile != null && !_authManager.CurrentProfile.HasCompletedInitiation)
                {
                    Debug.Log("[InitiationScreen] 🔁 Authenticated but Initiation incomplete. Auto-resuming ritual...");
                    _ = ResumeRitualAsync(_authManager.CurrentProfile);
                    return; // Skip normal intro
                }

                if (_oracle != null) await _oracle.RequestProphecy();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void InitializeWiring()
        {
            _root = uiDocument.rootVisualElement;
            _introRoot = _root.Q<VisualElement>("IntroRoot");
            _inputPhone = _root.Q<TextField>("InputPhone");
            _inputOTP = _root.Q<TextField>("InputOTP");
            _btnInitiate = _root.Q<Button>("BtnInitiate");
            _btnGoogleAuth = _root.Q<Button>("BtnGoogleAuth");
            _btnAppleAuth = _root.Q<Button>("BtnAppleAuth");
            _placeholder = _root.Q<Label>("PlaceholderLabel");
            _otpPlaceholder = _root.Q<Label>("OTPPlaceholder");
            _inputGroup = _root.Q("InputGroup");
            _otpGroup = _root.Q("OTPGroup");
            _oauthGroup = _root.Q("OAuthGroup");

            if (_btnInitiate != null)
            {
                _btnInitiate.clicked += () => _ = BeginInitiationAsync();
            }

            if (_inputPhone != null)
            {
                _inputPhone.RegisterValueChangedCallback(evt => {
                    string filtered = new string(evt.newValue.Where(c => char.IsDigit(c) || c == '+').ToArray());
                    if (filtered != evt.newValue) _inputPhone.SetValueWithoutNotify(filtered);
                    
                    if (_placeholder != null)
                        _placeholder.style.display = string.IsNullOrEmpty(filtered) ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }

            if (_inputOTP != null)
            {
                _inputOTP.RegisterValueChangedCallback(evt => {
                    string filtered = new string(evt.newValue.Where(char.IsDigit).ToArray());
                    if (filtered != evt.newValue) _inputOTP.SetValueWithoutNotify(filtered);
                    
                    if (_otpPlaceholder != null)
                        _otpPlaceholder.style.display = string.IsNullOrEmpty(filtered) ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }

            if (_btnGoogleAuth != null) _btnGoogleAuth.clicked += () => _ = BeginOAuthInitiationAsync("Google");
            if (_btnAppleAuth != null) _btnAppleAuth.clicked += () => _ = BeginOAuthInitiationAsync("Apple");

            // --- Sub-Controller Initialization ---
            _language?.Initialize(_root);
            _language?.SetActive(true);
            
            _oracle?.Initialize(_root);
            _oracle?.SetActive(true);
            
            _ritual?.Initialize(_root, _vfx, (text) => _oracle?.TypeOracleText(text) ?? UniTask.CompletedTask);
            _ritual?.SetActive(false); // Only active during the ritual

            EventBus.Subscribe<LanguageChangedEvent>(e => UpdateLocalization());
        }

        private void UpdateLocalization()
        {
            if (_localization == null) return;
            
            var tone = _localization.CurrentTone;
            var inputLabel = _root.Q<Label>("InputLabel");
            if (inputLabel != null) 
            {
                var val = _localization.GetPersonaString(AuraTerms.PHONE_NUMBER, tone, "PHONE NUMBER")?.ToUpper();
                inputLabel.text = string.IsNullOrEmpty(val) ? " " : val;
            }
            if (_btnInitiate != null) 
            {
                var val = _localization.GetPersonaString(AuraTerms.LOGIN, tone, "INITIATE")?.ToUpper();
                _btnInitiate.text = string.IsNullOrEmpty(val) ? " " : val;
            }
            if (_placeholder != null) 
            {
                var val = _localization.GetPersonaString(AuraTerms.PHONE_PLACEHOLDER, tone, "+380 00 000 0000");
                _placeholder.text = string.IsNullOrEmpty(val) ? " " : val;
            }
            
            var prophecyLabel = _root.Q<Label>("ProphecyLabel");
            if (prophecyLabel != null && string.IsNullOrEmpty(_inputPhone?.value)) 
            {
                var val = _localization.GetPersonaString(AuraTerms.INIT_PROPHECY_WAIT, tone, "Awaiting the sacred breath...");
                prophecyLabel.text = string.IsNullOrEmpty(val) ? " " : val;
            }

            if (_inputPhone != null && _placeholder != null)
                _placeholder.style.display = string.IsNullOrEmpty(_inputPhone.value) ? DisplayStyle.Flex : DisplayStyle.None;
        }


        private async UniTask BeginOAuthInitiationAsync(string provider)
        {
            // STUB for Google / Apple OAuth
            Debug.Log($"[InitiationScreen] 🔗 Connecting via {provider}...");
            _inputPhone.value = $"+OAuth_{provider}"; // Dummy number for mock
            await BeginInitiationAsync(skipOTP: true);
        }

        private async UniTask BeginInitiationAsync(bool skipOTP = false)
        {
            if (_isInitiating) return;

            var phoneNumber = _inputPhone?.value;
            var tone = _localization.CurrentTone;
            if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 3)
            {
                _oracle?.ShowFeedback(_localization?.GetPersonaString(AuraTerms.INIT_ERR_SHORT, tone, "The path requires a longer number...") ?? "The path requires a longer number...");
                return;
            }

            // OTP Flow
            if (!skipOTP && _otpGroup != null && _otpGroup.style.display == DisplayStyle.None)
            {
                // Show OTP Group
                if (_inputGroup != null) _inputGroup.style.display = DisplayStyle.None;
                if (_oauthGroup != null) _oauthGroup.style.display = DisplayStyle.None;
                _otpGroup.style.display = DisplayStyle.Flex;
                
                if (_btnInitiate != null) 
                {
                    _btnInitiate.text = _localization?.GetPersonaString(AuraTerms.LOGIN, tone, "VERIFY CODE") ?? "VERIFY CODE";
                }
                return;
            }

            if (!skipOTP && _otpGroup != null && _otpGroup.style.display == DisplayStyle.Flex)
            {
                if (_inputOTP == null || string.IsNullOrEmpty(_inputOTP.value) || _inputOTP.value.Length < 4)
                {
                    _oracle?.ShowFeedback("The ethereal code is incomplete. We need 4 digits.");
                    return;
                }
            }

            _isInitiating = true;
            _cts = new CancellationTokenSource();

            try
            {
                var loadingText = _localization?.GetPersonaString(AuraTerms.INIT_LOADING, tone, "OPENING THE PORTAL...") ?? "OPENING THE PORTAL...";
                if (_btnInitiate != null) 
                {
                    _btnInitiate.text = loadingText;
                    _btnInitiate.SetEnabled(false);
                }


                var result = await _authManager.AuthenticateAsync(_cts.Token);

                if (result.Profile != null && !result.Cancelled)
                {
                    if (result.Profile.Age == 0) 
                    {
                        if (_inputGroup != null) _inputGroup.style.display = DisplayStyle.None;
                        if (_otpGroup != null) _otpGroup.style.display = DisplayStyle.None;
                        if (_oauthGroup != null) _oauthGroup.style.display = DisplayStyle.None;
                        if (_btnInitiate != null) _btnInitiate.style.display = DisplayStyle.None;
                        
                        var logo = _root.Q("LogoMain");
                        if (logo != null) logo.style.display = DisplayStyle.None;
                        
                        // Hide the entire TopBar (language selection) once ritual starts
                        var topBar = _root.Q("TopBar");
                        if (topBar != null) topBar.style.display = DisplayStyle.None;
                        
                        if (_ritual != null) 
                        {
                            Debug.Log("<color=#FFD700><b>[PULSE]</b></color> Entering Sub-Panel: <color=#FF00FF><b>THE RITUAL</b></color>");
                            _ritual.SetActive(true);
                            await _ritual.RunRitualAsync(result.Profile);
                            _ritual.SetActive(false);
                            
                            // Task: Commit the new initiate's soul to the Akashic Records (Save to DB)
                            Debug.Log("[Initiation] 💾 Sealing the Ritual. Persisting profile to the Records...");
                            await _dataService.SaveUserProfileAsync(result.Profile, _cts.Token);
                            result.Profile.CompleteInitiation();
                            await _dataService.SaveUserProfileAsync(result.Profile, _cts.Token);
                        }
                    }
                    await TransitionToConvergenceAsync();
                }
                else
                {
                    tone = _localization.CurrentTone;
                    _oracle?.ShowFeedback(_localization?.GetPersonaString(AuraTerms.INIT_ERR_FAILED, tone, "The portal remains closed. Try again.") ?? "The portal remains closed. Try again.");
                    if (_btnInitiate != null)
                    {
                        _btnInitiate.text = _localization?.GetPersonaString(AuraTerms.LOGIN, tone, "INITIATE") ?? "INITIATE";
                        _btnInitiate.SetEnabled(true);
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.LogError($"[Initiation] Failed: {ex.Message}");
                tone = _localization.CurrentTone;
                _oracle?.ShowFeedback(_localization?.GetPersonaString(AuraTerms.INIT_ERR_CRITICAL, tone, "The Aura flickered. Please try again.") ?? "The Aura flickered. Please try again.");
                if (_btnInitiate != null)
                {
                    _btnInitiate.text = _localization?.GetPersonaString(AuraTerms.LOGIN, tone, "INITIATE") ?? "INITIATE";
                    _btnInitiate.SetEnabled(true);
                }
            }

            finally { _isInitiating = false; }
        }

        private async UniTask ResumeRitualAsync(UserProfile profile)
        {
            if (_inputGroup != null) _inputGroup.style.display = DisplayStyle.None;
            if (_otpGroup != null) _otpGroup.style.display = DisplayStyle.None;
            if (_oauthGroup != null) _oauthGroup.style.display = DisplayStyle.None;
            if (_btnInitiate != null) _btnInitiate.style.display = DisplayStyle.None;
            
            var logo = _root.Q("LogoMain");
            if (logo != null) logo.style.display = DisplayStyle.None;
            
            var topBar = _root.Q("TopBar");
            if (topBar != null) topBar.style.display = DisplayStyle.None;
            
            if (_ritual != null) 
            {
                Debug.Log("<color=#FFD700><b>[PULSE]</b></color> Entering Sub-Panel: <color=#FF00FF><b>THE RITUAL</b></color>");
                _ritual.SetActive(true);
                await _ritual.RunRitualAsync(profile);
                _ritual.SetActive(false);
                
                Debug.Log("[Initiation] 💾 Sealing the Ritual. Persisting profile to the Records...");
                await _dataService.SaveUserProfileAsync(profile, default);
                profile.CompleteInitiation();
                await _dataService.SaveUserProfileAsync(profile, default);
            }
            await TransitionToConvergenceAsync();
        }

        private async UniTask TransitionToConvergenceAsync()
        {
            await FadeOutAsync();
            try { await Addressables.LoadSceneAsync("Nexus").ToUniTask(); }
            catch (Exception ex)
            {
                Debug.LogError($"[Initiation] Failed to load Nexus: {ex.Message}");
                _oracle?.ShowFeedback("The way to Nexus is blocked. Check Build Settings.");
            }
        }

        private async UniTask FadeInAsync()
        {
            if (_introRoot == null) return;
            _introRoot.style.opacity = 0;
            _introRoot.style.display = DisplayStyle.Flex;
            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                _introRoot.style.opacity = Mathf.Lerp(0, 1, elapsed / fadeInDuration);
                await UniTask.Yield();
            }
            _introRoot.style.opacity = 1;
        }

        private async UniTask FadeOutAsync()
        {
            if (_introRoot == null) return;
            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _introRoot.style.opacity = Mathf.Lerp(1, 0, elapsed / fadeOutDuration);
                await UniTask.Yield();
            }
            _introRoot.style.opacity = 0;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
