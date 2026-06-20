using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Data;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.Auth
{
    public sealed class AuthManager : MonoBehaviour, IManager
    {
        public bool IsInitialized { get; private set; }
        [Inject]
        private IDataService dataService;

        [Inject]
        private AppConfig appConfig;

        public UserProfile CurrentProfile { get; private set; }
        public bool IsCurrentSessionNewUser { get; private set; }

        [Header("Development Tools")]
        [SerializeField] private bool forceNewUserTest;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            // Expect Zenject to inject dependencies. If missing, throw to fail fast during development.
            if (dataService == null) throw new System.InvalidOperationException("IDataService not injected into AuthManager. Ensure TimeAuraInstaller injects dependencies.");
            if (appConfig == null) throw new System.InvalidOperationException("AppConfig not injected into AuthManager. Ensure TimeAuraInstaller injects dependencies.");

            await UniTask.Yield(cancellationToken);
            IsInitialized = true;
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        public async UniTask<AuthFlowResult> VerifyPhone(string phone, CancellationToken ct = default)
        {
            Debug.Log($"[AuthManager] Verifying phone: {phone}");
            // In a real app this starts the SMS/Firebase flow. 
            // Here we proceed to the full authentication process.
            return await AuthenticateAsync(ct);
        }

        public async UniTask<AuthFlowResult> AuthenticateAsync(CancellationToken cancellationToken)
        {
            var session = await RequestPhoneAuthAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return AuthFlowResult.CancelledResult;
            }

            var profile = await dataService.GetUserProfileAsync(session.UserId, cancellationToken);
            var isNew = profile == null;
            if (profile == null)
            {
                profile = UserProfile.CreateNew(session.UserId, session.PhoneNumber, appConfig.InitialHoras);
                await dataService.SaveUserProfileAsync(profile, cancellationToken);
            }

            CurrentProfile = profile;
            IsCurrentSessionNewUser = isNew || forceNewUserTest;

            // Persist session for development/offline
            PlayerPrefs.SetString("TimeAura_LastUserId", profile.UserId);
            PlayerPrefs.Save();

            var result = new AuthFlowResult(profile, IsCurrentSessionNewUser);
            EventBus.Publish(new AuthCompletedEvent(result));
            return result;
        }

        public async UniTask<bool> CheckSessionAsync(CancellationToken ct = default)
        {
            Debug.Log("[AuthManager] 🔍 Looking for existing session in the ether...");
            
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            // In a real implementation:
            // var user = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser;
            // if (user != null) { ... load profile ... return true; }
            #endif

            await UniTask.Delay(TimeSpan.FromSeconds(0.3f), cancellationToken: ct);
            
            if (forceNewUserTest) return false;

            if (CurrentProfile != null) return true;

            // Try to restore from persistence
            string savedId = PlayerPrefs.GetString("TimeAura_LastUserId", "");
            if (!string.IsNullOrEmpty(savedId))
            {
                Debug.Log($"[AuthManager] 🕯️ Restoring session for: {savedId}");
                var profile = await dataService.GetUserProfileAsync(savedId, ct);
                if (profile != null && !string.IsNullOrWhiteSpace(profile.UserId))
                {
                    #if UNITY_EDITOR
                    if (!profile.HasCompletedInitiation)
                    {
                        Debug.Log("[AuthManager] 🛠️ DEV: Auto-completing initiation to jump straight to Nexus.");
                        profile.CompleteInitiation();
                    }
                    #endif
                    
                    CurrentProfile = profile;
                    return true;
                }
            }
            
            return false;
        }

        public void SignOut()
        {
            Debug.Log("[AuthManager] Clearing session and returning to the Void.");
            CurrentProfile = null;
            IsCurrentSessionNewUser = false;
            PlayerPrefs.DeleteKey("TimeAura_LastUserId");
            PlayerPrefs.Save();
        }

        private async UniTask<AuthSession> RequestPhoneAuthAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[AuthManager] Starting phone auth (stub).");
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);

            var userId = Guid.NewGuid().ToString("N");
            var phoneNumber = "+0000000000";
            return new AuthSession(userId, phoneNumber);
        }
    }

    public readonly struct AuthSession
    {
        public AuthSession(string userId, string phoneNumber)
        {
            UserId = userId;
            PhoneNumber = phoneNumber;
        }

        public string UserId { get; }
        public string PhoneNumber { get; }
    }

    public readonly struct AuthFlowResult
    {
        // Named differently from the instance property to avoid name collision
        public static readonly AuthFlowResult CancelledResult = new(null, false, true);

        public AuthFlowResult(UserProfile profile, bool isNew, bool cancelled = false)
        {
            Profile = profile;
            IsNew = isNew;
            Cancelled = cancelled;
        }

        public UserProfile Profile { get; }
        public bool IsNew { get; }
        public bool Cancelled { get; }
    }
}
