using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using TimeAura.Core.Services;
using System.Threading;

namespace TimeAura.Features.UI.Oracle
{
    public enum WhisperColor
    {
        Gold,
        Cyan,
        Sapphire
    }

    /// <summary>
    /// Global notification system "Oracle Whispers"
    /// Premium glassmorphism toasts that spawn on top of the UI.
    /// Also handles the "Hidden Ping" background loop.
    /// </summary>
    public class OracleWhisperManager
    {
        private readonly HapticService _hapticService;
        private readonly AudioService _audioService;
        private readonly IAuraOracleService _oracleService;
        
        private VisualElement _whisperContainer;
        private UIDocument _currentDoc;
        private CancellationTokenSource _whisperLoopCts;

        public OracleWhisperManager(HapticService hapticService, AudioService audioService, IAuraOracleService oracleService)
        {
            _hapticService = hapticService;
            _audioService = audioService;
            _oracleService = oracleService;
        }

        public void StartWhisperLoop()
        {
            StopWhisperLoop();
            _whisperLoopCts = new CancellationTokenSource();
            WhisperLoopAsync(_whisperLoopCts.Token).Forget();
        }

        public void StopWhisperLoop()
        {
            if (_whisperLoopCts != null)
            {
                _whisperLoopCts.Cancel();
                _whisperLoopCts.Dispose();
                _whisperLoopCts = null;
            }
        }

        private async UniTaskVoid WhisperLoopAsync(CancellationToken ct)
        {
            Debug.Log("[OracleWhisperManager] 👁️ Background Whisper Loop started.");
            
            while (!ct.IsCancellationRequested)
            {
                // Wait for a random interval between 2 to 5 minutes (for testing, normally 1-4 hours)
                int waitTimeMs = UnityEngine.Random.Range(120000, 300000); 
                
                #if UNITY_EDITOR
                // In Editor, speed it up to 30-60 seconds for debugging
                waitTimeMs = UnityEngine.Random.Range(30000, 60000);
                #endif
                
                await UniTask.Delay(waitTimeMs, cancellationToken: ct);
                
                if (ct.IsCancellationRequested) break;
                
                try 
                {
                    Debug.Log("[OracleWhisperManager] 🌌 Publishing background whisper loop check...");
                    EventBus.Publish(new NewbieStateTriggeredEvent("PeriodicWhisperCheck", "Background loop ping"));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OracleWhisperManager] Hidden ping trigger failed: {e.Message}");
                }
            }
        }

        private void EnsureContainer()
        {
            if (_currentDoc == null || !_currentDoc.gameObject.activeInHierarchy)
            {
                _currentDoc = UnityEngine.Object.FindAnyObjectByType<UIDocument>(FindObjectsInactive.Exclude);
            }

            if (_currentDoc == null || _currentDoc.rootVisualElement == null) return;

            var root = _currentDoc.rootVisualElement;
            _whisperContainer = root.Q<VisualElement>("OracleWhisperContainer");

            if (_whisperContainer == null)
            {
                _whisperContainer = new VisualElement { name = "OracleWhisperContainer" };
                _whisperContainer.style.position = Position.Absolute;
                _whisperContainer.style.top = 60; // Just below the top safe area
                _whisperContainer.style.width = Length.Percent(100);
                _whisperContainer.style.alignItems = Align.Center;
                _whisperContainer.pickingMode = PickingMode.Ignore;
                
                var screenRoot = root.Q("ScreenRoot") ?? root;
                screenRoot.Add(_whisperContainer);
                _whisperContainer.BringToFront();
            }
        }

        public void ShowWhisper(string text, WhisperColor colorType = WhisperColor.Gold)
        {
            EnsureContainer();
            if (_whisperContainer == null) return;

            _hapticService?.MediumTap();
            _audioService?.PlaySFX("OracleMessage");

            var toast = new VisualElement();
            toast.AddToClassList("oracle-whisper-toast");
            
            string glowClass = colorType switch
            {
                WhisperColor.Cyan => "aura-glow--cyan",
                WhisperColor.Sapphire => "aura-glow--sapphire",
                _ => "aura-glow--gold"
            };
            
            toast.AddToClassList(glowClass);

            var icon = new Label("👁️");
            icon.style.fontSize = 24;
            icon.style.marginRight = 10;
            
            var message = new Label(text);
            message.style.color = Color.white;
            message.style.fontSize = 18;
            message.style.whiteSpace = WhiteSpace.Normal;
            
            toast.Add(icon);
            toast.Add(message);

            toast.style.opacity = 0;
            toast.style.translate = new Translate(0, -50, 0);
            toast.style.transitionProperty = new StyleList<StylePropertyName>(new List<StylePropertyName> 
            { 
                new StylePropertyName("opacity"), 
                new StylePropertyName("translate") 
            });
            toast.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(0.5f, TimeUnit.Second) });

            _whisperContainer.Add(toast);

            AnimateToastAsync(toast).Forget();
        }

        private async UniTaskVoid AnimateToastAsync(VisualElement toast)
        {
            await UniTask.Yield();
            if (toast == null) return;
            
            toast.style.opacity = 1;
            toast.style.translate = new Translate(0, 0, 0);

            await UniTask.Delay(5500); // Wait 5.5 seconds so user can read it

            if (toast == null) return;

            toast.style.opacity = 0;
            toast.style.translate = new Translate(0, -20, 0);

            await UniTask.Delay(500);

            if (toast != null && toast.parent != null)
            {
                toast.RemoveFromHierarchy();
            }
        }
    }
}
