using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Data.SO;
using TimeAura.Core.Services;
using TimeAura.Features.UI.Oracle;
using TimeAura.Features.Localization;
using TimeAura.Core.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// OracleSelectionController - Coordinates the Oracle Slot and the Oracle Selection Modal.
    /// Sorts Oracles based on Resonance with Master's selected skill tags.
    /// "We do not choose the Oracle; the Oracle responds to our alignment."
    /// </summary>
    public class OracleSelectionController
    {
        private readonly VisualElement _root;
        private readonly AuraPresenter _presenter;
        private readonly OraclePromptFactory _promptFactory;
        private readonly LocalizationManager _localization;
        private OracleSO[] _oracles;
        private readonly AudioService _audioService;
        private readonly HapticService _hapticService;
        private readonly OracleWhisperManager _whisperManager;

        private VisualElement _slot;
        private VisualElement _slotGlow;
        private VisualElement _slotIcon;
        private Label _lblSlotHint;
        private Label _lblSlotName;

        private VisualElement _modal;
        private Label _lblModalHeader;
        private Label _lblResonanceHint;
        private ScrollView _listScroll;
        private VisualElement _listContainer;
        private Button _btnCloseModal;
        private Button _btnGenerateOracle;

        public OracleSelectionController(
            VisualElement root,
            AuraPresenter presenter,
            OraclePromptFactory promptFactory,
            LocalizationManager localization,
            OracleSO[] oracles,
            AudioService audioService,
            HapticService hapticService,
            OracleWhisperManager whisperManager)
        {
            _root = root;
            _presenter = presenter;
            _promptFactory = promptFactory;
            _localization = localization;
            _oracles = oracles ?? Array.Empty<OracleSO>();
            _audioService = audioService;
            _hapticService = hapticService;
            _whisperManager = whisperManager;

            InitializeElements();
            BindEvents();
            UpdateSlotVisuals();
        }

        private void InitializeElements()
        {
            if (_root == null) return;

            // Slot elements
            _slot = _root.Q("OracleSlot");
            _slotGlow = _root.Q("OracleSlotGlow");
            _slotIcon = _root.Q("OracleSlotIcon");
            _lblSlotHint = _root.Q<Label>("LblOracleSlotHint");
            _lblSlotName = _root.Q<Label>("LblOracleName");

            // Modal elements
            _modal = _root.Q("OracleSelectionModal");
            _lblModalHeader = _root.Q<Label>("LblOracleModalHeader");
            _lblResonanceHint = _root.Q<Label>("LblResonanceHint");
            _listScroll = _root.Q<ScrollView>("OracleListScroll");
            _listContainer = _root.Q("OracleListContainer");
            _btnCloseModal = _root.Q<Button>("BtnCloseOracleModal");
            _btnGenerateOracle = _root.Q<Button>("BtnGenerateOracle");

            // Setup programmatical fallbacks for static resource loading
            if (_oracles.Length == 0)
            {
                Debug.Log("[OracleSelectionController] 🧪 No OracleSO assets found in Resources. Spawning default candidates.");
                
                var coder = ScriptableObject.CreateInstance<OracleSO>();
                coder.Id = "oracle_coder";
                coder.DisplayName = "КОДЕР";
                coder.LocalizationKey = "ORACLE_CODER";
                coder.BasePersonality = "Ви є строгим архітектором ШІ. Ваші відповіді містичні та лаконічні.";
                coder.ResonantTags = new List<string> { "programming", "architecture", "code", "unity", "c#" };
                coder.ThemeColor = Color.cyan;
                coder.Tier = 0;

                var philosopher = ScriptableObject.CreateInstance<OracleSO>();
                philosopher.Id = "oracle_philosopher";
                philosopher.DisplayName = "ФІЛОСОФ";
                philosopher.LocalizationKey = "ORACLE_PHILOSOPHER";
                philosopher.BasePersonality = "Ви є стоїчним мислителем. Ваші відповіді глибокі та сповнені мудрості.";
                philosopher.ResonantTags = new List<string> { "logic", "philosophy", "mindset", "ethics", "life" };
                philosopher.ThemeColor = Color.yellow;
                philosopher.Tier = 0;

                var muse = ScriptableObject.CreateInstance<OracleSO>();
                muse.Id = "oracle_muse";
                muse.DisplayName = "МУЗА";
                muse.LocalizationKey = "ORACLE_MUSE";
                muse.BasePersonality = "Ви є поетичним та натхненним митцем. Ваші відповіді творчі та метафоричні.";
                muse.ResonantTags = new List<string> { "art", "music", "writing", "design", "creative" };
                muse.ThemeColor = new Color(0.7f, 0.3f, 1f);
                muse.Tier = 0;

                var marketer = ScriptableObject.CreateInstance<OracleSO>();
                marketer.Id = "oracle_marketer";
                marketer.DisplayName = "МАРКЕТОЛОГ";
                marketer.LocalizationKey = "ORACLE_MARKETER";
                marketer.BasePersonality = "Ви є енергійним стратегом росту. Ваші відповіді сфокусовані на розвитку та бізнесі.";
                marketer.ResonantTags = new List<string> { "marketing", "seo", "growth", "business", "strategy" };
                marketer.ThemeColor = new Color(1f, 0.5f, 0f);
                marketer.Tier = 0;

                _oracles = new[] { coder, philosopher, muse, marketer };
            }

            Debug.Log($"[OracleSelectionController] 🛠️ Wireframe bound. Slot: {_slot != null}, Modal: {_modal != null}, Generate: {_btnGenerateOracle != null}");
        }

        private void BindEvents()
        {
            if (_slot != null)
            {
                _slot.RegisterCallback<ClickEvent>(evt =>
                {
                    _hapticService?.LightTap();
                    _audioService?.PlaySFX("ButtonTap");
                    OpenSelectionModal();
                });
            }

            if (_btnCloseModal != null)
            {
                _btnCloseModal.clicked += () =>
                {
                    _hapticService?.LightTap();
                    _audioService?.PlaySFX("ButtonTap");
                    CloseSelectionModal();
                };
            }

            if (_btnGenerateOracle != null)
            {
                _btnGenerateOracle.clicked += () =>
                {
                    _hapticService?.MediumTap();
                    _audioService?.PlaySFX("ButtonTap");
                    GenerateCustomOracle().Forget();
                };
            }

            if (_presenter != null)
            {
                _presenter.OnDataChanged += UpdateSlotVisuals;
            }
        }

        public void Cleanup()
        {
            if (_presenter != null)
            {
                _presenter.OnDataChanged -= UpdateSlotVisuals;
            }
        }

        private void OpenSelectionModal()
        {
            if (_modal == null) return;

            PopulateOracleList();

            _modal.style.display = DisplayStyle.Flex;
            _modal.RemoveFromClassList("modal--hidden");
            Debug.Log("[Popup] Opened: OracleSelectionModal (Вибір Оракула)");
        }

        private void CloseSelectionModal()
        {
            if (_modal == null) return;

            _modal.AddToClassList("modal--hidden");
            _modal.style.display = DisplayStyle.None;
        }

        private async UniTaskVoid GenerateCustomOracle()
        {
            if (_btnGenerateOracle == null || _promptFactory == null) return;

            _btnGenerateOracle.SetEnabled(false);
            string oldText = _btnGenerateOracle.text;
            _btnGenerateOracle.text = "РИТУАЛ СТВОРЕННЯ...";

            _whisperManager?.ShowWhisper("Ковальня Оракулів активована. Запит до Нексусу...", WhisperColor.Gold);

            try
            {
                var customOracle = await _promptFactory.GenerateDynamicOracleAsync();
                if (customOracle != null)
                {
                    _whisperManager?.ShowWhisper($"Успіх! Створено кастомного Оракула: \"{customOracle.DisplayName}\"", WhisperColor.Cyan);
                    _audioService?.PlaySFX("RitualSeal");
                    
                    // Re-populate modal items & equip the new companion immediately
                    PopulateOracleList();
                    await SelectOracle(customOracle);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OracleSelectionController] Custom Oracle generation failed: {ex.Message}");
                _whisperManager?.ShowWhisper("Нексус мовчить. Спробуйте пізніше.", WhisperColor.Sapphire);
            }
            finally
            {
                if (_btnGenerateOracle != null)
                {
                    _btnGenerateOracle.SetEnabled(true);
                    _btnGenerateOracle.text = oldText;
                }
            }
        }

        private void PopulateOracleList()
        {
            if (_listContainer == null) return;
            _listContainer.Clear();

            var currentGifts = _presenter?.SelectedGifts ?? new List<string>();

            // Compile all available candidates
            var allOracles = new List<OracleSO>();

            // 1. Base Universal Oracle (oracle_base) - always present, silver color theme
            var baseOracle = ScriptableObject.CreateInstance<OracleSO>();
            baseOracle.Id = "oracle_base";
            baseOracle.DisplayName = "Універсал";
            baseOracle.LocalizationKey = "ORACLE_BASE";
            baseOracle.BasePersonality = "Я є універсальним помічником на всі випадки життя. Допомагаю структурувати будь-які сесії в Chamber.";
            baseOracle.ResonantTags = new List<string>();
            baseOracle.ThemeColor = Color.silver;
            baseOracle.Tier = 0;
            allOracles.Add(baseOracle);

            // 2. Add default candidate fallbacks
            if (_oracles != null)
            {
                foreach (var o in _oracles)
                {
                    if (o != null && o.Id != "oracle_base" && allOracles.All(x => x.Id != o.Id))
                    {
                        allOracles.Add(o);
                    }
                }
            }

            // 3. Add dynamically generated custom Oracles from UserProfile
            var profile = _promptFactory?.CurrentProfile;
            if (profile != null && profile.CustomOraclesJson != null)
            {
                foreach (var json in profile.CustomOraclesJson)
                {
                    try
                    {
                        var customOracle = OraclePromptFactory.CreateOracleSO(json);
                        if (customOracle != null && allOracles.All(x => x.Id != customOracle.Id))
                        {
                            allOracles.Add(customOracle);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[OracleSelectionController] Failed to deserialize custom Oracle: {ex.Message}");
                    }
                }
            }

            // Calculate Resonance and sort:
            // 1. High resonance (score > 0) sorted descending
            // 2. Tiers / defaults
            var oracleResonances = allOracles.Select(oracle => new
            {
                Oracle = oracle,
                Score = OraclePromptFactory.CalculateResonance(oracle, currentGifts)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Oracle.Tier)
            .ThenBy(x => x.Oracle.DisplayName)
            .ToList();

            foreach (var item in oracleResonances)
            {
                var card = CreateOracleCard(item.Oracle, item.Score, currentGifts);
                _listContainer.Add(card);
            }
        }

        private VisualElement CreateOracleCard(OracleSO oracle, float resonanceScore, List<string> activeGifts)
        {
            var card = new VisualElement();
            card.AddToClassList("oracle-card");

            if (resonanceScore > 0f)
            {
                card.AddToClassList("oracle-card--resonant");
            }
            else if (activeGifts.Count > 0)
            {
                card.AddToClassList("oracle-card--dimmed");
            }

            card.RegisterCallback<ClickEvent>(evt =>
            {
                _hapticService?.MediumTap();
                _audioService?.PlaySFX("RitualSeal");
                SelectOracle(oracle).Forget();
            });

            var left = new VisualElement();
            left.AddToClassList("oracle-card-left");
            left.style.borderTopColor = oracle.ThemeColor;
            left.style.borderBottomColor = oracle.ThemeColor;
            left.style.borderLeftColor = oracle.ThemeColor;
            left.style.borderRightColor = oracle.ThemeColor;

            var leftGlow = new VisualElement();
            leftGlow.AddToClassList("oracle-card-left-glow");
            leftGlow.style.backgroundColor = new Color(oracle.ThemeColor.r, oracle.ThemeColor.g, oracle.ThemeColor.b, 0.15f);
            left.Add(leftGlow);

            if (oracle.Icon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("oracle-card-left-icon");
                icon.style.backgroundImage = new StyleBackground(oracle.Icon);
                left.Add(icon);
            }
            else
            {
                var fallback = new Label(oracle.DisplayName.Substring(0, Mathf.Min(2, oracle.DisplayName.Length)).ToUpper());
                fallback.AddToClassList("oracle-card-left-fallback");
                left.Add(fallback);
            }
            card.Add(left);

            var center = new VisualElement();
            center.AddToClassList("oracle-card-center");

            var headerRow = new VisualElement();
            headerRow.AddToClassList("oracle-card-header-row");

            var title = new Label(_localization != null ? _localization.Get(oracle.LocalizationKey, oracle.DisplayName).ToUpper() : oracle.DisplayName.ToUpper());
            title.AddToClassList("oracle-card-title");
            headerRow.Add(title);

            int pct = Mathf.RoundToInt(resonanceScore * 100f);
            var badge = new Label(pct > 0 ? $"РЕЗОНАНС {pct}%" : "НЕЙТРАЛЬНИЙ");
            badge.AddToClassList("oracle-resonance-badge");
            if (resonanceScore >= 0.5f)
            {
                badge.AddToClassList("oracle-resonance-badge--high");
            }
            headerRow.Add(badge);
            center.Add(headerRow);

            var desc = new Label(oracle.BasePersonality);
            desc.AddToClassList("oracle-card-desc");
            center.Add(desc);

            var tagsRow = new VisualElement();
            tagsRow.AddToClassList("oracle-card-tags-row");
            foreach (var tag in oracle.ResonantTags)
            {
                var tagLbl = new Label($"#{(_localization != null ? _localization.Get(tag, tag) : tag)}");
                tagLbl.AddToClassList("oracle-card-tag");
                if (activeGifts.Any(g => string.Equals(g, tag, StringComparison.OrdinalIgnoreCase)))
                {
                    tagLbl.AddToClassList("oracle-card-tag--active");
                    tagLbl.style.color = oracle.ThemeColor;
                }
                tagsRow.Add(tagLbl);
            }
            center.Add(tagsRow);

            card.Add(center);
            return card;
        }

        private async UniTask SelectOracle(OracleSO oracle)
        {
            if (_presenter == null || _promptFactory == null) return;

            _presenter.SetEquippedOracle(oracle);

            var currentGifts = _presenter.SelectedGifts;
            _whisperManager?.ShowWhisper($"Злиття розумів... Оракул {oracle.DisplayName} прокидається", WhisperColor.Gold);

            try
            {
                await _promptFactory.SynthesizePromptAsync(oracle, currentGifts);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OracleSelectionController] Failed to synthesize prompt: {ex.Message}");
            }

            if (OracleWidgetController.Instance != null)
            {
                OracleWidgetController.Instance.SetState(OracleState.Active);
            }

            UpdateSlotVisuals();
            CloseSelectionModal();
        }

        private void UpdateSlotVisuals()
        {
            if (_slot == null) return;

            string equippedId = _presenter?.EquippedOracleId ?? "";
            
            if (string.IsNullOrEmpty(equippedId))
            {
                _slot.RemoveFromClassList("oracle-slot--active");
                _slot.AddToClassList("oracle-slot--empty");

                if (_slotIcon != null) _slotIcon.style.display = DisplayStyle.None;
                if (_lblSlotHint != null) _lblSlotHint.style.display = DisplayStyle.Flex;
                if (_lblSlotName != null) _lblSlotName.text = _localization != null ? _localization.Get("ORACLE_EQUIP_HINT", "ВИБРАТИ").ToUpper() : "ВИБРАТИ";
                
                if (_slotGlow != null)
                {
                    Color glowColor = new Color(0.83f, 0.69f, 0.22f, 0.2f);
                    _slotGlow.style.borderTopColor = glowColor;
                    _slotGlow.style.borderBottomColor = glowColor;
                    _slotGlow.style.borderLeftColor = glowColor;
                    _slotGlow.style.borderRightColor = glowColor;
                }
            }
            else
            {
                _slot.RemoveFromClassList("oracle-slot--empty");
                _slot.AddToClassList("oracle-slot--active");

                OracleSO oracle = null;

                if (equippedId == "oracle_base")
                {
                    var baseOracle = ScriptableObject.CreateInstance<OracleSO>();
                    baseOracle.Id = "oracle_base";
                    baseOracle.DisplayName = "Універсал";
                    baseOracle.LocalizationKey = "ORACLE_BASE";
                    baseOracle.BasePersonality = "Я є універсальним помічником на всі випадки життя. Допомагаю структурувати будь-які сесії в Chamber.";
                    baseOracle.ResonantTags = new List<string>();
                    baseOracle.ThemeColor = Color.silver;
                    baseOracle.Tier = 0;
                    oracle = baseOracle;
                }
                else
                {
                    oracle = _oracles?.FirstOrDefault(o => o.Id == equippedId);
                    if (oracle == null)
                    {
                        var profile = _promptFactory?.CurrentProfile;
                        if (profile != null && profile.CustomOraclesJson != null)
                        {
                            foreach (var json in profile.CustomOraclesJson)
                            {
                                try
                                {
                                    var customOracle = OraclePromptFactory.CreateOracleSO(json);
                                    if (customOracle != null && customOracle.Id == equippedId)
                                    {
                                        oracle = customOracle;
                                        break;
                                    }
                                }
                                catch
                                {
                                    // skip
                                }
                            }
                        }
                    }
                }

                if (oracle != null)
                {
                    if (_slotIcon != null && oracle.Icon != null)
                    {
                        _slotIcon.style.backgroundImage = new StyleBackground(oracle.Icon);
                        _slotIcon.style.display = DisplayStyle.Flex;
                        if (_lblSlotHint != null) _lblSlotHint.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        if (_slotIcon != null) _slotIcon.style.display = DisplayStyle.None;
                        if (_lblSlotHint != null)
                        {
                            _lblSlotHint.text = oracle.DisplayName.Substring(0, Mathf.Min(1, oracle.DisplayName.Length)).ToUpper();
                            _lblSlotHint.style.display = DisplayStyle.Flex;
                        }
                    }

                    if (_lblSlotName != null)
                    {
                        _lblSlotName.text = _localization != null ? _localization.Get(oracle.LocalizationKey, oracle.DisplayName).ToUpper() : oracle.DisplayName.ToUpper();
                    }

                    if (_slotGlow != null)
                    {
                        _slotGlow.style.borderTopColor = oracle.ThemeColor;
                        _slotGlow.style.borderBottomColor = oracle.ThemeColor;
                        _slotGlow.style.borderLeftColor = oracle.ThemeColor;
                        _slotGlow.style.borderRightColor = oracle.ThemeColor;
                    }
                }
            }
        }
    }
}
