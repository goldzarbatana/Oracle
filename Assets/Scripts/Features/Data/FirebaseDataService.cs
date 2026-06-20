using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Features.Auth;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
using Firebase.Firestore;
#endif

namespace TimeAura.Features.Data
{
    public sealed class FirebaseDataService : MonoBehaviour, IDataService
    {
        private readonly Dictionary<string, UserProfile> profiles = new();
        public bool IsInitialized { get; private set; }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Log("[FirebaseDataService] 📜 Opening the Akashic Records (Firestore)...");
            
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try
            {
                var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();
                if (status == Firebase.DependencyStatus.Available)
                {
                    var app = Firebase.FirebaseApp.DefaultInstance;
                    Debug.Log($"[FirebaseDataService] ✅ Firebase Initialized: {app.Name} (Options: {app.Options.ProjectId})");
                    
                    FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                    db.Settings.PersistenceEnabled = true;
                    _ = db.EnableNetworkAsync();
                    Debug.Log("[FirebaseDataService] ✨ Firestore Network Flow restored.");
                }
                else
                {
                    Debug.LogError($"[FirebaseDataService] ❌ Firebase dependencies failed: {status}. Check your API Key and Google Play Services.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseDataService] ❌ Firebase Init Error: {ex.Message}");
            }
#endif

            // Populate Mock Data for testing the Temple's Resonance
            CreateMockAdept("mock_lyra", "Lyra Starlight", "#FFD700", "Celestial Guide", 24, "Spirit", new List<string>{"Empathy", "Guidance"}, new List<string>{"Stability"});
            CreateMockAdept("mock_kaelen", "Kaelen Voidwalker", "#4B0082", "Void Scholar", 32, "Mind", new List<string>{"Mystery", "Chronos"}, new List<string>{"Connection"});
            CreateMockAdept("mock_serafina", "Serafina Bloom", "#00FF7F", "Life Weaver", 21, "Body", new List<string>{"Healing", "Growth"}, new List<string>{"Resonance"});

            IsInitialized = true;
            await UniTask.Yield();
        }

        private void CreateMockAdept(string id, string name, string color, string title, int age, string pillar, List<string> gifts, List<string> seeks)
        {
            var p = new UserProfile(id, "+000000000", name, 1000, 5);
            p.AuraColorHex = color;
            p.AuraTitle = title;
            p.Age = age;
            p.PrimaryPillar = pillar;
            p.UpdateAura(gifts, seeks, "In the Mirror of Aura, we see the truth.");
            p.CompleteInitiation();
            profiles[id] = p;
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            await UniTask.Yield();
        }

        public async UniTask<UserProfile> GetUserProfileAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            profiles.TryGetValue(userId, out var profile);
            if (profile != null) return profile;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                DocumentReference docRef = db.Collection("users").Document(userId).Collection("profile").Document("essence");
                
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync()
                    .AsUniTask()
                    .Timeout(System.TimeSpan.FromSeconds(5));
                
                if (snapshot.Exists)
                {
                    // Map snapshot to UserProfile
                    var data = snapshot.ToDictionary();
                    
                    // Create basic profile
                    string name = data.ContainsKey("display_name") ? data["display_name"].ToString() : "Unknown Adept";
                    float horas = data.ContainsKey("horas") ? Convert.ToSingle(data["horas"]) : 100f;
                    int status = data.ContainsKey("status") ? Convert.ToInt32(data["status"]) : 0;
                    
                    profile = new UserProfile(userId, "+000000000", name, horas, status);
                    
                    // Populate other fields
                    if (data.ContainsKey("has_completed_initiation") && Convert.ToBoolean(data["has_completed_initiation"]))
                        profile.CompleteInitiation();
                        
                    if (data.ContainsKey("age")) profile.Age = Convert.ToInt32(data["age"]);
                    if (data.ContainsKey("bio")) profile.Bio = data["bio"].ToString();
                    if (data.ContainsKey("location_zone")) profile.LocationZone = data["location_zone"].ToString();
                    if (data.ContainsKey("aura_color_hex")) profile.AuraColorHex = data["aura_color_hex"].ToString();
                    if (data.ContainsKey("aura_title")) profile.AuraTitle = data["aura_title"].ToString();
                    
                    if (data.ContainsKey("aura_gifts") && data["aura_gifts"] is List<object> gifts)
                        profile.UpdateAura(gifts.ConvertAll(o => o.ToString()), profile.AuraSeeks, profile.CustomNote);
                    
                    if (data.ContainsKey("aura_seeks") && data["aura_seeks"] is List<object> seeks)
                        profile.UpdateAura(profile.AuraGifts, seeks.ConvertAll(o => o.ToString()), profile.CustomNote);

                    profiles[userId] = profile;
                    Debug.Log($"[FirebaseDataService] 👤 Profile materialized from Akashic Records: {name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FirebaseDataService] Failed to fetch remote profile for {userId}: {ex.Message}");
            }
#endif

            return profile;
        }

        public async UniTask<List<UserProfile>> GetAllProfilesAsync(CancellationToken cancellationToken)
        {
            await UniTask.Yield();
            return new List<UserProfile>(profiles.Values);
        }

        public async UniTask SaveUserProfileAsync(UserProfile profile, CancellationToken cancellationToken)
        {
            if (profile == null) return;
            if (string.IsNullOrWhiteSpace(profile.UserId))
            {
                string newId = System.Guid.NewGuid().ToString("N");
                Debug.LogWarning($"[FirebaseDataService] ⚠️ Recovered missing UserId for Name: {profile.DisplayName}. Auto-generated ID: {newId}");
                profile.SetUserId(newId);
            }

            profiles[profile.UserId] = profile;
            
#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try 
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                
                // --- Task: Ensure Root Document Exists ---
                // We write a small piece of data to the root users/{uid} document 
                // so it's not "italicized" (virtual) in the Firebase console.
                DocumentReference rootRef = db.Collection("users").Document(profile.UserId);
                var rootMeta = new Dictionary<string, object>
                {
                    { "last_active", FieldValue.ServerTimestamp },
                    { "app_version", Application.version }
                };
                
                if (!string.IsNullOrEmpty(profile.FcmToken))
                {
                    rootMeta["_fcmToken"] = profile.FcmToken;
                }
                
                // Use SetOptions.MergeAll to avoid overwriting 'created_at' if we decide to add it later
                await rootRef.SetAsync(rootMeta, SetOptions.MergeAll).AsUniTask();

                // Path: users/{uid}/profile/essence
                DocumentReference docRef = db.Collection("users").Document(profile.UserId).Collection("profile").Document("essence");
                
                var essenceData = new Dictionary<string, object>
                {
                    { "display_name", profile.DisplayName ?? "" },
                    { "age", profile.Age },
                    { "bio", profile.Bio ?? "" },
                    { "location_zone", profile.LocationZone ?? "" },
                    { "aura_gifts", profile.AuraGifts ?? new List<string>() },
                    { "aura_seeks", profile.AuraSeeks ?? new List<string>() },
                    { "custom_note", profile.CustomNote ?? "" },
                    { "aura_color_hex", profile.AuraColorHex ?? "#FFFFFF" },
                    { "aura_title", profile.AuraTitle ?? "Initiate" },
                    { "horas", profile.Horas },
                    { "status", profile.Status },
                    { "has_completed_initiation", profile.HasCompletedInitiation },
                    { "updated_at", FieldValue.ServerTimestamp }
                };

                await docRef.SetAsync(essenceData, SetOptions.MergeAll)
                    .AsUniTask()
                    .Timeout(System.TimeSpan.FromSeconds(5));
                
                Debug.Log($"[FirebaseDataService] ✨ Essence sealed in Firestore: users/{profile.UserId}/profile/essence");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FirebaseDataService] ❌ Failed to save essence: {ex.Message}");
            }
#else
            Debug.Log($"[FirebaseDataService] Saved profile (Local Storage Only).");
#endif
        }

        // ── Harmony Messaging Implementation ──────────────────────────

        public async UniTask SendHarmonyMessageAsync(string sessionId, Harmony.ChatMessage message)
        {
            if (string.IsNullOrEmpty(sessionId) || message == null) return;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                DocumentReference docRef = db.Collection("harmony_sessions")
                                             .Document(sessionId)
                                             .Collection("messages")
                                             .Document(message.MessageId);

                var data = new Dictionary<string, object>
                {
                    { "sender_id", message.SenderId },
                    { "text", message.Text },
                    { "timestamp", FieldValue.ServerTimestamp },
                    { "type", message.Type.ToString() }
                };

                await docRef.SetAsync(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirebaseDataService] ❌ Message delivery failed: {ex.Message}");
            }
#endif
        }

        public IDisposable ListenToHarmonyMessages(string sessionId, Action<List<Harmony.ChatMessage>> onMessagesChanged)
        {
            if (string.IsNullOrEmpty(sessionId)) return null;

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            Query query = db.Collection("harmony_sessions")
                            .Document(sessionId)
                            .Collection("messages")
                            .OrderBy("timestamp");

            ListenerRegistration registration = query.Listen(snapshot =>
            {
                var messages = new List<Harmony.ChatMessage>();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        string senderId = doc.GetValue<string>("sender_id");
                        string text = doc.GetValue<string>("text");
                        string typeStr = doc.GetValue<string>("type");
                        
                        // Parse timestamp safely
                        DateTime ts = DateTime.UtcNow;
                        if (doc.ContainsField("timestamp") && doc.GetValue<object>("timestamp") != null)
                        {
                            ts = doc.GetValue<Timestamp>("timestamp").ToDateTime();
                        }

                        Enum.TryParse(typeStr, out Harmony.ChatMessageType type);
                        
                        var msg = new Harmony.ChatMessage(senderId, text, type)
                        {
                            MessageId = doc.Id,
                            Timestamp = ts
                        };
                        messages.Add(msg);
                    }
                }
                onMessagesChanged?.Invoke(messages);
            });

            return new FirebaseListenerToken(registration);
#else
            return null;
#endif
        }

        private class FirebaseListenerToken : IDisposable
        {
            private readonly ListenerRegistration _registration;
            public FirebaseListenerToken(ListenerRegistration registration) => _registration = registration;
            public void Dispose() => _registration?.Stop();
        }
    }
}
