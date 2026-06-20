using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;
using TimeAura.Core.Localization;
using TimeAura.Core.Data.SO;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Core.Services;
using LocationService = TimeAura.Core.Services.LocationService;
using LocationData = TimeAura.Core.Services.LocationData;

namespace TimeAura.Features.UI.Nexus
{
    public class NexusContentController : MonoBehaviour
    {
        public void SetActive(bool active) => enabled = active;

        [Inject] private AuthManager _authManager;
        [Inject] private LocalizationManager _localization;
        [Inject] private LocationService _locationService;
        [Inject] private IAuraOracleService _oracleService;

        private VisualElement _panelVault;
        private VisualElement _panelDiscovery;
        private ScrollView _discoveryFeed;
        private VisualElement _gridContainer;
        private VisualElement _discoveryTabs;

        private VisualTreeAsset _fateTileAsset;
        private AuraPillarSO[] _pillars;
        private List<MockAdeptData> _mockAdepts;
        private Button _activeTabBtn;

        public event Action<UserProfile> OnProfileAligned;

        private class MockAdeptData
        {
            public string UserId;
            public string Name;
            public string PillarId;
            public string PillarTitle;
            public Sprite PillarIcon;
            public int SharedTags;
            public Color ThemeColor;
            public double Latitude;
            public double Longitude;
            public bool IsAnonymous;
            public int Age;
        }

        public void Initialize(VisualElement root)
        {
            _panelVault = root.Q("VaultPanel");
            _panelDiscovery = root.Q("DiscoveryPanel");
            
            _discoveryFeed = root.Q<ScrollView>("DiscoveryFeed");
            _gridContainer = _discoveryFeed?.Q("GridContainer");
            _discoveryTabs = root.Q("DiscoveryTabs");

            _fateTileAsset = Resources.Load<VisualTreeAsset>("UI/FateTile");
            _pillars = Resources.LoadAll<AuraPillarSO>("Settings/Pillars");

            if (_fateTileAsset == null) Debug.LogError("[NexusContentController] ‼️ FateTile NOT FOUND in Resources/UI/FateTile.uxml");
            if (_pillars == null || _pillars.Length == 0) Debug.LogError("[NexusContentController] ‼️ No AuraPillars found in Resources/Settings/Pillars");

            if (_pillars.Length == 0)
            {
                Debug.LogWarning("[NexusContentController] ⚠️ No AuraPillars found in Resources/Settings/Pillars! Check your data.");
            }

            InitializeMockData();
            BuildTabs();

            // Default tab (Symmetry)
            if (_discoveryTabs != null && _discoveryTabs.childCount > 0)
            {
                var first = _discoveryTabs[0] as Button;
                if (first != null) SelectTab(first, "symmetry");
            }

            UpdateLocalization();
        }

        public void Refresh()
        {
            InitializeMockData();
            RenderFeed("all");
        }

        private void InitializeMockData()
        {
            var currentUser = _authManager?.CurrentProfile;
            var targetPillarId = currentUser?.PrimarySeek ?? "chronos";
            var targetPillar = _pillars?.FirstOrDefault(p => p.Id == targetPillarId) ?? _pillars?.FirstOrDefault();

            var currentPos = _locationService != null ? _locationService.CurrentLocation : new LocationData { Latitude = 50.4501, Longitude = 30.5234 }; // Kiev fallback

            _mockAdepts = new List<MockAdeptData>
            {
                new MockAdeptData { 
                    UserId = "u1", Name = "Lyra", PillarId = "chronos", PillarTitle = "CHRONOS SAGE", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "chronos")?.Icon,
                    SharedTags = 10, ThemeColor = new Color(0.2f, 0.8f, 1f), Age = 24, IsAnonymous = false,
                    Latitude = currentPos.Latitude + 0.005, Longitude = currentPos.Longitude + 0.005
                },
                new MockAdeptData { 
                    UserId = "u2", Name = "Kaelen", PillarId = "harmony", PillarTitle = "HARMONY WEAVER", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "harmony")?.Icon,
                    SharedTags = 7, ThemeColor = new Color(0.4f, 1f, 0.4f), Age = 29, IsAnonymous = false,
                    Latitude = currentPos.Latitude - 0.008, Longitude = currentPos.Longitude + 0.012
                },
                new MockAdeptData { 
                    UserId = "u3", Name = "Serafina", PillarId = "creation", PillarTitle = "QUANTUM CREATOR", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "creation")?.Icon,
                    SharedTags = 12, ThemeColor = new Color(1f, 0.84f, 0f), Age = 19, IsAnonymous = false,
                    Latitude = currentPos.Latitude + 0.015, Longitude = currentPos.Longitude - 0.005
                },
                new MockAdeptData { 
                    UserId = "u4", Name = "Adept-7721", PillarId = "void", PillarTitle = "VOID WALKER", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "void")?.Icon,
                    SharedTags = 3, ThemeColor = new Color(0.6f, 0.2f, 1f), Age = 45, IsAnonymous = true,
                    Latitude = currentPos.Latitude - 0.02, Longitude = currentPos.Longitude - 0.015
                },
                new MockAdeptData { 
                    UserId = "u5", Name = "Elias", PillarId = "strategy", PillarTitle = "WAR ARCHITECT", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "strategy")?.Icon,
                    SharedTags = 5, ThemeColor = new Color(1f, 0.3f, 0.3f), Age = 32, IsAnonymous = false,
                    Latitude = currentPos.Latitude + 0.002, Longitude = currentPos.Longitude - 0.025
                },
                new MockAdeptData { 
                    UserId = "u6", Name = "Adept-9090", PillarId = "healing", PillarTitle = "SOUL MENDER", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "healing")?.Icon,
                    SharedTags = 8, ThemeColor = new Color(0.3f, 1f, 0.8f), Age = 21, IsAnonymous = true,
                    Latitude = currentPos.Latitude - 0.005, Longitude = currentPos.Longitude - 0.005
                },
                new MockAdeptData { 
                    UserId = "u7", Name = "Nova", PillarId = "visionary", PillarTitle = "DREAMER", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "visionary")?.Icon,
                    SharedTags = 15, ThemeColor = new Color(1f, 0.5f, 0.8f), Age = 27, IsAnonymous = false,
                    Latitude = currentPos.Latitude + 0.03, Longitude = currentPos.Longitude + 0.01
                },
                new MockAdeptData { 
                    UserId = "u8", Name = "Adept-1102", PillarId = "crypto", PillarTitle = "CIPHER MASTER", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "crypto")?.Icon,
                    SharedTags = 2, ThemeColor = new Color(0.1f, 0.1f, 0.1f), Age = 56, IsAnonymous = true,
                    Latitude = currentPos.Latitude + 0.01, Longitude = currentPos.Longitude + 0.04
                },
                new MockAdeptData { 
                    UserId = "u9", Name = "Xavier", PillarId = "art", PillarTitle = "VOID ARTIST", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "art")?.Icon,
                    SharedTags = 9, ThemeColor = new Color(0.8f, 0.2f, 0.6f), Age = 31, IsAnonymous = false,
                    Latitude = currentPos.Latitude - 0.04, Longitude = currentPos.Longitude + 0.02
                },
                new MockAdeptData { 
                    UserId = "u10", Name = "Luna", PillarId = "music", PillarTitle = "SOUND SAGE", 
                    PillarIcon = _pillars?.FirstOrDefault(p => p.Id == "music")?.Icon,
                    SharedTags = 11, ThemeColor = new Color(0.2f, 0.5f, 1f), Age = 23, IsAnonymous = false,
                    Latitude = currentPos.Latitude - 0.015, Longitude = currentPos.Longitude - 0.03
                }
            };
        }

        private void BuildTabs()
        {
            if (_discoveryTabs == null) return;
            _discoveryTabs.Clear();

            // SYMMETRY Tab (Matchmaking)
            AddTab(AuraTerms.TAB_SYMMETRY, "symmetry", null);

            // NEAR Tab (Location)
            AddTab(AuraTerms.TAB_NEAR, "near", null);

            // Pillar Tabs (Dynamic)
            foreach (var pillar in _pillars)
            {
                AddTab(pillar.LocalizationKey, pillar.Id, pillar.Icon);
            }
        }

        private void AddTab(string locKey, string filterId, Sprite icon)
        {
            var btn = new Button();
            btn.clicked += () => SelectTab(btn, filterId);
            btn.AddToClassList("tab-btn");
            if (icon != null)
            {
                var iconEl = new VisualElement();
                iconEl.AddToClassList("tab-icon");
                iconEl.style.backgroundImage = new StyleBackground(icon);
                btn.Add(iconEl);
            }

            var tone = _localization != null ? _localization.CurrentTone : OracleTone.Business;
            var label = new Label(_localization != null ? _localization.GetPersonaString(locKey, tone, filterId.ToUpper()) : filterId.ToUpper());
            label.AddToClassList("tab-label");
            btn.Add(label);


            _discoveryTabs.Add(btn);
        }

        private void SelectTab(Button btn, string filterId)
        {
            if (_activeTabBtn != null) _activeTabBtn.RemoveFromClassList("tab-btn--active");
            _activeTabBtn = btn;
            _activeTabBtn.AddToClassList("tab-btn--active");
            RenderFeed(filterId);
        }

        private void RenderFeed(string filterId)
        {
            if (_gridContainer == null || _fateTileAsset == null) return;
            _gridContainer.Clear();

            IEnumerable<MockAdeptData> filtered = _mockAdepts;
            var currentUser = _authManager?.CurrentProfile;

            if (filterId == "symmetry")
            {
                // Sort by Resonance (shared tags + pillar match)
                filtered = _mockAdepts.OrderByDescending(a => a.SharedTags);
            }
            else if (filterId == "near")
            {
                // Sort by physical proximity
                if (_locationService != null)
                {
                    var myLoc = _locationService.CurrentLocation;
                    filtered = _mockAdepts.OrderBy(a => LocationService.GetDistanceBetween(myLoc.Latitude, myLoc.Longitude, a.Latitude, a.Longitude));
                }
            }
            else if (filterId != "all")
            {
                // Filter by specific Pillar
                filtered = _mockAdepts.Where(a => a.PillarId == filterId);
            }

            foreach (var adept in filtered)
            {
                var card = _fateTileAsset.Instantiate();
                
                // Fields
                card.Q<Label>("LblName").text = adept.Name;
                card.Q<Label>("LblPillar").text = adept.PillarTitle;
                
                var iconPillar = card.Q("IconPillar");
                if (iconPillar != null && adept.PillarIcon != null)
                    iconPillar.style.backgroundImage = new StyleBackground(adept.PillarIcon);

                var lblDistance = card.Q<Label>("LblDistance");
                if (lblDistance != null && _locationService != null)
                {
                    var myLoc = _locationService.CurrentLocation;
                    float dist = LocationService.GetDistanceBetween(myLoc.Latitude, myLoc.Longitude, adept.Latitude, adept.Longitude);
                    lblDistance.text = LocationService.FormatDistance(dist);
                }

                var auraGlow = card.Q("AuraGlow");
                if (auraGlow != null)
                {
                    // Task 2: Enhanced Anonymous Logic with border
                    if (adept.IsAnonymous)
                    {
                        auraGlow.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.3f));
                        auraGlow.style.borderTopColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 1f));
                    }
                    else
                    {
                        Color pillarColor = adept.ThemeColor;
                        auraGlow.style.backgroundColor = new StyleColor(new Color(pillarColor.r, pillarColor.g, pillarColor.b, 0.5f));
                        auraGlow.style.borderTopColor = new StyleColor(pillarColor);
                    }
                }

                var lblAge = card.Q<Label>("LblAge");
                if (lblAge != null) lblAge.text = $"{adept.Age}y";

                // Task 2: Gift/Seek Icons
                var iconGift = card.Q("IconGift");
                if (iconGift != null && adept.PillarIcon != null) 
                    iconGift.style.backgroundImage = new StyleBackground(adept.PillarIcon);
                
                // Mock seek icon using a random pillar for variety in demo
                var iconSeek = card.Q("IconSeek");
                if (iconSeek != null && _pillars.Length > 0)
                    iconSeek.style.backgroundImage = new StyleBackground(_pillars[UnityEngine.Random.Range(0, _pillars.Length)].Icon);

                var lblInitials = card.Q<Label>("LblInitials");
                if (lblInitials != null) lblInitials.text = adept.Name.Substring(0, Math.Min(2, adept.Name.Length)).ToUpper();

                var btnAlign = card.Q<Button>("BtnAlign");
                if (btnAlign != null)
                {
                    btnAlign.clicked += () => 
                    {
                        var profile = new UserProfile(adept.UserId, "", adept.Name, 0, 0);
                        OnProfileAligned?.Invoke(profile);
                    };
                }

                card.RegisterCallback<ClickEvent>(evt => 
                {
                    var profile = new UserProfile(adept.UserId, "", adept.Name, 0, 0);
                    OnProfileAligned?.Invoke(profile);
                });

                _gridContainer.Add(card);
            }
        }

        public void DisplaySymmetryResults(List<UserProfile> matches)
        {
            if (_gridContainer == null) Debug.LogError("[NexusContentController] ‼️ _gridContainer (GridContainer) is NULL! Check your UXML.");
            if (_fateTileAsset == null) Debug.LogError("[NexusContentController] ‼️ _fateTileAsset (UI/FateTile) is NULL! Check your Resources/UI folder.");

            if (_gridContainer == null || _fateTileAsset == null) return;
            
            // Task 3: Switch UI context to symmetry results
            _gridContainer.Clear();
            Debug.Log($"[NexusContentController] 🌌 Preparing to render {matches.Count} symmetry results...");
            
            // Map UserProfile to MockAdeptData (or similar UI model) for rendering
            foreach (var profile in matches)
            {
                var card = _fateTileAsset.Instantiate();
                
                // Fields
                card.Q<Label>("LblName").text = profile.Nickname;
                
                var targetPillarId = profile.PrimaryPillar ?? "chronos";
                var targetPillar = _pillars?.FirstOrDefault(p => p.Id == targetPillarId);
                
                var tone = _localization.CurrentTone;
                card.Q<Label>("LblPillar").text = targetPillar != null 
                    ? _localization.GetPersonaString(targetPillar.LocalizationKey, tone, targetPillar.Id).ToUpper() 
                    : "MASTER";

                
                var iconPillar = card.Q("IconPillar");
                if (iconPillar != null && targetPillar?.Icon != null)
                    iconPillar.style.backgroundImage = new StyleBackground(targetPillar.Icon);

                var auraGlow = card.Q("AuraGlow");
                if (auraGlow != null && targetPillar != null)
                {
                    // Task 2: Enhanced Anonymous Logic for search results
                    if (profile.IsAnonymous)
                    {
                        auraGlow.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 0.3f));
                        auraGlow.style.borderTopColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 1f));
                    }
                    else
                    {
                        Color pillarColor = targetPillar.ThemeColor;
                        auraGlow.style.backgroundColor = new StyleColor(new Color(pillarColor.r, pillarColor.g, pillarColor.b, 0.5f));
                        auraGlow.style.borderTopColor = new StyleColor(pillarColor);
                    }
                }

                var lblAge = card.Q<Label>("LblAge");
                if (lblAge != null) lblAge.text = $"{profile.Age}y";

                var iconGift = card.Q("IconGift");
                if (iconGift != null && targetPillar?.Icon != null)
                    iconGift.style.backgroundImage = new StyleBackground(targetPillar.Icon);

                var iconSeek = card.Q("IconSeek");
                if (iconSeek != null && !string.IsNullOrEmpty(profile.PrimarySeek))
                {
                    var seekPillar = _pillars.FirstOrDefault(p => p.Id == profile.PrimarySeek);
                    if (seekPillar != null) iconSeek.style.backgroundImage = new StyleBackground(seekPillar.Icon);
                }

                var lblInitials = card.Q<Label>("LblInitials");
                if (lblInitials != null) lblInitials.text = profile.Nickname.Substring(0, Math.Min(2, profile.Nickname.Length)).ToUpper();

                var btnAlign = card.Q<Button>("BtnAlign");
                if (btnAlign != null)
                {
                    btnAlign.clicked += () => 
                    {
                        OnProfileAligned?.Invoke(profile);
                    };
                }

                card.RegisterCallback<ClickEvent>(evt => 
                {
                    OnProfileAligned?.Invoke(profile);
                });

                _gridContainer.Add(card);

                // Fetch Oracle Whisper for top matches
                if (matches.IndexOf(profile) < 3 && _oracleService != null)
                {
                    FetchWhisper(card, profile).Forget();
                }
            }

            Debug.Log($"[NexusContentController] ✧ Rendered {matches.Count} Symmetry results.");
        }

        private async UniTaskVoid FetchWhisper(VisualElement card, UserProfile target)
        {
            var lblWhisper = card.Q<Label>("LblOracleWhisper");
            if (lblWhisper == null) return;

            try
            {
                var explanation = await _oracleService.ExplainSymmetryAsync(_authManager.CurrentProfile, target);
                lblWhisper.text = explanation;
                lblWhisper.style.display = DisplayStyle.Flex;
                lblWhisper.style.opacity = 0;
                
                // Subtle fade in
                for (int i = 0; i <= 10; i++)
                {
                    lblWhisper.style.opacity = i / 10f;
                    await UniTask.Delay(30);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Oracle] Whisper failed: {ex.Message}");
            }
        }

        public void UpdateLocalization()
        {
            if (_localization == null) return;

            var tone = _localization.CurrentTone;
            if (_panelVault != null)
            {
                _panelVault.Q<Label>("VaultHeader").text = _localization.GetPersonaString(AuraTerms.VAULT_HEADER, tone, "MASTER VAULT");
            }
        }


        public void RefreshVault()
        {
            var profile = _authManager?.CurrentProfile;
            if (profile == null) return;

            var lblHoras = _panelVault?.Q<Label>("LblHorasBalance");
            if (lblHoras != null) lblHoras.text = profile.Horas.ToString("N0");
        }

        public async UniTask RefreshFeedAsync()
        {
            // Optional: Reload logic if needed, currently RenderFeed handles synchronous build.
            await UniTask.Yield();
        }
    }
}
