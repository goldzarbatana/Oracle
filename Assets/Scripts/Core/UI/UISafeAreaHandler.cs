using UnityEngine;
using UnityEngine.UIElements;

namespace TimeAura.Core.UI
{
    /// <summary>
    /// Adjusts the UIDocument's root element to fit within the Screen.safeArea.
    /// Essential for modern mobile devices with notches.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UISafeAreaHandler : MonoBehaviour
    {
        [SerializeField] private bool _runInEditor = false;
        private UIDocument _uiDocument;
        private Rect _lastSafeArea = new Rect(0, 0, 0, 0);

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            ApplySafeArea();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!_runInEditor) 
            {
                var rootEl = _uiDocument?.rootVisualElement;
                if (rootEl != null && (rootEl.style.paddingTop != 0))
                {
                    rootEl.style.paddingTop = 0;
                    rootEl.style.paddingBottom = 0;
                    rootEl.style.paddingLeft = 0;
                    rootEl.style.paddingRight = 0;
                }
                return;
            }
#endif
            if (_lastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _lastSafeArea = Screen.safeArea;

            // Convert safe area to runtime UI coordinates
            var panel = root.panel;
            if (panel == null) return;

            // Calculate the padding in pixels
            // Note: UI Toolkit's root usually matches the Screen resolution if PanelSettings are set to Scale With Screen Size.
            // We apply the offset as padding to the root container.
            
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            var safeArea = Screen.safeArea;

            // We calculate relative padding
            float left = safeArea.x;
            float top = screenHeight - (safeArea.y + safeArea.height);
            float right = screenWidth - (safeArea.x + safeArea.width);
            float bottom = safeArea.y;

            // Set styles on the root
            root.style.paddingLeft = left * (root.layout.width / screenWidth);
            root.style.paddingRight = right * (root.layout.width / screenWidth);
            root.style.paddingTop = top * (root.layout.height / screenHeight);
            root.style.paddingBottom = bottom * (root.layout.height / screenHeight);

            Debug.Log($"[UISafeArea] Applied: L:{left} R:{right} T:{top} B:{bottom}");
        }
    }
}
