/*
 * MRUIManager.cs
 * ==============
 * 
 * UI Panel Creation and Management for MindNest MR Experience
 * 
 * Creates all Canvas UI panels programmatically using Unity uGUI:
 * - Welcome Panel
 * - Customization Panel (colors, accessories, sliders)
 * - Connection Confirm Panel
 * - Main Menu Sidebar
 * - Dialogue Bubble
 * - Breathing Overlay
 * - Altruistic Panel
 * - Tree Control Panel
 * - History Panel
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MindNest.MR
{
    /// <summary>
    /// UI Manager for all MR Canvas panels
    /// </summary>
    public class MRUIManager : MonoBehaviour
    {
        // ============================================================================
        // Panel References
        // ============================================================================
        
        [Header("Canvas References")]
        public Canvas worldSpaceCanvas;
        
        [Header("Panel GameObjects")]
        public GameObject welcomePanel;
        public GameObject customizationPanel;
        public GameObject connectionConfirmPanel;
        public GameObject mainMenuPanel;
        public GameObject mainMenuSidebar;
        public GameObject dialogueBubble;
        public GameObject taskOverlayPanel;
        public Text taskOverlayText;
        public GameObject breathingPanel;
        public GameObject altruisticPanel;
        public GameObject treeControlPanel;
        public GameObject historyPanel;
        
        [Header("UI Settings")]
        public Color panelBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        public Color buttonColor = new Color(0.9f, 0.7f, 0.6f, 1.0f);
        public Color buttonHoverColor = new Color(1.0f, 0.8f, 0.7f, 1.0f);
        
        [Header("Customization Panel References")]
        public Slider brightnessSlider;
        public Slider volumeSlider;
        public Slider scaleSlider;
        public Button[] themeColorButtons = new Button[5];
        public Button[] nomiColorButtons = new Button[5];
        public Button[] accessoryButtons = new Button[4];
        public Button finishCustomizationButton;
        
        [Header("Connection Confirm References")]
        public InputField chatInputField;
        public Button sendButton;
        public ScrollRect chatScrollView;
        public GameObject chatContentContainer;
        public Text listeningText;
        public Button finishConnectionButton;
        
        [Header("Main Menu References")]
        public Button breathingButton;
        public Button altruisticButton;
        public Button treeButton;
        public Button gearButton;
        public Button chatButton;  // 新增：聊天按钮
        public Button historyButton;
        public Button startHealingButton;
        public Text dialogueText;
        
        [Header("Breathing Panel References")]
        public Text globalTimerText;  // 新增：全局倒计时显示
        public Text breathingTimerText;
        public Button finishBreathingButton;
        
        [Header("Altruistic Panel References")]
        public Text altruisticInstructionText;
        public Text touchCountText;
        public Button comfortButton;           // 新增：安抚按钮
        public Button finishAltruisticButton;
        
        [Header("Gesture System References")]
        [Tooltip("手势提示UI容器")]
        public GameObject gestureUIContainer;
        [Tooltip("摄像头预览Raw Image")]
        public RawImage gestureCameraPreview;
        [Tooltip("手势图标")]
        public Image gestureIcon;
        [Tooltip("手势提示文本")]
        public Text gesturePromptText;
        [Tooltip("手势进度文本")]
        public Text gestureProgressText;
        
        [Header("Tree Control References")]
        public Dropdown seasonDropdown;
        public Button resetOrbsButton;
        public Button closeTreeButton;
        
        [Header("History Panel References")]
        public ScrollRect historyScrollRect;
        public Transform historyContentParent;
        public Button backFromHistoryButton;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Awake()
        {
            // Create world space canvas if not exists
            if (worldSpaceCanvas == null)
            {
                CreateWorldSpaceCanvas();
            }
        }
        
        void Start()
        {
            Debug.Log("🎨 MRUIManager: Building all UI panels...");
            
            // Build all panels
            BuildCustomizationPanel();
            BuildConnectionConfirmPanel();
            BuildMainMenuPanel();
            BuildBreathingPanel();
            BuildAltruisticPanel();
            BuildTreeControlPanel();
            BuildHistoryPanel();
            BuildTaskOverlayPanel();
            
            // Hide all panels initially
            HideAllPanels();
            
            Debug.Log("✅ MRUIManager: All panels created successfully");
        }
        
        // ============================================================================
        // Canvas Creation
        // ============================================================================
        
        private void CreateWorldSpaceCanvas()
        {
            GameObject canvasObj = new GameObject("MR_WorldSpace_Canvas");
            canvasObj.transform.SetParent(transform);
            
            worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
            worldSpaceCanvas.worldCamera = Camera.main;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Position canvas in front of camera
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1600, 1000);
            canvasRect.position = new Vector3(0, 1.5f, 3.0f); // Further away for better full-screen view
            canvasRect.localScale = new Vector3(0.005f, 0.005f, 0.005f); // Optimized scale for full-screen view
            
            // Create EventSystem if it doesn't exist (required for UI interaction)
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.transform.SetParent(transform);
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("Created EventSystem for UI interaction");
            }
            
            Debug.Log("Created WorldSpace Canvas");
        }
        
        // ============================================================================
        // Panel Builders
        // ============================================================================
        
        private void BuildCustomizationPanel()
        {
            customizationPanel = CreatePanel("CustomizationPanel", new Vector2(0, 0), new Vector2(1400, 800));
            
            // Left side - Basic Settings
            GameObject leftPanel = CreateSubPanel(customizationPanel, "LeftPanel", new Vector2(-350, 0), new Vector2(500, 700));
            CreateText(leftPanel, "TitleText", "Basic Settings", new Vector2(0, 300), new Vector2(400, 60), 48);
            
            // Brightness slider
            CreateText(leftPanel, "BrightnessLabel", "Brightness", new Vector2(0, 200), new Vector2(400, 40), 32);
            brightnessSlider = CreateSlider(leftPanel, "BrightnessSlider", new Vector2(0, 150), new Vector2(400, 40));
            
            // Volume slider
            CreateText(leftPanel, "VolumeLabel", "Volume", new Vector2(0, 80), new Vector2(400, 40), 32);
            volumeSlider = CreateSlider(leftPanel, "VolumeSlider", new Vector2(0, 30), new Vector2(400, 40));
            
            // Theme color buttons
            CreateText(leftPanel, "ThemeLabel", "Theme Color", new Vector2(0, -50), new Vector2(400, 40), 32);
            Color[] themeColors = { Color.white, Color.gray, new Color(0.6f, 0.9f, 0.8f), new Color(0.7f, 0.8f, 1f), new Color(1f, 0.8f, 0.9f) };
            for (int i = 0; i < 5; i++)
            {
                float xPos = -160 + i * 80;
                themeColorButtons[i] = CreateColorButton(leftPanel, $"ThemeColor{i}", new Vector2(xPos, -120), new Vector2(60, 60), themeColors[i]);
            }
            
            // Right side - Personalization
            GameObject rightPanel = CreateSubPanel(customizationPanel, "RightPanel", new Vector2(350, 0), new Vector2(500, 700));
            CreateText(rightPanel, "PersonalTitle", "Personalization", new Vector2(0, 300), new Vector2(400, 60), 48);
            
            // Nomi color buttons
            CreateText(rightPanel, "ColorLabel", "Color", new Vector2(0, 220), new Vector2(400, 40), 32);
            Color[] nomiColors = { Color.white, new Color(0.5f, 0.5f, 0.5f), new Color(0.6f, 0.9f, 0.8f), new Color(0.7f, 0.8f, 1f), new Color(1f, 0.8f, 0.9f) };
            for (int i = 0; i < 5; i++)
            {
                float xPos = -160 + i * 80;
                nomiColorButtons[i] = CreateColorButton(rightPanel, $"NomiColor{i}", new Vector2(xPos, 170), new Vector2(60, 60), nomiColors[i]);
            }
            
            // Accessories
            CreateText(rightPanel, "AccessoriesLabel", "Accessories", new Vector2(0, 80), new Vector2(400, 40), 32);
            string[] accessoryNames = { "Hat", "Halo", "Bow", "Cape" };
            for (int i = 0; i < 4; i++)
            {
                float xPos = -120 + i * 80;
                accessoryButtons[i] = CreateButton(rightPanel, $"Accessory{i}", accessoryNames[i], new Vector2(xPos, 20), new Vector2(70, 70));
            }
            
            // Scale slider
            CreateText(rightPanel, "ScaleLabel", "Scale", new Vector2(0, -80), new Vector2(400, 40), 32);
            scaleSlider = CreateSlider(rightPanel, "ScaleSlider", new Vector2(0, -130), new Vector2(400, 40));
            scaleSlider.minValue = 0.8f;
            scaleSlider.maxValue = 3.0f;
            scaleSlider.value = 2.0f;
            
            // Finish button
            finishCustomizationButton = CreateButton(customizationPanel, "FinishButton", "Finish", new Vector2(0, -340), new Vector2(300, 80));
        }
        
        private void BuildConnectionConfirmPanel()
        {
            connectionConfirmPanel = CreatePanel("ConnectionConfirmPanel", new Vector2(0, 0), new Vector2(800, 600));
            
            // 默认禁用面板背景图片（取消勾选）
            Image panelImage = connectionConfirmPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.enabled = false;
            }
            
            // Title
            CreateText(connectionConfirmPanel, "TitleText", "Chat with Nomi", new Vector2(0, 260), new Vector2(700, 60), 48);
            
            // REMOVED: Chat ScrollView for dialogue history (AI responses shown via 3D speech bubble)
            // chatScrollView = CreateChatScrollView(connectionConfirmPanel, "ChatScrollView", new Vector2(0, 50), new Vector2(700, 400));
            // chatContentContainer = chatScrollView.content.gameObject;
            
            // Input field (moved up to avoid overlap)
            chatInputField = CreateInputField(connectionConfirmPanel, "ChatInput", "Type your message...", new Vector2(-150, -180), new Vector2(500, 60));
            
            // Send button (moved up with input field)
            sendButton = CreateButton(connectionConfirmPanel, "SendButton", "Send", new Vector2(200, -180), new Vector2(150, 60));
            
            // Listening text (hidden initially, shown during AI processing)
            listeningText = CreateText(connectionConfirmPanel, "ListeningText", "Thinking...", new Vector2(0, -120), new Vector2(500, 40), 28);
            listeningText.gameObject.SetActive(false);
            
            // Finish button (moved down to avoid overlap with input)
            finishConnectionButton = CreateButton(connectionConfirmPanel, "FinishConnectButton", "Continue", new Vector2(0, -300), new Vector2(250, 60));
            
            // === 修改Continue按钮逻辑：根据首次运行状态决定跳转 ===
            finishConnectionButton.onClick.RemoveAllListeners();
            finishConnectionButton.onClick.AddListener(() => {
                // 检查是否为首次运行（在DIY完成时已标记为1）
                bool firstRunCompleted = PlayerPrefs.GetInt("FirstRun_Completed", 0) == 1;
                
                if (MRSceneStateManager.Instance != null)
                {
                    if (firstRunCompleted)
                    {
                        // 首次运行完成后进入主界面
                        Debug.Log("💬 首次聊天完成，进入主界面");
                        MRSceneStateManager.Instance.TransitionToState(MRSceneState.MainMenu);
                    }
                    else
                    {
                        // 后续从主界面Chat进入，返回主界面
                        Debug.Log("💬 聊天结束，返回主界面");
                        MRSceneStateManager.Instance.ReturnToMainMenu();
                    }
                }
            });
        }
        
        private void BuildMainMenuPanel()
        {
            // Main menu consists of sidebar only (dialogue bubble removed)
            mainMenuPanel = new GameObject("MainMenuPanel");
            mainMenuPanel.transform.SetParent(worldSpaceCanvas.transform, false);
            
            // Sidebar
            BuildMainMenuSidebar();
            
            // Dialogue bubble removed - AI responses shown via SpeechBubbleController
            // BuildDialogueBubble();
            
            // Start Healing button (centered at bottom)
            startHealingButton = CreateButton(mainMenuPanel, "StartHealingButton", "Start Healing", new Vector2(0, -400), new Vector2(300, 80));
        }
        
        private void BuildMainMenuSidebar()
        {
            mainMenuSidebar = CreateSubPanel(mainMenuPanel, "Sidebar", new Vector2(-700, 0), new Vector2(300, 700));
            
            // === 顶部图标按钮 - 三个按钮 ===
            gearButton = CreateButton(mainMenuSidebar, "GearButton", "SET", 
                new Vector2(-120, 300), new Vector2(60, 60));
            
            // 新增：Chat按钮（中间位置）
            chatButton = CreateButton(mainMenuSidebar, "ChatButton", "CHAT", 
                new Vector2(0, 300), new Vector2(60, 60));
            
            historyButton = CreateButton(mainMenuSidebar, "HistoryButton", "HIST", 
                new Vector2(120, 300), new Vector2(60, 60));
            
            // 调整按钮文字大小
            Text gearText = gearButton.GetComponentInChildren<Text>();
            if (gearText != null) { gearText.fontSize = 16; gearText.fontStyle = FontStyle.Bold; }
            
            Text chatText = chatButton.GetComponentInChildren<Text>();
            if (chatText != null) { chatText.fontSize = 16; chatText.fontStyle = FontStyle.Bold; }
            
            Text histText = historyButton.GetComponentInChildren<Text>();
            if (histText != null) { histText.fontSize = 14; histText.fontStyle = FontStyle.Bold; }
            
            // === 主功能按钮（不变） ===
            breathingButton = CreateButton(mainMenuSidebar, "BreathingButton", "🫁 呼吸疗愈", 
                new Vector2(0, 150), new Vector2(260, 80));
            altruisticButton = CreateButton(mainMenuSidebar, "AltruisticButton", "🎓 利他疗愈", 
                new Vector2(0, 30), new Vector2(260, 80));
            treeButton = CreateButton(mainMenuSidebar, "TreeButton", "🌳 我的树", 
                new Vector2(0, -90), new Vector2(260, 80));
        }
        
        private void BuildDialogueBubble()
        {
            // REMOVED: Dialogue bubble no longer displayed in main menu
            // AI responses are shown via 3D SpeechBubbleController instead
            // dialogueBubble = CreatePanel("DialogueBubble", new Vector2(0, 300), new Vector2(700, 150));
            // dialogueText = CreateText(dialogueBubble, "DialogueText", "The weather is really nice today.\nDo you want to go out for a walk?", new Vector2(0, 0), new Vector2(650, 100), 24);
        }
        
        private void BuildBreathingPanel()
        {
            breathingPanel = CreatePanel("BreathingPanel", new Vector2(0, 0), new Vector2(800, 600));
            
            // === 全局倒计时（左上角） ===
            globalTimerText = CreateText(breathingPanel, "GlobalTimer", "剩余: 01:21", 
                new Vector2(-250, 250), new Vector2(300, 50), 32);
            globalTimerText.alignment = TextAnchor.MiddleLeft;
            
            // === 中央呼吸指示 ===
            breathingTimerText = CreateText(breathingPanel, "TimerText", "吸气\n4秒\n第1/4周期", 
                new Vector2(0, 0), new Vector2(700, 120), 48);
            
            // === 完成按钮 ===
            finishBreathingButton = CreateButton(breathingPanel, "FinishBreathingButton", "结束", 
                new Vector2(0, -250), new Vector2(300, 80));
        }
        
        private void BuildAltruisticPanel()
        {
            altruisticPanel = CreatePanel("AltruisticPanel", new Vector2(0, 200), new Vector2(700, 300));
            
            // Hand icon
            CreateText(altruisticPanel, "HandIcon", "👋", new Vector2(0, 80), new Vector2(100, 100), 80);
            
            altruisticInstructionText = CreateText(altruisticPanel, "InstructionText", "Try to touch to interact with", new Vector2(0, -20), new Vector2(650, 60), 28);
            
            touchCountText = CreateText(altruisticPanel, "TouchCount", "Touches: 0/3", new Vector2(0, -80), new Vector2(400, 50), 32);
            
            // 创建两个按钮并排（Comfort在左，Finish在右）
            comfortButton = CreateButton(altruisticPanel, "ComfortButton", "Comfort", new Vector2(-160, -140), new Vector2(280, 70));
            finishAltruisticButton = CreateButton(altruisticPanel, "FinishAltruisticButton", "Finish", new Vector2(160, -140), new Vector2(280, 70));
        }
        
        private void BuildTreeControlPanel()
        {
            Debug.Log("🌳 Building Tree Control Panel...");
            
            treeControlPanel = CreatePanel("TreeControlPanel", new Vector2(600, 400), new Vector2(350, 250));
            
            // Season dropdown
            CreateText(treeControlPanel, "SeasonLabel", "Season", new Vector2(0, 80), new Vector2(300, 40), 24);
            
            // 使用简化的下拉框创建方法
            seasonDropdown = CreateSimpleDropdown(treeControlPanel, "SeasonDropdown", new Vector2(0, 40), new Vector2(300, 50));
            
            if (seasonDropdown != null)
            {
                Debug.Log($"✅ Season dropdown created successfully!");
                Debug.Log($"   - Options count: {seasonDropdown.options.Count}");
                Debug.Log($"   - Template: {seasonDropdown.template != null}");
                Debug.Log($"   - CaptionText: {seasonDropdown.captionText != null}");
                Debug.Log($"   - ItemText: {seasonDropdown.itemText != null}");
            }
            else
            {
                Debug.LogError("❌ Failed to create season dropdown!");
            }
            
            // Buttons
            resetOrbsButton = CreateButton(treeControlPanel, "ResetOrbsButton", "Reset Orbs", new Vector2(0, -30), new Vector2(280, 60));
            closeTreeButton = CreateButton(treeControlPanel, "CloseButton", "Close Controls", new Vector2(0, -100), new Vector2(280, 60));
            
            Debug.Log("✅ Tree Control Panel build complete!");
        }
        
        private void BuildHistoryPanel()
        {
            historyPanel = CreatePanel("HistoryPanel", new Vector2(0, 0), new Vector2(900, 800));
            
            // Title
            CreateText(historyPanel, "HistoryTitle", "Today's overview", new Vector2(0, 350), new Vector2(800, 60), 40);
            
            // Breathing summary
            GameObject summaryPanel = CreateSubPanel(historyPanel, "SummaryPanel", new Vector2(0, 250), new Vector2(800, 100));
            CreateText(summaryPanel, "BreathingSummary", "Breathing\n65 sec  [12sec]", new Vector2(-250, 0), new Vector2(300, 80), 20);
            CreateText(summaryPanel, "HealingSummary", "Healing\nHeart Rate indicates less anxiety", new Vector2(250, 0), new Vector2(400, 80), 20);
            
            // History label
            CreateText(historyPanel, "HistoryLabel", "History", new Vector2(0, 140), new Vector2(800, 50), 32);
            
            // Scroll view for history
            GameObject scrollView = new GameObject("HistoryScrollView");
            scrollView.transform.SetParent(historyPanel.transform, false);
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchoredPosition = new Vector2(0, -50);
            scrollRect.sizeDelta = new Vector2(800, 400);
            
            historyScrollRect = scrollView.AddComponent<ScrollRect>();
            Image scrollBg = scrollView.AddComponent<Image>();
            scrollBg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            
            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(scrollView.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(750, 400);
            contentRect.pivot = new Vector2(0.5f, 1f);
            
            historyContentParent = content.transform;
            historyScrollRect.content = contentRect;
            historyScrollRect.horizontal = false;
            historyScrollRect.vertical = true;
            
            // Back button
            backFromHistoryButton = CreateButton(historyPanel, "BackButton", "Back", new Vector2(0, -360), new Vector2(300, 80));
        }
        
        // ============================================================================
        // UI Component Creators
        // ============================================================================
        
        private GameObject CreatePanel(string name, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(worldSpaceCanvas.transform, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = panel.AddComponent<Image>();
            image.color = panelBackgroundColor;
            
            return panel;
        }
        
        private GameObject CreateSubPanel(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            GameObject subPanel = new GameObject(name);
            subPanel.transform.SetParent(parent.transform, false);
            
            RectTransform rect = subPanel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = subPanel.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.6f);
            
            return subPanel;
        }
        
        private Text CreateText(GameObject parent, string name, string content, Vector2 position, Vector2 size, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Text text = textObj.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            
            return text;
        }
        
        private Button CreateButton(GameObject parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = buttonColor;
            
            Button button = buttonObj.AddComponent<Button>();
            
            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            
            return button;
        }
        
        private Button CreateIconButton(GameObject parent, string name, string icon, Vector2 position, Vector2 size)
        {
            Button button = CreateButton(parent, name, icon, position, size);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) text.fontSize = 36;
            return button;
        }
        
        private Button CreateColorButton(GameObject parent, string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = color;
            
            Button button = buttonObj.AddComponent<Button>();
            
            return button;
        }
        
        private Slider CreateSlider(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Slider slider = sliderObj.AddComponent<Slider>();
            
            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = buttonColor;
            
            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;
            
            // Handle
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage; // Critical for interaction
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0.5f;
            slider.interactable = true;
            
            return slider;
        }
        
        /// <summary>
        /// 创建下拉框（完整实现）
        /// </summary>
        private Dropdown CreateDropdown(GameObject parent, string name, Vector2 position, Vector2 size, List<string> options)
        {
            GameObject dropdownObj = new GameObject(name);
            dropdownObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = dropdownObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = dropdownObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();
            
            // === Label（显示当前选中的选项） ===
            GameObject label = new GameObject("Label");
            label.transform.SetParent(dropdownObj.transform, false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-30, 0);
            Text labelText = label.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 28;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            
            // === Arrow（下拉箭头） ===
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(dropdownObj.transform, false);
            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = Vector2.one;
            arrowRect.sizeDelta = new Vector2(20, 0);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            Text arrowText = arrow.AddComponent<Text>();
            arrowText.text = "▼";
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            arrowText.fontSize = 16;
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = Color.white;
            
            // === Template（下拉列表模板） ===
            GameObject template = new GameObject("Template");
            template.transform.SetParent(dropdownObj.transform, false);
            template.SetActive(false);
            RectTransform templateRect = template.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            // 调整高度以适应5个选项（每个35像素 + 一些边距）
            templateRect.sizeDelta = new Vector2(0, 200);
            
            // 添加Template背景
            Image templateBg = template.AddComponent<Image>();
            templateBg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            
            // === Viewport（可视区域，带遮罩） ===
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = new Vector2(-10, -10);  // 留边距
            viewportRect.anchoredPosition = Vector2.zero;
            
            // 添加Mask组件（关键！）
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.clear;
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            // === Scrollbar（滚动条，可选但建议添加） ===
            GameObject scrollbar = new GameObject("Scrollbar");
            scrollbar.transform.SetParent(template.transform, false);
            RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            scrollbarRect.anchoredPosition = new Vector2(-10, 0);
            
            Image scrollbarBg = scrollbar.AddComponent<Image>();
            scrollbarBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;
            
            GameObject scrollbarHandle = new GameObject("Sliding Area");
            scrollbarHandle.transform.SetParent(scrollbar.transform, false);
            RectTransform handleAreaRect = scrollbarHandle.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20, -20);
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(scrollbarHandle.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            
            scrollbarComponent.handleRect = handleRect;
            scrollbarComponent.targetGraphic = handleImage;
            
            // 添加ScrollRect到viewport
            ScrollRect scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.verticalScrollbar = scrollbarComponent;
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            
            // === Content（选项容器） ===
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            // 根据选项数量计算高度（每个选项35像素）
            contentRect.sizeDelta = new Vector2(0, options.Count * 35);
            
            scrollRect.content = contentRect;
            
            // === 添加VerticalLayoutGroup以自动排列选项 ===
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 0;
            
            // === ContentSizeFitter以自动调整高度 ===
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // === ToggleGroup（用于管理选项的单选行为） ===
            ToggleGroup toggleGroup = content.AddComponent<ToggleGroup>();
            
            // === Item（选项模板） ===
            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.pivot = new Vector2(0.5f, 1);
            itemRect.sizeDelta = new Vector2(0, 35);  // 每个选项高度
            itemRect.anchoredPosition = Vector2.zero;
            
            // 添加LayoutElement确保高度固定为35像素
            LayoutElement itemLayout = item.AddComponent<LayoutElement>();
            itemLayout.minHeight = 35;
            itemLayout.preferredHeight = 35;
            
            // Item背景
            Image itemBg = item.AddComponent<Image>();
            itemBg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            
            // Item Toggle（关联到ToggleGroup）
            Toggle itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBg;
            itemToggle.group = toggleGroup;
            itemToggle.isOn = false;
            
            // Item背景（选中状态）
            GameObject itemBackground = new GameObject("Item Background");
            itemBackground.transform.SetParent(item.transform, false);
            RectTransform itemBgRect = itemBackground.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            Image itemBgImage = itemBackground.AddComponent<Image>();
            itemBgImage.color = new Color(0.4f, 0.6f, 0.8f, 0.5f);  // 选中时的颜色
            
            // Item Checkmark
            GameObject itemCheckmark = new GameObject("Item Checkmark");
            itemCheckmark.transform.SetParent(item.transform, false);
            RectTransform checkmarkRect = itemCheckmark.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = new Vector2(0, 1);
            checkmarkRect.sizeDelta = new Vector2(20, 0);
            checkmarkRect.anchoredPosition = new Vector2(10, 0);
            Text checkmarkText = itemCheckmark.AddComponent<Text>();
            checkmarkText.text = "✓";
            checkmarkText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            checkmarkText.fontSize = 20;
            checkmarkText.alignment = TextAnchor.MiddleCenter;
            checkmarkText.color = Color.white;
            
            // Item Label - 必须在checkmark和background之后添加，确保渲染顺序正确
            GameObject itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            // 设置在层级最后，确保显示在最上层
            itemLabel.transform.SetAsLastSibling();
            
            RectTransform itemLabelRect = itemLabel.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(30, 2);  // 留出checkmark空间，上下留2像素边距
            itemLabelRect.offsetMax = new Vector2(-5, -2);
            
            Text itemLabelText = itemLabel.AddComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabelText.fontSize = 20;  // 稍微减小字体，确保在35像素高度内显示良好
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.color = Color.white;
            itemLabelText.text = "Option";  // 设置默认文本
            itemLabelText.supportRichText = false;
            itemLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            itemLabelText.verticalOverflow = VerticalWrapMode.Truncate;
            
            // 配置Toggle - 使用checkmark作为graphic（选中时显示✓）
            itemToggle.graphic = checkmarkText;
            itemToggle.isOn = false;
            
            // 配置Toggle颜色
            ColorBlock colors = itemToggle.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            itemToggle.colors = colors;
            
            // 配置targetGraphic（背景在hover时高亮）
            itemToggle.targetGraphic = itemBg;
            
            // === 配置Dropdown ===
            dropdown.targetGraphic = image;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;
            dropdown.template = templateRect;
            
            // 先清空现有选项
            dropdown.ClearOptions();
            
            // 添加新选项
            if (options != null && options.Count > 0)
            {
                // 清空并重新添加选项
                dropdown.ClearOptions();
                dropdown.AddOptions(options);
                
                // 强制更新content高度以适应所有选项
                Transform viewportTransform = dropdown.template.Find("Viewport");
                if (viewportTransform != null)
                {
                    Transform contentTransform = viewportTransform.Find("Content");
                    if (contentTransform != null)
                    {
                        RectTransform dynamicContentRect = contentTransform.GetComponent<RectTransform>();
                        if (dynamicContentRect != null)
                        {
                            dynamicContentRect.sizeDelta = new Vector2(0, options.Count * 35);
                        }
                    }
                }
                
                // 设置初始值并刷新显示
                dropdown.value = 0;
                dropdown.RefreshShownValue();
                
                // 强制更新布局（关键！）
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(dropdown.template);
                
                Debug.Log($"✅ Created dropdown '{name}' with {options.Count} options: {string.Join(", ", options)}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Dropdown '{name}' created with no options!");
            }
            
            return dropdown;
        }
        
        /// <summary>
        /// 创建简化版下拉框（更可靠的实现）
        /// </summary>
        private Dropdown CreateSimpleDropdown(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            // 创建下拉框主对象
            GameObject dropdownObj = new GameObject(name);
            dropdownObj.transform.SetParent(parent.transform, false);
            
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchoredPosition = position;
            dropdownRect.sizeDelta = size;
            
            // 添加背景
            Image dropdownBg = dropdownObj.AddComponent<Image>();
            dropdownBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // 添加Dropdown组件
            Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();
            
            // === 1. Label（显示当前选项） ===
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 0);
            labelRect.offsetMax = new Vector2(-30, 0);
            
            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 28;  // 增大字体
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.text = "Default";
            labelText.supportRichText = false;
            
            // === 2. Arrow（箭头） ===
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            
            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = Vector2.one;
            arrowRect.sizeDelta = new Vector2(20, 0);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            
            Text arrowText = arrowObj.AddComponent<Text>();
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            arrowText.fontSize = 16;
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.color = Color.white;
            arrowText.text = "▼";
            
            // === 3. Template（下拉列表模板） ===
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            templateObj.SetActive(false);
            
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, -2);
            templateRect.sizeDelta = new Vector2(0, 180);  // 足够容纳5个选项
            
            Image templateBg = templateObj.AddComponent<Image>();
            templateBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // === 4. Viewport ===
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            
            Image viewportImg = viewportObj.AddComponent<Image>();
            viewportImg.color = Color.clear;
            
            // 添加Mask但默认禁用，这样内容就可见了
            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewportMask.enabled = false;  // 默认禁用Mask，让内容可见
            
            // 添加ScrollRect组件
            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            // === 5. Content ===
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 150);  // 5个选项 × 30像素
            
            // 添加垂直布局组件，自动排列选项
            VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 0;
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            
            // 添加ContentSizeFitter以适应内容
            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // 连接Content到ScrollRect
            scrollRect.content = contentRect;
            
            // 注意：不要添加ToggleGroup！Dropdown会自己管理选项的单选行为
            
            // === 6. Item（选项模板） ===
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 30);
            
            // 添加LayoutElement确保固定高度
            LayoutElement itemLayout = itemObj.AddComponent<LayoutElement>();
            itemLayout.minHeight = 30;
            itemLayout.preferredHeight = 30;
            itemLayout.flexibleHeight = 0;
            
            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            
            // Item背景
            GameObject itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            
            RectTransform itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            
            Image itemBgImg = itemBgObj.AddComponent<Image>();
            itemBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Item Checkmark
            GameObject checkmarkObj = new GameObject("Item Checkmark");
            checkmarkObj.transform.SetParent(itemObj.transform, false);
            
            RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = new Vector2(0, 1);
            checkmarkRect.sizeDelta = new Vector2(20, 0);
            checkmarkRect.anchoredPosition = new Vector2(10, 0);
            
            Image checkmarkImg = checkmarkObj.AddComponent<Image>();
            checkmarkImg.color = Color.white;
            
            // Item Label
            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            
            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(25, 1);
            itemLabelRect.offsetMax = new Vector2(-5, -1);
            
            Text itemLabelText = itemLabelObj.AddComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabelText.fontSize = 24;  // 增大字体
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            itemLabelText.color = Color.white;
            itemLabelText.text = "Option";
            itemLabelText.supportRichText = false;
            itemLabelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            itemLabelText.verticalOverflow = VerticalWrapMode.Truncate;
            
            // 配置Toggle - 使用最简单的配置
            itemToggle.targetGraphic = itemBgImg;
            itemToggle.graphic = checkmarkImg;
            itemToggle.isOn = false;
            
            // 配置Toggle的transition（避免动画问题）
            itemToggle.transition = Selectable.Transition.None;
            
            // === 配置Dropdown ===
            dropdown.targetGraphic = dropdownBg;
            dropdown.template = templateRect;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;
            
            // 添加选项
            dropdown.ClearOptions();
            List<string> options = new List<string> { "Default", "Spring", "Summer", "Autumn", "Winter" };
            dropdown.AddOptions(options);
            dropdown.value = 0;
            dropdown.RefreshShownValue();
            
            Debug.Log($"✅ Created simple dropdown with {options.Count} options");
            
            return dropdown;
        }
        
        private InputField CreateInputField(GameObject parent, string name, string placeholder, Vector2 position, Vector2 size)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image bg = inputObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            InputField inputField = inputObj.AddComponent<InputField>();
            inputField.targetGraphic = bg;
            
            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 0);
            placeholderRect.offsetMax = new Vector2(-10, 0);
            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 24;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            
            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.lineType = InputField.LineType.MultiLineNewline;
            
            return inputField;
        }
        
        private ScrollRect CreateChatScrollView(GameObject parent, string name, Vector2 position, Vector2 size)
        {
            GameObject scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = scrollObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image bg = scrollObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
            
            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.offsetMin = new Vector2(10, 10);
            viewportRect.offsetMax = new Vector2(-10, -10);
            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = Color.clear;
            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            
            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = null;
            
            return scrollRect;
        }
        
        // ============================================================================
        // Public Show/Hide Methods
        // ============================================================================
        
        public void HideAllPanels()
        {
            if (welcomePanel != null) welcomePanel.SetActive(false);
            HideCustomizationPanel();
            HideConnectionConfirmPanel();
            HideMainMenuPanel();
            HideBreathingPanel();
            HideAltruisticPanel();
            HideTreeControlPanel();
            HideHistoryPanel();
        }
        
        public void ShowCustomizationPanel()
        {
            if (customizationPanel != null) customizationPanel.SetActive(true);
        }
        
        public void HideCustomizationPanel()
        {
            if (customizationPanel != null) customizationPanel.SetActive(false);
        }
        
        public void ShowConnectionConfirmPanel()
        {
            if (connectionConfirmPanel != null) connectionConfirmPanel.SetActive(true);
        }
        
        public void HideConnectionConfirmPanel()
        {
            if (connectionConfirmPanel != null) connectionConfirmPanel.SetActive(false);
        }
        
        public void ShowMainMenuPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (mainMenuSidebar != null) mainMenuSidebar.SetActive(true);
            if (dialogueBubble != null) dialogueBubble.SetActive(true);
        }
        
        public void HideMainMenuPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        }
        
        public void HideMainMenuSidebar()
        {
            if (mainMenuSidebar != null) mainMenuSidebar.SetActive(false);
        }
        
        public void ShowBreathingPanel()
        {
            if (breathingPanel != null) breathingPanel.SetActive(true);
        }
        
        public void HideBreathingPanel()
        {
            if (breathingPanel != null) breathingPanel.SetActive(false);
        }
        
        public void ShowAltruisticPanel()
        {
            if (altruisticPanel != null) altruisticPanel.SetActive(true);
        }
        
        public void HideAltruisticPanel()
        {
            if (altruisticPanel != null) altruisticPanel.SetActive(false);
        }
        
        public void ShowTreeControlPanel()
        {
            if (treeControlPanel != null) treeControlPanel.SetActive(true);
        }
        
        public void HideTreeControlPanel()
        {
            if (treeControlPanel != null) treeControlPanel.SetActive(false);
        }
        
        public void ShowHistoryPanel()
        {
            if (historyPanel != null) historyPanel.SetActive(true);
        }
        
        public void HideHistoryPanel()
        {
            if (historyPanel != null) historyPanel.SetActive(false);
        }
        
        // Helper to create history card
        public GameObject CreateHistoryCard(string date, string time, string description)
        {
            GameObject card = CreateSubPanel(historyContentParent.gameObject, $"Card_{date}_{time}", Vector2.zero, new Vector2(700, 120));
            
            CreateText(card, "Date", date, new Vector2(-250, 30), new Vector2(100, 40), 32);
            CreateText(card, "Time", time, new Vector2(-250, -10), new Vector2(100, 30), 20);
            CreateText(card, "Description", description, new Vector2(50, 0), new Vector2(400, 60), 22);
            CreateButton(card, "RecallButton", "Recall", new Vector2(280, 0), new Vector2(120, 50));
            
            return card;
        }
        
        // ============================================================================
        // Build Task Overlay Panel
        // ============================================================================
        
        private void BuildTaskOverlayPanel()
        {
            Debug.Log("🎨 Building Task Overlay Panel...");
            
            // Create full-screen overlay (like tree_final.html #task-overlay)
            taskOverlayPanel = new GameObject("TaskOverlay");
            taskOverlayPanel.transform.SetParent(worldSpaceCanvas.transform, false);
            
            // Full canvas coverage for background
            RectTransform overlayRect = taskOverlayPanel.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            overlayRect.anchoredPosition = Vector2.zero;
            
            // Semi-transparent dark background (rgba(16, 24, 32, 0.85))
            Image overlayBg = taskOverlayPanel.AddComponent<Image>();
            overlayBg.color = new Color(16f/255f, 24f/255f, 32f/255f, 0.85f);
            
            // Make background clickable to dismiss
            Button overlayButton = taskOverlayPanel.AddComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;
            
            // Create centered content panel (similar to tree_final.html styling)
            GameObject contentPanel = CreateSubPanel(taskOverlayPanel, "TaskContent", Vector2.zero, new Vector2(600, 400));
            RectTransform contentRect = contentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            
            // Styled background for content panel (border-radius: 16px equivalent)
            Image contentBg = contentPanel.GetComponent<Image>();
            if (contentBg != null)
            {
                contentBg.color = new Color(0.1f, 0.15f, 0.2f, 0.95f);
            }
            
            // Title text
            CreateText(contentPanel, "TaskTitle", "Behavioral Activation Task", new Vector2(0, 140), new Vector2(560, 50), 40);
            
            // Task description text
            taskOverlayText = CreateText(contentPanel, "TaskDescription", "", new Vector2(0, 0), new Vector2(560, 240), 28);
            taskOverlayText.alignment = TextAnchor.MiddleCenter;
            taskOverlayText.horizontalOverflow = HorizontalWrapMode.Wrap;
            taskOverlayText.verticalOverflow = VerticalWrapMode.Overflow;
            
            // Dismiss button
            Button dismissButton = CreateButton(contentPanel, "DismissButton", "Got it!", new Vector2(0, -140), new Vector2(200, 60));
            dismissButton.onClick.AddListener(() => HideTaskOverlay());
            
            // Initially hidden
            taskOverlayPanel.SetActive(false);
            
            Debug.Log("✅ Task Overlay Panel built successfully");
        }
        
        // ============================================================================
        // Task Overlay Methods
        // ============================================================================
        
        public void ShowTaskOverlay(string taskText)
        {
            if (taskOverlayPanel != null && taskOverlayText != null)
            {
                taskOverlayText.text = taskText;
                taskOverlayPanel.SetActive(true);
                Debug.Log($"📋 Task overlay shown: {taskText}");
            }
        }
        
        public void HideTaskOverlay()
        {
            if (taskOverlayPanel != null)
            {
                taskOverlayPanel.SetActive(false);
                Debug.Log("📋 Task overlay hidden");
            }
        }
    }
}

