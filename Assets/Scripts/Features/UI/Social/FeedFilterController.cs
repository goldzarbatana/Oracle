using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Social;
using TimeAura.Features.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Features.UI.Social
{
    /// <summary>
    /// FeedFilterController — "Oracle of Filters".
    /// Manages the horizontal quick-filter bar and the full mystical filter modal.
    /// Attached to the same GameObject as ConvergenceFeedController.
    /// </summary>
    public class FeedFilterController : MonoBehaviour
    {
        // ── State ────────────────────────────────────────────────────────────────
        public FeedFilterState State { get; private set; } = new FeedFilterState();

        public event Action OnFiltersChanged;

        // ── UI references ────────────────────────────────────────────────────────
        private VisualElement _root;
        private VisualElement _filterBar;       // horizontal chip bar
        private VisualElement _filterModal;     // full-screen modal overlay
        private Label _lblActiveCount;
        private LocalizationManager _localization;

        // ── Chip arrays for iteration ───────────────────────────────────────────
        private static readonly FeedContentFilter[] ContentFilters = { FeedContentFilter.All, FeedContentFilter.ChronicleOnly, FeedContentFilter.RequestsOnly };
        private static readonly FeedLocationFilter[] LocationFilters = { FeedLocationFilter.Near5km, FeedLocationFilter.City20km, FeedLocationFilter.All };
        private static readonly ServiceCategory[] PillarFilters = { ServiceCategory.All, ServiceCategory.Teaching, ServiceCategory.Craft, ServiceCategory.Code, ServiceCategory.Art, ServiceCategory.Nature };
        private static readonly FeedTimeFilter[] TimeFilters = { FeedTimeFilter.Urgent24h, FeedTimeFilter.Recent3d, FeedTimeFilter.All };
        private static readonly FeedPriceFilter[] PriceFilters = { FeedPriceFilter.Free, FeedPriceFilter.Low_1_3, FeedPriceFilter.Mid_3_10, FeedPriceFilter.High_10plus, FeedPriceFilter.Negotiate, FeedPriceFilter.All };

        // ── Chip Labels (Localized on-the-fly) ───────────────────────────────────
        private string GetContentChipLabel(FeedContentFilter val, bool isUk)
        {
            return val switch
            {
                FeedContentFilter.All => isUk ? "🌌 Все" : "🌌 All",
                FeedContentFilter.ChronicleOnly => isUk ? "📜 Хроніки" : "📜 Chronicles",
                FeedContentFilter.RequestsOnly => isUk ? "⚡ Запити" : "⚡ Requests",
                _ => ""
            };
        }

        private string GetLocationChipLabel(FeedLocationFilter val, bool isUk)
        {
            return val switch
            {
                FeedLocationFilter.Near5km => isUk ? "🏠 Поруч" : "🏠 Nearby",
                FeedLocationFilter.City20km => isUk ? "🏙️ Царство" : "🏙️ Realm",
                FeedLocationFilter.All => isUk ? "🌍 Всі Ефіри" : "🌍 All Realms",
                _ => ""
            };
        }

        private string GetPillarChipLabel(ServiceCategory val, bool isUk)
        {
            return val switch
            {
                ServiceCategory.All => isUk ? "⚔️ Всі" : "⚔️ All",
                ServiceCategory.Teaching => isUk ? "👩 Навчання" : "👩 Teaching",
                ServiceCategory.Craft => isUk ? "🔧 Ремесло" : "🔧 Craft",
                ServiceCategory.Code => isUk ? "💻 Ефірний Код" : "💻 Ether Code",
                ServiceCategory.Art => isUk ? "🎨 Мистецтво" : "🎨 Art",
                ServiceCategory.Nature => isUk ? "🌿 Природа" : "🌿 Nature",
                _ => ""
            };
        }

        private string GetTimeChipLabel(FeedTimeFilter val, bool isUk)
        {
            return val switch
            {
                FeedTimeFilter.Urgent24h => isUk ? "🔥 Термінові" : "🔥 Urgent",
                FeedTimeFilter.Recent3d => isUk ? "🌟 Свіжі" : "🌟 Recent",
                FeedTimeFilter.All => isUk ? "🌙 Всі Цикли" : "🌙 All Cycles",
                _ => ""
            };
        }

        private string GetPriceChipLabel(FeedPriceFilter val, bool isUk)
        {
            return val switch
            {
                FeedPriceFilter.Free => isUk ? "🎁 Дар" : "🎁 Gift",
                FeedPriceFilter.Low_1_3 => isUk ? "👻 1-3 Х" : "👻 1-3 H",
                FeedPriceFilter.Mid_3_10 => isUk ? "💫 3-10 Х" : "💫 3-10 H",
                FeedPriceFilter.High_10plus => isUk ? "🌟 10+ Х" : "🌟 10+ H",
                FeedPriceFilter.Negotiate => isUk ? "🤝 Домовитись" : "🤝 Negotiate",
                FeedPriceFilter.All => isUk ? "⚡ Будь-яка" : "⚡ Any",
                _ => ""
            };
        }

        // ── Colours ─────────────────────────────────────────────────────────────
        private static readonly Color GOLD       = new Color(0.83f, 0.68f, 0.21f, 1f);
        private static readonly Color DEEP_BLUE  = new Color(0.04f, 0.04f, 0.10f, 0.96f);
        private static readonly Color CHIP_ACTIVE = new Color(0.83f, 0.68f, 0.21f, 0.22f);
        private static readonly Color CHIP_IDLE   = new Color(1f, 1f, 1f, 0.05f);

        // ────────────────────────────────────────────────────────────────────────

        /// <summary>Call from ConvergenceFeedController.Initialize after binding root.</summary>
        public void Initialize(VisualElement nexusRoot)
        {
            _root = nexusRoot;
            _localization = UnityEngine.Object.FindAnyObjectByType<LocalizationManager>(FindObjectsInactive.Include);
            BuildFilterBar();
        }

        // ── Filtering ────────────────────────────────────────────────────────────

        public IReadOnlyList<Post> Apply(IEnumerable<Post> posts)
        {
            var s = State;
            return posts.Where(p =>
            {
                // Content
                if (s.Content == FeedContentFilter.ChronicleOnly && p.postType != PostType.Chronicle) return false;
                if (s.Content == FeedContentFilter.RequestsOnly  && p.postType != PostType.ServiceRequest) return false;

                // Pillar (only applies to service requests)
                if (s.Pillar != ServiceCategory.All && p.postType == PostType.ServiceRequest && p.serviceCategory != s.Pillar) return false;

                // Location
                if (s.Location == FeedLocationFilter.Near5km  && p.distanceKm > 5f)  return false;
                if (s.Location == FeedLocationFilter.City20km && p.distanceKm > 20f) return false;
                if (s.Location == FeedLocationFilter.Custom   && p.distanceKm > s.CustomKm) return false;

                // Time
                if (s.Time == FeedTimeFilter.Urgent24h && (DateTime.UtcNow - p.createdAt).TotalHours > 24) return false;
                if (s.Time == FeedTimeFilter.Recent3d  && (DateTime.UtcNow - p.createdAt).TotalDays  > 3)  return false;

                // Price (only service requests)
                if (p.postType == PostType.ServiceRequest)
                {
                    switch (s.Price)
                    {
                        case FeedPriceFilter.Free:       if (p.priceType != PriceType.Free)     return false; break;
                        case FeedPriceFilter.Low_1_3:    if (p.horasPrice < 1 || p.horasPrice > 3)  return false; break;
                        case FeedPriceFilter.Mid_3_10:   if (p.horasPrice < 3 || p.horasPrice > 10) return false; break;
                        case FeedPriceFilter.High_10plus:if (p.horasPrice < 10) return false; break;
                        case FeedPriceFilter.Negotiate:  if (p.priceType != PriceType.Negotiate) return false; break;
                    }
                }

                return true;
            }).ToList();
        }

        public int ActiveFilterCount()
        {
            int n = 0;
            if (State.Content  != FeedContentFilter.All)    n++;
            if (State.Location != FeedLocationFilter.All)   n++;
            if (State.Pillar   != ServiceCategory.All)      n++;
            if (State.Time     != FeedTimeFilter.All)       n++;
            if (State.Price    != FeedPriceFilter.All)      n++;
            return n;
        }

        // ── Quick-filter bar ─────────────────────────────────────────────────────

        private void BuildFilterBar()
        {
            _filterBar = new VisualElement();
            _filterBar.name = "FeedFilterBar";
            _filterBar.style.flexDirection = FlexDirection.Row;
            _filterBar.style.alignItems = Align.Center;
            _filterBar.style.paddingLeft = 16;
            _filterBar.style.paddingRight = 16;
            _filterBar.style.paddingTop = 8;
            _filterBar.style.paddingBottom = 8;
            _filterBar.style.marginBottom = 4;
            _filterBar.style.backgroundColor = new StyleColor(new Color(0.04f, 0.04f, 0.10f, 0.6f));
            _filterBar.style.borderBottomWidth = 1;
            _filterBar.style.borderBottomColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.15f));

            // Horizontal scroll area for quick chips
            var scroll = new ScrollView(ScrollViewMode.Horizontal);
            scroll.style.flexGrow = 1;
            scroll.style.height = 40;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            var chipRow = new VisualElement();
            chipRow.name = "FilterChipRow";
            chipRow.style.flexDirection = FlexDirection.Row;
            chipRow.style.alignItems = Align.Center;
            chipRow.style.paddingRight = 8;

            bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;

            // Content quick chips
            foreach (var val in ContentFilters)
            {
                var chip = MakeChip(GetContentChipLabel(val, isUk), val == State.Content);
                var captured = val;
                chip.clicked += () =>
                {
                    State.Content = captured;
                    RebuildBar();
                    OnFiltersChanged?.Invoke();
                };
                chipRow.Add(chip);
            }

            // Separator
            chipRow.Add(MakeSeparator());

            // Pillar quick chips
            foreach (var val in PillarFilters)
            {
                var chip = MakeChip(GetPillarChipLabel(val, isUk), val == State.Pillar);
                var captured = val;
                chip.clicked += () =>
                {
                    State.Pillar = captured;
                    // Showing pillar filter implies requests only
                    if (captured != ServiceCategory.All && State.Content == FeedContentFilter.All)
                        State.Content = FeedContentFilter.RequestsOnly;
                    RebuildBar();
                    OnFiltersChanged?.Invoke();
                };
                chipRow.Add(chip);
            }

            scroll.Add(chipRow);
            _filterBar.Add(scroll);

            // "Oracle Filters" button
            var btnOracle = new Button(() => ShowFilterModal());
            btnOracle.name = "BtnOracleFilters";
            btnOracle.text = "\ud83d\udd2e";
            btnOracle.style.width = 40;
            btnOracle.style.height = 40;
            btnOracle.style.fontSize = 20;
            btnOracle.style.marginLeft = 8;
            btnOracle.style.flexShrink = 0;
            btnOracle.style.backgroundColor = new StyleColor(CHIP_IDLE);
            btnOracle.style.borderTopLeftRadius = btnOracle.style.borderTopRightRadius =
            btnOracle.style.borderBottomLeftRadius = btnOracle.style.borderBottomRightRadius = 20;
            btnOracle.style.borderTopWidth = btnOracle.style.borderBottomWidth =
            btnOracle.style.borderLeftWidth = btnOracle.style.borderRightWidth = 1;
            btnOracle.style.borderTopColor = btnOracle.style.borderBottomColor =
            btnOracle.style.borderLeftColor = btnOracle.style.borderRightColor =
                new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.3f));

            _lblActiveCount = new Label();
            _lblActiveCount.style.position = Position.Absolute;
            _lblActiveCount.style.top = -4;
            _lblActiveCount.style.right = -4;
            _lblActiveCount.style.width = 18;
            _lblActiveCount.style.height = 18;
            _lblActiveCount.style.fontSize = 10;
            _lblActiveCount.style.backgroundColor = new StyleColor(GOLD);
            _lblActiveCount.style.color = new StyleColor(new Color(0.05f, 0.05f, 0.1f));
            _lblActiveCount.style.unityFontStyleAndWeight = FontStyle.Bold;
            _lblActiveCount.style.unityTextAlign = TextAnchor.MiddleCenter;
            _lblActiveCount.style.borderTopLeftRadius = _lblActiveCount.style.borderTopRightRadius =
            _lblActiveCount.style.borderBottomLeftRadius = _lblActiveCount.style.borderBottomRightRadius = 9;
            _lblActiveCount.style.display = DisplayStyle.None;
            btnOracle.Add(_lblActiveCount);

            _filterBar.Add(btnOracle);

            // Insert bar into FeedPanel, just before the ListView
            var feedPanel = _root.Q("FeedPanel");
            if (feedPanel != null)
            {
                var feedList = feedPanel.Q("FeedList");
                if (feedList != null && feedList.parent != null)
                {
                    feedList.parent.Insert(feedList.parent.IndexOf(feedList), _filterBar);
                }
                else
                {
                    feedPanel.Add(_filterBar);
                }
            }
            UpdateActiveCount();
        }

        private void RebuildBar()
        {
            if (_filterBar == null) return;
            var chipRow = _filterBar.Q("FilterChipRow");
            if (chipRow == null) return;

            chipRow.Clear();

            bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;

            foreach (var val in ContentFilters)
            {
                var chip = MakeChip(GetContentChipLabel(val, isUk), val == State.Content);
                var captured = val;
                chip.clicked += () => { State.Content = captured; RebuildBar(); OnFiltersChanged?.Invoke(); };
                chipRow.Add(chip);
            }
            chipRow.Add(MakeSeparator());
            foreach (var val in PillarFilters)
            {
                var chip = MakeChip(GetPillarChipLabel(val, isUk), val == State.Pillar);
                var captured = val;
                chip.clicked += () =>
                {
                    State.Pillar = captured;
                    if (captured != ServiceCategory.All && State.Content == FeedContentFilter.All)
                        State.Content = FeedContentFilter.RequestsOnly;
                    RebuildBar();
                    OnFiltersChanged?.Invoke();
                };
                chipRow.Add(chip);
            }
            UpdateActiveCount();
        }

        private void UpdateActiveCount()
        {
            if (_lblActiveCount == null) return;
            int n = ActiveFilterCount();
            _lblActiveCount.text = n.ToString();
            _lblActiveCount.style.display = n > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // ── Filter Modal ─────────────────────────────────────────────────────────

        private void ShowFilterModal()
        {
            // Remove existing modal if any
            _root.Q("FeedFilterModal")?.RemoveFromHierarchy();

            _filterModal = new VisualElement { name = "FeedFilterModal" };
            _filterModal.style.position = Position.Absolute;
            _filterModal.style.top = _filterModal.style.bottom =
            _filterModal.style.left = _filterModal.style.right = 0;
            _filterModal.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0.85f));
            _filterModal.style.justifyContent = Justify.FlexEnd;

            // Bottom sheet panel
            var sheet = new VisualElement();
            sheet.style.backgroundColor = new StyleColor(DEEP_BLUE);
            sheet.style.borderTopLeftRadius = sheet.style.borderTopRightRadius = 24;
            sheet.style.borderTopWidth = 2;
            sheet.style.borderTopColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.5f));
            sheet.style.paddingTop = 8;
            sheet.style.paddingBottom = 24;

            // Handle
            var handle = new VisualElement();
            handle.style.width = 40;
            handle.style.height = 4;
            handle.style.backgroundColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.4f));
            handle.style.borderTopLeftRadius = handle.style.borderTopRightRadius =
            handle.style.borderBottomLeftRadius = handle.style.borderBottomRightRadius = 2;
            handle.style.alignSelf = Align.Center;
            handle.style.marginBottom = 16;
            sheet.Add(handle);

            bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;

            // Header
            var header = new Label(isUk ? "🔮 ОРАКУЛ ФІЛЬТРІВ" : "🔮 ORACLE FILTERS");
            header.style.fontSize = 22;
            header.style.color = new StyleColor(GOLD);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.style.marginBottom = 20;
            sheet.Add(header);

            var scroll = new ScrollView();
            scroll.style.maxHeight = 520;
            scroll.style.paddingLeft = 20;
            scroll.style.paddingRight = 20;

            // Sections
            scroll.Add(BuildModalSection(
                isUk ? "🔮 СФЕРИ РЕЗОНАНСУ" : "🔮 RESONANCE SPHERES", 
                ContentFilters.Select(val => (GetContentChipLabel(val, isUk), val == State.Content)).ToArray(),
                i => { State.Content = ContentFilters[i]; RefreshModal(); }));

            scroll.Add(BuildModalSection(
                isUk ? "🏙️ КОЛО ЕНЕРГІЇ" : "🏙️ ENERGY CIRCLE", 
                LocationFilters.Select(val => (GetLocationChipLabel(val, isUk), val == State.Location)).ToArray(),
                i => { State.Location = LocationFilters[i]; RefreshModal(); }));

            scroll.Add(BuildModalSection(
                isUk ? "🏛️ ПІЛЛАРИ" : "🏛️ PILLARS", 
                PillarFilters.Select(val => (GetPillarChipLabel(val, isUk), val == State.Pillar)).ToArray(),
                i => { State.Pillar = PillarFilters[i]; RefreshModal(); }));

            scroll.Add(BuildModalSection(
                isUk ? "⏱️ ПЛИН ЧАСУ" : "⏱️ FLOW OF TIME", 
                TimeFilters.Select(val => (GetTimeChipLabel(val, isUk), val == State.Time)).ToArray(),
                i => { State.Time = TimeFilters[i]; RefreshModal(); }));

            scroll.Add(BuildModalSection(
                isUk ? "⚖️ ВАГИ ХРОНОСА" : "⚖️ SCALES OF CHRONOS", 
                PriceFilters.Select(val => (GetPriceChipLabel(val, isUk), val == State.Price)).ToArray(),
                i => { State.Price = PriceFilters[i]; RefreshModal(); }));

            sheet.Add(scroll);

            // Footer buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;
            btnRow.style.marginTop = 16;
            btnRow.style.paddingLeft = 20;
            btnRow.style.paddingRight = 20;

            var btnReset = new Button(() => { State = new FeedFilterState(); CloseModal(); OnFiltersChanged?.Invoke(); });
            btnReset.text = isUk ? "✖ Скинути" : "✖ Reset";
            StyleModalBtn(btnReset, false);
            btnRow.Add(btnReset);

            var btnApply = new Button(() => { CloseModal(); OnFiltersChanged?.Invoke(); });
            btnApply.text = isUk ? "✔ Застосувати" : "✔ Apply";
            StyleModalBtn(btnApply, true);
            btnRow.Add(btnApply);

            sheet.Add(btnRow);
            _filterModal.Add(sheet);

            // Close by tapping background
            _filterModal.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _filterModal) CloseModal();
            });

            _root.Add(_filterModal);
        }

        private void RefreshModal()
        {
            RebuildBar();
            // Rebuild modal UI in-place
            ShowFilterModal();
        }

        private void CloseModal()
        {
            _filterModal?.RemoveFromHierarchy();
            _filterModal = null;
            RebuildBar();
            UpdateActiveCount();
        }

        public void UpdateLocalization()
        {
            RebuildBar();
            if (_filterModal != null)
            {
                ShowFilterModal();
            }
        }

        private VisualElement BuildModalSection(string title, (string label, bool active)[] chips, Action<int> onSelect)
        {
            var section = new VisualElement();
            section.style.marginBottom = 20;

            var lbl = new Label(title);
            lbl.style.color = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.8f));
            lbl.style.fontSize = 12;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.marginBottom = 10;
            section.Add(lbl);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;

            for (int i = 0; i < chips.Length; i++)
            {
                var (label, active) = chips[i];
                var chip = MakeChip(label, active);
                var capturedIndex = i;
                chip.clicked += () => onSelect(capturedIndex);
                row.Add(chip);
            }
            section.Add(row);
            return section;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private Button MakeChip(string label, bool active)
        {
            var btn = new Button();
            btn.text = label;
            btn.style.height = 32;
            btn.style.paddingLeft = 12;
            btn.style.paddingRight = 12;
            btn.style.marginRight = 6;
            btn.style.marginBottom = 6;
            btn.style.fontSize = 12;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 16;
            btn.style.borderTopWidth = btn.style.borderBottomWidth =
            btn.style.borderLeftWidth = btn.style.borderRightWidth = 1;
            ApplyChipStyle(btn, active);
            return btn;
        }

        private void ApplyChipStyle(Button btn, bool active)
        {
            if (active)
            {
                btn.style.backgroundColor = new StyleColor(CHIP_ACTIVE);
                btn.style.color = new StyleColor(GOLD);
                btn.style.borderTopColor = btn.style.borderBottomColor =
                btn.style.borderLeftColor = btn.style.borderRightColor = new StyleColor(GOLD);
            }
            else
            {
                btn.style.backgroundColor = new StyleColor(CHIP_IDLE);
                btn.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
                btn.style.borderTopColor = btn.style.borderBottomColor =
                btn.style.borderLeftColor = btn.style.borderRightColor =
                    new StyleColor(new Color(1f, 1f, 1f, 0.12f));
            }
        }

        private VisualElement MakeSeparator()
        {
            var sep = new VisualElement();
            sep.style.width = 1;
            sep.style.height = 24;
            sep.style.backgroundColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.2f));
            sep.style.marginLeft = 6;
            sep.style.marginRight = 10;
            return sep;
        }

        private void StyleModalBtn(Button btn, bool primary)
        {
            btn.style.height = 48;
            btn.style.width = new Length(47, LengthUnit.Percent);
            btn.style.fontSize = 14;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 10;
            btn.style.borderTopWidth = btn.style.borderBottomWidth =
            btn.style.borderLeftWidth = btn.style.borderRightWidth = 1;

            if (primary)
            {
                btn.style.backgroundColor = new StyleColor(GOLD);
                btn.style.color = new StyleColor(new Color(0.05f, 0.05f, 0.1f));
                btn.style.borderTopColor = btn.style.borderBottomColor =
                btn.style.borderLeftColor = btn.style.borderRightColor = new StyleColor(GOLD);
            }
            else
            {
                btn.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f, 0.05f));
                btn.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
                btn.style.borderTopColor = btn.style.borderBottomColor =
                btn.style.borderLeftColor = btn.style.borderRightColor =
                    new StyleColor(new Color(1f, 1f, 1f, 0.15f));
            }
        }
    }

    // ── Filter State & Enums ─────────────────────────────────────────────────────

    public class FeedFilterState
    {
        public FeedContentFilter  Content  = FeedContentFilter.All;
        public FeedLocationFilter Location = FeedLocationFilter.All;
        public ServiceCategory    Pillar   = ServiceCategory.All;
        public FeedTimeFilter     Time     = FeedTimeFilter.All;
        public FeedPriceFilter    Price    = FeedPriceFilter.All;
        public float              CustomKm = 50f;
    }

    public enum FeedContentFilter  { All, ChronicleOnly, RequestsOnly }
    public enum FeedLocationFilter { All, Near5km, City20km, Custom }
    public enum FeedTimeFilter     { All, Recent3d, Urgent24h }
    public enum FeedPriceFilter    { All, Free, Low_1_3, Mid_3_10, High_10plus, Negotiate }
}
