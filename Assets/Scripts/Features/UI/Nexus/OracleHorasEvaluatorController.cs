using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// Ваги Хроноса — The Scales of Chronos.
    /// Orchestrates the mystical Oracle evaluation panel where the AI determines
    /// the fair Horas cost of a task. Animates ancient scales balancing as
    /// coins appear one by one in the right pan.
    /// </summary>
    public class OracleHorasEvaluatorController
    {
        // ── External Dependencies ──────────────────────────────────────
        private readonly IAuraOracleService _oracle;
        private readonly AudioService _audio;
        private readonly HapticService _haptic;

        // ── UXML References ────────────────────────────────────────────
        private readonly VisualElement _root;
        private readonly Label _lblTitle;
        private readonly Label _lblSubtitle;
        private readonly VisualElement _scalesBeam;
        private readonly VisualElement _scalesPanLeft;
        private readonly VisualElement _scalesPanRight;
        private readonly Label _lblMasterSkills;
        private readonly VisualElement _coinsContainer;
        private readonly Label _lblHorasCount;
        private readonly VisualElement _verdictContainer;
        private readonly Label _lblVerdict;
        private readonly VisualElement _actionsContainer;
        private readonly Button _btnAccept;
        private readonly Button _btnManual;
        private readonly Button _btnCancel;

        // ── State ──────────────────────────────────────────────────────
        private bool _isVisible;
        private int _evaluatedHoras;
        private int _evaluatedMinutes;
        private UserProfile _clientProfile;

        // ── Events ─────────────────────────────────────────────────────
        /// <summary>Fires when the user accepts the Oracle's evaluation (or manual entry). Passes hours and minutes.</summary>
        public event Action<int, int> OnHorasAccepted;
        /// <summary>Fires when the user cancels the evaluation.</summary>
        public event Action OnCancelled;

        // ── Oracle JSON response shape ─────────────────────────────────
        [System.Serializable]
        private class OracleHorasResponse
        {
            public int    horas;
            public int    minutes;
            public string reasoning;
        }

        // ──────────────────────────────────────────────────────────────
        public OracleHorasEvaluatorController(
            VisualElement root,
            IAuraOracleService oracle,
            AudioService audio,
            HapticService haptic)
        {
            _root   = root;
            _oracle = oracle;
            _audio  = audio;
            _haptic = haptic;

            _lblTitle        = _root.Q<Label>("LblScalesTitle");
            _lblSubtitle     = _root.Q<Label>("LblScalesSubtitle");
            _scalesBeam      = _root.Q("ScalesBeam");
            _scalesPanLeft   = _root.Q("ScalesPanLeft");
            _scalesPanRight  = _root.Q("ScalesPanRight");
            _lblMasterSkills = _root.Q<Label>("LblMasterSkills");
            _coinsContainer  = _root.Q("HorasCoinsContainer");
            _lblHorasCount   = _root.Q<Label>("LblHorasCount");
            _verdictContainer = _root.Q("VerdictContainer");
            _lblVerdict      = _root.Q<Label>("LblVerdict");
            _actionsContainer = _root.Q("ScalesActions");
            _btnAccept       = _root.Q<Button>("BtnAcceptEvaluation");
            _btnManual       = _root.Q<Button>("BtnManualHoras");
            _btnCancel       = _root.Q<Button>("BtnCancelScales");

            if (_btnAccept != null) _btnAccept.clicked += OnAcceptClicked;
            if (_btnManual != null) _btnManual.clicked += OnManualClicked;
            if (_btnCancel != null) _btnCancel.clicked += OnCancelClicked;
        }

        // ── Public API ─────────────────────────────────────────────────

        /// <summary>
        /// Show the Scales panel and let the Oracle evaluate the task.
        /// </summary>
        /// <param name="masterProfile">The Master whose skills are being evaluated.</param>
        /// <param name="clientProfile">The Client who is requesting the evaluation.</param>
        public void Show(UserProfile masterProfile, UserProfile clientProfile, string chatHistory = null)
        {
            if (_root == null) return;

            _isVisible = true;
            _evaluatedHoras = 1; // safe default
            _evaluatedMinutes = 0;
            _clientProfile = clientProfile;

            // Reset visual state
            ResetVisuals();

            // Populate left pan with master skills
            string skillsText = BuildSkillsLabel(masterProfile);
            if (_lblMasterSkills != null) _lblMasterSkills.text = skillsText;

            // Show the panel
            _root.RemoveFromClassList("modal--hidden");
            _root.style.display = DisplayStyle.Flex;
            _root.pickingMode   = PickingMode.Position;
            Debug.Log("[Popup] Opened: HorasScalesPanel (Ваги Хроноса)");

            // Kick off the Oracle evaluation sequence
            RunEvaluationSequence(masterProfile, skillsText, chatHistory).Forget();
        }

        public void Hide()
        {
            _isVisible = false;
            _root.AddToClassList("modal--hidden");
            _root.style.display = StyleKeyword.Null;
            _root.pickingMode   = PickingMode.Ignore;
        }

        // ── Private: Sequence ──────────────────────────────────────────

        private async UniTaskVoid RunEvaluationSequence(UserProfile master, string skillsLabel, string chatHistory)
        {
            // 1. Animate left pan sinking (task is heavy)
            AnimatePanSinking(_scalesPanLeft, down: true);
            await UniTask.Delay(600);

            // 2. Ask Oracle
            SetSubtitle("Оракул читає нитки часу...");
            var verdict = await AskOracleForTime(master, skillsLabel, chatHistory);
            _evaluatedHoras = verdict.horas;
            _evaluatedMinutes = verdict.minutes;

            if (!_isVisible) return; // user cancelled while waiting

            // 3. Animate coins falling one by one into right pan
            SetSubtitle("Зважую хораси...");
            await AnimateCoinsDropping(verdict.horas);

            if (!_isVisible) return;

            // 4. Balance the beam
            AnimateBeamBalancing();
            AnimatePanSinking(_scalesPanLeft, down: false);
            _audio?.PlaySFX("HarmonyAligned", 0.6f);
            _haptic?.LightTap();

            await UniTask.Delay(500);
            if (!_isVisible) return;

            // 5. Show verdict text
            string verdictText = BuildVerdictText(verdict.horas, verdict.minutes, master);
            if (_lblVerdict != null) _lblVerdict.text = verdictText;
            if (_verdictContainer != null) _verdictContainer.style.opacity = 1f;
            SetSubtitle("Баланс часу встановлено");

            await UniTask.Delay(700);
            if (!_isVisible) return;

            // 6. Reveal action buttons
            if (_actionsContainer != null) _actionsContainer.style.opacity = 1f;
        }

        // ── Private: Oracle Call ───────────────────────────────────────

        private async UniTask<(int horas, int minutes)> AskOracleForTime(UserProfile master, string skills, string chatHistory = null)
        {
            if (_oracle == null) return FallbackTime(master);

            string prompt = BuildOraclePrompt(master, skills, chatHistory);

            try
            {
                string rawResponse = await _oracle.GetPhilosophicalGuidanceAsync(prompt, "HorasEvaluation");

                // Parse JSON from Oracle response
                string json = ExtractJsonFromResponse(rawResponse);
                if (!string.IsNullOrEmpty(json))
                {
                    var result = JsonUtility.FromJson<OracleHorasResponse>(json);
                    if (result != null)
                    {
                        int h = Mathf.Clamp(result.horas, 0, 20);
                        int m = Mathf.Clamp(result.minutes, 0, 59);
                        if (h == 0 && m < 5) m = 5; // ensure some minimum
                        Debug.Log($"[HorasEvaluator] ⚖️ Oracle verdict: {h} Horas, {m} minutes — {result.reasoning}");
                        return (h, m);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HorasEvaluator] ⚠️ Oracle parse failed: {ex.Message}. Using fallback.");
            }

            return FallbackTime(master);
        }

        private string BuildOraclePrompt(UserProfile master, string skills, string chatHistory = null)
        {
            var prompts = TimeAura.Core.Data.SO.OracleCorePromptsSO.GetInstance();
            string template = prompts != null ? prompts.ScalesEvaluatorPromptTemplate : "";
            if (string.IsNullOrEmpty(template)) return "";

            string chatText = "";
            if (!string.IsNullOrEmpty(chatHistory))
            {
                chatText = $"\nHere is the actual chat context / negotiation history between the two users:\n" +
                           $"====================================\n" +
                           $"{chatHistory}\n" +
                           $"====================================\n" +
                           $"Read this conversation history very carefully to understand the exact scope, volume, difficulty, and terms of the task they agreed upon. Base your valuation on what they discussed!\n";
            }

            string regionalContext = "";
            if (_clientProfile != null && master != null)
            {
                regionalContext = $"\nRegional Economic Context:\n" +
                                  $"- Adept (Initiator / Buyer) location: {_clientProfile.LocationZone ?? "Unknown"}\n" +
                                  $"- Master (Performer / Provider) location: {master.LocationZone ?? "Unknown"}\n" +
                                  $"Estimate the fair duration based on local market prices converted to hours/minutes, considering that 1 Horas = 1 hour of labor.\n";
            }

            try
            {
                return string.Format(template, skills, regionalContext, chatText);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HorasEvaluator] Prompt formatting failed: {ex.Message}. Using fallback.");
                return template;
            }
        }

        private static string ExtractJsonFromResponse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            int start = raw.IndexOf('{');
            int end   = raw.LastIndexOf('}');
            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);
            return null;
        }

        private static (int horas, int minutes) FallbackTime(UserProfile master)
        {
            int tagCount = (master?.AuraGifts?.Count ?? 0);
            int horas = Mathf.Clamp(tagCount > 0 ? tagCount : 2, 1, 5);
            return (horas, 0);
        }

        // ── Private: Animations ────────────────────────────────────────

        private async UniTask AnimateCoinsDropping(int count)
        {
            if (_coinsContainer == null) return;

            // Update the counter label as coins fall
            if (_lblHorasCount != null) _lblHorasCount.text = "0";

            var coins = new List<VisualElement>();

            // Ensure we clamp count to avoid dropping too many coins
            int dropCount = Mathf.Clamp(count, 0, 20);

            for (int i = 0; i < dropCount; i++)
            {
                if (!_isVisible) return;

                // Create coin
                var coin = new VisualElement();
                coin.AddToClassList("horas-coin");
                _coinsContainer.Add(coin);
                coins.Add(coin);

                // Sound + haptic
                _audio?.PlaySFX("CrystalClick", 0.4f);
                _haptic?.LightTap();

                // Let Unity process the add, then trigger transition
                await UniTask.Delay(50);
                coin.AddToClassList("horas-coin--visible");

                // Update count label
                if (_lblHorasCount != null) _lblHorasCount.text = $"{i + 1}";

                // Slightly rock the beam to show weight shifting
                RockBeam(i, dropCount);

                await UniTask.Delay(380);
            }

            // Final beam settle
            _haptic?.MediumTap();
        }

        private void RockBeam(int coinIndex, int totalCoins)
        {
            if (_scalesBeam == null || totalCoins <= 0) return;

            // As coins accumulate on right side, beam tilts clockwise (right side goes down)
            float progress = (float)(coinIndex + 1) / totalCoins;
            // Rock between left-heavy → balanced
            // Left starts heavy (-8°), gradually moves to 0° as coins fill right
            float targetAngle = Mathf.Lerp(-8f, 0f, progress);

            _scalesBeam.style.rotate = new StyleRotate(new Rotate(targetAngle));
        }

        private void AnimateBeamBalancing()
        {
            if (_scalesBeam == null) return;
            _scalesBeam.style.rotate = new StyleRotate(new Rotate(0f));
        }

        private void AnimatePanSinking(VisualElement pan, bool down)
        {
            if (pan == null) return;
            float offset = down ? 12f : 0f;
            pan.style.translate = new StyleTranslate(new Translate(0, offset, 0));
        }

        // ── Private: Text builders ─────────────────────────────────────

        private static string BuildSkillsLabel(UserProfile master)
        {
            if (master?.AuraGifts == null || master.AuraGifts.Count == 0)
                return "Майстер";

            // Show up to 3 tags to fit in the pan
            return string.Join(" · ", master.AuraGifts.GetRange(0, Mathf.Min(3, master.AuraGifts.Count)));
        }

        private static string BuildVerdictText(int horas, int minutes, UserProfile master)
        {
            string name = master?.DisplayName ?? "Майстер";
            
            // Format hours and minutes in Ukrainian
            string hoursStr = "";
            if (horas > 0)
            {
                string suffix = horas == 1 ? "Хорас" : horas < 5 ? "Хораси" : "Хорасів";
                hoursStr = $"{horas} {suffix}";
            }
            
            string minsStr = "";
            if (minutes > 0)
            {
                string suffix = minutes == 1 ? "Атом" : "Атомів";
                minsStr = $"{minutes} {suffix}";
            }
            
            string timeString = $"{hoursStr} {minsStr}".Trim();
            if (string.IsNullOrEmpty(timeString))
            {
                timeString = "0 Атомів";
            }

            return $"Ваги Хроноса визначили: ця Симетрія важить {timeString}.\n" +
                   $"Навички {name} оцінені справедливо.";
        }

        private void SetSubtitle(string text)
        {
            if (_lblSubtitle != null) _lblSubtitle.text = text;
        }

        // ── Private: Button handlers ───────────────────────────────────

        private void OnAcceptClicked()
        {
            _haptic?.MediumTap();
            _audio?.PlaySFX("RitualSeal", 0.6f);
            Hide();
            OnHorasAccepted?.Invoke(_evaluatedHoras, _evaluatedMinutes);
        }

        private void OnManualClicked()
        {
            // Expose the EscrowModal directly with empty fields
            _haptic?.LightTap();
            Hide();
            // Pass -1, -1 to signal "manual mode" — NexusController will open EscrowModal without pre-fills
            OnHorasAccepted?.Invoke(-1, -1);
        }

        private void OnCancelClicked()
        {
            _haptic?.LightTap();
            Hide();
            OnCancelled?.Invoke();
        }

        // ── Private: Reset ─────────────────────────────────────────────

        private void ResetVisuals()
        {
            // Clear coins
            _coinsContainer?.Clear();
            if (_lblHorasCount   != null) _lblHorasCount.text = "?";
            if (_lblVerdict      != null) _lblVerdict.text     = "";
            if (_verdictContainer != null) _verdictContainer.style.opacity = 0f;
            if (_actionsContainer != null) _actionsContainer.style.opacity = 0f;
            if (_scalesBeam      != null) _scalesBeam.style.rotate = new StyleRotate(new Rotate(-8f));
            AnimatePanSinking(_scalesPanLeft, down: false);
            AnimatePanSinking(_scalesPanRight, down: false);
            SetSubtitle("Визначаю важкість твого наміру");
        }
    }
}
