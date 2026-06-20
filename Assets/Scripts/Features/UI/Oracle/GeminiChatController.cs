using System;
using System.Collections.Generic;
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
    /// <summary>
    /// GeminiChatController - Manages the slide-out chat interface for the Oracle.
    /// Follows the Singleton pattern for easy access from the Oracle Widget.
    /// </summary>
    public class GeminiChatController : MonoBehaviour
    {
        private static GeminiChatController _instance;
        public static GeminiChatController Instance => _instance;

        [Header("Components")]
        [SerializeField] private UIDocument _uiDocument;

        [Inject] private IAuraOracleService _oracleService;
        [Inject] private LocalizationManager _localization;
        [Inject] private TimeAura.Features.Auth.AuthManager _auth;
        [Inject] private IOracleService _gemini;

        private VisualElement _root;
        private VisualElement _overlay;
        private ScrollView _chatScrollView;
        private TextField _chatInput;
        private Button _btnSend;
        private Button _btnClose;
        private Button _btnFullChat;
        private Button _btnClearChat;

        private VisualElement _eyeRoot;
        private float _eyeSize = 130f;

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
            // Initialization will be handled by OracleWidgetController 
            // to ensure correct eyeRoot and eyeSize references.
        }

        public void Initialize(VisualElement root, VisualElement eyeRoot, float eyeSize)
        {
            _root = root;
            _eyeRoot = eyeRoot;
            _eyeSize = eyeSize;
            _overlay = root.Q<VisualElement>("GeminiChatOverlay");
            _chatScrollView = root.Q<ScrollView>("ChatScrollView");
            _chatInput = root.Q<TextField>("InputChat");
            _btnSend = root.Q<Button>("BtnSendChat");
            _btnClose = root.Q<Button>("BtnCloseChatOverlay") ?? root.Q<Button>("BtnCloseChat");
            _btnFullChat = root.Q<Button>("BtnFullChat");
            _btnClearChat = root.Q<Button>("BtnClearChat");

            if (_btnSend != null) _btnSend.clicked += SendMessage;
            if (_btnClose != null)
            {
                _btnClose.clicked += () =>
                {
                    if (OracleWidgetController.Instance != null && OracleWidgetController.Instance.WidgetState == OracleWidgetController.OracleWidgetState.MiniChat)
                    {
                        OracleWidgetController.Instance.ToggleMiniChat();
                    }
                    else
                    {
                        Close();
                    }
                };
            }
            
            if (_btnFullChat != null)
            {
                _btnFullChat.clicked += () =>
                {
                    if (OracleWidgetController.Instance != null)
                    {
                        OracleWidgetController.Instance.ExpandToFullChat();
                    }
                    else
                    {
                        var nav = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Nexus.NexusNavigationManager>();
                        if (nav != null)
                        {
                            nav.SwitchTo("sanctuary");
                        }
                        Close();
                    }
                };
            }

            if (_btnClearChat != null) _btnClearChat.clicked += ClearChat;
            
            if (_chatInput != null)
            {
                _chatInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return) SendMessage();
                });
            }

            UpdateLocalization();
        }

        public void Open()
        {
            if (_overlay == null) return;
            UpdatePosition();
            _overlay.RemoveFromClassList("chat-overlay--hidden");
            _chatInput?.Focus();
            OracleContextManager.SetContext(UIContext.Nexus); // Default chat context
        }

        public void UpdatePosition()
        {
            if (_overlay == null || _eyeRoot == null || _root == null) return;

            // Use immediate style values for drag-sync
            float screenW = _root.resolvedStyle.width;
            float screenH = _root.resolvedStyle.height;
            
            float eyeX = _eyeRoot.resolvedStyle.left > 0 ? _eyeRoot.resolvedStyle.left : _eyeRoot.style.left.value.value;
            float eyeY = _eyeRoot.resolvedStyle.top > 0 ? _eyeRoot.resolvedStyle.top : _eyeRoot.style.top.value.value;

            // Horizontal logic: Flip to the side with more space
            if (eyeX > screenW * 0.5f)
            {
                _overlay.style.left = StyleKeyword.Null;
                _overlay.style.right = _eyeSize + 20;
            }
            else
            {
                _overlay.style.right = StyleKeyword.Null;
                _overlay.style.left = _eyeSize + 20;
            }

            // Vertical logic: If eye is too high, show chat BELOW it
            if (eyeY < 460) 
            {
                _overlay.style.bottom = StyleKeyword.Null;
                _overlay.style.top = 20;
            }
            else
            {
                _overlay.style.top = StyleKeyword.Null;
                _overlay.style.bottom = 140;
            }
        }

        public void Close()
        {
            if (_overlay == null) return;
            _overlay.AddToClassList("chat-overlay--hidden");
        }

        public void ClearChat()
        {
            if (_chatScrollView != null)
            {
                _chatScrollView.Clear();
                var tone = _auth?.CurrentProfile?.OracleTone ?? OracleTone.Business;
                string greeting = _localization != null 
                    ? _localization.GetPersonaString("oracle.wisdom_greeting", tone, "Я Оракул твого серця. Запитай мене про будь-що...") 
                    : "Я Оракул твого серця. Запитай мене про будь-що...";
                AddMessage(greeting, false);
            }
        }

        private void SendMessage()
        {
            if (_chatInput == null || string.IsNullOrWhiteSpace(_chatInput.value)) return;

            string userMsg = _chatInput.value;
            _chatInput.value = "";

            AddMessage(userMsg, true);
            SanctuaryUIController.Instance?.InjectDivineIntervention($"🌌 Master: {userMsg}", "notification");
            ProcessOracleResponse(userMsg).Forget();
        }

        private async UniTaskVoid ProcessOracleResponse(string message)
        {
            if (_oracleService == null) return;

            var thinkingLabel = AddMessage("...", false);
            string responseText = "";
            
            try
            {
                string activeSessionPrompt = _auth?.CurrentProfile?.ActiveSessionPrompt;
                if (!string.IsNullOrEmpty(activeSessionPrompt) && _gemini != null)
                {
                    Debug.Log("[GeminiChat] Equipped Oracle ActiveSessionPrompt detected. Using custom chat prompt.");
                    string lang = _localization?.CurrentLanguage.ToString() ?? "English";
                    string contextDesc = OracleContextManager.GetContextDescription();
                    
                    string giftsText = "";
                    if (_auth.CurrentProfile.AuraGifts != null && _auth.CurrentProfile.AuraGifts.Count > 0)
                    {
                        giftsText = $" The Master's active gifts are: {string.Join(", ", _auth.CurrentProfile.AuraGifts)}.";
                    }

                    string systemInstruction = $"{activeSessionPrompt}\n\nCurrent Context: {contextDesc}\nLanguage: {lang}.{giftsText}";
                    
                    responseText = await _gemini.RequestOracleWithSystem(systemInstruction, message, "Connection faltered.");
                    thinkingLabel.text = responseText;
                }
                else
                {
                    responseText = await _oracleService.GetPhilosophicalGuidanceAsync(message, OracleContextManager.CurrentContext.ToString());
                    thinkingLabel.text = responseText;
                }
                SanctuaryUIController.Instance?.InjectDivineIntervention($"👁️ Oracle: {responseText}", "notification");
            }
            catch (Exception ex)
            {
                var tone = _auth?.CurrentProfile?.OracleTone ?? OracleTone.Business;
                string errText = _localization?.GetPersonaString(AuraTerms.ORACLE_ERR_TURBULENT, tone, "The cosmic flows are turbulent. Try again later.") ?? "The cosmic flows are turbulent. Try again later.";
                thinkingLabel.text = errText;
                SanctuaryUIController.Instance?.InjectDivineIntervention($"👁️ Oracle: {errText}", "notification");
                Debug.LogError($"[GeminiChat] Error: {ex.Message}");
            }

            _chatScrollView?.ScrollTo(thinkingLabel);
        }

        public void ShowOracleMessage(string text, bool replaceLast = false)
        {
            if (_overlay == null) return;
            Open();
            
            if (replaceLast && _chatScrollView.childCount > 0)
            {
                var last = _chatScrollView[_chatScrollView.childCount - 1] as Label;
                if (last != null && last.ClassListContains("chat-message--oracle"))
                {
                    last.text = text;
                    _chatScrollView.ScrollTo(last);
                    return;
                }
            }
            
            AddMessage(text, false);
        }

        public Label AddUserMessage(string text)
        {
            if (_overlay == null) return null;
            Open();
            return AddMessage(text, true);
        }

        public Label AddOracleMessage(string text)
        {
            if (_overlay == null) return null;
            Open();
            return AddMessage(text, false);
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

        public void UpdateLocalization()
        {
            if (_localization == null || _root == null) return;
            
            var tone = _auth?.CurrentProfile?.OracleTone ?? OracleTone.Business;
            var title = _root.Q<Label>(null, "chat-title");
            if (title != null) title.text = _localization.GetPersonaString(AuraTerms.ORACLE_WISDOM, tone, "ORACLE WISDOM").ToUpper();
        }
    }
}
