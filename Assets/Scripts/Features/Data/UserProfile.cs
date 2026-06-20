using System;
using System.Collections.Generic;
using UnityEngine;
using TimeAura.Core.Data.SO;

namespace TimeAura.Features.Data
{
    public enum OracleScanFrequency
    {
        Disabled,
        Hourly,
        Daily
    }

    /// <summary>
    /// User Profile - The Master's digital soul.
    /// Contains identity, currency (Horas), and Aura (Tags/Colors).
    /// "In the Mirror of Aura, we see who we truly are."
    /// </summary>
    [Serializable]
    public sealed class UserProfile
    {
        // Identification
        [SerializeField] private string _userId;
        
        public void SetUserId(string id) => _userId = id;

        [SerializeField] private string _phoneNumber;
        [SerializeField] private string _username; // Social handle
        [SerializeField] private string _displayName; // Master's name

        // Game State
        [SerializeField] private long _timeBalanceMinutes; // Time Balance in Minutes (Atoms)
        [SerializeField] private long _timeEscrowMinutes;  // Time locked in active contracts
        [SerializeField] private long _fiatBalance;      // Real money balance in cents
        [SerializeField] private long _fiatEscrow;       // Real money locked in active contracts in cents
        [SerializeField] private long _wavesBalance;       // Waves balance (integer representation of Quants)
        [SerializeField] private long _wavesEscrow;        // Waves locked in active escrow
        [SerializeField] private int _status; // Level / Reputation
        [SerializeField] private bool _hasCompletedInitiation;
        [SerializeField] private bool _isAnonymous;

        // Aura (The Mirror)
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_giftTags")] private List<string> _auraGifts = new(); // My Aura Offer
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_seekTags")] private List<string> _auraSeeks = new(); // My Aura Needs
        [SerializeField] private string _customNote = "";
        [SerializeField] private string _activityStatus = "idle";
        
        // Aura Oracle (AI Generated)
        [SerializeField] private string _auraColorHex = "#FFFFFF";
        [SerializeField] private string _auraTitle = "Initiate";

        // Social Metadata (Merged from Social feature)
        [SerializeField] private string _avatarUrl;
        [SerializeField] private string _bio;
        [SerializeField] private int _followersCount;
        [SerializeField] private int _followingCount;
        [SerializeField] private bool _isVerified;
        [SerializeField] private DateTime _createdAt;
        [SerializeField] private List<string> _legacy = new();
        [SerializeField] private List<SymmetryRecord> _constellations = new();
        
        [Header("Orders & Anti-Fraud")]
        [SerializeField] private string _chronosOrderId;
        [SerializeField] private string _chronosOrderRole;
        [SerializeField] private float _darkEnergy;
        [SerializeField] private string _lastIdentityUpdate; // ISO String
        [SerializeField] private string _fcmToken; // Firebase Cloud Messaging Token
        
        // Constellation Metadata (Discovery & Social)
        [SerializeField] private int _age;
        [SerializeField] private string _gender;
        [SerializeField] private string _locationZone;
        [SerializeField] private double _latitude;
        [SerializeField] private double _longitude;
        [SerializeField] private string _primaryPillar;
        [SerializeField] private List<string> _chronosHistory = new();
        [SerializeField] private OracleScanFrequency _scanFrequency = OracleScanFrequency.Daily;

        // Service Categories & Filters
        [SerializeField] private bool _offersPhysical = true;
        [SerializeField] private bool _offersIntangible = true;
        [SerializeField] private bool _seeksPhysical = true;
        [SerializeField] private bool _seeksIntangible = true;
        [SerializeField] private int _minAgeFilter = 18;
        [SerializeField] private int _maxAgeFilter = 99;
        [SerializeField] private float _distanceFilter = 50f; // km
        [SerializeField] private byte[] _visageData;
        [SerializeField] private OracleTone _oracleTone = OracleTone.Business;
        [SerializeField] private bool _isAscensionSubscribed = false;
        [SerializeField] private string _ascensionExpiresAt; // ISO String
        [SerializeField] private int _chronoCreditLimitMinutes = -240; // Default newbies get -240 minutes (-4 Horas) credit limit

        [Header("Monetization & Limits")]
        [SerializeField] private int _contractsConcludedInPeriod = 0;
        [SerializeField] private string _contractPeriodStartDate; // ISO String
        [SerializeField] private int _extraContracts = 0;

        // Oracle Equipment (The Master's Companion)
        [SerializeField] private string _equippedOracleId = "";     // Persisted to Firebase
        [NonSerialized] private string _activeSessionPrompt = "";   // Runtime only — synthesized by OraclePromptFactory
        [SerializeField] private List<string> _customOraclesJson = new(); // Persisted list of custom JSONs

        public UserProfile(string userId, string phoneNumber, string displayName, float initialHoras, int initialStatus)
        {
            this._userId = userId;
            this._phoneNumber = phoneNumber;
            this._displayName = displayName;
            this._timeBalanceMinutes = (int)(initialHoras * 60f);
            this._status = initialStatus;
            this._createdAt = DateTime.UtcNow;
            this._hasCompletedInitiation = false;
        }

        #region Properties
        public bool IsAscensionSubscribed { get => _isAscensionSubscribed; set => _isAscensionSubscribed = value; }
        public DateTime? AscensionExpiresAt => string.IsNullOrEmpty(_ascensionExpiresAt) ? null : DateTime.Parse(_ascensionExpiresAt);
        public void SetAscensionExpiresAt(DateTime? expiresAt) => _ascensionExpiresAt = expiresAt?.ToString("O") ?? "";
        
        public int ChronoCreditLimitMinutes { get => _chronoCreditLimitMinutes; set => _chronoCreditLimitMinutes = value; }

        public int ContractsConcludedInPeriod { get => _contractsConcludedInPeriod; set => _contractsConcludedInPeriod = value; }
        public DateTime? ContractPeriodStartDate => string.IsNullOrEmpty(_contractPeriodStartDate) ? null : DateTime.Parse(_contractPeriodStartDate);
        public int ExtraContracts { get => _extraContracts; set => _extraContracts = value; }

        public string UserId => _userId;
        public string PhoneNumber => _phoneNumber;
        
        // Social compatibility
        public string userId => _userId; 
        public string username => _username;
        public string displayName => _displayName;
        public string avatarUrl => _avatarUrl;
        public string bio => _bio;

        public string Username { get => _username; set => _username = value; }
        public string DisplayName { get => _displayName; set => _displayName = value; }
        public string Nickname { get => _displayName; set => _displayName = value; }
        
        public string FcmToken { get => _fcmToken; set => _fcmToken = value; }
        
        public long TimeBalanceMinutes => _timeBalanceMinutes;
        public long TimeEscrowMinutes => _timeEscrowMinutes;
        public float Horas => _timeBalanceMinutes / 60f;
        
        public long FiatBalance => _fiatBalance;
        public long FiatEscrow => _fiatEscrow;

        public long WavesBalance { get => _wavesBalance; set => _wavesBalance = value; }
        public long WavesEscrow { get => _wavesEscrow; set => _wavesEscrow = value; }
        public float Quants => _wavesBalance / 100f;
        
        public int Status => _status;
        public bool HasCompletedInitiation => _hasCompletedInitiation;
        public bool IsAnonymous { get => _isAnonymous; set => _isAnonymous = value; }

        [SerializeField] private bool _isAiMaster;
        public bool IsAiMaster { get => _isAiMaster; set => _isAiMaster = value; }

        public List<string> AuraGifts => _auraGifts;
        public List<string> AuraSeeks => _auraSeeks;
        public string CustomNote => _customNote;
        public string ActivityStatus => _activityStatus;
        
        public string AuraColorHex { get => _auraColorHex; set => _auraColorHex = value; }
        public string AuraTitle { get => _auraTitle; set => _auraTitle = value; }

        public string AvatarUrl { get => _avatarUrl; set => _avatarUrl = value; }
        public string Bio { get => _bio; set => _bio = value; }
        public bool IsVerified => _isVerified;
        public bool HasVisage => !string.IsNullOrEmpty(_avatarUrl);
        public List<string> Legacy => _legacy ??= new List<string>();
        public List<SymmetryRecord> Constellations => _constellations ??= new List<SymmetryRecord>();
        
        public string ChronosOrderId { get => _chronosOrderId; set => _chronosOrderId = value; }
        public string ChronosOrderRole { get => _chronosOrderRole; set => _chronosOrderRole = value; }
        public float DarkEnergy { get => _darkEnergy; set => _darkEnergy = value; }
        
        public int Age { get => _age; set => _age = value; }
        public string Gender { get => _gender; set => _gender = value; }
        public string LocationZone { get => _locationZone; set => _locationZone = value; }
        public double Latitude { get => _latitude; set => _latitude = value; }
        public double Longitude { get => _longitude; set => _longitude = value; }
        public string PrimaryPillar { get => _primaryPillar; set => _primaryPillar = value; }
        [SerializeField] private string _primarySeek;
        public string PrimarySeek { get => _primarySeek; set => _primarySeek = value; }
        public List<string> ChronosHistory => _chronosHistory ??= new List<string>();
        public OracleScanFrequency ScanFrequency { get => _scanFrequency; set => _scanFrequency = value; }
        public DateTime? LastIdentityUpdate => string.IsNullOrEmpty(_lastIdentityUpdate) ? null : DateTime.Parse(_lastIdentityUpdate);
        
        public long UpdatedAt => LastIdentityUpdate.HasValue 
            ? ((DateTimeOffset)LastIdentityUpdate.Value).ToUnixTimeSeconds() 
            : ((DateTimeOffset)_createdAt).ToUnixTimeSeconds();

        public bool CanUpdateIdentity()
        {
            if (string.IsNullOrEmpty(_lastIdentityUpdate)) return true;
            if (DateTime.TryParse(_lastIdentityUpdate, out var last))
            {
                return (DateTime.UtcNow - last).TotalDays >= 7;
            }
            return true;
        }

        public void RegisterIdentityUpdate()
        {
            _lastIdentityUpdate = DateTime.UtcNow.ToString("O");
        }

        public bool OffersPhysical { get => _offersPhysical; set => _offersPhysical = value; }
        public bool OffersIntangible { get => _offersIntangible; set => _offersIntangible = value; }
        public bool SeeksPhysical { get => _seeksPhysical; set => _seeksPhysical = value; }
        public bool SeeksIntangible { get => _seeksIntangible; set => _seeksIntangible = value; }
        public int MinAgeFilter { get => _minAgeFilter; set => _minAgeFilter = value; }
        public int MaxAgeFilter { get => _maxAgeFilter; set => _maxAgeFilter = value; }
        public float DistanceFilter { get => _distanceFilter; set => _distanceFilter = value; }
        public byte[] VisageData { get => _visageData; set => _visageData = value; }
        public OracleTone OracleTone { get => _oracleTone; set => _oracleTone = value; }


        // Oracle Equipment
        public string EquippedOracleId { get => _equippedOracleId; set => _equippedOracleId = value; }
        /// <summary>Runtime-only synthesized system prompt. Set by OraclePromptFactory after Oracle selection.</summary>
        public string ActiveSessionPrompt { get => _activeSessionPrompt; set => _activeSessionPrompt = value; }
        public List<string> CustomOraclesJson => _customOraclesJson ??= new List<string>();
        
        public bool IsValid => !string.IsNullOrEmpty(userId);

        public Color GetPrimaryAuraColor()
        {
            if (ColorUtility.TryParseHtmlString(_auraColorHex, out Color aiColor))
            {
                return aiColor;
            }

            if (_auraGifts != null && _auraGifts.Count > 0)
            {
                return new Color(0.83f, 0.69f, 0.22f); // Fallback Gold
            }
            return new Color(0.5f, 0.5f, 0.5f); // Gray/Neutral
        }

        // Aspects (Initiation compatibility)
        [SerializeField] private List<AspectData> aspectList = new();
        private Dictionary<Core.AspectType, int> _aspectsCache;

        public Dictionary<Core.AspectType, int> Aspects
        {
            get
            {
                if (_aspectsCache == null)
                {
                    _aspectsCache = new Dictionary<Core.AspectType, int>();
                    foreach (var ad in aspectList) _aspectsCache[ad.aspect] = ad.level;
                }
                return _aspectsCache;
            }
        }

        [Serializable]
        private struct AspectData
        {
            public Core.AspectType aspect;
            public int level;
        }

        [Serializable]
        public class SymmetryRecord
        {
            public string PartnerId;
            public string PartnerName;
            public string Category;
            public long minutesExchanged;
            public long Timestamp;
            // Coordinate for the star map UI [0..1] range
            public float MapX;
            public float MapY;
        }

        #endregion

        public static UserProfile CreateNew(string userId, string phoneNumber, float initialVectors)
        {
            return new UserProfile(userId, phoneNumber, "", initialVectors, 0);
        }

        public void CompleteInitiation() => _hasCompletedInitiation = true;
        
        public void SetNickname(string name) => _displayName = name;
        
        public void SetAspect(Core.AspectType aspect, int level)
        {
            aspectList.RemoveAll(a => a.aspect == aspect);
            aspectList.Add(new AspectData { aspect = aspect, level = level });
            _aspectsCache = null; // Invalidate cache
        }

        public void AddMinutes(long amount) => _timeBalanceMinutes += amount;
        public void SpendMinutes(long amount) => _timeBalanceMinutes -= amount;
        public void AddHoras(float amount) => _timeBalanceMinutes += (long)(amount * 60f);
        public void SpendHoras(float amount) => _timeBalanceMinutes -= (long)(amount * 60f);
        
        public void AddFiat(long amount) => _fiatBalance += amount;
        public void SpendFiat(long amount) => _fiatBalance -= amount;

        public void AddWaves(long amount) => _wavesBalance += amount;
        public void SpendWaves(long amount) => _wavesBalance -= amount;
        public void AddQuants(float amount) => _wavesBalance += (long)(amount * 100f);
        public void SpendQuants(float amount) => _wavesBalance -= (long)(amount * 100f);
        
        // Escrow
        public void LockMinutesInEscrow(long amount) { _timeBalanceMinutes -= amount; _timeEscrowMinutes += amount; }
        public void ReleaseMinutesEscrow(long amount) { _timeEscrowMinutes -= amount; }
        public void RevertMinutesEscrow(long amount) { _timeEscrowMinutes -= amount; _timeBalanceMinutes += amount; }

        public void LockWavesInEscrow(long amount) { _wavesBalance -= amount; _wavesEscrow += amount; }
        public void ReleaseWavesEscrow(long amount) { _wavesEscrow -= amount; }
        public void RevertWavesEscrow(long amount) { _wavesEscrow -= amount; _wavesBalance += amount; }

        public void LockFiatInEscrow(long amount) { _fiatBalance -= amount; _fiatEscrow += amount; }
        public void ReleaseFiatEscrow(long amount) { _fiatEscrow -= amount; }
        public void RevertFiatEscrow(long amount) { _fiatEscrow -= amount; _fiatBalance += amount; }
        
        public void IncreaseStatus(int amount) => _status += amount;
        
        public void AddLegacyEntry(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _legacy ??= new List<string>();
            _legacy.Insert(0, text.Trim());
            if (_legacy.Count > 50) _legacy.RemoveAt(_legacy.Count - 1);
        }

        public void AddConstellationStar(SymmetryRecord record)
        {
            if (record == null) return;
            _constellations ??= new List<SymmetryRecord>();
            _constellations.Add(record);
        }

        public void UpdateAura(List<string> gifts, List<string> seeks, string note)
        {
            _auraGifts = gifts ?? new List<string>();
            _auraSeeks = seeks ?? new List<string>();
            _customNote = note ?? "";
        }

        public void SetActivityStatus(string status) => _activityStatus = status;

        public void RecordContractStarted(int periodDays)
        {
            if (!IsAscensionSubscribed && _extraContracts > 0) 
            {
                _extraContracts--; // Consume the ad bonus first
                return;
            }

            if (ContractPeriodStartDate == null || (DateTime.UtcNow - ContractPeriodStartDate.Value).TotalDays >= periodDays)
            {
                _contractPeriodStartDate = DateTime.UtcNow.ToString("O");
                _contractsConcludedInPeriod = 0;
            }
            _contractsConcludedInPeriod++;
        }

        public bool CanStartFreeContract(int limit, int periodDays)
        {
            if (IsAscensionSubscribed) return true;
            if (_extraContracts > 0) return true; // Ad bonus slot available!
            
            if (ContractPeriodStartDate == null || (DateTime.UtcNow - ContractPeriodStartDate.Value).TotalDays >= periodDays)
            {
                return true;
            }
            return _contractsConcludedInPeriod < limit;
        }
    }
}
