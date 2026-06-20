using System;
using UnityEngine;

namespace TimeAura.Features.Social
{
    /// <summary>
    /// User Privacy Settings - controls visibility and private mode.
    /// "Some paths are walked in silence, known only to those chosen."
    /// </summary>
    [Serializable]
    public class PrivacySettings
    {
        public bool isPrivateMode;
        public bool showInConvergence;
        public bool allowMatchingRequests;
        public VisibilityLevel visibilityLevel;
        public string[] guardianIds; // List of Guardian user IDs (trusted friends)

        public PrivacySettings()
        {
            isPrivateMode = false;
            showInConvergence = true;
            allowMatchingRequests = true;
            visibilityLevel = VisibilityLevel.Public;
            guardianIds = Array.Empty<string>();
        }

        /// <summary>
        /// Checks if a user can see this profile.
        /// </summary>
        public bool CanUserView(string userId)
        {
            switch (visibilityLevel)
            {
                case VisibilityLevel.Public:
                    return true;

                case VisibilityLevel.GuardiansOnly:
                    return Array.IndexOf(guardianIds, userId) >= 0;

                case VisibilityLevel.Private:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Adds a Guardian (trusted friend).
        /// </summary>
        public void AddGuardian(string userId)
        {
            if (Array.IndexOf(guardianIds, userId) < 0)
            {
                Array.Resize(ref guardianIds, guardianIds.Length + 1);
                guardianIds[guardianIds.Length - 1] = userId;
            }
        }

        /// <summary>
        /// Removes a Guardian.
        /// </summary>
        public void RemoveGuardian(string userId)
        {
            var list = new System.Collections.Generic.List<string>(guardianIds);
            list.Remove(userId);
            guardianIds = list.ToArray();
        }
    }

    public enum VisibilityLevel
    {
        Public,         // Visible to all
        GuardiansOnly,  // Visible only to Guardians
        Private         // Hidden from everyone
    }

    /// <summary>
    /// Extended Social Manager with Privacy features.
    /// </summary>
    public partial class SocialManager
    {
        private PrivacySettings _privacySettings;

        /// <summary>
        /// Gets current user's privacy settings.
        /// </summary>
        public PrivacySettings GetPrivacySettings()
        {
            if (_privacySettings == null)
            {
                _privacySettings = new PrivacySettings();
            }
            return _privacySettings;
        }

        /// <summary>
        /// Updates privacy settings.
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTask UpdatePrivacySettingsAsync(
            PrivacySettings settings,
            System.Threading.CancellationToken cancellationToken = default)
        {
            _privacySettings = settings;

            // Save to Firestore
            if (_networkService != null && IsLoggedIn)
            {
                try
                {
                    await _networkService.PostAsync<PrivacySettings, object>($"users/{CurrentUser.UserId}/privacy", settings, cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SocialManager] Failed to save privacy to cloud: {ex.Message}");
                }
            }
            await Cysharp.Threading.Tasks.UniTask.Delay(100, cancellationToken: cancellationToken);

            Debug.Log($"[SocialManager] 🔒 Privacy updated: {settings.visibilityLevel}");
        }

        /// <summary>
        /// Enables Private Mode - hides user from Convergence.
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTask EnablePrivateModeAsync(
            System.Threading.CancellationToken cancellationToken = default)
        {
            var settings = GetPrivacySettings();
            settings.isPrivateMode = true;
            settings.showInConvergence = false;
            settings.visibilityLevel = VisibilityLevel.GuardiansOnly;

            await UpdatePrivacySettingsAsync(settings, cancellationToken);

            Debug.Log("[SocialManager] 🌙 Private Mode enabled. You walk in shadow.");
        }

        /// <summary>
        /// Disables Private Mode - shows user publicly.
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTask DisablePrivateModeAsync(
            System.Threading.CancellationToken cancellationToken = default)
        {
            var settings = GetPrivacySettings();
            settings.isPrivateMode = false;
            settings.showInConvergence = true;
            settings.visibilityLevel = VisibilityLevel.Public;

            await UpdatePrivacySettingsAsync(settings, cancellationToken);

            Debug.Log("[SocialManager] 🌟 Private Mode disabled. You are visible to all.");
        }

        /// <summary>
        /// Checks if a user can view another user's profile.
        /// </summary>
        public bool CanViewProfile(string targetUserId, PrivacySettings targetSettings)
        {
            string currentUserId = CurrentUser?.UserId ?? "guest";

            return targetSettings.CanUserView(currentUserId);
        }
    }
}
