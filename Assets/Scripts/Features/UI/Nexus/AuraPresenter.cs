using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Services;
using TimeAura.Features.Data;
using TimeAura.Core.Data.SO;
using TimeAura.Features.Auth;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.UI.Nexus
{
    /// <summary>
    /// AuraPresenter - Logic for the "Mirror of Aura" ritual.
    /// Manages the selection of Aura Colors (Gifts and Seeks).
    /// "Your Aura is the spectrum of your soul's contribution."
    /// </summary>
    public class AuraPresenter
    {
        private readonly IDataService _dataService;
        private readonly AuthManager _authManager;
        private readonly IAuraOracleService _oracleService;
        private readonly AuraPillarSO[] _pillars;

        [Inject]
        public AuraPresenter(IDataService dataService, AuthManager authManager, IAuraOracleService oracleService, AuraPillarSO[] pillars)
        {
            _dataService = dataService;
            _authManager = authManager;
            _oracleService = oracleService;
            _pillars = pillars;
        }

        public AuraPillarSO[] Pillars => _pillars;

        private bool _isBusy;
        private List<string> _selectedGifts = new();
        private List<string> _selectedSeeks = new();
        private string _customNote = "";

        // Filters
        private bool _seeksPhysical = true;
        private bool _seeksIntangible = true;
        private int _minAge = 18;
        private int _maxAge = 99;
        private float _distance = 50f;
        private string _activePillarId = "";
        private string _equippedOracleId = "";

        public OracleTone OracleTone => _authManager?.CurrentProfile?.OracleTone ?? OracleTone.Business;
        
        private const int TagLimit = 3;

        public int GetTagLimit()
        {
            var profile = _authManager?.CurrentProfile;
            if (profile != null && profile.IsAscensionSubscribed)
            {
                return 7; // Ascension Subscription: 7 tags max
            }
            return TagLimit; // Base Limit: 3 tags max
        }

        public event Action OnDataChanged;
        public event Action<string> OnError;
        public event Action<List<Color>> OnAuraColorsUpdated;
        public event Action<OracleSuggestion> OnOracleSuggestion;

        public List<string> SelectedGifts => _selectedGifts;
        public List<string> SelectedSeeks => _selectedSeeks;
        public string CustomNote => _customNote;
        public float ResonanceStrength { get; private set; }

        public bool SeeksPhysical { get => _seeksPhysical; set { _seeksPhysical = value; OnDataChanged?.Invoke(); } }
        public bool SeeksIntangible { get => _seeksIntangible; set { _seeksIntangible = value; OnDataChanged?.Invoke(); } }
        public int MinAge { get => _minAge; set { _minAge = value; OnDataChanged?.Invoke(); } }
        public int MaxAge { get => _maxAge; set { _maxAge = value; OnDataChanged?.Invoke(); } }
        public float Distance { get => _distance; set { _distance = value; OnDataChanged?.Invoke(); } }

        public bool HasVisage => _authManager?.CurrentProfile?.HasVisage ?? false;
        public Color GetAuraColor() => _authManager?.CurrentProfile?.GetPrimaryAuraColor() ?? Color.gray;
        public string AuraTitle => _authManager?.CurrentProfile?.AuraTitle ?? "Initiate";
        public bool IsBusy => _isBusy;
        public string ActivePillarId { get => _activePillarId; set => _activePillarId = value; }
        public string EquippedOracleId { get => _equippedOracleId; set => _equippedOracleId = value; }

        public AuraPresenter(IDataService dataService, AuthManager authManager, IAuraOracleService oracleService)
        {
            _dataService = dataService;
            _authManager = authManager;
            _oracleService = oracleService;
        }

        public void InitializeFromProfile(UserProfile profile)
        {
            if (profile == null) return;
            
            _selectedGifts = new List<string>(profile.AuraGifts);
            _selectedSeeks = new List<string>(profile.AuraSeeks);
            _customNote = profile.CustomNote;
            _equippedOracleId = profile.EquippedOracleId ?? "";

            _seeksPhysical = profile.SeeksPhysical;
            _seeksIntangible = profile.SeeksIntangible;
            _minAge = profile.MinAgeFilter;
            _maxAge = profile.MaxAgeFilter;
            _distance = profile.DistanceFilter;
            
            OnDataChanged?.Invoke();
            NotifyAuraColorsChanged();
        }

        /// <summary>
        /// Equip an Oracle as the Master's companion for this Aura build.
        /// Persists the choice to the current UserProfile.
        /// </summary>
        public void SetEquippedOracle(Core.Data.SO.OracleSO oracle)
        {
            _equippedOracleId = oracle != null ? oracle.Id : "";
            var profile = _authManager?.CurrentProfile;
            if (profile != null)
                profile.EquippedOracleId = _equippedOracleId;
            OnDataChanged?.Invoke();
        }

        public bool ToggleGift(string tagKey) => ToggleTag(_selectedGifts, tagKey);
        public bool ToggleSeek(string tagKey) => ToggleTag(_selectedSeeks, tagKey);

        private bool ToggleTag(List<string> list, string tagKey)
        {
            if (list.Contains(tagKey))
            {
                list.Remove(tagKey);
                OnDataChanged?.Invoke();
                NotifyAuraColorsChanged();
                return true;
            }

            int currentLimit = GetTagLimit();
            if (list.Count >= currentLimit)
            {
                OnError?.Invoke($"Limit of {currentLimit} tags reached.");
                return false;
            }

            list.Add(tagKey);
            UpdateResonanceStrength();
            OnDataChanged?.Invoke();
            NotifyAuraColorsChanged();
            return true;
        }

        private void UpdateResonanceStrength()
        {
            if (_selectedGifts.Count == 0)
            {
                ResonanceStrength = 0;
                return;
            }

            float totalWeight = 0;
            foreach (var tag in _selectedGifts)
            {
                var pillar = _pillars.FirstOrDefault(p => p.tags != null && p.tags.Contains(tag));
                if (pillar != null) totalWeight += pillar.weight;
                else totalWeight += 50; // Default weight for custom/unknown tags
            }

            // Average weight normalized (assuming max weight per tag is around 100)
            ResonanceStrength = Mathf.Clamp((totalWeight / (_selectedGifts.Count * 100f)) * 100f, 10, 100);
        }

        private void NotifyAuraColorsChanged()
        {
            var activeColors = new List<Color>();
            foreach (var tag in _selectedGifts)
            {
                var pillar = _pillars.FirstOrDefault(p => p.tags != null && p.tags.Contains(tag));
                if (pillar != null && !activeColors.Contains(pillar.ThemeColor))
                {
                    activeColors.Add(pillar.ThemeColor);
                }
            }
            OnAuraColorsUpdated?.Invoke(activeColors);
        }

        public void SetBusy(bool busy)
        {
            _isBusy = busy;
            OnDataChanged?.Invoke();
        }

        public void SetCustomNote(string note)
        {
            _customNote = note;
            OnDataChanged?.Invoke();
        }

        public async UniTask TriggerOracleAnalysis(string thoughts = "")
        {
            if (_isBusy) return;
            
            _isBusy = true;
            OnDataChanged?.Invoke();

            try
            {
                OracleTone tone = _authManager?.CurrentProfile?.OracleTone ?? OracleTone.Business;
                var suggestion = await _oracleService.SuggestResonanceAsync(_selectedGifts, _selectedSeeks, thoughts, tone, _activePillarId);
                OnOracleSuggestion?.Invoke(suggestion);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"The Oracle is silent: {ex.Message}");
            }
            finally
            {
                _isBusy = false;
                OnDataChanged?.Invoke();
            }
        }

        public async UniTask<bool> ActivateAuraAsync()
        {
            var profile = _authManager.CurrentProfile;
            if (profile == null) return false;

            if (_selectedGifts.Count == 0)
            {
                OnError?.Invoke("You must offer at least one Aura color to the Nexus.");
                return false;
            }

            _isBusy = true;
            OnDataChanged?.Invoke();

            try
            {
                // 1. Update Profile local aura data
                profile.UpdateAura(_selectedGifts, _selectedSeeks, _customNote);
                
                // 2. Consult the Oracle for dynamic humorous evolutionary titles
                var verdict = await _oracleService.PredictEvolutionaryTitleAsync(profile);
                
                // 3. Apply evolutionary properties
                profile.AuraColorHex = verdict.ColorHex;
                profile.AuraTitle = verdict.Title;
                
                profile.SeeksPhysical = _seeksPhysical;
                profile.SeeksIntangible = _seeksIntangible;
                profile.MinAgeFilter = _minAge;
                profile.MaxAgeFilter = _maxAge;
                profile.DistanceFilter = _distance;

                // 3. Save to Firebase
                await _dataService.SaveUserProfileAsync(profile, default);
                
                Debug.Log($"[AuraPresenter] Oracle Decree: {verdict.Title} ({verdict.ColorHex})");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"The Oracle is silent: {ex.Message}");
                return false;
            }
            finally
            {
                _isBusy = false;
                OnDataChanged?.Invoke();
            }
        }

        public bool IsSelected(string tagKey, bool isGift)
        {
            return isGift ? _selectedGifts.Contains(tagKey) : _selectedSeeks.Contains(tagKey);
        }
    }
}
