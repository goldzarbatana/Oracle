using System;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using TimeAura.Core.Services;
using TimeAura.Core.Localization;
using TimeAura.Features.Auth;
using TimeAura.Features.Data;
using TimeAura.Features.Localization;

namespace TimeAura.Features.UI.Nexus
{
    public class SymmetryRiteController
    {
        private VisualElement _root;
        private AudioService _audio;
        private HapticService _haptic;
        private LocalizationManager _localization;

        private VisualElement _sealContainer;
        private VisualElement _sealBase;
        private VisualElement _sealFill;
        private Label _lblHoldHint;
        private Button _btnCancelRite;

        public event Action OnHarmonyAchieved;
        public event Action OnSymmetryDeclined;

        private bool _isHolding;
        private float _holdTime;
        private bool _isMerging;
        private bool _isVisible;

        private UserProfile _targetUser;
        public UserProfile TargetUser => _targetUser;

        public SymmetryRiteController(VisualElement root, AudioService audio, HapticService haptic, LocalizationManager localization = null)
        {
            _root = root;
            _audio = audio;
            _haptic = haptic;
            _localization = localization;

            _sealContainer = _root.Q("SealContainer");
            _sealBase = _root.Q("SealBase");
            _sealFill = _root.Q("SealFill");
            _lblHoldHint = _root.Q<Label>("LblHoldHint");
            _btnCancelRite = _root.Q<Button>("BtnCancelRite");

            if (_sealContainer != null)
            {
                _sealContainer.RegisterCallback<PointerDownEvent>(OnPointerDown);
                _sealContainer.RegisterCallback<PointerUpEvent>(OnPointerUp);
                _sealContainer.RegisterCallback<PointerLeaveEvent>(OnPointerUp);
            }

            if (_btnCancelRite != null)
            {
                _btnCancelRite.clicked += () => {
                    Hide();
                    OnSymmetryDeclined?.Invoke();
                };
            }
        }

        public void Show(UserProfile target)
        {
            _targetUser = target;
            
            _root.RemoveFromClassList("modal--hidden");
            _root.style.display = DisplayStyle.Flex; 
            _root.pickingMode = PickingMode.Position;
            Debug.Log("[Popup] Opened: SymmetryRiteModal (Печать наміру)");
            _holdTime = 0;
            _isHolding = false;
            _isVisible = true;

            AnimateAmorphousAura(_sealBase).Forget();
        }

        public void Hide()
        {
            _root.AddToClassList("modal--hidden");
            _root.style.display = StyleKeyword.Null;
            _root.pickingMode = PickingMode.Ignore;
            _isVisible = false;
            StopHolding(-1);
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            if (_isMerging || !_isVisible) return;
            _isHolding = true;
            _holdTime = 0;
            _sealContainer.CapturePointer(e.pointerId);
            _sealFill?.AddToClassList("seal-fill--active");
            _audio?.PlaySFX("RadarPulse", 0.3f);
            TrackHoldProgress().Forget();
        }

        private void OnPointerUp(PointerUpEvent e) => StopHolding(e.pointerId);
        private void OnPointerUp(PointerLeaveEvent e) => StopHolding(e.pointerId);

        private void StopHolding(int pointerId)
        {
            if (!_isHolding) return;
            _isHolding = false;
            if (pointerId >= 0 && _sealContainer.HasPointerCapture(pointerId))
                _sealContainer.ReleasePointer(pointerId);
            
            _sealFill?.RemoveFromClassList("seal-fill--active");
            
            if (_sealBase != null)
            {
                _sealBase.style.scale = new StyleScale(new Scale(new Vector3(1, 1, 1)));
                _sealBase.style.opacity = 0.5f;
            }
        }

        private async UniTaskVoid TrackHoldProgress()
        {
            while (_isHolding && !_isMerging)
            {
                _holdTime += Time.deltaTime;
                
                float progress = Mathf.Clamp01(_holdTime / 2.0f); // 2 seconds to align
                
                if (_sealFill != null)
                {
                    _sealFill.style.opacity = progress;
                    float scale = 0.8f + (0.2f * progress);
                    _sealFill.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1)));
                }

                if (_sealBase != null)
                {
                    float baseScale = 1.0f - (0.1f * progress);
                    _sealBase.style.scale = new StyleScale(new Scale(new Vector3(baseScale, baseScale, 1)));
                }

                if (_holdTime >= 2.0f)
                {
                    await TriggerHarmony();
                }

                await UniTask.Yield();
            }
        }

        private async UniTask TriggerHarmony()
        {
            _isMerging = true;
            _isHolding = false;
            _haptic?.MediumTap();
            _audio?.PlaySFX("HarmonyAligned");

            if (_sealFill != null)
            {
                _sealFill.style.scale = new StyleScale(new Scale(new Vector3(1.5f, 1.5f, 1)));
                _sealFill.style.opacity = 0;
            }
            if (_lblHoldHint != null)
            {
                _lblHoldHint.text = _localization?.Get(AuraTerms.HARMONY_STATUS_ACTIVE, "HARMONY ACHIEVED").ToUpper() ?? "HARMONY ACHIEVED";
                _lblHoldHint.style.color = new StyleColor(new Color(1f, 0.84f, 0f, 1f)); 
            }

            await UniTask.Delay(1000);
            
            Hide();
            OnHarmonyAchieved?.Invoke();
            _isMerging = false;

            if (_lblHoldHint != null)
            {
                _lblHoldHint.text = _localization?.Get(AuraTerms.SYMMETRY_HOLD_HINT, "Hold to align") ?? "Hold to align";
                _lblHoldHint.style.color = new StyleColor(new Color(1f, 1f, 1f, 0.6f));
            }
        }

        private async UniTaskVoid AnimateAmorphousAura(VisualElement target)
        {
            if (target == null) return;
            while (Application.isPlaying && _isVisible && !_isMerging)
            {
                if (!_isHolding)
                {
                    target.style.scale = new StyleScale(new Scale(new Vector3(1.05f, 1.05f, 1f)));
                    await UniTask.Delay(1500);
                    if (!_isHolding) target.style.scale = new StyleScale(new Scale(new Vector3(0.95f, 0.95f, 1f)));
                    await UniTask.Delay(1500);
                }
                else
                {
                    await UniTask.Delay(100);
                }
            }
        }
    }
}
