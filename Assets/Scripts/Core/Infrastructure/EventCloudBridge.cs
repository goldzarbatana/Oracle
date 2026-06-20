using System;
using System.Collections.Generic;
using TimeAura.Core;
using TimeAura.Features.Auth;
using TimeAura.Features.Harmony;
using TimeAura.Features.Localization;
using TimeAura.Features.Security;
using TimeAura.Features.Matching;
using UnityEngine;
using VContainer.Unity;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using Firebase.Firestore;
using Firebase;
#endif

namespace TimeAura.Core.Infrastructure
{
    public class EventCloudBridge : IStartable, IDisposable
    {
        private readonly AuthManager _authManager;
        private readonly List<IDisposable> _subscriptions = new();
        private IDisposable _actionsListener;
        private DateTime _launchTime;

        public EventCloudBridge(AuthManager authManager)
        {
            _authManager = authManager;
            _launchTime = DateTime.UtcNow;
        }

        public void Start()
        {
            Debug.Log("[EventCloudBridge] 🌌 Establishing the Optimized Bifrost Event Bridge...");

            // 🔴 LEVEL 1: Local-only events (Zero Cloud Function cost, updates database profile directly)
            RegisterSubscription(EventBus.Subscribe<LanguageChangedEvent>(e =>
                HandleLanguageChangedLocal(e)));

            // 🟡 LEVEL 2: Accumulative events (Written to user's events_log subcollection, does NOT trigger Cloud Function)
            RegisterSubscription(EventBus.Subscribe<GameStateChangedEvent>(e =>
                SendAccumulativeLog("GameStateChanged", JsonUtility.ToJson(e))));

            RegisterSubscription(EventBus.Subscribe<NewbieStateTriggeredEvent>(e =>
                SendAccumulativeLog("NewbieStateTriggered", JsonUtility.ToJson(e))));

            RegisterSubscription(EventBus.Subscribe<AuthCompletedEvent>(e =>
            {
                SendAccumulativeLog("AuthCompleted", JsonUtility.ToJson(e));
                if (e.Result.Profile != null)
                {
                    StartListeningToActions(e.Result.Profile.UserId);
                }
            }));

            RegisterSubscription(EventBus.Subscribe<InitiationCompletedEvent>(e =>
                SendAccumulativeLog("InitiationCompleted", JsonUtility.ToJson(e))));

            RegisterSubscription(EventBus.Subscribe<HarmonyStartedEvent>(e =>
                SendAccumulativeLog("HarmonyStarted", JsonUtility.ToJson(e))));

            // 🟢 LEVEL 3: Critical business events (Triggers Cloud Function & Gemini AI immediately)
            RegisterSubscription(EventBus.Subscribe<ContractCreatedEvent>(e =>
                SendCriticalTrigger("ContractCreated", JsonUtility.ToJson(e))));

            RegisterSubscription(EventBus.Subscribe<DisputeRaisedEvent>(e =>
                SendCriticalTrigger("DisputeRaised", JsonUtility.ToJson(e))));

            RegisterSubscription(EventBus.Subscribe<VoiceCommandReceivedEvent>(e =>
                SendCriticalTrigger("VoiceCommandReceived", JsonUtility.ToJson(e))));

            // If already authenticated at boot, start listening immediately
            if (_authManager != null && _authManager.CurrentProfile != null)
            {
                StartListeningToActions(_authManager.CurrentProfile.UserId);
            }
        }

        private void RegisterSubscription(IDisposable sub)
        {
            if (sub != null)
            {
                _subscriptions.Add(sub);
            }
        }

        // 🔴 Level 1 Handler: Updates Firestore profile directly (No event documents, zero function cost)
        private async void HandleLanguageChangedLocal(LanguageChangedEvent e)
        {
            string userId = _authManager?.CurrentProfile?.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            string langString = e.Language.ToString();
            Debug.Log($"[EventCloudBridge] 🔴 Level 1: LanguageChanged. Updating Firestore profile language to: {langString}");

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                DocumentReference docRef = db.Collection("users").Document(userId).Collection("profile").Document("essence");
                
                // Directly merge the language preference into the user's profile essence
                await docRef.UpdateAsync("language", langString);
                Debug.Log("[EventCloudBridge] Language setting synced with Firestore profile.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventCloudBridge] Failed to update language in Firestore profile: {ex.Message}");
            }
#else
            await System.Threading.Tasks.Task.Yield();
#endif
        }

        // 🟡 Level 2 Handler: Saves logs in a per-user collection (No function trigger, perfect for budget context)
        private async void SendAccumulativeLog(string type, string jsonPayload)
        {
            string userId = _authManager?.CurrentProfile?.UserId ?? "anonymous";
            if (userId == "anonymous") return;

            Debug.Log($"[EventCloudBridge] 🟡 Level 2: Logging accumulative event '{type}' in user history logs...");

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                // Write into per-user logs subcollection users/{userId}/events_log/{logId}
                DocumentReference logRef = db.Collection("users").Document(userId).Collection("events_log").Document();
                
                var data = new Dictionary<string, object>
                {
                    { "type", type },
                    { "payload", jsonPayload },
                    { "timestamp", FieldValue.ServerTimestamp }
                };

                await logRef.SetAsync(data);
                Debug.Log($"[EventCloudBridge] Accumulative event logged in users/{userId}/events_log/{logRef.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventCloudBridge] Failed to write event log to Firestore: {ex.Message}");
            }
#else
            await System.Threading.Tasks.Task.Yield();
#endif
        }

        // 🟢 Level 3 Handler: Writes to global /events to trigger Cloud Functions and Gemini
        private async void SendCriticalTrigger(string type, string jsonPayload)
        {
            string userId = _authManager?.CurrentProfile?.UserId ?? "anonymous";
            Debug.Log($"[EventCloudBridge] 🟢 Level 3: Dispatching critical event '{type}'! Activating AI Orchestrator...");

            if (userId != "anonymous" && _actionsListener == null)
            {
                Debug.Log("[EventCloudBridge] Action listener was not active. Starting it now before sending critical trigger...");
                StartListeningToActions(userId);
            }

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                DocumentReference docRef = db.Collection("events").Document();
                
                var data = new Dictionary<string, object>
                {
                    { "type", type },
                    { "payload", jsonPayload },
                    { "userId", userId },
                    { "timestamp", FieldValue.ServerTimestamp }
                };

                await docRef.SetAsync(data);
                Debug.Log($"[EventCloudBridge] 🚀 Critical trigger saved: events/{docRef.Id}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventCloudBridge] Failed to write critical trigger to Firestore: {ex.Message}");
            }
#else
            await System.Threading.Tasks.Task.Yield();
#endif
        }

        private void StartListeningToActions(string userId)
        {
            _actionsListener?.Dispose();
            _actionsListener = null;

            if (string.IsNullOrEmpty(userId)) return;

            Debug.Log($"[EventCloudBridge] 🎧 Opening action channels for Adept: {userId}...");

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                
                // Query only actions meant for the logged-in user, created since the listener opened
                Timestamp startTimestamp = Timestamp.FromDateTime(_launchTime);
                
                Query query = db.Collection("actions")
                                .WhereEqualTo("userId", userId);

                _actionsListener = new FirebaseListenerToken(query.Listen(snapshot =>
                {
                    foreach (DocumentChange change in snapshot.GetChanges())
                    {
                        if (change.ChangeType == DocumentChange.Type.Added)
                        {
                            DocumentSnapshot doc = change.Document;
                            if (doc.Exists)
                            {
                                // Filter locally to avoid needing a composite index in Firestore
                                Timestamp docTimestamp = doc.ContainsField("timestamp") ? doc.GetValue<Timestamp>("timestamp") : Timestamp.GetCurrentTimestamp();
                                if (docTimestamp.CompareTo(startTimestamp) >= 0)
                                {
                                    string actionType = doc.GetValue<string>("type");
                                    string payloadJson = doc.ContainsField("payload") ? doc.GetValue<string>("payload") : "{}";

                                    Debug.Log($"[EventCloudBridge] ⚡ Inbound Action Received: {actionType}");
                                    EventBus.Publish(new CloudActionEvent(actionType, payloadJson));
                                }
                            }
                        }
                    }
                }));
                Debug.Log($"[EventCloudBridge] 🎧 Active streaming for actions established.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventCloudBridge] ❌ Failed to establish action listener: {ex.Message}");
            }
#endif
        }

        public void Dispose()
        {
            Debug.Log("[EventCloudBridge] 🌌 Dismantling Bifrost Event Bridge.");
            foreach (var sub in _subscriptions)
            {
                sub?.Dispose();
            }
            _subscriptions.Clear();
            _actionsListener?.Dispose();
        }

        private class FirebaseListenerToken : IDisposable
        {
            private readonly ListenerRegistration _registration;
            public FirebaseListenerToken(ListenerRegistration registration) => _registration = registration;
            public void Dispose() => _registration?.Stop();
        }
    }
}
