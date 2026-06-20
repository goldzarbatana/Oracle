using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Features.Auth;
using TimeAura.Features.Localization;
using TimeAura.Features.UI.Oracle;
using TimeAura.Features.Data;

namespace TimeAura.Features.UI.Nexus
{
    public class MasterDossierController
    {
        private VisualElement _root;
        private LocalizationManager _localization;
        private AudioService _audio;
        private HapticService _haptic;
        private IAuraOracleService _oracle;
        private AuthManager _auth;

        private Label _lblHeader;
        private Label _lblCardName;
        private Label _lblDistance;
        private Label _lblLastActive;
        private Label _lblCardBio;
        private VisualElement _bioContainer;
        private VisualElement _tagsContainer;
        
        private Button _btnDecline;
        private Button _btnInitiateRite;
        private Button _btnOracleExplain;
        private Button _btnOpenQuickChat;

        private Label _lblResonanceTitle;
        private Label _lblFrequencyTitle;
        private Label _lblDossierSectionTitle;

        public event Action OnDossierClosed;
        public event Action<UserProfile> OnQuickChatRequested;

        private UserProfile _targetUser;

        public MasterDossierController(
            VisualElement root, 
            LocalizationManager loc, 
            AudioService audio, 
            HapticService haptic,
            IAuraOracleService oracle,
            AuthManager auth)
        {
            _root = root;
            _localization = loc;
            _audio = audio;
            _haptic = haptic;
            _oracle = oracle;
            _auth = auth;

            _lblCardName = _root.Q<Label>("LblCardName");
            _lblDistance = _root.Q<Label>("LblDistance");
            _lblLastActive = _root.Q<Label>("LblLastActive");
            _lblCardBio = _root.Q<Label>("LblCardBio");
            _bioContainer = _root.Q("BioContainer");
            _tagsContainer = _root.Q("CardTags");

            _btnDecline = _root.Q<Button>("BtnDeclineDossier");
            _btnOpenQuickChat = _root.Q<Button>("BtnOpenQuickChat");
            _btnOracleExplain = _root.Q<Button>("BtnOracleExplain");

            _lblResonanceTitle = _root.Q<Label>("LblResonanceTitle");
            _lblFrequencyTitle = _root.Q<Label>("LblFrequencyTitle");
            _lblDossierSectionTitle = _root.Q<Label>("LblDossierSectionTitle");

            // Set the Oracle eye icon programmatically to avoid UXML url() parsing issues
            if (_btnOracleExplain != null)
            {
                var oracleEyeTex = Resources.Load<Texture2D>("UI/Oracle/Textures/OracleEye_Base");
                if (oracleEyeTex != null)
                    _btnOracleExplain.style.backgroundImage = new StyleBackground(oracleEyeTex);
            }
            
            // Allow close by clicking the X button we added in top bar
            var btnClose = _root.Q<Button>("BtnCloseDossier");
            if (btnClose != null)
            {
                btnClose.clicked += () => {
                    Hide();
                    OnDossierClosed?.Invoke();
                };
            }

            if (_btnDecline != null)
            {
                _btnDecline.clicked += () => {
                    Hide();
                    OnDossierClosed?.Invoke();
                };
            }

            if (_btnOpenQuickChat != null)
            {
                _btnOpenQuickChat.clicked += () => {
                    if (_targetUser == null) return;
                    Hide();
                    if (_targetUser.IsAiMaster)
                    {
                        if (_auth?.CurrentProfile != null)
                        {
                            _auth.CurrentProfile.ActiveSessionPrompt = _targetUser.ActiveSessionPrompt;
                        }
                        if (GeminiChatController.Instance != null)
                        {
                            GeminiChatController.Instance.Open();
                            GeminiChatController.Instance.ClearChat();
                            GeminiChatController.Instance.ShowOracleMessage($"Greetings. I am {_targetUser.DisplayName}. How may I assist you?");
                        }
                    }
                    else
                    {
                        OnQuickChatRequested?.Invoke(_targetUser);
                    }
                };
            }

            if (_btnOracleExplain != null)
            {
                _btnOracleExplain.clicked += OnOracleExplainClicked;
            }
        }

        public void Show(UserProfile target)
        {
            Debug.Log($"[MasterDossierController] 👁️ Show() invoked for {target?.DisplayName}");
            _targetUser = target;
            if (_lblCardName != null) 
            {
                string aiPrefix = target.IsAiMaster ? "🤖 [AI] " : "";
                _lblCardName.text = aiPrefix + target.DisplayName;
            }
            
            // Handle Bio Visibility
            if (_lblCardBio != null && _bioContainer != null)
            {
                bool hasBio = !string.IsNullOrEmpty(target.Bio);
                _lblCardBio.text = hasBio ? target.Bio : "Akasha remains silent about this master... Their intents are hidden in the mists of time.";
                _lblCardBio.style.color = hasBio ? new StyleColor(new Color(1f, 1f, 1f, 0.85f)) : new StyleColor(new Color(1f, 1f, 1f, 0.4f));
                _bioContainer.style.display = DisplayStyle.Flex; // Always show bio box now
            }

            // Populate Pillars
            if (_tagsContainer != null)
            {
                _tagsContainer.Clear();
                if (target.AuraGifts != null && target.AuraGifts.Count > 0)
                {
                    foreach (var pillar in target.AuraGifts)
                    {
                        var tag = new Label(pillar);
                        tag.AddToClassList("presence-tag");
                        tag.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.15f, 0.8f));
                        tag.style.color = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 1f)); // Gold
                        tag.style.marginTop = 5;
                        tag.style.marginBottom = 5;
                        tag.style.marginLeft = 5;
                        tag.style.marginRight = 5;
                        tag.style.paddingTop = 8;
                        tag.style.paddingBottom = 8;
                        tag.style.paddingLeft = 12;
                        tag.style.paddingRight = 12;
                        tag.style.borderTopLeftRadius = 8;
                        tag.style.borderTopRightRadius = 8;
                        tag.style.borderBottomLeftRadius = 8;
                        tag.style.borderBottomRightRadius = 8;
                        tag.style.borderTopWidth = 1;
                        tag.style.borderBottomWidth = 1;
                        tag.style.borderLeftWidth = 1;
                        tag.style.borderRightWidth = 1;
                        tag.style.borderTopColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.5f));
                        tag.style.borderBottomColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.5f));
                        tag.style.borderLeftColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.5f));
                        tag.style.borderRightColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.5f));
                        
                        _tagsContainer.Add(tag);
                    }
                }
                else
                {
                    var emptyTag = new Label("No awakened pillars");
                    emptyTag.AddToClassList("presence-tag");
                    emptyTag.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.3f));
                    _tagsContainer.Add(emptyTag);
                }
            }

            // Populate Stats
            var lblResonance = _root.Q<Label>("LblResonance");
            var lblFrequency = _root.Q<Label>("LblFrequency");
            
            if (lblResonance != null)
            {
                // In a real app, this comes from matchmaking score. For now, random high number based on Name length
                int resScore = 80 + (target.DisplayName != null ? target.DisplayName.Length % 20 : 0);
                lblResonance.text = $"{resScore}%";
            }
            if (lblFrequency != null)
            {
                // Frequency is a lore concept (energetic temporal vibe). Generates a unique Hz for each user.
                string uid = target.UserId ?? "Unknown";
                int freq = 400 + (Mathf.Abs(uid.GetHashCode()) % 200);
                lblFrequency.text = $"{freq} Hz";
            }

            if (_lblDistance != null) 
            {
                _lblDistance.text = "✦ < 5 KM"; 
            }

            if (_lblLastActive != null)
            {
                _lblLastActive.text = GetLastActiveString(target.UpdatedAt);
            }

            _root.RemoveFromClassList("dossier-overlay--hidden");
            _root.style.display = DisplayStyle.Flex; 
            _root.pickingMode = PickingMode.Position;
            Debug.Log($"[MasterDossierController] 👁️ Show() finished. display: {_root.style.display}, pickingMode: {_root.pickingMode}");
        }

        public void Hide()
        {
            _root.AddToClassList("dossier-overlay--hidden");
            _root.style.display = DisplayStyle.None; // Force hide
            _root.pickingMode = PickingMode.Ignore;
        }

        public void UpdateLocalization()
        {
            if (_localization == null || _root == null) return;
            bool isUk = _localization.CurrentLanguage == SystemLanguage.Ukrainian;

            if (_lblDossierSectionTitle != null) _lblDossierSectionTitle.text = isUk ? "ХРОНІКИ АКАШІ" : "AKASHIC RECORDS";
            if (_lblResonanceTitle != null) _lblResonanceTitle.text = isUk ? "РЕЗОНАНС" : "RESONANCE";
            if (_lblFrequencyTitle != null) _lblFrequencyTitle.text = isUk ? "ЧАСТОТА" : "FREQUENCY";
            if (_btnOpenQuickChat != null) _btnOpenQuickChat.text = isUk ? "ПЕРЕГУКНУТИСЯ" : "RESONATE";
            if (_btnDecline != null) _btnDecline.text = isUk ? "ЗАКРИТИ ДОСЬЄ" : "CLOSE DOSSIER";
            
            // Re-render strings that depend on target user if dossier is open
            if (_root.style.display == DisplayStyle.Flex && _targetUser != null)
            {
                if (_lblDistance != null) _lblDistance.text = "✦ < 5 KM";
                if (_lblLastActive != null) _lblLastActive.text = GetLastActiveString(_targetUser.UpdatedAt);
                
                if (_lblCardBio != null && _bioContainer != null)
                {
                    bool hasBio = !string.IsNullOrEmpty(_targetUser.Bio);
                    string defaultBio = isUk ? "Акаша мовчить про цього майстра..." : "Akasha remains silent about this master...";
                    _lblCardBio.text = hasBio ? _targetUser.Bio : defaultBio;
                }
            }
        }

        private string GetLastActiveString(long timestamp)
        {
            if (timestamp == 0) return "✦ UNKNOWN";
            
            var lastActive = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
            var now = DateTime.Now;
            var diff = now - lastActive;

            if (diff.TotalMinutes < 60) return "✦ IN NEXUS NOW";
            if (lastActive.Date == now.Date) return "✦ TODAY IN NEXUS";
            if (lastActive.Date == now.Date.AddDays(-1)) return "✦ YESTERDAY IN NEXUS";
            
            return $"✦ {lastActive:dd.MM.yy}";
        }

        private void OnOracleExplainClicked()
        {
            if (_oracle == null || _auth?.CurrentProfile == null || _targetUser == null) return;
            
            _haptic?.LightTap();
            _audio?.PlaySFX("MessageSent2", 0.5f);
            
            RequestSymmetryExplanation().Forget();
        }

        private async UniTaskVoid RequestSymmetryExplanation()
        {
            if (GeminiChatController.Instance != null)
            {
                GeminiChatController.Instance.Open();
                GeminiChatController.Instance.ShowOracleMessage("...");
            }

            var explanation = await _oracle.ExplainSymmetryAsync(_auth.CurrentProfile, _targetUser);
            
            if (GeminiChatController.Instance != null)
            {
                GeminiChatController.Instance.ShowOracleMessage(explanation);
            }
        }
    }
}
