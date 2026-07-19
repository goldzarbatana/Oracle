using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Data.SO;
using TimeAura.Features.Auth;
using TimeAura.Core.Localization;
using TimeAura.Features.Localization;
using TimeAura.Features.Aura;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Core.Services;
using TimeAura.Features.Data;

namespace TimeAura.Features.UI.Initiation
{
    public class InitiationRitualController : MonoBehaviour
    {
        public void SetActive(bool active) => enabled = active;

        [Inject] private AuthManager _authManager;
        [Inject] private LocalizationManager _localization;
        [Inject] private TimeAura.Core.Services.LocationService _locationService;
        [Inject] private GeocodingService _geocodingService;
        [Inject] private MediaService _mediaService;
        [Inject] private HapticService _hapticService;
        [Inject] private AuraPillarSO[] _pillars;
        [Inject] private UIManager _uiManager;
        
        private AuraVFXController _vfxController;
        private UserProfile _currentProfile;

        private VisualElement _ritualContainer, _pillarContainer, _quickStartSection, _ageSection, _genderSection, _visageSection, _locationSection, _nameSection, _locResults, _personaSection, _scaleSection, _smartLocationContainer, _searchContainer;
        private SliderInt _sliderAge, _sliderScale;
        private Label _lblAgeValue, _lblScaleValue, _lblSampleText, _locPlaceholder, _lblPersonaQuestion, _lblSmartLocationText;
        private VisualElement _imgVisage;
        private TextField _inputLocation, _inputName;
        private Button _btnGenderM, _btnGenderF, _btnGenderX, _btnLocate, _btnConfirmAge, _btnConfirmScale, _btnConfirmVisage, _btnPickGallery, _btnPickCamera, _btnConfirmName, _btnSkipName, _btnBack, _btnSmartLocYes, _btnSmartLocNo;
        private Button _btnQuickStartFindMaster, _btnQuickStartAskOracle, _btnQuickStartConfigureAura;
        private Button _btnPersonaMystic, _btnPersonaBusiness, _btnPersonaCasual, _btnPersonaTech;
        private Label _lblGenderM, _lblGenderF, _lblGenderX;
        
        private bool _personaConfirmed;
        private bool _ageConfirmed;
        private bool _scaleConfirmed;
        private bool _locationConfirmed;
        private bool _nameConfirmed;
        private bool _isSkippingName;
        private string _selectedGender;
        private bool _backRequested;
        private string _quickStartChoice;
        private RitualStep _currentStep = RitualStep.QuickStart;

        private void SetPersona(OracleTone tone)
        {
            var profile = _currentProfile ?? _authManager?.CurrentProfile;
            
            if (profile != null)
            {
                Debug.Log($"[InitiationRitual] 🎭 Setting Persona to: {tone}");
                _hapticService?.LightTap();
                profile.OracleTone = tone;
                _personaConfirmed = true;
                _ = _typeOracleText?.Invoke(_localization.Get("oracle.persona_confirmed", "Your resonance has been aligned."));
            }
            else
            {
                Debug.LogError("[InitiationRitual] ‼️ Cannot set Persona: Both _currentProfile and AuthManager.Profile are NULL!");
            }
        }

        private enum RitualStep
        {
            QuickStart = 0,
            Scale = 1,
            Persona = 2,
            Age = 3,
            Gender = 4,
            Visage = 5,
            Location = 6,
            Name = 7,
            Complete = 8
        }

        private Func<string, UniTask> _typeOracleText;

        public void Initialize(VisualElement root, AuraVFXController vfx, Func<string, UniTask> typeOracleTextCallback)
        {
            if (root == null) return;

            _vfxController = vfx;
            _typeOracleText = typeOracleTextCallback;
            _ritualContainer = root.Q("RitualContainer");
            _pillarContainer = root.Q("PillarContainer");

            _quickStartSection = root.Q("QuickStartSection");
            _btnQuickStartFindMaster = root.Q<Button>("BtnQuickStartFindMaster");
            _btnQuickStartAskOracle = root.Q<Button>("BtnQuickStartAskOracle");
            _btnQuickStartConfigureAura = root.Q<Button>("BtnQuickStartConfigureAura");

            if (_btnQuickStartFindMaster != null) _btnQuickStartFindMaster.clicked += () => ExecuteQuickStartChoice("FindMaster");
            if (_btnQuickStartAskOracle != null) _btnQuickStartAskOracle.clicked += () => ExecuteQuickStartChoice("AskOracle");
            if (_btnQuickStartConfigureAura != null) _btnQuickStartConfigureAura.clicked += () => ExecuteQuickStartChoice("ConfigureAura");

            _ageSection = root.Q("AgeSection");
            _genderSection = root.Q("GenderSection");
            _personaSection = root.Q("PersonaSection");
            _lblPersonaQuestion = root.Q<Label>("LblPersonaQuestion");
            _btnPersonaMystic = root.Q<Button>("BtnPersonaMystic");
            _btnPersonaBusiness = root.Q<Button>("BtnPersonaBusiness");
            _btnPersonaCasual = root.Q<Button>("BtnPersonaCasual");
            _btnPersonaTech = root.Q<Button>("BtnPersonaTech");


            if (_btnPersonaMystic != null) _btnPersonaMystic.clicked += () => { Debug.Log("[InitiationRitual] 🌌 Mystic Clicked"); SetPersona(OracleTone.Mystic); };
            if (_btnPersonaBusiness != null) _btnPersonaBusiness.clicked += () => { Debug.Log("[InitiationRitual] 💼 Business Clicked"); SetPersona(OracleTone.Business); };
            if (_btnPersonaCasual != null) _btnPersonaCasual.clicked += () => { Debug.Log("[InitiationRitual] 🎈 Casual Clicked"); SetPersona(OracleTone.Casual); };
            if (_btnPersonaTech != null) _btnPersonaTech.clicked += () => { Debug.Log("[InitiationRitual] 🦾 Tech Clicked"); SetPersona(OracleTone.Tech); };

            _locationSection = root.Q("LocationSection");
            _smartLocationContainer = root.Q("SmartLocationContainer");
            _searchContainer = root.Q("SearchContainer");
            _lblSmartLocationText = root.Q<Label>("LblSmartLocationText");
            _btnSmartLocYes = root.Q<Button>("BtnSmartLocYes");
            _btnSmartLocNo = root.Q<Button>("BtnSmartLocNo");
            _locResults = root.Q("LocationResults");
            _locPlaceholder = root.Q<Label>("LocPlaceholder");
            _inputLocation = root.Q<TextField>("InputLocation");

            if (_btnSmartLocYes != null)
            {
                _btnSmartLocYes.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] ✅ Smart Location Accepted");
                    _locationConfirmed = true;
                };
            }
            if (_btnSmartLocNo != null)
            {
                _btnSmartLocNo.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] ❌ Smart Location Rejected. Falling back to manual.");
                    if (_smartLocationContainer != null) _smartLocationContainer.style.display = DisplayStyle.None;
                    if (_searchContainer != null) _searchContainer.style.display = DisplayStyle.Flex;
                    if (_btnLocate != null) _btnLocate.style.display = DisplayStyle.Flex;
                };
            }
            
            _sliderAge = root.Q<SliderInt>("SliderAge");
            _lblAgeValue = root.Q<Label>("LblAgeValue");
            _btnConfirmAge = root.Q<Button>("BtnConfirmAge");
            
            _scaleSection = root.Q("ScaleSection");
            _sliderScale = root.Q<SliderInt>("SliderScale");
            _lblScaleValue = root.Q<Label>("LblScaleValue");
            _lblSampleText = root.Q<Label>("LblSampleText");
            _btnConfirmScale = root.Q<Button>("BtnConfirmScale");
            
            _btnBack = root.Q<Button>("BtnRitualBack");

            if (_btnConfirmAge != null) 
            {
                _btnConfirmAge.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] ✅ Age Confirmed Clicked");
                    _ageConfirmed = true;
                };
            }
            if (_btnConfirmScale != null)
            {
                _btnConfirmScale.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] ✅ Scale Confirmed Clicked");
                    _scaleConfirmed = true;
                };
            }
            if (_btnBack != null) 
            {
                _btnBack.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] ⬅️ Back Requested");
                    _backRequested = true;
                };
            }

            _btnGenderM = root.Q<Button>("BtnGenderM");
            _btnGenderF = root.Q<Button>("BtnGenderF");
            _btnGenderX = root.Q<Button>("BtnGenderX");
            _lblGenderM = root.Q<Label>("LblGenderM");
            _lblGenderF = root.Q<Label>("LblGenderF");
            _lblGenderX = root.Q<Label>("LblGenderX");
            
            _visageSection = root.Q("VisageSection");
            _imgVisage = root.Q("ImgVisage");
            _btnConfirmVisage = root.Q<Button>("BtnConfirmVisage");
            _btnPickGallery = root.Q<Button>("BtnPickGallery");
            _btnPickCamera = root.Q<Button>("BtnPickCamera");

            if (_imgVisage != null)
            {
                _imgVisage.RegisterCallback<ClickEvent>(e => _ = RequestVisageAsync(true));
            }
            if (_btnPickGallery != null) _btnPickGallery.clicked += () => _ = RequestVisageAsync(true);
            if (_btnPickCamera != null) _btnPickCamera.clicked += () => _ = RequestVisageAsync(false);
            
            if (_btnConfirmVisage != null)
            {
                _btnConfirmVisage.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] ✅ Visage Confirmed Clicked");
                    _visageConfirmed = true;
                };
            }

            _inputLocation = root.Q<TextField>("InputLocation");
            _locPlaceholder = root.Q<Label>("LocPlaceholder");
            _btnLocate = root.Q<Button>("BtnLocate");
            if (_btnLocate != null) 
            {
                _btnLocate.clicked += () => _ = SearchLocationAsync(_inputLocation?.value ?? "", true);
                _btnLocate.SetEnabled(false);
            }

            _nameSection = root.Q("NameSection");
            _inputName = root.Q<TextField>("InputName");
            _btnConfirmName = root.Q<Button>("BtnConfirmName");
            _btnSkipName = root.Q<Button>("BtnSkipName");

            if (_btnConfirmName != null) 
            {
                _btnConfirmName.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] ✅ Name Confirmed Clicked");
                    _nameConfirmed = true;
                };
            }
            if (_btnSkipName != null) 
            {
                _btnSkipName.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] 👤 Name Skipped");
                    _nameConfirmed = true; 
                    _isSkippingName = true; 
                };
            }

            // --- Validation Logs ---
            if (_pillars == null || _pillars.Length == 0) Debug.LogError("[InitiationRitual] ‼️ AuraPillarSO NOT FOUND in DI! Check Resources/Settings/Pillars.");
            if (_ritualContainer == null) Debug.LogError("[InitiationRitual] ‼️ RitualContainer NOT FOUND in UXML!");
            if (_pillarContainer == null) Debug.LogError("[InitiationRitual] ‼️ PillarContainer NOT FOUND in UXML!");
            if (_ageSection == null) Debug.LogError("[InitiationRitual] ‼️ AgeSection NOT FOUND in UXML!");
            if (_genderSection == null) Debug.LogError("[InitiationRitual] ‼️ GenderSection NOT FOUND in UXML!");
            if (_locationSection == null) Debug.LogError("[InitiationRitual] ‼️ LocationSection NOT FOUND in UXML!");
            if (_btnBack == null) Debug.LogWarning("[InitiationRitual] ⚠️ BtnRitualBack not found.");

            if (_sliderAge != null)
            {
                _sliderAge.RegisterValueChangedCallback(evt => UpdateAgeLabel(evt.newValue));
            }

            if (_btnConfirmAge != null) 
            {
                _btnConfirmAge.clicked += () => 
                {
                    Debug.Log("[InitiationRitual] 🕯️ Age Confirmed click detected.");
                    _ageConfirmed = true;
                };
            }

            if (_sliderScale != null)
            {
                _sliderScale.RegisterValueChangedCallback(evt => {
                    float s = IndexToScale(evt.newValue);
                    _uiManager?.SetGlobalScale(s);
                    UpdateScaleLabel(evt.newValue);
                });
                
                // Set initial value based on index
                int initialIndex = ScaleToIndex(_uiManager?.GlobalScale ?? 1.0f);
                _sliderScale.value = initialIndex;
                
                // FORCE UPDATE LOCALIZATION AND LABELS IMMEDIATELY
                UpdateLocalization();
                UpdateScaleLabel(initialIndex);
                UpdateAgeLabel(_sliderAge != null ? _sliderAge.value : 25); 
            }
            if (_btnGenderM != null) _btnGenderM.clicked += () => _selectedGender = "male";
            if (_btnGenderF != null) _btnGenderF.clicked += () => _selectedGender = "female";
            if (_btnGenderX != null) _btnGenderX.clicked += () => _selectedGender = "non-binary";
            if (_btnBack != null) _btnBack.clicked += () => _backRequested = true;

            if (_inputLocation != null)
            {
                _inputLocation.RegisterValueChangedCallback(evt => {
                    if (_locPlaceholder != null)
                        _locPlaceholder.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
                    
                    if (_btnLocate != null)
                        _btnLocate.SetEnabled(!string.IsNullOrEmpty(evt.newValue) && evt.newValue.Trim().Length >= 2);

                    if (!_isProgrammaticChange)
                        _ = SearchLocationAsync(evt.newValue, false);
                });
            }
        }

        private bool _isProgrammaticChange;
        private CancellationTokenSource _searchCts;
        private async UniTask SearchLocationAsync(string query, bool immediate = true)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            
            try 
            {
                _vfxController?.UpdateAuraColors(new List<Color> { new Color(0.8f, 0.7f, 0.2f, 0.5f) }); // Soft pulsing color
                
                if (!immediate)
                    await UniTask.Delay(500, cancellationToken: _searchCts.Token);
                    
                var results = await _geocodingService.SearchLocationAsync(query);
                
                if (results == null || results.Count == 0)
                {
                    if (!string.IsNullOrEmpty(query) && query.Length >= 3)
                    {
                        string mysticalFail = _localization.Get("msg.location_not_found", 
                            "This Realm is hidden from my sight. The currents of Aether are turbulent here... Try naming the nearest large city.");
                        await _typeOracleText(mysticalFail);
                    }
                }
                
                DisplayLocationResults(results);
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Return to a calm, steady state
                _vfxController?.UpdateAuraColors(new List<Color> { new Color(0.1f, 0.1f, 0.2f, 0.3f) }); 
            }
        }

        private void DisplayLocationResults(List<LocationResult> results)
        {
            if (_locResults == null) return;
            _locResults.Clear();
            
            if (results == null || results.Count == 0)
            {
                _locResults.style.display = DisplayStyle.None;
                return;
            }

            _locResults.style.display = DisplayStyle.Flex;
            foreach (var res in results)
            {
                var btn = new Button { text = res.DisplayName };
                btn.AddToClassList("location-result-btn");
                btn.clicked += () => SelectLocation(res);
                _locResults.Add(btn);
            }
        }

        private void SelectLocation(LocationResult res)
        {
            if (_authManager.CurrentProfile != null)
            {
                var p = _authManager.CurrentProfile;
                p.LocationZone = res.DisplayName;
                p.Latitude = res.Latitude;
                p.Longitude = res.Longitude;
                
                _isProgrammaticChange = true;
                if (_inputLocation != null) _inputLocation.value = res.DisplayName;
                _isProgrammaticChange = false;
                
                if (_locResults != null) _locResults.style.display = DisplayStyle.None;
                _locationConfirmed = true;
                
                Debug.Log($"<color=#FFD700><b>[PULSE]</b></color> <color=#00FFFF><b>LOCATION BOUND:</b></color> {p.LocationZone}");
            }
        }

        private void ToggleCategory(ref bool value, Button btn)
        {
            value = !value;
            if (value) btn.AddToClassList("ritual-btn--active");
            else btn.RemoveFromClassList("ritual-btn--active");
            _vfxController?.PlayFlash(value ? Color.white : Color.gray);
        }

        public async UniTask<bool> RunRitualAsync(Features.Data.UserProfile profile)
        {
            _currentProfile = profile;
            if (_ritualContainer != null) _ritualContainer.style.display = DisplayStyle.Flex;
            _currentStep = RitualStep.QuickStart;

            while (_currentStep < RitualStep.Complete)
            {
                Debug.Log($"[InitiationRitual] 🌀 Current Step: {_currentStep}");
                _backRequested = false;
                if (_btnBack != null) _btnBack.style.visibility = (_currentStep == RitualStep.Persona) ? Visibility.Hidden : Visibility.Visible;
                
                // Smoothly transition between sections
                await TransitionSectionAsync(GetSectionForStep(_currentStep), profile);

                switch (_currentStep)
                {
                    case RitualStep.QuickStart:
                        await RunQuickStartChoiceStep(profile);
                        break;
                    case RitualStep.Scale:
                        await RunScaleStep(profile);
                        break;
                    case RitualStep.Persona:
                        await RunPersonaStep(profile);
                        break;
                    case RitualStep.Age:
                        await RunAgeStep(profile);
                        break;
                    case RitualStep.Gender:
                        await RunGenderStep(profile);
                        break;
                    case RitualStep.Visage:
                        await RunVisageStep(profile);
                        break;
                    case RitualStep.Location:
                        await RunLocationStep(profile);
                        break;
                    case RitualStep.Name:
                        await RunNameStep(profile);
                        break;
                }

                if (_backRequested)
                {
                    if ((int)_currentStep > 0)
                    {
                        _currentStep--;
                        if (_vfxController != null) _vfxController.PlayFlash(Color.gray);
                
                        if (_locPlaceholder != null) _locPlaceholder.text = _localization.Get(AuraTerms.INIT_LOC_PLACEHOLDER, "SEARCH YOUR REALM...");
                        await UniTask.Delay(300);
                    }
                    else
                    {
                        Debug.Log("[InitiationRitual] 🚪 Aborting Ritual. Returning to Initiation Gateway.");
                        if (_ritualContainer != null) await FadeOutElement(_ritualContainer);
                        if (_ritualContainer != null) _ritualContainer.style.display = DisplayStyle.None;
                        return false;
                    }
                }
                else
                {
                    _currentStep++;
                }
                
                await UniTask.Yield();
            }

            if (_ritualContainer != null) await FadeOutElement(_ritualContainer);
            _ritualContainer.style.display = DisplayStyle.None;
            
            // Task 1: Meaningful pause at the end of the ritual
            string finalBlessing = _localization.GetPersonaString(AuraTerms.INIT_FINAL_BLESSING, profile.OracleTone, "Your path is sealed. Welcome.");
            await _typeOracleText(finalBlessing);
            
            // Add a subtle hint that we are moving forward
            await UniTask.Delay(2000);
            await _typeOracleText(_localization.Get("msg.convergence_begins", "The convergence begins..."));
            await UniTask.Delay(1500); 
            return true;
        }

        private void ExecuteQuickStartChoice(string choice)
        {
            Debug.Log($"[InitiationRitual] ⚡ Quick Start Choice: {choice}");
            _quickStartChoice = choice;
        }

        private async UniTask RunQuickStartChoiceStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.ORACLE_QUICKSTART, profile.OracleTone, "The path is open. Choose your destiny."));
            if (_quickStartSection != null) _quickStartSection.style.display = DisplayStyle.Flex;

            _quickStartChoice = null;
            while (string.IsNullOrEmpty(_quickStartChoice) && !_backRequested) await UniTask.Yield();
            
            if (_quickStartChoice == "FindMaster" || _quickStartChoice == "AskOracle")
            {
                // Assign temporary name if anonymous
                profile.DisplayName = "Adept-" + UnityEngine.Random.Range(1000, 9999);
                profile.IsAnonymous = true;
                
                // Skip the rest of the ritual
                _currentStep = RitualStep.Complete; 
                
                // Save destination to jump immediately after tutorial/scene transition
                PlayerPrefs.SetString("QuickStart_Destination", _quickStartChoice);
                PlayerPrefs.Save();
            }
        }

        private async UniTask RunScaleStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.ORACLE_SCALE_INTRO, profile.OracleTone, "Before we begin, adjust your spiritual sight."));
            if (_scaleSection != null) _scaleSection.style.display = DisplayStyle.Flex;
            if (_btnConfirmScale != null) _btnConfirmScale.text = _localization.GetPersonaString(AuraTerms.BTN_ALIGN_SIGHT, profile.OracleTone, "ALIGN SIGHT").ToUpper();
            if (_lblSampleText != null) _lblSampleText.text = _localization.GetPersonaString(AuraTerms.MSG_SAMPLE_TEXT, profile.OracleTone, "The Aura resonates through time and space.");
            
            _scaleConfirmed = false;
            while (!_scaleConfirmed && !_backRequested) await UniTask.Yield();
        }

        private async UniTask RunPersonaStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.ORACLE_PERSONA_QUESTION, profile.OracleTone, "Choose the voice that speaks to your soul..."));
            if (_personaSection != null) _personaSection.style.display = DisplayStyle.Flex;
            
            if (_lblPersonaQuestion != null) _lblPersonaQuestion.text = _localization.GetPersonaString(AuraTerms.ORACLE_PERSONA_QUESTION, profile.OracleTone, "Choose your resonance style");
            if (_btnPersonaMystic != null) _btnPersonaMystic.text = "🌌 " + _localization.GetPersonaString(AuraTerms.PERSONA_MYSTIC, profile.OracleTone, "MYSTIC");
            if (_btnPersonaBusiness != null) _btnPersonaBusiness.text = "💼 " + _localization.GetPersonaString(AuraTerms.PERSONA_BUSINESS, profile.OracleTone, "BUSINESS");
            if (_btnPersonaCasual != null) _btnPersonaCasual.text = "🎈 " + _localization.GetPersonaString(AuraTerms.PERSONA_CASUAL, profile.OracleTone, "CASUAL");
            if (_btnPersonaTech != null) _btnPersonaTech.text = "🦾 " + _localization.GetPersonaString(AuraTerms.PERSONA_TECH, profile.OracleTone, "TECH");

            _personaConfirmed = false;
            while (!_personaConfirmed && !_backRequested) await UniTask.Yield();
        }

        private async UniTask RunAgeStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.INIT_AGE_QUESTION, profile.OracleTone, "How many winter cycles has your spark burned?"));
            if (_ageSection != null) _ageSection.style.display = DisplayStyle.Flex;
            if (_btnConfirmAge != null) _btnConfirmAge.text = _localization.GetPersonaString(AuraTerms.BTN_CONFIRM_AGE, profile.OracleTone, "CONFIRM AGE").ToUpper();
            
            _ageConfirmed = false;
            while (!_ageConfirmed && !_backRequested) await UniTask.Yield();
            if (!_backRequested && _sliderAge != null) profile.Age = _sliderAge.value;
        }

        private async UniTask RunGenderStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.INIT_GENDER_QUESTION, profile.OracleTone, "What energy do you bring to the Nexus?"));
            if (_genderSection != null) _genderSection.style.display = DisplayStyle.Flex;
            
            if (_lblGenderM != null) _lblGenderM.text = _localization.GetPersonaString(AuraTerms.INIT_GENDER_M, profile.OracleTone, "MALE");
            if (_lblGenderF != null) _lblGenderF.text = _localization.GetPersonaString(AuraTerms.INIT_GENDER_F, profile.OracleTone, "FEMALE");
            if (_lblGenderX != null) _lblGenderX.text = _localization.GetPersonaString(AuraTerms.INIT_GENDER_X, profile.OracleTone, "NEUTRAL");

            _selectedGender = null;
            while (string.IsNullOrEmpty(_selectedGender) && !_backRequested) await UniTask.Yield();
            if (!_backRequested) profile.Gender = _selectedGender;
        }

        private bool _visageConfirmed;
        private async UniTask RunVisageStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.INIT_VISAGE_START, profile.OracleTone, "Capture your essence. How shall the Nexus see you?"));
            if (_visageSection != null) _visageSection.style.display = DisplayStyle.Flex;
            
            _visageConfirmed = false;
            while (!_visageConfirmed && !_backRequested) await UniTask.Yield();
        }

        private async UniTask RequestVisageAsync(bool fromGallery)
        {
            var data = fromGallery ? await _mediaService.PickImageAsync() : await _mediaService.TakePhotoAsync();
            
            if (data != null && data.Length > 0)
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(data);
                if (_imgVisage != null)
                {
                    _imgVisage.style.backgroundImage = new StyleBackground(tex);
                    // Find icon in parent since it's a sibling
                    var icon = _imgVisage.parent?.Q<Label>(null, "visage-icon");
                    if (icon != null) icon.style.display = DisplayStyle.None;
                }
                
                if (_authManager.CurrentProfile != null)
                    _authManager.CurrentProfile.VisageData = data;
                
                _vfxController?.PlayFlash(Color.white);
                _hapticService?.MediumTap();
            }
        }

        private async UniTask RunLocationStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.INIT_LOCATION_QUESTION, profile.OracleTone, "Where is your Aura strongest in space?"));
            if (_locationSection != null) _locationSection.style.display = DisplayStyle.Flex;
            
            // Smart Geolocation Trigger
            if (_smartLocationContainer != null) _smartLocationContainer.style.display = DisplayStyle.None;
            if (_searchContainer != null) _searchContainer.style.display = DisplayStyle.None;
            if (_btnLocate != null) _btnLocate.style.display = DisplayStyle.None;
            
            _locationConfirmed = false;
            
            // Auto-detect location
            var locData = await _locationService.RequestLocationAsync();
            if (locData.Latitude != 0 || locData.Longitude != 0)
            {
                string city = await _geocodingService.ReverseGeocodeAsync(locData.Latitude, locData.Longitude);
                if (!string.IsNullOrEmpty(city))
                {
                    profile.Latitude = locData.Latitude;
                    profile.Longitude = locData.Longitude;
                    profile.LocationZone = city;

                    if (_smartLocationContainer != null) _smartLocationContainer.style.display = DisplayStyle.Flex;
                    if (_lblSmartLocationText != null) _lblSmartLocationText.text = $"Ми відчуваємо твою енергію тут: {city}. Це твоє царство?";
                }
                else
                {
                    ShowManualLocationSearch();
                }
            }
            else
            {
                ShowManualLocationSearch();
            }

            while (!_locationConfirmed && !_backRequested) await UniTask.Yield();
        }
        
        private void ShowManualLocationSearch()
        {
            if (_searchContainer != null) _searchContainer.style.display = DisplayStyle.Flex;
            if (_btnLocate != null) _btnLocate.style.display = DisplayStyle.Flex;
            if (_locPlaceholder != null) _locPlaceholder.text = _localization.GetPersonaString(AuraTerms.INIT_LOC_PLACEHOLDER, _currentProfile?.OracleTone ?? OracleTone.Business, "SEARCH YOUR REALM...");
            if (_btnLocate != null) 
            {
                _btnLocate.text = _localization.GetPersonaString(AuraTerms.BTN_ILLUMINATE, _currentProfile?.OracleTone ?? OracleTone.Business, "ILLUMINATE MY PRESENCE").ToUpper();
                _btnLocate.SetEnabled(!string.IsNullOrEmpty(_inputLocation?.value) && _inputLocation.value.Length >= 2);
            }
        }

        private async UniTask RunNameStep(Features.Data.UserProfile profile)
        {
            await _typeOracleText(_localization.GetPersonaString(AuraTerms.INIT_NAME_QUESTION, profile.OracleTone, "What name shall the Constellations remember?"));
            if (_nameSection != null) _nameSection.style.display = DisplayStyle.Flex;
            
            if (_btnConfirmName != null) _btnConfirmName.text = _localization.GetPersonaString(AuraTerms.BTN_REVEAL, profile.OracleTone, "REVEAL");
            
            _nameConfirmed = false;
            _isSkippingName = false;
            
            while (!_nameConfirmed && !_backRequested) await UniTask.Yield();
            
            if (!_backRequested)
            {
                if (_isSkippingName)
                {
                    profile.DisplayName = "Adept-" + UnityEngine.Random.Range(1000, 9999);
                    profile.IsAnonymous = true;
                    await _typeOracleText(_localization.GetPersonaString(AuraTerms.INIT_SHADOW_PATH, profile.OracleTone, "You have chosen the shadow. Your Aura will be dim, and finding Symmetry will be harder."));
                    await UniTask.Delay(3000);
                }
                else
                {
                    string name = _inputName?.value ?? "Unknown";
                    if (string.IsNullOrWhiteSpace(name)) name = "Adept-" + UnityEngine.Random.Range(1000, 9999);
                    profile.DisplayName = name;
                    profile.IsAnonymous = false;
                }
            }
        }

        private async UniTask RunGiftsStep(Features.Data.UserProfile profile)
        {
            // Removed as per user request - Pillars will be chosen in Vault/Profile
            await UniTask.CompletedTask;
        }



        private string _selectedPillarId;

        private void GeneratePillarChips(bool isGift)
        {
            if (_pillarContainer == null || _pillars == null) return;
            
            _pillarContainer.Clear();
            foreach (var pillar in _pillars)
            {
                var btn = new Button();
                btn.AddToClassList("pillar-chip");
                
                // Dynamic Aesthetic Wiring
                btn.style.borderLeftColor = pillar.ThemeColor;
                btn.style.borderRightColor = pillar.ThemeColor;
                btn.style.borderTopColor = pillar.ThemeColor;
                btn.style.borderBottomColor = pillar.ThemeColor;
                
                var bgColor = pillar.ThemeColor;
                bgColor.a = 0.05f; // Glassmorphism tint
                btn.style.backgroundColor = bgColor;

                if (pillar.Icon != null)
                {
                    var iconEl = new VisualElement();
                    iconEl.AddToClassList("pillar-chip__icon");
                    iconEl.style.backgroundImage = new StyleBackground(pillar.Icon);
                    iconEl.style.unityBackgroundImageTintColor = pillar.ThemeColor;
                    btn.Add(iconEl);
                }

                var label = new Label(_localization.Get(pillar.LocalizationKey, pillar.Id).ToUpper());
                label.AddToClassList("pillar-chip__label");
                btn.Add(label);

                btn.clicked += () => {
                    _selectedPillarId = pillar.Id;
                    _vfxController?.PlayFlash(pillar.ThemeColor);
                    UpdatePillarSelectionUI(btn, pillar.ThemeColor);
                };
                
                _pillarContainer.Add(btn);
            }
        }

        private void UpdatePillarSelectionUI(Button selectedBtn, Color theme)
        {
            _pillarContainer.Query<Button>(className: "pillar-chip").ForEach(b => {
                b.RemoveFromClassList("pillar-chip--active");
                var bg = b.style.backgroundColor.value;
                bg.a = 0.05f;
                b.style.backgroundColor = bg;
            });
            
            selectedBtn.AddToClassList("pillar-chip--active");
            var activeBg = theme;
            activeBg.a = 0.2f; // Stronger glow when selected
            selectedBtn.style.backgroundColor = activeBg;
        }

        private void SelectGender(string gender, Button btn)
        {
            if (_authManager.CurrentProfile != null)
                _authManager.CurrentProfile.Gender = gender;
            
            btn.parent.Query<Button>(className: "ritual-btn").ForEach(b => b.RemoveFromClassList("ritual-btn--active"));
            btn.AddToClassList("ritual-btn--active");
        }

        private async UniTask HandleLocationRitual()
        {
            if (_btnLocate == null || _locationService == null) return;
            
            _btnLocate.SetEnabled(false);
            string originalText = _btnLocate.text;
            _btnLocate.text = _localization.Get(AuraTerms.LOADING, "ILLUMINATING...");
            
            Debug.Log("<color=#FFD700><b>[PULSE]</b></color> <color=#00FFFF><b>LOCATION RITUAL:</b></color> Calling the Sacred Compass...");
            
            var locData = await _locationService.RequestLocationAsync();
            
            if (_authManager.CurrentProfile != null)
            {
                var profile = _authManager.CurrentProfile;
                profile.Latitude = locData.Latitude;
                profile.Longitude = locData.Longitude;
                profile.LocationZone = locData.Zone;
                
                Debug.Log($"<color=#FFD700><b>[PULSE]</b></color> <color=#00FFFF><b>LOCATION BOUND:</b></color> {profile.LocationZone} ({profile.Latitude:F4}, {profile.Longitude:F4})");
            }
            
            _btnLocate.text = _localization.Get(AuraTerms.INIT_LOCATION_SUCCESS, "PRESENCE ILLUMINATED");
            await UniTask.Delay(1000);
        }
        private VisualElement GetSectionForStep(RitualStep step)
        {
            return step switch
            {
                RitualStep.QuickStart => _quickStartSection,
                RitualStep.Scale => _scaleSection,
                RitualStep.Persona => _personaSection,
                RitualStep.Age => _ageSection,
                RitualStep.Gender => _genderSection,
                RitualStep.Visage => _visageSection,
                RitualStep.Location => _locationSection,
                RitualStep.Name => _nameSection,
                _ => null
            };
        }

        private async UniTask TransitionSectionAsync(VisualElement nextSection, Features.Data.UserProfile profile)
        {
            // 1. Fade out current active section if any
            var sections = new[] { _quickStartSection, _scaleSection, _personaSection, _ageSection, _genderSection, _visageSection, _locationSection, _nameSection };
            foreach (var sec in sections)
            {
                if (sec != null && sec.style.display == DisplayStyle.Flex && sec != nextSection)
                {
                    await FadeOutElement(sec);
                    sec.style.display = DisplayStyle.None;
                }
            }

            // 2. Fade in next section
            if (nextSection != null)
            {
                nextSection.style.opacity = 0;
                nextSection.style.display = DisplayStyle.Flex;
                await FadeInElement(nextSection);
            }
        }

        private async UniTask FadeInElement(VisualElement el, float duration = 0.5f)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                el.style.opacity = Mathf.Lerp(0, 1, elapsed / duration);
                await UniTask.Yield();
            }
            el.style.opacity = 1;
        }

        private async UniTask FadeOutElement(VisualElement el, float duration = 0.3f)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                el.style.opacity = Mathf.Lerp(1, 0, elapsed / duration);
                await UniTask.Yield();
            }
            el.style.opacity = 0;
        }

        private void UpdateAgeLabel(int age)
        {
            if (_lblAgeValue != null)
                _lblAgeValue.text = $"{age} " + _localization.Get(AuraTerms.INIT_AGE_UNIT, "winter cycles");
        }

        private void UpdateScaleLabel(int index)
        {
            if (_lblScaleValue == null || _localization == null) return;
            var tone = _currentProfile?.OracleTone ?? OracleTone.Business;
            
            string label = index switch
            {
                0 => _localization.GetPersonaString(AuraTerms.LBL_SCALE_SMALL, tone, "SMALL"),
                2 => _localization.GetPersonaString(AuraTerms.LBL_SCALE_LARGE, tone, "LARGE"),
                _ => _localization.GetPersonaString(AuraTerms.LBL_SCALE_MEDIUM, tone, "MEDIUM")
            };
            
            _lblScaleValue.text = label + " " + _localization.GetPersonaString(AuraTerms.LBL_VISION, tone, "VISION");
            if (_lblSampleText != null) _lblSampleText.text = _localization.GetPersonaString(AuraTerms.MSG_SAMPLE_TEXT, tone, "The Aura resonates through time and space.");
        }

        private float IndexToScale(int index)
        {
            return index switch
            {
                0 => 0.8f,
                2 => 1.3f,
                _ => 1.0f
            };
        }

        private void UpdateLocalization()
        {
            if (_localization == null) return;

            var tone = _localization.CurrentTone;
            var root = GetComponent<UIDocument>().rootVisualElement;
            
            var lblInput = root.Q<Label>("InputLabel");
            if (lblInput != null) lblInput.text = _localization.GetPersonaString(AuraTerms.PHONE_NUMBER, tone, "PHONE NUMBER").ToUpper();

            var lblPlaceholder = root.Q<Label>("PlaceholderLabel");
            if (lblPlaceholder != null) lblPlaceholder.text = _localization.GetPersonaString(AuraTerms.PHONE_PLACEHOLDER, tone, "+380 00 000 0000");

            var btnInitiate = root.Q<Button>("BtnInitiate");
            if (btnInitiate != null) btnInitiate.text = _localization.GetPersonaString(AuraTerms.LOGIN, tone, "INITIATE").ToUpper();

            var btnConfirmAge = root.Q<Button>("BtnConfirmAge");
            if (btnConfirmAge != null) btnConfirmAge.text = _localization.GetPersonaString(AuraTerms.BTN_CONFIRM_AGE, tone, "CONFIRM").ToUpper();

            var btnConfirmScale = root.Q<Button>("BtnConfirmScale");
            if (btnConfirmScale != null) btnConfirmScale.text = _localization.GetPersonaString(AuraTerms.BTN_ALIGN_SIGHT, tone, "ALIGN SIGHT").ToUpper();

            var lblProphecy = root.Q<Label>("ProphecyLabel");
            if (lblProphecy != null) lblProphecy.text = _localization.GetPersonaString(AuraTerms.INIT_PROPHECY_WAIT, tone, "Awaiting the sacred breath...");

            var btnFindMaster = root.Q<Button>("BtnQuickStartFindMaster");
            if (btnFindMaster != null) btnFindMaster.text = "🔍 " + _localization.GetPersonaString("btn.find_master", tone, "FIND MASTER");
            
            var btnAskOracle = root.Q<Button>("BtnQuickStartAskOracle");
            if (btnAskOracle != null) btnAskOracle.text = "👁️ " + _localization.GetPersonaString("btn.ask_oracle", tone, "ASK ORACLE");
            
            var btnConfigAura = root.Q<Button>("BtnQuickStartConfigureAura");
            if (btnConfigAura != null) btnConfigAura.text = "✨ " + _localization.GetPersonaString("btn.config_aura", tone, "CONFIGURE AURA");
        }

        private int ScaleToIndex(float scale)
        {
            if (scale <= 0.9f) return 0;
            if (scale >= 1.2f) return 2;
            return 1;
        }
    }
}
