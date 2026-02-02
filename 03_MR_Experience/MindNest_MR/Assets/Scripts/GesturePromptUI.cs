/*
 * GesturePromptUI.cs
 * ==================
 * 
 * 手势提示UI控制器
 * 
 * 功能：
 * - 显示当前需要做的手势动作
 * - 实时显示手势识别进度
 * - 显示摄像头预览窗口（小窗）
 * - 手势成功/失败的视觉反馈
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MindNest.MR
{
    /// <summary>
    /// 手势提示UI控制器（支持模式切换）
    /// </summary>
    public class GesturePromptUI : MonoBehaviour
    {
        [Header("UI元素引用")]
        public RawImage cameraPreview;           // 摄像头预览窗口
        public Text promptText;                  // 提示文本
        public Image gestureIcon;                // 手势图标
        public Text progressText;                // 进度文本
        public Image feedbackImage;              // 反馈图像（成功/失败）
        public Text feedbackText;                // 反馈文本
        
        [Header("模式切换UI")]
        public Button modeSwitchButton;          // 模式切换按钮
        public Text modeStatusText;              // 模式状态文本
        public GameObject fallbackHintPanel;     // 回退提示面板
        
        [Header("UI父容器")]
        public GameObject uiContainer;           // UI总容器
        
        [Header("交互模式管理器")]
        public InteractionModeManager interactionModeManager;  // 交互模式管理器
        
        [Header("摄像头预览设置")]
        public Vector2 previewSize = new Vector2(320, 240);
        public Vector2 previewPosition = new Vector2(-300, 200);  // 相对位置
        
        [Header("反馈设置")]
        public float feedbackDuration = 1.5f;
        public Color successColor = Color.green;
        public Color failColor = Color.red;
        
        [Header("调试")]
        public bool enableDebugLog = true;
        
        // ============================================================================
        // 内部状态
        // ============================================================================
        
        #pragma warning disable 0414
        private bool isVisible = false;
        #pragma warning restore 0414
        private Coroutine feedbackCoroutine;
        
        // ============================================================================
        // Unity生命周期
        // ============================================================================
        
        void Start()
        {
            // 默认隐藏UI
            if (uiContainer != null)
            {
                uiContainer.SetActive(false);
            }
            
            // 连接按钮事件
            if (modeSwitchButton != null)
            {
                modeSwitchButton.onClick.AddListener(OnModeSwitchClicked);
            }
            
            // 连接交互模式管理器事件
            if (interactionModeManager != null)
            {
                interactionModeManager.OnModeChanged += OnModeChanged;
                interactionModeManager.OnFallbackSuggested += OnFallbackSuggested;
                UpdateModeStatusUI(interactionModeManager.currentMode);
            }
            
            // 默认隐藏回退提示
            if (fallbackHintPanel != null)
            {
                fallbackHintPanel.SetActive(false);
            }
            
            LogInfo("GesturePromptUI initialized");
        }
        
        void OnDestroy()
        {
            if (modeSwitchButton != null)
            {
                modeSwitchButton.onClick.RemoveListener(OnModeSwitchClicked);
            }
            
            if (interactionModeManager != null)
            {
                interactionModeManager.OnModeChanged -= OnModeChanged;
                interactionModeManager.OnFallbackSuggested -= OnFallbackSuggested;
            }
        }
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 显示UI
        /// </summary>
        public void ShowUI()
        {
            if (uiContainer != null)
            {
                uiContainer.SetActive(true);
                isVisible = true;
                LogInfo("UI shown");
            }
        }
        
        /// <summary>
        /// 隐藏UI
        /// </summary>
        public void HideUI()
        {
            if (uiContainer != null)
            {
                uiContainer.SetActive(false);
                isVisible = false;
                LogInfo("UI hidden");
            }
        }
        
        /// <summary>
        /// 设置摄像头预览纹理
        /// </summary>
        public void SetCameraTexture(WebCamTexture texture)
        {
            if (cameraPreview != null)
            {
                cameraPreview.texture = texture;
                LogInfo("Camera texture set");
            }
        }
        
        /// <summary>
        /// 更新手势提示
        /// </summary>
        public void UpdateGesturePrompt(GestureType gestureType)
        {
            // 更新提示文本
            if (promptText != null)
            {
                string description = GestureEvent.GetGestureDescription(gestureType);
                promptText.text = $"请做出 [{description}] 手势";
            }
            
            // 更新手势图标
            if (gestureIcon != null)
            {
                string iconName = GestureEvent.GetGestureIconName(gestureType);
                Texture2D icon = Resources.Load<Texture2D>($"GestureIcons/{iconName}");
                
                if (icon != null)
                {
                    gestureIcon.sprite = Sprite.Create(
                        icon,
                        new Rect(0, 0, icon.width, icon.height),
                        new Vector2(0.5f, 0.5f)
                    );
                    gestureIcon.enabled = true;
                }
                else
                {
                    // 如果没有图标，隐藏图标显示
                    gestureIcon.enabled = false;
                    LogWarning($"Gesture icon not found: {iconName}");
                }
            }
            
            LogInfo($"Prompt updated: {GestureEvent.GetGestureDescription(gestureType)}");
        }
        
        /// <summary>
        /// 更新进度显示
        /// </summary>
        public void UpdateProgress(int current, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"进度: {current}/{total}";
            }
        }
        
        /// <summary>
        /// 显示成功反馈
        /// </summary>
        public void ShowSuccessFeedback(string message = "识别成功！")
        {
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            
            feedbackCoroutine = StartCoroutine(ShowFeedbackCoroutine(message, successColor));
        }
        
        /// <summary>
        /// 显示失败反馈
        /// </summary>
        public void ShowFailFeedback(string message = "请重试")
        {
            if (feedbackCoroutine != null)
            {
                StopCoroutine(feedbackCoroutine);
            }
            
            feedbackCoroutine = StartCoroutine(ShowFeedbackCoroutine(message, failColor));
        }
        
        /// <summary>
        /// 清除所有显示
        /// </summary>
        public void ClearDisplay()
        {
            if (promptText != null)
            {
                promptText.text = "";
            }
            
            if (progressText != null)
            {
                progressText.text = "";
            }
            
            if (feedbackText != null)
            {
                feedbackText.text = "";
            }
            
            if (feedbackImage != null)
            {
                feedbackImage.enabled = false;
            }
            
            if (gestureIcon != null)
            {
                gestureIcon.enabled = false;
            }
        }
        
        // ============================================================================
        // UI创建（如果没有预设UI）
        // ============================================================================
        
        /// <summary>
        /// 动态创建UI元素
        /// </summary>
        public void CreateUI(Canvas parentCanvas)
        {
            if (parentCanvas == null)
            {
                LogError("Parent canvas is null");
                return;
            }
            
            LogInfo("Creating gesture UI dynamically...");
            
            // 创建容器
            GameObject container = new GameObject("GesturePromptUI_Container");
            container.transform.SetParent(parentCanvas.transform, false);
            uiContainer = container;
            
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            // 创建摄像头预览
            CreateCameraPreview(container.transform);
            
            // 创建提示文本
            CreatePromptText(container.transform);
            
            // 创建手势图标
            CreateGestureIcon(container.transform);
            
            // 创建进度文本
            CreateProgressText(container.transform);
            
            // 创建反馈显示
            CreateFeedbackDisplay(container.transform);
            
            LogInfo("UI created successfully");
        }
        
        private void CreateCameraPreview(Transform parent)
        {
            GameObject previewObj = new GameObject("CameraPreview");
            previewObj.transform.SetParent(parent, false);
            
            RectTransform rect = previewObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = previewSize;
            rect.anchoredPosition = previewPosition;
            
            cameraPreview = previewObj.AddComponent<RawImage>();
            cameraPreview.color = new Color(1, 1, 1, 0.9f);
            
            // 添加边框
            Outline outline = previewObj.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, -2);
        }
        
        private void CreatePromptText(Transform parent)
        {
            GameObject textObj = new GameObject("PromptText");
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.8f);
            rect.anchorMax = new Vector2(0.5f, 0.8f);
            rect.sizeDelta = new Vector2(600, 80);
            rect.anchoredPosition = Vector2.zero;
            
            promptText = textObj.AddComponent<Text>();
            promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            promptText.fontSize = 32;
            promptText.alignment = TextAnchor.MiddleCenter;
            promptText.color = Color.white;
            promptText.text = "准备开始...";
            
            // 添加阴影
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(2, -2);
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
        }
        
        private void CreateGestureIcon(Transform parent)
        {
            GameObject iconObj = new GameObject("GestureIcon");
            iconObj.transform.SetParent(parent, false);
            
            RectTransform rect = iconObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.65f);
            rect.anchorMax = new Vector2(0.5f, 0.65f);
            rect.sizeDelta = new Vector2(128, 128);
            rect.anchoredPosition = Vector2.zero;
            
            gestureIcon = iconObj.AddComponent<Image>();
            gestureIcon.color = Color.white;
            gestureIcon.enabled = false;
        }
        
        private void CreateProgressText(Transform parent)
        {
            GameObject textObj = new GameObject("ProgressText");
            textObj.transform.SetParent(parent, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.9f);
            rect.anchorMax = new Vector2(0.5f, 0.9f);
            rect.sizeDelta = new Vector2(300, 50);
            rect.anchoredPosition = Vector2.zero;
            
            progressText = textObj.AddComponent<Text>();
            progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            progressText.fontSize = 24;
            progressText.alignment = TextAnchor.MiddleCenter;
            progressText.color = Color.white;
            progressText.text = "进度: 0/3";
            
            Shadow shadow = textObj.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(2, -2);
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
        }
        
        private void CreateFeedbackDisplay(Transform parent)
        {
            GameObject feedbackObj = new GameObject("FeedbackDisplay");
            feedbackObj.transform.SetParent(parent, false);
            
            RectTransform rect = feedbackObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400, 150);
            rect.anchoredPosition = Vector2.zero;
            
            feedbackImage = feedbackObj.AddComponent<Image>();
            feedbackImage.color = new Color(0, 0, 0, 0.7f);
            feedbackImage.enabled = false;
            
            // 反馈文本
            GameObject textObj = new GameObject("FeedbackText");
            textObj.transform.SetParent(feedbackObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            feedbackText = textObj.AddComponent<Text>();
            feedbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            feedbackText.fontSize = 40;
            feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackText.color = Color.white;
            feedbackText.fontStyle = FontStyle.Bold;
        }
        
        // ============================================================================
        // 协程
        // ============================================================================
        
        private IEnumerator ShowFeedbackCoroutine(string message, Color color)
        {
            // 显示反馈
            if (feedbackImage != null)
            {
                feedbackImage.enabled = true;
            }
            
            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = color;
            }
            
            // 等待
            yield return new WaitForSeconds(feedbackDuration);
            
            // 隐藏反馈
            if (feedbackImage != null)
            {
                feedbackImage.enabled = false;
            }
            
            if (feedbackText != null)
            {
                feedbackText.text = "";
            }
        }
        
        // ============================================================================
        // 模式切换功能
        // ============================================================================
        
        /// <summary>
        /// 模式切换按钮点击事件
        /// </summary>
        private void OnModeSwitchClicked()
        {
            if (interactionModeManager == null) return;
            
            InteractionMode currentMode = interactionModeManager.currentMode;
            InteractionMode newMode;
            
            // 循环切换模式
            switch (currentMode)
            {
                case InteractionMode.GestureOnly:
                    newMode = InteractionMode.ClickOnly;
                    break;
                case InteractionMode.ClickOnly:
                    newMode = InteractionMode.Hybrid;
                    break;
                case InteractionMode.Hybrid:
                    newMode = InteractionMode.AutoFallback;
                    break;
                case InteractionMode.AutoFallback:
                default:
                    newMode = InteractionMode.GestureOnly;
                    break;
            }
            
            interactionModeManager.SwitchMode(newMode);
            LogInfo($"Mode switched to: {newMode}");
        }
        
        /// <summary>
        /// 模式改变事件处理
        /// </summary>
        private void OnModeChanged(InteractionMode newMode)
        {
            UpdateModeStatusUI(newMode);
            
            // 隐藏回退提示
            if (fallbackHintPanel != null)
            {
                fallbackHintPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// 回退建议事件处理
        /// </summary>
        private void OnFallbackSuggested()
        {
            LogWarning("Gesture recognition failed too many times, suggesting click mode");
            
            // 显示回退提示
            if (fallbackHintPanel != null)
            {
                fallbackHintPanel.SetActive(true);
                StartCoroutine(HideFallbackHintAfterDelay(5f));
            }
            
            // 显示反馈消息
            ShowFailFeedback("手势识别失败过多，建议切换到点击模式");
        }
        
        /// <summary>
        /// 更新模式状态UI
        /// </summary>
        private void UpdateModeStatusUI(InteractionMode mode)
        {
            if (modeStatusText == null) return;
            
            string statusText = "";
            switch (mode)
            {
                case InteractionMode.GestureOnly:
                    statusText = "模式：手势识别";
                    break;
                case InteractionMode.ClickOnly:
                    statusText = "模式：点击交互";
                    break;
                case InteractionMode.Hybrid:
                    statusText = "模式：混合模式";
                    break;
                case InteractionMode.AutoFallback:
                    statusText = "模式：智能切换";
                    break;
            }
            
            modeStatusText.text = statusText;
        }
        
        /// <summary>
        /// 延迟隐藏回退提示
        /// </summary>
        private IEnumerator HideFallbackHintAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (fallbackHintPanel != null)
            {
                fallbackHintPanel.SetActive(false);
            }
        }
        
        // ============================================================================
        // 日志工具
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[GesturePromptUI] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[GesturePromptUI] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GesturePromptUI] {message}");
        }
    }
}

