using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Localization;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Economy;
using TimeAura.Features.Harmony;
using TimeAura.Features.Localization;
using TimeAura.Core.Utils;
using TimeAura.Core.Services;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TimeAura.Features.Harmony
{
    /// <summary>
    /// HarmonyChannelController — Manages the Harmony Channel chat UI.
    /// Updated with Sensory Awakening audio hooks and loading states.
    /// </summary>
    public class HarmonyChannelController
    {
        private readonly VisualElement _root;
        private readonly AuthManager _authManager;
        private readonly IDataService _dataService;
        private readonly LocalizationManager _localization;
        private readonly Economy.HorasEconomyService _economy;
        private readonly MediaService _media;
        private readonly IAuraOracleService _oracle;
        private readonly AudioService _audio;

        private HarmonySession _session;
        private UserProfile _partner;
        private HarmonyContext _context; // stored for deal summary card

        private VisualElement _partnerVisage;
        private Label _lblPartnerName;
        private Label _lblPartnerTitle;
        private Label _lblPartnerLocation;
        private Label _lblPartnerStatus;
        private Label _lblHarmonyStatus;
        private Label _lblTimeRemaining;
        private VisualElement _timerFill;
        
        private VisualElement _contextBanner;
        private Label _lblContextTitle, _lblContextPrice;

        private ScrollView _messageList;
        private TextField _inputMessage;
        private Button _btnSendMessage, _btnSendHoras, _btnCloseHarmony, _btnAttachMaterial, _btnBack;
        private List<ChatMessage> _localMessages = new List<ChatMessage>();

        private VisualElement _horasWidget;
        private Label _lblHorasProposal;
        private Button _btnAcceptHoras, _btnDeclineHoras;

        private VisualElement _closeModal;
        private Button _btnConfirmClose, _btnCancelClose;

        private VisualElement _escrowModal;
        private Label _lblProphecy;
        private Button _btnSealPact, _btnCompleteHarmony;
        private Button _btnConfirmEscrow, _btnCancelEscrow;
        private TextField _inputEscrowAmount;

        private VisualElement _gratitudeModal;
        private TextField _inputGratitude;
        private Button _btnSendGratitude;

        // Deal Agreement UI
        private VisualElement _dealSummaryModal;
        private VisualElement _dealActionBar;
        private Button _btnAgreeToTerms;
        private Button _btnDeclineTerms;
        private Button _btnCancelDeal;
        private Label _lblDealDesc;
        private Label _lblDealPrice;
        private Label _lblDealRealm;
        private Label _lblDealPartner;

        private long _lockedAmount = 0;
        private IDisposable _langSubscription;
        private IDisposable _messageListener;
        private IDisposable _sysMsgListener;
        private CancellationTokenSource _timerCts;
        private TimeSpan _totalWindow = TimeSpan.FromHours(24);
        private int _pendingHorasAmount = 0;

        public event Action OnHarmonyDissolved;
        public event Action<UserProfile, string> OnInitiateRiteRequested;
        public event Action OnClosed;

        public HarmonyChannelController(
            VisualElement root,
            AuthManager authManager,
            IDataService dataService,
            LocalizationManager localization,
            Economy.HorasEconomyService economy,
            MediaService media,
            IAuraOracleService oracle,
            AudioService audio)
        {
            _root = root;
            _authManager = authManager;
            _dataService = dataService;
            _localization = localization;
            _economy = economy;
            _media = media;
            _oracle = oracle;
            _audio = audio;
            _langSubscription = EventBus.Subscribe<LanguageChangedEvent>(e => UpdateLocalization());
            _sysMsgListener = EventBus.Subscribe<SystemMessageEvent>(e => AddSystemMessage($"[СУД ХРОНОСУ] {e.Text}"));
            
            BindElements();
            _root.style.display = DisplayStyle.None;
            _root.style.opacity = 0f;
        }

        public void Dispose()
        {
            _langSubscription?.Dispose();
            _messageListener?.Dispose();
            _sysMsgListener?.Dispose();
            _timerCts?.Cancel();
        }

        public void Open(HarmonySession session, HarmonyContext context)
        {
            _session = session;
            _context = context;
            _partner = context.PartnerProfile;
            _lockedAmount = _session != null ? _session.lockedMinutes : 0;

            if (_lockedAmount > 0)
            {
                // Escrow already locked — show Complete, hide Deal Action Bar
                if (_btnSealPact != null) _btnSealPact.style.display = DisplayStyle.None;
                if (_btnCompleteHarmony != null) _btnCompleteHarmony.style.display = DisplayStyle.Flex;
                if (_dealActionBar != null) _dealActionBar.style.display = DisplayStyle.None;
            }
            else
            {
                // No escrow yet — show Deal Action Bar
                if (_btnSealPact != null) _btnSealPact.style.display = DisplayStyle.Flex;
                if (_btnCompleteHarmony != null) _btnCompleteHarmony.style.display = DisplayStyle.None;
                if (_dealActionBar != null) _dealActionBar.style.display = DisplayStyle.Flex;
            }

            if (context.RelatedPost != null)
            {
                if (_contextBanner != null) _contextBanner.style.display = DisplayStyle.Flex;
                if (_lblContextTitle != null) _lblContextTitle.text = context.RelatedPost.content;
                
                string priceText = "";
                if (context.RelatedPost.priceType == TimeAura.Features.Social.PriceType.Free) priceText = "🎁 ДАР";
                else if (context.RelatedPost.priceType == TimeAura.Features.Social.PriceType.Negotiate) priceText = "🤝 Домовитись";
                else priceText = context.RelatedPost.realm == TimeAura.Features.Social.ExchangeRealm.Matter ? EconomyFormatter.FormatQuants(context.RelatedPost.priceWaves) : EconomyFormatter.FormatHoras(context.RelatedPost.priceAtoms);
                
                if (_lblContextPrice != null) _lblContextPrice.text = priceText;
            }
            else
            {
                if (_contextBanner != null) _contextBanner.style.display = DisplayStyle.None;
            }

            if (_partner != null)
            {
                if (_lblPartnerName != null) _lblPartnerName.text = !string.IsNullOrEmpty(_partner.Nickname) ? _partner.Nickname : "Unknown Adept";
                if (_lblPartnerTitle != null) _lblPartnerTitle.text = !string.IsNullOrEmpty(_partner.Bio) ? _partner.Bio : "Time Weaver";
                if (_lblPartnerLocation != null) _lblPartnerLocation.text = !string.IsNullOrEmpty(_partner.LocationZone) ? _partner.LocationZone : "Unknown Zone";
                if (_lblPartnerStatus != null) _lblPartnerStatus.text = "In Harmony";
            }

            UpdateLocalization();

            if (_session.status == HarmonyStatus.Disputed)
            {
                TransformToAltarOfJustice();
            }

            StartTimer();
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 1f;
            
            var innerRoot = _root.Q("HarmonyChannelRoot");
            if (innerRoot != null)
            {
                innerRoot.style.display = DisplayStyle.Flex;
                innerRoot.style.opacity = 1f;
            }
            _messageListener?.Dispose();
            _messageListener = _dataService.ListenToHarmonyMessages(_session.sessionId, OnMessagesReceived);
            
            if (_messageListener == null)
            {
                Debug.Log("[Harmony] 🕯️ Firebase connection not active. Using local memory storage.");
                OnMessagesReceived(_localMessages);
            }
        }

        private void OnMessagesReceived(List<ChatMessage> messages)
        {
            if (_messageList == null) return;
            _messageList.Clear();
            
            var row = new VisualElement(); row.AddToClassList("msg-system");
            var sysLabel = new Label("👁️ Оракул з'єднав ваші нитки долі. Шановні Адепти, обговоріть деталі вашої Симетрії. Нехай Хори течуть справедливо."); 
            sysLabel.AddToClassList("msg-system-text");
            row.Add(sysLabel); _messageList.Add(row);

            foreach (var msg in messages)
            {
                bool isSelf = msg.SenderId == _authManager.CurrentProfile.UserId;
                RenderMessage(msg, isSelf);
            }
            ScrollToBottom();
        }

        public void Close()
        {
            _timerCts?.Cancel();
            _root.style.display = DisplayStyle.None;
            OnClosed?.Invoke();
        }

        public void HideSilently()
        {
            _timerCts?.Cancel();
            _root.style.display = DisplayStyle.None;
        }

        public bool IsVisible => _root != null && _root.style.display == DisplayStyle.Flex;

        private void BindElements()
        {
            _partnerVisage = _root.Q("PartnerVisage");
            _lblPartnerName = _root.Q<Label>("LblPartnerName");
            _lblPartnerTitle = _root.Q<Label>("LblPartnerTitle");
            _lblPartnerLocation = _root.Q<Label>("LblPartnerLocation");
            _lblPartnerStatus = _root.Q<Label>("LblPartnerStatus");
            _lblHarmonyStatus = _root.Q<Label>("LblHarmonyStatus");
            _lblTimeRemaining = _root.Q<Label>("LblTimeRemaining");
            _timerFill = _root.Q("TimerFill");
            
            _contextBanner = _root.Q("ContextBanner");
            _lblContextTitle = _root.Q<Label>("LblContextTitle");
            _lblContextPrice = _root.Q<Label>("LblContextPrice");
            
            _messageList = _root.Q<ScrollView>("MessageList");
            _inputMessage = _root.Q<TextField>("InputMessage");
            _btnSendMessage = _root.Q<Button>("BtnSendMessage");
            _btnSendHoras = _root.Q<Button>("BtnSendHoras");
            _btnCloseHarmony = _root.Q<Button>("BtnCloseHarmony");
            _btnBack = _root.Q<Button>("BtnBack");
            _btnAttachMaterial = _root.Q<Button>("BtnAttachMaterial");
            _horasWidget = _root.Q("HorasWidget");
            _lblHorasProposal = _root.Q<Label>("LblHorasProposal");
            _btnAcceptHoras = _root.Q<Button>("BtnAcceptHoras");
            _btnDeclineHoras = _root.Q<Button>("BtnDeclineHoras");
            _closeModal = _root.Q("CloseHarmonyModal");
            _btnConfirmClose = _root.Q<Button>("BtnConfirmClose");
            _btnCancelClose = _root.Q<Button>("BtnCancelClose");
            _escrowModal = _root.Q("EscrowModal");
            _lblProphecy = _root.Q<Label>("LblProphecy");
            _btnSealPact = _root.Q<Button>("BtnSealPact");
            _btnCompleteHarmony = _root.Q<Button>("BtnCompleteHarmony");
            _btnConfirmEscrow = _root.Q<Button>("BtnConfirmEscrow");
            _btnCancelEscrow = _root.Q<Button>("BtnCancelEscrow");
            _inputEscrowAmount = _root.Q<TextField>("InputEscrowAmount");
            _gratitudeModal = _root.Q("GratitudeModal");
            _inputGratitude = _root.Q<TextField>("InputGratitude");
            _btnSendGratitude = _root.Q<Button>("BtnSendGratitude");

            // Deal Agreement bindings — Q() BEFORE wiring events
            _dealSummaryModal = _root.Q("DealSummaryModal");
            _dealActionBar    = _root.Q("DealActionBar");
            _btnAgreeToTerms  = _root.Q<Button>("BtnAgreeToTerms");
            _btnDeclineTerms  = _root.Q<Button>("BtnDeclineTerms");
            _btnCancelDeal    = _root.Q<Button>("BtnCancelDeal");
            _lblDealDesc      = _root.Q<Label>("LblDealDesc");
            _lblDealPrice     = _root.Q<Label>("LblDealPrice");
            _lblDealRealm     = _root.Q<Label>("LblDealRealm");
            _lblDealPartner   = _root.Q<Label>("LblDealPartner");

            if (_btnSendMessage != null)    _btnSendMessage.clicked    += OnSendMessage;
            if (_btnSendHoras != null)      _btnSendHoras.clicked      += OnSendHorasOffer;
            if (_btnCloseHarmony != null)   _btnCloseHarmony.clicked   += OnRequestClose;
            if (_btnAcceptHoras != null)    _btnAcceptHoras.clicked    += OnAcceptHorasOffer;
            if (_btnDeclineHoras != null)   _btnDeclineHoras.clicked   += OnDeclineHorasOffer;
            if (_btnConfirmClose != null)   _btnConfirmClose.clicked   += () => DissolveHarmonyAsync().Forget();
            if (_btnCancelClose != null)    _btnCancelClose.clicked    += () => SetModalVisible(_closeModal, false);
            if (_btnBack != null)           _btnBack.clicked           += () => Close();
            if (_btnAttachMaterial != null) _btnAttachMaterial.clicked += OnAttachMaterialBeta;
            // BtnSealPact fires OnInitiateRiteRequested — NexusController shows Scales of Chronos
            if (_btnSealPact != null)       _btnSealPact.clicked       += () => { CloseDealSummaryModal(); OnInitiateRiteRequested?.Invoke(_partner, GetChatHistoryContext()); };
            if (_btnCompleteHarmony != null) _btnCompleteHarmony.clicked += () => CompleteHarmonyAsync().Forget();
            if (_btnSendGratitude != null)  _btnSendGratitude.clicked  += () => SendGratitudeAsync().Forget();

            // Deal Agreement buttons
            if (_btnAgreeToTerms != null)  _btnAgreeToTerms.clicked  += OnAgreeToTerms;
            if (_btnDeclineTerms != null)  _btnDeclineTerms.clicked  += OnDeclineTerms;
            if (_btnCancelDeal != null)    _btnCancelDeal.clicked    += CloseDealSummaryModal;
            // Note: BtnConfirmEscrow / BtnCancelEscrow are wired by NexusController.OpenEscrowModal()

            if (_inputMessage != null)
            {
                _inputMessage.RegisterCallback<KeyDownEvent>(e => {
                    if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) OnSendMessage();
                });
            }

            SetHorasWidgetVisible(false);
            SetModalVisible(_closeModal, false);
            SetModalVisible(_escrowModal, false);
        }

        private void UpdateLocalization()
        {
            if (_partner == null || _localization == null) return;
            _lblPartnerName.text = _partner.Nickname;
            string title = _localization.Get(_partner.AuraTitle, _partner.AuraTitle);
            _lblPartnerTitle.text = title.ToUpper();
            
            if (_lblPartnerLocation != null)
                _lblPartnerLocation.text = string.IsNullOrEmpty(_partner.LocationZone) ? "📍 Незвідані Землі" : $"📍 {_partner.LocationZone}";
                
            if (_lblPartnerStatus != null)
                _lblPartnerStatus.text = $"✦ Lvl {_partner.Status}";

            if (ColorUtility.TryParseHtmlString(_partner.AuraColorHex ?? "#FFD700", out var c)) {
                _lblPartnerTitle.style.color = c;
                _partnerVisage.style.backgroundColor = c;
            }
            _lblHarmonyStatus.text = _localization.Get(AuraTerms.HARMONY_STATUS_ACTIVE, "Active");
            if (_btnSendHoras != null) _btnSendHoras.text = _localization.Get(AuraTerms.CURRENCY, "HORAS");
            if (_btnCloseHarmony != null) _btnCloseHarmony.text = "×";

            if (_btnSealPact != null)
                _btnSealPact.text = _localization.Get("harmony_seal_pact", "✦ СКРІПИТИ ДОГОВІР ✦");
            if (_btnCompleteHarmony != null)
                _btnCompleteHarmony.text = _localization.Get("harmony_complete", "✦ ЗАВЕРШИТИ ГАРМОНІЮ ✦");
        }

        private void StartTimer()
        {
            _timerCts = new CancellationTokenSource();
            RunTimerAsync(_timerCts.Token).Forget();
        }

        private async UniTaskVoid RunTimerAsync(CancellationToken cancellationToken)
        {
            var remaining = _totalWindow - (DateTime.UtcNow - _session.startTime);
            while (remaining.TotalSeconds > 0 && !cancellationToken.IsCancellationRequested)
            {
                if (_lblTimeRemaining != null) _lblTimeRemaining.text = remaining.ToString(@"hh\:mm\:ss");
                if (_timerFill != null) _timerFill.style.width = Length.Percent((float)(remaining.TotalSeconds / _totalWindow.TotalSeconds) * 100f);
                await UniTask.Delay(1000, cancellationToken: cancellationToken);
                remaining -= TimeSpan.FromSeconds(1);
            }
            if (!cancellationToken.IsCancellationRequested) await DissolveHarmonyAsync();
        }

        private void OnSendMessage()
        {
            string text = _inputMessage?.value?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            
            // Перевіряємо, чи є якісь активні модальні вікна, які можна закрити командою
            if (IsAnyModalActive())
            {
                ProcessChatCommand(text.ToLowerInvariant());
            }
            
            var msg = new ChatMessage(_authManager.CurrentProfile.UserId, text);
            
            if (_messageListener == null)
            {
                _localMessages.Add(msg);
                OnMessagesReceived(_localMessages);
            }
            else
            {
                _dataService.SendHarmonyMessageAsync(_session.sessionId, msg).Forget();
            }
            
            _audio?.PlaySFX("MessageSent2");
            _inputMessage.value = string.Empty;
        }

        /// <summary>
        /// Перевіряє, чи є активні модальні вікна, які можна закрити командами
        /// </summary>
        private bool IsAnyModalActive()
        {
            return (_dealSummaryModal != null && _dealSummaryModal.ClassListContains("modal--hidden") == false) ||
                   (_escrowModal != null && _escrowModal.ClassListContains("modal--hidden") == false) ||
                   (_closeModal != null && _closeModal.ClassListContains("modal--hidden") == false);
        }

        /// <summary>
        /// Обробляє команди з чату для закриття модальних вікон
        /// </summary>
        private void ProcessChatCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            // Перевіряємо команди для відхилення/скасування
            if (command.Contains("відхилити") || command.Contains("відмінити") || 
                command.Contains("відміна") || command.Contains("скасувати") || 
                command.Contains("cancel") || command.Contains("decline") || 
                command.Contains("reject") || command.Contains("ні") || 
                command.Contains("no"))
            {
                // Закриваємо активні модальні вікна
                if (_dealSummaryModal != null && !_dealSummaryModal.ClassListContains("modal--hidden"))
                {
                    CloseDealSummaryModal();
                    AddSystemMessage("📋 Умови угоди відхилено. Вікно закрито.");
                }
                else if (_escrowModal != null && !_escrowModal.ClassListContains("modal--hidden"))
                {
                    SetModalVisible(_escrowModal, false);
                    AddSystemMessage("💳 Ескроу вікно відхилено. Вікно закрито.");
                }
                else if (_closeModal != null && !_closeModal.ClassListContains("modal--hidden"))
                {
                    SetModalVisible(_closeModal, false);
                    AddSystemMessage("🚪 Вікно виходу відхилено. Вікно закрито.");
                }
            }
        }

        private void OnAttachMaterialBeta()
        {
            _audio?.PlaySFX("OracleMessage");
            AddSystemMessage(_localization.Get(AuraTerms.BETA_FEATURE_NOTICE, "👁️ Оракул: Ця функція проявиться у наступних циклах (Beta)"));
        }

        private async UniTaskVoid AttachMaterialAsync()
        {
            if (_btnAttachMaterial == null) return;
            
            _btnAttachMaterial.SetEnabled(false);
            string originalText = _btnAttachMaterial.text;
            _btnAttachMaterial.text = _localization.Get(AuraTerms.PRESERVING, "Preserving...");

            try {
                byte[] data = await _media.CaptureMaterialAsync();
                if (data == null) return;
                
                AddSystemMessage(_localization.Get(AuraTerms.PRESERVING, "Preserving material in the ether..."));
                string mediaId = Guid.NewGuid().ToString("N");
                string url = await _media.PreserveMaterialAsync(data, _session.sessionId, mediaId);
                
                if (!string.IsNullOrEmpty(url)) {
                    var msg = new ChatMessage(_authManager.CurrentProfile.UserId, "shared a Material", ChatMessageType.Material, url);
                    await _dataService.SendHarmonyMessageAsync(_session.sessionId, msg);
                    _audio?.PlaySFX("MessageSent2");
                }
            } finally {
                _btnAttachMaterial.SetEnabled(true);
                _btnAttachMaterial.text = originalText;
            }
        }

        private void OpenEscrowModal()
        {
            SetModalVisible(_escrowModal, true);
            UpdateProphecyAsync().Forget();
        }

        private async UniTaskVoid UpdateProphecyAsync()
        {
            if (_lblProphecy == null) return;
            _lblProphecy.text = "Oracle analyzing the flow...";
            string context = "Professional exchange of assets and skills.";
            string prophecy = await _oracle.GetProphecyAsync(context);
            _lblProphecy.text = $"👁️ Пророцтво: {prophecy}";
        }

        /// <summary>
        /// Called by NexusController after Oracle evaluation or manual entry.
        /// Locks the given amount of Horas in escrow and activates the Harmony Chamber.
        /// </summary>
        public async UniTask ConfirmHarmonySeal(long amount = 1)
        {
            if (amount < 1) amount = 1;
            ContractRealm realm = _session?.realm ?? ContractRealm.Ether;
            long fiat = _session?.fiatAmountCents ?? 0L;
            
            bool success = await _economy.LockFundsAsync(_authManager.CurrentProfile, realm, amount, fiat, _session?.sessionId, "", _partner);
            if (success)
            {
                _lockedAmount = amount;
                if (_session != null) _session.lockedMinutes = amount;
                _audio?.PlaySFX("HorasTransfer");
                AddSystemMessage($"✶ Кошти запечатано у Ескроу сховищі. Кімната активна.");
                // After lock: hide Deal Action Bar, show Complete button
                if (_dealActionBar != null) _dealActionBar.style.display = DisplayStyle.None;
                if (_btnSealPact != null) _btnSealPact.style.display = DisplayStyle.None;
                if (_btnCompleteHarmony != null) _btnCompleteHarmony.style.display = DisplayStyle.Flex;
            }
            else
            {
                AddSystemMessage("⚠️ Ритуал не вдався. Недостатньо Хорасів на балансі.");
            }
        }

        private async UniTaskVoid CompleteHarmonyAsync()
        {
            AddSystemMessage("✦ Finalizing the Seal... ✦");
            ContractRealm realm = _session?.realm ?? ContractRealm.Ether;
            long fiat = _session?.fiatAmountCents ?? 0L;
            
            await _economy.ReleaseFundsToReceiverAsync(_authManager.CurrentProfile, _partner, realm, _lockedAmount, fiat, _session?.sessionId);
            _audio?.PlaySFX("HorasTransfer");
            SetModalVisible(_gratitudeModal, true);
        }

        private async UniTaskVoid SendGratitudeAsync()
        {
            string text = _inputGratitude.value;
            if (!string.IsNullOrEmpty(text)) {
                _partner.AddLegacyEntry(text);
                
                // Add Constellation Star
                var record = new UserProfile.SymmetryRecord {
                    PartnerId = _authManager.CurrentProfile.UserId,
                    PartnerName = _authManager.CurrentProfile.DisplayName,
                    Category = _session?.realm.ToString() ?? "Ether",
                    minutesExchanged = _lockedAmount,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    MapX = UnityEngine.Random.value,
                    MapY = UnityEngine.Random.value
                };
                _partner.AddConstellationStar(record);
                
                await _dataService.SaveUserProfileAsync(_partner, default);
                _audio?.PlaySFX("MessageSent2");
            }
            SetModalVisible(_gratitudeModal, false);
            await UniTask.Delay(1000);
            OnHarmonyDissolved?.Invoke();
            Close();
        }

        private void OnSendHorasOffer() {
            _pendingHorasAmount = Mathf.Max(1, (int)(_authManager.CurrentProfile.Horas * 0.1f));
            SetHorasWidgetVisible(true);
            if (_lblHorasProposal != null) _lblHorasProposal.text = $"Proposal: {_pendingHorasAmount} Horas";
        }

        private void OnAcceptHorasOffer() { _audio?.PlaySFX("HorasTransfer"); SetHorasWidgetVisible(false); }
        private void OnDeclineHorasOffer() { SetHorasWidgetVisible(false); }

        // ── Deal Agreement ────────────────────────────────────────────────────────

        private void OnAgreeToTerms()
        {
            _audio?.PlaySFX("OracleMessage");
            AddSystemMessage("✅ Адепт запропонував умови угоди. Очікування підтвердження...");
            ShowDealSummaryModal();
        }

        private void OnDeclineTerms()
        {
            _audio?.PlaySFX("AuraResonance");
            AddSystemMessage("❌ Адепт відхилив поточні умови. Продовжуйте обговорення.");
        }

        private void ShowDealSummaryModal()
        {
            // Fill deal card from context
            var post = _context?.RelatedPost;
            if (_lblDealDesc != null)
                _lblDealDesc.text = post != null && !string.IsNullOrEmpty(post.content)
                    ? post.content
                    : "Умови узгоджені в чаті";

            if (_lblDealPrice != null)
            {
                if (post != null && post.priceType == TimeAura.Features.Social.PriceType.Free)
                    _lblDealPrice.text = "🎁 ДАР (Безкоштовно)";
                else if (post != null && post.realm == TimeAura.Features.Social.ExchangeRealm.Matter)
                    _lblDealPrice.text = EconomyFormatter.FormatQuants(post.priceWaves);
                else if (post != null)
                    _lblDealPrice.text = EconomyFormatter.FormatHoras(post.priceAtoms);
                else
                    _lblDealPrice.text = "— домовились в чаті";
            }

            if (_lblDealRealm != null)
            {
                if (post != null)
                    _lblDealRealm.text = post.realm == TimeAura.Features.Social.ExchangeRealm.Matter
                        ? "💵 Фіат (USD)"
                        : "🌌 Ефір (Хори)";
                else
                    _lblDealRealm.text = _session?.realm == ContractRealm.Material ? "💵 Фіат (USD)" : "🌌 Ефір (Хори)";
            }

            if (_lblDealPartner != null)
                _lblDealPartner.text = _partner != null
                    ? (!string.IsNullOrEmpty(_partner.Nickname) ? _partner.Nickname : _partner.DisplayName)
                    : "Невідомий Адепт";

            Debug.Log("[Popup] Opened: DealSummaryModal (Умови Угоди)");
            SetModalVisible(_dealSummaryModal, true);
        }

        private void CloseDealSummaryModal()
        {
            SetModalVisible(_dealSummaryModal, false);
        }

        private void RenderMessage(ChatMessage msg, bool isSelf)
        {
            var row = new VisualElement(); row.AddToClassList("msg-row"); row.AddToClassList(isSelf ? "msg-row--self" : "msg-row--other");
            var bubble = new VisualElement(); bubble.AddToClassList("msg-bubble"); bubble.AddToClassList(isSelf ? "msg-bubble--self" : "msg-bubble--other");
            if (msg.Type == ChatMessageType.Material && !string.IsNullOrEmpty(msg.ImageUrl)) {
                var imgContainer = new VisualElement(); imgContainer.AddToClassList("msg-material"); bubble.Add(imgContainer);
                LoadMaterialImage(imgContainer, msg.ImageUrl).Forget();
            } else {
                var msgLabel = new Label(msg.Text); msgLabel.AddToClassList("msg-text"); bubble.Add(msgLabel);
            }
            var timeLabel = new Label(msg.FormattedTime); timeLabel.AddToClassList("msg-time"); bubble.Add(timeLabel);
            row.Add(bubble); _messageList?.Add(row);
        }

        private async UniTaskVoid LoadMaterialImage(VisualElement container, string url) {
            var tex = await ImageLoader.LoadTextureAsync(url);
            if (tex != null) {
                container.style.backgroundImage = new StyleBackground(tex);
                float aspect = (float)tex.width / tex.height;
                container.style.width = 240 * aspect; container.style.height = 240;
            }
        }

        private void AddSystemMessage(string text) {
            var row = new VisualElement(); row.AddToClassList("msg-system");
            var sysLabel = new Label(text); sysLabel.AddToClassList("msg-system-text");
            row.Add(sysLabel); _messageList?.Add(row); ScrollToBottom();
        }

        private void ScrollToBottom() => _messageList?.schedule.Execute(() => _messageList.scrollOffset = new Vector2(0, float.MaxValue)).ExecuteLater(50);
        private void SetHorasWidgetVisible(bool visible) => _horasWidget?.EnableInClassList("horas-widget--hidden", !visible);
        private void SetModalVisible(VisualElement modal, bool visible)
        {
            if (modal != null)
            {
                modal.EnableInClassList("modal--hidden", !visible);
                if (visible)
                {
                    Debug.Log($"[Popup] Opened: {modal.name}");
                }
            }
        }
        private void OnRequestClose() => SetModalVisible(_closeModal, true);

        private async UniTask DissolveHarmonyAsync() {
            SetModalVisible(_closeModal, false);
            AddSystemMessage("✦ Harmony has been peacefully closed. ✦");
            await UniTask.Delay(1200); OnHarmonyDissolved?.Invoke(); Close();
        }

        private void TransformToAltarOfJustice()
        {
            if (_root == null) return;

            // Change background and headers to Altar of Justice aesthetic
            _root.AddToClassList("altar-mode");

            // Change Title
            var titleLabel = _root.Q<Label>("LblHeaderTitle");
            if (titleLabel != null) titleLabel.text = _localization.Get("altar_of_justice", "ВІВТАР ПРАВОСУДДЯ");

            if (_lblHarmonyStatus != null)
            {
                _lblHarmonyStatus.text = _localization.Get("harmony_status_disputed", "Суперечка. Кошти Заморожено");
                _lblHarmonyStatus.style.color = new StyleColor(new Color(0.8f, 0.2f, 0.2f)); // Crimson red
            }

            // Change Attach button text
            if (_btnAttachMaterial != null)
            {
                _btnAttachMaterial.text = _localization.Get("harmony_attach_evidence", "Надати Доказ");
            }
            
            // Re-bind click event to load evidence instead of regular material (if needed, or reuse AttachMaterialAsync)
            // _btnAttachMaterial.clicked -= OnAttachMaterialBeta;
            // _btnAttachMaterial.clicked += AttachEvidenceBeta;
            
            _audio?.PlaySFX("AuraResonance");
        }

        public string GetChatHistoryContext()
        {
            var sb = new System.Text.StringBuilder();
            if (_messageList != null)
            {
                foreach (var child in _messageList.Children())
                {
                    var bubble = child.Q(className: "msg-bubble");
                    if (bubble != null)
                    {
                        var textLabel = bubble.Q<Label>(className: "msg-text");
                        if (textLabel != null)
                        {
                            bool isSelf = child.ClassListContains("msg-row--self");
                            string senderName = isSelf ? "Adept (Me)" : (_partner != null ? _partner.DisplayName : "Partner");
                            sb.AppendLine($"{senderName}: {textLabel.text}");
                        }
                    }
                    else if (child.ClassListContains("msg-system"))
                    {
                        var sysText = child.Q<Label>(className: "msg-system-text");
                        if (sysText != null)
                        {
                            sb.AppendLine($"[SYSTEM]: {sysText.text}");
                        }
                    }
                }
            }
            return sb.ToString();
        }
    }
}
