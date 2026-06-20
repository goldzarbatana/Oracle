using UnityEngine;
using System.Runtime.InteropServices;

namespace TimeAura.Core.Services
{
    /// <summary>
    /// Sacred Touch Service - Handles haptic feedback for premium sensory experiences.
    /// Uses JNI for precise micro-vibrations on Android.
    /// </summary>
    public class HapticService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static AndroidJavaClass _vibrationEffectClass;
        private static int _apiLevel;
#endif

        public HapticService()
        {
            Initialize();
        }

        private void Initialize()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                using (var buildVersion = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _apiLevel = buildVersion.GetStatic<int>("SDK_INT");
                }

                if (_apiLevel >= 26)
                {
                    _vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticService] Failed to initialize Android Vibrator: {e.Message}");
            }
#endif
        }

        /// <summary>
        /// Perform a light mystical tap.
        /// </summary>
        public void LightTap()
        {
            Vibrate(20);
        }

        /// <summary>
        /// Perform a medium ritual impact.
        /// </summary>
        public void MediumTap()
        {
            Vibrate(50);
        }

        /// <summary>
        /// Perform a heavy resonance impact.
        /// </summary>
        public void HeavyTap()
        {
            Vibrate(100);
        }

        /// <summary>
        /// Vibrate for a specific duration in milliseconds.
        /// </summary>
        public void Vibrate(long milliseconds)
        {
#if UNITY_EDITOR
            // No vibration in editor
#elif UNITY_ANDROID
            if (_vibrator == null) return;

            try
            {
                if (_apiLevel >= 26 && _vibrationEffectClass != null)
                {
                    using (var effect = _vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticService] Vibration failed: {e.Message}");
            }
#elif UNITY_IOS
            // Basic iOS vibration (Requires native plugin for Taptic Engine)
            Handheld.Vibrate(); 
#else
            Handheld.Vibrate();
#endif
        }
    }
}
