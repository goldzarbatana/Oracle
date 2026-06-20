using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Localization;
using TimeAura.Core.Services;
using TimeAura.Core.Data.SO;
using TimeAura.Features.Localization;
using TimeAura.Features.UI.Oracle;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// AuraView - Handles the visual representation of the Mirror of Aura.
    /// Binds UXML elements to the AuraPresenter.
    /// </summary>
    public class AuraView
    {
        private readonly VisualElement _root;
        private readonly AuraPresenter _presenter;
        private readonly LocalizationManager _localization;
        private readonly OraclePromptFactory _promptFactory;
        private readonly OracleSO[] _oracles;
        private readonly AudioService _audioService;
        private readonly HapticService _hapticService;
        private readonly OracleWhisperManager _whisperManager;

        private OracleSelectionController _oracleSelectionController;

        private VisualElement _containerGifts;
        private VisualElement _containerSeeks;
        private VisualElement _containerSelected;
        private Button _btnLaunchResonance;
        private Label _header, _lblGifts, _lblSeeks, _lblCustom, _lblVisageHint, _lblAuraTitle, _lblMinAge, _lblMaxAge, _lblDistance, _lblOracleWhisper, _lblOraclePrompt, _lblOraclePlaceholder;
        private SliderInt _sliderMinAge, _sliderMaxAge;
        private Slider _sliderDistance;
        private VisualElement _visagePlaceholder, _loadingOverlay;
        private VisualElement _tabUnified, _tabCompass;
        private Button _btnTabUnified, _btnTabCompass;
        private TextField _inputOracleThoughts;
        private Button _btnConsultOracle;
        private ScrollView _pillarManifestSelector, _pillarIntentSelector;
        private AuraPillarSO _activePillarManifest, _activePillarIntent;

        private List<string> _tagLibrary => _presenter.Pillars?.SelectMany(p => p.tags ?? new List<string>()).Distinct().ToList() ?? new List<string>();

        public event Action OnLaunchResonance;

        public AuraView(VisualElement root, AuraPresenter presenter, LocalizationManager localization,
            OraclePromptFactory promptFactory, OracleSO[] oracles, AudioService audioService, HapticService hapticService,
            OracleWhisperManager whisperManager)
        {
            _root = root;
            _presenter = presenter;
            _localization = localization;
            _promptFactory = promptFactory;
            _oracles = oracles;
            _audioService = audioService;
            _hapticService = hapticService;
            _whisperManager = whisperManager;

            if (_root == null)
            {
                Debug.LogError("[Aura] ❌ Root VisualElement is NULL! UI cannot bind.");
                return;
            }

            Debug.Log("[Aura] 🌀 Initializing AuraView Ritual...");
            InitializeElements();
            InitializeTabs();
            if (_presenter != null)
            {
                _presenter.OnDataChanged += RefreshVisuals;
                _presenter.OnError += (msg) => 
                {
                    Debug.LogWarning($"[Aura] ⚠️ Oracle Warning: {msg}");
                    if (OracleWidgetController.Instance != null)
                    {
                        OracleWidgetController.Instance.ShowHint($"⚠️ {msg}");
                    }
                };
            }
        }

        private void InitializeElements()
        {
            if (_root == null) return;
            // Lookup by new names
            _header = _root.Q<Label>("AuraHeader");
            _containerGifts = _root.Q("GiftsContainer");
            _containerSeeks = _root.Q("SeeksContainer");
            _containerSelected = _root.Q("SelectedTagsList");
            _btnLaunchResonance = _root.Q<Button>("BtnLaunchResonance");

            _sliderMinAge = _root.Q<SliderInt>("SliderMinAge");
            _sliderMaxAge = _root.Q<SliderInt>("SliderMaxAge");
            _sliderDistance = _root.Q<Slider>("SliderDistance");

            _lblMinAge = _root.Q<Label>("LblMinAge");
            _lblMaxAge = _root.Q<Label>("LblMaxAge");
            _lblDistance = _root.Q<Label>("LblDistance");
            _lblOracleWhisper = _root.Q<Label>("LblOracleWhisper");
            _lblOraclePrompt = _root.Q<Label>("LblOraclePrompt");
            
            _inputOracleThoughts = _root.Q<TextField>("InputOracleThoughts");
            _btnConsultOracle = _root.Q<Button>("BtnConsultOracle");

            _lblGifts = _root.Q<Label>("LblGifts");
            _lblSeeks = _root.Q<Label>("LblSeeks");
            _lblCustom = _root.Q<Label>("LblCustom");
            _lblVisageHint = _root.Q<Label>("LblVisageHint");
            _visagePlaceholder = _root.Q("VisagePlaceholder");
            _lblAuraTitle = _root.Q<Label>("LblAuraTitle");
            _loadingOverlay = _root.Q("LoadingOverlay");

            // Tabs
            _btnTabUnified = _root.Q<Button>("BtnTabUnified");
            _btnTabCompass = _root.Q<Button>("BtnTabCompass");

            _tabUnified = _root.Q("TabContentUnified");
            _tabCompass = _root.Q("TabContentCompass");

            _pillarManifestSelector = _root.Q<ScrollView>("ManifestPillarSelector");
            _pillarIntentSelector = _root.Q<ScrollView>("IntentPillarSelector");

            var btnCloseAura = _root.Q<Button>("BtnCloseAura");
            if (btnCloseAura != null)
            {
                btnCloseAura.clicked += () =>
                {
                    _root.style.display = DisplayStyle.None;
                    _root.style.visibility = Visibility.Hidden;
                    _root.style.opacity = 0f;
                    _root.pickingMode = PickingMode.Ignore;
                    _audioService?.PlaySFX("CrystalClick");
                    _hapticService?.LightTap();
                };
            }

            if (_presenter != null && _presenter.Pillars != null && _presenter.Pillars.Length > 0)
            {
                _activePillarManifest = _presenter.Pillars[0];
                _activePillarIntent = _presenter.Pillars[0];
                _presenter.ActivePillarId = _activePillarManifest.Id;
                ApplyPillarTheme(_activePillarManifest);
            }

            if (_btnLaunchResonance != null)
            {
                _btnLaunchResonance.clicked += async () =>
                {
                    _presenter.SetCustomNote(_inputOracleThoughts?.value);
                    if (await _presenter.ActivateAuraAsync())
                    {
                        _root.style.display = DisplayStyle.None;
                        _root.style.visibility = Visibility.Hidden;
                        _root.style.opacity = 0f;
                        _root.pickingMode = PickingMode.Ignore;
                        OnLaunchResonance?.Invoke();
                    }
                };
            }

            if (_btnConsultOracle != null)
                _btnConsultOracle.clicked += async () => 
                {
                    await _presenter.TriggerOracleAnalysis(_inputOracleThoughts?.value);
                };

            if (_presenter != null)
            {
                _presenter.OnOracleSuggestion += HandleOracleSuggestion;
            }

            if (_inputOracleThoughts != null)
            {
                _lblOraclePlaceholder = _root.Q<Label>("OraclePlaceholder");
                _inputOracleThoughts.RegisterValueChangedCallback(evt => 
                {
                    if (_lblOraclePlaceholder != null)
                        _lblOraclePlaceholder.style.display = string.IsNullOrEmpty(evt.newValue) ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }

            if (_sliderMinAge != null)
                _sliderMinAge.RegisterValueChangedCallback(evt => { _presenter.MinAge = evt.newValue; if (_sliderMaxAge != null && evt.newValue > _sliderMaxAge.value) _sliderMaxAge.value = evt.newValue; });
            
            if (_sliderMaxAge != null)
                _sliderMaxAge.RegisterValueChangedCallback(evt => { _presenter.MaxAge = evt.newValue; if (_sliderMinAge != null && evt.newValue < _sliderMinAge.value) _sliderMinAge.value = evt.newValue; });

            if (_sliderDistance != null)
                _sliderDistance.RegisterValueChangedCallback(evt => _presenter.Distance = evt.newValue);

            if (_presenter != null)
            {
                PopulatePillars();
                PopulateTags();
            }
            else
            {
                Debug.LogWarning("[Aura] ⚠️ AuraPresenter is missing during InitializeElements. Visuals will stay in limbo.");
            }

            // Initialize Oracle Selection Controller
            _oracleSelectionController = new OracleSelectionController(
                _root,
                _presenter,
                _promptFactory,
                _localization,
                _oracles,
                _audioService,
                _hapticService,
                _whisperManager
            );
        }

        private void InitializeTabs()
        {
            if (_btnTabUnified != null) _btnTabUnified.clicked += () => SwitchTab("unified");
            if (_btnTabCompass != null) _btnTabCompass.clicked += () => SwitchTab("compass");
            
            SwitchTab("unified"); // Default
        }

        private void SwitchTab(string tabId)
        {
            // Update Buttons
            _btnTabUnified?.RemoveFromClassList("aura-tab-btn--active");
            _btnTabCompass?.RemoveFromClassList("aura-tab-btn--active");

            if (tabId == "unified") _btnTabUnified?.AddToClassList("aura-tab-btn--active");
            if (tabId == "compass") _btnTabCompass?.AddToClassList("aura-tab-btn--active");

            // Update Content
            if (_tabUnified != null) _tabUnified.style.display = (tabId == "unified") ? DisplayStyle.Flex : DisplayStyle.None;
            if (_tabCompass != null) _tabCompass.style.display = (tabId == "compass") ? DisplayStyle.Flex : DisplayStyle.None;

            Debug.Log($"[AuraView] Switched to Tab: {tabId.ToUpper()}");
        }

        private void PopulatePillars()
        {
            if (_presenter == null || _presenter.Pillars == null) return;

            void FillSelector(ScrollView selector, bool isGift)
            {
                if (selector == null) return;
                selector.contentContainer.Clear();
                foreach (var pillar in _presenter.Pillars)
                {
                    var btn = new Button();
                    btn.text = _localization.Get(pillar.LocalizationKey, pillar.Id).ToUpper();
                    btn.AddToClassList("pillar-btn");
                    
                    if ((isGift && _activePillarManifest == pillar) || (!isGift && _activePillarIntent == pillar))
                        btn.AddToClassList("pillar-btn--active");

                    btn.clicked += () => 
                    {
                        if (isGift) 
                        {
                            _activePillarManifest = pillar;
                            _presenter.ActivePillarId = pillar.Id;
                            ApplyPillarTheme(pillar);
                        }
                        else 
                        {
                            _activePillarIntent = pillar;
                        }
                        
                        PopulatePillars();
                        PopulateTags();
                    };
                    selector.Add(btn);
                }
            }

            FillSelector(_pillarManifestSelector, true);
            FillSelector(_pillarIntentSelector, false);
        }

        private void PopulateTags()
        {
            if (_presenter == null || _containerGifts == null || _containerSeeks == null) return;

            void FillTags(VisualElement container, AuraPillarSO activePillar, bool isGift)
            {
                container.Clear();
                if (activePillar == null || activePillar.tags == null) return;
                foreach (var key in activePillar.tags)
                {
                    container.Add(CreateTagButton(key, isGift));
                }
            }

            FillTags(_containerGifts, _activePillarManifest, true);
            FillTags(_containerSeeks, _activePillarIntent, false);
        }

        private Button CreateTagButton(string key, bool isGift)
        {
            var btn = new Button();
            btn.name = key;
            btn.AddToClassList("tag-base");
            btn.AddToClassList(isGift ? "tag-gift" : "tag-seek");
            
            btn.clicked += () => 
            {
                Debug.Log($"[Aura] 👆 Tag clicked: {key} (Gift: {isGift})");
                if (isGift) _presenter.ToggleGift(key);
                else _presenter.ToggleSeek(key);
                // Immediately update this button's visual
                UpdateTagButton(btn, key, isGift);
            };

            UpdateTagButton(btn, key, isGift);
            btn.pickingMode = PickingMode.Position;
            
            return btn;
        }

        private void UpdateTagButton(Button btn, string key, bool isGift)
        {
            string term = _localization != null ? _localization.Get(key, key) : key;
            btn.text = $"#{term}";
            
            string activeClass = isGift ? "tag-gift--active" : "tag-seek--active";
            if (_presenter != null && _presenter.IsSelected(key, isGift))
                btn.AddToClassList(activeClass);
            else
                btn.RemoveFromClassList(activeClass);
        }

        public void RefreshVisuals()
        {
            if (_localization == null) return;
            
            var tone = _presenter.OracleTone;
            if (_header != null) _header.text = _localization.GetPersonaString(AuraTerms.AURA_HEADER, tone, "RESONATE YOUR AURA").ToUpper();
            if (_lblGifts != null) _lblGifts.text = _localization.GetPersonaString(AuraTerms.MY_GIFTS, tone, "MY GIFTS").ToUpper();
            if (_lblSeeks != null) _lblSeeks.text = _localization.GetPersonaString(AuraTerms.I_SEEK, tone, "I SEEK").ToUpper();
            if (_lblCustom != null) _lblCustom.text = _localization.GetPersonaString(AuraTerms.AURA_CUSTOM, tone, "FORGE YOUR OWN PATH").ToUpper();
            
            var lblCurrent = _root.Q("ActiveSelectionContainer")?.Q<Label>();
            if (lblCurrent != null) lblCurrent.text = _localization.GetPersonaString(AuraTerms.CURRENT_AURA, tone, "YOUR CURRENT AURA...");
            
            if (_btnTabUnified != null) _btnTabUnified.text = _localization.GetPersonaString(AuraTerms.TAB_UNIFIED, tone, "AURA").ToUpper();
            if (_btnTabCompass != null) _btnTabCompass.text = _localization.GetPersonaString(AuraTerms.TAB_COMPASS, tone, "COMPASS").ToUpper();

            if (_lblOraclePrompt != null) _lblOraclePrompt.text = _localization.GetPersonaString(AuraTerms.ORACLE_PROMPT, tone, "MEDITATE WITH THE ORACLE").ToUpper();

            
            if (_inputOracleThoughts != null)
            {
                // UI Toolkit TextField placeholder is often set via attributes or code
                // Using .value as placeholder for now if it's empty, or using internal implementation if available
            }
            
            if (_btnConsultOracle != null) _btnConsultOracle.text = _localization.GetPersonaString(AuraTerms.BTN_CONSULT_ORACLE, tone, "CONSULT THE ORACLE").ToUpper();


            if (_lblAuraTitle != null)
            {
                _lblAuraTitle.text = _localization.Get(_presenter.AuraTitle, _presenter.AuraTitle).ToUpper();
                _lblAuraTitle.style.color = _presenter.GetAuraColor();
            }

            if (_loadingOverlay != null)
            {
                _loadingOverlay.style.display = _presenter.IsBusy ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_lblVisageHint != null)
            {
                _lblVisageHint.text = _localization.Get(AuraTerms.AURA_VISAGE_HINT, "A Visage draws Harmony closer");
                _lblVisageHint.style.display = _presenter.HasVisage ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_visagePlaceholder != null && !_presenter.HasVisage)
            {
                _visagePlaceholder.style.backgroundColor = _presenter.GetAuraColor();
                // Add pulsating animation class if defined in USS
                _visagePlaceholder.AddToClassList("visage-pulse");
            }

            if (_containerGifts != null)
            {
                foreach (Button b in _containerGifts.Children().OfType<Button>())
                    UpdateTagButton(b, b.name, true);
            }

            if (_containerSeeks != null)
            {
                foreach (Button b in _containerSeeks.Children().OfType<Button>())
                    UpdateTagButton(b, b.name, false);
            }

            UpdateSelectedAltar();

            // Sync Filters
            if (_sliderMinAge != null) _sliderMinAge.value = _presenter.MinAge;
            if (_sliderMaxAge != null) _sliderMaxAge.value = _presenter.MaxAge;
            if (_sliderDistance != null) _sliderDistance.value = _presenter.Distance;
            
            if (_lblMinAge != null) _lblMinAge.text = _presenter.MinAge.ToString();
            if (_lblMaxAge != null) _lblMaxAge.text = _presenter.MaxAge.ToString();
            if (_lblDistance != null) _lblDistance.text = Mathf.RoundToInt(_presenter.Distance).ToString();

        }

        private void UpdateSelectedAltar()
        {
            if (_containerSelected == null) return;
            _containerSelected.Clear();

            var gifts = _presenter.SelectedGifts;
            var seeks = _presenter.SelectedSeeks;

            foreach (var g in gifts) _containerSelected.Add(CreateStaticTag(g, true));
            foreach (var s in seeks) _containerSelected.Add(CreateStaticTag(s, false));

            var manifestationFill = _root.Q("ManifestationFill");
            var intentFill = _root.Q("IntentFill");
            var lblHarmony = _root.Q<Label>("LblHarmonyValue");
            
            if (manifestationFill != null && intentFill != null && lblHarmony != null)
            {
                int limit = _presenter.GetTagLimit();
                float manifestProgress = Mathf.Clamp01((float)gifts.Count / limit);
                float intentProgress = Mathf.Clamp01((float)seeks.Count / limit);
                
                manifestationFill.style.width = new Length(manifestProgress * 50f, LengthUnit.Percent);
                intentFill.style.width = new Length(intentProgress * 50f, LengthUnit.Percent);
                
                string mTerm = _localization.Get(AuraTerms.TAB_MANIFEST, "MANIFEST").ToUpper();
                string iTerm = _localization.Get(AuraTerms.TAB_INTENT, "INTENT").ToUpper();
                int strength = Mathf.RoundToInt(_presenter.ResonanceStrength);
                lblHarmony.text = $"{mTerm}: {Mathf.RoundToInt(manifestProgress * 100f)}% | {iTerm}: {Mathf.RoundToInt(intentProgress * 100f)}% | RESONANCE: {strength}%";
            }
        }

        private VisualElement CreateStaticTag(string key, bool isGift)
        {
            var tag = new VisualElement();
            tag.AddToClassList("tag-base");
            tag.AddToClassList(isGift ? "tag-gift--active" : "tag-seek--active");
            
            var lbl = new Label($"#{_localization.Get(key, key)}");
            tag.Add(lbl);
            
            return tag;
        }
        public void UpdateLocalization()
        {
            var tone = _presenter.OracleTone;
            if (_lblOraclePlaceholder != null)
                _lblOraclePlaceholder.text = _localization.GetPersonaString(AuraTerms.ORACLE_THOUGHTS_PH, tone, "Write what's on your soul...");

            if (_lblOraclePrompt != null)
                _lblOraclePrompt.text = _localization.GetPersonaString(AuraTerms.ORACLE_PROMPT, tone, "MEDITATE WITH THE ORACLE");

            if (_btnConsultOracle != null)
                _btnConsultOracle.text = _localization.GetPersonaString(AuraTerms.BTN_CONSULT_ORACLE, tone, "CONSULT THE ORACLE").ToUpper();


            RefreshVisuals();
        }


        private void HandleOracleSuggestion(OracleSuggestion suggestion)
        {
            Debug.Log($"<color=#D4AF37><b>[ORACLE WHISPER]</b></color> 👁️ <i>\"{suggestion.Whisper}\"</i>");
            
            if (_lblOracleWhisper != null)
            {
                _lblOracleWhisper.text = suggestion.Whisper;
                _lblOracleWhisper.style.display = DisplayStyle.Flex;
                
                // Animate fade in
                _lblOracleWhisper.style.opacity = 0;
                AnimateWhisperIn().Forget();
            }

            if (suggestion.SuggestedTags != null && suggestion.SuggestedTags.Count > 0)
            {
                foreach (var tag in suggestion.SuggestedTags)
                {
                    // Auto-select the first matching tag if not already selected
                    if (!_presenter.IsSelected(tag, true))
                    {
                        _presenter.ToggleGift(tag);
                    }
                }
                
                // Refresh pillars and tags to show selection
                PopulatePillars();
                PopulateTags();
            }
        }

        private void ApplyPillarTheme(AuraPillarSO pillar)
        {
            if (pillar == null) return;
            
            // Apply color to the Altar Indicator
            var manifestationFill = _root.Q("ManifestationFill");
            if (manifestationFill != null)
                manifestationFill.style.backgroundColor = pillar.ThemeColor;

            // Update Header Color for mystical effect
            if (_header != null)
                _header.style.color = pillar.ThemeColor;

            // Sync with the Oracle Eye if it exists
            var oracle = GameObject.FindAnyObjectByType<Oracle.OracleWidgetController>();
            if (oracle != null)
            {
                oracle.SetPupilColor(pillar.ThemeColor);
            }
            
            Debug.Log($"[Aura] ✨ Resonance aligned with {pillar.Id} (Color: {pillar.ThemeColor})");
        }

        private async UniTaskVoid AnimateWhisperIn()
        {
            for (int i = 0; i <= 10; i++)
            {
                if (_lblOracleWhisper == null) return;
                _lblOracleWhisper.style.opacity = i / 10f;
                await UniTask.Delay(30);
            }
        }
    }
}
