using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Services;
using UnityEngine;
using TimeAura.Features.Data;
using VContainer;

namespace TimeAura.Features.Social
{
    /// <summary>
    /// Manages social network features: authentication, profiles, feed, posts.
    /// Uses modern async patterns with UniTask and Addressables.
    /// </summary>
    public partial class SocialManager : MonoBehaviour, IManager
    {
        [Inject] private INetworkService _networkService;
        [Inject] private AddressableAssetService _assetService;
        [Inject] private TimeAura.Features.Localization.LocalizationManager _localization;

        private UserProfile _currentUser;
        private readonly Dictionary<string, UserProfile> _profileCache = new();
        private readonly Dictionary<string, Texture2D> _avatarCache = new();

        public bool IsInitialized { get; private set; }
        public bool IsLoggedIn => _currentUser != null && _currentUser.IsValid;
        public UserProfile CurrentUser => _currentUser;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            // Try to restore session from saved token
            if (_networkService.IsAuthenticated)
            {
                try
                {
                    _currentUser = await _networkService.GetAsync<UserProfile>(
                        "users/me",
                        cancellationToken
                    );
                    Debug.Log($"[SocialManager] Session restored: {_currentUser.Username}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SocialManager] Failed to restore session: {ex.Message}");
                    _networkService.ClearAuth();
                }
            }

            IsInitialized = true;
        }

        public async UniTask ShutdownAsync()
        {
            _profileCache.Clear();
            _avatarCache.Clear();
            IsInitialized = false;
            await UniTask.Yield();
        }

        #region Authentication

        /// <summary>
        /// Login with email and password
        /// </summary>
        public async UniTask<bool> LoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new LoginRequest { email = email, password = password };
                var response = await _networkService.PostAsync<LoginRequest, LoginResponse>(
                    "auth/login",
                    request,
                    cancellationToken
                );

                if (response?.token != null)
                {
                    _networkService.SetAuthToken(response.token);
                    _currentUser = response.user;

                    // Preload user avatar
                    _ = LoadAvatarAsync(_currentUser.UserId, _currentUser.AvatarUrl, cancellationToken);

                    Debug.Log($"[SocialManager] Login successful: {_currentUser.Username}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocialManager] Login failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        public async UniTask LogoutAsync()
        {
            try
            {
                // Notify server (optional)
                // await _networkService.PostAsync<object, object>("auth/logout", null);

                _networkService.ClearAuth();
                _currentUser = null;
                _profileCache.Clear();
                _avatarCache.Clear();

                Debug.Log("[SocialManager] Logout successful");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocialManager] Logout failed: {ex.Message}");
            }

            await UniTask.Yield();
        }

        #endregion

        #region User Profiles

        /// <summary>
        /// Get user profile by ID with caching
        /// </summary>
        public async UniTask<UserProfile> GetUserProfileAsync(
            string userId,
            bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            // Check cache
            if (!forceRefresh && _profileCache.TryGetValue(userId, out var cached))
            {
                return cached;
            }

            // 🤖 Native interception of AI Master profiles
            if (userId != null && userId.StartsWith("ai_qwen_"))
            {
                var aiMasters = TimeAura.Features.Data.AIMasterFactory.GetAIMasters();
                var found = aiMasters.Find(m => m.UserId == userId);
                if (found != null)
                {
                    _profileCache[userId] = found;
                    return found;
                }
            }

            try
            {
                var profile = await _networkService.GetAsync<UserProfile>(
                    $"users/{userId}",
                    cancellationToken
                );

                if (profile != null && profile.IsValid)
                {
                    _profileCache[userId] = profile;

                    // Preload avatar if not in cache
                    if (!string.IsNullOrEmpty(profile.AvatarUrl) && !_avatarCache.ContainsKey(userId))
                    {
                        _ = LoadAvatarAsync(userId, profile.AvatarUrl, cancellationToken);
                    }
                }

                return profile;
            }
            catch (Exception)
            {
                // Cache the mock profile to prevent spamming network requests for the same failing user
                var mock = GetMockProfile(userId);
                _profileCache[userId] = mock;
                
                // Reduced log level to avoid flooding the console during offline testing
                Debug.Log($"[SocialManager] 📡 Offline fallback: Materializing persona for {userId}");
                return mock;
            }
        }

        private UserProfile GetMockProfile(string userId)
        {
            string[] mockNames = { "Lyra Starlight", "Kaelen Voidwalker", "Serafina Bloom", "Aurelius Rex", "Zen Master" };
            string[] mockBios = {
                "Guided by the stars, seeking harmony in the void.",
                "Scholar of the forbidden Chronos arts.",
                "Healing the world, one resonance at a time.",
                "Legacy of the ancient kings flows through my aura.",
                "In silence, the Nexus reveals all truths."
            };

            int idx = Mathf.Abs(userId.GetHashCode()) % mockNames.Length;
            
            return new UserProfile(userId, "", mockNames[idx], 25 + idx, 5 + idx)
            {
                Bio = mockBios[idx],
                PrimaryPillar = "Spirit"
            };
        }

        /// <summary>
        /// Load user avatar from URL or Addressables
        /// </summary>
        public async UniTask<Texture2D> LoadAvatarAsync(
            string userId,
            string avatarUrl,
            CancellationToken cancellationToken = default)
        {
            // Check cache
            if (_avatarCache.TryGetValue(userId, out var cached))
            {
                return cached;
            }

            try
            {
                Texture2D avatar;

                // If URL is Addressable address, load from Addressables
                if (avatarUrl.StartsWith("avatar_"))
                {
                    avatar = await _assetService.LoadAssetAsync<Texture2D>(
                        avatarUrl,
                        useCache: true,
                        cancellationToken
                    );
                }
                else
                {
                    // Load from remote URL
                    avatar = await _networkService.DownloadTextureAsync(avatarUrl, cancellationToken);
                }

                if (avatar != null)
                {
                    _avatarCache[userId] = avatar;
                }

                return avatar;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocialManager] Load avatar failed: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Feed & Posts

        /// <summary>
        /// Get feed posts with pagination
        /// </summary>
        public async UniTask<FeedResponse> GetFeedAsync(
            int page = 1,
            int pageSize = 20,
            string userId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = string.IsNullOrEmpty(userId)
                    ? $"feed?page={page}&pageSize={pageSize}"
                    : $"users/{userId}/posts?page={page}&pageSize={pageSize}";

                var feed = await _networkService.GetAsync<FeedResponse>(endpoint, cancellationToken);

                if (feed?.posts != null)
                {
                    // Preload avatars and images for visible posts
                    _ = PreloadFeedAssetsAsync(feed.posts, cancellationToken);
                }

                return feed;
            }
            catch (Exception ex)
            {
                Debug.Log($"[SocialManager] 📡 Network feed unavailable ({ex.Message}). Summoning Ethereal Chronicles...");
                // Add a small delay to avoid synchronous UI virtualization issues
                await UniTask.Delay(100, cancellationToken: cancellationToken);
                return GetMockFeed(page, pageSize);
            }
        }

        private FeedResponse GetMockFeed(int page, int pageSize)
        {
            // Rich preset posts that showcase the Service Request concept
            var preset = new List<Post>
            {
                new Post {
                    postId = "sr_001", userId = "user_1", username = "Oksana Kovalenko",
                    content = "\ud83d\udd27 Потрібен сантехнік для заміни змішувача і труб у ванній. Є всі матеріали. Орієнтовно 3-4 год роботи.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Craft,
                    horasPrice = 3, priceType = PriceType.Fixed,
                    distanceKm = 1.8f, isUrgent = true, authorCity = "\u041a\u0438\u0457\u0432, \u041e\u0431\u043e\u043b\u043e\u043d\u044c",
                    likesCount = 12, commentsCount = 3, createdAt = DateTime.UtcNow.AddHours(-2),
                    realm = ExchangeRealm.Ether, priceAtoms = 180
                },
                new Post {
                    postId = "ch_001", userId = "user_2", username = "Lev Starlight",
                    content = "\u0421\u044c\u043e\u0433\u043e\u0434\u043d\u0456 Ефір \u043e\u0441\u043e\u0431\u043b\u0438\u0432\u043e \u0447\u0438\u0441\u0442\u0438\u0439. \u0427\u0430\u0441 \u0440\u0443\u0445\u0430\u0454\u0442\u044c\u0441\u044f \u043d\u0430 \u043d\u043e\u0432\u0438\u0445 \u0432\u0456\u0431\u0440\u0430\u0446\u0456\u0441\u0446\u044f\u0445. \u041f\u0440\u0430\u0446\u044e\u0439\u0442\u0435 \u0443 Nexus \u0437 \u0440\u0430\u0434\u0456\u0441\u0442\u044e \u2014 \u0446\u0435 \u043f\u043e\u0432\u0435\u0440\u0442\u0430\u0454\u0442\u044c\u0441\u044f!",
                    postType = PostType.Chronicle,
                    likesCount = 247, commentsCount = 18, createdAt = DateTime.UtcNow.AddHours(-5),
                    realm = ExchangeRealm.None
                },
                new Post {
                    postId = "sr_002", userId = "user_3", username = "Ivan Petrenko",
                    content = "\ud83d\udcda \u0420\u0435\u043f\u0435\u0442\u0438\u0442\u043e\u0440 \u0437 \u0430\u043d\u0433\u043b\u0456\u0439\u0441\u044c\u043a\u043e\u0457 — \u0434\u043b\u044f \u043f\u0456\u0434\u0433\u043e\u0442\u043e\u0432\u043a\u0438 \u0434\u043e \u0441\u043f\u0456\u0432\u0431\u0435\u0441\u0456\u0434\u0438 \u0437 \u0456\u043d\u043e\u0437\u0435\u043c\u043d\u0438\u043c\u0438 \u043f\u0430\u0440\u0442\u043d\u0435\u0440\u0430\u043c\u0438. \u0420\u0456\u0432\u0435\u043d\u044c B1-B2. 2-3 \u0441\u0435\u0441\u0456\u0457.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Teaching,
                    horasPrice = 5, priceType = PriceType.Fixed,
                    distanceKm = 12.5f, isUrgent = false, authorCity = "\u041a\u0438\u0457\u0432, \u041f\u0435\u0447\u0435\u0440\u0441\u044c\u043a",
                    likesCount = 34, commentsCount = 8, createdAt = DateTime.UtcNow.AddHours(-8),
                    realm = ExchangeRealm.Ether, priceAtoms = 300
                },
                new Post {
                    postId = "sr_003", userId = "user_4", username = "Marta Lysenko",
                    content = "\ud83c\udf3f \u041f\u043e\u0442\u0440\u0456\u0431\u043d\u0430 \u0434\u043e\u043f\u043e\u043c\u043e\u0433\u0430 \u0437 \u0440\u043e\u0437\u0431\u0438\u0432\u043a\u043e\u044e \u043e\u0433\u043e\u0440\u043e\u0434\u0443 \u0456 \u043f\u043e\u0441\u0430\u0434\u043a\u043e\u044e \u0440\u043e\u0441\u043b\u0438\u043d. \u041c\u0430\u0454\u043c\u043e \u0434\u0430\u0447\u0443 30 \u0441\u043e\u0442\u043e\u043a, 2 \u0434\u043d\u0456.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Nature,
                    horasPrice = 0, priceType = PriceType.Negotiate,
                    distanceKm = 4.2f, isUrgent = false, authorCity = "\u0411\u0443\u0447\u0430",
                    likesCount = 9, commentsCount = 2, createdAt = DateTime.UtcNow.AddDays(-1),
                    realm = ExchangeRealm.Ether, priceAtoms = 0
                },
                new Post {
                    postId = "sr_004", userId = "user_5", username = "Dmytro Kravets",
                    content = "\ud83d\udcbb \u041d\u0435\u043e\u0431\u0445\u0456\u0434\u043d\u0438\u0439 Flutter-\u0440\u043e\u0437\u0440\u043e\u0431\u043d\u0438\u0439 \u0434\u043b\u044f MVP \u043c\u043e\u0431\u0456\u043b\u044c\u043d\u043e\u0433\u043e \u0434\u043e\u0434\u0430\u0442\u043a\u0443. \u0421\u043f\u0440\u0430\u0432\u0436\u043d\u0456\u0441\u0442\u044c 1-2 \u0442\u0438\u0436\u043d\u0456. \u0420\u043e\u0431\u043e\u0442\u0430 \u0443 \u043a\u043e\u043c\u0430\u043d\u0434\u0456.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Code,
                    horasPrice = 10, priceType = PriceType.Fixed,
                    distanceKm = 30f, isUrgent = true, authorCity = "\u0412\u0456\u0434\u0434\u0430\u043b\u0435\u043d\u043e",
                    likesCount = 67, commentsCount = 14, createdAt = DateTime.UtcNow.AddHours(-20),
                    realm = ExchangeRealm.Ether, priceAtoms = 600
                },
                new Post {
                    postId = "sr_005", userId = "user_0", username = "Anna Bondarenko",
                    content = "\ud83c\udfa8 \u0428\u0443\u043a\u0430\u044e \u0445\u0443\u0434\u043e\u0436\u043d\u0438\u043a\u0430 \u0434\u043b\u044f \u043e\u0444\u043e\u0440\u043c\u043b\u0435\u043d\u043d\u044f \u043b\u043e\u0433\u043e\u0442\u0438\u043f\u0443 \u0456 \u043f\u0440\u0435\u0437\u0435\u043d\u0442\u0430\u0446\u0456\u0439. \u0421\u0442\u0438\u043b\u044c: \u043c\u0456\u043d\u0456\u043c\u0430\u043b\u0456\u0437\u043c + \u043c\u0456\u0441\u0442\u0438\u043a\u0430.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Art,
                    horasPrice = 7, priceType = PriceType.Fixed,
                    distanceKm = 8.1f, isUrgent = false, authorCity = "\u041b\u044c\u0432\u0456\u0432",
                    likesCount = 53, commentsCount = 11, createdAt = DateTime.UtcNow.AddDays(-2),
                    realm = ExchangeRealm.Ether, priceAtoms = 700
                },
                new Post {
                    postId = "ch_002", userId = "user_2", username = "Lev Starlight",
                    content = "\u041d\u0435\u0432\u0456\u0440\u043e\u0433\u0456\u0434\u043d\u043e, \u0430\u043b\u0435 \u0437\u0430 \u0446\u0435\u0439 \u0442\u0438\u0436\u0434\u0435\u043d\u044c Nexus \u0434\u0430\u0432 \u043c\u0435\u043d\u0456 +12 \u0425\u043e\u0440. \u041d\u0430\u0434\u0430\u0432 \u0434\u0432\u0430 \u0443\u0440\u043e\u043a\u0438 \u0437 Python \u0434\u043b\u044f \u043f\u043e\u0447\u0430\u0442\u043a\u0456\u0432\u0446\u044f \u0437\u0456 \u0421\u0443\u043c \u0434\u0438\u0441\u0442\u0430\u043d\u0446\u0456\u0439\u043d\u043e. Nexus \u0432\u0456\u0434\u043a\u0440\u0438\u0432\u0430\u0454 \u0442\u043e\u0447\u043a\u0438 \u0437\u0456\u0442\u043a\u043d\u0435\u043d\u043d\u044f \u2014 \u0434\u044f\u043a\u0443\u0447\u0438!",
                    postType = PostType.Chronicle,
                    likesCount = 189, commentsCount = 32, createdAt = DateTime.UtcNow.AddDays(-1),
                    realm = ExchangeRealm.None
                },
                new Post {
                    postId = "sr_006", userId = "user_3", username = "Yuliia Koval",
                    content = "\ud83d\udcc8 \u041f\u043e\u0442\u0440\u0456\u0431\u043d\u0438\u0439 \u0431\u0443\u0445\u0433\u0430\u043b\u0442\u0435\u0440 \u0434\u043b\u044f \u043c\u0430\u043b\u043e\u0433\u043e \u0424\u041e\u041f. \u0412\u0435\u0434\u0435\u043d\u043d\u044f \u041f\u0414\u0412 + \u0437\u0432\u0456\u0442\u043d\u0456\u0441\u0442\u044c. \u041e\u0434\u0438\u043d \u0440\u0430\u0437 \u043d\u0430 \u043c\u0456\u0441\u044f\u0446\u044c.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Teaching,
                    horasPrice = 0, priceType = PriceType.Free,
                    distanceKm = 2.5f, isUrgent = false, authorCity = "\u041a\u0438\u0457\u0432",
                    likesCount = 28, commentsCount = 7, createdAt = DateTime.UtcNow.AddHours(-36),
                    realm = ExchangeRealm.Ether, priceAtoms = 0
                },
                new Post {
                    postId = "sr_007", userId = "user_4", username = "Serhii Nechyporenko",
                    content = "\ud83d\udc68\u200d\ud83d\udd27 \u041f\u043e\u0442\u0440\u0456\u0431\u043d\u0430 \u0443\u043a\u043b\u0430\u0434\u043a\u0430 \u043f\u043b\u0438\u0442\u043a\u0438 \u0443 \u0432\u0430\u043d\u043d\u0456\u0439. \u041f\u043b\u0438\u0442\u043a\u0430 \u0454, \u043a\u043b\u0435\u0439 \u0454, \u043f\u043e\u0442\u0440\u0456\u0431\u0435\u043d \u043c\u0430\u0439\u0441\u0442\u0435\u0440. \u041e\u0440\u0456\u0454\u043d\u0442\u043e\u0432\u043d\u043e 4 \u0433\u043e\u0434\u0438.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Craft,
                    horasPrice = 4, priceType = PriceType.Fixed,
                    distanceKm = 0.9f, isUrgent = true, authorCity = "\u041a\u0438\u0457\u0432, \u041f\u043e\u0434\u0456\u043b",
                    likesCount = 6, commentsCount = 1, createdAt = DateTime.UtcNow.AddHours(-3),
                    realm = ExchangeRealm.Ether, priceAtoms = 240
                },
                new Post {
                    postId = "sr_008", userId = "user_1", username = "Natalia Kravchenko",
                    content = "\u270d\ufe0f \u041f\u043e\u0442\u0440\u0456\u0431\u043d\u0430 \u0440\u0435\u0434\u0430\u043a\u0442\u0443\u0440\u0430 \u0441\u0430\u0439\u0442\u0443 (\u0443\u0445\u0432\u0430\u043b\u0435\u043d\u043d\u044f, \u0431\u043b\u043e\u0433, \u043f\u0440\u043e\u0434\u0430\u0436\u0456 \u0442\u0435\u043a\u0441\u0442\u0438). 3 \u0441\u0442\u0430\u0442\u0442\u0456 x 1500 \u0441\u043b\u0456\u0432. SEO-\u0434\u0440\u0443\u0436\u043d\u044c\u043e \u0432\u0456\u0442\u0430\u044e.",
                    postType = PostType.ServiceRequest, serviceCategory = ServiceCategory.Art,
                    horasPrice = 3, priceType = PriceType.Fixed,
                    distanceKm = 99f, isUrgent = false, authorCity = "\u0412\u0456\u0434\u0434\u0430\u043b\u0435\u043d\u043e",
                    likesCount = 41, commentsCount = 9, createdAt = DateTime.UtcNow.AddDays(-3),
                    realm = ExchangeRealm.Matter, priceWaves = 300
                },
            };

            // For further pages, generate generic posts based on page
            var mockNames = new[] { "Lyra Starlight", "Kaelen Voidwalker", "Serafina Bloom", "Aurelius Rex", "Zen Master" };
            var mockContent = new[] {
                "\u0421\u043f\u043e\u0441\u0442\u0435\u0440\u0456\u0433\u0430\u044e\u0442\u044c \u0435\u043d\u0435\u0440\u0433\u0456\u0457 Ефіру. \u041d\u043e\u0432\u0430 \u0443\u0433\u043e\u0434\u0430 \u0432\u0436\u0435 \u0431\u043b\u0438\u0437\u044c\u043a\u043e.",
                "\u0412\u0456\u0434\u0447\u0443\u0432\u0430\u044e \u0441\u0438\u043b\u044c\u043d\u0438\u0439 \u0440\u0435\u0437\u043e\u043d\u0430\u043d\u0441 \u0443 \u0440\u0430\u0439\u043e\u043d\u0456 \u0425\u0440\u043e\u043d\u043e\u0441\u0443.",
                "\u0425\u0442\u043e \u043f\u043e\u0454\u0434\u043d\u0430\u0454\u0442\u044c\u0441\u044f \u043d\u0430 \u043a\u043e\u043b\u0435\u043a\u0442\u0438\u0432\u043d\u0443 \u043c\u0435\u0434\u0438\u0442\u0430\u0446\u0456\u044e \u0443 \u0421\u0430\u043d\u043a\u0442\u0443\u0430\u0440\u0456\u0457?",
                "\u0428\u0430\u0431\u043b\u043e\u043d\u0438 \u0432 \u041f\u043e\u0440\u043e\u0436\u043d\u0454\u0447\u0438\u043d\u0456 \u0437\u043c\u0456\u043d\u044e\u044e\u0442\u044c\u0441\u044f. \u0411\u0443\u0434\u044c\u0442\u0435 \u043d\u0430 \u0432\u0430\u0440\u0442\u0456.",
                "\u0429\u043e\u0439\u043d\u043e \u0434\u043e\u0441\u044f\u0433 \u043d\u043e\u0432\u043e\u0433\u043e \u0440\u0456\u0432\u043d\u044f \u0413\u0430\u0440\u043c\u043e\u043d\u0456\u0457. \u0428\u043b\u044f\u0445 \u0432\u0438\u044f\u0432\u043b\u044f\u0454 \u0441\u0435\u0431\u0435."
            };

            var result = new List<Post>();

            if (page == 1)
            {
                // 🤖 Inject AI Masters into the very top of the feed as Service Requests
                var aiMasters = TimeAura.Features.Data.AIMasterFactory.GetAIMasters();
                foreach (var master in aiMasters)
                {
                    long horasPrice = 0;
                    if (master.UserId == "ai_qwen_coder") horasPrice = 50;
                    if (master.UserId == "ai_qwen_translator") horasPrice = 20;
                    if (master.UserId == "ai_qwen_lawyer") horasPrice = 100;

                    bool isUk = _localization != null && _localization.CurrentLanguage == SystemLanguage.Ukrainian;
                    string titleStr = isUk ? "ШІ МАЙСТЕР" : "AI MASTER";
                    string promptStr = isUk ? "Готовий до співпраці. Натисніть 'ДОСЬЄ' для початку взаємодії." : "Ready for collaboration. Tap 'DOSSIER' to initiate interaction.";

                    result.Add(new Post {
                        postId = $"ai_sr_{master.UserId}",
                        userId = master.UserId,
                        username = master.DisplayName,
                        content = $"🤖 [{titleStr}] {master.Bio}\n\n{promptStr}",
                        postType = PostType.ServiceRequest,
                        serviceCategory = ServiceCategory.Code,
                        horasPrice = horasPrice,
                        priceType = PriceType.Fixed,
                        distanceKm = 0f,
                        isUrgent = true,
                        authorCity = "Nexus Core",
                        likesCount = UnityEngine.Random.Range(50, 999),
                        commentsCount = UnityEngine.Random.Range(10, 100),
                        createdAt = DateTime.UtcNow,
                        realm = ExchangeRealm.Matter,
                        priceAtoms = horasPrice * 60,
                        priceWaves = (long)(horasPrice * 0.3f)
                    });
                }

                result.AddRange(preset);
                // Fill remaining slots from page 1 with generics
                for (int i = preset.Count; i < pageSize; i++)
                {
                    int idx = i % 5;
                    result.Add(new Post {
                        postId = $"gen_{i}",
                        userId = $"user_{idx}",
                        username = mockNames[idx],
                        content = mockContent[idx],
                        postType = PostType.Chronicle,
                        likesCount = UnityEngine.Random.Range(5, 500),
                        commentsCount = UnityEngine.Random.Range(2, 50),
                        isLiked = UnityEngine.Random.value > 0.5f,
                        createdAt = DateTime.UtcNow.AddHours(-i - 48)
                    });
                }
            }
            else
            {
                for (int i = 0; i < pageSize; i++)
                {
                    int id = ((page - 1) * pageSize) + i;
                    int idx = id % 5;
                    result.Add(new Post {
                        postId = $"mock_{id}",
                        userId = $"user_{idx}",
                        username = mockNames[idx],
                        content = mockContent[idx],
                        postType = PostType.Chronicle,
                        likesCount = UnityEngine.Random.Range(5, 500),
                        commentsCount = UnityEngine.Random.Range(2, 50),
                        isLiked = UnityEngine.Random.value > 0.5f,
                        createdAt = DateTime.UtcNow.AddHours(-id)
                    });
                }
            }

            return new FeedResponse
            {
                posts = result.ToArray(),
                hasMore = page < 3
            };
        }


        /// <summary>
        /// Create new post
        /// </summary>
        public async UniTask<Post> CreatePostAsync(
            string content,
            string[] imageUrls = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new CreatePostRequest
                {
                    content = content,
                    imageUrls = imageUrls ?? Array.Empty<string>()
                };

                var response = await _networkService.PostAsync<CreatePostRequest, CreatePostResponse>(
                    "posts",
                    request,
                    cancellationToken
                );

                return response?.post;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocialManager] Create post failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Upload image for post
        /// </summary>
        public async UniTask<string> UploadImageAsync(
            byte[] imageData,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var fileName = $"post_{Guid.NewGuid():N}.jpg";
                var url = await _networkService.UploadFileAsync(
                    "uploads/images",
                    imageData,
                    fileName,
                    progress,
                    cancellationToken
                );

                return url;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocialManager] Upload image failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Like/unlike post
        /// </summary>
        public async UniTask<bool> ToggleLikeAsync(
            string postId,
            bool isLiked,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = isLiked ? $"posts/{postId}/unlike" : $"posts/{postId}/like";
                await _networkService.PostAsync<object, object>(endpoint, null, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SocialManager] Toggle like simulated locally. Network error: {ex.Message}");
                return false; // Still returning false but logging as a warning so it doesn't spam errors
            }
        }

        #endregion

        #region Performance Optimizations

        /// <summary>
        /// Preload assets for feed items (avatars, images)
        /// </summary>
        private async UniTask PreloadFeedAssetsAsync(
            Post[] posts,
            CancellationToken cancellationToken)
        {
            var tasks = new List<UniTask>();

            foreach (var post in posts)
            {
                // Preload user avatar
                if (!string.IsNullOrEmpty(post.userAvatarUrl))
                {
                    tasks.Add(LoadAvatarAsync(post.userId, post.userAvatarUrl, cancellationToken));
                }

                // Preload first image of post
                if (post.imageUrls != null && post.imageUrls.Length > 0)
                {
                    var imageUrl = post.imageUrls[0];
                    tasks.Add(PreloadImageAsync(imageUrl, cancellationToken));
                }
            }

            await UniTask.WhenAll(tasks);
        }

        private async UniTask PreloadImageAsync(string imageUrl, CancellationToken cancellationToken)
        {
            try
            {
                // For Addressable images
                if (imageUrl.StartsWith("post_image_"))
                {
                    await _assetService.LoadAssetAsync<Texture2D>(imageUrl, true, cancellationToken);
                }
                else
                {
                    // For remote images, just initiate download
                    await _networkService.DownloadTextureAsync(imageUrl, cancellationToken);
                }
            }
            catch
            {
                // Silently fail preloading
            }
        }

        #endregion
    }
}
