using System;
using TimeAura.Core;
using TimeAura.Core.Services;
using TimeAura.Core.Infrastructure;
using UnityEngine;
using UnityEngine.UIElements;
using TimeAura.Core.Localization;
using TimeAura.Features.Localization;
using VContainer;

namespace TimeAura.Features.UI.Oracle
{
    /// <summary>
    /// AuraOrchestrationTester — Тимчасовий інструмент тестування (Debug Tool) ШІ-Оркестрації.
    /// Дозволяє розробникам вручну симулювати критичні бізнес-події 3-го рівня.
    /// </summary>
    public class AuraOrchestrationTester : MonoBehaviour
    {
        [Header("Mock Test Data")]
        [SerializeField] private string _mockSessionId = "test_harmony_session_999";
        [SerializeField] private string _mockInitiatorId = "mock_lyra";
        [SerializeField] private string _mockRecipientId = "mock_kaelen";
        [SerializeField] private int _mockLockedMinutes = 120;
        [SerializeField] private ContractRealm _mockContractRealm = ContractRealm.Ether;
        [SerializeField] private float _mockFiatAmount = 0f;
        
        [TextArea(3, 5)]
        [SerializeField] private string _mockContractTerms = "Намалювати 3 концепт-арти містичного порталу в стилі гласморфізму.";

        [TextArea(3, 5)]
        [SerializeField] private string _mockDisputeReason = "Виконавець не з'явився на об'єкт і затримав здачу Симетрії на 24 години.";

        [TextArea(3, 5)]
        [SerializeField] private string _mockVoiceCommand = "Я тракторист Василь, треба терміново зорати ниву за 2 години, плачу 3 Хораси.";

        [ContextMenu("🔴 Симулювати створення контракту (ContractCreatedEvent)")]
        public void SimulateContractCreated()
        {
            Debug.Log($"<color=#FF7F50><b>[TESTER]</b></color> 🟢 Симуляція події Рівня 3: <b>ContractCreatedEvent</b>");
            Debug.Log($"[TESTER] Payload: SessionId={_mockSessionId}, Realm={_mockContractRealm}, Minutes={_mockLockedMinutes}, Fiat={_mockFiatAmount}, Terms=\"{_mockContractTerms}\"");

            if (_mockContractRealm == ContractRealm.Ether && !ValidateChronoCredit(_mockLockedMinutes)) return;

            // 1. Переводимо Око Оракула в режим пульсуючої медитації (очікування хмари)
            SetOracleMeditating();

            // 2. Відправляємо подію в локальний EventBus
            var evt = new ContractCreatedEvent(
                _mockSessionId, 
                _mockInitiatorId, 
                _mockRecipientId, 
                _mockContractRealm,
                _mockLockedMinutes,
                _mockFiatAmount,
                _mockContractTerms
            );
            EventBus.Publish(evt);
        }

        [ContextMenu("🔴 Симулювати суперечку (DisputeRaisedEvent)")]
        public void SimulateDisputeRaised()
        {
            Debug.Log($"<color=#FF7F50><b>[TESTER]</b></color> 🟢 Симуляція події Рівня 3: <b>DisputeRaisedEvent</b>");
            Debug.Log($"[TESTER] Payload: SessionId={_mockSessionId}, RaisedBy={_mockInitiatorId}, Reason=\"{_mockDisputeReason}\"");

            // 1. Переводимо Око Оракула в режим пульсуючої медитації
            SetOracleMeditating();

            // 2. Відправляємо подію
            var evt = new DisputeRaisedEvent(_mockSessionId, _mockInitiatorId, _mockDisputeReason);
            EventBus.Publish(evt);
        }

        [ContextMenu("🔴 Симулювати голосову команду (VoiceCommandReceivedEvent)")]
        public void SimulateVoiceCommand()
        {
            Debug.Log($"<color=#FF7F50><b>[TESTER]</b></color> 🟢 Симуляція події Рівня 3: <b>VoiceCommandReceivedEvent</b>");
            Debug.Log($"[TESTER] Payload: CommandText=\"{_mockVoiceCommand}\"");

            // 1. Переводимо Око Оракула в режим пульсуючої медитації
            SetOracleMeditating();

            // 2. Відправляємо подію
            var evt = new VoiceCommandReceivedEvent(_mockVoiceCommand);
            EventBus.Publish(evt);
        }

        [ContextMenu("🔴 Симулювати пошук ланцюжка (Proactive Scan / SHOW_AUTONOMOUS_MATCH)")]
        public void SimulateProactiveMatch()
        {
            Debug.Log($"<color=#FF7F50><b>[TESTER]</b></color> 🟢 Симуляція події Рівня 3: <b>Proactive Autonomous Match</b>");

            string payload = @"{
                ""matchDescription"": ""Ти кодиш архітектуру для Анни ➔ Анна малює концепт для Василя ➔ Василь підстригає твій газон. Твій внесок маніфестовано!"",
                ""oracleMessage"": ""Космічне Кільце Симетрії замкнулося! Час перебуває в ідеальному резонансі між трьома адептами."",
                ""userANickname"": ""Ромчик (Ти)"",
                ""userAAvatar"": ""https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=120"",
                ""roleA"": ""C# Кодинг (2.5x)"",
                ""userBNickname"": ""Анна"",
                ""userBAvatar"": ""https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=120"",
                ""roleB"": ""3D Концепт (2.0x)"",
                ""userCNickname"": ""Василь"",
                ""userCAvatar"": ""https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=120"",
                ""roleC"": ""Садівництво (1.0x)""
            }";

            var evt = new CloudActionEvent("SHOW_AUTONOMOUS_MATCH", payload);
            EventBus.Publish(evt);
        }

        [ContextMenu("🔮 UI: Згенерувати Глобальне Пророцтво (Global Prophecy)")]
        public void SimulateGlobalProphecy()
        {
            Debug.Log("[TESTER] 🔮 Публікація Глобального Пророцтва...");
            var evt = new Core.GlobalProphecyEvent(
                "prophecy_123",
                "Епоха Дизайнерів",
                "Хронос відчуває нестачу візуальної гармонії у вашому секторі. Зорі вказують на великий попит на графічних магів.",
                "Пропонуйте послуги дизайну (UI/UX, Ілюстрації)",
                2.0f
            );
            EventBus.Publish(evt);
        }

        [ContextMenu("🎨 UI: Увімкнути/Вимкнути Вівтар Правосуддя (Toggle Altar Mode)")]
        public void ForceToggleAltarMode()
        {
            var uiDocs = FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach(var doc in uiDocs)
            {
                var harmonyPanel = doc.rootVisualElement?.Q("HarmonyPanel");
                if (harmonyPanel != null)
                {
                    if (harmonyPanel.ClassListContains("altar-mode"))
                    {
                        harmonyPanel.RemoveFromClassList("altar-mode");
                        Debug.Log("[TESTER] ⚖️ Вівтар Правосуддя ВИМКНЕНО (Altar Mode disabled)");
                    }
                    else
                    {
                        harmonyPanel.AddToClassList("altar-mode");
                        Debug.Log("[TESTER] ⚖️ Вівтар Правосуддя УВІМКНЕНО (Altar Mode enabled)");
                    }
                }
            }
        }

        [ContextMenu("📩 UI: Ін'єкція Системного Повідомлення від Судді (Test System Message)")]
        public void InjectTestSystemMessage()
        {
            Debug.Log("[TESTER] 📩 Відправка системного повідомлення у чат...");
            string text = "Ваші докази прийнято. Очікуйте на вирок Хроносу.";
            EventBus.Publish(new SystemMessageEvent(text));
        }

        [ContextMenu("🔄 UI: Змінити Персону Оракула (Cycle OracleTone)")]
        public void CycleUiPersonas()
        {
            var auth = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.Auth.AuthManager>(FindObjectsInactive.Include);
            if (auth != null && auth.CurrentProfile != null)
            {
                var profile = auth.CurrentProfile;
                int currentToneInt = (int)profile.OracleTone;
                currentToneInt = (currentToneInt + 1) % 3; // Mystic=0, Business=1, Casual=2
                
                profile.OracleTone = (TimeAura.Core.Data.SO.OracleTone)currentToneInt;
                Debug.Log($"[TESTER] 🔄 Персона змінена на: {profile.OracleTone}. Перевіряємо відображення Хорасів/Хвилин!");
                
                // Publish language changed to refresh all UI texts and layouts immediately
                EventBus.Publish(new TimeAura.Core.LanguageChangedEvent(Application.systemLanguage));
            }
            else
            {
                Debug.LogWarning("[TESTER] ⚠️ Неможливо змінити персону: профіль користувача не знайдено.");
            }
        }

        [ContextMenu("😈 UI: Нарахувати Темну Енергію (Inject Dark Energy)")]
        public void InjectDarkEnergy()
        {
            var auth = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.Auth.AuthManager>(FindObjectsInactive.Include);
            if (auth != null && auth.CurrentProfile != null)
            {
                auth.CurrentProfile.DarkEnergy += 10.0f;
                Debug.Log($"[TESTER] 😈 Темна енергія нарахована! Поточний рівень: {auth.CurrentProfile.DarkEnergy}. Якщо > 50, Оракул заблокує контракти.");
                
                if (auth.CurrentProfile.DarkEnergy > 50.0f)
                {
                    EventBus.Publish(new SystemMessageEvent("Ваша Темна Енергія занадто висока. Хронос обмежує ваші взаємодії."));
                }
            }
        }

        [ContextMenu("🔴 Симулювати ШІ-Еволюцію Титулу (AI Title Evolution)")]
        public async void SimulateAiTitleEvolution()
        {
            Debug.Log("<color=#FFD700><b>[TESTER]</b></color> 🟢 Запуск симуляції еволюції титулу за допомогою Gemini...");
            
            var auth = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.Auth.AuthManager>(FindObjectsInactive.Include);
            
            if (auth != null && auth.CurrentProfile != null)
            {
                var profile = auth.CurrentProfile;
                
                // Add mock legacy if empty to show humor capability
                if (profile.Legacy.Count == 0)
                {
                    profile.AddLegacyEntry("Дякую за фікс баги в репозиторії о 3 ночі!");
                    profile.AddLegacyEntry("Найкраща кава, яку я пив, дякую за допомогу!");
                }

                SetOracleMeditating();

                // Direct dependency retrieval or fallback request
                var oracleService = VContainer.Unity.LifetimeScope.Find<TimeAuraLifetimeScope>()?.Container.Resolve<IAuraOracleService>();
                if (oracleService != null)
                {
                    var verdict = await oracleService.PredictEvolutionaryTitleAsync(profile);
                    profile.AuraTitle = verdict.Title;
                    profile.AuraColorHex = verdict.ColorHex;
                    
                    Debug.Log($"[TESTER] 🎉 ШІ успішно еволюціонував ваш титул: <color={verdict.ColorHex}><b>{verdict.Title}</b></color>!");
                    Debug.Log($"[TESTER] Коментар Оракула: \"{verdict.Reason}\"");
                    
                    // Trigger UI refresh
                    EventBus.Publish(new TimeAura.Core.LanguageChangedEvent(Application.systemLanguage));
                }
                else
                {
                    Debug.LogWarning("[TESTER] ⚠️ IAuraOracleService не знайдено в контейнері VContainer.");
                }

                if (OracleWidgetController.Instance != null)
                {
                    OracleWidgetController.Instance.SetState(OracleState.Active);
                }
            }
            else
            {
                Debug.LogWarning("[TESTER] ⚠️ Неможливо запустити еволюцію: профіль користувача не знайдено.");
            }
        }

        private void SetOracleMeditating()
        {
            if (OracleWidgetController.Instance != null)
            {
                Debug.Log("[TESTER] 👁️ Око Оракула переходить у режим пульсуючої медітації (Processing)...");
                OracleWidgetController.Instance.SetState(OracleState.Processing);
            }
            else
            {
                Debug.LogWarning("[TESTER] ⚠️ OracleWidgetController.Instance не знайдено на сцені.");
            }
        }
        private bool ValidateChronoCredit(int requiredMinutes)
        {
            var auth = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.Auth.AuthManager>(FindObjectsInactive.Include);
            var loc = UnityEngine.Object.FindAnyObjectByType<TimeAura.Features.Localization.LocalizationManager>(FindObjectsInactive.Include);
            
            if (auth != null && auth.CurrentProfile != null)
            {
                var profile = auth.CurrentProfile;
                long expectedBalanceMinutes = profile.TimeBalanceMinutes - requiredMinutes;
                if (expectedBalanceMinutes < profile.ChronoCreditLimitMinutes)
                {
                    string rejectMsg = "Твій Хроно-борг занадто великий. Віддай свій час Нексусу!";
                    if (loc != null)
                    {
                        var tone = profile.OracleTone;
                        rejectMsg = loc.GetPersonaString(
                            TimeAura.Core.Localization.AuraTerms.ORACLE_DEBT_LIMIT_REACHED, 
                            tone, 
                            "Василю, у тебе закінчився ліміт довіри! Будь ласка, допоможи комусь із сусідів, щоб підняти баланс, а потім створюй новий запит!"
                        );
                    }

                    Debug.LogWarning($"<color=#FF0000><b>[TESTER] ❌ Відхилено:</b></color> {rejectMsg} (Борг хв.: {expectedBalanceMinutes}, Ліміт хв.: {profile.ChronoCreditLimitMinutes})");
                    if (OracleWidgetController.Instance != null)
                    {
                        OracleWidgetController.Instance.SetState(OracleState.DebtAlert);
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
