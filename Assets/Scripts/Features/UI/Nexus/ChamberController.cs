using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Features.Auth;
using TimeAura.Core.Localization;
using TimeAura.Features.Localization;
using TimeAura.Features.Data;
using TimeAura.Features.Economy;

namespace TimeAura.Features.UI.Nexus
{
    public class ChamberController : MonoBehaviour
    {
        [Inject] private AuthManager _auth;
        [Inject] private LocalizationManager _localization;
        [Inject] private TimeAura.Core.Services.RemoteConfigService _remoteConfig;

        private VisualElement _panel;
        private Label _lblActivePacts;
        private Label _lblHoras;
        private Label _lblSymmetries;
        
        private ScrollView _activeSessionsList;
        private VisualElement _emptyState;

        private Button _btnActionCreate;
        private Button _btnActionOracle;
        private Button _btnActionVault;

        private NexusNavigationManager _navManager;

        public void Initialize(VisualElement root, NexusNavigationManager navManager)
        {
            _navManager = navManager;
            _panel = root.Q<VisualElement>("ChamberPanel");

            if (_panel == null) return;

            // Stats
            _lblActivePacts = _panel.Q<Label>("LblChamberActivePacts");
            _lblHoras = _panel.Q<Label>("LblChamberHoras");
            _lblSymmetries = _panel.Q<Label>("LblChamberSymmetries");

            // Sessions
            _activeSessionsList = _panel.Q<ScrollView>("ChamberActiveSessions");
            _emptyState = _panel.Q<VisualElement>("ChamberEmptyState");

            // Actions
            _btnActionCreate = _panel.Q<Button>("BtnActionCreate");
            _btnActionOracle = _panel.Q<Button>("BtnActionOracle");
            _btnActionVault = _panel.Q<Button>("BtnActionVault");

            // Bind Actions
            if (_btnActionCreate != null) _btnActionCreate.clicked += () => _navManager?.SwitchTo("feed");
            if (_btnActionOracle != null) _btnActionOracle.clicked += () => _navManager?.SwitchTo("sanctuary");
            if (_btnActionVault != null) _btnActionVault.clicked += () => _navManager?.SwitchTo("vault");

            Refresh();
        }

        public void Refresh()
        {
            if (_panel == null) return;

            // Populate Stats from UserProfile
            var profile = _auth?.CurrentProfile;
            if (profile != null)
            {
                if (_lblHoras != null) _lblHoras.text = EconomyFormatter.FormatHoras(profile.TimeBalanceMinutes);
                if (_lblSymmetries != null) _lblSymmetries.text = profile.Constellations.Count.ToString();
                if (_lblActivePacts != null) _lblActivePacts.text = "0"; // Hook this to active pacts later
            }

            // Populate Active Sessions List
            RefreshActiveSessions();
        }

        public event System.Action<UserProfile, TimeAura.Features.Social.Post> OnSessionEntered;

        private void RefreshActiveSessions()
        {
            if (_activeSessionsList == null || _emptyState == null) return;

            bool isDemo = _remoteConfig == null || _remoteConfig.IsDemoMode;
            if (!isDemo)
            {
                Debug.Log("[ChamberController] Subscribing to real active_sessions collection in Firestore...");
            }

            // Currently mock check: always show mock session
            bool hasActiveSessions = true; 

            if (hasActiveSessions)
            {
                _emptyState.style.display = DisplayStyle.None;
                _activeSessionsList.Clear();

                if (isDemo)
                {
                    var cardAsset = UnityEngine.Resources.Load<VisualTreeAsset>("UI/Nexus/ChamberSessionCard");
                if (cardAsset != null)
                {
                    var card = cardAsset.Instantiate();
                    
                    var btnEnter = card.Q<Button>("BtnEnterHarmony");
                    if (btnEnter != null)
                    {
                        btnEnter.clicked += () => 
                        {
                            // Create mock data
                            var mockPartner = new UserProfile("mock_lyra", "", "Lyra Starlight", 0f, 0);
                            var mockPost = new TimeAura.Features.Social.Post { 
                                postId = "mock_post_1", 
                                userId = "mock_lyra", 
                                username = "Lyra Starlight",
                                content = "Ремонт магічного крана", 
                                createdAt = System.DateTime.UtcNow,
                                horasPrice = 3
                            };
                            OnSessionEntered?.Invoke(mockPartner, mockPost);
                        };
                    }

                    _activeSessionsList.Add(card);
                }
                } // End if (isDemo)
            }
            else
            {
                _emptyState.style.display = DisplayStyle.Flex;
                _activeSessionsList.Clear();
                _activeSessionsList.Add(_emptyState);
            }
        }
    }
}
