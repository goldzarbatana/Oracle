using System;
using TimeAura.Core;
using TimeAura.Core.Services;
using TimeAura.Features.UI.Nexus;
using TimeAura.Features.UI.Oracle;
using TimeAura.Features.Economy;
using TimeAura.Features.Data;
using UnityEngine;
using VContainer.Unity;

namespace TimeAura.Core.Infrastructure
{
    public class ActionExecutor : IStartable, IDisposable
    {
        private readonly OracleWhisperManager _whisperManager;
        private readonly AudioService _audioService;
        private readonly TimeAura.Features.Auth.AuthManager _authManager;
        private readonly IOracleService _oracle;
        private readonly HorasEconomyService _economyService;
        private readonly IDataService _dataService;
        
        private IDisposable _subscription;
        private IDisposable _disputeSubscription;

        public ActionExecutor(
            OracleWhisperManager whisperManager, 
            AudioService audioService, 
            TimeAura.Features.Auth.AuthManager authManager,
            IOracleService oracle,
            HorasEconomyService economyService,
            IDataService dataService)
        {
            _whisperManager = whisperManager;
            _audioService = audioService;
            _authManager = authManager;
            _oracle = oracle;
            _economyService = economyService;
            _dataService = dataService;
        }

        public void Start()
        {
            Debug.Log("[ActionExecutor] ⚡ Awakening the Divine Action Executor...");
            _subscription = EventBus.Subscribe<CloudActionEvent>(OnActionReceived);
            _disputeSubscription = EventBus.Subscribe<DisputeRaisedEvent>(OnDisputeRaised);
        }

        private void OnActionReceived(CloudActionEvent action)
        {
            Debug.Log($"<color=#00FFFF><b>[ActionExecutor]</b></color> ⚡ Отримано дію від ШІ-Оркестратора: <b>{action.Type}</b>");
            Debug.Log($"[ActionExecutor] Payload: {action.Payload}");

            // Повертаємо Око Оракула в звичайний активний стан (зупиняємо медитацію)
            if (OracleWidgetController.Instance != null)
            {
                Debug.Log("[ActionExecutor] 👁️ Око Оракула завершило медитацію та відкривається (Active).");
                OracleWidgetController.Instance.SetState(OracleState.Active);
            }

            try
            {
                switch (action.Type.ToUpper())
                {
                    case "SHOW_ORACLE_WHISPER":
                        ExecuteWhisper(action.Payload);
                        break;

                    case "PLAY_SFX":
                        ExecutePlaySfx(action.Payload);
                        break;

                    case "SWITCH_SCREEN":
                        ExecuteSwitchScreen(action.Payload);
                        break;

                    case "SHOW_ORACLE_CHAT":
                        ExecuteShowOracleChat(action.Payload);
                        break;

                    case "CREATE_CONTRACT_INTENT":
                        ExecuteCreateContractIntent(action.Payload);
                        break;

                    case "SEND_HARMONY_SYSTEM_MESSAGE":
                        ExecuteSendHarmonySystemMessage(action.Payload);
                        break;

                    case "SHOW_AUTONOMOUS_MATCH":
                        ExecuteShowAutonomousMatch(action.Payload);
                        break;

                    default:
                        Debug.LogWarning($"[ActionExecutor] ⚠️ Unhandled Action Type: {action.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActionExecutor] ❌ Error executing action '{action.Type}': {ex.Message}");
            }
        }

        private void ExecuteWhisper(string payloadJson)
        {
            var data = JsonUtility.FromJson<WhisperPayload>(payloadJson);
            if (data == null || string.IsNullOrEmpty(data.text))
            {
                Debug.LogWarning("[ActionExecutor] Invalid SHOW_ORACLE_WHISPER payload.");
                return;
            }

            WhisperColor color = WhisperColor.Gold;
            if (!string.IsNullOrEmpty(data.color))
            {
                Enum.TryParse(data.color, true, out color);
            }

            _whisperManager?.ShowWhisper(data.text, color);
        }

        private void ExecutePlaySfx(string payloadJson)
        {
            var data = JsonUtility.FromJson<SfxPayload>(payloadJson);
            if (data == null || string.IsNullOrEmpty(data.soundName))
            {
                Debug.LogWarning("[ActionExecutor] Invalid PLAY_SFX payload.");
                return;
            }

            _audioService?.PlaySFX(data.soundName);
        }

        private void ExecuteSwitchScreen(string payloadJson)
        {
            var data = JsonUtility.FromJson<ScreenPayload>(payloadJson);
            if (data == null || string.IsNullOrEmpty(data.panelId))
            {
                Debug.LogWarning("[ActionExecutor] Invalid SWITCH_SCREEN payload.");
                return;
            }

            var nav = UnityEngine.Object.FindAnyObjectByType<NexusNavigationManager>(FindObjectsInactive.Include);
            if (nav != null)
            {
                nav.SwitchTo(data.panelId);
            }
            else
            {
                Debug.LogWarning("[ActionExecutor] ⚠️ NexusNavigationManager not found in active scene.");
            }
        }

        private void ExecuteShowOracleChat(string payloadJson)
        {
            var data = JsonUtility.FromJson<ChatPayload>(payloadJson);
            if (data == null || string.IsNullOrEmpty(data.text))
            {
                Debug.LogWarning("[ActionExecutor] Invalid SHOW_ORACLE_CHAT payload.");
                return;
            }

            if (GeminiChatController.Instance != null)
            {
                GeminiChatController.Instance.ShowOracleMessage(data.text);
            }
            else
            {
                Debug.LogWarning("[ActionExecutor] ⚠️ GeminiChatController instance is not active.");
            }
        }

        private void ExecuteCreateContractIntent(string payloadJson)
        {
            var data = JsonUtility.FromJson<ContractIntentPayload>(payloadJson);
            if (data == null)
            {
                Debug.LogWarning("[ActionExecutor] Invalid CREATE_CONTRACT_INTENT payload.");
                return;
            }

            Debug.Log($"[ActionExecutor] 📜 Processing Voice Contract Intent. Realm: {(ContractRealm)data.realm}, Minutes: {data.minutes}, Fiat: {data.fiat}");

            string initiatorId = _authManager?.CurrentProfile?.UserId ?? "unknown";
            
            // Generate a random session ID for the intent
            string sessionId = $"voice_session_{Guid.NewGuid().ToString().Substring(0, 8)}";

            var contractEvent = new ContractCreatedEvent(
                sessionId,
                initiatorId,
                "", // No recipient yet, it's just created!
                (ContractRealm)data.realm,
                data.minutes,
                data.fiat,
                data.terms
            );

            // Re-publish the event locally so the rest of the app (UI, DB savey) can handle the created contract
            EventBus.Publish(contractEvent);
            Debug.Log($"[ActionExecutor] ✅ Published local ContractCreatedEvent from AI Intent!");
        }

        private void ExecuteSendHarmonySystemMessage(string payloadJson)
        {
            var data = JsonUtility.FromJson<ChatPayload>(payloadJson);
            if (data == null || string.IsNullOrEmpty(data.text))
            {
                Debug.LogWarning("[ActionExecutor] Invalid SEND_HARMONY_SYSTEM_MESSAGE payload.");
                return;
            }

            EventBus.Publish(new SystemMessageEvent(data.text));
            Debug.Log($"[ActionExecutor] ⚖️ Sent System Message to Harmony Channel: {data.text}");
        }

        private void ExecuteShowAutonomousMatch(string payloadJson)
        {
            var data = JsonUtility.FromJson<AutonomousMatchPayload>(payloadJson);
            if (data == null || string.IsNullOrEmpty(data.matchDescription))
            {
                Debug.LogWarning("[ActionExecutor] Invalid SHOW_AUTONOMOUS_MATCH payload.");
                return;
            }

            var evt = new AutonomousMatchFoundEvent(
                data.matchDescription, 
                data.oracleMessage,
                data.userANickname, data.userAAvatar, data.roleA,
                data.userBNickname, data.userBAvatar, data.roleB,
                data.userCNickname, data.userCAvatar, data.roleC
            );

            EventBus.Publish(evt);
            Debug.Log($"[ActionExecutor] 🔮 Proactive Game Master found a match loop! Firing event...");
            _audioService?.PlaySFX("AuraResonance");
        }

        public void Dispose()
        {
            Debug.Log("[ActionExecutor] ⚡ Shutting down Divine Action Executor.");
            _subscription?.Dispose();
            _disputeSubscription?.Dispose();
        }

        private async void OnDisputeRaised(DisputeRaisedEvent evt)
        {
            Debug.Log($"<color=#FF4500><b>[ActionExecutor]</b></color> ⚖️ Суд Хроносу скликано для сесії <b>{evt.SessionId}</b>");
            
            // 1. Око Оракула переходить в режим медіації
            if (OracleWidgetController.Instance != null)
            {
                OracleWidgetController.Instance.SetState(OracleState.Processing);
            }

            // 2. Збираємо контекст чату та учасників
            string chatHistory = "";
            UserProfile initiator = _authManager.CurrentProfile;
            UserProfile recipient = null;
            
            // Перевіряємо активний Nexus та канал зв'язку
            var nexus = NexusController.Instance;
            if (nexus != null)
            {
                recipient = nexus.ActivePartner;
                if (nexus.HarmonyChannel != null)
                {
                    chatHistory = nexus.HarmonyChannel.GetChatHistoryContext();
                }
            }

            if (recipient == null && _dataService != null)
            {
                // Якщо немає активного партнера в UI, спробуємо знайти в системі
                try
                {
                    var profiles = await _dataService.GetAllProfilesAsync(default);
                    recipient = profiles.Find(p => p.UserId != initiator.UserId);
                }
                catch
                {
                    recipient = new UserProfile("mock_partner", "mock_phone", "Адепт Каелен", 5f, 1);
                }
            }

            if (recipient == null)
            {
                recipient = new UserProfile("mock_partner", "mock_phone", "Адепт Каелен", 5f, 1);
            }

            if (string.IsNullOrEmpty(chatHistory))
            {
                chatHistory = "[Система]: Чат-лог відсутній. Початок розмови. Докази завантажено.";
            }

            // 3. Формуємо запит до Gemini
            var promptSO = TimeAura.Core.Data.SO.OracleCorePromptsSO.GetInstance();
            string prompt = string.Format(promptSO.ArbitrationVerdictTemplate, initiator.DisplayName, evt.SessionId, evt.Reason, chatHistory);

            Debug.Log("[ActionExecutor] 🔮 Звернення до Вищого Розуму для винесення вироку...");
            
            string verdictJson = await _oracle.RequestOracleWithSystem(
                "You are the supreme, humorous cosmic judge of the Court of Chronos (Суд Хроносу). Evaluate dispute and return ONLY valid JSON matching format.",
                prompt,
                "{\"refundPercentage\": 50, \"verdict\": \"Хронос бачить туман війни. Кошти розділено порівну між обома адептами.\"}"
            );

            // 4. Розбираємо вердикт
            int refundPercentage = 50;
            string verdictText = "Космічний баланс відновлено.";

            try
            {
                // Simple JSON parser
                if (verdictJson.Contains("\"refundPercentage\""))
                {
                    int index = verdictJson.IndexOf("\"refundPercentage\"");
                    string sub = verdictJson.Substring(index);
                    int colonIndex = sub.IndexOf(":");
                    int commaIndex = sub.IndexOf(",");
                    string val = sub.Substring(colonIndex + 1, commaIndex - colonIndex - 1).Trim();
                    int.TryParse(val, out refundPercentage);
                }

                if (verdictJson.Contains("\"verdict\""))
                {
                    int index = verdictJson.IndexOf("\"verdict\"");
                    string sub = verdictJson.Substring(index);
                    int colonIndex = sub.IndexOf(":");
                    int quoteStart = sub.IndexOf("\"", colonIndex);
                    int quoteEnd = sub.IndexOf("\"", quoteStart + 1);
                    verdictText = sub.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ActionExecutor] ⚠️ Помилка розбору вердикту ШІ: {ex.Message}. Використовуємо дефолтні значення.");
            }

            // 5. Виконуємо розрахунки та фінансову транзакцію
            int lockedMinutes = 120; // Default
            int refundMinutes = (lockedMinutes * refundPercentage) / 100;
            int releaseMinutes = lockedMinutes - refundMinutes;

            Debug.Log($"[ActionExecutor] ⚖️ Рішення Суду: Refund {refundPercentage}% ({refundMinutes} хв) ініціатору, Решта {releaseMinutes} хв виконавцю.");

            try
            {
                if (refundMinutes > 0)
                {
                    await _economyService.RefundFundsAsync(initiator, ContractRealm.Ether, refundMinutes, 0L, evt.SessionId);
                }
                
                if (releaseMinutes > 0 && recipient != null)
                {
                    await _economyService.ReleaseFundsToReceiverAsync(initiator, recipient, ContractRealm.Ether, releaseMinutes, 0L, evt.SessionId);
                }

                _audioService?.PlaySFX("SuccessRitual");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ActionExecutor] ❌ Помилка проведення транзакції Хроносу: {ex.Message}");
            }

            // 6. Відправляємо системне повідомлення в чат
            string finalSystemText = $"⚖️ <b>[ВИРОК СУДУ ХРОНОСУ]</b>\n\n{verdictText}\n\nПовернення ініціатору: {refundMinutes} хв ({refundPercentage}%).\nВиплата виконавцю: {releaseMinutes} хв ({100 - refundPercentage}%).";
            EventBus.Publish(new SystemMessageEvent(finalSystemText));

            // Повертаємо Око Оракула в нормальний стан
            if (OracleWidgetController.Instance != null)
            {
                OracleWidgetController.Instance.SetState(OracleState.Active);
            }
        }

        // --- Payload Deserialization Stubs ---

        [Serializable]
        private class WhisperPayload
        {
            public string text;
            public string color;
        }

        [Serializable]
        private class SfxPayload
        {
            public string soundName;
        }

        [Serializable]
        private class ScreenPayload
        {
            public string panelId;
        }

        [Serializable]
        private class ChatPayload
        {
            public string text;
        }

        [Serializable]
        private class ContractIntentPayload
        {
            public int realm;
            public int minutes;
            public float fiat;
            public string terms;
        }

        [Serializable]
        private class AutonomousMatchPayload
        {
            public string matchDescription;
            public string oracleMessage;
            
            // Triple-agent loop details
            public string userANickname;
            public string userAAvatar;
            public string roleA;

            public string userBNickname;
            public string userBAvatar;
            public string roleB;

            public string userCNickname;
            public string userCAvatar;
            public string roleC;
        }
    }
}
