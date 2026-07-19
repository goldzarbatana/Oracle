using System;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using TimeAura.Features.Economy;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Core.Data.SO;
using TimeAura.Core.Localization;

namespace TimeAura.Features.UI.Nexus
{
    public class VaultController : MonoBehaviour
    {
        private AuthManager _authManager;
        private LocalizationManager _localization;
        private MediaService _mediaService;
        private AudioService _audioService;
        private IDataService _dataService;
        private QuantumWalletService _walletService;

        private VisualElement _vaultViewMode, _vaultEditMode;
        private Label _lblName, _lblAge, _lblLocation, _lblTitle, _lblBio, _lblCooldownHint;
        private Label _statSymmetries, _statHoras, _statQuants;
        private TextField _inputName, _inputBio;
        private SliderInt _sliderAge;
        private Button _btnSave, _btnCancel;
        private Label _lblEditAgeValue;
        private VisualElement _vaultAvatarContainer, _vaultAvatarImage, _lblChangePhoto;
        private Button _btnAd, _btnIap, _btnOpenShop, _btnOpenAura, _btnProfileSupport;
        private Label _vaultHeader;

        private bool _isEditing;

        public void Initialize(VisualElement root, AuthManager auth, LocalizationManager loc, MediaService media, AudioService audio, IDataService data, QuantumWalletService wallet)
        {
            _authManager = auth;
            _localization = loc;
            _mediaService = media;
            _audioService = audio;
            _dataService = data;
            _walletService = wallet;

            Debug.Log("[Vault] 🛠️ Initializing with full data mastery.");
            _vaultViewMode = root.Q("VaultViewMode");
            _vaultEditMode = root.Q("VaultEditMode");

            // View Mode Elements
            _lblName = root.Q<Label>("VaultName");
            _lblAge = root.Q<Label>("VaultAge");
            _lblLocation = root.Q<Label>("VaultLocation");
            _lblTitle = root.Q<Label>("VaultTitle");
            _lblBio = root.Q<Label>("VaultBio");
            _statSymmetries = root.Q<Label>("StatSymmetries");
            _statHoras = root.Q<Label>("StatHoras");
            _statQuants = root.Q<Label>("StatQuants");

            // Edit Mode Elements
            _inputName = root.Q<TextField>("InputVaultName");
            _inputBio = root.Q<TextField>("InputVaultBio");
            _sliderAge = root.Q<SliderInt>("SliderVaultAge");
            _lblEditAgeValue = root.Q<Label>("LblEditAgeValue");
            _btnSave = root.Q<Button>("BtnSaveProfile");
            _btnCancel = root.Q<Button>("BtnCancelEdit");
            _lblCooldownHint = root.Q<Label>("LblCooldownHint");
            _vaultHeader = root.Q<Label>("VaultHeader");
            _btnProfileSupport = root.Q<Button>("BtnProfileSupport");

            // Callbacks
            if (_sliderAge != null)
            {
                _sliderAge.RegisterValueChangedCallback(evt => {
                    if (_lblEditAgeValue != null) _lblEditAgeValue.text = evt.newValue.ToString();
                });
            }

            // Common
            _vaultAvatarContainer = root.Q("VaultAuraSphere");
            _vaultAvatarImage = root.Q("VaultAvatar");
            _lblChangePhoto = root.Q("LblChangePhoto");

            var pName = root.Q("PencilEditName");
            var pAge = root.Q("PencilEditAge");
            var pBio = root.Q("PencilEditBio");

            // Callbacks
            pName?.RegisterCallback<ClickEvent>(e => ToggleEditMode());
            pAge?.RegisterCallback<ClickEvent>(e => ToggleEditMode());
            pBio?.RegisterCallback<ClickEvent>(e => ToggleEditMode());
            
            if (_btnSave != null) _btnSave.clicked += SaveProfile;
            if (_btnCancel != null) _btnCancel.clicked += () => SetEditMode(false);
            
            if (_vaultAvatarContainer != null)
                _vaultAvatarContainer.RegisterCallback<ClickEvent>(OnAvatarClick);

            _btnAd = root.Q<Button>("BtnWatchAd");
            if (_btnAd != null) _btnAd.clicked += () => ShowAdAsync().Forget();
            else Debug.LogError("[Vault] ❌ BtnWatchAd not found in UXML!");

            _btnIap = root.Q<Button>("BtnBuyStarterPack");
            if (_btnIap != null) _btnIap.clicked += () => 
            {
                if (UniversalMonetization.MonetizationOrchestrator.Instance == null)
                    Debug.LogError("[Vault] ❌ MonetizationOrchestrator.Instance is NULL! Did you add the Prefab to the scene?");
                else
                    UniversalMonetization.MonetizationOrchestrator.Instance.Iap.BuyProduct("com.timeaura.horas.pack1");
            };
            
            _btnOpenShop = root.Q<Button>("BtnOpenShop");
            _btnOpenAura = root.Q<Button>("BtnOpenAura");
            var auraPanel = root.parent?.Q("AuraPanel") ?? root.Q("AuraPanel");
            if (_btnOpenAura != null && auraPanel != null)
            {
                _btnOpenAura.clicked += () =>
                {
                    _audioService?.PlaySFX("CrystalClick");
                    auraPanel.RemoveFromClassList("panel--hidden");
                    auraPanel.style.display = DisplayStyle.Flex;
                    auraPanel.style.visibility = Visibility.Visible;
                    auraPanel.style.opacity = 1f;
                    auraPanel.pickingMode = PickingMode.Position;
                    auraPanel.BringToFront();
                };
            }

            RefreshUI();
        }

        private async UniTaskVoid ShowAdAsync()
        {
            if (UniversalMonetization.MonetizationOrchestrator.Instance == null)
            {
                Debug.LogError("[Vault] ❌ MonetizationOrchestrator.Instance is NULL! Did you add the Prefab to the scene?");
                return;
            }
            
            Debug.Log("[Vault] Requesting Rewarded Ad...");
            var result = await UniversalMonetization.MonetizationOrchestrator.Instance.Ads.ShowRewardedAdAsync();
            
            if (result.IsSuccess)
            {
                Debug.Log("[Vault] Ad finished! Reward will be granted by EconomyAdapter.");
                // Give it a moment for the event to process before refreshing
                await UniTask.Delay(500); 
                RefreshUI();
            }
            else
            {
                Debug.LogWarning($"[Vault] Ad failed: {result.FailureReason}");
            }
        }

        public void RefreshUI()
        {
            var profile = _authManager != null ? _authManager.CurrentProfile : null;
            
            if (profile == null)
            {
                Debug.LogWarning("[Vault] ⚠️ CurrentProfile is null. Attempting to restore session from the ether...");
                // Attempt to restore if possible (async, won't block UI but will refresh once done)
                TryRestoreSession().Forget();
                
                // Show placeholder state
                var pTone = _localization != null ? _localization.CurrentTone : OracleTone.Business;
                if (_lblName != null) 
                    _lblName.text = _localization != null ? _localization.GetPersonaString(AuraTerms.SYNCING, pTone, "SYNCHRONIZING...") : "SYNCHRONIZING...";
                if (_lblTitle != null) 
                    _lblTitle.text = _localization != null ? _localization.GetPersonaString(AuraTerms.TRANSCENDING, pTone, "TRANSCENDING") : "TRANSCENDING";
                return;
            }

            Debug.Log($"[Vault] 🔄 Refreshing UI for: {profile.DisplayName} (Age: {profile.Age}, Status: {profile.Status})");

            var tone = _localization != null ? _localization.CurrentTone : OracleTone.Business;

            // Calculate Dynamic Title based on status (only if not already set to a custom dynamic title)
            if (string.IsNullOrEmpty(profile.AuraTitle) || profile.AuraTitle.StartsWith("title.") || profile.AuraTitle.Equals("Initiate", StringComparison.OrdinalIgnoreCase))
            {
                string titleKey = AuraTerms.TITLE_INITIATE;
                if (profile.Status > 100) titleKey = AuraTerms.TITLE_ORACLE;
                else if (profile.Status > 50) titleKey = AuraTerms.TITLE_ELDER;
                else if (profile.Status > 10) titleKey = AuraTerms.TITLE_MASTER;
                
                profile.AuraTitle = _localization.GetPersonaString(titleKey, tone, titleKey.Replace("title.", "").ToUpper());
            }

            // View Mode
            if (_lblName != null) _lblName.text = profile.DisplayName.ToUpper();
            
            string ageUnit = _localization != null ? _localization.GetPersonaString(AuraTerms.INIT_AGE_UNIT, tone, "winter cycles") : "winter cycles";
            if (_lblAge != null) _lblAge.text = $"{profile.Age} {ageUnit}";
            
            string unknownRealm = _localization != null ? _localization.GetPersonaString(AuraTerms.REALM_UNKNOWN, tone, "Unknown Realm") : "Unknown Realm";
            if (_lblLocation != null) _lblLocation.text = $"📍 {profile.LocationZone ?? unknownRealm}";
            
            if (_lblTitle != null) _lblTitle.text = profile.AuraTitle.ToUpper();
            
            string emptyBio = _localization != null ? _localization.GetPersonaString(AuraTerms.BIO_EMPTY, tone, "Your legacy remains unwritten...") : "Your legacy remains unwritten...";
            if (_lblBio != null) _lblBio.text = string.IsNullOrEmpty(profile.Bio) ? emptyBio : profile.Bio;

            
            if (_statSymmetries != null) _statSymmetries.text = profile.Legacy.Count.ToString();
            if (_statHoras != null) 
            {
                _statHoras.text = EconomyFormatter.FormatHoras(profile.TimeBalanceMinutes);
                _statHoras.style.color = profile.TimeBalanceMinutes < 0 ? new StyleColor(Color.red) : new StyleColor(new Color(0.83f, 0.69f, 0.22f)); // Red or Gold
            }
            if (_statQuants != null)
            {
                _statQuants.text = EconomyFormatter.FormatQuants(profile.WavesBalance);
                _statQuants.style.color = profile.WavesBalance < 0 ? new StyleColor(Color.red) : new StyleColor(new Color(0.83f, 0.69f, 0.22f));
            }

            if (_btnAd != null)
            {
                _btnAd.text = _localization != null ? _localization.Get(AuraTerms.VAULT_BTN_WATCH_AD, "👁️ WATCH AD (+1 CONTRACT SLOT)") : "👁️ WATCH AD (+1 CONTRACT SLOT)";
            }
            if (_btnIap != null)
            {
                _btnIap.text = _localization != null ? _localization.Get(AuraTerms.VAULT_BTN_BUY_STARTER, "🛒 BUY STARTER PACK (10 HORAS)") : "🛒 BUY STARTER PACK (10 HORAS)";
            }
            if (_btnOpenShop != null)
            {
                _btnOpenShop.text = _localization != null ? _localization.Get(AuraTerms.VAULT_BTN_OPEN_SHOP, "💎 REFILL QUANTS") : "💎 REFILL QUANTS";
            }
            if (_btnOpenAura != null)
            {
                _btnOpenAura.text = _localization != null ? _localization.Get("vault_btn_aura", "🔮 МОЯ АУРА").ToUpper() : "🔮 МОЯ АУРА";
            }
            if (_btnProfileSupport != null)
            {
                _btnProfileSupport.text = _localization != null ? _localization.GetPersonaString("vault_btn_support", tone, "SEND ENERGY TO CREATORS").ToUpper() : "SEND ENERGY TO CREATORS";
            }
            if (_vaultHeader != null)
            {
                _vaultHeader.text = _localization != null ? _localization.GetPersonaString("vault_header", tone, "MASTER VAULT").ToUpper() : "MASTER VAULT";
            }
            if (_inputName != null) _inputName.label = _localization != null ? _localization.GetPersonaString("vault_input_name", tone, "NEXUS NAME").ToUpper() : "NEXUS NAME";
            if (_sliderAge != null) _sliderAge.label = _localization != null ? _localization.GetPersonaString("vault_input_age", tone, "YOUR AGE").ToUpper() : "YOUR AGE";
            if (_inputBio != null) _inputBio.label = _localization != null ? _localization.GetPersonaString("vault_input_bio", tone, "LEGACY (BIO)").ToUpper() : "LEGACY (BIO)";
            if (_btnSave != null) _btnSave.text = _localization != null ? _localization.GetPersonaString("vault_btn_save", tone, "SEAL CHANGES").ToUpper() : "SEAL CHANGES";
            if (_btnCancel != null) _btnCancel.text = _localization != null ? _localization.GetPersonaString("vault_btn_cancel", tone, "CANCEL").ToUpper() : "CANCEL";
            if (_lblCooldownHint != null) _lblCooldownHint.text = _localization != null ? _localization.GetPersonaString("vault_cooldown_hint", tone, "Identity stability: 7 days cooldown") : "Identity stability: 7 days cooldown";

            // Edit Mode Pre-fill
            if (_inputName != null) _inputName.value = profile.DisplayName;
            if (_inputBio != null) _inputBio.value = profile.Bio;
            if (_sliderAge != null) _sliderAge.value = profile.Age;
            if (_lblEditAgeValue != null) _lblEditAgeValue.text = profile.Age.ToString();

            UpdateAvatarVisuals(profile);
            DrawConstellations(profile);
            SetEditMode(false);
        }

        private VisualElement _constellationContainer;
        private void DrawConstellations(UserProfile profile)
        {
            if (_vaultViewMode == null) return;
            
            if (_constellationContainer == null)
            {
                _constellationContainer = new VisualElement();
                _constellationContainer.style.height = 150;
                _constellationContainer.style.marginTop = 20;
                _constellationContainer.style.backgroundColor = new StyleColor(new Color(0.05f, 0.05f, 0.1f, 0.5f));
                _constellationContainer.style.borderTopColor = new StyleColor(new Color(0.83f, 0.69f, 0.22f));
                _constellationContainer.style.borderTopWidth = 1;
                _constellationContainer.style.overflow = Overflow.Hidden;
                
                var tone = _localization != null ? _localization.CurrentTone : OracleTone.Business;
                var title = new Label(_localization != null ? _localization.GetPersonaString("vault_star_map", tone, "✨ ЗОРЯНА КАРТА РЕПУТАЦІЇ ✨").ToUpper() : "✨ ЗОРЯНА КАРТА РЕПУТАЦІЇ ✨");
                title.style.color = new StyleColor(new Color(0.83f, 0.69f, 0.22f));
                title.style.fontSize = 14;
                title.style.unityTextAlign = TextAnchor.MiddleCenter;
                title.style.marginTop = 10;
                _constellationContainer.Add(title);
                
                _vaultViewMode.Add(_constellationContainer);
            }

            // Clear previous stars
            for (int i = _constellationContainer.childCount - 1; i >= 1; i--)
            {
                _constellationContainer.RemoveAt(i);
            }

            if (profile.Constellations == null || profile.Constellations.Count == 0) return;

            foreach (var star in profile.Constellations)
            {
                var starDot = new VisualElement();
                starDot.style.position = Position.Absolute;
                starDot.style.width = 10;
                starDot.style.height = 10;
                starDot.style.borderTopLeftRadius = 5;
                starDot.style.borderTopRightRadius = 5;
                starDot.style.borderBottomLeftRadius = 5;
                starDot.style.borderBottomRightRadius = 5;
                starDot.style.backgroundColor = Color.white;
                
                // MapX and MapY are 0..1
                starDot.style.left = new Length(star.MapX * 100, LengthUnit.Percent);
                starDot.style.top = new Length(star.MapY * 100, LengthUnit.Percent);
                
                var tooltip = new Label($"{star.PartnerName}\n+{EconomyFormatter.FormatHoras(star.minutesExchanged)}");
                tooltip.style.position = Position.Absolute;
                tooltip.style.left = 15;
                tooltip.style.top = -10;
                tooltip.style.color = new StyleColor(new Color(0.83f, 0.69f, 0.22f));
                tooltip.style.fontSize = 10;
                tooltip.style.display = DisplayStyle.None; // Show on hover if possible
                
                starDot.Add(tooltip);
                
                starDot.RegisterCallback<MouseEnterEvent>(e => tooltip.style.display = DisplayStyle.Flex);
                starDot.RegisterCallback<MouseLeaveEvent>(e => tooltip.style.display = DisplayStyle.None);

                _constellationContainer.Add(starDot);
            }
        }

        private async UniTaskVoid TryRestoreSession()
        {
            if (_authManager == null) return;
            
            bool restored = await _authManager.CheckSessionAsync();
            if (restored)
            {
                Debug.Log("[Vault] ✨ Session restored successfully. Re-aligning UI...");
                RefreshUI();
            }
            else
            {
                Debug.LogWarning("[Vault] ❌ Failed to restore session. Are you connected to the Ley Lines?");
            }
        }

        private void UpdateAvatarVisuals(UserProfile profile)
        {
            if (_vaultAvatarImage == null) return;
            // In a real app, load from AvatarUrl
            // For now, use a colored glow
            _vaultAvatarImage.style.backgroundColor = profile.GetPrimaryAuraColor();
        }

        private void ToggleEditMode()
        {
            SetEditMode(!_isEditing);
        }

        private void SetEditMode(bool editing)
        {
            _isEditing = editing;
            if (_vaultViewMode != null) _vaultViewMode.style.display = editing ? DisplayStyle.None : DisplayStyle.Flex;
            if (_vaultEditMode != null) _vaultEditMode.style.display = editing ? DisplayStyle.Flex : DisplayStyle.None;
            if (_lblChangePhoto != null) _lblChangePhoto.style.display = editing ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (editing)
            {
                _audioService?.PlaySFX("CrystalClick");
                var profile = _authManager.CurrentProfile;
                bool canEditIdentity = profile != null && profile.CanUpdateIdentity();
                
                if (_inputName != null) _inputName.SetEnabled(canEditIdentity);
                if (_sliderAge != null) _sliderAge.SetEnabled(canEditIdentity);
                if (_lblCooldownHint != null) _lblCooldownHint.style.display = canEditIdentity ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private async void SaveProfile()
        {
            var profile = _authManager.CurrentProfile;
            if (profile == null || _dataService == null) return;

            bool identityChanged = profile.DisplayName != _inputName.value || profile.Age != _sliderAge.value;
            
            // Task: Update local essence
            profile.Bio = _inputBio.value;
            
            if (identityChanged && profile.CanUpdateIdentity())
            {
                profile.DisplayName = _inputName.value;
                profile.Age = _sliderAge.value;
                profile.RegisterIdentityUpdate();
                Debug.Log("[Vault] 👤 Identity stabilized locally.");
            }

            // Task: Synchronize with the Akashic Records (Firestore)
            try
            {
                _audioService?.PlaySFX("CrystalClick");
                Debug.Log("[Vault] 💾 Committing changes to Firestore...");
                
                await _dataService.SaveUserProfileAsync(profile, default);
                
                Debug.Log("[Vault] ✅ Profile sealed in the Akashic Records.");
                _audioService?.PlaySFX("SuccessRitual");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Vault] ❌ Ritual failed (Save Error): {ex.Message}");
                var tone = _localization != null ? _localization.CurrentTone : OracleTone.Business;
                string errTitle = _localization != null ? _localization.GetPersonaString("vault_err_title", tone, "Помилка") : "Помилка";
                string errMsg = _localization != null ? _localization.GetPersonaString("vault_err_msg", tone, "Не вдалося зберегти профіль:") : "Не вдалося зберегти профіль:";
                FindAnyObjectByType<UIManager>()?.ShowErrorPopup(errTitle, $"{errMsg} {ex.Message}");
            }
            
            RefreshUI();
            SetEditMode(false);
        }

        private async void OnAvatarClick(ClickEvent evt)
        {
            if (!_isEditing) return;

            var profile = _authManager.CurrentProfile;
            if (profile == null) return;

            Debug.Log("[Vault] 🎞️ Initiating Visage Transmutation...");
            _audioService?.PlaySFX("CrystalClick");
            
            // Task: Capture material from the physical realm
            var data = await _mediaService.CaptureMaterialAsync();
            if (data != null)
            {
                if (_vaultAvatarImage != null)
                    _vaultAvatarImage.style.backgroundColor = Color.white;

                Debug.Log("[Vault] 🏗️ Transmuting Material to Digital Essence...");
                
                // MOCK UPLOAD
                await UniTask.Delay(1500); 
                
                string mockUrl = "https://firebasestorage.googleapis.com/v0/b/timeaura/avatar_" + profile.UserId;
                profile.AvatarUrl = mockUrl;
                
                await _dataService.SaveUserProfileAsync(profile, default);
                
                Debug.Log("[Vault] ✨ Visage finalized and saved.");
                _audioService?.PlaySFX("SuccessRitual");
                
                UpdateAvatarVisuals(profile);
            }
        }

        public void UpdateLocalization()
        {
            RefreshUI();
        }
    }
}
