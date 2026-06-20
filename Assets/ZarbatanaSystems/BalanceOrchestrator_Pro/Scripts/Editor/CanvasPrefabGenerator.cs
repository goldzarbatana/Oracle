#if USING_UNITY_UI
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ZarbatanaSystems.BalanceOrchestrator.Pro;

namespace ZarbatanaSystems.BalanceOrchestrator.Pro.Demo
{
    public static class CanvasPrefabGenerator
    {
        [MenuItem("Tools/Balance Orchestrator/Generate Demo Canvas Prefab")]
        public static void GenerateCanvasPrefab()
        {
            string savePath = "Assets/ZarbatanaSystems/BalanceOrchestrator_Pro/Demo/DemoCanvas.prefab";
            
            // Create Demo directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder("Assets/ZarbatanaSystems/BalanceOrchestrator_Pro/Demo"))
            {
                AssetDatabase.CreateFolder("Assets/ZarbatanaSystems/BalanceOrchestrator_Pro", "Demo");
            }

            // Create Canvas root
            GameObject canvasGO = new GameObject("DemoCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Portrait reference matches screenshot better
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Font loading with fallback
            Font customFont = null;
            try
            {
                customFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
            }
            catch (System.ArgumentException)
            {
                customFont = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");
            }

            // --- Panel (Top Center) ---
            GameObject panelGO = new GameObject("Live Data Feed Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.67f); // Reduced height to fit logs tightly
            panelRect.anchorMax = new Vector2(0.95f, 0.98f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // Add Vertical Layout Group for automatic neat spacing (prevents overlap)
            VerticalLayoutGroup vLayout = panelGO.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 25, 5); // Bottom padding: 5, Side padding: 10
            vLayout.spacing = 10; // Reduced spacing to keep it tight
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // Title
            CreateText("Title", panelGO.transform, "LIVE DDA DATA FORMULA", customFont, 54, TextAnchor.MiddleCenter, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Color.cyan, true);

            // Difficulty
            CreateText("DifficultyText", panelGO.transform, "Current Difficulty: 50.0%", customFont, 44, TextAnchor.MiddleCenter, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Color.white, true);
                
            // ☁️ Sheet Data
            CreateText("SheetDataText", panelGO.transform, "☁️ Sheet Data (Cloud): HP x1.00 | Spd x1.00", customFont, 36, TextAnchor.MiddleLeft, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Color.white, true);
                
            // 🧠 Local DDA
            CreateText("LocalDdaText", panelGO.transform, "🧠 Local DDA (Runtime): HP x1.00 | Spd x1.00", customFont, 36, TextAnchor.MiddleLeft, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Color.white, true);

            // 🔥 FINAL RESULT
            CreateText("FinalResultText", panelGO.transform, "🔥 FINAL RESULT: HP x1.00 | Spd x1.00", customFont, 44, TextAnchor.MiddleLeft, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, new Color(1f, 0.4f, 0.4f), true);

            // --- Log Container ---
            GameObject logContainerGO = new GameObject("LogContainer");
            logContainerGO.transform.SetParent(panelGO.transform, false);
            
            VerticalLayoutGroup logVLayout = logContainerGO.AddComponent<VerticalLayoutGroup>();
            logVLayout.spacing = 8;
            logVLayout.childAlignment = TextAnchor.UpperCenter;
            logVLayout.childControlWidth = true;
            logVLayout.childControlHeight = true;
            logVLayout.childForceExpandWidth = true;
            logVLayout.childForceExpandHeight = false;

            // System Log Title (inside LogContainer)
            CreateText("SystemLogTitle", logContainerGO.transform, "System Log:", customFont, 40, TextAnchor.MiddleCenter, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Color.green, true);

            // System Log Content (inside LogContainer)
            CreateText("LogText", logContainerGO.transform, "Ready.", customFont, 32, TextAnchor.UpperCenter, 
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Color.green, true);

            // --- WorkSpace (Middle) ---
            GameObject workspaceGO = new GameObject("WorkSpace");
            workspaceGO.transform.SetParent(canvasGO.transform, false);
            RectTransform workspaceRect = workspaceGO.AddComponent<RectTransform>();
            workspaceRect.anchorMin = new Vector2(0.05f, 0.18f);
            workspaceRect.anchorMax = new Vector2(0.95f, 0.66f); // Enlarged workspace area to match new panel anchor
            workspaceRect.pivot = new Vector2(0.5f, 0.5f);
            workspaceRect.offsetMin = Vector2.zero;
            workspaceRect.offsetMax = Vector2.zero;
            
            // Add a light grey transparent image to define gameplay bounds visually
            Image wsImage = workspaceGO.AddComponent<Image>();
            wsImage.color = new Color(1, 1, 1, 0.05f);

            // --- Buttons Container (Bottom Center) ---
            GameObject btnContainer = new GameObject("Buttons Container");
            btnContainer.transform.SetParent(canvasGO.transform, false);
            RectTransform btnContRect = btnContainer.AddComponent<RectTransform>();
            btnContRect.anchorMin = new Vector2(0.02f, 0.02f); // Widened slightly to maximize horizontal space
            btnContRect.anchorMax = new Vector2(0.98f, 0.17f);
            btnContRect.pivot = new Vector2(0.5f, 0f);
            btnContRect.offsetMin = Vector2.zero;
            btnContRect.offsetMax = Vector2.zero;

            // Horizontal layout for buttons
            HorizontalLayoutGroup layout = btnContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10; // Slightly reduced spacing
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            // Buttons - Font size 62 as requested
            CreateButton("FetchButton", btnContainer.transform, "🔄 Fetch Latest Sheet", customFont, 62, new Color(0.18f, 0.35f, 0.58f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            CreateButton("StrugglingButton", btnContainer.transform, "⚠️ Simulate Struggling", customFont, 62, new Color(0.72f, 0.3f, 0.3f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            CreateButton("DominatingButton", btnContainer.transform, "🔥 Simulate Dominating", customFont, 62, new Color(0.3f, 0.65f, 0.3f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // --- Attach Controller ---
            var controller = canvasGO.AddComponent<Pro_DDA_DemoController>();
            var serializedObject = new SerializedObject(controller);
            
            // Assign UI fields (recursive find to support manual restructuring)
            serializedObject.FindProperty("difficultyText").objectReferenceValue = FindTransformDeep(canvasGO.transform, "DifficultyText")?.GetComponent<Text>();
            serializedObject.FindProperty("sheetDataText").objectReferenceValue = FindTransformDeep(canvasGO.transform, "SheetDataText")?.GetComponent<Text>();
            serializedObject.FindProperty("localDdaText").objectReferenceValue = FindTransformDeep(canvasGO.transform, "LocalDdaText")?.GetComponent<Text>();
            serializedObject.FindProperty("finalResultText").objectReferenceValue = FindTransformDeep(canvasGO.transform, "FinalResultText")?.GetComponent<Text>();
            serializedObject.FindProperty("logContainer").objectReferenceValue = FindTransformDeep(canvasGO.transform, "LogContainer")?.GetComponent<RectTransform>();
            serializedObject.FindProperty("systemLogTitle").objectReferenceValue = FindTransformDeep(canvasGO.transform, "SystemLogTitle")?.GetComponent<Text>();
            serializedObject.FindProperty("logText").objectReferenceValue = FindTransformDeep(canvasGO.transform, "LogText")?.GetComponent<Text>();
            
            serializedObject.FindProperty("fetchButton").objectReferenceValue = FindTransformDeep(canvasGO.transform, "FetchButton")?.GetComponent<Button>();
            serializedObject.FindProperty("strugglingButton").objectReferenceValue = FindTransformDeep(canvasGO.transform, "StrugglingButton")?.GetComponent<Button>();
            serializedObject.FindProperty("dominatingButton").objectReferenceValue = FindTransformDeep(canvasGO.transform, "DominatingButton")?.GetComponent<Button>();
            serializedObject.FindProperty("workSpaceRect").objectReferenceValue = FindTransformDeep(canvasGO.transform, "WorkSpace")?.GetComponent<RectTransform>();

            // Auto-assign difficultyProfile if available
            var profileGuids = AssetDatabase.FindAssets("Default_DifficultyProfile t:Pro_DifficultyProfile");
            if (profileGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(profileGuids[0]);
                var profile = AssetDatabase.LoadAssetAtPath<Pro_DifficultyProfile>(path);
                if (profile != null)
                {
                    serializedObject.FindProperty("difficultyProfile").objectReferenceValue = profile;
                }
            }
            serializedObject.ApplyModifiedProperties();

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(canvasGO, savePath);
            GameObject.DestroyImmediate(canvasGO);
            
            Debug.Log($"[Pro DDA Prefab Gen] Successfully generated Demo Canvas Prefab at: {savePath}");
        }

        private static GameObject CreateText(string name, Transform parent, string textStr, Font font, int fontSize, TextAnchor alignment, 
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color color, bool useContentSizeFitter = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.text = textStr;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (useContentSizeFitter)
            {
                ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            return go;
        }

        private static GameObject CreateButton(string name, Transform parent, string textStr, Font font, int fontSize, Color bgColor, 
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.color = bgColor;

            Button btn = go.AddComponent<Button>();
            
            // Add Text
            CreateText("Text", go.transform, textStr, font, fontSize, TextAnchor.MiddleCenter, 
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);

            return go;
        }

        private static Transform FindTransformDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform found = FindTransformDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
