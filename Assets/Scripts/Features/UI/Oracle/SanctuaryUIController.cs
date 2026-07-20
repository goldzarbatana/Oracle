using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Localization;
using TimeAura.Core.Localization;
using TimeAura.Core.Data.SO;
using VContainer;

namespace TimeAura.Features.UI.Oracle
{
    public class SanctuaryUIController : MonoBehaviour
    {
        private static SanctuaryUIController _instance;
        public static SanctuaryUIController Instance => _instance;

        [Header("Components")]
        [SerializeField] private UIDocument _uiDocument;

        [Inject] private IAuraOracleService _oracleService;
        [Inject] private LocalizationManager _localization;
        [Inject] private TimeAura.Features.Auth.AuthManager _auth;
        [Inject] private TimeAura.Features.Economy.HorasEconomyService _economy;
        [Inject] private IOracleService _gemini;
        [Inject] private HapticService _haptic;
        [Inject] private AudioService _audio;

        public event Action OnExitRequested;

        private VisualElement _root;
        private VisualElement _sanctuaryRoot;
        
        // Tabs
        private Button _btnTabOracle, _btnTabChronicles, _btnTabResonances;
        private ScrollView _oracleScroll, _chroniclesScroll, _resonancesScroll;
        
        // Input Area & Quick Prompts
        private TextField _inputMeditation;
        private Button _btnMeditate;
        private Button _btnExit;
        private VisualElement _btnVoiceInput;
        private VisualElement _sacredCircle;
        private VisualElement _inputAreaContainer;
        private ScrollView _quickPromptsScroll;

        private IVisualElementScheduledItem _pulseTask;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                Initialize(_uiDocument.rootVisualElement);
            }
        }

        private void Update()
        {
            if (_sacredCircle != null && _sacredCircle.resolvedStyle.display != DisplayStyle.None)
            {
                float angle = Time.time * 5.0f;
                _sacredCircle.style.rotate = new Rotate(new Angle(angle));
            }
        }

        public void Initialize(VisualElement root)
        {
            _root = root;
            _sanctuaryRoot = root.Q<VisualElement>("SanctuaryRoot");
            _sacredCircle = root.Q<VisualElement>("SacredCircle");
            
            // Tabs setup
            _btnTabOracle = root.Q<Button>("BtnTabOracle");
            _btnTabChronicles = root.Q<Button>("BtnTabChronicles");
            _btnTabResonances = root.Q<Button>("BtnTabResonances");
            
            _oracleScroll = root.Q<ScrollView>("SanctuaryChat");
            _chroniclesScroll = root.Q<ScrollView>("ChroniclesScroll");
            _resonancesScroll = root.Q<ScrollView>("ResonancesScroll");
            
            if (_btnTabOracle != null) _btnTabOracle.clicked += () => SwitchTab("oracle");
            if (_btnTabChronicles != null) _btnTabChronicles.clicked += () => SwitchTab("chronicles");
            if (_btnTabResonances != null) _btnTabResonances.clicked += () => SwitchTab("resonances");

            // Input and Prompts
            _inputMeditation = root.Q<TextField>("InputMeditation");
            _btnMeditate = root.Q<Button>("BtnMeditate");
            _btnExit = root.Q<Button>("BtnExitSanctuary");
            _inputAreaContainer = root.Q<VisualElement>("InputAreaContainer");
            _quickPromptsScroll = root.Q<ScrollView>("QuickPromptsScroll");

            var btnPromptAura = root.Q<Button>("BtnPromptAura");
            var btnPromptDestiny = root.Q<Button>("BtnPromptDestiny");
            var btnPromptRitual = root.Q<Button>("BtnPromptRitual");

            if (btnPromptAura != null) btnPromptAura.clicked += () => SendQuickPrompt("Interpret my Aura");
            if (btnPromptDestiny != null) btnPromptDestiny.clicked += () => SendQuickPrompt("What awaits me today?");
            if (btnPromptRitual != null) btnPromptRitual.clicked += () => SendQuickPrompt("Explain my last Ritual");

            if (_btnMeditate != null) _btnMeditate.clicked += Meditate;
            if (_btnExit != null) _btnExit.clicked += Hide;

            _btnVoiceInput = root.Q<VisualElement>("BtnRecordVoiceSanctuary");
            if (_btnVoiceInput != null)
            {
                _btnVoiceInput.RegisterCallback<PointerDownEvent>(OnVoiceInputPointerDown);
                _btnVoiceInput.RegisterCallback<PointerUpEvent>(OnVoiceInputPointerUp);
            }

            // Initialize Economy display
            var lblHoras = root.Q<Label>("LblSanctuaryHoras");
            if (lblHoras != null)
            {
                var profile = _auth?.CurrentProfile;
                if (profile != null && _localization != null)
                {
                    lblHoras.text = _localization.FormatTimeBalance(profile.TimeBalanceMinutes, profile.OracleTone);
                }
                else
                {
                    lblHoras.text = (profile?.Horas ?? 0).ToString("F1");
                }
            }

            UpdateLocalization();
        }

        public void Show()
        {
            _inputMeditation?.Focus();
        }

        public void Hide()
        {
            OnExitRequested?.Invoke();
        }

        private void SwitchTab(string tabId)
        {
            _btnTabOracle?.RemoveFromClassList("sanctuary-tab-btn--active");
            _btnTabChronicles?.RemoveFromClassList("sanctuary-tab-btn--active");
            _btnTabResonances?.RemoveFromClassList("sanctuary-tab-btn--active");

            _oracleScroll?.AddToClassList("sanctuary-messages--hidden");
            _chroniclesScroll?.AddToClassList("sanctuary-messages--hidden");
            _resonancesScroll?.AddToClassList("sanctuary-messages--hidden");

            _inputAreaContainer?.AddToClassList("sanctuary-messages--hidden");
            _quickPromptsScroll?.AddToClassList("sanctuary-messages--hidden");

            switch (tabId)
            {
                case "oracle":
                    _btnTabOracle?.AddToClassList("sanctuary-tab-btn--active");
                    _oracleScroll?.RemoveFromClassList("sanctuary-messages--hidden");
                    _inputAreaContainer?.RemoveFromClassList("sanctuary-messages--hidden");
                    _quickPromptsScroll?.RemoveFromClassList("sanctuary-messages--hidden");
                    break;
                case "chronicles":
                    _btnTabChronicles?.AddToClassList("sanctuary-tab-btn--active");
                    _chroniclesScroll?.RemoveFromClassList("sanctuary-messages--hidden");
                    break;
                case "resonances":
                    _btnTabResonances?.AddToClassList("sanctuary-tab-btn--active");
                    _resonancesScroll?.RemoveFromClassList("sanctuary-messages--hidden");
                    break;
            }
        }

        private void SendQuickPrompt(string text)
        {
            AddMessage(text, true, _oracleScroll);
            ProcessMeditation(text).Forget();
        }

        private void Meditate()
        {
            if (_inputMeditation == null || string.IsNullOrWhiteSpace(_inputMeditation.value)) return;

            string userMsg = _inputMeditation.value;
            _inputMeditation.value = "";

            AddMessage(userMsg, true, _oracleScroll);
            ProcessMeditation(userMsg).Forget();
        }

        private async UniTaskVoid ProcessMeditation(string message)
        {
            if (_oracleService == null) return;

            var thinkingLabel = AddMessage("...", false, _oracleScroll);
            
            try
            {
                string response = await _oracleService.GetPhilosophicalGuidanceAsync(message, "Sanctuary");
                thinkingLabel.text = response;
            }
            catch (Exception ex)
            {
                var tone = _auth?.CurrentProfile?.OracleTone ?? OracleTone.Business;
                thinkingLabel.text = _localization?.GetPersonaString(AuraTerms.ERR_SILENCE_ABSOLUTE, tone, "The silence is absolute today. Try again later.") ?? "The silence is absolute today. Try again later.";
                Debug.LogError($"[Sanctuary] Error: {ex.Message}");
            }

            _oracleScroll?.ScrollTo(thinkingLabel);
        }

        public void InjectDivineIntervention(string message, string eventType = "notification", string actionText = null)
        {
            var container = new VisualElement();
            container.AddToClassList("chat-message");
            container.AddToClassList("chat-message--oracle");
            container.style.fontSize = 20;
            
            var label = new Label(message);
            label.style.whiteSpace = WhiteSpace.Normal;
            container.Add(label);

            if (eventType == "harmony") container.AddToClassList("aura-glow--gold");
            else if (eventType == "request") container.AddToClassList("aura-glow--cyan");

            // Add interactive action button if specified
            if (!string.IsNullOrEmpty(actionText))
            {
                var actionBtn = new Button { text = actionText };
                actionBtn.AddToClassList("interactive-action-btn");
                actionBtn.clicked += () => 
                {
                    // Mock action: navigate to harmony
                    var nav = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Nexus.NexusNavigationManager>(FindObjectsInactive.Include);
                    nav?.SwitchTo("harmony");
                    Hide(); // close sanctuary
                };
                container.Add(actionBtn);
            }

            // Always inject into Chronicles
            _chroniclesScroll?.Add(container);
            _chroniclesScroll?.ScrollTo(container);
            
            // Highlight the Chronicles tab to draw attention
            _btnTabChronicles?.AddToClassList("aura-glow--gold");
        }

        private Label AddMessage(string text, bool isUser, ScrollView targetScroll)
        {
            var label = new Label(text);
            label.AddToClassList("chat-message");
            label.AddToClassList(isUser ? "chat-message--user" : "chat-message--oracle");
            label.style.fontSize = 20; 
            targetScroll?.Add(label);
            targetScroll?.ScrollTo(label);
            return label;
        }

        private void OnVoiceInputPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
            if (_btnVoiceInput == null) return;

            _haptic?.HeavyTap();
            _audio?.PlaySFX("AuraResonance");

            _btnVoiceInput.AddToClassList("chat-voice-btn--recording");
            _pulseTask?.Pause();
            _pulseTask = _btnVoiceInput.schedule.Execute(() =>
            {
                float scaleVal = 1.15f + Mathf.PingPong(Time.time * 3f, 0.15f);
                _btnVoiceInput.style.scale = new StyleScale(new Scale(new Vector3(scaleVal, scaleVal, 1f)));
            }).Every(33);

            var capture = UnityEngine.Object.FindAnyObjectByType<VoiceCaptureService>();
            if (capture != null)
            {
                capture.StartRecording();
            }
            else
            {
                Debug.LogWarning("[Sanctuary] VoiceCaptureService not found!");
            }
        }

        private void OnVoiceInputPointerUp(PointerUpEvent evt)
        {
            evt.StopPropagation();
            if (_btnVoiceInput == null) return;

            _haptic?.MediumTap();
            
            _btnVoiceInput.RemoveFromClassList("chat-voice-btn--recording");
            _pulseTask?.Pause();
            _pulseTask = null;
            _btnVoiceInput.style.scale = new StyleScale(new Scale(Vector3.one));

            var capture = UnityEngine.Object.FindAnyObjectByType<VoiceCaptureService>();
            if (capture != null)
            {
                capture.StopRecording(async audioBase64 =>
                {
                    if (string.IsNullOrEmpty(audioBase64))
                    {
                        var uiManager = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.UIManager>();
                        string msg = _localization?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_VOICE_EMPTY, "⚠️ Voice recording empty or failed.") ?? "⚠️ Voice recording empty or failed.";
                        uiManager?.ShowToast(msg, "error");
                        return;
                    }

                    var tone = _auth?.CurrentProfile?.OracleTone ?? OracleTone.Business;
                    var uiManagerToast = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.UIManager>();
                    string toastMsg = _localization?.GetPersonaString("sanctuary_voice_recognizing", tone, "🔮 Oracle is parsing voice...") ?? "🔮 Oracle is parsing voice...";
                    uiManagerToast?.ShowToast(toastMsg, "hint");

                    string requestMsg = _localization?.GetPersonaString("sanctuary_voice_request", tone, "🎙️ [Voice Request]") ?? "🎙️ [Voice Request]";
                    var userMsgLabel = AddMessage(requestMsg, true, _oracleScroll);
                    var thinkingLabel = AddMessage("...", false, _oracleScroll);

                    try
                    {
                        string customSystemInstruction = "";
                        string activeSessionPrompt = _auth?.CurrentProfile?.ActiveSessionPrompt;
                        if (!string.IsNullOrEmpty(activeSessionPrompt))
                        {
                            string lang = _localization?.CurrentLanguage.ToString() ?? "English";
                            string contextDesc = "Sanctuary Panel Full Chat";
                            string giftsText = "";
                            if (_auth?.CurrentProfile?.AuraGifts != null && _auth.CurrentProfile.AuraGifts.Count > 0)
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

                        if (_gemini != null)
                        {
                            string response = await _gemini.RequestOracleWithAudio(
                                audioBase64, 
                                systemInstruction: customSystemInstruction,
                                fallback: _localization?.GetPersonaString("sanctuary_voice_fallback", tone, "[Request: (Voice request without backend)]\nThe Oracle hears your voice, but the Cloud Function URL is not set in AppConfig. Fill this field in the Unity Inspector to unlock full voice recognition.") ?? "[Request: (Voice Request)]\nBackend missing."
                            );

                            string userRequestText = requestMsg;
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

                            // Clean search tag if present in the response
                            int searchIndex = oracleResponseText.IndexOf("[Search:");
                            if (searchIndex >= 0)
                            {
                                oracleResponseText = oracleResponseText.Substring(0, searchIndex).Trim();
                            }

                            userMsgLabel.text = userRequestText;
                            thinkingLabel.text = oracleResponseText;

                            InjectDivineIntervention($"🌌 Master: {userRequestText}", "notification");
                            InjectDivineIntervention($"👁️ Oracle: {oracleResponseText}", "notification");
                        }
                    }
                    catch (Exception ex)
                    {
                        // tone is already declared in enclosing scope
                        thinkingLabel.text = _localization?.GetPersonaString(AuraTerms.ERR_SILENCE_ABSOLUTE, tone, "The silence is absolute today. Try again later.") ?? "The silence is absolute today. Try again later.";
                        Debug.LogError($"[Sanctuary Voice] Error: {ex.Message}");
                    }

                    _oracleScroll?.ScrollTo(thinkingLabel);
                });
            }
            else
            {
                Debug.LogWarning("[Sanctuary] VoiceCaptureService not found!");
            }
        }

        public void UpdateLocalization()
        {
            if (_localization == null || _root == null) return;
            
            var tone = _auth?.CurrentProfile?.OracleTone ?? OracleTone.Business;
            var title = _root.Q<Label>(null, "sanctuary-title");
            if (title != null) title.text = _localization.GetPersonaString(AuraTerms.ORACLE_SANCTUARY, tone, "ORACLE SANCTUARY").ToUpper();
            
            if (_btnExit != null) _btnExit.text = _localization.GetPersonaString(AuraTerms.BTN_EXIT_SILENCE, tone, "EXIT SILENCE").ToUpper();
            
            // Translate missing UI Elements
            var subtitle = _root.Q<Label>(null, "sanctuary-subtitle");
            if (subtitle != null) subtitle.text = _localization.GetPersonaString("sanctuary_subtitle", tone, "A place for deep contemplation beyond time.");

            if (_btnTabOracle != null) _btnTabOracle.text = _localization.GetPersonaString("sanctuary_tab_oracle", tone, "👁️ ORACLE");
            if (_btnTabChronicles != null) _btnTabChronicles.text = _localization.GetPersonaString("sanctuary_tab_chronicles", tone, "📜 CHRONICLES");
            if (_btnTabResonances != null) _btnTabResonances.text = _localization.GetPersonaString("sanctuary_tab_resonances", tone, "✨ RESONANCES");

            if (_inputMeditation != null) ((TextField)_inputMeditation).value = ""; // clear to show placeholder
            
            var btnPromptAura = _root.Q<Button>("BtnPromptAura");
            var btnPromptDestiny = _root.Q<Button>("BtnPromptDestiny");
            var btnPromptRitual = _root.Q<Button>("BtnPromptRitual");
            
            if (btnPromptAura != null) btnPromptAura.text = _localization.GetPersonaString("sanctuary_prompt_aura", tone, "Interpret my Aura");
            if (btnPromptDestiny != null) btnPromptDestiny.text = _localization.GetPersonaString("sanctuary_prompt_destiny", tone, "What awaits me?");
            if (btnPromptRitual != null) btnPromptRitual.text = _localization.GetPersonaString("sanctuary_prompt_ritual", tone, "Explain last Ritual");
        }
    }
}
