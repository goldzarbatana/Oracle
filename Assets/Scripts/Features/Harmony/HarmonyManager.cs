using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Data;
using TimeAura.Features.Social;
using UnityEngine;
using VContainer;

namespace TimeAura.Features.Harmony
{
    /// <summary>
    /// Manages active Harmony sessions - the sacred exchange of Horas between Masters.
    /// "In the flow of time, two souls become one rhythm."
    /// </summary>
    public class HarmonyManager : IManager
    {
        public bool IsInitialized { get; private set; }
        private readonly IDataService _dataService;
        private readonly SocialManager _socialManager;

        [Inject]
        public HarmonyManager(IDataService dataService, SocialManager socialManager)
        {
            _dataService = dataService;
            _socialManager = socialManager;
        }

        private HarmonySession _currentSession;
        private readonly Dictionary<string, HarmonySession> _activeSessions = new();

        public event Action<HarmonySession> OnSessionStarted;
        public event Action<HarmonySession> OnSessionCompleted;
        public event Action<HarmonySession> OnSessionDissolved;
        public event Action<float> OnProgressUpdated;

        public HarmonySession CurrentSession => _currentSession;
        public bool HasActiveSession => _currentSession != null && _currentSession.status == HarmonyStatus.ActiveChannel;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[HarmonyManager] 🌊 The Flow of Harmony awakens...");
            await LoadPendingSessions(cancellationToken);
            IsInitialized = true;
            Debug.Log("[HarmonyManager] ✨ Harmony channels are now open.");
        }

        public async UniTask ShutdownAsync()
        {
            foreach (var session in _activeSessions.Values)
            {
                if (session.status == HarmonyStatus.ActiveChannel)
                {
                    session.Dissolve();
                }
            }
            _activeSessions.Clear();
            _currentSession = null;
            IsInitialized = false;

            await UniTask.Yield();
            Debug.Log("[HarmonyManager] 🌙 Harmony channels closed.");
        }

        public async UniTask<HarmonySession> StartHarmonyAsync(
            string recipientUserId,
            int horasToExchange,
            CancellationToken cancellationToken = default)
        {
            if (HasActiveSession)
            {
                Debug.LogWarning("[HarmonyManager] ⚠️ Already in an active cycle of Harmony.");
                return null;
            }

            try
            {
                string currentUserId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser?.UserId ?? "unknown";

                var session = new HarmonySession(currentUserId, recipientUserId, horasToExchange);
                _currentSession = session;
                _activeSessions[session.sessionId] = session;

                await SaveSessionAsync(session, cancellationToken);
                await NotifyRecipientAsync(session, cancellationToken);

                OnSessionStarted?.Invoke(session);
                Debug.Log($"[HarmonyManager] 🌟 Harmony initiated: {horasToExchange} Horas → {recipientUserId}");

                TrackProgress(session, cancellationToken).Forget();
                return session;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HarmonyManager] ❌ Failed to initiate Harmony: {ex.Message}");
                return null;
            }
        }

        public async UniTask<bool> CompleteHarmonyAsync(
            ResonanceLevel resonance,
            CancellationToken cancellationToken = default)
        {
            if (!HasActiveSession) return false;

            try
            {
                _currentSession.Complete(resonance);

                await UpdateHorasAsync(_currentSession, cancellationToken);
                await SaveResonanceAsync(_currentSession, cancellationToken);

                OnSessionCompleted?.Invoke(_currentSession);
                Debug.Log($"[HarmonyManager] ✨ Harmony cycle complete. Resonance: {resonance}");

                _activeSessions.Remove(_currentSession.sessionId);
                _currentSession = null;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HarmonyManager] ❌ Failed to seal Harmony: {ex.Message}");
                return false;
            }
        }

        public async UniTask DissolveHarmonyAsync(CancellationToken cancellationToken = default)
        {
            if (!HasActiveSession) return;

            _currentSession.Dissolve();
            await SaveSessionAsync(_currentSession, cancellationToken);

            OnSessionDissolved?.Invoke(_currentSession);
            Debug.Log("[HarmonyManager] 🚫 Harmony dissolved.");

            _activeSessions.Remove(_currentSession.sessionId);
            _currentSession = null;
        }

        private async UniTaskVoid TrackProgress(HarmonySession session, CancellationToken cancellationToken)
        {
            while (session.status == HarmonyStatus.ActiveChannel && !cancellationToken.IsCancellationRequested)
            {
                OnProgressUpdated?.Invoke(session.Progress);
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
        }

        private async UniTask LoadPendingSessions(CancellationToken cancellationToken) => await UniTask.Yield(cancellationToken);
        private async UniTask SaveSessionAsync(HarmonySession session, CancellationToken cancellationToken) => await UniTask.Delay(100, cancellationToken: cancellationToken);
        private async UniTask NotifyRecipientAsync(HarmonySession session, CancellationToken cancellationToken) => await UniTask.Delay(100, cancellationToken: cancellationToken);
        private async UniTask UpdateHorasAsync(HarmonySession session, CancellationToken cancellationToken) => await UniTask.Delay(100, cancellationToken: cancellationToken);
        private async UniTask SaveResonanceAsync(HarmonySession session, CancellationToken cancellationToken) => await UniTask.Delay(100, cancellationToken: cancellationToken);
    }
}
