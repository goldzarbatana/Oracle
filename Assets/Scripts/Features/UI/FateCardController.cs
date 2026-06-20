using TimeAura.Core;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Features.UI
{
    /// <summary>
    /// Fate Card Controller - Manages individual card presentation in Convergence stream
    /// </summary>
    public sealed class FateCardController
    {
        private readonly VisualElement root;
        private readonly UserProfile profile;
        private readonly AppConfig appConfig;

        public FateCardController(VisualElement cardRoot, UserProfile profile, AppConfig appConfig)
        {
            this.root = cardRoot;
            this.profile = profile;
            this.appConfig = appConfig;

            InitializeCard();
        }

        private void InitializeCard()
        {
            // Set nickname
            var nicknameLabel = root.Q<Label>("Nickname");
            if (nicknameLabel != null)
            {
                nicknameLabel.text = profile.Nickname;
            }

            // Set status
            var statusLabel = root.Q<Label>("Status");
            if (statusLabel != null)
            {
                statusLabel.text = $"Status: {profile.Status}";
            }

            // Set aspect icons colors based on user's primary aspects
            SetAspectIcon("AspectLumen", AspectType.Lumen);
            SetAspectIcon("AspectForma", AspectType.Forma);
            SetAspectIcon("AspectAction", AspectType.Action);
            SetAspectIcon("AspectEssence", AspectType.Essence);

            // Bind Start Transformation button
            var button = root.Q<Button>("StartTransformationButton");
            if (button != null)
            {
                button.clicked += OnStartTransformationClicked;
            }
        }

        private void SetAspectIcon(string iconName, AspectType aspect)
        {
            var icon = root.Q<VisualElement>(iconName);
            if (icon != null)
            {
                var color = appConfig.GetAspectColor(aspect);
                icon.style.backgroundColor = new StyleColor(color);

                // If user has this aspect, make it more prominent
                if (profile.Aspects.ContainsKey(aspect))
                {
                    // Set border color per edge (IStyle does not expose a single borderColor property)
                    icon.style.borderLeftColor = new StyleColor(color);
                    icon.style.borderTopColor = new StyleColor(color);
                    icon.style.borderRightColor = new StyleColor(color);
                    icon.style.borderBottomColor = new StyleColor(color);

                    // Set border width per edge
                    icon.style.borderLeftWidth = new StyleFloat(2f);
                    icon.style.borderTopWidth = new StyleFloat(2f);
                    icon.style.borderRightWidth = new StyleFloat(2f);
                    icon.style.borderBottomWidth = new StyleFloat(2f);
                }
            }
        }

        private void OnStartTransformationClicked()
        {
            Debug.Log($"[FateCardController] Start Transformation with {profile.Nickname}");
            EventBus.Publish(new TransformationRequestedEvent(profile));
        }
    }

    public readonly struct TransformationRequestedEvent
    {
        public TransformationRequestedEvent(UserProfile partner)
        {
            Partner = partner;
        }

        public UserProfile Partner { get; }
    }
}
