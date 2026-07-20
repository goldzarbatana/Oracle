using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Social;
using TimeAura.Features.Data;
using TimeAura.Features.Economy;
using TimeAura.Features.Auth;
using TimeAura.Core.Services;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Core.Localization;
using TimeAura.Core.Data.SO;
using TimeAura.Features.Localization;
using TimeAura.Core;

namespace TimeAura.Features.UI.Social
{
    public class ConvergenceFeedController : MonoBehaviour
    {
        private VisualTreeAsset _fateCardTemplate;
        private VisualElement _root;
        private Label _prophecyTitle;
        private Core.GlobalProphecyEvent? _lastProphecyEvent;

        [Inject] private SocialManager _socialManager;
        [Inject] private AddressableAssetService _assetService;
        [Inject] private LocalizationManager _localization;
        [Inject] private QuantumWalletService _walletService;
        [Inject] private TimeWalletService _timeWalletService;
        [Inject] private AuthManager _authManager;
        [Inject] private OracleIntentParser _intentParser;
        [Inject] private UIManager _uiManager;
        [Inject] private RemoteConfigService _remoteConfig;
        [Inject] private GeocodingService _geocodingService;

        private ScrollView _feedList;
        private List<Post> _allPosts  = new();   // full unfiltered data
        private List<Post> _posts     = new();   // filtered list shown in ScrollView
        private int _currentPage = 1;
        private bool _isLoading;
        private bool _hasMore = true;
        private CancellationTokenSource _loadCts;
        private readonly HashSet<string> _expandedPostIds = new();

        private ExchangeRealm _currentRealmFilter = ExchangeRealm.None;

        private VisualElement _createPostModal;
        private Button _btnCreatePost;
        private TextField _inputPostContent;
        private TextField _inputPostPrice;
        private DropdownField _inputPostCategory;
        private Button _btnModalEther;
        private Button _btnModalMatter;
        private Button _btnConfirmPost;
        private Button _btnCancelPost;
        private ExchangeRealm _modalSelectedRealm = ExchangeRealm.Ether;

        // Location search fields
        private TextField _inputLocationSearch;
        private ScrollView _locationResultsList;
        private Label _lblSelectedLocation;
        private Button _btnLocationOnline;
        private string _selectedLocation = "Online (Віддалено)";
        private CancellationTokenSource _locationSearchCts;

        // Oracle Intent UI fields
        private VisualElement _oracleIntentModal;
        private TextField _inputIntentText;
        private Button _btnRecordVoice;
        private Button _btnSubmitIntent;
        private Button _btnSwitchToManual;
        private Button _btnCloseIntent;

        // Filter controller (auto-added as component)
        private FeedFilterController _filter;

        // Navigation and dossier — injected from NexusController after init
        public Action<string> OnOpenDossierForUser;
        public Action<Post> OnOpenChatForPost;
        public Action OnOpenSanctuary;

        private static readonly System.Random _rng = new System.Random();

        public void Initialize(VisualElement root, VisualTreeAsset template)
        {
            _root = root;
            _fateCardTemplate = template;
            _feedList = root.Q<ScrollView>("FeedList");
            if (_feedList == null)
            {
                Debug.LogWarning("[ConvergenceFeed] ScrollView 'FeedList' not found in root.");
                return;
            }

            if (_feedList.verticalScroller != null)
                _feedList.verticalScroller.valueChanged += OnScrollValueChanged;

            // ── Prophecy Header ────────────────────────────────────────────────
            _prophecyContainer = new VisualElement();
            _prophecyContainer.style.display = DisplayStyle.None;
            _prophecyContainer.style.backgroundColor = new StyleColor(new Color(0.15f, 0.05f, 0.25f, 0.9f));
            _prophecyContainer.style.borderBottomColor = new StyleColor(new Color(0.83f, 0.69f, 0.22f));
            _prophecyContainer.style.borderBottomWidth = 2;
            _prophecyContainer.style.paddingTop = 15;
            _prophecyContainer.style.paddingBottom = 15;
            _prophecyContainer.style.paddingLeft = 20;
            _prophecyContainer.style.paddingRight = 20;
            _prophecyContainer.style.marginBottom = 10;

            _prophecyTitle = new Label("\ud83d\udd2e \u0413\u041b\u041e\u0411\u0410\u041b\u042c\u041d\u0415 \u041f\u0420\u041e\u0420\u041e\u0426\u0422\u0412\u041e");
            _prophecyTitle.style.color = new StyleColor(new Color(0.83f, 0.69f, 0.22f));
            _prophecyTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _prophecyTitle.style.fontSize = 18;
            _prophecyTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _prophecyContainer.Add(_prophecyTitle);

            _lblProphecyDesc = new Label(" ");
            _lblProphecyDesc.style.color = Color.white;
            _lblProphecyDesc.style.whiteSpace = WhiteSpace.Normal;
            _lblProphecyDesc.style.marginTop = 10;
            _lblProphecyDesc.style.fontSize = 14;
            _prophecyContainer.Add(_lblProphecyDesc);

            _lblProphecyBonus = new Label(" ");
            _lblProphecyBonus.style.color = new StyleColor(new Color(0.4f, 0.9f, 0.4f));
            _lblProphecyBonus.style.unityFontStyleAndWeight = FontStyle.Bold;
            _lblProphecyBonus.style.marginTop = 5;
            _lblProphecyBonus.style.fontSize = 14;
            _prophecyContainer.Add(_lblProphecyBonus);

            root.Insert(0, _prophecyContainer);
            _prophecySubscription = EventBus.Subscribe<Core.GlobalProphecyEvent>(OnGlobalProphecyReceived);

            // ── Filter controller ──────────────────────────────────────────────
            _filter = GetComponent<FeedFilterController>();
            if (_filter == null) _filter = gameObject.AddComponent<FeedFilterController>();
            _filter.Initialize(root);
            _filter.OnFiltersChanged += ApplyFiltersAndRefreshList;

            // ── Realm Switcher ────────────────────────────────────────────────
            var btnAll = root.Q<Button>("BtnRealmAll");
            var btnEther = root.Q<Button>("BtnRealmEther");
            var btnMatter = root.Q<Button>("BtnRealmMatter");

            if (btnAll != null) btnAll.clicked += () => SetRealmFilter(ExchangeRealm.None, btnAll, btnEther, btnMatter);
            if (btnEther != null) btnEther.clicked += () => SetRealmFilter(ExchangeRealm.Ether, btnAll, btnEther, btnMatter);
            if (btnMatter != null) btnMatter.clicked += () => SetRealmFilter(ExchangeRealm.Matter, btnAll, btnEther, btnMatter);

            SetupCreatePostModal(root);

            UpdateLocalization();

            RefreshAsync().Forget();
        }

        private VisualElement _prophecyContainer;
        private Label _lblProphecyDesc;
        private Label _lblProphecyBonus;
        private IDisposable _prophecySubscription;

        private void OnGlobalProphecyReceived(Core.GlobalProphecyEvent evt)
        {
            _lastProphecyEvent = evt;
            _prophecyContainer.style.display = DisplayStyle.Flex;
            _lblProphecyDesc.text = evt.Description;
            
            bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;
            _lblProphecyBonus.text = isUk 
                ? $"Рек. дія: {evt.RecommendedAction} (Множник: x{evt.BonusMultiplier})" 
                : $"Rec. action: {evt.RecommendedAction} (Multiplier: x{evt.BonusMultiplier})";
        }

        private void OnScrollValueChanged(float value)
        {
            if (_feedList == null || _isLoading || !_hasMore) return;
            if (value > _feedList.verticalScroller.highValue * 0.8f)
                LoadMoreAsync().Forget();
        }

        private void OnDestroy()
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _prophecySubscription?.Dispose();
            _locationSearchCts?.Cancel();
            _locationSearchCts?.Dispose();
            if (_filter != null) _filter.OnFiltersChanged -= ApplyFiltersAndRefreshList;
        }

        // ── Data Loading ─────────────────────────────────────────────────────────

        public async UniTaskVoid RefreshAsync()
        {
            if (_isLoading) return;
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            try
            {
                _isLoading = true;
                _allPosts.Clear();
                _posts.Clear();
                if (_feedList != null) _feedList.Clear();
                _currentPage = 1;
                _hasMore = true;
                _expandedPostIds.Clear();
                await LoadMoreInternalAsync();
            }
            finally { _isLoading = false; }
        }

        private async UniTask LoadMoreAsync()
        {
            if (_isLoading || !_hasMore) return;
            try { _isLoading = true; await LoadMoreInternalAsync(); }
            finally { _isLoading = false; }
        }

        private async UniTask LoadMoreInternalAsync()
        {
            if (_socialManager == null || !_hasMore) return;
            try
            {
                bool isDemo = _remoteConfig == null || _remoteConfig.IsDemoMode;
                if (!isDemo)
                {
                    Debug.Log("[ConvergenceFeed] Subscribing to real posts collection in Firestore...");
                }

                var response = await _socialManager.GetFeedAsync(_currentPage, 20, null, _loadCts.Token);
                if (response?.posts != null)
                {
                    _allPosts.AddRange(response.posts);
                    _hasMore = response.hasMore;
                    _currentPage++;
                    ApplyFiltersAndRefreshList();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.LogError($"[ConvergenceFeed] Load failed: {ex.Message}"); }
        }

        /// <summary>Apply active filters and refresh the ScrollView.</summary>
        private void ApplyFiltersAndRefreshList()
        {
            _posts.Clear();
            var filtered = _filter != null ? _filter.Apply(_allPosts) : (IReadOnlyList<Post>)_allPosts;
            foreach (var p in filtered)
            {
                if (_currentRealmFilter == ExchangeRealm.None || p.realm == _currentRealmFilter || p.postType == PostType.Chronicle)
                {
                    _posts.Add(p);
                }
            }
            
            if (_feedList != null)
            {
                _feedList.Clear();
                for (int i = 0; i < _posts.Count; i++)
                {
                    var element = _fateCardTemplate.Instantiate();
                    var cardRoot = element.Q<VisualElement>("FateCardRoot");
                    if (cardRoot != null)
                    {
                        cardRoot.RegisterCallback<ClickEvent>(OnCardClicked);
                    }
                    BindFateCard(element, i);
                    _feedList.Add(element);
                }
            }
        }

        // ── Card Binding ─────────────────────────────────────────────────────────

        private void BindFateCard(VisualElement element, int index)
        {
            if (index >= _posts.Count) return;
            var post   = _posts[index];
            var cardRoot = element.Q<VisualElement>("FateCardRoot");
            var tone   = _localization != null ? _localization.CurrentTone : OracleTone.Business;
            bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;

            // ── Part 1: Entry animation ────────────────────────────────────────
            // Reset to invisible/translated so transition plays on every bind
            if (cardRoot != null)
            {
                cardRoot.RemoveFromClassList("fate-card--visible");
                cardRoot.schedule.Execute(() => cardRoot.AddToClassList("fate-card--visible"))
                        .StartingIn((long)(index * 60));   // stagger: 60 ms per card
            }

            // ── Part 2: Urgent glow ─────────────────────────────────────────────
            if (cardRoot != null)
            {
                if (post.postType == PostType.ServiceRequest && post.isUrgent)
                    cardRoot.AddToClassList("fate-card--urgent");
                else
                    cardRoot.RemoveFromClassList("fate-card--urgent");
            }

            // ── Remove old dynamic overlays from pool recycling ─────────────────
            element.Q("SRBadge")?.RemoveFromHierarchy();
            element.Q("LocationRow")?.RemoveFromHierarchy();

            if (post.postType == PostType.ServiceRequest)
                BuildServiceRequestBadge(cardRoot, post);
            else
                if (cardRoot != null) cardRoot.style.paddingTop = 16;

            // ── Content image visibility ────────────────────────────────────────
            var contentImgCont = element.Q<VisualElement>("ContentImageContainer");
            bool hasImage = post.imageUrls != null && post.imageUrls.Length > 0;
            if (contentImgCont != null)
                contentImgCont.style.display = hasImage ? DisplayStyle.Flex : DisplayStyle.None;

            // ── Common text fields ──────────────────────────────────────────────
            SetText(element.Q<Label>("LblUsername"),    post.username?.ToUpper());
            SetText(element.Q<Label>("LblContentText"), post.content);

            string transformLabel = _localization?.GetPersonaString(AuraTerms.FEED_LBL_TRANSFORMS, tone, "Transforms") ?? "Transforms";
            SetText(element.Q<Label>("LblTransforms"),  $"{FormatCount(post.likesCount)} {transformLabel}");
            SetText(element.Q<Label>("LblConnections"), $"{FormatCount(post.commentsCount)} \ud83d\udcac");
            SetText(element.Q<Label>("LblTimestamp"),   FormatTime(post.createdAt));

            if (_socialManager != null) BindProfileDataAsync(element, post.userId, tone).Forget();
            LoadCardAssetsAsync(element, post).Forget();

            // ── Part 5a: RESONATE button ────────────────────────────────────────
            var btnTransform = element.Q<Button>("BtnTransform");
            if (btnTransform != null)
            {
                string resonateText = post.isLiked
                    ? _localization?.GetPersonaString(AuraTerms.FEED_BTN_RESONATING, tone, "RESONATING \u2726") ?? "RESONATING \u2726"
                    : _localization?.GetPersonaString(AuraTerms.FEED_BTN_RESONATE,   tone, "RESONATE \u2726")   ?? "RESONATE \u2726";
                btnTransform.text = string.IsNullOrEmpty(resonateText) ? " " : resonateText.ToUpper();
                btnTransform.clickable = null;
                btnTransform.RegisterCallback<ClickEvent>(e => {
                    e.StopPropagation();
                    ToggleLike(post, btnTransform, element, tone);
                });
            }

            // ── Part 5b: BtnConnect — "НАПИСАТИ" for SR posts, "CONNECT" otherwise
            var btnConnect = element.Q<Button>("BtnConnect");
            if (btnConnect != null)
            {
                if (post.postType == PostType.ServiceRequest)
                {
                    btnConnect.text = isUk ? "ДОСЬЄ" : "DOSSIER";
                    btnConnect.RemoveFromClassList("fate-card__btn--write");
                    btnConnect.clickable = null;
                    btnConnect.RegisterCallback<ClickEvent>(e => {
                        e.StopPropagation();
                        if (!string.IsNullOrEmpty(post.userId))
                            OpenUserDossier(post.userId);
                    });
                }
                else
                {
                    btnConnect.text = isUk ? "З'ЄДНАТИСЬ" : "CONNECT";
                    btnConnect.RemoveFromClassList("fate-card__btn--write");
                    btnConnect.clickable = null;
                    btnConnect.RegisterCallback<ClickEvent>(e => {
                        e.StopPropagation();
                        if (!string.IsNullOrEmpty(post.userId))
                            OpenUserDossier(post.userId);
                    });
                }
            }

            // ── Part 5c: BtnRespond — only on ServiceRequest posts ──────────────
            var btnRespond = element.Q<Button>("BtnRespond");
            if (btnRespond != null)
            {
                bool isServiceRequest = post.postType == PostType.ServiceRequest;
                btnRespond.style.display = isServiceRequest ? DisplayStyle.Flex : DisplayStyle.None;
                if (isServiceRequest)
                {
                    btnRespond.clickable = null;
                    btnRespond.RegisterCallback<ClickEvent>(e => {
                        e.StopPropagation();
                        OnOpenChatForPost?.Invoke(post);
                    });
                }
            }

            // ── Card expand state ───────────────────────────────────────────────
            if (cardRoot != null)
            {
                cardRoot.userData = post.postId;
                bool isExpanded = _expandedPostIds.Contains(post.postId);
                if (isExpanded) cardRoot.AddToClassList("fate-card--expanded");
                else            cardRoot.RemoveFromClassList("fate-card--expanded");
            }
        }

        // ── Part 5d: Open MasterDossier for a userId ────────────────────────────
        private void OpenUserDossier(string userId)
        {
            if (OnOpenDossierForUser != null)
            {
                OnOpenDossierForUser.Invoke(userId);
                return;
            }
            // Fallback: navigate via NexusNavigationManager if hooked up
            Debug.LogWarning($"[ConvergenceFeed] OnOpenDossierForUser not wired. userId={userId}");
        }

        private void BuildServiceRequestBadge(VisualElement cardRoot, Post post)
        {
            if (cardRoot == null) return;
            cardRoot.style.paddingTop = 38;

            // ── Top badge bar ──────────────────────────────────────────────────
            var badge = new VisualElement { name = "SRBadge" };
            badge.style.position = Position.Absolute;
            badge.style.top = 0;
            badge.style.left = 0;
            badge.style.right = 0;
            badge.style.flexDirection = FlexDirection.Row;
            badge.style.justifyContent = Justify.SpaceBetween;
            badge.style.alignItems = Align.Center;
            badge.style.paddingLeft = 12;
            badge.style.paddingRight = 12;
            badge.style.paddingTop = 5;
            badge.style.paddingBottom = 5;
            badge.style.backgroundColor = new StyleColor(new Color(0.06f, 0.04f, 0.01f, 0.92f));
            badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius = 18;
            badge.style.borderBottomWidth = 1;
            badge.style.borderBottomColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.4f));
            badge.pickingMode = PickingMode.Ignore;

            var leftRow = new VisualElement();
            leftRow.style.flexDirection = FlexDirection.Row;
            leftRow.style.alignItems = Align.Center;

            string prefix = post.realm == ExchangeRealm.Matter ? "💵 " : "";
            var catLabel = new Label($"{prefix}{GetCategoryIcon(post.serviceCategory)} {GetCategoryName(post.serviceCategory).ToUpper()}");
            if (post.realm == ExchangeRealm.Matter)
                catLabel.style.color = new StyleColor(new Color(0.18f, 0.8f, 0.44f)); // Green for premium Matter
            else
                catLabel.style.color = new StyleColor(new Color(0.83f, 0.68f, 0.21f)); // Gold for Ether
            catLabel.style.fontSize = 11;
            catLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            leftRow.Add(catLabel);

            if (post.isUrgent)
            {
                bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;
                var urgentLbl = new Label(isUk ? " 🔥 ТЕРМІНОВО" : " 🔥 URGENT");
                urgentLbl.style.color = new StyleColor(new Color(1f, 0.42f, 0.1f));
                urgentLbl.style.fontSize = 10;
                urgentLbl.style.marginLeft = 8;
                leftRow.Add(urgentLbl);
            }
            badge.Add(leftRow);

            var priceLabel = new Label(FormatPrice(post));
            if (post.realm == ExchangeRealm.Matter)
                priceLabel.style.color = new StyleColor(new Color(0.18f, 0.8f, 0.44f)); // Green
            else
                priceLabel.style.color = new StyleColor(new Color(0.83f, 0.68f, 0.21f)); // Gold
            priceLabel.style.fontSize = 13;
            priceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.Add(priceLabel);

            cardRoot.Add(badge);

            // ── Location row ───────────────────────────────────────────────────
            if (post.distanceKm > 0 || !string.IsNullOrEmpty(post.authorCity))
            {
                var locRow = new VisualElement { name = "LocationRow" };
                locRow.style.flexDirection = FlexDirection.Row;
                locRow.style.marginTop = 6;
                locRow.pickingMode = PickingMode.Ignore;

                var locText = new Label($"\ud83d\udccd {post.authorCity}  \u2022  {FormatDistance(post.distanceKm)}");
                locText.style.fontSize = 11;
                locText.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.75f));
                locRow.Add(locText);

                var contentArea = cardRoot.Q<VisualElement>("fate-card__content") ?? cardRoot.Q(null, "fate-card__content");
                if (contentArea != null) contentArea.Add(locRow);
                else cardRoot.Add(locRow);
            }
        }

        // ── Profile & asset helpers ──────────────────────────────────────────────

        private void OnCardClicked(ClickEvent evt)
        {
            if (evt.currentTarget is VisualElement root && root.userData is string postId)
                ToggleExpansion(postId, root);
        }

        private void ToggleExpansion(string postId, VisualElement root)
        {
            if (_expandedPostIds.Contains(postId))
            {
                _expandedPostIds.Remove(postId);
                root.RemoveFromClassList("fate-card--expanded");
            }
            else
            {
                _expandedPostIds.Add(postId);
                root.AddToClassList("fate-card--expanded");
            }
        }

        private async UniTaskVoid BindProfileDataAsync(VisualElement element, string userId, OracleTone tone)
        {
            if (_socialManager == null) return;
            var profile = await _socialManager.GetUserProfileAsync(userId);
            if (profile == null) return;
            SetText(element.Q<Label>("LblStatus"), string.IsNullOrEmpty(profile.Bio) ? " " : $"\"{profile.Bio}\"");
            string vFmt = _localization?.GetPersonaString(AuraTerms.FEED_LBL_VECTOR, tone, "Vector: {0} H/hr") ?? "Vector: {0} H/hr";
            SetText(element.Q<Label>("LblVector"), string.Format(vFmt, UnityEngine.Random.Range(50, 500)));
        }

        private async UniTaskVoid LoadCardAssetsAsync(VisualElement element, Post post)
        {
            var avatarImg          = element.Q<VisualElement>("AvatarImage");
            var avatarInitials     = element.Q<Label>("LblAvatarInitials");
            var contentImgCont     = element.Q<VisualElement>("ContentImageContainer");
            var contentImg         = element.Q<VisualElement>("ContentImage");

            // ── Part 4: Avatar with initials fallback ───────────────────────────
            if (!string.IsNullOrEmpty(post.userAvatarUrl) && _socialManager != null)
            {
                var tex = await _socialManager.LoadAvatarAsync(post.userId, post.userAvatarUrl);
                if (tex != null && avatarImg != null)
                {
                    avatarImg.style.backgroundImage = new StyleBackground(tex);
                    if (avatarInitials != null) avatarInitials.style.display = DisplayStyle.None;
                }
                else
                {
                    ShowAvatarInitials(avatarImg, avatarInitials, post.username);
                }
            }
            else
            {
                ShowAvatarInitials(avatarImg, avatarInitials, post.username);
            }

            // ── Post image ───────────────────────────────────────────────────────
            if (post.imageUrls != null && post.imageUrls.Length > 0 && _socialManager != null)
            {
                if (contentImgCont != null) contentImgCont.style.display = DisplayStyle.Flex;
                var tex = await _socialManager.LoadAvatarAsync($"post_{post.postId}", post.imageUrls[0]);
                if (tex != null && contentImg != null) contentImg.style.backgroundImage = new StyleBackground(tex);
            }
            else
            {
                if (contentImgCont != null) contentImgCont.style.display = DisplayStyle.None;
            }
        }

        /// <summary>Show gold initials when no avatar texture is available.</summary>
        private static void ShowAvatarInitials(VisualElement avatarImg, Label initialsLabel, string username)
        {
            if (avatarImg != null) avatarImg.style.backgroundImage = StyleKeyword.None;
            if (initialsLabel == null) return;
            initialsLabel.style.display = DisplayStyle.Flex;
            var name = string.IsNullOrEmpty(username) ? "?" : username.Trim();
            initialsLabel.text = name.Length >= 2
                ? $"{char.ToUpper(name[0])}{char.ToUpper(name[1])}"
                : char.ToUpper(name[0]).ToString();
        }

        private void ToggleLike(Post post, Button btn, VisualElement element, OracleTone tone)
        {
            post.isLiked = !post.isLiked;
            post.likesCount += post.isLiked ? 1 : -1;
            string resonateText = post.isLiked
                ? _localization?.GetPersonaString(AuraTerms.FEED_BTN_RESONATING, tone, "RESONATING ✦") ?? "RESONATING ✦"
                : _localization?.GetPersonaString(AuraTerms.FEED_BTN_RESONATE,   tone, "RESONATE ✦")   ?? "RESONATE ✦";
            btn.text = string.IsNullOrEmpty(resonateText) ? " " : resonateText.ToUpper();
            
            // Update UI transforms instantly
            string transformLabel = _localization?.GetPersonaString(AuraTerms.FEED_LBL_TRANSFORMS, tone, "Transforms") ?? "Transforms";
            SetText(element.Q<Label>("LblTransforms"), $"{FormatCount(post.likesCount)} {transformLabel}");

            _socialManager?.ToggleLikeAsync(post.postId, post.isLiked).Forget();
        }

        // ── Utility ──────────────────────────────────────────────────────────────

        private static void SetText(Label lbl, string text)
        {
            if (lbl == null) return;
            lbl.text = string.IsNullOrEmpty(text) ? " " : text;
        }

        private string FormatPrice(Post post)
        {
            if (post.userId == "ai_qwen_coder") return "50 H / $15";
            if (post.userId == "ai_qwen_translator") return "20 H / $5";
            if (post.userId == "ai_qwen_lawyer") return "100 H / $50";

            if (post.priceType == PriceType.Free)      return "🎁 FREE";
            if (post.priceType == PriceType.Negotiate) return "🤝 Negotiate";
            if (post.realm == ExchangeRealm.Matter)
            {
                return EconomyFormatter.FormatFiat(post.priceWaves);
            }
            else
            {
                return EconomyFormatter.FormatHoras(post.priceAtoms);
            }
        }

        private string FormatDistance(float km)
        {
            if (km <= 0)   return string.Empty;
            if (km >= 99f) return "\ud83c\udf0d \u0412\u0456\u0434\u0434\u0430\u043b\u0435\u043d\u043e";
            return $"{km:0.#} \u043a\u043c";
        }

        private string GetCategoryIcon(ServiceCategory cat) => cat switch
        {
            ServiceCategory.Teaching => "\ud83d\udc69",
            ServiceCategory.Craft    => "\ud83d\udd27",
            ServiceCategory.Code     => "\ud83d\udcbb",
            ServiceCategory.Art      => "\ud83c\udfa8",
            ServiceCategory.Nature   => "\ud83c\udf3f",
            _                        => "\u26a1"
        };

        private string GetCategoryName(ServiceCategory cat) => cat switch
        {
            ServiceCategory.Teaching => "\u041d\u0430\u0432\u0447\u0430\u043d\u043d\u044f",
            ServiceCategory.Craft    => "\u0420\u0435\u043c\u0435\u0441\u043b\u043e",
            ServiceCategory.Code     => "\u0415\u0444\u0456\u0440\u043d\u0438\u0439 \u041a\u043e\u0434",
            ServiceCategory.Art      => "\u041c\u0438\u0441\u0442\u0435\u0446\u0442\u0432\u043e",
            ServiceCategory.Nature   => "\u041f\u0440\u0438\u0440\u043e\u0434\u0430",
            _                        => "\u0417\u0430\u043f\u0438\u0442"
        };

        private string FormatCount(int count)
        {
            if (count < 1000) return count.ToString();
            return $"{count / 1000f:0.#}K";
        }

        private string FormatTime(DateTime dt)
        {
            var span = DateTime.UtcNow - dt;
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m";
            if (span.TotalHours < 24)   return $"{(int)span.TotalHours}h";
            return dt.ToString("MMM dd");
        }

        private void SetRealmFilter(ExchangeRealm realm, Button btnAll, Button btnEther, Button btnMatter)
        {
            _currentRealmFilter = realm;
            
            if (btnAll != null) SetButtonActiveState(btnAll, realm == ExchangeRealm.None);
            if (btnEther != null) SetButtonActiveState(btnEther, realm == ExchangeRealm.Ether);
            if (btnMatter != null) SetButtonActiveState(btnMatter, realm == ExchangeRealm.Matter);

            ApplyFiltersAndRefreshList();
        }

        private void SetButtonActiveState(Button btn, bool active)
        {
            if (active)
            {
                btn.RemoveFromClassList("modal-btn--gold");
                btn.AddToClassList("modal-btn--gold");
            }
            else
            {
                btn.RemoveFromClassList("modal-btn--gold");
            }
        }

        private void SetupCreatePostModal(VisualElement root)
        {
            _createPostModal = root.Q("CreatePostModal");
            _btnCreatePost = root.Q<Button>("BtnCreatePost");
            _inputPostContent = root.Q<TextField>("InputPostContent");
            _inputPostPrice = root.Q<TextField>("InputPostPrice");
            _inputPostCategory = root.Q<DropdownField>("DropdownPostCategory");

            // Location search setup (replaces simple city dropdown)
            _inputLocationSearch = root.Q<TextField>("InputLocationSearch");
            _locationResultsList = root.Q<ScrollView>("LocationResultsList");
            _lblSelectedLocation = root.Q<Label>("LblSelectedLocation");
            _btnLocationOnline = root.Q<Button>("BtnLocationOnline");

            if (_btnLocationOnline != null)
                _btnLocationOnline.clicked += () => SetSelectedLocation("Online (Віддалено)");

            if (_inputLocationSearch != null)
            {
                _inputLocationSearch.RegisterValueChangedCallback(evt =>
                {
                    // Clear confirmed selection when user types again
                    if (_locationResultsList != null) _locationResultsList.style.display = DisplayStyle.None;
                    if (!string.IsNullOrWhiteSpace(evt.newValue) && evt.newValue.Trim().Length >= 2)
                    {
                        _locationSearchCts?.Cancel();
                        _locationSearchCts = new CancellationTokenSource();
                        SearchLocationAsync(evt.newValue, _locationSearchCts.Token).Forget();
                    }
                });
            }
            
            if (_inputPostCategory != null)
            {
                _inputPostCategory.choices = new List<string> {
                    "Навчання (Teaching)",
                    "Ремесло (Craft)",
                    "Код / IT (Code)",
                    "Мистецтво (Art)",
                    "Природа (Nature)"
                };
            }
            
            _btnModalEther = root.Q<Button>("BtnModalEther");
            _btnModalMatter = root.Q<Button>("BtnModalMatter");
            _btnConfirmPost = root.Q<Button>("BtnConfirmPost");
            _btnCancelPost = root.Q<Button>("BtnCancelPost");

            if (_btnCreatePost != null) _btnCreatePost.clicked += OpenOracleIntentModal;
            if (_btnCancelPost != null) _btnCancelPost.clicked += CloseCreatePostModal;
            if (_btnConfirmPost != null) _btnConfirmPost.clicked += PublishPost;

            if (_btnModalEther != null) _btnModalEther.clicked += () => SetModalRealm(ExchangeRealm.Ether);
            if (_btnModalMatter != null) _btnModalMatter.clicked += () => SetModalRealm(ExchangeRealm.Matter);

            // Oracle Intent Modal bindings
            _oracleIntentModal = root.Q("OracleIntentModal");
            _inputIntentText = root.Q<TextField>("InputIntentText");
            _btnRecordVoice = root.Q<Button>("BtnRecordVoice");
            _btnSubmitIntent = root.Q<Button>("BtnSubmitIntent");
            _btnSwitchToManual = root.Q<Button>("BtnSwitchToManual");
            _btnCloseIntent = root.Q<Button>("BtnCloseIntent");

            if (_btnCloseIntent != null) _btnCloseIntent.clicked += CloseOracleIntentModal;
            if (_btnSwitchToManual != null) _btnSwitchToManual.clicked += SwitchFromAIToManual;
            if (_btnRecordVoice != null)
            {
                _btnRecordVoice.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation();
                    _btnRecordVoice.style.scale = new StyleScale(new Scale(new Vector3(1.15f, 1.15f, 1f)));
                    if (_uiManager != null)
                    {
                        string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_VOICE_START, "🎙️ Запис голосу розпочато...") ?? "🎙️ Запис голосу розпочато...";
                        _uiManager.ShowToast(msg, "hint");
                    }

                    var capture = UnityEngine.Object.FindAnyObjectByType<VoiceCaptureService>();
                    if (capture != null) capture.StartRecording();
                    else Debug.LogWarning("[ConvergenceFeed] VoiceCaptureService not found!");
                });

                _btnRecordVoice.RegisterCallback<PointerUpEvent>(evt =>
                {
                    evt.StopPropagation();
                    _btnRecordVoice.style.scale = new StyleScale(new Scale(Vector3.one));

                    var capture = UnityEngine.Object.FindAnyObjectByType<VoiceCaptureService>();
                    if (capture != null)
                    {
                        capture.StopRecording(async audioBase64 =>
                        {
                            if (!string.IsNullOrEmpty(audioBase64))
                            {
                                if (_uiManager != null)
                                {
                                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_ORACLE_RECOGNIZING, "🔮 Оракул розпізнає твій голос...") ?? "🔮 Оракул розпізнає твій голос...";
                                    _uiManager.ShowToast(msg, "hint");
                                }
                                if (_btnSubmitIntent != null) _btnSubmitIntent.SetEnabled(false);
                                
                                try
                                {
                                    var result = await _intentParser.ParseIntentWithAudioAsync(audioBase64);
                                    if (_inputIntentText != null)
                                    {
                                        _inputIntentText.value = result.content;
                                    }
                                    ShowIntentPreview(result);
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogError($"[ConvergenceFeed] Audio intent parsing failed: {ex.Message}");
                                    if (_uiManager != null)
                                    {
                                        string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_VOICE_FAILED, "❌ Не вдалося розпізнати голос.") ?? "❌ Не вдалося розпізнати голос.";
                                        _uiManager.ShowToast(msg, "error");
                                    }
                                }
                                finally
                                {
                                    if (_btnSubmitIntent != null) _btnSubmitIntent.SetEnabled(true);
                                }
                            }
                            else
                            {
                                if (_uiManager != null)
                                {
                                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_VOICE_EMPTY, "⚠️ Запис порожній або не вдався.") ?? "⚠️ Запис порожній або не вдався.";
                                    _uiManager.ShowToast(msg, "error");
                                }
                            }
                        });
                    }
                });
            }

            if (_btnSubmitIntent != null) _btnSubmitIntent.clicked += () => SubmitIntentAsync().Forget();
        }

        private void OpenCreatePostModal()
        {
            if (_createPostModal != null)
            {
                _createPostModal.style.display = DisplayStyle.Flex;
                _createPostModal.RemoveFromClassList("modal--hidden");
                Debug.Log("[Popup] Opened: CreatePostModal (Новий запит)");
            }
            SetModalRealm(ExchangeRealm.Ether);
            if (_inputPostContent != null) _inputPostContent.value = "";
            if (_inputPostPrice != null) _inputPostPrice.value = "0";

            if (_inputPostCategory != null)
                _inputPostCategory.value = "Код / IT (Code)";

            // Reset location search
            var profile = _authManager != null ? _authManager.CurrentProfile : null;
            string userCity = profile != null ? profile.LocationZone : "";
            _selectedLocation = !string.IsNullOrEmpty(userCity) ? userCity : "Online (Віддалено)";
            if (_inputLocationSearch != null)
            {
                _inputLocationSearch.value = _selectedLocation;
            }
            if (_lblSelectedLocation != null)
            {
                _lblSelectedLocation.text = $"✔ {_selectedLocation}";
                _lblSelectedLocation.style.display = DisplayStyle.Flex;
            }
            if (_locationResultsList != null) _locationResultsList.style.display = DisplayStyle.None;
        }

        private void CloseCreatePostModal()
        {
            if (_createPostModal != null)
            {
                _createPostModal.style.display = DisplayStyle.None;
                _createPostModal.AddToClassList("modal--hidden");
            }
        }

        private void OpenOracleIntentModal()
        {
            if (_oracleIntentModal != null)
            {
                _oracleIntentModal.style.display = DisplayStyle.Flex;
                _oracleIntentModal.RemoveFromClassList("modal--hidden");
                Debug.Log("[Popup] Opened: OracleIntentModal (Оракул бажань)");
            }
            if (_inputIntentText != null) _inputIntentText.value = "";
        }

        private void CloseOracleIntentModal()
        {
            if (_oracleIntentModal != null)
            {
                _oracleIntentModal.style.display = DisplayStyle.None;
                _oracleIntentModal.AddToClassList("modal--hidden");
            }
        }

        private void SwitchFromAIToManual()
        {
            CloseOracleIntentModal();
            OpenCreatePostModal();
        }

        private async UniTaskVoid SubmitIntentAsync()
        {
            string text = _inputIntentText?.value ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                if (_uiManager != null)
                {
                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_INTENT_EMPTY, "❌ Бажання не може бути порожнім!") ?? "❌ Бажання не може бути порожнім!";
                    _uiManager.ShowToast(msg, "error");
                }
                return;
            }

            if (_intentParser != null && _intentParser.IsDailyLimitReached)
            {
                if (_uiManager != null)
                {
                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_AI_LIMIT, "🔮 AI request limit reached! Enlightened subscription required.") ?? "🔮 AI request limit reached! Enlightened subscription required.";
                    _uiManager.ShowToast(msg, "error");
                }
                return;
            }

            if (_uiManager != null)
            {
                string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_ORACLE_WEAVING, "🔮 Оракул тче нитки часу...") ?? "🔮 Оракул тче нитки часу...";
                _uiManager.ShowToast(msg, "hint");
            }

            if (_btnSubmitIntent != null) _btnSubmitIntent.SetEnabled(false);

            try
            {
                var result = await _intentParser.ParseIntentAsync(text);
                ShowIntentPreview(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConvergenceFeed] Intent parsing failed: {ex.Message}");
                if (_uiManager != null)
                {
                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_ORACLE_PARSE_FAILED, "❌ Оракул не зміг розпізнати бажання.") ?? "❌ Оракул не зміг розпізнати бажання.";
                    _uiManager.ShowToast(msg, "error");
                }
            }
            finally
            {
                if (_btnSubmitIntent != null) _btnSubmitIntent.SetEnabled(true);
            }
        }

        private void ShowIntentPreview(OracleIntentResult result)
        {
            CloseOracleIntentModal();

            if (result.confidence < 0.7f)
            {
                if (_uiManager != null)
                {
                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_AI_CONFIDENCE_LOW, "⚠️ AI confidence low (< 70%). Verify data manually.") ?? "⚠️ AI confidence low (< 70%). Verify data manually.";
                    _uiManager.ShowToast(msg, "error");
                }

                OpenCreatePostModal();

                if (_inputPostContent != null) _inputPostContent.value = result.content;
                
                ExchangeRealm targetRealm = ExchangeRealm.Ether;
                if (string.Equals(result.realm, "Matter", StringComparison.OrdinalIgnoreCase))
                {
                    targetRealm = ExchangeRealm.Matter;
                }
                SetModalRealm(targetRealm);

                if (_inputPostPrice != null) _inputPostPrice.value = result.price.ToString("F1");
                if (_inputPostCategory != null)
                {
                    string catLower = (result.category ?? "").ToLower();
                    if (catLower.Contains("teach") || catLower.Contains("навч")) _inputPostCategory.value = "Навчання (Teaching)";
                    else if (catLower.Contains("craft") || catLower.Contains("рем")) _inputPostCategory.value = "Ремесло (Craft)";
                    else if (catLower.Contains("code") || catLower.Contains("it") || catLower.Contains("прог")) _inputPostCategory.value = "Код / IT (Code)";
                    else if (catLower.Contains("art") || catLower.Contains("мист")) _inputPostCategory.value = "Мистецтво (Art)";
                    else if (catLower.Contains("nature") || catLower.Contains("прир")) _inputPostCategory.value = "Природа (Nature)";
                    else _inputPostCategory.value = "Код / IT (Code)";
                }
            }
            else
            {
                ExchangeRealm targetRealm = ExchangeRealm.Ether;
                if (string.Equals(result.realm, "Matter", StringComparison.OrdinalIgnoreCase))
                {
                    targetRealm = ExchangeRealm.Matter;
                }

                float priceVal = result.price;
                ServiceCategory cat = ServiceCategory.All;
                if (Enum.TryParse<ServiceCategory>(result.category, true, out var parsedCat))
                {
                    cat = parsedCat;
                }
                else
                {
                    string cLower = result.category.ToLower();
                    if (cLower.Contains("code") || cLower.Contains("it") || cLower.Contains("прог")) cat = ServiceCategory.Code;
                    else if (cLower.Contains("teach") || cLower.Contains("навч")) cat = ServiceCategory.Teaching;
                    else if (cLower.Contains("craft") || cLower.Contains("рем")) cat = ServiceCategory.Craft;
                    else if (cLower.Contains("art") || cLower.Contains("мист")) cat = ServiceCategory.Art;
                    else if (cLower.Contains("nature") || cLower.Contains("прир")) cat = ServiceCategory.Nature;
                }

                var profile = _authManager != null ? _authManager.CurrentProfile : null;
                string username = profile != null ? profile.DisplayName : "Adept";
                string userId = profile != null ? profile.UserId : "user_self";

                var newPost = new Post
                {
                    postId = $"post_{Guid.NewGuid():N}",
                    userId = userId,
                    username = username,
                    content = result.content,
                    postType = PostType.ServiceRequest,
                    serviceCategory = cat,
                    horasPrice = targetRealm == ExchangeRealm.Ether ? (long)priceVal : 0L,
                    priceType = priceVal > 0f ? PriceType.Fixed : PriceType.Free,
                    distanceKm = 0.1f,
                    isUrgent = true,
                    authorCity = "Kyiv",
                    realm = targetRealm,
                    priceAtoms = targetRealm == ExchangeRealm.Ether && _timeWalletService != null ? _timeWalletService.HorasToMinutes(priceVal) : 0L,
                    priceWaves = targetRealm == ExchangeRealm.Matter && _walletService != null ? _walletService.QuantsToWaves(priceVal) : 0L,
                    likesCount = 0,
                    commentsCount = 0,
                    createdAt = DateTime.UtcNow
                };

                _allPosts.Insert(0, newPost);
                ApplyFiltersAndRefreshList();
                
                if (_uiManager != null)
                {
                    string currencyLabel = targetRealm == ExchangeRealm.Ether 
                        ? (_localization?.Get(TimeAura.Core.Localization.AuraTerms.CURRENCY, "Horas") ?? "Horas")
                        : "Quants";
                    string pattern = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_ORACLE_POST_CREATED, "✨ Оракул створив запит: {0} {1}!") ?? "✨ Оракул створив запит: {0} {1}!";
                    string msg = string.Format(pattern, result.price, currencyLabel);
                    _uiManager.ShowToast(msg, "hint");
                }
            }
        }

        private async UniTaskVoid SearchLocationAsync(string query, CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(400, cancellationToken: cancellationToken);

                if (_geocodingService == null) return;
                var results = await _geocodingService.SearchLocationAsync(query);

                if (cancellationToken.IsCancellationRequested) return;

                if (_locationResultsList == null) return;

                _locationResultsList.Clear();

                if (results == null || results.Count == 0)
                {
                    _locationResultsList.style.display = DisplayStyle.None;
                    return;
                }

                _locationResultsList.style.display = DisplayStyle.Flex;
                foreach (var res in results)
                {
                    var locationName = res.DisplayName;
                    var btn = new Button();
                    btn.text = locationName;
                    btn.AddToClassList("location-result-btn");
                    btn.clicked += () => SetSelectedLocation(locationName);
                    _locationResultsList.Add(btn);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ConvergenceFeed] Location search error: {ex.Message}");
            }
        }

        private void SetSelectedLocation(string location)
        {
            _selectedLocation = location;
            if (_inputLocationSearch != null) _inputLocationSearch.value = location;
            if (_lblSelectedLocation != null)
            {
                _lblSelectedLocation.text = $"✔ {location}";
                _lblSelectedLocation.style.display = DisplayStyle.Flex;
            }
            if (_locationResultsList != null) _locationResultsList.style.display = DisplayStyle.None;
        }

        private void SetModalRealm(ExchangeRealm realm)
        {
            _modalSelectedRealm = realm;
            if (_btnModalEther != null) SetButtonActiveState(_btnModalEther, realm == ExchangeRealm.Ether);
            if (_btnModalMatter != null) SetButtonActiveState(_btnModalMatter, realm == ExchangeRealm.Matter);
            
            if (_inputPostPrice != null)
            {
                _inputPostPrice.label = realm == ExchangeRealm.Ether ? "ЧАС (ХОРИ)" : "ЦІНА (USD)";
            }
        }

        private void PublishPost()
        {
            string content = _inputPostContent?.value ?? "";
            string categoryStr = _inputPostCategory?.value ?? "";
            string city = _selectedLocation;
            float priceVal = 0f;
            float.TryParse(_inputPostPrice?.value ?? "0", out priceVal);

            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("[ConvergenceFeed] Cannot publish post: Empty content.");
                return;
            }

            ServiceCategory cat = ServiceCategory.All;
            if (Enum.TryParse<ServiceCategory>(categoryStr, true, out var parsedCat))
            {
                cat = parsedCat;
            }
            else
            {
                string cLower = categoryStr.ToLower();
                if (cLower.Contains("code") || cLower.Contains("it") || cLower.Contains("прог")) cat = ServiceCategory.Code;
                else if (cLower.Contains("teach") || cLower.Contains("навч")) cat = ServiceCategory.Teaching;
                else if (cLower.Contains("craft") || cLower.Contains("рем")) cat = ServiceCategory.Craft;
                else if (cLower.Contains("art") || cLower.Contains("мист")) cat = ServiceCategory.Art;
                else if (cLower.Contains("nature") || cLower.Contains("прир")) cat = ServiceCategory.Nature;
            }

            var profile = _authManager != null ? _authManager.CurrentProfile : null;
            string username = profile != null ? profile.DisplayName : "Adept";
            string userId = profile != null ? profile.UserId : "user_self";

            var newPost = new Post
            {
                postId = $"post_{Guid.NewGuid():N}",
                userId = userId,
                username = username,
                content = content,
                postType = PostType.ServiceRequest,
                serviceCategory = cat,
                horasPrice = _modalSelectedRealm == ExchangeRealm.Ether ? (long)priceVal : 0L,
                priceType = priceVal > 0f ? PriceType.Fixed : PriceType.Free,
                distanceKm = 0.1f,
                isUrgent = true,
                authorCity = string.IsNullOrEmpty(city) || city.Contains("Online") ? "Online" : city,
                realm = _modalSelectedRealm,
                priceAtoms = _modalSelectedRealm == ExchangeRealm.Ether && _timeWalletService != null ? _timeWalletService.HorasToMinutes(priceVal) : 0L,
                priceWaves = _modalSelectedRealm == ExchangeRealm.Matter && _walletService != null ? _walletService.QuantsToWaves(priceVal) : 0L,
                likesCount = 0,
                commentsCount = 0,
                createdAt = DateTime.UtcNow
            };

            _allPosts.Insert(0, newPost);
            ApplyFiltersAndRefreshList();
            CloseCreatePostModal();
            Debug.Log($"[ConvergenceFeed] New service request published: ID={newPost.postId}, realm={newPost.realm}");
        }

        public void UpdateLocalization()
        {
            if (_root == null || _localization == null) return;

            bool isUk = _localization.CurrentLanguage == SystemLanguage.Ukrainian;

            var feedHeader = _root.Q<Label>("FeedHeader");
            if (feedHeader != null)
            {
                feedHeader.text = isUk ? "СТРІЧКА" : "FEED";
            }

            var feedSubtitle = _root.Q<Label>("FeedSubtitle");
            if (feedSubtitle != null)
            {
                feedSubtitle.text = isUk ? "ХРОНІКИ ЦАРСТВА" : "CHRONICLES OF THE REALM";
            }

            if (_prophecyTitle != null)
            {
                _prophecyTitle.text = isUk ? "🔮 ГЛОБАЛЬНЕ ПРОРОЦТВО" : "🔮 GLOBAL PROPHECY";
            }

            if (_lastProphecyEvent != null)
            {
                var evt = _lastProphecyEvent.Value;
                _lblProphecyBonus.text = isUk 
                    ? $"Рек. дія: {evt.RecommendedAction} (Множник: x{evt.BonusMultiplier})" 
                    : $"Rec. action: {evt.RecommendedAction} (Multiplier: x{evt.BonusMultiplier})";
            }

            var btnAll = _root.Q<Button>("BtnRealmAll");
            var btnEther = _root.Q<Button>("BtnRealmEther");
            var btnMatter = _root.Q<Button>("BtnRealmMatter");

            if (btnAll != null) btnAll.text = isUk ? "ВСІ" : "ALL";
            if (btnEther != null) btnEther.text = isUk ? "🌌 ЕФІР" : "🌌 ETHER";
            if (btnMatter != null) btnMatter.text = isUk ? "💵 МАТЕРІЯ" : "💵 MATTER";

            _filter?.UpdateLocalization();
            ApplyFiltersAndRefreshList();
        }
    }
}
