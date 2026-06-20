#if UNITY_EDITOR && HAS_UGUI
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UniversalMonetization
{
    public static class MonetizationSetupUtility
    {
        [MenuItem("Tools/Monetization Orchestrator/Setup Resources & Prefabs")]
        public static void GeneratePrefabs()
        {
            // Ensure Resources directory exists
            string resourcesPath = "Assets/Plugins/MonetizationOrchestrator/Resources";
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
            }

            // Ensure Prefabs directory exists
            string prefabsPath = "Assets/Plugins/MonetizationOrchestrator/Prefabs";
            if (!Directory.Exists(prefabsPath))
            {
                Directory.CreateDirectory(prefabsPath);
            }

            CreateRoundedSprite(resourcesPath);
            GenerateMockAdCanvas(resourcesPath);
            GenerateAdLoadingOverlay(resourcesPath);
            GenerateMonetizationOrchestrator(prefabsPath);
            GenerateDemoCanvas(prefabsPath, resourcesPath);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", "Monetization Orchestrator prefabs successfully generated in the Prefabs and Resources directories!", "Awesome");
        }

        private static void CreateRoundedSprite(string resourcesPath)
        {
            string assetPath = Path.Combine(resourcesPath, "RoundedUI.png").Replace("\\", "/");
            if (File.Exists(assetPath)) return;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(1, 1, 1, 0);
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(center, new Vector2(x, y));
                    if (dist < radius - 1) tex.SetPixel(x, y, Color.white);
                    else if (dist < radius) tex.SetPixel(x, y, new Color(1, 1, 1, radius - dist));
                    else tex.SetPixel(x, y, transparent);
                }
            }
            tex.Apply();
            
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spriteBorder = new Vector4(60, 60, 60, 60);
                ti.SaveAndReimport();
            }
        }

        private static void GenerateMockAdCanvas(string resourcesPath)
        {
            string prefabPath = Path.Combine(resourcesPath, "MockAdCanvas.prefab");

            // 1. Create Root Canvas
            GameObject canvasGo = new GameObject("MockAdCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Render on top of everything

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            canvasGo.AddComponent<GraphicRaycaster>();
            MockAdCanvasController controller = canvasGo.AddComponent<MockAdCanvasController>();

            // 2. Background Blocking Panel
            GameObject panelGo = new GameObject("BlockingPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            Image bgImage = panelGo.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.95f); // Deep dark translucent background
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // 3. Central Card Frame
            GameObject cardGo = new GameObject("CardFrame");
            cardGo.transform.SetParent(panelGo.transform, false);
            Image cardImage = cardGo.AddComponent<Image>();
            cardImage.color = new Color(0.15f, 0.16f, 0.22f, 1f); // Sleek card background
            ApplyPremiumStyle(cardImage, resourcesPath);
            AddShadow(cardGo, new Color(0, 0, 0, 0.5f), new Vector2(0, -10));
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(800, 1000);

            // 4. Card Title Text
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(cardGo.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "SIMULATED AD PLAYING";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 64;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.22f, 0.78f, 1f, 1f); // Vibrant light blue HSL color
            titleText.alignment = TextAnchor.MiddleCenter;
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.sizeDelta = Vector2.zero;

            // 5. Timer Text
            GameObject timerGo = new GameObject("TimerText");
            timerGo.transform.SetParent(cardGo.transform, false);
            Text timerText = timerGo.AddComponent<Text>();
            timerText.text = "Ad closes in 5.0s";
            timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timerText.fontSize = 54;
            timerText.color = Color.white;
            timerText.alignment = TextAnchor.MiddleCenter;
            RectTransform timerRect = timerGo.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0, 0.5f);
            timerRect.anchorMax = new Vector2(1, 0.75f);
            timerRect.sizeDelta = Vector2.zero;

            // 6. Progress Slider
            GameObject sliderGo = new GameObject("ProgressSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(cardGo.transform, false);
            Slider slider = sliderGo.AddComponent<Slider>();
            slider.interactable = false;
            RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(600, 30);
            sliderRect.anchoredPosition = new Vector2(0, 50);

            GameObject backgroundGo = new GameObject("Background");
            backgroundGo.transform.SetParent(sliderGo.transform, false);
            Image sliderBg = backgroundGo.AddComponent<Image>();
            sliderBg.color = new Color(0.1f, 0.1f, 0.14f, 1f);
            RectTransform sliderBgRect = backgroundGo.GetComponent<RectTransform>();
            sliderBgRect.anchorMin = Vector2.zero;
            sliderBgRect.anchorMax = Vector2.one;
            sliderBgRect.sizeDelta = Vector2.zero;

            GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.22f, 0.78f, 1f, 1f);
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;

            // 7. Claim Button
            GameObject claimBtnGo = new GameObject("ClaimButton");
            claimBtnGo.transform.SetParent(cardGo.transform, false);
            Image claimImage = claimBtnGo.AddComponent<Image>();
            claimImage.color = new Color(0.18f, 0.8f, 0.44f, 1f); // Emerald green success button
            ApplyPremiumStyle(claimImage, resourcesPath);
            AddShadow(claimBtnGo, new Color(0.1f, 0.4f, 0.2f, 0.8f), new Vector2(0, -5));
            Button claimBtn = claimBtnGo.AddComponent<Button>();
            RectTransform claimRect = claimBtnGo.GetComponent<RectTransform>();
            claimRect.sizeDelta = new Vector2(500, 100);
            claimRect.anchoredPosition = new Vector2(0, -200);

            GameObject claimTxtGo = new GameObject("Text");
            claimTxtGo.transform.SetParent(claimBtnGo.transform, false);
            Text claimTxt = claimTxtGo.AddComponent<Text>();
            claimTxt.text = "CLAIM REWARD";
            claimTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            claimTxt.fontSize = 48;
            claimTxt.fontStyle = FontStyle.Bold;
            claimTxt.color = Color.white;
            claimTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform claimTxtRect = claimTxtGo.GetComponent<RectTransform>();
            claimTxtRect.anchorMin = Vector2.zero;
            claimTxtRect.anchorMax = Vector2.one;
            claimTxtRect.sizeDelta = Vector2.zero;

            // 8. Close Button
            GameObject closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(cardGo.transform, false);
            Image closeImage = closeBtnGo.AddComponent<Image>();
            closeImage.color = new Color(0.9f, 0.29f, 0.23f, 1f); // Alizarin red close button
            ApplyPremiumStyle(closeImage, resourcesPath);
            AddShadow(closeBtnGo, new Color(0.4f, 0.1f, 0.1f, 0.8f), new Vector2(0, -5));
            Button closeBtn = closeBtnGo.AddComponent<Button>();
            RectTransform closeRect = closeBtnGo.GetComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(500, 100);
            closeRect.anchoredPosition = new Vector2(0, -350);

            GameObject closeTxtGo = new GameObject("Text");
            closeTxtGo.transform.SetParent(closeBtnGo.transform, false);
            Text closeTxt = closeTxtGo.AddComponent<Text>();
            closeTxt.text = "SKIP / CLOSE";
            closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeTxt.fontSize = 48;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform closeTxtRect = closeTxtGo.GetComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.sizeDelta = Vector2.zero;

            // 9. Wire Controller Fields using SerializedObject
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("titleText").objectReferenceValue = titleText;
            serializedController.FindProperty("timerText").objectReferenceValue = timerText;
            serializedController.FindProperty("claimButton").objectReferenceValue = claimBtn;
            serializedController.FindProperty("closeButton").objectReferenceValue = closeBtn;
            serializedController.FindProperty("progressSlider").objectReferenceValue = slider;
            serializedController.ApplyModifiedProperties();

            // Set up button listeners
            claimBtn.onClick.AddListener(controller.OnClaimClicked);
            closeBtn.onClick.AddListener(controller.OnCloseClicked);

            // Set the Canvas GameObject inactive by default before saving as prefab
            SetLayerRecursive(canvasGo, 5);
            canvasGo.SetActive(false);

            // Save Prefab and destroy scene copy
            PrefabUtility.SaveAsPrefabAsset(canvasGo, prefabPath);
            Object.DestroyImmediate(canvasGo);
        }

        private static void GenerateAdLoadingOverlay(string resourcesPath)
        {
            string prefabPath = Path.Combine(resourcesPath, "AdLoadingOverlay.prefab");

            // 1. Create Root Canvas
            GameObject canvasGo = new GameObject("AdLoadingOverlay");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000; // Above mock ad canvas

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            canvasGo.AddComponent<GraphicRaycaster>();
            AdLoadingOverlay overlay = canvasGo.AddComponent<AdLoadingOverlay>();

            // 2. Spinner Blocking Background
            GameObject spinnerBgGo = new GameObject("SpinnerContainer");
            spinnerBgGo.transform.SetParent(canvasGo.transform, false);
            Image bgImage = spinnerBgGo.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.7f); // Translucent black shade
            RectTransform spinnerBgRect = spinnerBgGo.GetComponent<RectTransform>();
            spinnerBgRect.anchorMin = Vector2.zero;
            spinnerBgRect.anchorMax = Vector2.one;
            spinnerBgRect.sizeDelta = Vector2.zero;

            // 3. Loading Text Message
            GameObject txtGo = new GameObject("LoadingText");
            txtGo.transform.SetParent(spinnerBgGo.transform, false);
            Text text = txtGo.AddComponent<Text>();
            text.text = "Loading Ad... Please wait";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 54;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRect = txtGo.GetComponent<RectTransform>();
            txtRect.sizeDelta = new Vector2(800, 100);
            txtRect.anchoredPosition = new Vector2(0, -100);

            // 4. Simple Visual Spinner Icon (Self-Rotating)
            GameObject spinnerIconGo = new GameObject("SpinnerIcon");
            spinnerIconGo.transform.SetParent(spinnerBgGo.transform, false);
            Image spinnerIconImage = spinnerIconGo.AddComponent<Image>();
            spinnerIconImage.color = new Color(0.22f, 0.78f, 1f, 1f); // Vibrant light blue
            RectTransform spinnerIconRect = spinnerIconGo.GetComponent<RectTransform>();
            spinnerIconRect.sizeDelta = new Vector2(120, 120);
            spinnerIconRect.anchoredPosition = new Vector2(0, 100);
            
            // Add a simple rotation script
            spinnerIconGo.AddComponent<SimpleAdLoadingSpinner>();

            // 5. Toast Message Card (Ad Unavailable Popup)
            GameObject toastContainerGo = new GameObject("ToastContainer");
            toastContainerGo.transform.SetParent(canvasGo.transform, false);
            Image toastBgImage = toastContainerGo.AddComponent<Image>();
            toastBgImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            CanvasGroup canvasGroup = toastContainerGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            RectTransform toastContainerRect = toastContainerGo.GetComponent<RectTransform>();
            toastContainerRect.anchorMin = new Vector2(0.5f, 1f);
            toastContainerRect.anchorMax = new Vector2(0.5f, 1f);
            toastContainerRect.pivot = new Vector2(0.5f, 1f);
            toastContainerRect.sizeDelta = new Vector2(800, 180);
            toastContainerRect.anchoredPosition = new Vector2(0, -150); // Drop down from top

            GameObject toastTxtGo = new GameObject("ToastText");
            toastTxtGo.transform.SetParent(toastContainerGo.transform, false);
            Text toastText = toastTxtGo.AddComponent<Text>();
            toastText.text = "Ad not available. Try again later.";
            toastText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            toastText.fontSize = 46;
            toastText.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            toastText.alignment = TextAnchor.MiddleCenter;
            RectTransform toastTxtRect = toastTxtGo.GetComponent<RectTransform>();
            toastTxtRect.anchorMin = Vector2.zero;
            toastTxtRect.anchorMax = Vector2.one;
            toastTxtRect.sizeDelta = Vector2.zero;

            // 6. Wire fields using SerializedObject
            SerializedObject serializedOverlay = new SerializedObject(overlay);
            serializedOverlay.FindProperty("spinnerContainer").objectReferenceValue = spinnerBgGo;
            serializedOverlay.FindProperty("toastContainer").objectReferenceValue = toastContainerGo;
            serializedOverlay.FindProperty("toastText").objectReferenceValue = toastText;
            serializedOverlay.ApplyModifiedProperties();

            // Set to UI layer (Layer 5)
            SetLayerRecursive(canvasGo, 5);

            // Save Prefab and destroy scene copy
            PrefabUtility.SaveAsPrefabAsset(canvasGo, prefabPath);
            Object.DestroyImmediate(canvasGo);
        }

        private static void GenerateMonetizationOrchestrator(string prefabsPath)
        {
            string prefabPath = Path.Combine(prefabsPath, "MonetizationOrchestrator.prefab");

            GameObject orchestratorGo = new GameObject("MonetizationOrchestrator");
            var ads = orchestratorGo.AddComponent<AdManager>();
            var iap = orchestratorGo.AddComponent<IAPManager>();
            orchestratorGo.AddComponent<RemoteConfigManager>();
            
            // Add the unified orchestrator component and wire references
            var orch = orchestratorGo.AddComponent<MonetizationOrchestrator>();
            
            // Wire components via SerializedObject
            SerializedObject serializedOrch = new SerializedObject(orch);
            serializedOrch.FindProperty("adManager").objectReferenceValue = ads;
            serializedOrch.FindProperty("iapManager").objectReferenceValue = iap;
            serializedOrch.ApplyModifiedProperties();

            // Save Prefab and destroy scene copy
            PrefabUtility.SaveAsPrefabAsset(orchestratorGo, prefabPath);
            Object.DestroyImmediate(orchestratorGo);
        }

        private static void GenerateDemoCanvas(string prefabsPath, string resourcesPath)
        {
            string prefabPath = Path.Combine(prefabsPath, "MonetizationDemoCanvas.prefab");

            // 1. Create Root Canvas
            GameObject canvasGo = new GameObject("MonetizationDemoCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);

            canvasGo.AddComponent<GraphicRaycaster>();
            
            // Add Demo Script (via reflection to decouple editor assembly from demo scripts)
            Component demoScript = null;
            System.Type demoType = System.Type.GetType("UniversalMonetization.Demo.MonetizationTest, Assembly-CSharp");
            if (demoType == null)
            {
                demoType = System.Type.GetType("UniversalMonetization.Demo.MonetizationTest, Assembly-CSharp-firstpass");
            }
            if (demoType == null)
            {
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    demoType = assembly.GetType("UniversalMonetization.Demo.MonetizationTest");
                    if (demoType != null) break;
                }
            }
            if (demoType != null)
            {
                demoScript = canvasGo.AddComponent(demoType);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[MonetizationOrchestrator] Could not find UniversalMonetization.Demo.MonetizationTest script. Demo canvas will be created without it.");
            }

            // 1.5 Full screen background to hide the ugly standard skybox
            GameObject bgGo = new GameObject("FullscreenBackground");
            bgGo.transform.SetParent(canvasGo.transform, false);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.04f, 0.06f, 1f); // Premium dark violet/black
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // 2. Central Panel
            GameObject panelGo = new GameObject("ButtonPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            Image panelImage = panelGo.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.11f, 0.15f, 0.9f);
            ApplyPremiumStyle(panelImage, resourcesPath);
            AddShadow(panelGo, new Color(0, 0, 0, 0.5f), new Vector2(0, -8));
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(750, 600);
            
            // Anchor to center of the screen
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0, 150); // Shifted higher

            // 3. Title text
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(panelGo.transform, false);
            Text titleText = titleGo.AddComponent<Text>();
            titleText.text = "MONETIZATION DEMO";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 44;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(0.22f, 0.78f, 1f, 1f);
            titleText.alignment = TextAnchor.MiddleCenter;
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.sizeDelta = Vector2.zero;

            // 4. Rewarded Button
            GameObject rewardedBtnGo = new GameObject("RewardedButton");
            rewardedBtnGo.transform.SetParent(panelGo.transform, false);
            Image rewardedImg = rewardedBtnGo.AddComponent<Image>();
            rewardedImg.color = new Color(0.18f, 0.44f, 0.8f, 1f); // Blue
            ApplyPremiumStyle(rewardedImg, resourcesPath);
            AddShadow(rewardedBtnGo, new Color(0.08f, 0.2f, 0.4f, 0.8f), new Vector2(0, -4));
            Button rewardedBtn = rewardedBtnGo.AddComponent<Button>();
            RectTransform rewardedRect = rewardedBtnGo.GetComponent<RectTransform>();
            rewardedRect.anchorMin = new Vector2(0.5f, 0.6f);
            rewardedRect.anchorMax = new Vector2(0.5f, 0.6f);
            rewardedRect.sizeDelta = new Vector2(500, 80);
            rewardedRect.anchoredPosition = Vector2.zero;

            GameObject rewardedTxtGo = new GameObject("Text");
            rewardedTxtGo.transform.SetParent(rewardedBtnGo.transform, false);
            Text rewardedTxt = rewardedTxtGo.AddComponent<Text>();
            rewardedTxt.text = "Show Rewarded Ad";
            rewardedTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rewardedTxt.fontSize = 36;
            rewardedTxt.fontStyle = FontStyle.Bold;
            rewardedTxt.color = Color.white;
            rewardedTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform rewardedTxtRect = rewardedTxtGo.GetComponent<RectTransform>();
            rewardedTxtRect.anchorMin = Vector2.zero;
            rewardedTxtRect.anchorMax = Vector2.one;
            rewardedTxtRect.sizeDelta = Vector2.zero;

            // 5. Interstitial Button
            GameObject interstitialBtnGo = new GameObject("InterstitialButton");
            interstitialBtnGo.transform.SetParent(panelGo.transform, false);
            Image interstitialImg = interstitialBtnGo.AddComponent<Image>();
            interstitialImg.color = new Color(0.55f, 0.25f, 0.85f, 1f); // Vibrant Amethyst Purple
            ApplyPremiumStyle(interstitialImg, resourcesPath);
            AddShadow(interstitialBtnGo, new Color(0.05f, 0.05f, 0.08f, 0.8f), new Vector2(0, -4));
            Button interstitialBtn = interstitialBtnGo.AddComponent<Button>();
            RectTransform interstitialRect = interstitialBtnGo.GetComponent<RectTransform>();
            interstitialRect.anchorMin = new Vector2(0.5f, 0.4f);
            interstitialRect.anchorMax = new Vector2(0.5f, 0.4f);
            interstitialRect.sizeDelta = new Vector2(500, 80);
            interstitialRect.anchoredPosition = Vector2.zero;

            GameObject interstitialTxtGo = new GameObject("Text");
            interstitialTxtGo.transform.SetParent(interstitialBtnGo.transform, false);
            Text interstitialTxt = interstitialTxtGo.AddComponent<Text>();
            interstitialTxt.text = "Show Interstitial Ad";
            interstitialTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            interstitialTxt.fontSize = 36;
            interstitialTxt.fontStyle = FontStyle.Bold;
            interstitialTxt.color = Color.white;
            interstitialTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform interstitialTxtRect = interstitialTxtGo.GetComponent<RectTransform>();
            interstitialTxtRect.anchorMin = Vector2.zero;
            interstitialTxtRect.anchorMax = Vector2.one;
            interstitialTxtRect.sizeDelta = Vector2.zero;

            // 6. IAP Button
            GameObject iapBtnGo = new GameObject("IAPButton");
            iapBtnGo.transform.SetParent(panelGo.transform, false);
            Image iapImg = iapBtnGo.AddComponent<Image>();
            iapImg.color = new Color(0.9f, 0.6f, 0.15f, 1f); // Gold/Orange
            ApplyPremiumStyle(iapImg, resourcesPath);
            AddShadow(iapBtnGo, new Color(0.4f, 0.2f, 0.05f, 0.8f), new Vector2(0, -4));
            Button iapBtn = iapBtnGo.AddComponent<Button>();
            RectTransform iapRect = iapBtnGo.GetComponent<RectTransform>();
            iapRect.anchorMin = new Vector2(0.5f, 0.2f);
            iapRect.anchorMax = new Vector2(0.5f, 0.2f);
            iapRect.sizeDelta = new Vector2(500, 80);
            iapRect.anchoredPosition = Vector2.zero;

            GameObject iapTxtGo = new GameObject("Text");
            iapTxtGo.transform.SetParent(iapBtnGo.transform, false);
            Text iapTxt = iapTxtGo.AddComponent<Text>();
            iapTxt.text = "Buy Product Bundle";
            iapTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iapTxt.fontSize = 36;
            iapTxt.fontStyle = FontStyle.Bold;
            iapTxt.color = Color.white;
            iapTxt.alignment = TextAnchor.MiddleCenter;
            RectTransform iapTxtRect = iapTxtGo.GetComponent<RectTransform>();
            iapTxtRect.anchorMin = Vector2.zero;
            iapTxtRect.anchorMax = Vector2.one;
            iapTxtRect.sizeDelta = Vector2.zero;

            // 7. LiveOps Text Display (Bottom Center)
            GameObject liveOpsBgGo = new GameObject("LiveOpsBg");
            liveOpsBgGo.transform.SetParent(canvasGo.transform, false);
            Image liveOpsBgImg = liveOpsBgGo.AddComponent<Image>();
            liveOpsBgImg.color = new Color(0.1f, 0.12f, 0.16f, 0.85f); // Dark semi-transparent
            ApplyPremiumStyle(liveOpsBgImg, resourcesPath);
            RectTransform liveOpsBgRt = liveOpsBgGo.GetComponent<RectTransform>();
            liveOpsBgRt.anchorMin = new Vector2(0.5f, 0f);
            liveOpsBgRt.anchorMax = new Vector2(0.5f, 0f);
            liveOpsBgRt.pivot = new Vector2(0.5f, 0f);
            liveOpsBgRt.anchoredPosition = new Vector2(0, 250); // Shifted higher
            liveOpsBgRt.sizeDelta = new Vector2(800, 480);

            GameObject liveOpsTxtGo = new GameObject("LiveOpsText");
            liveOpsTxtGo.transform.SetParent(liveOpsBgGo.transform, false);
            Text liveOpsTxt = liveOpsTxtGo.AddComponent<Text>();
            liveOpsTxt.text = "<color=#38ff8e><b>☁️ LiveOps Waiting...</b></color>";
            liveOpsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            liveOpsTxt.fontSize = 30;
            liveOpsTxt.lineSpacing = 1.3f;
            liveOpsTxt.alignment = TextAnchor.MiddleCenter;
            liveOpsTxt.supportRichText = true;
            RectTransform liveOpsTxtRt = liveOpsTxtGo.GetComponent<RectTransform>();
            liveOpsTxtRt.anchorMin = Vector2.zero;
            liveOpsTxtRt.anchorMax = Vector2.one;
            liveOpsTxtRt.sizeDelta = new Vector2(-40, -40); // 20px padding
            liveOpsTxtRt.anchoredPosition = Vector2.zero;

            // 8. Wire fields using SerializedObject if script exists
            if (demoScript != null)
            {
                SerializedObject serializedTest = new SerializedObject(demoScript);
                serializedTest.FindProperty("rewardedButton").objectReferenceValue = rewardedBtn;
                serializedTest.FindProperty("interstitialButton").objectReferenceValue = interstitialBtn;
                serializedTest.FindProperty("iapButton").objectReferenceValue = iapBtn;
                serializedTest.FindProperty("liveOpsDataText").objectReferenceValue = liveOpsTxt;
                serializedTest.ApplyModifiedProperties();
            }

            // Set to UI layer (Layer 5)
            SetLayerRecursive(canvasGo, 5);

            // Save Prefab and destroy scene copy
            PrefabUtility.SaveAsPrefabAsset(canvasGo, prefabPath);
            Object.DestroyImmediate(canvasGo);
        }

        private static void SetLayerRecursive(GameObject go, int layer)
        {
            if (go == null) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursive(child.gameObject, layer);
            }
        }

        private static void ApplyPremiumStyle(Image img, string resourcesPath)
        {
            string assetPath = Path.Combine(resourcesPath, "RoundedUI.png").Replace("\\", "/");
            Sprite uiSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (uiSprite != null)
            {
                img.sprite = uiSprite;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 3f; // Enhances corner tightness
            }
        }

        private static void AddShadow(GameObject go, Color color, Vector2 distance)
        {
            var shadow = go.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }
    }
}
#endif
