using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Social;
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

        // ── Chip data ────────────────────────────────────────────────────────────
        private static readonly (string label, FeedContentFilter val)[] ContentChips =
        {
            ("\ud83c\udf0c \u0412\u0441\u0435",   FeedContentFilter.All),
            ("\ud83d\udcdc \u0425\u0440\u043e\u043d\u0456\u043a\u0438", FeedContentFilter.ChronicleOnly),
            ("\u26a1 \u0417\u0430\u043f\u0438\u0442\u0438",  FeedContentFilter.RequestsOnly),
        };

        private static readonly (string label, FeedLocationFilter val)[] LocationChips =
        {
            ("\ud83c\udfe0 \u041f\u043e\u0440\u0443\u0447",  FeedLocationFilter.Near5km),
            ("\ud83c\udfd9\ufe0f \u0426\u0430\u0440\u0441\u0442\u0432\u043e", FeedLocationFilter.City20km),
            ("\ud83c\udf0d \u0412\u0441\u0456 \u0415\u0444\u0456\u0440\u0438", FeedLocationFilter.All),
        };

        private static readonly (string label, ServiceCategory val)[] PillarChips =
        {
            ("\u2694\ufe0f \u0412\u0441\u0456",      ServiceCategory.All),
            ("\ud83d\udc69 \u041d\u0430\u0432\u0447\u0430\u043d\u043d\u044f", ServiceCategory.Teaching),
            ("\ud83d\udd27 \u0420\u0435\u043c\u0435\u0441\u043b\u043e",  ServiceCategory.Craft),
            ("\ud83d\udcbb \u0415\u0444\u0456\u0440\u043d\u0438\u0439 \u041a\u043e\u0434", ServiceCategory.Code),
            ("\ud83c\udfa8 \u041c\u0438\u0441\u0442\u0435\u0446\u0442\u0432\u043e", ServiceCategory.Art),
            ("\ud83c\udf3f \u041f\u0440\u0438\u0440\u043e\u0434\u0430",  ServiceCategory.Nature),
        };

        private static readonly (string label, FeedTimeFilter val)[] TimeChips =
        {
            ("\ud83d\udd25 \u0422\u0435\u0440\u043c\u0456\u043d\u043e\u0432\u0456",  FeedTimeFilter.Urgent24h),
            ("\ud83c\udf1f \u0421\u0432\u0456\u0436\u0456",  FeedTimeFilter.Recent3d),
            ("\ud83c\udf19 \u0412\u0441\u0456 \u0426\u0438\u043a\u043b\u0438",  FeedTimeFilter.All),
        };

        private static readonly (string label, FeedPriceFilter val)[] PriceChips =
        {
            ("\ud83c\udf81 \u0414\u0430\u0440",    FeedPriceFilter.Free),
            ("\ud83d\udc7b 1-3 \u0425",  FeedPriceFilter.Low_1_3),
            ("\ud83d\udcab 3-10 \u0425", FeedPriceFilter.Mid_3_10),
            ("\ud83c\udf1f 10+ \u0425",  FeedPriceFilter.High_10plus),
            ("\ud83e\udd1d \u0414\u043e\u043c\u043e\u0432\u0438\u0442\u0438\u0441\u044c", FeedPriceFilter.Negotiate),
            ("\u26a1 \u0411\u0443\u0434\u044c-\u044f\u043a\u0430",  FeedPriceFilter.All),
        };

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

            // Content quick chips
            foreach (var (label, val) in ContentChips)
            {
                var chip = MakeChip(label, val == State.Content);
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
            foreach (var (label, val) in PillarChips)
            {
                var chip = MakeChip(label, val == State.Pillar);
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
                var feedList = feedPanel.Q<ListView>("FeedList");
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

            foreach (var (label, val) in ContentChips)
            {
                var chip = MakeChip(label, val == State.Content);
                var captured = val;
                chip.clicked += () => { State.Content = captured; RebuildBar(); OnFiltersChanged?.Invoke(); };
                chipRow.Add(chip);
            }
            chipRow.Add(MakeSeparator());
            foreach (var (label, val) in PillarChips)
            {
                var chip = MakeChip(label, val == State.Pillar);
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

            // Header
            var header = new Label("\ud83d\udd2e \u041e\u0420\u0410\u041a\u0423\u041b \u0424\u0406\u041b\u042c\u0422\u0420\u0406\u0412");
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
            scroll.Add(BuildModalSection("\ud83d\udd2e \u0421\u0424\u0415\u0420\u0418 \u0420\u0415\u0417\u041e\u041d\u0410\u041d\u0421\u0423", ContentChips.Select(c => (c.label, c.val == State.Content)).ToArray(),
                i => { State.Content = ContentChips[i].val; RefreshModal(); }));

            scroll.Add(BuildModalSection("\ud83c\udfd9\ufe0f \u041a\u041e\u041b\u041e \u0415\u041d\u0415\u0420\u0413\u0406\u0407", LocationChips.Select(c => (c.label, c.val == State.Location)).ToArray(),
                i => { State.Location = LocationChips[i].val; RefreshModal(); }));

            scroll.Add(BuildModalSection("\ud83c\udfdb\ufe0f \u041f\u0406\u041b\u041b\u0410\u0420\u0418", PillarChips.Select(c => (c.label, c.val == State.Pillar)).ToArray(),
                i => { State.Pillar = PillarChips[i].val; RefreshModal(); }));

            scroll.Add(BuildModalSection("\u23f1\ufe0f \u041f\u041b\u0418\u041d \u0427\u0410\u0421\u0423", TimeChips.Select(c => (c.label, c.val == State.Time)).ToArray(),
                i => { State.Time = TimeChips[i].val; RefreshModal(); }));

            scroll.Add(BuildModalSection("\u2696\ufe0f \u0412\u0410\u0413\u0418 \u0425\u0420\u041e\u041d\u041e\u0421\u0410", PriceChips.Select(c => (c.label, c.val == State.Price)).ToArray(),
                i => { State.Price = PriceChips[i].val; RefreshModal(); }));

            sheet.Add(scroll);

            // Footer buttons
            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.justifyContent = Justify.SpaceBetween;
            btnRow.style.marginTop = 16;
            btnRow.style.paddingLeft = 20;
            btnRow.style.paddingRight = 20;

            var btnReset = new Button(() => { State = new FeedFilterState(); CloseModal(); OnFiltersChanged?.Invoke(); });
            btnReset.text = "\u2716 \u0421\u043a\u0438\u043d\u0443\u0442\u0438";
            StyleModalBtn(btnReset, false);
            btnRow.Add(btnReset);

            var btnApply = new Button(() => { CloseModal(); OnFiltersChanged?.Invoke(); });
            btnApply.text = "\u2714 \u0417\u0430\u0441\u0442\u043e\u0441\u0443\u0432\u0430\u0442\u0438";
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
