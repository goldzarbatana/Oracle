using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Firebase.Messaging;
using TimeAura.Features.Data;
using TimeAura.Features.Auth;

namespace TimeAura.Core.Services
{
    public interface IPushNotificationService : IManager
    {
        string CurrentToken { get; }
    }

    /// <summary>
    /// Handles Firebase Cloud Messaging (FCM) push notifications and device tokens.
    /// </summary>
    public class PushNotificationService : IPushNotificationService
    {
        public bool IsInitialized { get; private set; }
        public string CurrentToken { get; private set; }

        private readonly IDataService _dataService;
        private readonly AuthManager _authManager;

        public PushNotificationService(IDataService dataService, AuthManager authManager)
        {
            _dataService = dataService;
            _authManager = authManager;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            try
            {
                FirebaseMessaging.TokenReceived += OnTokenReceived;
                FirebaseMessaging.MessageReceived += OnMessageReceived;
                
                // Trigger token fetch
                var token = await FirebaseMessaging.GetTokenAsync().AsUniTask();
                CurrentToken = token;
                Debug.Log($"[PushNotificationService] 📱 FCM Token acquired: {CurrentToken}");

                // If user is already authenticated during boot, save it
                if (_authManager != null && _authManager.CurrentProfile != null)
                {
                    await SaveTokenToProfileAsync();
                }

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PushNotificationService] ⚠️ Failed to initialize FCM: {ex.Message}. Make sure Firebase Messaging package is imported.");
            }
        }

        public async UniTask ShutdownAsync()
        {
            FirebaseMessaging.TokenReceived -= OnTokenReceived;
            FirebaseMessaging.MessageReceived -= OnMessageReceived;
            IsInitialized = false;
            await UniTask.Yield();
        }

        private async void OnTokenReceived(object sender, TokenReceivedEventArgs token)
        {
            Debug.Log($"[PushNotificationService] 🔄 FCM Token updated: {token.Token}");
            CurrentToken = token.Token;
            
            if (_authManager != null && _authManager.CurrentProfile != null)
            {
                await SaveTokenToProfileAsync();
            }
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Debug.Log($"[PushNotificationService] ✉️ Received message: {e.Message.Notification?.Title} - {e.Message.Notification?.Body}");
            // Handle foreground message visual here if we want to bypass the OS default
        }

        private async UniTask SaveTokenToProfileAsync()
        {
            if (string.IsNullOrEmpty(CurrentToken)) return;
            
            // In a real scenario, you'd add an fcmToken field to UserProfile and update it via DataService
            // For now, we will just log it. (Ensure UserProfile has 'public string FcmToken { get; set; }')
            Debug.Log($"[PushNotificationService] 💾 Token ready to be saved for user {_authManager.CurrentProfile.UserId}");
            
            // Let's assume we update the UserProfile here:
            _authManager.CurrentProfile.FcmToken = CurrentToken;
            await _dataService.SaveUserProfileAsync(_authManager.CurrentProfile, CancellationToken.None);
        }
    }
}
