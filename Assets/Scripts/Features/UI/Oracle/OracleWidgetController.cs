using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TimeAura.Core.Services;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using VContainer;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Localization;
using TimeAura.Core.Localization;
using TimeAura.Core.Data.SO;

namespace TimeAura.Features.UI.Oracle
{
    public class OracleWidgetController : MonoBehaviour
    {
        private static OracleWidgetController _instance;
        public static OracleWidgetController Instance => _instance;

        public enum OracleWidgetState
        {
            Closed,
            Open,
            MiniChat
        }
        private OracleWidgetState _widgetState = OracleWidgetState.Closed;
        public OracleWidgetState WidgetState => _widgetState;

        [Header("Components")]
        [SerializeField] private UIDocument _uiDocument;
        
        [Header("🎨 Visual Assets")]
        [SerializeField] private Sprite _eyeSprite;
        [SerializeField] private Sprite _pupilSprite;
        
        [Header("📐 Eye Scaling (Tuning)")]
        [Range(50, 800)]
        [SerializeField] private float _eyeSize = 130f;
        [Range(0.1f, 2f)]
        [SerializeField] private float _pupilScale = 0.5f;
        [SerializeField] private Vector2 _pupilVisualOffset = Vector2.zero;
        
        [Header("✨ Animation")]
        [SerializeField] private float _swayAmount = 5f;
        [SerializeField] private float _swaySpeed = 1.5f;

        [Header("🔊 Audio (SFX Names)")]
        // [SerializeField] private string _interactionSfx = "MessageSent2";
        [SerializeField] private string _snapSfx = "RadarPulse";
        [SerializeField] private string _whisperSfx = "MessageSent2";

        [Header("State")]
        [SerializeField] private OracleState _currentState = OracleState.Active;

        private VisualElement _root;
        private VisualElement _eyeRoot;
        private VisualElement _eyeContainer;
        private VisualElement _pupil;
        private VisualElement _hintBubble;
        private Label _hintLabel;
        
        // Chat UI (Managed by GeminiChatController, but references kept for initialization if needed)
        private VisualElement _chatOverlay;
        private TextField _chatInput;
        private ScrollView _chatScrollView;
        private Button _btnSend;
        private Button _btnCloseChat;

        private IAuraOracleService _oracleService;
        private HapticService _haptic;
        private AudioService _audio;
        private LocalizationManager _localization;
        private TimeAura.Features.Auth.AuthManager _auth;

        private Vector2 _pupilTargetPos;
        private bool _isDragging;
        private Vector2 _dragOffset;
        private Vector2 _mouseDownPos;
        
        private CancellationTokenSource _widgetCts;
        private CancellationTokenSource _typeCts;
        

        private bool _hasPositionedDefault = false;
        private int _oracleAlertLevel = 0;
        private IVisualElementScheduledItem _menuButtonParticleTask;

        private readonly string[] _mysticalPhrases = new[]
        {
            "The threads of time are weaving...",
            "I sense a shift in the cosmic flow.",
            "The stars whisper of new symmetries.",
            "The Nexus is breathing with your aura.",
            "Silence is the language of the Oracle.",
            "Every moment is a new convergence.",
            "Your resonance reaches across the void.",
            "Watch the patterns, Seeker.",
            "The Chronos pulse is stable... for now.",
            "Communing with the ancient whispers."
        };

        private OraclePromptFactory _promptFactory;
        private IOracleService _gemini;

        [Inject]
        public void Construct(
            IAuraOracleService oracleService, 
            HapticService haptic, 
            AudioService audio, 
            LocalizationManager localization, 
            TimeAura.Features.Auth.AuthManager auth,
            OraclePromptFactory promptFactory,
            IOracleService gemini)
        {
            _oracleService = oracleService;
            _haptic = haptic;
            _audio = audio;
            _localization = localization;
            _auth = auth;
            _promptFactory = promptFactory;
            _gemini = gemini;
        }


        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            // Prevent the duplicate uncontrolled UI from GlobalManagers from rendering
            var localDoc = GetComponent<UIDocument>();
            if (localDoc != null && localDoc.rootVisualElement != null)
            {
                localDoc.rootVisualElement.style.display = DisplayStyle.None;
            }
        }

        public void SetDocument(UIDocument doc)
        {
            Debug.Log($"[OracleWidget] 👁️ Document swapped to: {(doc != null ? doc.name : "NULL")}");
            _uiDocument = doc;
            _eyeRoot = null; // Reset to force re-discovery
            Refresh();
        }

        public void Refresh()
        {
            _widgetCts?.Cancel();
            _widgetCts?.Dispose();
            _widgetCts = null;
            InitializeRoutine().Forget();
        }

        private async UniTaskVoid InitializeRoutine()
        {
            _hasPositionedDefault = false;
            
            // Cancel and recreate token source
            _widgetCts?.Cancel();
            _widgetCts?.Dispose();
            _widgetCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            var ct = _widgetCts.Token;

            float timeout = 5.0f;
            while (timeout > 0)
            {
                if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
                
                if (_uiDocument != null && _uiDocument.rootVisualElement != null && _uiDocument.rootVisualElement.childCount > 0)
                {
                    _root = _uiDocument.rootVisualElement;
                    _eyeRoot = _root.Q("OracleEyeRoot");
                    if (_eyeRoot != null) {
                        Debug.Log($"[OracleWidget] Found Eye in assigned doc: {_uiDocument.name}");
                        break;
                    }
                }
                
                timeout -= Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
            }

            if (_eyeRoot == null)
            {
                Debug.LogWarning("[OracleWidget] 👁️ OracleEyeRoot not found.");
                return;
            }

            // Bind Elements
            _eyeContainer = _eyeRoot.Q("EyeContainer");
            _pupil = _eyeRoot.Q("Pupil");
            _hintBubble = _eyeRoot.Q("HintBubble");
            _hintLabel = _eyeRoot.Q<Label>("HintLabel");
            
            _chatOverlay = _eyeRoot.Q("GeminiChatOverlay");
            
            // Link to specialized GeminiChatController if it exists
            if (GeminiChatController.Instance != null)
            {
                GeminiChatController.Instance.Initialize(_root, _eyeRoot, _eyeSize);
            }

            // Events
            _eyeRoot.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _eyeRoot.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _eyeRoot.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _eyeRoot.RegisterCallback<PointerLeaveEvent>(OnPointerUp);

            ApplyBaseTuning();
            SubscribeToService();
            
            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                _root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                
                // If geometry is already resolved (e.g. Nexus scene is loaded), position immediately!
                if (_root.resolvedStyle.width > 100 && _root.resolvedStyle.height > 100)
                {
                    float defaultLeft = _root.resolvedStyle.width - _eyeSize - 20;
                    float defaultTop = _root.resolvedStyle.height - _eyeSize - (_root.resolvedStyle.height * 0.22f);

                    _eyeRoot.style.left = defaultLeft;
                    _eyeRoot.style.top = defaultTop;
                    _hasPositionedDefault = true;
                    Debug.Log($"[OracleWidget] 👁️ Eye default position set immediately on init: left={defaultLeft}, top={defaultTop} (Screen: {_root.resolvedStyle.width}x{_root.resolvedStyle.height})");
                }
            }
            
            _eyeRoot.style.width = _eyeSize;
            _eyeRoot.style.height = _eyeSize;

            // Ensure the widget starts in a consistent open state
            OpenWidget();
            GlowPulseLoopAsync(ct).Forget();
            IdleSwayLoopAsync(ct).Forget();
            AutoBlinkLoopAsync(ct).Forget();
            RandomProphecyLoopAsync(ct).Forget();

            InitializeRoutineInternal();
        }

        private void InitializeRoutineInternal()
        {
            var btnMic = _eyeRoot.Q<VisualElement>("BtnGoldenMic") ?? _eyeRoot.Q<VisualElement>("BtnMic");
            var btnText = _eyeRoot.Q<Button>("BtnText");
            var btnExpand = _eyeRoot.Q<Button>("BtnExpand");
            var btnClose = _eyeRoot.Q<Button>("BtnClose");

            if (btnMic != null)
            {
                btnMic.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation(); // Prevent eye drag
                    btnMic.style.scale = new StyleScale(new Scale(new Vector3(1.35f, 1.35f, 1f)));
                    _haptic?.HeavyTap();
                    _audio?.PlaySFX("AuraResonance");

                    // Trigger burst
                    for (int i = 0; i < 15; i++) SpawnMicParticle(btnMic);
                    
                    // Start continuous spawning
                    _micParticleTask?.Pause();
                    _micParticleTask = btnMic.schedule.Execute(() => SpawnMicParticle(btnMic)).Every(50);

                    var capture = UnityEngine.Object.FindAnyObjectByType<VoiceCaptureService>();
                    if (capture != null) capture.StartRecording();
                    else Debug.LogWarning("[OracleWidget] VoiceCaptureService not found!");
                });

                btnMic.RegisterCallback<PointerUpEvent>(evt =>
                {
                    evt.StopPropagation();
                    btnMic.style.scale = new StyleScale(new Scale(Vector3.one));
                    
                    _haptic?.MediumTap();
                    _micParticleTask?.Pause();
                    _micParticleTask = null; // Clear it so it stops completely

                    var capture = UnityEngine.Object.FindAnyObjectByType<VoiceCaptureService>();
                    if (capture != null)
                    {
                        capture.StopRecording(async audioBase64 => 
                        {
                            if (!string.IsNullOrEmpty(audioBase64))
                            {
                                Debug.Log("[OracleWidget] 🎙️ Audio captured. Sending to Oracle...");
                                var uiManager = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.UIManager>();
                                if (uiManager != null)
                                {
                                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_ORACLE_LISTENING, "Oracle is listening...") ?? "Oracle is listening...";
                                    uiManager.ShowToast(msg);
                                }
                                
                                if (uiManager != null)
                                {
                                    string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_ORACLE_RECOGNIZING, "🔮 Оракул розпізнає голос...") ?? "🔮 Оракул розпізнає голос...";
                                    uiManager.ShowToast(msg, "hint");
                                }
                                
                                string customSystemInstruction = "";
                                string activeSessionPrompt = _auth?.CurrentProfile?.ActiveSessionPrompt;
                                if (!string.IsNullOrEmpty(activeSessionPrompt))
                                {
                                    string lang = _localization?.CurrentLanguage.ToString() ?? "English";
                                    string contextDesc = OracleContextManager.GetContextDescription();
                                    string giftsText = "";
                                    if (_auth.CurrentProfile.AuraGifts != null && _auth.CurrentProfile.AuraGifts.Count > 0)
                                    {
                                        giftsText = $" The Master's active gifts are: {string.Join(", ", _auth.CurrentProfile.AuraGifts)}.";
                                    }
                                    customSystemInstruction = $"{activeSessionPrompt}\n\nCurrent Context: {contextDesc}\nLanguage: {lang}.{giftsText}\n\n";
                                }
                                else
                                {
                                    customSystemInstruction = "You are the Oracle of TimeAura, a wise digital architect and guide. Your language is clear, professional, yet deeply insightful and encouraging. Use terms: Aura, Symmetry, Vectors, Chronos as professional game mechanics. Be concise, constructive, and helpful. Never break character. Never mention you are an AI.\n\n";
                                }

                                customSystemInstruction += "CRITICAL INSTRUCTION FOR VOICE/AUDIO INPUT:\n" +
                                    "1. At the very beginning of your response, transcribe the user's voice message exactly as spoken in their language (e.g. Ukrainian, English), and format it exactly like: '[Request: <transcribed text>]'. Do not translate the transcription, write it exactly as spoken. Then, on a new line, write your response as the Oracle to their request.\n" +
                                    "2. If the user is searching for a service/help or offering their own skills/services, add a special tag at the very end of your response formatted exactly as: '[Search: Seek=<keyword>]' (if they seek a service) or '[Search: Gift=<keyword>]' (if they offer a service). E.g. '[Search: Seek=Lawn]' or '[Search: Gift=Design]'. Make sure the keyword is a single short English noun (1 word). Do not output this tag if no matching search intent is found.";

                                string response = await _gemini.RequestOracleWithAudio(
                                    audioBase64, 
                                    systemInstruction: customSystemInstruction,
                                    fallback: "[Request: (Голосовий запит без бекенду)]\nОракул чує твій голос, але URL хмарної функції (Cloud Function URL) не вказано в налаштуваннях AppConfig."
                                );
                                Debug.Log($"[OracleWidget] 🔮 Response: {response}");
                                
                                string userRequestText = "🎙️ [Голосовий запит]";
                                string oracleResponseText = response;

                                if (response.StartsWith("[Request:"))
                                {
                                    int closingBracket = response.IndexOf(']');
                                    if (closingBracket > 9)
                                    {
                                        userRequestText = "🎙️ " + response.Substring(9, closingBracket - 9).Trim();
                                        oracleResponseText = response.Substring(closingBracket + 1).Trim();
                                    }
                                }

                                string searchTag = null;
                                bool isSeek = true;
                                int searchIndex = oracleResponseText.IndexOf("[Search:");
                                if (searchIndex >= 0)
                                {
                                    int closingSearch = oracleResponseText.IndexOf(']', searchIndex);
                                    if (closingSearch > searchIndex + 8)
                                    {
                                        string searchContent = oracleResponseText.Substring(searchIndex + 8, closingSearch - (searchIndex + 8)).Trim();
                                        if (searchContent.StartsWith("Seek="))
                                        {
                                            searchTag = searchContent.Substring(5).Trim();
                                            isSeek = true;
                                        }
                                        else if (searchContent.StartsWith("Gift="))
                                        {
                                            searchTag = searchContent.Substring(5).Trim();
                                            isSeek = false;
                                        }
                                        
                                        oracleResponseText = (oracleResponseText.Substring(0, searchIndex) + oracleResponseText.Substring(closingSearch + 1)).Trim();
                                    }
                                }

                                if (uiManager != null) uiManager.ShowToast(oracleResponseText, "Golden");
                                _audio?.PlaySFX("MessageSent2");

                                // Log voice exchange permanently to Sanctuary Chronicles
                                var sanctuary = SanctuaryUIController.Instance;
                                if (sanctuary != null)
                                {
                                    sanctuary.InjectDivineIntervention($"🌌 Master: {userRequestText}", "user_chat");
                                    sanctuary.InjectDivineIntervention($"👁️ Oracle: {oracleResponseText}", "oracle_chat");
                                }

                                if (!string.IsNullOrEmpty(searchTag))
                                {
                                    if (_auth != null && _auth.CurrentProfile != null)
                                    {
                                        if (isSeek)
                                        {
                                            if (!_auth.CurrentProfile.AuraSeeks.Contains(searchTag))
                                                _auth.CurrentProfile.AuraSeeks.Add(searchTag);
                                            _auth.CurrentProfile.PrimarySeek = searchTag;
                                        }
                                        else
                                        {
                                            if (!_auth.CurrentProfile.AuraGifts.Contains(searchTag))
                                                _auth.CurrentProfile.AuraGifts.Add(searchTag);
                                            _auth.CurrentProfile.PrimaryPillar = searchTag;
                                        }
                                    }

                                    await UniTask.Delay(2500);

                                    var nav = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Nexus.NexusNavigationManager>();
                                    var radarCtrl = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Nexus.RadarController>();
                                    
                                    if (nav != null)
                                    {
                                        nav.SwitchTo("feed");
                                    }
                                    if (radarCtrl != null)
                                    {
                                        radarCtrl.StartRadarSearch();
                                    }
                                }
                                else
                                {
                                    // Conversational response -> open mini-chat and show dialogue
                                    if (GeminiChatController.Instance != null)
                                    {
                                        _widgetState = OracleWidgetState.MiniChat;
                                        GeminiChatController.Instance.Open();
                                        GeminiChatController.Instance.AddUserMessage(userRequestText);
                                        GeminiChatController.Instance.AddOracleMessage(oracleResponseText);
                                        UpdateMenuButtonVisualState();
                                    }
                                }
                            }
                            else
                            {
                                 var uiManager = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.UIManager>();
                                 if (uiManager != null)
                                 {
                                     string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_MIC_BLOCKED, "⚠️ Мікрофон заблоковано іншою програмою.") ?? "⚠️ Мікрофон заблоковано іншою програмою.";
                                     uiManager.ShowToast(msg, "error");
                                 }
                            }
                        });
                    }
                });

                btnMic.RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    btnMic.style.scale = new StyleScale(new Scale(Vector3.one));
                    _micParticleTask?.Pause();
                    _micParticleTask = null;
                });
            }

            if (btnText != null) btnText.clicked += ToggleMiniChat;
            if (btnExpand != null) btnExpand.clicked += ExpandToFullChat;
            if (btnClose != null) btnClose.clicked += CloseWidget;

            UpdateMenuButtonVisualState();
            TestAlertsLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
            Debug.Log("[OracleWidget] 👁️ Oracle Controls fully bound and operational.");
        }

        public void OpenWidget()
        {
            _widgetState = OracleWidgetState.Open;
            if (_eyeRoot != null)
            {
                _eyeRoot.style.display = DisplayStyle.Flex;
                _eyeRoot.style.visibility = Visibility.Visible;
                _eyeRoot.style.opacity = 1f; // Force opacity
                _eyeRoot.style.scale = new StyleScale(new Scale(Vector3.one)); // Force scale
                _eyeRoot.RemoveFromClassList("oracle-eye--closed");
                _eyeRoot.BringToFront(); // Ensure it's not hidden behind panels!
                
                // Force position just in case layout engine corrupted it
                if (_root != null && _root.resolvedStyle.width > 100)
                {
                    _eyeRoot.style.left = _root.resolvedStyle.width - _eyeSize - 20;
                    _eyeRoot.style.top = _root.resolvedStyle.height - _eyeSize - (_root.resolvedStyle.height * 0.22f);
                }

                // Debug log to ensure size and position are correct
                Debug.Log($"[OracleWidget] 👁️ Eye Opened. TargetPos: {_eyeRoot.style.left.value.value},{_eyeRoot.style.top.value.value} | PrevRect: {_eyeRoot.resolvedStyle.width}x{_eyeRoot.resolvedStyle.height} @ {_eyeRoot.resolvedStyle.left},{_eyeRoot.resolvedStyle.top}");
                
                DumpLayoutDelayedAsync().Forget();
            }
            SetOracleAlertState(0);
            GeminiChatController.Instance?.ClearChat();
            _audio?.PlaySFX("SuccessRitual");
            _haptic?.MediumTap();
            Debug.Log("[OracleWidget] 👁️ Oracle Eye opened.");
        }

        private async UniTaskVoid DumpLayoutDelayedAsync()
        {
            await UniTask.Yield(); // wait for layout
            if (_eyeRoot != null)
            {
                Debug.Log($"[OracleWidget-DUMP] WorldBound: {_eyeRoot.worldBound} | Resolved: {_eyeRoot.resolvedStyle.width}x{_eyeRoot.resolvedStyle.height} @ {_eyeRoot.resolvedStyle.left},{_eyeRoot.resolvedStyle.top} | Display: {_eyeRoot.resolvedStyle.display} | Opacity: {_eyeRoot.resolvedStyle.opacity}");
            }
        }

        public void CloseWidget()
        {
            _widgetState = OracleWidgetState.Closed;
            if (_eyeRoot != null)
            {
                _eyeRoot.AddToClassList("oracle-eye--closed");
                _eyeRoot.style.opacity = 0f; // Force it to be invisible
                _eyeRoot.style.visibility = Visibility.Hidden; // Force hide
                _eyeRoot.style.display = DisplayStyle.None; // Remove from layout
            }
            UpdateMenuButtonVisualState();
            GeminiChatController.Instance?.Close();
            GeminiChatController.Instance?.ClearChat();
            _audio?.PlaySFX("CrystalClick");
            _haptic?.LightTap();
            Debug.Log("[OracleWidget] 👁️ Oracle Eye closed & collapsed to bottom menu.");
        }

        public void ToggleMiniChat()
        {
            if (_widgetState == OracleWidgetState.MiniChat)
            {
                _widgetState = OracleWidgetState.Open;
                GeminiChatController.Instance?.Close();
                _audio?.PlaySFX("CrystalClick");
            }
            else
            {
                _widgetState = OracleWidgetState.MiniChat;
                GeminiChatController.Instance?.Open();
                _audio?.PlaySFX("MessageSent2");
            }
            UpdateMenuButtonVisualState();
            _haptic?.LightTap();
        }

        public void ExpandToFullChat()
        {
            _widgetState = OracleWidgetState.Closed;
            if (_eyeRoot != null)
            {
                _eyeRoot.style.display = DisplayStyle.None;
                _eyeRoot.AddToClassList("oracle-eye--closed");
            }
            UpdateMenuButtonVisualState();
            GeminiChatController.Instance?.Close();
            GeminiChatController.Instance?.ClearChat();
            
            var nav = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Nexus.NexusNavigationManager>();
            if (nav != null)
            {
                nav.SwitchTo("sanctuary");
            }
            _audio?.PlaySFX("SuccessRitual");
            _haptic?.MediumTap();
        }

        public void SetOracleAlertState(int importanceLevel)
        {
            _oracleAlertLevel = Mathf.Clamp(importanceLevel, 0, 3);
            UpdateMenuButtonVisualState();
        }

        private void UpdateMenuButtonVisualState()
        {
            VisualElement collapsedBtn = null;
            if (_root != null)
            {
                collapsedBtn = _root.Q("BtnOracleEyeCollapsed");
            }

            if (collapsedBtn == null)
            {
                var allDocs = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var doc in allDocs)
                {
                    if (doc.rootVisualElement != null)
                    {
                        collapsedBtn = doc.rootVisualElement.Q("BtnOracleEyeCollapsed");
                        if (collapsedBtn != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (collapsedBtn == null)
            {
                return;
            }

            var lblNavOracle = collapsedBtn.Q<Label>("LblNavOracle");

            // Stop existing particle task if any
            _menuButtonParticleTask?.Pause();
            _menuButtonParticleTask = null;

            // Clear classes
            collapsedBtn.RemoveFromClassList("nav-item--active");
            collapsedBtn.RemoveFromClassList("aura-glow--gold");
            collapsedBtn.RemoveFromClassList("aura-glow--cyan");
            collapsedBtn.RemoveFromClassList("aura-glow--sapphire");
            collapsedBtn.RemoveFromClassList("aura-glow--pulse");

            if (lblNavOracle != null)
            {
                lblNavOracle.text = (_localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian) ? "👁️ ОРАКУЛ" : "👁️ ORACLE";
            }

            if (_widgetState == OracleWidgetState.Closed)
            {
                if (_oracleAlertLevel > 0)
                {
                    // Oracle needs to suggest/inform/warn the player -> Glow & Particles!
                    if (_oracleAlertLevel == 1)
                    {
                        collapsedBtn.AddToClassList("aura-glow--cyan");
                        // Slow, subtle particles
                        _menuButtonParticleTask = collapsedBtn.schedule.Execute(() => SpawnMenuParticle(collapsedBtn, 1)).Every(300);
                    }
                    else if (_oracleAlertLevel == 2)
                    {
                        collapsedBtn.AddToClassList("aura-glow--gold");
                        collapsedBtn.AddToClassList("aura-glow--pulse");
                        // Medium particles
                        _menuButtonParticleTask = collapsedBtn.schedule.Execute(() => SpawnMenuParticle(collapsedBtn, 2)).Every(150);
                    }
                    else if (_oracleAlertLevel == 3)
                    {
                        collapsedBtn.AddToClassList("aura-glow--gold");
                        collapsedBtn.AddToClassList("aura-glow--pulse");
                        // Fast, intense particle stream
                        _menuButtonParticleTask = collapsedBtn.schedule.Execute(() => SpawnMenuParticle(collapsedBtn, 3)).Every(60);
                    }
                }
            }
        }

        private void SpawnMenuParticle(VisualElement btn, int level)
        {
            if (btn == null || btn.parent == null) return;
            
            var particle = new VisualElement();
            particle.AddToClassList("magic-particle");
            particle.pickingMode = PickingMode.Ignore;
            
            particle.style.position = Position.Absolute;
            particle.style.left = Length.Percent(UnityEngine.Random.Range(10f, 90f));
            particle.style.top = Length.Percent(UnityEngine.Random.Range(10f, 90f));
            particle.style.opacity = 0f;
            
            btn.Add(particle);
            
            AnimateMenuParticle(particle, level).Forget();
        }

        private async UniTaskVoid AnimateMenuParticle(VisualElement particle, int level)
        {
            if (particle == null) return;
            
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float speedMultiplier = level switch
            {
                1 => 0.5f,
                2 => 1.0f,
                3 => 1.8f,
                _ => 1.0f
            };
            float distance = UnityEngine.Random.Range(30f, 80f) * speedMultiplier;
            float endY = Mathf.Sin(angle) * distance;
            float endX = Mathf.Cos(angle) * distance;
            
            float duration = UnityEngine.Random.Range(0.4f, 0.8f) / speedMultiplier;
            
            particle.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) });
            particle.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutCubic) });
            
            await UniTask.Delay(10);
            if (particle == null) return;

            particle.style.translate = new Translate(endX, endY, 0);
            particle.style.opacity = UnityEngine.Random.Range(0.6f, 0.9f);
            
            Color pColor = level switch
            {
                1 => new Color(0f, 1f, 1f), // Cyan
                2 => new Color(1f, 0.84f, 0f), // Gold
                3 => new Color(1f, 0.2f, 0.1f), // Crimson/Red warning
                _ => new Color(1f, 0.84f, 0f)
            };
            particle.style.backgroundColor = new StyleColor(pColor);
            particle.style.scale = new Scale(new Vector2(UnityEngine.Random.Range(1.0f, 2.0f), UnityEngine.Random.Range(1.0f, 2.0f)));

            await UniTask.Delay((int)(duration * 500));
            if (particle == null) return;
            
            particle.style.opacity = 0f;
            
            await UniTask.Delay((int)(duration * 500));
            if (particle != null && particle.parent != null)
            {
                particle.RemoveFromHierarchy();
            }
        }

        private async UniTaskVoid TestAlertsLoopAsync(CancellationToken ct)
        {
            await UniTask.Delay(5000, cancellationToken: ct);
            if (ct.IsCancellationRequested || _widgetState != OracleWidgetState.Closed) return;
            Debug.Log("[OracleWidget] 🔮 SIMULATION: Low Alert (Cyan + Slow Particles)");
            SetOracleAlertState(1);

            await UniTask.Delay(10000, cancellationToken: ct);
            if (ct.IsCancellationRequested || _widgetState != OracleWidgetState.Closed) return;
            Debug.Log("[OracleWidget] 🔮 SIMULATION: Medium Alert (Gold + Medium Particles)");
            SetOracleAlertState(2);

            await UniTask.Delay(10000, cancellationToken: ct);
            if (ct.IsCancellationRequested || _widgetState != OracleWidgetState.Closed) return;
            Debug.Log("[OracleWidget] 🔮 SIMULATION: High Alert (Gold + Crimson + Fast Particles)");
            SetOracleAlertState(3);
        }

        private IVisualElementScheduledItem _micParticleTask;
        private List<VisualElement> _micParticles = new List<VisualElement>();

        private void SpawnMicParticle(VisualElement btn)
        {
            if (btn == null || btn.parent == null) return;
            
            _micParticles.RemoveAll(p => p == null || p.parent == null);
            if (_micParticles.Count > 60) return;

            var particle = new VisualElement();
            particle.AddToClassList("magic-particle");
            particle.pickingMode = PickingMode.Ignore; // Ensure particles don't steal clicks!
            
            particle.style.position = Position.Absolute;
            particle.style.left = Length.Percent(50f);
            particle.style.top = Length.Percent(50f);
            particle.style.opacity = 0f;
            
            btn.Add(particle);
            _micParticles.Add(particle);
            
            AnimateMicParticle(particle).Forget();
        }

        private async UniTaskVoid AnimateMicParticle(VisualElement particle)
        {
            if (particle == null) return;
            
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(40f, 150f);
            float endY = Mathf.Sin(angle) * distance;
            float endX = Mathf.Cos(angle) * distance;
            float duration = UnityEngine.Random.Range(0.6f, 1.2f);
            
            particle.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) });
            particle.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) });
            
            await UniTask.Delay(10);
            if (particle == null) return;

            particle.style.translate = new Translate(endX, endY, 0);
            particle.style.opacity = UnityEngine.Random.Range(0.7f, 1f);
            particle.style.backgroundColor = new StyleColor(new Color(1f, 0.84f, 0f));
            particle.style.scale = new Scale(new Vector2(UnityEngine.Random.Range(1.0f, 2.5f), UnityEngine.Random.Range(1.0f, 2.5f)));

            await UniTask.Delay((int)(duration * 500));
            if (particle == null) return;
            
            particle.style.opacity = 0f;
            
            await UniTask.Delay((int)(duration * 500));
            if (particle != null && particle.parent != null)
            {
                particle.RemoveFromHierarchy();
                _micParticles.Remove(particle);
            }
        }

        private void Update()
        {
            if (_eyeRoot == null || _currentState == OracleState.Closed) return;

#if UNITY_EDITOR
            ApplyBaseTuning();
#endif
        }

        private Vector2 _lastPupilOffset;
        private float _lastEyeSize;
        private float _lastPupilScale;

        private void ApplyBaseTuning()
        {
            if (_eyeRoot == null) return;
            
            if (_eyeSize > 0 && Mathf.Abs(_lastEyeSize - _eyeSize) > 0.1f)
            {
                _eyeRoot.style.width = Mathf.Max(_eyeSize, 120f); // Wide enough for mic aura
                _eyeRoot.style.height = _eyeSize + 130f; // Tall enough to include mic
                
                if (_eyeContainer != null) {
                    _eyeContainer.style.width = _eyeSize;
                    _eyeContainer.style.height = _eyeSize;
                    _eyeContainer.style.position = Position.Absolute;
                    _eyeContainer.style.top = 0;
                }
                _lastEyeSize = _eyeSize;
            }

            if (_eyeContainer != null && _eyeSprite != null && _eyeContainer.style.backgroundImage.value.sprite != _eyeSprite)
            {
                _eyeContainer.style.backgroundImage = new StyleBackground(_eyeSprite);
            }

            if (_pupil != null)
            {
                if (Mathf.Abs(_lastPupilScale - _pupilScale) > 0.01f)
                {
                    _pupil.style.width = Length.Percent(_pupilScale * 100f);
                    _pupil.style.height = Length.Percent(_pupilScale * 100f);
                    _lastPupilScale = _pupilScale;
                }

                if (_pupilSprite != null && _pupil.style.backgroundImage.value.sprite != _pupilSprite)
                {
                    _pupil.style.backgroundImage = new StyleBackground(_pupilSprite);
                }

                if (_lastPupilOffset != _pupilVisualOffset)
                {
                    // Use percent based on the parent for proper scaling, or direct offset
                    _pupil.style.translate = new Translate(new Length(_pupilVisualOffset.x, LengthUnit.Percent), new Length(_pupilVisualOffset.y, LengthUnit.Percent), 0);
                    _lastPupilOffset = _pupilVisualOffset;
                }
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            // Defensive check: Do not interact or drag if the target is one of the action buttons
            var target = evt.target as VisualElement;
            while (target != null && target != _eyeRoot)
            {
                if (target.name == "BtnGoldenMic" || target.name == "BtnMic" || 
                    target.name == "BtnText" || target.name == "BtnExpand" || 
                    target.name == "BtnClose")
                {
                    return; // Ignore this click on the eye root!
                }
                target = target.parent;
            }

            _isDragging = true;
            _mouseDownPos = evt.position;
            _dragOffset = (Vector2)evt.position - new Vector2(_eyeRoot.resolvedStyle.left, _eyeRoot.resolvedStyle.top);
            _eyeRoot.CapturePointer(evt.pointerId);
            if (_eyeContainer != null) _eyeContainer.AddToClassList("oracle-eye--active");
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging) return;
            Vector2 newPos = (Vector2)evt.position - _dragOffset;
            
            // Clamp
            float screenW = _root.resolvedStyle.width;
            float screenH = _root.resolvedStyle.height;
            newPos.x = Mathf.Clamp(newPos.x, 0, screenW - _eyeSize);
            newPos.y = Mathf.Clamp(newPos.y, 0, screenH - _eyeSize);

            _eyeRoot.style.left = newPos.x;
            _eyeRoot.style.top = newPos.y;

            // Update chat position in real-time if it's open
            if (GeminiChatController.Instance != null)
            {
                GeminiChatController.Instance.UpdatePosition();
            }
        }

        private void OnPointerUp(IPointerEvent evt)
        {
            if (!_isDragging) return;
            
            float dragDistance = Vector2.Distance(_mouseDownPos, evt.position);
            _isDragging = false;
            _eyeRoot.ReleasePointer(evt.pointerId);
            if (_eyeContainer != null) _eyeContainer.RemoveFromClassList("oracle-eye--active");
            
            if (dragDistance < 10f) {
                InteractWithOracle();
            } else {
                ApplyMagneticEffect();
            }
        }

        private void InteractWithOracle()
        {
            PulseEyeEffect().Forget();
            ToggleMiniChat();
        }

        private async UniTaskVoid PulseEyeEffect()
        {
            if (_pupil == null) return;
            
            var originalColor = _pupil.style.unityBackgroundImageTintColor.value;
            _pupil.style.scale = new Scale(new Vector2(1.4f, 1.4f));
            _pupil.style.unityBackgroundImageTintColor = new StyleColor(Color.white); // Flash white
            
            await UniTask.Delay(150);
            
            if (_pupil != null)
            {
                _pupil.style.scale = new Scale(Vector2.one);
                _pupil.style.unityBackgroundImageTintColor = originalColor;
            }
        }

        private void ApplyMagneticEffect()
        {
            float screenWidth = _root.resolvedStyle.width;
            if (screenWidth <= 0) return;

            float currentX = _eyeRoot.resolvedStyle.left + (_eyeSize/2);
            float targetX = (currentX < screenWidth/2) ? 20 : screenWidth - _eyeSize - 20;
            _eyeRoot.style.left = targetX;
            _audio?.PlaySFX(_snapSfx, 0.3f);
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            float screenWidth = evt.newRect.width;
            float screenHeight = evt.newRect.height;

            if (screenWidth <= 100 || screenHeight <= 100) return;

            if (!_hasPositionedDefault)
            {
                float defaultLeft = screenWidth - _eyeSize - 20;
                float defaultTop = screenHeight - _eyeSize - (screenHeight * 0.22f);

                _eyeRoot.style.left = defaultLeft;
                _eyeRoot.style.top = defaultTop;
                _hasPositionedDefault = true;
                Debug.Log($"[OracleWidget] 👁️ Eye default position initialized to: left={defaultLeft}, top={defaultTop} (Screen: {screenWidth}x{screenHeight})");
            }
            else
            {
                float currentLeft = _eyeRoot.resolvedStyle.left;
                float currentTop = _eyeRoot.resolvedStyle.top;

                // When display is None, resolvedStyle returns 0 or NaN. Fallback to the saved style value.
                if (_eyeRoot.style.display == DisplayStyle.None || float.IsNaN(currentLeft) || float.IsNaN(currentTop))
                {
                    currentLeft = _eyeRoot.style.left.value.value;
                    currentTop = _eyeRoot.style.top.value.value;
                    
                    // If even the style value is invalid/0 from a fresh init, use defaults
                    if (currentLeft == 0f && currentTop == 0f)
                    {
                        currentLeft = screenWidth - _eyeSize - 20;
                        currentTop = screenHeight - _eyeSize - (screenHeight * 0.22f);
                    }
                }

                float clampedLeft = Mathf.Clamp(currentLeft, 0, screenWidth - _eyeSize);
                float clampedTop = Mathf.Clamp(currentTop, 0, screenHeight - _eyeSize);

                if (Mathf.Abs(currentLeft - clampedLeft) > 1f || Mathf.Abs(currentTop - clampedTop) > 1f)
                {
                    _eyeRoot.style.left = clampedLeft;
                    _eyeRoot.style.top = clampedTop;
                    // Debug.Log($"[OracleWidget] 👁️ Clamped eye position to bounds: left={clampedLeft}, top={clampedTop}");
                }
            }
        }

        private IEnumerator SetDefaultPosition()
        {
            // Fully handled dynamically by OnRootGeometryChanged
            yield break;
        }

        private void SubscribeToService()
        {
            if (_oracleService != null) {
                _oracleService.OnStatusUpdate -= HandleStatusUpdate;
                _oracleService.OnStatusUpdate += HandleStatusUpdate;
            }
        }

        private void HandleStatusUpdate(OracleStatusUpdate update)
        {
            SetState(update.State);
            if (!string.IsNullOrEmpty(update.Message) && update.Message != "...") {
                ShowHint(update.Message);
            }
        }

        public void SetState(OracleState state)
        {
            _currentState = state;
            if (_eyeRoot == null) return;

            _eyeRoot.RemoveFromClassList("oracle-eye--closed");
            _eyeRoot.RemoveFromClassList("oracle-eye--active");
            _eyeRoot.RemoveFromClassList("oracle-eye--processing");
            _eyeRoot.RemoveFromClassList("oracle-eye--alert");
            _eyeRoot.RemoveFromClassList("oracle-eye--privacy");

            Color stateColor = new Color(1f, 0.84f, 0f); // Default Gold
            float glowAlpha = 0.2f;

            switch (state) {
                case OracleState.Closed: 
                    _eyeRoot.AddToClassList("oracle-eye--closed"); 
                    stateColor = new Color(0.3f, 0.3f, 0.3f);
                    break;
                case OracleState.Active: 
                    _eyeRoot.AddToClassList("oracle-eye--active"); 
                    var equippedOracle = _promptFactory?.GetEquippedOracle();
                    if (equippedOracle != null)
                    {
                        stateColor = equippedOracle.ThemeColor;
                    }
                    else
                    {
                        stateColor = new Color(1f, 0.84f, 0f); // Default Gold
                    }
                    break;
                case OracleState.Processing: 
                    _eyeRoot.AddToClassList("oracle-eye--processing"); 
                    stateColor = new Color(0f, 1f, 1f); // Cyan
                    glowAlpha = 0.4f;
                    break;
                case OracleState.Alert: 
                    _eyeRoot.AddToClassList("oracle-eye--alert"); 
                    stateColor = new Color(1f, 0.2f, 0.2f); // Red
                    glowAlpha = 0.5f;
                    _haptic?.LightTap();
                    break;
                case OracleState.Privacy: 
                    _eyeRoot.AddToClassList("oracle-eye--privacy"); 
                    stateColor = new Color(0.7f, 0f, 1f); // Purple
                    glowAlpha = 0.4f;
                    break;
                case OracleState.DebtAlert:
                    _eyeRoot.AddToClassList("oracle-eye--closed"); // Make it look dim/exhausted
                    stateColor = new Color(0.5f, 0.5f, 0.5f); // Gray/Dimmed
                    glowAlpha = 0.1f;
                    break;
            }

            SetPupilColor(stateColor, glowAlpha);
        }

        public void ShowHint(string text)
        {
            if (_hintBubble == null || _hintLabel == null) return;
            _typeCts?.Cancel();
            _typeCts?.Dispose();
            _typeCts = CancellationTokenSource.CreateLinkedTokenSource(_widgetCts.Token);
            TypeTextAsync(text, _typeCts.Token).Forget();
        }

        private async UniTaskVoid TypeTextAsync(string text, CancellationToken ct)
        {
            try
            {
                _hintLabel.text = "";
                _hintBubble.AddToClassList("hint-bubble--visible");
                _audio?.PlaySFX(_whisperSfx, 0.4f);
                
                foreach (char c in text) {
                    ct.ThrowIfCancellationRequested();
                    _hintLabel.text += c;
                    await UniTask.Delay(TimeSpan.FromSeconds(0.03f), cancellationToken: ct);
                }
                await UniTask.Delay(TimeSpan.FromSeconds(4f), cancellationToken: ct);
                _hintBubble.RemoveFromClassList("hint-bubble--visible");
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid GlowPulseLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested) {
                    if (_eyeContainer != null) {
                        _eyeContainer.ToggleInClassList("oracle-eye-container--glow");
                    }
                    await UniTask.Delay(TimeSpan.FromSeconds(1.5f), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid IdleSwayLoopAsync(CancellationToken ct)
        {
            try
            {
                float timer = 0;
                while (!ct.IsCancellationRequested) {
                    if (!_isDragging && _eyeRoot != null) {
                        timer += Time.deltaTime * _swaySpeed;
                        float offset = Mathf.Sin(timer) * _swayAmount;
                        _eyeRoot.style.translate = new Translate(0, offset, 0);
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid AutoBlinkLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested) {
                    float delay = UnityEngine.Random.Range(3f, 10f);
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);
                    if (_currentState != OracleState.Closed && !_isDragging && _eyeRoot != null) {
                        _eyeRoot.AddToClassList("oracle-eye--closed");
                        await UniTask.Delay(TimeSpan.FromSeconds(0.15f), cancellationToken: ct);
                        if (_eyeRoot != null) _eyeRoot.RemoveFromClassList("oracle-eye--closed");
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private async UniTask<string> GetPersonalizedHintAsync()
        {
            // TEMPORARILY DISABLED API CALLS FOR WHISPERS TO SAVE COSTS (Returning Local Phrases Only)
            await UniTask.Yield();
            return _mysticalPhrases[UnityEngine.Random.Range(0, _mysticalPhrases.Length)];
            
            /* ORIGINAL AI LOGIC HIDDEN UNTIL PROJECT IS PROFITABLE
            var equippedOracle = _promptFactory?.GetEquippedOracle();
            string activeSessionPrompt = _auth?.CurrentProfile?.ActiveSessionPrompt;

            if (equippedOracle != null && !string.IsNullOrEmpty(activeSessionPrompt) && _gemini != null)
            {
                try
                {
                    string contextDesc = OracleContextManager.GetContextDescription();
                    string giftsText = (_auth.CurrentProfile.AuraGifts != null && _auth.CurrentProfile.AuraGifts.Count > 0)
                        ? string.Join(", ", _auth.CurrentProfile.AuraGifts) 
                        : "none";

                    string metaPrompt = $"You are acting as the equipped Oracle '{equippedOracle.DisplayName}' in symbiosis with the Master. " +
                                        $"The current scene or action the Master is taking is: \"{contextDesc}\". " +
                                        $"The Master's active skill tags are: [{giftsText}]. " +
                                        $"Please whisper one short, extremely concise, personalized hint, tip, or wise prediction (max 15 words) in Ukrainian. " +
                                        $"Tailor it to your character, their tags, and their current activity. Do not explain, just return the whisper.";

                    string hint = await _gemini.RequestOracleWithSystem(activeSessionPrompt, metaPrompt, "");
                    if (!string.IsNullOrWhiteSpace(hint))
                    {
                        return hint.Replace("\"", "").Trim();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[OracleWidget] Failed to generate personalized hint: {ex.Message}");
                }
            }
            */

            // Fallback to local mystical phrases
            // return _mysticalPhrases[UnityEngine.Random.Range(0, _mysticalPhrases.Length)];
        }

        private async UniTaskVoid RandomProphecyLoopAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // INCREASED DELAY to save Gemini API costs! (from 25-50s to 5-10 minutes)
                    float delay = UnityEngine.Random.Range(300f, 600f);
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct);

                    // Only show if not busy and chat is closed
                    if (_currentState == OracleState.Active && !_isDragging && 
                        (_chatOverlay == null || _chatOverlay.ClassListContains("chat-overlay--hidden")))
                    {
                        string hintText = await GetPersonalizedHintAsync();
                        ct.ThrowIfCancellationRequested();

                        if (!string.IsNullOrEmpty(hintText))
                        {
                            ShowHint(hintText);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        private void ToggleChat(bool show)
        {
            if (GeminiChatController.Instance != null)
            {
                if (show) GeminiChatController.Instance.Open();
                else GeminiChatController.Instance.Close();
                return;
            }
        }

        private void SendChatMessage()
        {
            if (_chatInput == null || string.IsNullOrWhiteSpace(_chatInput.value)) return;
            
            string userMsg = _chatInput.value;
            _chatInput.value = "";
            
            AddMessage(userMsg, true);
            ProcessOracleResponse(userMsg).Forget();
        }

        private async UniTaskVoid ProcessOracleResponse(string message)
        {
            if (_oracleService == null) return;
            
            var thinkingMsg = AddMessage("...", false);
            
            try 
            {
                string response = await _oracleService.GetPhilosophicalGuidanceAsync(message, "Nexus Scene");
                thinkingMsg.text = response;
            }
            catch (Exception ex)
            {
                var tone = _auth?.CurrentProfile?.OracleTone ?? OracleTone.Business;
                thinkingMsg.text = _localization?.GetPersonaString(AuraTerms.ORACLE_ERR_TURBULENT, tone, "The cosmic flows are turbulent. Try again later.") ?? "The cosmic flows are turbulent. Try again later.";
                Debug.LogError($"[Oracle] Chat Error: {ex.Message}");
            }
            
            _chatScrollView.ScrollTo(thinkingMsg);
        }

        private Label AddMessage(string text, bool isUser)
        {
            var label = new Label(text);
            label.AddToClassList("chat-message");
            label.AddToClassList(isUser ? "chat-message--user" : "chat-message--oracle");
            _chatScrollView?.Add(label);
            _chatScrollView?.ScrollTo(label);
            return label;
        }

        public void SetPupilColor(Color color, float alpha = 0f)
        {
            if (_pupil != null)
            {
                // Use tint instead of background color to avoid square artifacts
                _pupil.style.unityBackgroundImageTintColor = color;
                
                // Optional: Use background color as a glow underlayer (now circular due to USS fix)
                if (alpha > 0)
                {
                    Color glow = color;
                    glow.a = alpha;
                    _pupil.style.backgroundColor = glow;
                }
                else
                {
                    _pupil.style.backgroundColor = Color.clear;
                }
            }
        }

        private void OnDisable()
        {
            if (_oracleService != null) _oracleService.OnStatusUpdate -= HandleStatusUpdate;
            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            }
            _widgetCts?.Cancel();
            _widgetCts?.Dispose();
            _widgetCts = null;
            _typeCts?.Cancel();
            _typeCts?.Dispose();
            _typeCts = null;
        }
    }
}
