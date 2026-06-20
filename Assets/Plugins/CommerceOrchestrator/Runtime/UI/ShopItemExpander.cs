using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CommerceOrchestrator.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ShopItemExpander : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float expandDuration = 0.3f;
        [SerializeField] private float collapseDuration = 0.25f;
        [SerializeField] private Ease expandEase = Ease.OutBack;
        [SerializeField] private Ease collapseEase = Ease.InBack;
        [SerializeField] private float targetScaleMultiplier = 1.3f;

        [Header("Dimmer")]
        [SerializeField] private Color dimmerColor = new Color(0, 0, 0, 0.6f);

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _originalAnchoredPosition;
        private Vector3 _originalScale;
        private int _originalSiblingIndex;
        private Transform _originalParent;

        private GameObject _dimmerObject;
        private bool _isExpanded = false;
        private bool _isAnimating = false;

        public bool IsExpanded => _isExpanded;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Toggle()
        {
            if (_isAnimating) return;
            if (_isExpanded) Collapse();
            else Expand();
        }

        public void Expand()
        {
            if (_isExpanded || _isAnimating) return;
            _isAnimating = true;

            _originalParent = _rectTransform.parent;
            _originalSiblingIndex = _rectTransform.GetSiblingIndex();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;
            _originalScale = _rectTransform.localScale;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                CreateDimmer(parentCanvas.transform);
                _rectTransform.SetParent(parentCanvas.transform, true);
                _rectTransform.SetAsLastSibling();
            }

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 3000;
            gameObject.AddComponent<GraphicRaycaster>();

            _rectTransform.DOAnchorPos(Vector2.zero, expandDuration).SetEase(expandEase);
            _rectTransform.DOScale(_originalScale * targetScaleMultiplier, expandDuration).SetEase(expandEase)
                .OnComplete(() =>
                {
                    _isExpanded = true;
                    _isAnimating = false;
                });
        }

        public void Collapse()
        {
            if (!_isExpanded || _isAnimating) return;
            _isAnimating = true;

            if (_dimmerObject != null)
            {
                Destroy(_dimmerObject);
            }

            _rectTransform.DOAnchorPos(_originalAnchoredPosition, collapseDuration).SetEase(collapseEase);
            _rectTransform.DOScale(_originalScale, collapseDuration).SetEase(collapseEase)
                .OnComplete(() =>
                {
                    var raycaster = GetComponent<GraphicRaycaster>();
                    if (raycaster != null) Destroy(raycaster);
                    var canvas = GetComponent<Canvas>();
                    if (canvas != null) Destroy(canvas);

                    _rectTransform.SetParent(_originalParent, true);
                    _rectTransform.SetSiblingIndex(_originalSiblingIndex);

                    _isExpanded = false;
                    _isAnimating = false;
                });
        }

        private void CreateDimmer(Transform parent)
        {
            _dimmerObject = new GameObject("ShopItemDimmer");
            _dimmerObject.transform.SetParent(parent, false);
            _dimmerObject.transform.SetSiblingIndex(transform.GetSiblingIndex());

            var rt = _dimmerObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = _dimmerObject.AddComponent<Image>();
            img.color = dimmerColor;

            var btn = _dimmerObject.AddComponent<Button>();
            btn.onClick.AddListener(Collapse);
        }

        private void OnDisable()
        {
            if (_isExpanded)
            {
                if (_dimmerObject != null) Destroy(_dimmerObject);
                var raycaster = GetComponent<GraphicRaycaster>();
                if (raycaster != null) Destroy(raycaster);
                var canvas = GetComponent<Canvas>();
                if (canvas != null) Destroy(canvas);
                
                if (_originalParent != null)
                {
                    _rectTransform.SetParent(_originalParent, false);
                    _rectTransform.SetSiblingIndex(_originalSiblingIndex);
                }
                _rectTransform.anchoredPosition = _originalAnchoredPosition;
                _rectTransform.localScale = _originalScale;
                _isExpanded = false;
            }
        }
    }
}
