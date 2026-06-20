using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Social;
using TimeAura.Features.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TimeAura.Features.UI.Social
{
    /// <summary>
    /// "Convergence Feed" — The central stream where Adepts witness each other's fates.
    /// Infinite scroll through mystical FateCards, each revealing a glimpse of destiny.
    /// "In the convergence, all timelines meet and intertwine."
    /// </summary>
    public class ConvergenceFeed : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private FateCard fateCardPrefab;
        [SerializeField] private CanvasGroup feedCanvasGroup;

        [Header("Loading & Refresh")]
        [SerializeField] private GameObject loadingOrb; // Animated golden orb
        [SerializeField] private Button refreshButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private ParticleSystem convergenceParticles;

        [Header("Settings")]
        [SerializeField] private int pageSize = 15; // Cards per page
        [SerializeField] private float preloadThreshold = 0.75f; // Preload at 75% scroll

        [Header("Theme")]
        [SerializeField] private Color goldenColor = new Color(1f, 0.84f, 0f);

        [Inject] private SocialManager socialManager;

        private readonly List<FateCard> activeFateCards = new();
        private readonly Queue<FateCard> pooledFateCards = new();
        private readonly Dictionary<string, UserProfile> profileCache = new();

        private int currentPage = 1;
        private bool isLoading;
        private bool hasMoreFates = true;
        private CancellationTokenSource loadCts;

        private void Awake()
        {
            // Setup theme
            statusText.color = goldenColor;

            // Setup scroll tracking
            scrollRect.onValueChanged.AddListener(OnScrollChanged);

            // Setup refresh
            refreshButton.onClick.AddListener(() => _ = RefreshConvergenceAsync());
        }

        private void OnDestroy()
        {
            loadCts?.Cancel();
            loadCts?.Dispose();

            scrollRect.onValueChanged.RemoveAllListeners();
            refreshButton.onClick.RemoveAllListeners();
        }

        private async void Start()
        {
            // Entrance animation
            await AnimateEntranceAsync();

            // Load initial fates
            _ = LoadMoreFatesAsync();
        }

        #region Public API

        /// <summary>
        /// Refresh the convergence from the beginning.
        /// </summary>
        public async UniTask RefreshConvergenceAsync()
        {
            if (isLoading) return;

            loadCts?.Cancel();
            loadCts = new CancellationTokenSource();

            SetStatus("Realigning the convergence...");

            currentPage = 1;
            hasMoreFates = true;

            // Return all cards to pool
            ClearAllFates();

            // Reload
            await LoadMoreFatesAsync();
        }

        /// <summary>
        /// Load fates from a specific Adept only.
        /// </summary>
        public async UniTask LoadAdeptFatesAsync(string userId)
        {
            if (isLoading) return;

            loadCts?.Cancel();
            loadCts = new CancellationTokenSource();

            SetStatus($"Viewing {userId}'s timeline...");

            currentPage = 1;
            hasMoreFates = true;
            ClearAllFates();

            await LoadMoreFatesAsync(userId);
        }

        #endregion

        #region Load & Display

        private async UniTask LoadMoreFatesAsync(string userId = null)
        {
            if (isLoading || !hasMoreFates) return;

            isLoading = true;
            ShowLoadingOrb(true);

            try
            {
                // Fetch feed from SocialManager
                var feedResponse = await socialManager.GetFeedAsync(
                    currentPage,
                    pageSize,
                    userId,
                    loadCts.Token
                );

                if (feedResponse?.posts != null && feedResponse.posts.Length > 0)
                {
                    // Display each post as a FateCard
                    await DisplayFatesAsync(feedResponse.posts);

                    hasMoreFates = feedResponse.hasMore;
                    currentPage++;

                    SetStatus($"{activeFateCards.Count} fates revealed...");
                }
                else
                {
                    hasMoreFates = false;

                    if (activeFateCards.Count == 0)
                    {
                        SetStatus("The convergent silence... No fates to show.");
                    }
                    else
                    {
                        SetStatus("You've reached the edge of the known convergence.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ConvergenceFeed] Load cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConvergenceFeed] Failed to load fates: {ex.Message}");
                SetStatus("The convergence flickered. Pull to retry.");
            }
            finally
            {
                isLoading = false;
                ShowLoadingOrb(false);
            }
        }

        private async UniTask DisplayFatesAsync(Post[] posts)
        {
            foreach (var post in posts)
            {
                try
                {
                    // Get or fetch user profile
                    UserProfile profile = null;
                    if (!profileCache.TryGetValue(post.userId, out profile))
                    {
                        profile = await socialManager.GetUserProfileAsync(
                            post.userId,
                            forceRefresh: false,
                            cancellationToken: loadCts.Token
                        );

                    }

                    // Get FateCard from pool or create new
                    var fateCard = GetOrCreateFateCard();

                    // Initialize with post and profile data
                    fateCard.Initialize(post, profile, loadCts.Token);

                    fateCard.gameObject.SetActive(true);
                    activeFateCards.Add(fateCard);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ConvergenceFeed] Failed to display fate: {ex.Message}");
                }
            }
        }

        private FateCard GetOrCreateFateCard()
        {
            if (pooledFateCards.Count > 0)
            {
                var card = pooledFateCards.Dequeue();
                card.transform.SetAsLastSibling();
                return card;
            }

            var newCard = Instantiate(fateCardPrefab, contentContainer);
            return newCard;
        }

        private void ClearAllFates()
        {
            foreach (var fateCard in activeFateCards)
            {
                fateCard.gameObject.SetActive(false);
                pooledFateCards.Enqueue(fateCard);
            }

            activeFateCards.Clear();
        }

        #endregion

        #region Scroll Handling

        private void OnScrollChanged(Vector2 scrollPosition)
        {
            // Infinite scroll: load more when scrolled near bottom
            if (!isLoading && hasMoreFates && scrollPosition.y < (1f - preloadThreshold))
            {
                _ = LoadMoreFatesAsync();
            }

            // Update aura intensity based on scroll position for visible cards
            UpdateCardAurasBasedOnScroll();
        }

        /// <summary>
        /// Cards closer to center of viewport get stronger auras.
        /// </summary>
        private void UpdateCardAurasBasedOnScroll()
        {
            if (activeFateCards.Count == 0) return;

            var viewportRect = scrollRect.viewport.rect;
            var viewportCenter = viewportRect.center;

            foreach (var card in activeFateCards)
            {
                if (!card.gameObject.activeInHierarchy) continue;

                // Get card position relative to viewport
                var cardRect = card.GetComponent<RectTransform>();
                var cardWorldPos = cardRect.position;
                var viewportLocalPos = scrollRect.viewport.InverseTransformPoint(cardWorldPos);

                // Calculate distance from viewport center (0 = center, 1 = edge)
                float distanceFromCenter = Mathf.Abs(viewportLocalPos.y) / (viewportRect.height * 0.5f);
                distanceFromCenter = Mathf.Clamp01(distanceFromCenter);

                // Notify card (it can update shader intensity)
                if (distanceFromCenter < 1.5f)
                {
                    card.OnBecameVisible();
                }
                else
                {
                    card.OnBecameInvisible();
                }
            }
        }

        #endregion

        #region Visual Feedback

        private async UniTask AnimateEntranceAsync()
        {
            feedCanvasGroup.alpha = 0f;

            // Start convergence particles
            if (convergenceParticles != null)
            {
                convergenceParticles.Play();
            }

            // Fade in
            float duration = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                feedCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                await UniTask.Yield();
            }

            feedCanvasGroup.alpha = 1f;
            SetStatus("The convergence reveals itself...");
        }

        private void ShowLoadingOrb(bool show)
        {
            if (loadingOrb != null)
            {
                loadingOrb.SetActive(show);
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        #endregion
    }
}
