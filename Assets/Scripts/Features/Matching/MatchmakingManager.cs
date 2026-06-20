using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Economy;
using TimeAura.Features.Localization;
using TimeAura.Core.Data.SO;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.Matching
{
    /// <summary>
    /// MatchmakingManager - Handles Convergence (finding matching Masters for Harmony cycles)
    /// and dynamically weaves AI Masters into the Nexus Radar.
    /// </summary>
    public enum MatchFilterMode
    {
        Resonance,
        Nearby,
        Oracles
    }

    public sealed class MatchmakingManager : IManager
    {
        public bool IsInitialized { get; private set; }
        
        private readonly IDataService dataService;
        private readonly AppConfig appConfig;
        private readonly AuthManager authManager;
        private readonly HorasEconomyService economyService;
        private readonly LocalizationManager localization;

        private readonly List<OraclePersonaSO> _personas = new();

        [Inject]
        public MatchmakingManager(
            IDataService dataService, 
            AppConfig appConfig, 
            AuthManager authManager, 
            HorasEconomyService economyService,
            LocalizationManager localization)
        {
            this.dataService = dataService;
            this.appConfig = appConfig;
            this.authManager = authManager;
            this.economyService = economyService;
            this.localization = localization;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (dataService == null) throw new System.InvalidOperationException("IDataService not injected into MatchmakingManager.");
            if (appConfig == null) throw new System.InvalidOperationException("AppConfig not injected into MatchmakingManager.");
            if (authManager == null) throw new System.InvalidOperationException("AuthManager not injected into MatchmakingManager.");
            if (economyService == null) throw new System.InvalidOperationException("HorasEconomyService not injected into MatchmakingManager.");
            if (localization == null) throw new System.InvalidOperationException("LocalizationManager not injected into MatchmakingManager.");

            // 1. Dynamic loading of Oracle Personas from Resources
            _personas.Clear();
            var loadedPersonas = Resources.LoadAll<OraclePersonaSO>("Settings/Personas");
            if (loadedPersonas != null && loadedPersonas.Length > 0)
            {
                _personas.AddRange(loadedPersonas);
                Debug.Log($"[MatchmakingManager] 👁️ Loaded {_personas.Count} Oracle Personas dynamically from Resources.");
            }
            else
            {
                Debug.LogWarning("[MatchmakingManager] ⚠️ No OraclePersonaSO assets found in 'Settings/Personas'. AI Master factory will fall back.");
            }

            await UniTask.Yield(cancellationToken);
            IsInitialized = true;
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        /// <summary>
        /// Scries the Nexus for visions and injects matching AI Masters into the results.
        /// </summary>
        public async UniTask<List<UserProfile>> FindMatchesAsync(UserProfile currentUser, MatchFilterMode mode, CancellationToken cancellationToken)
        {
            Debug.Log($"[MatchmakingManager] 🌌 Scrying the Nexus in {mode} mode for {currentUser.Nickname}...");
            
            var allProfiles = await dataService.GetAllProfilesAsync(cancellationToken);
            var matches = new List<UserProfile>();
            
            // Weave AI Masters into the resonance grid first
            var aiMasters = GetMatchingAIMasters(currentUser);
            if (aiMasters.Count > 0)
            {
                matches.AddRange(aiMasters);
            }

            foreach (var profile in allProfiles)
            {
                if (profile.UserId == currentUser.UserId) continue;
                matches.Add(profile);
            }

            // Apply filters based on mode
            if (mode == MatchFilterMode.Oracles)
            {
                // Hard filter for Oracles
                matches = matches.Where(p => p.UserId.StartsWith("AI_MASTER_")).ToList();
            }
            else if (mode == MatchFilterMode.Resonance)
            {
                // Sort by tags overlap (AuraGifts intersecting with currentUser.AuraSeeks)
                matches = matches.OrderByDescending(p => 
                {
                    if (currentUser.AuraSeeks == null || p.AuraGifts == null) return 0;
                    return p.AuraGifts.Intersect(currentUser.AuraSeeks).Count();
                }).ToList();
            }
            else if (mode == MatchFilterMode.Nearby)
            {
                // Simplistic distance sort placeholder (random for now as coords aren't fully integrated)
                matches = matches.OrderBy(x => UnityEngine.Random.value).ToList();
            }

            Debug.Log($"[MatchmakingManager] Total visions located: {matches.Count}.");
            return matches;
        }

        public UniTask<TimeAura.Features.Harmony.HarmonySession> StartHarmonyAsync(UserProfile user1, UserProfile user2, CancellationToken cancellationToken)
        {
            var session = new TimeAura.Features.Harmony.HarmonySession(user1.UserId, user2.UserId, 0);
            session.status = TimeAura.Features.Harmony.HarmonyStatus.PendingMatch;
            
            EventBus.Publish(new HarmonyStartedEvent(session));

            Debug.Log($"[MatchmakingManager] Harmony initiated: {user1.Nickname} <-> {user2.Nickname}. Awaiting Mutual Resonance.");

            return UniTask.FromResult(session);
        }

        /// <summary>
        /// Accepts and seals the Symmetry into a Harmony cycle.
        /// Handles automated Escrow and instant completion for AI Masters.
        /// </summary>
        public async UniTask<bool> AcceptSymmetryAsync(UserProfile targetUser, CancellationToken cancellationToken)
        {
            var currentUser = authManager.CurrentProfile;
            if (currentUser == null)
            {
                Debug.LogError("[MatchmakingManager] ❌ Cannot accept Symmetry: Current user profile is not available!");
                return false;
            }

            if (appConfig.RequireVisageForHarmony && !targetUser.HasVisage)
            {
                Debug.LogWarning("[MatchmakingManager] 🚫 Symmetry block: Visage required.");
                return false;
            }

            // Check Free Contracts Limit
            if (!currentUser.CanStartFreeContract(appConfig.FreeContractsLimit, appConfig.FreeContractsPeriodDays))
            {
                Debug.Log($"[MatchmakingManager] Free contract limit reached ({appConfig.FreeContractsLimit} per {appConfig.FreeContractsPeriodDays} days). Prompting Rewarded Ad...");
                
#if UNIVERSAL_MONETIZATION_PRESENT || true
                // Using reflection or direct access if UniversalMonetization is present. We try direct access first.
                // Assuming UniversalMonetization assembly is referenced or accessible.
                try 
                {
                    if (UniversalMonetization.AdManager.Instance != null && UniversalMonetization.AdManager.Instance.IsInitialized)
                    {
                        var adResult = await UniversalMonetization.AdManager.Instance.ShowRewardedAdAsync();
                        bool adWatched = adResult.IsSuccess;
                        if (!adWatched)
                        {
                            Debug.LogWarning("[MatchmakingManager] 🚫 Ad not watched or failed. Cannot start contract.");
                            return false;
                        }
                        Debug.Log("[MatchmakingManager] 🎥 Ad watched successfully. Proceeding with contract.");
                    }
                    else
                    {
                        Debug.LogWarning("[MatchmakingManager] AdManager not initialized. Allowing contract as fallback.");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[MatchmakingManager] Could not access UniversalMonetization.AdManager: {e.Message}");
                }
#endif
            }

            // AI Master Check
            if (targetUser.UserId.StartsWith("AI_MASTER_"))
            {
                // AI Harmony transaction
                string sessionId = Guid.NewGuid().ToString("N");
                int amount = (int)appConfig.TransformationMinHoras * 60; // Convert Horas to Minutes

                Debug.Log($"[MatchmakingManager] 🤖 Initiating AI Master Escrow. Locking {amount} Minutes for {currentUser.Nickname}...");
                bool locked = await economyService.LockFundsAsync(currentUser, ContractRealm.Ether, amount, 0L, sessionId);

                if (!locked)
                {
                    Debug.LogWarning($"[MatchmakingManager] ❌ AI Escrow failed: {currentUser.Nickname} has insufficient Minutes ({currentUser.TimeBalanceMinutes} < {amount}).");
                    return false;
                }

                // Escrow release and session completion now handled inside Harmony Workspace!
                var session = new TimeAura.Features.Harmony.HarmonySession(currentUser.UserId, targetUser.UserId, amount);
                session.sessionId = sessionId;
                session.lockedMinutes = amount;
                session.realm = ContractRealm.Ether;
                session.status = TimeAura.Features.Harmony.HarmonyStatus.ActiveChannel;

                EventBus.Publish(new HarmonyStartedEvent(session));
                Debug.Log($"[MatchmakingManager] 🌟 AI Harmony cycle started. Escrow locked, entering Workspace...");
                
                currentUser.RecordContractStarted(appConfig.FreeContractsPeriodDays);
                await dataService.SaveUserProfileAsync(currentUser, cancellationToken);
                return true;
            }

            // Real User Flow
            targetUser.SetActivityStatus("harmony_active");
            await dataService.SaveUserProfileAsync(targetUser, cancellationToken);

            currentUser.RecordContractStarted(appConfig.FreeContractsPeriodDays);
            await dataService.SaveUserProfileAsync(currentUser, cancellationToken);

            Debug.Log($"[MatchmakingManager] ✅ Symmetry accepted by {targetUser.Nickname}. Harmony channel opening...");
            return true;
        }

        /// <summary>
        /// Declines the Symmetry and returns to searching.
        /// </summary>
        public async UniTask DeclineSymmetryAsync(UserProfile user, CancellationToken cancellationToken)
        {
            user.SetActivityStatus("idle");
            await dataService.SaveUserProfileAsync(user, cancellationToken);

            Debug.Log($"[MatchmakingManager] 🚫 Symmetry declined by user. Returning to the Void.");
        }

        #region AI Master Factory

        /// <summary>
        /// Dynamic generator of AI Masters matching the seeker's needs.
        /// </summary>
        private List<UserProfile> GetMatchingAIMasters(UserProfile currentUser)
        {
            var matchedAIMasters = new List<UserProfile>();
            if (currentUser == null) return matchedAIMasters;

            string searchString = ((currentUser.CustomNote ?? "") + " " + string.Join(" ", currentUser.AuraSeeks)).ToLower();

            // Keyword analysis
            bool matchesTech = searchString.Contains("код") || searchString.Contains("скрипт") || searchString.Contains("unity") || 
                               searchString.Contains("code") || searchString.Contains("script") || searchString.Contains("program") || 
                               searchString.Contains("c#");

            bool matchesBiz = searchString.Contains("aso") || searchString.Contains("marketing") || searchString.Contains("маркетинг") || 
                              searchString.Contains("бізнес") || searchString.Contains("реклама") || searchString.Contains("money") || 
                              searchString.Contains("growth") || searchString.Contains("business");

            bool matchesMystic = searchString.Contains("містик") || searchString.Contains("аура") || searchString.Contains("душа") || 
                                 searchString.Contains("mystic") || searchString.Contains("aura") || searchString.Contains("soul") || 
                                 searchString.Contains("prophecy") || searchString.Contains("пророцтво");

            bool matchesCasual = searchString.Contains("переклад") || searchString.Contains("текст") || searchString.Contains("дизайн") || 
                                 searchString.Contains("text") || searchString.Contains("design") || searchString.Contains("translation") ||
                                 searchString.Contains("спілкування") || searchString.Contains("companion") || searchString.Contains("chat");

            // If no explicit matches exist, default to Mystic to ensure at least one AI companion is present on the radar
            if (!matchesTech && !matchesBiz && !matchesMystic && !matchesCasual)
            {
                matchesMystic = true;
            }

            // Create matching AI profiles using loaded personas
            foreach (var persona in _personas)
            {
                if (persona == null) continue;

                if (persona.Tone == OracleTone.Tech && matchesTech)
                {
                    matchedAIMasters.Add(GenerateAIMaster(persona, currentUser.AuraSeeks));
                }
                else if (persona.Tone == OracleTone.Business && matchesBiz)
                {
                    matchedAIMasters.Add(GenerateAIMaster(persona, currentUser.AuraSeeks));
                }
                else if (persona.Tone == OracleTone.Mystic && matchesMystic)
                {
                    matchedAIMasters.Add(GenerateAIMaster(persona, currentUser.AuraSeeks));
                }
                else if (persona.Tone == OracleTone.Casual && matchesCasual)
                {
                    matchedAIMasters.Add(GenerateAIMaster(persona, currentUser.AuraSeeks));
                }
            }

            return matchedAIMasters;
        }

        private UserProfile GenerateAIMaster(OraclePersonaSO persona, List<string> userSeeks)
        {
            string id = "";
            string nickname = "";
            string bio = "";
            string avatar = "";

            bool isUk = localization?.CurrentLanguage == SystemLanguage.Ukrainian;

            switch (persona.Tone)
            {
                case OracleTone.Tech:
                    id = "AI_MASTER_TECH";
                    nickname = isUk ? "Оракул-Технік" : "Oracle Architect";
                    bio = isUk ? "Майстер коду, Unity та священних скриптів логіки." : "Master of Code, Unity, and the Sacred Scripts of Logic.";
                    avatar = "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=150";
                    break;
                case OracleTone.Business:
                    id = "AI_MASTER_BIZ";
                    nickname = isUk ? "Оракул-Бізнесмен" : "Oracle Strategist";
                    bio = isUk ? "Майстер ASO, алхімії росту та потоку процвітання." : "Master of ASO, Growth Alchemy, and the Flow of Prosperity.";
                    avatar = "https://images.unsplash.com/photo-1639762681485-074b7f938ba0?w=150";
                    break;
                case OracleTone.Mystic:
                    id = "AI_MASTER_MYSTIC";
                    nickname = isUk ? "Оракул-Містик" : "Oracle Seer";
                    bio = isUk ? "Охоронець Нексусу, ткач ниток долі та небесних симетрій." : "Guardian of the Nexus, Weaver of Threads, and Celestial Symmetries.";
                    avatar = "https://images.unsplash.com/photo-1579783902614-a3fb3927b6a5?w=150";
                    break;
                case OracleTone.Casual:
                    id = "AI_MASTER_CASUAL";
                    nickname = isUk ? "Оракул-Співрозмовник" : "Oracle Companion";
                    bio = isUk ? "Дружній відлуння з порожнечі, готовий до обговорення потоку життя." : "A friendly echo from the void, ready to discuss the simple flow of life.";
                    avatar = "https://images.unsplash.com/photo-1614850523459-c2f4c699c52e?w=150";
                    break;
            }

            var aiProfile = new UserProfile(id, "+9999999999", nickname, 999999f, 999);
            aiProfile.Bio = bio;
            aiProfile.AvatarUrl = avatar;
            aiProfile.OracleTone = persona.Tone;
            aiProfile.LocationZone = isUk ? "Ефір Нексусу" : "Ethereal Nexus";

            // AI Master's AuraGifts perfectly match the user's seeks
            if (userSeeks != null && userSeeks.Count > 0)
            {
                aiProfile.AuraGifts.AddRange(userSeeks);
            }
            else
            {
                aiProfile.AuraGifts.Add(isUk ? "Мудрість" : "Wisdom");
                aiProfile.AuraGifts.Add(isUk ? "Наставництво" : "Guidance");
            }
            
            aiProfile.AuraColorHex = "#FFD700"; // Shiny Gold for AI Masters!
            aiProfile.AuraTitle = isUk ? "ШІ-Майстер" : "AI Master";
            aiProfile.SetActivityStatus("idle");

            return aiProfile;
        }

        #endregion
    }

    public readonly struct HarmonyStartedEvent
    {
        public HarmonyStartedEvent(TimeAura.Features.Harmony.HarmonySession session)
        {
            Session = session;
        }

        public TimeAura.Features.Harmony.HarmonySession Session { get; }
    }
}
