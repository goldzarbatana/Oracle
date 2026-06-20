using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Data;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.Auth
{
    /// <summary>
    /// InitiationProcessor - Handles first-time user onboarding and Aspect selection
    /// </summary>
    public sealed class InitiationProcessor : MonoBehaviour, IManager
    {
        public bool IsInitialized { get; private set; }
        [Inject] private IDataService dataService;
        [Inject] private AppConfig appConfig;

        public event Action<UserProfile> OnInitiationComplete;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (dataService == null) throw new InvalidOperationException("IDataService not injected into InitiationProcessor.");
            if (appConfig == null) throw new InvalidOperationException("AppConfig not injected into InitiationProcessor.");

            await UniTask.Yield(cancellationToken);
            IsInitialized = true;
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        /// <summary>
        /// Start the Initiation flow: choose nickname and select primary Aspects
        /// </summary>
        public async UniTask<UserProfile> BeginInitiationAsync(string userId, string phoneNumber, CancellationToken cancellationToken)
        {
            var profile = UserProfile.CreateNew(userId, phoneNumber, appConfig.InitialHoras);

            // In real implementation, show UI for nickname and Aspect selection
            // For now, stub values
            profile.SetNickname("Adept_" + userId.Substring(0, 6));
            profile.SetAspect(AspectType.Lumen, 1);
            profile.SetAspect(AspectType.Forma, 1);

            profile.CompleteInitiation();

            await dataService.SaveUserProfileAsync(profile, cancellationToken);

            OnInitiationComplete?.Invoke(profile);
            EventBus.Publish(new InitiationCompletedEvent(profile));

            Debug.Log($"[InitiationProcessor] Initiation completed for {profile.Nickname}. Horas: {profile.Horas}");

            return profile;
        }

        /// <summary>
        /// Check if user has completed initiation
        /// </summary>
        public async Task<bool> HasCompletedInitiationAsync(string userId, CancellationToken cancellationToken)
        {
            var profile = await dataService.GetUserProfileAsync(userId, cancellationToken);
            return profile?.HasCompletedInitiation ?? false;
        }
    }

    public readonly struct InitiationCompletedEvent
    {
        public InitiationCompletedEvent(UserProfile profile)
        {
            Profile = profile;
        }

        public UserProfile Profile { get; }
    }
}
