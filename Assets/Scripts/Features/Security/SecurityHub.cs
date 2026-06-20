using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.Security
{
    public sealed class SecurityHub : MonoBehaviour, IManager
    {
        public bool IsInitialized { get; private set; }
        [SerializeField] private float watchdogIntervalSeconds = 5f;

        private CancellationTokenSource watchdogCts;
        [Inject]
        private ISmsGateway smsGateway;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (smsGateway == null) throw new InvalidOperationException("ISmsGateway not injected into SecurityHub. Ensure TimeAuraInstaller injects dependencies.");
            watchdogCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = WatchdogLoopAsync(watchdogCts.Token);
            IsInitialized = true;
            await UniTask.Yield(cancellationToken);
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            watchdogCts?.Cancel();
            watchdogCts?.Dispose();
            await UniTask.Yield();
        }

        public UniTask TriggerSmsChallengeAsync(string phoneNumber, CancellationToken cancellationToken)
        {
            return smsGateway.SendSmsAsync(phoneNumber, "TimeAura security check", cancellationToken).AsUniTask();
        }

        private async UniTask WatchdogLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                EventBus.Publish(new WatchdogTickEvent(DateTimeOffset.UtcNow));
                await UniTask.Delay(TimeSpan.FromSeconds(watchdogIntervalSeconds), cancellationToken: cancellationToken);
            }
        }
    }

    public readonly struct WatchdogTickEvent
    {
        public WatchdogTickEvent(DateTimeOffset timestamp)
        {
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }
    }
}
