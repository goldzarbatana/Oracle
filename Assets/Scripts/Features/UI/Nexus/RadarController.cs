using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Localization;
using TimeAura.Features.Localization;
using TimeAura.Core.Services;
using TimeAura.Features.Data;
using TimeAura.Features.Auth;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using TimeAura.Core.Data.SO;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// Handles the Radar UI, search logic, and pulse animations.
    /// Extracted from the NexusController monolith.
    /// </summary>
    public class RadarController : MonoBehaviour
    {
        [Inject] private LocalizationManager _localization;
        [Inject] private AuthManager _authManager;
        [Inject] private AudioService _audioService;
        [Inject] private Matching.MatchmakingManager _matchmakingManager;
        [Inject] private AuraPresenter _auraPresenter;

        [Header("🎨 Radar Assets")]
        [SerializeField] private Sprite _scanLineSprite;
        [SerializeField] private Sprite _compassNeedleSprite;
        [SerializeField] private Sprite _resonancePointSprite;

        public void SetActive(bool active) => enabled = active;

        private VisualElement _panelRadar;
        private VisualElement _mistContainer;
        private VisualElement _resonancePointsContainer;
        private Label _lblStatus;
        private Button _btnToggleSearch;


        private bool _isSearching;
        private Matching.MatchFilterMode _currentFilterMode = Matching.MatchFilterMode.Resonance;
        private List<VisualElement> _mistParticles = new();
        private System.Threading.CancellationTokenSource _cts = new();

        public event Action<List<UserProfile>> OnSearchCompleted;

        public void Initialize(VisualElement root)
        {
            _panelRadar = root.Q("RadarPopup");
            _mistContainer = _panelRadar?.Q("MistContainer");
            _resonancePointsContainer = _panelRadar?.Q("ResonancePoints");
            
            _lblStatus = _panelRadar?.Q<Label>("LblStatus");
            _btnToggleSearch = _panelRadar?.Q<Button>("BtnToggleSearch");

            if (_panelRadar == null) 
            {
                Debug.LogError($"[Radar] RadarPopup NOT FOUND in UXML root: {root.name}!");
            }

            if (_btnToggleSearch != null)
                _btnToggleSearch.clicked += ToggleSearch;

            ApplyVisualAssets();
            UpdateLocalization();
        }


        private void ApplyVisualAssets()
        {

            if (_resonancePointSprite != null)
            {
                // This will be used in ShowResonancePoints
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Allow live texture updates in the Unity Editor
            ApplyVisualAssets();
#endif
        }

        public void UpdateLocalization()
        {
            if (_localization == null) return;
            var tone = _localization.CurrentTone;
            
            if (_lblStatus != null) 
                _lblStatus.text = _localization.GetPersonaString(AuraTerms.LBL_RADAR_READY, tone, "READY FOR SYMMETRY").ToUpper();
                
            if (_btnToggleSearch != null) 
            {
                _btnToggleSearch.text = _isSearching 
                    ? _localization.GetPersonaString(AuraTerms.RADAR_STOP, tone, "STOP SCAN").ToUpper()
                    : _localization.GetPersonaString(AuraTerms.BTN_START_PULSE, tone, "RESONATE").ToUpper();
            }
        }

        private void ToggleSearch() 
        { 
            if (_isSearching) StopSearch(); 
            else StartSearch().Forget(); 
        }

        private async UniTaskVoid StartSearch()
        {
            if (_isSearching) return;
            var currentUser = _authManager.CurrentProfile;
            if (currentUser == null) 
            {
                Debug.LogWarning("[Radar] Cannot search: No current user profile!");
                return;
            }

            _isSearching = true;

            if (_panelRadar != null)
            {
                _panelRadar.style.display = DisplayStyle.Flex;
                _panelRadar.style.opacity = 1f;
                _panelRadar.RemoveFromClassList("modal--hidden");
                _panelRadar.BringToFront();
                Debug.Log("[Popup] Opened: RadarPopup (Оракул шукає резонанс)");
            }

            if (_btnToggleSearch != null) {
                _btnToggleSearch.text = _localization.GetPersonaString(AuraTerms.RADAR_STOP, _localization.CurrentTone, "STOP SCAN").ToUpper();
                _btnToggleSearch.AddToClassList("btn-toggle-search--active");
            }

            SetStatus(_localization.GetPersonaString(AuraTerms.RADAR_INIT, _localization.CurrentTone, "INITIATING PULSE..."));
            StartPulseAnimation().Forget();

            await UniTask.Delay(1000); 
            if (!_isSearching) return;

            SetStatus(_localization.GetPersonaString(AuraTerms.RADAR_SEARCHING, _localization.CurrentTone, "SEARCHING FOR SYMMETRY..."));
            
            // 🔮 Mystical Delay: Let the orb swirl and build tension so the user can enjoy the VFX
            await UniTask.Delay(1500, cancellationToken: _cts.Token);
            if (!_isSearching) return;

            // Perform real matchmaking
            var matches = await _matchmakingManager.FindMatchesAsync(currentUser, _currentFilterMode, _cts.Token);
            
            if (!_isSearching) return;

            // FALLBACK TO MOCK DATA IF EMPTY
            if (matches == null || matches.Count == 0)
            {
                Debug.Log("[Radar] 🌌 Real matchmaking empty. Summoning Mock Adepts...");
                matches = GetMockProfiles(currentUser);
            }

            if (matches != null && matches.Count > 0)
            {
                SetStatus(_localization.GetPersonaString(AuraTerms.RADAR_LOCATED, _localization.CurrentTone, "SYMMETRY DETECTED!"));
                ShowResonancePoints(matches);
                // Wait longer to let the user admire the constellation of points before transitioning
                await UniTask.Delay(1000);
                
                StopSearch();
                OnSearchCompleted?.Invoke(matches);
            }
            else
            {
                SetStatus(_localization.GetPersonaString(AuraTerms.RADAR_EMPTY, _localization.CurrentTone, "NO RESONANCE DETECTED..."));
                await UniTask.Delay(1000);
                StopSearch();
            }
        }

        private void ShowResonancePoints(List<UserProfile> matches)
        {
            if (_resonancePointsContainer == null) return;
            _resonancePointsContainer.Clear();
            
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var point = new VisualElement();
                point.AddToClassList("resonance-point");
                
                // If it's an AI Master, apply the new universal glow
                if (match.UserId.StartsWith("AI_MASTER_"))
                {
                    point.AddToClassList("aura-glow--gold");
                    Color c = Color.yellow;
                    ColorUtility.TryParseHtmlString("#FFD700", out c);
                    point.style.backgroundColor = c;
                }
                
                // Position INSIDE the crystal ball (Visions)
                float angle = UnityEngine.Random.Range(0, 360) * Mathf.Deg2Rad;
                float dist = UnityEngine.Random.Range(5, 35); // 50 is the edge of the orb, so 5-35 is safely inside
                point.style.left = Length.Percent(50 + Mathf.Cos(angle) * dist);
                point.style.top = Length.Percent(50 + Mathf.Sin(angle) * dist);

                if (_resonancePointSprite != null)
                    point.style.backgroundImage = new StyleBackground(_resonancePointSprite);

                _resonancePointsContainer.Add(point);
                
                // Animate in with delay
                AnimatePointIn(point, i * 100).Forget();
            }
        }

        private async UniTaskVoid AnimatePointIn(VisualElement point, int delayMs)
        {
            await UniTask.Delay(delayMs);
            point.AddToClassList("resonance-point--active");
        }

        private List<UserProfile> GetMockProfiles(UserProfile currentUser)
        {
            var targetPillar = currentUser.PrimarySeek ?? "chronos";
            var list = new List<UserProfile>();
            list.AddRange(TimeAura.Features.Data.AIMasterFactory.GetAIMasters());
            list.Add(new UserProfile("mock_lyra", "", "Lyra", 100, 5) { PrimaryPillar = targetPillar, Gender = "female", LocationZone = "Nexus Core" });
            list.Add(new UserProfile("mock_kaelen", "", "Kaelen", 50, 2) { PrimaryPillar = "harmony", Gender = "male", LocationZone = "Harmony Gardens" });
            return list;
        }

        public void StopSearch()
        {
            _isSearching = false;
            var tone = _localization != null ? _localization.CurrentTone : OracleTone.Business;
            if (_btnToggleSearch != null) {
                _btnToggleSearch.text = _localization.GetPersonaString(AuraTerms.BTN_START_PULSE, tone, "PEER INTO THE ORB").ToUpper();
                _btnToggleSearch.RemoveFromClassList("btn-toggle-search--active");
            }
            SetStatus(_localization.GetPersonaString(AuraTerms.RADAR_READY, tone, "READY TO SCRY"));

            if (_panelRadar != null)
            {
                _panelRadar.style.display = DisplayStyle.None;
                _panelRadar.style.opacity = 0f;
                _panelRadar.AddToClassList("modal--hidden");
            }
        }

        public void StartRadarSearch()
        {
            if (!_isSearching)
            {
                StartSearch().Forget();
            }
        }

        private void SetStatus(string msg) 
        { 
            if (_lblStatus != null) _lblStatus.text = msg; 
        }

        public void Resume() 
        {
            // Reset CTS for a new lifecycle if needed, but StartSearch handles it
        }

        public void Pause() 
        {
            _isSearching = false;
            _cts?.Cancel();
            _cts = new System.Threading.CancellationTokenSource();
            
            if (_mistContainer != null) _mistContainer.Clear();
            _mistParticles.Clear();
            
            Debug.Log("[Radar] 🔇 Resonance suspended. Clearing visual echoes.");
        }

        private async UniTaskVoid StartPulseAnimation()
        {
            if (_resonancePointsContainer != null) _resonancePointsContainer.Clear();
            
            float particleSpawnTimer = 0;
            float rotationAngle = 0;
            
            while (_isSearching)
            {
                float deltaTime = Time.deltaTime;
                particleSpawnTimer += deltaTime;
                rotationAngle += 120f * deltaTime; // Rotate 120 degrees per second for tornado effect

                if (_mistContainer != null)
                {
                    _mistContainer.style.rotate = new Rotate(new Angle(rotationAngle, AngleUnit.Degree));
                }

                if (particleSpawnTimer > 0.10f) // Spawn faster for thicker tornado
                {
                    particleSpawnTimer = 0;
                    SpawnMistParticle();
                }

                await UniTask.Yield();
            }
        }

        private void SpawnMistParticle()
        {
            if (_mistContainer == null) return;
            if (_mistParticles.Count > 30) return; // Cap mist

            var particle = new VisualElement();
            particle.AddToClassList("orb-mist-particle");
            
            // Random start near bottom/center
            float startX = UnityEngine.Random.Range(40f, 60f);
            float startY = UnityEngine.Random.Range(70f, 95f);
            
            // Random size (Much larger for thick mist)
            float size = UnityEngine.Random.Range(100f, 250f);
            particle.style.width = size;
            particle.style.height = size;
            
            particle.style.left = Length.Percent(startX);
            particle.style.top = Length.Percent(startY);
            particle.style.opacity = 0f;
            particle.style.scale = new Scale(new Vector2(0.3f, 0.3f));
            
            _mistContainer.Add(particle);
            _mistParticles.Add(particle);
            
            AnimateMistParticle(particle).Forget();
        }

        private async UniTaskVoid AnimateMistParticle(VisualElement particle)
        {
            float duration = UnityEngine.Random.Range(2.0f, 3.5f);
            float endY = UnityEngine.Random.Range(-150f, -300f); // Fly much higher
            float endX = UnityEngine.Random.Range(-50f, 50f);    // Wider spread
            
            particle.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) });
            particle.style.transitionTimingFunction = new StyleList<EasingFunction>(new List<EasingFunction> { new EasingFunction(EasingMode.EaseInOutSine) });
            
            await UniTask.Delay(50);
            if (particle == null || particle.parent == null) return;

            particle.style.translate = new Translate(endX, endY, 0);
            particle.style.opacity = UnityEngine.Random.Range(0.2f, 0.5f);
            particle.style.scale = new Scale(new Vector2(1.5f, 1.5f));

            await UniTask.Delay((int)(duration * 500)); // Halfway fade out
            if (particle == null || particle.parent == null) return;
            
            particle.style.opacity = 0f;
            
            await UniTask.Delay((int)(duration * 500));
            if (particle != null && particle.parent != null)
            {
                particle.RemoveFromHierarchy();
                _mistParticles.Remove(particle);
            }
        }
    }
}
