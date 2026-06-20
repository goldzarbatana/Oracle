using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TimeAura.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Features.UI
{
    /// <summary>
    /// UIManager - Manages screen stack and transitions (Luxury Style)
    /// </summary>
    public sealed class UIManager : MonoBehaviour, IManager
    {
        public bool IsInitialized { get; private set; }
        public float GlobalScale { get; private set; } = 1.0f;
        private readonly Stack<IScreen> screenStack = new();
        public event Action<IScreen> OnScreenPushed;
        public event Action<IScreen> OnScreenPopped;

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            GlobalScale = PlayerPrefs.GetFloat("UIScale", 1.0f);
            Debug.Log($"[UIManager] Initialized. Scale: {GlobalScale}");
            IsInitialized = true;
            await UniTask.Yield(cancellationToken);
        }

        public void SetGlobalScale(float scale)
        {
            GlobalScale = scale;
            PlayerPrefs.SetFloat("UIScale", scale);
            
            // Apply to all active UIDocuments in the scene
            var docs = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var doc in docs)
            {
                if (doc.panelSettings != null)
                {
                    // Scale the Panel
                    doc.panelSettings.scale = scale;
                }
                
                if (doc.rootVisualElement != null)
                {
                    // Adaptive Typography: Scale base font size (16px base)
                    doc.rootVisualElement.style.fontSize = new Length(16f * scale, LengthUnit.Pixel);
                }
            }
        }

        public async UniTask ShutdownAsync()
        {
            IsInitialized = false;
            while (screenStack.Count > 0)
            {
                var screen = screenStack.Pop();
                screen.Hide();
            }

            await UniTask.Yield();
        }

        public void PushScreen(IScreen screen)
        {
            if (screenStack.Count > 0)
            {
                screenStack.Peek().Hide();
            }

            screenStack.Push(screen);
            screen.Show();
            OnScreenPushed?.Invoke(screen);
        }

        public void PopScreen()
        {
            if (screenStack.Count == 0)
            {
                return;
            }

            var screen = screenStack.Pop();
            screen.Hide();
            OnScreenPopped?.Invoke(screen);

            if (screenStack.Count > 0)
            {
                screenStack.Peek().Show();
            }
        }

        public IScreen CurrentScreen => screenStack.Count > 0 ? screenStack.Peek() : null;

        // ── UX AUDIT #5, #6, #14 ────────────────────────────────────────────────

        /// <summary>
        /// Shows a transient toast notification at the bottom of the screen.
        /// UX Audit #5: Used for terminology hints and system feedback.
        /// </summary>
        public void ShowToast(string message, string styleClass = "default", float durationSeconds = 3.0f)
        {
            var activeDoc = GetActiveMainDocument();
            if (activeDoc == null || activeDoc.rootVisualElement == null)
            {
                Debug.Log($"[UIManager] TOAST ({styleClass}): {message}");
                return;
            }

            var root = activeDoc.rootVisualElement;

            var toast = new VisualElement();
            toast.name = "UIToast";
            toast.AddToClassList("ui-toast");
            toast.AddToClassList($"ui-toast--{styleClass}");
            toast.style.position = Position.Absolute;
            toast.style.bottom = new Length(40, LengthUnit.Pixel);
            toast.style.left = new Length(10, LengthUnit.Percent);
            toast.style.right = new Length(10, LengthUnit.Percent);
            toast.style.backgroundColor = styleClass == "error"
                ? new StyleColor(new Color(0.6f, 0.05f, 0.1f, 0.92f))
                : new StyleColor(new Color(0.08f, 0.08f, 0.14f, 0.92f));
            toast.style.paddingTop = toast.style.paddingBottom = 12;
            toast.style.paddingLeft = toast.style.paddingRight = 20;
            toast.style.borderTopLeftRadius = toast.style.borderTopRightRadius =
            toast.style.borderBottomLeftRadius = toast.style.borderBottomRightRadius = 12;
            toast.style.borderTopWidth = toast.style.borderBottomWidth =
            toast.style.borderLeftWidth = toast.style.borderRightWidth = 1;
            var bclr = styleClass == "error"
                ? new StyleColor(new Color(0.8f, 0.2f, 0.2f, 0.7f))
                : new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.5f));
            toast.style.borderTopColor = toast.style.borderBottomColor =
            toast.style.borderLeftColor = toast.style.borderRightColor = bclr;
            toast.style.alignItems = Align.Center;
            toast.pickingMode = PickingMode.Ignore;
            toast.style.opacity = 0;

            var lbl = new Label(message);
            lbl.style.whiteSpace = WhiteSpace.Normal;
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbl.style.fontSize = 14;
            lbl.style.color = new StyleColor(new Color(0.95f, 0.95f, 0.95f, 1f));
            toast.Add(lbl);
            root.Add(toast);

            DismissToastAsync(toast, durationSeconds).Forget();
        }

        public void ShowContextHint(string contextId, string message, float durationSeconds = 5.0f)
        {
            if (PlayerPrefs.GetInt($"Hint_{contextId}", 0) == 1) return;
            PlayerPrefs.SetInt($"Hint_{contextId}", 1);
            PlayerPrefs.Save();
            
            Debug.Log($"[UIManager] 💡 ShowContextHint (Panel/ID): {contextId} - Message: {message}");
            ShowToast($"💡 {message}", "hint", durationSeconds);
        }

        private async UniTaskVoid DismissToastAsync(VisualElement toast, float durationSeconds)
        {
            for (int i = 0; i <= 10; i++) { if (toast.parent == null) return; toast.style.opacity = i / 10f; await UniTask.Delay(30); }
            await UniTask.Delay((int)(durationSeconds * 1000));
            for (int i = 10; i >= 0; i--) { if (toast.parent == null) return; toast.style.opacity = i / 10f; await UniTask.Delay(30); }
            if (toast.parent != null) toast.RemoveFromHierarchy();
        }

        /// <summary>
        /// UX Audit #5: Shows a terminology hint the first time the user encounters a term.
        /// Saves to PlayerPrefs so it only appears once per device.
        /// Format: "🔮 TERM\n💡 Plain explanation"
        /// </summary>
        public void ShowContextHint(string term, string explanation, string playerPrefsKey = null)
        {
            if (!string.IsNullOrEmpty(playerPrefsKey) && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1) return;
            if (!string.IsNullOrEmpty(playerPrefsKey)) PlayerPrefs.SetInt(playerPrefsKey, 1);
            
            Debug.Log($"[UIManager] 🔮 ShowContextHint (Terminology): {term} - Explanation: {explanation} (Key: {playerPrefsKey})");
            ShowToast($"\U0001f52e {term}\n\U0001f4a1 {explanation}", "hint", 5.0f);
        }

        /// <summary>
        /// UX Audit #14: Shows a full-screen onboarding modal with title, explanation, and a confirm button.
        /// </summary>
        public void ShowOnboardingStep(string title, string body, System.Action onConfirm = null)
        {
            Debug.Log($"[UIManager] 🌌 ShowOnboardingStep (Modal): {title}");
            var activeDoc = GetActiveMainDocument();
            if (activeDoc == null || activeDoc.rootVisualElement == null)
            {
                Debug.LogWarning("[UIManager] Onboarding step requested but main UIDocument is not ready.");
                return; 
            }

            var root = activeDoc.rootVisualElement;
            root.Q("OnboardingOverlay")?.RemoveFromHierarchy();

            var overlay = new VisualElement { name = "OnboardingOverlay" };
            overlay.style.position = Position.Absolute;
            overlay.style.top = overlay.style.bottom = overlay.style.left = overlay.style.right = 0;
            overlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.75f));
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            var card = new VisualElement();
            card.style.backgroundColor = new StyleColor(new Color(0.07f, 0.07f, 0.13f, 0.97f));
            card.style.paddingTop = card.style.paddingBottom = 32;
            card.style.paddingLeft = card.style.paddingRight = 28;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 16;
            card.style.borderTopWidth = card.style.borderBottomWidth =
            card.style.borderLeftWidth = card.style.borderRightWidth = 1;
            card.style.borderTopColor = card.style.borderBottomColor =
            card.style.borderLeftColor = card.style.borderRightColor =
                new StyleColor(new Color(0.83f, 0.68f, 0.21f, 0.5f));
            card.style.width = new Length(85, LengthUnit.Percent);
            card.style.maxWidth = 420;

            var titleLbl = new Label(title);
            titleLbl.style.fontSize = 20;
            titleLbl.style.color = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 1f));
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            titleLbl.style.marginBottom = 16;
            titleLbl.style.whiteSpace = WhiteSpace.Normal;

            var bodyLbl = new Label(body);
            bodyLbl.style.fontSize = 15;
            bodyLbl.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f, 0.88f));
            bodyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            bodyLbl.style.whiteSpace = WhiteSpace.Normal;
            bodyLbl.style.marginBottom = 28;

            var btn = new Button();
            btn.text = "ЗРОЗУМІЛО \u2726";
            btn.style.height = 48;
            btn.style.fontSize = 15;
            btn.style.backgroundColor = new StyleColor(new Color(0.83f, 0.68f, 0.21f, 1f));
            btn.style.color = new StyleColor(new Color(0.05f, 0.05f, 0.1f, 1f));
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius =
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 10;
            
            // Prevent accidental fast-clicking/event propagation
            btn.SetEnabled(false);
            UniTask.Delay(500, true).ContinueWith(() => {
                if (btn != null) btn.SetEnabled(true);
            }).Forget();

            btn.clicked += () => {
                Debug.Log($"[UIManager] 💎 Onboarding button clicked: {title}");
                overlay.RemoveFromHierarchy();
                onConfirm?.Invoke();
            };

            card.Add(titleLbl);
            card.Add(bodyLbl);
            card.Add(btn);
            overlay.Add(card);
            root.Add(overlay);
        }

        // ── ERROR POPUP ──────────────────────────────────────────────────────────

        public void ShowErrorPopup(string title, string message)
        {
            Debug.LogError($"[UIManager] ERROR POPUP - {title}: {message}");
            
            var activeDoc = GetActiveMainDocument();
            if (activeDoc != null && activeDoc.rootVisualElement != null)
            {
                var overlay = new VisualElement();
                overlay.style.position = Position.Absolute;
                overlay.style.top = 0;
                overlay.style.bottom = 0;
                overlay.style.left = 0;
                overlay.style.right = 0;
                overlay.style.backgroundColor = new Color(0, 0, 0, 0.8f);
                overlay.style.justifyContent = Justify.Center;
                overlay.style.alignItems = Align.Center;

                var popup = new VisualElement();
                popup.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
                popup.style.paddingTop = popup.style.paddingBottom = popup.style.paddingLeft = popup.style.paddingRight = 20;
                popup.style.borderTopLeftRadius = popup.style.borderTopRightRadius = popup.style.borderBottomLeftRadius = popup.style.borderBottomRightRadius = 10;
                popup.style.width = new Length(80, LengthUnit.Percent);
                popup.style.maxWidth = 400;

                var titleLabel = new Label(title);
                titleLabel.style.fontSize = 24;
                titleLabel.style.color = Color.white;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.marginBottom = 10;
                titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

                var messageLabel = new Label(message);
                messageLabel.style.fontSize = 16;
                messageLabel.style.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                messageLabel.style.whiteSpace = WhiteSpace.Normal;
                messageLabel.style.marginBottom = 20;
                messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

                var okButton = new Button();
                okButton.clicked += () => overlay.RemoveFromHierarchy();
                okButton.text = "OK";
                okButton.style.height = 40;
                okButton.style.backgroundColor = new Color(0.863f, 0.078f, 0.235f); // Action Color (Crimson)
                okButton.style.color = Color.white;
                okButton.style.borderTopLeftRadius = okButton.style.borderTopRightRadius = okButton.style.borderBottomLeftRadius = okButton.style.borderBottomRightRadius = 5;

                popup.Add(titleLabel);
                popup.Add(messageLabel);
                popup.Add(okButton);
                overlay.Add(popup);

                activeDoc.rootVisualElement.Add(overlay);
            }
        }

        private UIDocument GetActiveMainDocument()
        {
            var docs = UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in docs)
            {
                if (doc != null && doc.gameObject.name != "GlobalManagers" && doc.isActiveAndEnabled && doc.rootVisualElement != null)
                {
                    return doc;
                }
            }
            return UnityEngine.Object.FindFirstObjectByType<UIDocument>();
        }
    }

    public interface IScreen
    {
        void Show();
        void Hide();
    }
}
