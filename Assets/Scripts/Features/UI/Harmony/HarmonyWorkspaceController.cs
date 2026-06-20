using System;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Harmony;
using TimeAura.Features.Economy;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace TimeAura.Features.UI.Harmony
{
    public class HarmonyWorkspaceController : MonoBehaviour
    {
        [Inject] private HarmonyManager _harmonyManager;
        [Inject] private HorasEconomyService _economyService;
        [Inject] private IAuraOracleService _oracleService;
        [Inject] private AuthManager _authManager;
        [Inject] private IDataService _dataService;
        [Inject] private LocalizationManager _localization;
        [Inject] private TimeAura.Features.UI.Oracle.OracleWhisperManager _whisperManager;
        [Inject] private AudioService _audioService;
        [Inject] private IOracleService _gemini;
        
        [Header("UI Configuration")]
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private VisualTreeAsset _aiResultCardTemplate;
        
        private VisualElement _root;
        private VisualElement _workspaceContainer;
        private Label _lblHeader;
        
        // New Ritual Elements
        private VisualElement _ritualNexusContainer;
        private Label _lblRitualStatus;
        private ScrollView _ritualLogScroll;
        
        private TextField _txtOffering;
        private Button _btnSubmitOffering;
        private Button _btnSubmitResult;
        private Button _btnAcceptResult;
        
        private HarmonySession _currentSession;
        private UserProfile _partnerProfile;
        private bool _isProcessing;
        private TimeAura.Features.UI.Nexus.NexusNavigationManager _navManager;

        // Arbitration
        private Button _btnRequestArbitration;

        private void Start()
        {
            if (_uiDocument == null) _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null) return;
            
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            // Bind Elements
            _workspaceContainer = _root.Q<VisualElement>("HarmonyWorkspaceContainer");
            _lblHeader = _root.Q<Label>("LblWorkspaceHeader");
            
            _ritualNexusContainer = _root.Q<VisualElement>("RitualNexusContainer");
            _lblRitualStatus = _root.Q<Label>("LblRitualStatus");
            _ritualLogScroll = _root.Q<ScrollView>("RitualLogScroll");
            
            _txtOffering = _root.Q<TextField>("TxtOffering");
            _btnSubmitOffering = _root.Q<Button>("BtnSubmitOffering");
            _btnSubmitResult = _root.Q<Button>("BtnSubmitResult");
            _btnAcceptResult = _root.Q<Button>("BtnAcceptResult");
            _btnRequestArbitration = _root.Q<Button>("BtnRequestArbitration");

            // Dynamically add arbitration button if not in UXML yet
            if (_btnRequestArbitration == null && _workspaceContainer != null)
            {
                _btnRequestArbitration = new Button { text = "⚖️ АРБІТРАЖ" };
                _btnRequestArbitration.AddToClassList("ritual-btn");
                _btnRequestArbitration.AddToClassList("ritual-btn--danger");
                _btnRequestArbitration.AddToClassList("hidden");
                _btnRequestArbitration.tooltip = "Відкрити суперечку. Куратор Нексусу розгляне вашу ситуацію.";
                _workspaceContainer.Add(_btnRequestArbitration);
                Debug.Log("[HarmonyWorkspace] ⚖️ Arbitration button created dynamically.");
            }
            
            if (_btnSubmitOffering != null) _btnSubmitOffering.clicked += OnSubmitOffering;
            if (_btnAcceptResult != null) _btnAcceptResult.clicked += OnAcceptResult;
            if (_btnRequestArbitration != null) _btnRequestArbitration.clicked += OnRequestArbitration;
            
            // Subscribe to Harmony Events
            if (_harmonyManager != null)
            {
                _harmonyManager.OnSessionStarted += HandleSessionStarted;
                _harmonyManager.OnSessionCompleted += HandleSessionCompleted;
                _harmonyManager.OnSessionDissolved += HandleSessionDissolved;
            }
            
            CloseWorkspace();
        }

        private void OnDestroy()
        {
            if (_harmonyManager != null)
            {
                _harmonyManager.OnSessionStarted -= HandleSessionStarted;
                _harmonyManager.OnSessionCompleted -= HandleSessionCompleted;
                _harmonyManager.OnSessionDissolved -= HandleSessionDissolved;
            }
        }

        private async void HandleSessionStarted(HarmonySession session)
        {
            _currentSession = session;
            if (_currentSession.status != HarmonyStatus.ActiveChannel) return;
            
            string currentUserId = _authManager.CurrentProfile.UserId;
            string partnerId = session.initiatorUserId == currentUserId ? session.recipientUserId : session.initiatorUserId;
            
            if (partnerId.StartsWith("AI_MASTER_"))
            {
                bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;
                _partnerProfile = new UserProfile(partnerId, "", isUk ? "ШІ-Майстер" : "AI Master", 0, 0);
            }
            else
            {
                _partnerProfile = await _dataService.GetUserProfileAsync(partnerId, default);
            }
            
            OpenWorkspace();
        }

        private void HandleSessionCompleted(HarmonySession session)
        {
            _currentSession = null;
            _partnerProfile = null;
            CloseWorkspace();
        }
        
        private void HandleSessionDissolved(HarmonySession session)
        {
            _currentSession = null;
            _partnerProfile = null;
            CloseWorkspace();
        }

        private void OpenWorkspace()
        {
            if (_workspaceContainer == null) return;
            _workspaceContainer.RemoveFromClassList("hidden");
            
            if (_navManager == null) _navManager = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Nexus.NexusNavigationManager>(FindObjectsInactive.Include);
            if (_navManager != null) _navManager.ToggleMenu(false); // Hide bottom nav

            if (_lblHeader != null)
            {
                string title = _localization != null ? _localization.Get("harmony.chamber", "THE SECRET CHAMBER") : "THE SECRET CHAMBER";
                _lblHeader.text = title;
            }
                
            if (_txtOffering != null)
            {
                _txtOffering.value = "";
                _txtOffering.SetEnabled(true);
            }
            
            if (_btnSubmitOffering != null) _btnSubmitOffering.SetEnabled(true);
            
            _ritualLogScroll?.Clear();
            AddLogEntry("[System] The Chamber is sealed. Time flows slowly here.", "system-entry");

            _isProcessing = false;
            UpdateWorkspaceState();
        }

        private void CloseWorkspace()
        {
            if (_workspaceContainer != null)
                _workspaceContainer.AddToClassList("hidden");
                
            if (_navManager != null) _navManager.ToggleMenu(true); // Show bottom nav
        }

        private async void OnSubmitOffering()
        {
            if (_isProcessing || _currentSession == null || _txtOffering == null) return;
            
            string offeringText = _txtOffering.value;
            if (string.IsNullOrWhiteSpace(offeringText)) return;
            
            _isProcessing = true;
            _txtOffering.value = "";
            _txtOffering.SetEnabled(false);
            if (_btnSubmitOffering != null) _btnSubmitOffering.SetEnabled(false);

            _currentSession.status = HarmonyStatus.OfferingSubmitted;
            _currentSession.contractTerms = offeringText; 
            
            AddLogEntry($"[You]: {offeringText}", "user-entry");
            PlaySendSound();
            
            UpdateWorkspaceState(); 

            if (_partnerProfile != null && _partnerProfile.UserId.StartsWith("AI_MASTER_"))
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2));
                AcceptTaskByAI(offeringText);
            }
        }

        private async void AcceptTaskByAI(string taskText)
        {
            _currentSession.status = HarmonyStatus.RitualConfirmed;
            AddLogEntry($"[Master {_partnerProfile.Nickname}]: Запит прийнято. Розпочинаю створення Симетрії.", "master-entry");
            UpdateWorkspaceState();
            
            await ProcessAITaskAsync(taskText);
        }

        private void UpdateWorkspaceState()
        {
            if (_currentSession == null) return;
            
            bool isInitiator = _authManager.CurrentProfile.UserId == _currentSession.initiatorUserId;

            _btnSubmitOffering?.AddToClassList("hidden");
            _btnSubmitResult?.AddToClassList("hidden");
            _btnAcceptResult?.AddToClassList("hidden");
            
            if (_lblRitualStatus != null) _lblRitualStatus.RemoveFromClassList("aura-glow--gold");

            switch (_currentSession.status)
            {
                case HarmonyStatus.ActiveChannel:
                    if (isInitiator)
                    {
                        _btnSubmitOffering?.RemoveFromClassList("hidden");
                        SetStatus("AWAITING YOUR OFFERING");
                    }
                    else
                    {
                        SetStatus("WAITING FOR INITIATOR");
                    }
                    break;
                case HarmonyStatus.OfferingSubmitted:
                    if (isInitiator)
                    {
                        SetStatus("MASTER IS REVIEWING...");
                    }
                    else
                    {
                        // Mock for Master
                        SetStatus("OFFERING RECEIVED");
                    }
                    break;
                case HarmonyStatus.RitualConfirmed:
                    SetStatus("RITUAL IN PROGRESS");
                    if (_lblRitualStatus != null) _lblRitualStatus.AddToClassList("aura-glow--gold");
                    break;
                case HarmonyStatus.ResultSubmitted:
                    SetStatus("MANIFESTATION COMPLETE");
                    if (isInitiator) _btnAcceptResult?.RemoveFromClassList("hidden");
                    // Arbitration available if initiator disputes the result
                    if (isInitiator) _btnRequestArbitration?.RemoveFromClassList("hidden");
                    break;
                case HarmonyStatus.Disputed:
                    SetStatus("⚖️ АРБІТРАЖ — РОЗГЛЯД КУРАТОРОМ");
                    if (_lblRitualStatus != null) _lblRitualStatus.AddToClassList("aura-glow--sapphire");
                    break;
            }
        }

        private void SetStatus(string text)
        {
            if (_lblRitualStatus != null) _lblRitualStatus.text = text;
        }

        private void AddLogEntry(string text, string entryClass)
        {
            if (_ritualLogScroll == null) return;
            
            var entry = new VisualElement();
            entry.AddToClassList("log-entry");
            entry.AddToClassList(entryClass);
            
            var label = new Label(text);
            label.AddToClassList("log-text");
            entry.Add(label);
            
            _ritualLogScroll.Add(entry);
            _ritualLogScroll.ScrollTo(entry);
        }

        private async UniTask ProcessAITaskAsync(string taskText)
        {
            try
            {
                string resultText;
                string activeSessionPrompt = _authManager?.CurrentProfile?.ActiveSessionPrompt;

                if (!string.IsNullOrEmpty(activeSessionPrompt))
                {
                    Debug.Log($"[Chamber] Equipped Oracle ActiveSessionPrompt detected. Using dynamic prompt synthesis.");
                    string userPrompt = $"The Master client has provided this contract requirement: \"{taskText}\". Please fulfill it perfectly according to your custom instructions.";
                    resultText = await _gemini.RequestOracleWithSystem(activeSessionPrompt, userPrompt, "Connection faltered.");
                }
                else
                {
                    string prompt = $"You are acting as an AI Master fulfilling a contract. The client has provided the following task or offering details:\n\n\"{taskText}\"\n\nPlease provide the complete, professional result for this task. Do not break character.";
                    resultText = await _oracleService.GetPhilosophicalGuidanceAsync(prompt, "Harmony Workspace");
                }
                
                _currentSession.status = HarmonyStatus.ResultSubmitted;
                ShowResult(resultText);

                _whisperManager?.ShowWhisper("👁️ Оракул завершив проявлення симетрії!", TimeAura.Features.UI.Oracle.WhisperColor.Gold);
                var sanctuary = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Oracle.SanctuaryUIController>(FindObjectsInactive.Include);
                sanctuary?.InjectDivineIntervention($"👁️ Збурення часу: Майстер {_partnerProfile?.Nickname ?? "AI"} завершив роботу.", "harmony", "ПЕРЕГЛЯНУТИ РЕЗУЛЬТАТ");
                
                if (_navManager != null)
                {
                    _navManager.SetTabResonance("sanctuary", true, "aura-glow--gold");
                    _navManager.SetTabResonance("harmony", true, "aura-glow--gold");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Workspace] Error processing AI task: {ex.Message}");
                _currentSession.status = HarmonyStatus.ResultSubmitted;
                ShowResult("The AI Master encountered turbulence in the cosmic flow and could not complete the task. Please try again.");
            }
        }

        private void ShowResult(string resultText)
        {
            UpdateWorkspaceState(); 
            AddLogEntry($"[Master {_partnerProfile.Nickname}]: {resultText}", "master-entry");
            PlaySendSound();
            _isProcessing = false;
        }

        private async void OnAcceptResult()
        {
            if (_currentSession == null || _partnerProfile == null) return;
            
            Debug.Log($"[Workspace] ✅ Result accepted by {_authManager.CurrentProfile.Nickname}. Finalizing contract...");
            if (_btnAcceptResult != null) _btnAcceptResult.SetEnabled(false);
            
            if (_partnerProfile.UserId.StartsWith("AI_MASTER_"))
            {
                await _economyService.ReleaseFundsToReceiverAsync(_authManager.CurrentProfile, _partnerProfile, TimeAura.Core.ContractRealm.Ether, _currentSession.lockedMinutes, 0L, _currentSession.sessionId);
                await _harmonyManager.CompleteHarmonyAsync(TimeAura.Features.Harmony.ResonanceLevel.Transcendent);
            }
            else
            {
                await _harmonyManager.CompleteHarmonyAsync(TimeAura.Features.Harmony.ResonanceLevel.Harmonious);
            }
        }

        private void PlaySendSound()
        {
            if (_audioService != null)
            {
                // Play a generic UI click if MessageSent doesn't exist, or try MessageSent2
                _audioService.PlaySFX("MessageSent2"); 
            }
        }

        /// <summary>
        /// Opens an arbitration request for the current session.
        /// Transitions session to Disputed and alerts the Oracle Sanctuary.
        /// </summary>
        private void OnRequestArbitration()
        {
            if (_currentSession == null) return;

            _currentSession.status = HarmonyStatus.Disputed;
            Debug.Log($"[Workspace] ⚖️ Arbitration requested for session {_currentSession.sessionId} by {_authManager?.CurrentProfile?.Nickname}");

            // Hide accept + arbitration button to prevent double-click
            _btnAcceptResult?.AddToClassList("hidden");
            _btnRequestArbitration?.AddToClassList("hidden");

            AddLogEntry("[⚖️ Арбітраж]: Запит отримано. Кошти заморожені до рішення куратора Нексусу.", "system-entry");

            // Notify through Oracle Sanctuary
            var sanctuary = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.UI.Oracle.SanctuaryUIController>(FindObjectsInactive.Include);
            sanctuary?.InjectDivineIntervention(
                "⚖️ Збурення Нексусу: відкрито суперечку. Справедливість буде відновлена.",
                "harmony",
                "ПЕРЕГЛЯНУТИ СУПЕРЕЧКУ"
            );

            // Glow the Oracle tab to draw attention
            _navManager?.SetTabResonance("sanctuary", true, "aura-glow--sapphire");

            // Whisper notification
            _whisperManager?.ShowWhisper("⚖️ Арбітраж розпочато. Очікуйте рішення куратора.", TimeAura.Features.UI.Oracle.WhisperColor.Gold);

            UpdateWorkspaceState();
        }
    }
}
