using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Features.Auth;
using TimeAura.Features.Localization;
using UnityEngine;
using VContainer;

namespace TimeAura.Core
{
    public enum GameState
    {
        Initialising,
        Auth,
        MainHub,
        ActiveQuest
    }

    public sealed class GameManager : MonoBehaviour, IManager
    {
        public event Action<GameState> StateChanged;

        public GameState CurrentState { get; private set; } = GameState.Initialising;
        public bool IsInitialized { get; private set; }
        [Inject]
        private AuthManager authManager;

        [Inject]
        private LocalizationManager localizationManager;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            SetState(GameState.Initialising);

            Debug.Log("[GameManager] Core systems ready. Standing by for Auth.");

            if (authManager == null)
            {
                throw new InvalidOperationException("AuthManager not injected into GameManager.");
            }

            await UniTask.Yield(cancellationToken);
            IsInitialized = true;
            SetState(GameState.Auth);
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        public void EnterActiveQuest()
        {
            SetState(GameState.ActiveQuest);
        }

        public void ReturnToHub()
        {
            SetState(GameState.MainHub);
        }

        private void SetState(GameState state)
        {
            if (CurrentState == state)
            {
                return;
            }

            CurrentState = state;
            StateChanged?.Invoke(state);
            EventBus.Publish(new GameStateChangedEvent(state));
        }
    }
}
