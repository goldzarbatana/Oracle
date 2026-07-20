using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Localization;
using TimeAura.Features.Localization;
using TimeAura.Core.Services;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Features.UI.Oracle;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// Manages panel switching, bottom navigation, and swipe gestures.
    /// Extracted from the NexusController monolith.
    /// </summary>
    public class NexusNavigationManager : MonoBehaviour
    {
        public void SetActive(bool active) => enabled = active;

        [Inject] private LocalizationManager _localization;
        [Inject] private AudioService _audioService;
        [Inject] private HapticService _hapticService;
        [Inject] private UIManager _uiManager;

        private VisualElement _bottomNav;
        
        public void ToggleMenu(bool show)
        {
            if (_bottomNav == null) return;
            if (show) _bottomNav.RemoveFromClassList("hidden");
            else _bottomNav.AddToClassList("hidden");
        }
        private Label _lblNavFeed, _lblNavOracle, _lblNavVault, _lblNavSanctuary;
        private List<VisualElement> _allPanels;
        private string _activePanelId = "feed";
        private string _previousPanelId = "feed";

        // Swipe Settings
        private Vector2 _swipeStartPosition;
        private float _swipeStartTime;
        private const float SwipeThreshold = 80f;
        private const float SwipeMaxTime = 0.5f;
        private readonly string[] _swipeOrder = { "feed", "chamber" };

        public string ActivePanelId => _activePanelId;
        public event Action<string> OnPanelSwitched;

        public void Initialize(VisualElement root, List<VisualElement> panels)
        {
            _allPanels = panels;
            _bottomNav = root.Q("NavigationDock");
            
            _lblNavFeed = root.Q<Label>("LblNavFeed");
            _lblNavOracle = root.Q<Label>("LblNavOracle");

            root.Q("NavFeed")?.RegisterCallback<ClickEvent>(e => SwitchTo("feed"));
            root.Q("BtnOpenChamber")?.RegisterCallback<ClickEvent>(e => SwitchTo("chamber"));
            var btnOracle = root.Q("BtnOracleEyeCollapsed");
            if (btnOracle != null)
            {
                float lastToggleTime = 0f;
                // Register both Click and PointerUp to ensure mobile taps are caught
                EventCallback<EventBase> toggleOracle = e => {
                    if (Time.time - lastToggleTime < 0.2f) return;
                    lastToggleTime = Time.time;
                    
                    if (OracleWidgetController.Instance != null)
                    {
                        _audioService?.PlaySFX("CrystalClick");
                        _hapticService?.MediumTap();
                        
                        var widget = OracleWidgetController.Instance;
                        if (widget.WidgetState == OracleWidgetController.OracleWidgetState.Closed)
                        {
                            widget.OpenWidget();
                        }
                        else
                        {
                            widget.CloseWidget();
                        }
                    }
                };
                
                btnOracle.RegisterCallback<ClickEvent>(toggleOracle);
                btnOracle.RegisterCallback<PointerUpEvent>(toggleOracle);
            }

            root.Q("BtnOpenProfile")?.RegisterCallback<ClickEvent>(e => SwitchTo("vault"));
            // BtnConsultOracle removed — was a duplicate of BtnOpenSanctuary.
            // Now BtnOpenSanctuary is the single entry point for Oracle/Sanctuary.
            root.Q("BtnOpenSanctuary")?.RegisterCallback<ClickEvent>(e => SwitchTo("sanctuary"));
            // BtnConsultOracle is repurposed in UXML: it now shows Horas balance (see VaultController).

            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(evt => {
                _swipeStartPosition = evt.position;
                _swipeStartTime = Time.time;
                // Spawn click burst only for significant taps (not every tap for performance)
                // SpawnClickBurst disabled per UX audit #11 — too noisy
            }, TrickleDown.TrickleDown);

            // Breathing animation — keep it. Magic particles throttled per UX audit #11.
            root.schedule.Execute(PulseGlow).Every(2000);   // was 1500
            root.schedule.Execute(SpawnMagicParticle).Every(1200); // was 400

            UpdateLocalization();
        }

        private List<VisualElement> _particles = new List<VisualElement>();
        
        private void SpawnMagicParticle()
        {
            if (_bottomNav == null) return;
            var activeNav = _bottomNav.Q(className: "nav-item--active");

            _particles.RemoveAll(p => p == null || p.parent == null);
            if (_particles.Count > 8) return; // UX audit #11: was 25, reduced to 8

            if (activeNav != null)
            {
                var particle = CreateParticle();
                activeNav.Add(particle);
                AnimateParticle(particle).Forget();
            }

            // Spawn on glowing elements with 10% chance (was 30%) to stay subtle
            var glowingElements = _bottomNav.panel.visualTree.Query<VisualElement>(className: "aura-glow--gold").ToList();

            foreach (var glowEl in glowingElements)
            {
                if (UnityEngine.Random.value < 0.1f) // UX audit #11: was 0.3f
                {
                    var particle = CreateParticle();
                    glowEl.Add(particle);
                    AnimateParticle(particle).Forget();
                }
            }
        }

        private VisualElement CreateParticle()
        {
            var particle = new VisualElement();
            particle.AddToClassList("magic-particle");
            float startX = UnityEngine.Random.Range(20f, 80f);
            particle.style.left = Length.Percent(startX);
            particle.style.bottom = 0;
            particle.style.opacity = 0f;
            _particles.Add(particle);
            return particle;
        }

        private async UniTaskVoid AnimateParticle(VisualElement particle)
        {
            if (particle == null) return;
            
            float endY = UnityEngine.Random.Range(-20f, -60f); // Float upwards
            float endX = UnityEngine.Random.Range(-30f, 30f);  // Drift sideways
            float duration = UnityEngine.Random.Range(1.5f, 3f);
            
            particle.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) });
            particle.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) });
            
            await UniTask.Delay(50); // wait for layout to apply
            if (particle == null) return;

            particle.style.translate = new Translate(endX, endY, 0);
            particle.style.opacity = 0.8f;
            particle.style.scale = new Scale(new Vector2(UnityEngine.Random.Range(0.5f, 1.5f), UnityEngine.Random.Range(0.5f, 1.5f)));

            await UniTask.Delay((int)(duration * 500)); // Halfway through, start fading out
            if (particle == null) return;
            
            particle.style.opacity = 0f;
            
            await UniTask.Delay((int)(duration * 500));
            if (particle != null && particle.parent != null)
            {
                particle.RemoveFromHierarchy();
                _particles.Remove(particle);
            }
        }

        private void SpawnClickBurst(VisualElement root, Vector2 position)
        {
            if (root == null) return;
            
            _particles.RemoveAll(p => p == null || p.parent == null);
            if (_particles.Count > 80) return; // prevent overflow

            int count = UnityEngine.Random.Range(6, 15);
            for (int i = 0; i < count; i++)
            {
                var particle = new VisualElement();
                particle.AddToClassList("magic-particle");
                
                root.Add(particle);
                _particles.Add(particle);

                particle.style.position = Position.Absolute;
                particle.style.left = position.x;
                particle.style.top = position.y;
                particle.style.opacity = 0f;

                AnimateBurstParticle(particle).Forget();
            }
        }

        private async UniTaskVoid AnimateBurstParticle(VisualElement particle)
        {
            if (particle == null) return;
            
            float endY = UnityEngine.Random.Range(-80f, 80f);
            float endX = UnityEngine.Random.Range(-80f, 80f);
            float duration = UnityEngine.Random.Range(0.6f, 1.5f);
            
            particle.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) });
            particle.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction> { new EasingFunction(EasingMode.EaseOutSine) });
            
            await UniTask.Delay(10);
            if (particle == null) return;

            particle.style.translate = new Translate(endX, endY, 0);
            particle.style.opacity = UnityEngine.Random.Range(0.7f, 1f);
            particle.style.scale = new Scale(new Vector2(UnityEngine.Random.Range(1.0f, 2.5f), UnityEngine.Random.Range(1.0f, 2.5f)));

            await UniTask.Delay((int)(duration * 500));
            if (particle == null) return;
            
            particle.style.opacity = 0f;
            
            await UniTask.Delay((int)(duration * 500));
            if (particle != null && particle.parent != null)
            {
                particle.RemoveFromHierarchy();
                _particles.Remove(particle);
            }
        }

        private bool _glowPulse = false;
        private void PulseGlow()
        {
            if (_bottomNav == null) return;
            _glowPulse = !_glowPulse;
            
            var activeNav = _bottomNav.Q(className: "nav-item--active");
            if (activeNav != null)
            {
                if (_glowPulse) activeNav.AddToClassList("aura-glow--pulse");
                else activeNav.RemoveFromClassList("aura-glow--pulse");
            }
        }

        public void UpdateLocalization()
        {
            if (_localization == null) return;
            if (_lblNavFeed != null) _lblNavFeed.text = _localization.Get(AuraTerms.NAV_FEED, "FEED").ToUpper();
            if (_lblNavOracle != null) _lblNavOracle.text = _localization.Get("nav_oracle", "👁️ ORACLE").ToUpper();
        }

        public void SetTabResonance(string tabId, bool isActive, string glowClass = "aura-glow--cyan")
        {
            // Map tabId to the actual VisualElement name (NavFeed, BtnOpenChamber, etc.)
            string elementId = tabId.ToLower() switch
            {
                "feed" => "NavFeed",
                "chamber" => "BtnOpenChamber",
                "vault" => "BtnOpenProfile",
                "sanctuary" => "BtnOpenSanctuary",
                _ => ""
            };

            if (string.IsNullOrEmpty(elementId)) return;

            // Find in the whole visual tree instead of just _bottomNav, since BtnOpenSanctuary is in TopBar
            var tab = _bottomNav?.panel?.visualTree?.Q(elementId);
            if (tab == null) return;

            if (isActive)
            {
                tab.AddToClassList(glowClass);
            }
            else
            {
                tab.RemoveFromClassList(glowClass);
            }
        }

        public void SwitchTo(string panelId)
        {
            string newId = panelId.ToLower();
            
            // Redirect deprecated panel views to their modal overlays
            if (newId == "radar")
            {
                newId = "feed";
                var radarCtrl = UnityEngine.Object.FindAnyObjectByType<RadarController>();
                if (radarCtrl != null)
                {
                    radarCtrl.StartRadarSearch();
                }
            }
            if (newId == "aura")
            {
                newId = "vault";
                var auraPanel = _bottomNav?.panel?.visualTree?.Q("AuraPanel");
                if (auraPanel != null)
                {
                    auraPanel.RemoveFromClassList("panel--hidden");
                    auraPanel.style.display = DisplayStyle.Flex;
                    auraPanel.style.visibility = Visibility.Visible;
                    auraPanel.style.opacity = 1f;
                    auraPanel.pickingMode = PickingMode.Position;
                    auraPanel.BringToFront();
                }
            }
            else if (newId != "vault")
            {
                var auraPanel = _bottomNav?.panel?.visualTree?.Q("AuraPanel");
                if (auraPanel != null)
                {
                    auraPanel.style.display = DisplayStyle.None;
                    auraPanel.style.visibility = Visibility.Hidden;
                    auraPanel.style.opacity = 0f;
                    auraPanel.pickingMode = PickingMode.Ignore;
                }
            }

            if (_activePanelId != newId && _activePanelId != "settings" && _activePanelId != "chamber")
            {
                _previousPanelId = _activePanelId;
            }

            _activePanelId = newId;
            
            // Clear glow when selecting the tab
            SetTabResonance(_activePanelId, false, "aura-glow--cyan");
            SetTabResonance(_activePanelId, false, "aura-glow--gold");
            SetTabResonance(_activePanelId, false, "aura-glow--sapphire");
            
            if (_bottomNav != null) 
            {
                bool shouldHideNav = (_activePanelId == "settings");
                _bottomNav.style.display = shouldHideNav ? DisplayStyle.None : DisplayStyle.Flex;
                
                // Ensure the dock is on top if it's visible
                if (!shouldHideNav) _bottomNav.BringToFront();
            }

            foreach (var p in _allPanels)
            {
                if (p == null) continue;
                
                bool isTarget = false;

                // 1. Check explicit overrides first
                if (_activePanelId == "feed")
                {
                    isTarget = (p.name == "FeedPanel");
                }
                else if (_activePanelId == "chamber")
                {
                    isTarget = (p.name == "ChamberPanel");
                }
                else if (_activePanelId == "discovery")
                {
                    isTarget = (p.name == "DiscoveryPanel");
                }
                else if (_activePanelId == "social")
                {
                    isTarget = (p.name == "FeedPanel");
                }
                else if (_activePanelId == "radar")
                {
                    isTarget = (p.name == "RadarPanel");
                }
                else if (_activePanelId == "vault")
                {
                    isTarget = (p.name == "VaultPanel");
                }
                else if (_activePanelId == "aura")
                {
                    isTarget = (p.name == "AuraPanel");
                }
                else if (_activePanelId == "sanctuary")
                {
                    isTarget = (p.name == "SanctuaryPanel");
                }
                else if (_activePanelId == "settings")
                {
                    isTarget = (p.name == "SettingsPanel");
                }
                else
                {
                    // 2. Generic fallback if no explicit override
                    isTarget = p.name.ToLower().Contains(_activePanelId);
                }

                p.style.display = isTarget ? DisplayStyle.Flex : DisplayStyle.None;
                p.style.visibility = isTarget ? Visibility.Visible : Visibility.Hidden;
                p.style.opacity = isTarget ? 1f : 0f;
                p.pickingMode = isTarget ? PickingMode.Position : PickingMode.Ignore;
            }

            // UX Audit #5: Show context hints only when the panel actually becomes active
            if (_uiManager != null)
            {
                if (_activePanelId == "vault")
                {
                    _uiManager.ShowContextHint("Vault", "Your personal grimoire. Change your visage and bio here.", 5f);
                }
                else if (_activePanelId == "aura")
                {
                    _uiManager.ShowContextHint("Aura", "Configure what you seek and what you offer to the world.", 5f);
                }
            }

            UpdateNavVisuals(_activePanelId);
            Debug.Log($"<color=#FFD700><b>[PULSE]</b></color> Entering Panel: <color=#00FFFF><b>{_activePanelId.ToUpper()}</b></color>");
            OnPanelSwitched?.Invoke(_activePanelId);
            ShowPanelToast(_activePanelId.ToUpper()).Forget();
        }

        private async UniTaskVoid ShowPanelToast(string text)
        {
            if (_bottomNav == null || _bottomNav.panel == null) return;
            var toast = _bottomNav.panel.visualTree.Q<Label>("PanelTitleToast");
            if (toast == null) return;

            string localized = _localization != null ? _localization.Get($"panel_{text.ToLower()}", text).ToUpper() : text;
            toast.text = localized;
            toast.style.opacity = 1f;
            toast.style.scale = new Scale(new Vector2(1.1f, 1.1f));
            
            await UniTask.Delay(1500);
            if (toast != null) {
                toast.style.opacity = 0f;
                toast.style.scale = new Scale(new Vector2(1f, 1f));
            }
        }

        public void ReturnToPrevious()
        {
            SwitchTo(_previousPanelId);
        }

        private void UpdateNavVisuals(string activeId)
        {
            if (_bottomNav == null) return;
            foreach (var id in new[] { "feed", "chamber" }) {
                var navElem = id == "chamber" ? _bottomNav.Q("BtnOpenChamber") : _bottomNav.Q("NavFeed");
                if (navElem != null) { 
                    if (id == activeId.ToLower()) 
                    {
                        navElem.AddToClassList("nav-item--active"); 
                        navElem.AddToClassList("aura-glow--gold");
                    }
                    else 
                    {
                        navElem.RemoveFromClassList("nav-item--active"); 
                        navElem.RemoveFromClassList("aura-glow--gold");
                        navElem.RemoveFromClassList("aura-glow--pulse");
                    }
                }
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            float duration = Time.time - _swipeStartTime;
            if (duration > SwipeMaxTime) return;
            
            Vector2 delta = (Vector2)evt.position - _swipeStartPosition;
            
            // TikTok style: horizontal swipe must be dominant
            if (Mathf.Abs(delta.x) > SwipeThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.5f) 
            {
                NavigateSwipe(delta.x < 0);
            }
        }

        private void NavigateSwipe(bool next)
        {
            int cur = Array.IndexOf(_swipeOrder, _activePanelId);
            if (cur == -1) return;
            int nextIdx = next ? cur + 1 : cur - 1;
            
            if (nextIdx >= 0 && nextIdx < _swipeOrder.Length) 
            {
                _hapticService?.LightTap();
                SwitchTo(_swipeOrder[nextIdx]);
            }
        }
    }
}
