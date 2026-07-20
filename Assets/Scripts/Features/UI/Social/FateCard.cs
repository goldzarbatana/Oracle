using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Localization;
using TimeAura.Core.Services;
using TimeAura.Features.Localization;
using TimeAura.Features.Social;
using TimeAura.Features.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace TimeAura.Features.UI.Social
{
    /// <summary>
    /// FateCard - A mystical representation of an adept in the Convergence Feed.
    /// Each card reveals: Avatar (Visage), Status (Current State), Vector (Time Value).
    /// "In the cards, we see reflections of infinite possibilities."
    /// </summary>
    public class FateCard : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image avatarBorder; // Golden border with aura
        [SerializeField] private Image auraGlow; // Shader-driven pulsating aura
        [SerializeField] private ParticleSystem auraDust; // Subtle particles around avatar

        [Header("Text Elements")]
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private TextMeshProUGUI statusText; // userStatus from profile
        [SerializeField] private TextMeshProUGUI vectorText; // "Vector: 150 $/hr"
        [SerializeField] private TextMeshProUGUI timestampText;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Image contentImage;
        [SerializeField] private GameObject contentImageContainer;

        [Header("Interaction Buttons")]
        [SerializeField] private Button transformButton; // "Start Transformation"
        [SerializeField] private Image transformButtonGlow;
        [SerializeField] private TextMeshProUGUI transformButtonText;
        [SerializeField] private Button connectButton; // View profile
        [SerializeField] private Button shareButton; // Share fate

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI transformsCountText;
        [SerializeField] private TextMeshProUGUI connectionsCountText;

        [Header("Theme Colors")]
        [SerializeField] private Color goldenColor = new Color(1f, 0.84f, 0f); // #FFD700
        [SerializeField] private Color darkColor = new Color(0.1f, 0.1f, 0.1f);
        [SerializeField] private Color glowColor = new Color(1f, 0.92f, 0.5f, 0.3f);

        [Header("Shader Integration")]
        [SerializeField] private Material auraMaterial; // Aura shader material instance
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float pulseIntensity = 0.3f;

        [Inject] private SocialManager socialManager;
        [Inject] private AddressableAssetService assetService;
        [Inject] private LocalizationManager localizationManager;

        private Post currentPost;
        private UserProfile currentProfile;
        private CancellationTokenSource cts;
        private bool isTransforming;

        private void Awake()
        {
            // Setup theme
            cardBackground.color = darkColor;
            avatarBorder.color = goldenColor;
            usernameText.color = goldenColor;
            statusText.color = goldenColor * 0.8f;
            vectorText.color = glowColor;

            // Setup buttons
            transformButton.onClick.AddListener(() => _ = OnTransformClickedAsync());
            connectButton.onClick.AddListener(OnConnectClicked);
            shareButton.onClick.AddListener(OnShareClicked);

            // Initial state
            transformButtonGlow.gameObject.SetActive(false);
            contentImageContainer.SetActive(false);
        }

        private void Update()
        {
            // Pulse aura shader continuously
            if (auraMaterial != null && auraGlow.gameObject.activeInHierarchy)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                float intensity = 0.5f + (pulse * pulseIntensity);

                auraMaterial.SetFloat("_Intensity", intensity);
                auraMaterial.SetColor("_AuraColor", goldenColor * intensity);

                // Update glow alpha
                var color = glowColor;
                color.a = pulse * 0.5f;
                auraGlow.color = color;
            }
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();

            transformButton.onClick.RemoveAllListeners();
            connectButton.onClick.RemoveAllListeners();
            shareButton.onClick.RemoveAllListeners();
        }

        #region Public API

        /// <summary>
        /// Initialize FateCard with post and profile data.
        /// </summary>
        public void Initialize(Post post, UserProfile profile, CancellationToken parentCt)
        {
            cts?.Cancel();
            cts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);

            currentPost = post;
            currentProfile = profile;

            // Set text data immediately
            usernameText.text = profile?.DisplayName ?? post.username;
            var bioText = profile?.Bio ?? localizationManager?.Get(AuraTerms.MSG_SEEKING_CONVERGENCE, "Seeking convergence...") ?? "Seeking convergence...";
            statusText.text = $"\"{bioText}\"";

            // Display Vector (time value)
            if (profile != null)
            {
                // TODO: Get actual vector value from profile
                // Use Horas as a proxy for the vector value if available
                int vectorValue = profile.Horas > 0 ? (int)profile.Horas : UnityEngine.Random.Range(50, 500);
                vectorText.text = localizationManager?.GetFormatted(AuraTerms.UI_VECTOR_PER_HOUR, vectorValue) ?? $"Vector: {vectorValue} /hr";
            }

            contentText.text = post.content;
            timestampText.text = FormatTimestamp(post.createdAt);
            transformsCountText.text = FormatCount(post.likesCount);
            connectionsCountText.text = FormatCount(post.commentsCount);

            // Load visuals asynchronously
            _ = LoadVisualsAsync(cts.Token);
        }

        /// <summary>
        /// Increase pulse intensity when card becomes visible in scroll view.
        /// </summary>
        public void OnBecameVisible()
        {
            if (auraDust != null && !auraDust.isPlaying)
            {
                auraDust.Play();
            }

            auraGlow.gameObject.SetActive(true);
        }

        /// <summary>
        /// Decrease pulse when card exits scroll view for performance.
        /// </summary>
        public void OnBecameInvisible()
        {
            if (auraDust != null && auraDust.isPlaying)
            {
                auraDust.Stop();
            }

            auraGlow.gameObject.SetActive(false);
        }

        #endregion

        #region Load Visuals

        private async UniTask LoadVisualsAsync(CancellationToken ct)
        {
            try
            {
                // Load avatar (Visage)
                if (!string.IsNullOrEmpty(currentPost.userAvatarUrl))
                {
                    var avatar = await socialManager.LoadAvatarAsync(
                        currentPost.userId,
                        currentPost.userAvatarUrl,
                        ct
                    );

                    if (avatar != null)
                    {
                        avatarImage.sprite = Sprite.Create(
                            avatar,
                            new Rect(0, 0, avatar.width, avatar.height),
                            new Vector2(0.5f, 0.5f)
                        );
                    }
                    else
                    {
                        // Load default avatar from Addressables (Visages group)
                        await LoadDefaultAvatarAsync(ct);
                    }
                }
                else
                {
                    await LoadDefaultAvatarAsync(ct);
                }

                // Load content image if available
                if (currentPost.imageUrls != null && currentPost.imageUrls.Length > 0)
                {
                    var texture = await socialManager.LoadAvatarAsync(
                        $"post_{currentPost.postId}",
                        currentPost.imageUrls[0],
                        ct
                    );

                    if (texture != null)
                    {
                        contentImage.sprite = Sprite.Create(
                            texture,
                            new Rect(0, 0, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f)
                        );
                        contentImageContainer.SetActive(true);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[FateCard] Visual load cancelled for post {currentPost.postId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FateCard] Failed to load visuals: {ex.Message}");
            }
        }

        private async UniTask LoadDefaultAvatarAsync(CancellationToken ct)
        {
            try
            {
                // Load default avatar from Addressables "Visages" group
                var defaultAvatar = await assetService.LoadAssetAsync<Texture2D>(
                    "Visages/avatar_default",
                    useCache: true,
                    ct
                );

                if (defaultAvatar != null)
                {
                    avatarImage.sprite = Sprite.Create(
                        defaultAvatar,
                        new Rect(0, 0, defaultAvatar.width, defaultAvatar.height),
                        new Vector2(0.5f, 0.5f)
                    );
                }
            }
            catch
            {
                // Use built-in fallback
                avatarImage.color = darkColor;
            }
        }

        #endregion

        #region User Interactions

        /// <summary>
        /// "Start Transformation" - Optimistic UI with golden glow.
        /// </summary>
        private async UniTask OnTransformClickedAsync()
        {
            if (isTransforming) return;

            isTransforming = true;

            // Optimistic UI: Immediate golden glow
            transformButtonGlow.gameObject.SetActive(true);
            transformButton.interactable = false;
            transformButtonText.text = localizationManager?.Get(AuraTerms.TRANSCENDING, "TRANSFORMING...") ?? "TRANSFORMING...";

            try
            {
                // Animate golden glow pulsation
                var glowCts = new CancellationTokenSource();
                _ = PulseTransformGlowAsync(glowCts.Token);

                // Update state optimistically
                currentPost.isLiked = !currentPost.isLiked;
                currentPost.likesCount += currentPost.isLiked ? 1 : -1;
                transformsCountText.text = FormatCount(currentPost.likesCount);

                // Send request to server
                var success = await socialManager.ToggleLikeAsync(
                    currentPost.postId,
                    currentPost.isLiked,
                    cts.Token
                );

                glowCts.Cancel();

                if (success)
                {
                    // Success: Keep the glow briefly
                    transformButtonText.text = localizationManager?.Get(AuraTerms.SUCCESS, "TRANSFORMED ✦") != null 
                        ? localizationManager.Get(AuraTerms.SUCCESS, "TRANSFORMED ✦") + " ✦"
                        : "TRANSFORMED ✦";
                    await UniTask.Delay(1000, cancellationToken: cts.Token);
                }
                else
                {
                    // Rollback on failure
                    currentPost.isLiked = !currentPost.isLiked;
                    currentPost.likesCount += currentPost.isLiked ? 1 : -1;
                    transformsCountText.text = FormatCount(currentPost.likesCount);

                    transformButtonText.text = localizationManager?.Get(AuraTerms.ERROR, "CONNECTION LOST") ?? "CONNECTION LOST";
                    await UniTask.Delay(1500, cancellationToken: cts.Token);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FateCard] Transform failed: {ex.Message}");

                // Rollback
                currentPost.isLiked = !currentPost.isLiked;
                currentPost.likesCount += currentPost.isLiked ? 1 : -1;
                transformsCountText.text = FormatCount(currentPost.likesCount);
            }
            finally
            {
                // Reset button
                transformButtonGlow.gameObject.SetActive(false);
                transformButton.interactable = true;
                transformButtonText.text = currentPost.isLiked ? "RESONATING ✦" : "START TRANSFORMATION";
                isTransforming = false;
            }
        }

        private async UniTask PulseTransformGlowAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                    var color = goldenColor;
                    color.a = pulse * 0.8f;
                    transformButtonGlow.color = color;

                    await UniTask.Yield(ct);
                }
            }
            catch (OperationCanceledException)
            {
                transformButtonGlow.gameObject.SetActive(false);
            }
        }

        private void OnConnectClicked()
        {
            Debug.Log($"[FateCard] Connect with adept: {currentProfile?.username ?? currentPost.username}");
            string toast = localizationManager?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_OPENING_PROFILE, "Відкриття профілю Майстра...") ?? "Відкриття профілю Майстра...";
            FindAnyObjectByType<TimeAura.Features.UI.UIManager>()?.ShowToast(toast);
        }

        private void OnShareClicked()
        {
            string shareLink = $"https://timeaura.com/adept/{currentPost.userId}";
            GUIUtility.systemCopyBuffer = shareLink;
            Debug.Log($"[FateCard] Copied to clipboard: {shareLink}");
            string toast = localizationManager?.Get(TimeAura.Core.Localization.AuraTerms.TOAST_LINK_COPIED, "Посилання скопійовано!") ?? "Посилання скопійовано!";
            FindAnyObjectByType<TimeAura.Features.UI.UIManager>()?.ShowToast(toast);
        }

        #endregion

        #region Utility

        private string FormatTimestamp(DateTime timestamp)
        {
            var elapsed = DateTime.UtcNow - timestamp;

            if (elapsed.TotalMinutes < 1) return localizationManager?.Get(AuraTerms.TIME_JUST_NOW, "Just now") ?? "Just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}{localizationManager?.Get(AuraTerms.TIME_M_AGO, "m ago")}";
            if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}{localizationManager?.Get(AuraTerms.TIME_H_AGO, "h ago")}";
            if (elapsed.TotalDays < 7) return $"{(int)elapsed.TotalDays}{localizationManager?.Get(AuraTerms.TIME_D_AGO, "d ago")}";

            return timestamp.ToString("MMM dd");
        }

        private string FormatCount(int count)
        {
            if (count < 1000) return count.ToString();
            if (count < 1000000) return $"{count / 1000f:0.#}K";
            return $"{count / 1000000f:0.#}M";
        }

        #endregion
    }
}
