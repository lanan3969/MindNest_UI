/*
 * AltruisticHealingController.cs
 * ===============================
 * 
 * Altruistic Healing System (Comforting Nomi) - 手势识别版本
 * 
 * Implements interactive healing through gesture recognition:
 * - Displays sad Nomi expression
 * - Detects 6 types of gestures (抚摸、戳戳、投喂、抱抱、挥手、比心)
 * - Changes expression based on gesture type
 * - Shows positive speech bubbles
 * - Awards nutrients based on gesture difficulty
 * 
 * Author: MindNest Team
 * Date: 2026-01-29 (Updated with gesture recognition)
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MindNest.MR
{
    /// <summary>
    /// Controls altruistic healing interaction with gesture recognition
    /// </summary>
    public class AltruisticHealingController : MonoBehaviour
    {
        [Header("References")]
        public MRUIManager uiManager;
        public GameObject nomiBillboard;
        public Material nomiMaterial;
        public Camera mainCamera;
        public MindNestMRController mrController;
        public MRSceneStateManager stateManager;
        
        [Header("Gesture System References")]
        public HandDetectionManager handDetector;
        public GestureRecognizer gestureRecognizer;
        public GesturePromptUI gesturePromptUI;
        
        [Header("Enhanced Features")]
        public InteractionModeManager interactionModeManager;  // 交互模式管理器
        public GestureTutorialUI gestureTutorialUI;            // 手势教学UI
        
        [Header("Healing Settings")]
        [Tooltip("Number of gestures required")]
        public int requiredGestures = 5;
        
        [Tooltip("Total nutrients awarded on completion")]
        public int totalNutrientsReward = 100;
        
        [Tooltip("Cooldown between gestures (seconds)")]
        public float gestureCooldown = 2.0f;
        
        [Header("Expression Settings")]
        public string sadExpression = "sad";
        public string happyExpression = "happy";
        
        [Header("Gesture Pool")]
        [Tooltip("可选的手势类型")]
        public GestureType[] availableGestures = new GestureType[]
        {
            GestureType.Stroke,
            GestureType.Poke,
            GestureType.Feed,
            GestureType.Hug,
            GestureType.Wave,
            GestureType.Heart
        };
        
        [Header("Positive Messages")]
        public string[] positiveMessages = new string[]
        {
            "谢谢你！我感觉好多了！",
            "你真好！",
            "我很感激你的帮助！",
            "你让我心里暖暖的！",
            "我现在感觉好多了！"
        };
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private int gestureCount = 0;
        private int totalNutrientsEarned = 0;
        private bool isHealing = false;
        private float lastGestureTime = 0f;
        private GameObject stormCloud;
        
        // 手势追踪
        private GestureType currentRequestedGesture = GestureType.None;
        private List<GestureType> completedGestures = new List<GestureType>();
        
        // 点击检测（备用方案）
        #pragma warning disable 0414
        private bool enableClickDetection = false;
        #pragma warning restore 0414
        private float lastClickTime = 0f;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            Debug.Log("🎓 AltruisticHealingController: Initializing (Gesture Version)");
            
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            SetupGestureSystem();
            SetupUI();
        }
        
        void Update()
        {
            // 检查是否应该启用点击检测
            if (isHealing && interactionModeManager != null && interactionModeManager.IsClickEnabled())
            {
                DetectNomiClick();
            }
        }
        
        void OnDestroy()
        {
            // 清理事件订阅
            if (gestureRecognizer != null)
            {
                gestureRecognizer.OnGestureRecognized -= OnGestureDetected;
            }
        }
        
        // ============================================================================
        // Setup
        // ============================================================================
        
        private void SetupGestureSystem()
        {
            // 如果组件引用为空，尝试查找或创建
            if (handDetector == null)
            {
                handDetector = GetComponent<HandDetectionManager>();
                if (handDetector == null)
                {
                    handDetector = gameObject.AddComponent<HandDetectionManager>();
                }
            }
            
            if (gestureRecognizer == null)
            {
                gestureRecognizer = GetComponent<GestureRecognizer>();
                if (gestureRecognizer == null)
                {
                    gestureRecognizer = gameObject.AddComponent<GestureRecognizer>();
                }
            }
            
            if (gesturePromptUI == null)
            {
                gesturePromptUI = GetComponent<GesturePromptUI>();
                if (gesturePromptUI == null)
                {
                    gesturePromptUI = gameObject.AddComponent<GesturePromptUI>();
                }
            }
            
            // 配置手势识别器
            if (gestureRecognizer != null)
            {
                gestureRecognizer.nomiTransform = nomiBillboard != null ? nomiBillboard.transform : null;
                gestureRecognizer.mainCamera = mainCamera;
                gestureRecognizer.OnGestureRecognized += OnGestureDetected;
            }
            
            // 配置手部检测器
            if (handDetector != null)
            {
                handDetector.OnHandsDetected += OnHandsDetected;
            }
            
            Debug.Log("✅ Gesture system setup complete");
        }
        
        private void SetupUI()
        {
            if (uiManager != null)
            {
                // 绑定Comfort按钮（安抚Nomi）
                if (uiManager.comfortButton != null)
                {
                    uiManager.comfortButton.onClick.AddListener(OnComfortClicked);
                }
                
                // 绑定Finish按钮（直接退出）
                if (uiManager.finishAltruisticButton != null)
                {
                    uiManager.finishAltruisticButton.onClick.AddListener(OnFinishClicked);
                }
            }
            
            // 创建手势UI（如果需要）
            if (gesturePromptUI != null && uiManager != null && uiManager.worldSpaceCanvas != null)
            {
                gesturePromptUI.CreateUI(uiManager.worldSpaceCanvas);
            }
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Start altruistic healing with gesture recognition
        /// </summary>
        public void StartAltruisticHealing()
        {
            Debug.Log("🎓 Starting altruistic healing (Enhanced with Tutorial & Click Fallback)");
            
            // 重置状态
            gestureCount = 0;
            totalNutrientsEarned = 0;
            completedGestures.Clear();
            isHealing = true;
            lastGestureTime = 0f;
            
            // Set Nomi to sad expression
            SetNomiExpression(sadExpression);
            
            // 检查是否启用手势识别
            bool gestureEnabled = interactionModeManager == null || interactionModeManager.IsGestureEnabled();
            
            if (gestureEnabled)
            {
                // 启动摄像头和手部检测
                if (handDetector != null)
            {
                    StartCoroutine(handDetector.StartDetection());
                }
                
                // 显示手势UI
                if (gesturePromptUI != null)
                {
                    gesturePromptUI.ShowUI();
                    
                    // 设置摄像头预览
                    if (handDetector != null)
                    {
                        StartCoroutine(WaitForCameraAndSetPreview());
                    }
                }
            }
            
            // 请求第一个手势（包括教学）
            RequestNextGesture();
            
            // Create storm cloud
            CreateStormCloud();
            
            // 立即设置Finish按钮文字（根据healing path）
            SetupFinishButton();
        }
        
        /// <summary>
        /// Stop altruistic healing
        /// </summary>
        public void StopAltruisticHealing()
        {
            Debug.Log("🎓 Stopping altruistic healing (Gesture Recognition)");
            
            isHealing = false;
            
            // 停止手部检测
            if (handDetector != null)
            {
                handDetector.StopDetection();
            }
            
            // 隐藏手势UI
            if (gesturePromptUI != null)
            {
                gesturePromptUI.HideUI();
            }
            
            // 重置手势识别器
            if (gestureRecognizer != null)
            {
                gestureRecognizer.ResetRecognizer();
            }
            
            // Clean up storm cloud
            if (stormCloud != null)
            {
                Destroy(stormCloud);
                stormCloud = null;
            }
        }
        
        // ============================================================================
        // Gesture Event Handlers
        // ============================================================================
        
        private void OnHandsDetected(HandData[] hands)
        {
            // 将手部数据传递给手势识别器
            if (gestureRecognizer != null && isHealing)
            {
                gestureRecognizer.ProcessHandData(hands);
            }
        }
        
        private void OnGestureDetected(GestureEvent gestureEvent)
        {
            if (!isHealing) return;
            
            // 检查冷却时间
            if (Time.time - lastGestureTime < gestureCooldown)
            {
                return;
            }
            
            // 检查是否是请求的手势
            if (gestureEvent.gestureType != currentRequestedGesture)
            {
                // 如果不是请求的手势，显示提示
                if (gesturePromptUI != null)
                {
                    gesturePromptUI.ShowFailFeedback($"请做 [{GestureEvent.GetGestureDescription(currentRequestedGesture)}] 手势");
                }
                return;
            }
            
            // 成功识别手势
            OnGestureCompleted(gestureEvent);
        }
        
        private void OnGestureCompleted(GestureEvent gestureEvent)
        {
            gestureCount++;
            lastGestureTime = Time.time;
            completedGestures.Add(gestureEvent.gestureType);
            
            Debug.Log($"✅ Gesture completed: {gestureEvent.description} ({gestureCount}/{requiredGestures})");
            
            // 获取奖励
            int reward = GestureEvent.GetGestureReward(gestureEvent.gestureType);
            totalNutrientsEarned += reward;
            
            // 更新Nomi表情
            string expression = GestureEvent.GetGestureExpression(gestureEvent.gestureType);
            SetNomiExpression(expression);
            
            // 显示正面消息
            ShowPositiveMessage();
            
            // 显示成功反馈
            if (gesturePromptUI != null)
            {
                gesturePromptUI.ShowSuccessFeedback($"{gestureEvent.description}成功！+{reward}养料");
            }
            
            // 更新风暴云
            UpdateStormCloud();
            
            // 更新UI进度
            UpdateGestureProgress();
            
            // 检查是否完成所有required次数
            if (gestureCount >= requiredGestures)
            {
                // 显示完成提示，但不自动跳转
                if (gesturePromptUI != null)
                {
                    gesturePromptUI.ShowSuccessFeedback($"太棒了！你已经完成了所有安抚！可以点击Finish结束");
                }
            }
            
            // 请求下一个手势（让用户可以继续安抚或点击Finish）
            StartCoroutine(RequestNextGestureAfterDelay(2f));
        }
        
        // ============================================================================
        // Gesture Management
        // ============================================================================
        
        private void RequestNextGesture()
        {
            // 从可用手势中随机选择一个（避免重复）
            List<GestureType> remaining = new List<GestureType>(availableGestures);
            
            // 移除刚完成的手势（如果有）
            if (currentRequestedGesture != GestureType.None && remaining.Contains(currentRequestedGesture))
            {
                remaining.Remove(currentRequestedGesture);
            }
            
            if (remaining.Count == 0)
            {
                remaining = new List<GestureType>(availableGestures);
            }
            
            // 随机选择
            currentRequestedGesture = remaining[Random.Range(0, remaining.Count)];
            
            // 显示手势教学（如果需要）
            if (gestureTutorialUI != null)
            {
                gestureTutorialUI.ShowTutorial(currentRequestedGesture);
            }
            
            // 更新UI提示
            if (gesturePromptUI != null)
            {
                gesturePromptUI.UpdateGesturePrompt(currentRequestedGesture);
            }
            
            Debug.Log($"📝 Requesting gesture: {GestureEvent.GetGestureDescription(currentRequestedGesture)}");
        }
        
        private void UpdateGestureProgress()
        {
            // 更新进度显示
            if (gesturePromptUI != null)
            {
                gesturePromptUI.UpdateProgress(gestureCount, requiredGestures);
            }
            
            // 更新传统UI（如果存在）
            if (uiManager != null && uiManager.touchCountText != null)
            {
                uiManager.touchCountText.text = $"手势: {gestureCount}/{requiredGestures} | 养料: +{totalNutrientsEarned}";
            }
        }
        
        // ============================================================================
        // Visual Feedback
        // ============================================================================
        
        private void SetNomiExpression(string expressionName)
        {
            if (nomiBillboard == null) return;
            
            Texture2D expressionTexture = Resources.Load<Texture2D>($"Expressions/{expressionName}");
            if (expressionTexture == null)
            {
                Debug.LogWarning($"⚠️ Expression texture not found: {expressionName}");
                return;
            }
            
            Renderer renderer = nomiBillboard.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.mainTexture = expressionTexture;
            }
        }
        
        private void ShowPositiveMessage()
        {
            string message = positiveMessages[Random.Range(0, positiveMessages.Length)];
            
            if (uiManager != null && uiManager.dialogueText != null)
            {
                uiManager.dialogueText.text = message;
            }
            
            Debug.Log($"💬 Nomi says: {message}");
        }
        
        private void CreateStormCloud()
        {
            if (stormCloud == null && nomiBillboard != null)
            {
                // Create storm cloud above Nomi
                stormCloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stormCloud.name = "StormCloud";
                stormCloud.transform.SetParent(nomiBillboard.transform);
                stormCloud.transform.localPosition = new Vector3(0, 0.8f, 0);
                stormCloud.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
                
                Renderer renderer = stormCloud.GetComponent<Renderer>();
                renderer.material.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
                
                // Add floating animation
                stormCloud.AddComponent<CloudFloater>();
            }
            }
            
        private void UpdateStormCloud()
        {
            // Make cloud lighter with each gesture
            if (stormCloud != null)
            {
                Renderer renderer = stormCloud.GetComponent<Renderer>();
                float progress = (float)gestureCount / requiredGestures;
                float alpha = 0.7f * (1f - progress);
                Color cloudColor = Color.Lerp(new Color(0.3f, 0.3f, 0.3f), Color.white, progress);
                cloudColor.a = alpha;
                renderer.material.color = cloudColor;
            }
        }
        
        // ============================================================================
        // Coroutines
        // ============================================================================
        
        private IEnumerator WaitForCameraAndSetPreview()
        {
            // 等待摄像头启动
            float timeout = 5f;
            float elapsed = 0f;
            
            while (handDetector != null && !handDetector.IsRunning() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 设置摄像头预览
            if (handDetector != null && handDetector.IsRunning())
            {
                WebCamTexture camTexture = handDetector.GetCameraTexture();
                if (camTexture != null && gesturePromptUI != null)
                {
                    gesturePromptUI.SetCameraTexture(camTexture);
                    Debug.Log("✅ Camera preview set");
                }
            }
        }
        
        private IEnumerator RequestNextGestureAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (isHealing && gestureCount < requiredGestures)
            {
                // 恢复到悲伤表情
                SetNomiExpression(sadExpression);
                
                // 请求下一个手势
                RequestNextGesture();
            }
        }
        
        private IEnumerator CompleteHealingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            OnHealingComplete();
        }
        
        private void OnHealingComplete()
        {
            Debug.Log("✅ All required gestures completed!");
            
            // Award total nutrients earned
            int currentNutrients = PlayerPrefs.GetInt("TotalNutrients", 0);
            currentNutrients += totalNutrientsEarned;
            PlayerPrefs.SetInt("TotalNutrients", currentNutrients);
            PlayerPrefs.Save();
            
            Debug.Log($"🌱 Awarded {totalNutrientsEarned} nutrients! Total: {currentNutrients}");
            
            // Set happy expression
            SetNomiExpression(happyExpression);
            
            // Show completion message (but don't stop healing - user can click Finish to end)
            if (gesturePromptUI != null)
            {
                gesturePromptUI.ShowSuccessFeedback($"太棒了！获得 {totalNutrientsEarned} 养料！点击Finish继续");
            }
            
            if (uiManager != null && uiManager.touchCountText != null)
            {
                uiManager.touchCountText.text = $"完成！+{totalNutrientsEarned} 养料 (可点击Finish)";
            }
            
            // 注意：不自动停止或跳转，让用户决定何时点击Finish
        }
        
        private IEnumerator ReturnToMainMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (MRSceneStateManager.Instance != null)
            {
                MRSceneStateManager.Instance.ReturnToMainMenu();
            }
        }
        
        // ============================================================================
        // Button Callbacks
        // ============================================================================
        
        private void OnComfortClicked()
        {
            Debug.Log("💝 Comfort button clicked - Comforting Nomi");
            
            // 检查冷却时间
            if (Time.time - lastGestureTime < gestureCooldown)
            {
                Debug.Log($"⏳ Still in cooldown ({Time.time - lastGestureTime:F1}s)");
                return;
            }
            
            // 检查是否已完成
            if (gestureCount >= requiredGestures)
            {
                Debug.Log("✅ Already completed all required gestures");
                if (gesturePromptUI != null)
                {
                    gesturePromptUI.ShowSuccessFeedback("已完成！点击Finish退出");
                }
                return;
            }
            
            // 执行安抚逻辑（相当于一次成功的交互）
            lastGestureTime = Time.time;
            gestureCount++;
            
            // 随机获得养料（基础奖励）
            int nutrientReward = Random.Range(1, 4);
            totalNutrientsEarned += nutrientReward;
            
            Debug.Log($"✨ Comfort successful! Count: {gestureCount}/{requiredGestures}, +{nutrientReward} nutrients");
            
            // 更新Nomi表情（随机开心表情）
            string[] happyExpressions = { happyExpression, "happy2", "happy3" };
            string randomHappy = happyExpressions[Random.Range(0, happyExpressions.Length)];
            SetNomiExpression(randomHappy);
            
            // 显示反馈
            if (gesturePromptUI != null)
            {
                gesturePromptUI.ShowSuccessFeedback($"安抚成功！+{nutrientReward} 养料");
            }
            
            // 更新进度
            UpdateGestureProgress();
            UpdateStormCloud();
            
            // 检查是否完成所有required次数
            if (gestureCount >= requiredGestures)
            {
                Debug.Log("🎉 All comforts completed!");
                // 显示完成提示，但不停止疗愈
                if (gesturePromptUI != null)
                {
                    gesturePromptUI.ShowSuccessFeedback($"太棒了！你已经完成了所有安抚！可以点击Finish结束");
                }
            }
            
            // 继续下一轮，稍后恢复sad表情
            StartCoroutine(ResetToSadAfterDelay(2f));
        }
        
        private IEnumerator ResetToSadAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetNomiExpression(sadExpression);
        }
        
        private void OnFinishClicked()
        {
            Debug.Log("🚪 Finish/Next button clicked");
            
            // 给予已获得的养料（如果有的话）
            if (totalNutrientsEarned > 0)
            {
                int currentNutrients = PlayerPrefs.GetInt("TotalNutrients", 0);
                currentNutrients += totalNutrientsEarned;
                PlayerPrefs.SetInt("TotalNutrients", currentNutrients);
                PlayerPrefs.Save();
                
                Debug.Log($"🌱 Earned {totalNutrientsEarned} nutrients");
            }
            
            // 停止疗愈
            StopAltruisticHealing();
            
            // 检查是引导式疗愈还是独立访问
            if (MRSceneStateManager.Instance != null)
            {
                if (MRSceneStateManager.Instance.ShouldShowNextButton())
                {
                    // 进入下一环节
                    Debug.Log("➡️ Continuing to next healing step");
                    MRSceneStateManager.Instance.ContinueHealingFlow();
                }
                else
                {
                    // 完成疗愈流程
                    Debug.Log("🏁 Finishing healing flow");
                    MRSceneStateManager.Instance.FinishHealingFlow();
                }
            }
            else
            {
                // Fallback: 直接返回主菜单
                Debug.LogWarning("⚠️ MRSceneStateManager.Instance is null, returning to main menu");
                if (MRSceneStateManager.Instance != null)
                {
                    MRSceneStateManager.Instance.ReturnToMainMenu();
                }
            }
        }
        
        // ============================================================================
        // 点击检测（备用方案）
        // ============================================================================
        
        /// <summary>
        /// 检测鼠标点击Nomi（作为手势识别的备用方案）
        /// </summary>
        private void DetectNomiClick()
        {
            // 检查冷却时间
            if (Time.time - lastClickTime < gestureCooldown)
                return;
            
            // 检测鼠标点击
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit))
        {
                    if (hit.collider != null && hit.collider.gameObject == nomiBillboard)
                    {
                        OnNomiClicked();
                    }
                }
            }
            
            // 检测触摸（移动设备）
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    Ray ray = mainCamera.ScreenPointToRay(touch.position);
                    RaycastHit hit;
                    
                    if (Physics.Raycast(ray, out hit))
                    {
                        if (hit.collider != null && hit.collider.gameObject == nomiBillboard)
                        {
                            OnNomiClicked();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理Nomi被点击事件
        /// </summary>
        private void OnNomiClicked()
        {
            lastClickTime = Time.time;
            
            // 记录点击统计
            if (interactionModeManager != null)
            {
                interactionModeManager.RecordClick();
            }
            
            // 增加计数
            gestureCount++;
            
            // 添加奖励（点击奖励较低）
            int clickReward = 5;
            totalNutrientsEarned += clickReward;
            
            Debug.Log($"🖱️ Nomi clicked! Count: {gestureCount}/{requiredGestures}, Reward: +{clickReward}");
            
            // 更新表情
            SetNomiExpression(happyExpression);
            
            // 显示反馈
            ShowPositiveMessage();
            
            if (gesturePromptUI != null)
            {
                gesturePromptUI.ShowSuccessFeedback("点击成功！");
            }
            
            // 更新风暴云
            UpdateStormCloud();
            
            // 更新UI进度
            UpdateGestureProgress();
            
            // 检查是否完成
            if (gestureCount >= requiredGestures)
            {
                StartCoroutine(CompleteHealingAfterDelay(2f));
            }
            else
            {
                // 继续等待下一次点击
                StartCoroutine(ResetExpressionAfterDelay(1.5f));
            }
        }
        
        /// <summary>
        /// 延迟重置表情
        /// </summary>
        private IEnumerator ResetExpressionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetNomiExpression(sadExpression);
        }
        
        /// <summary>
        /// 设置Finish按钮的文字（根据healing path）
        /// </summary>
        private void SetupFinishButton()
        {
            if (uiManager == null || uiManager.finishAltruisticButton == null)
            {
                Debug.LogWarning("⚠️ UI Manager or Finish button not found");
                return;
            }
            
            // 检查是否应该显示 Next 按钮
            bool showNext = MRSceneStateManager.Instance != null && 
                            MRSceneStateManager.Instance.ShouldShowNextButton();
            
            Debug.Log($"🔍 Setting up Finish button, should show Next: {showNext}");
            
            // 更新按钮文字
            UnityEngine.UI.Text buttonText = uiManager.finishAltruisticButton.GetComponentInChildren<UnityEngine.UI.Text>();
            if (buttonText != null)
            {
                buttonText.text = showNext ? "Next →" : "Finish";
                Debug.Log($"✅ Button text set to: {buttonText.text}");
            }
            else
            {
                Debug.LogWarning("⚠️ Button Text component not found");
            }
        }
    }
    
    /// <summary>
    /// Simple component to make cloud float
    /// </summary>
    public class CloudFloater : MonoBehaviour
    {
        private Vector3 startPos;
        
        void Start()
        {
            startPos = transform.localPosition;
        }
        
        void Update()
        {
            float yOffset = Mathf.Sin(Time.time * 0.5f) * 0.05f;
            transform.localPosition = startPos + new Vector3(0, yOffset, 0);
        }
    }
}

