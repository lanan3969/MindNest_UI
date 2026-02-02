/*
 * MainMenuController.cs
 * =====================
 * 
 * Main Menu System
 * 
 * Manages the main menu interface with sidebar buttons, dialogue bubble,
 * and healing recommendations based on current anxiety level.
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MindNest.MR
{
    /// <summary>
    /// Controls main menu interactions
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("References")]
        public MRUIManager uiManager;
        public MindNestMRController mrController;
        public MRSceneStateManager stateManager;
        
        [Header("Dialogue Settings")]
        [Tooltip("Array of random dialogue messages")]
        public string[] randomDialogues = new string[]
        {
            "The weather is really nice today.\nDo you want to go out for a walk?",
            "You are truly precious, and\nyou're doing a great job.",
            "Remember to take a moment\nfor yourself today!",
            "I'm here to help you feel better.",
            "Let's work on this together!",
            "How are you feeling right now?"
        };
        
        [Header("Animation Settings")]
        public float dialogueChangeInterval = 8f;
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private string currentAnxietyLevel = "moderate";
        private Coroutine dialogueCoroutine;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            Debug.Log("📋 MainMenuController: Initializing");
            SetupButtonListeners();
        }
        
        void OnEnable()
        {
            // Start dialogue rotation when menu is shown
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
            }
            dialogueCoroutine = StartCoroutine(RotateDialogue());
        }
        
        void OnDisable()
        {
            // Stop dialogue rotation when menu is hidden
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
                dialogueCoroutine = null;
            }
        }
        
        // ============================================================================
        // Setup
        // ============================================================================
        
        private void SetupButtonListeners()
        {
            if (uiManager == null) return;
            
            // Sidebar buttons
            if (uiManager.breathingButton != null)
            {
                uiManager.breathingButton.onClick.AddListener(OnBreathingClicked);
            }
            
            if (uiManager.altruisticButton != null)
            {
                uiManager.altruisticButton.onClick.AddListener(OnAltruisticClicked);
            }
            
            if (uiManager.treeButton != null)
            {
                uiManager.treeButton.onClick.AddListener(OnTreeClicked);
            }
            
            if (uiManager.gearButton != null)
            {
                uiManager.gearButton.onClick.AddListener(OnGearClicked);
            }
            
            // === 新增：Chat按钮监听器 ===
            if (uiManager.chatButton != null)
            {
                uiManager.chatButton.onClick.AddListener(OnChatClicked);
            }
            
            if (uiManager.historyButton != null)
            {
                uiManager.historyButton.onClick.AddListener(OnHistoryClicked);
            }
            
            // Start Healing button
            if (uiManager.startHealingButton != null)
            {
                uiManager.startHealingButton.onClick.AddListener(OnStartHealingClicked);
            }
        }
        
        // ============================================================================
        // Button Callbacks
        // ============================================================================
        
        private void OnBreathingClicked()
        {
            Debug.Log("🫁 Breathing button clicked");
            TransitionToState(MRSceneState.BreathingHealing);
        }
        
        private void OnAltruisticClicked()
        {
            Debug.Log("🎓 Altruistic Healing button clicked");
            TransitionToState(MRSceneState.AltruisticHealing);
        }
        
        private void OnTreeClicked()
        {
            Debug.Log("🌳 My Tree button clicked");
            TransitionToState(MRSceneState.TreeView);
        }
        
        private void OnGearClicked()
        {
            Debug.Log("⚙️ Settings button clicked - returning to customization");
            TransitionToState(MRSceneState.Customization);
        }
        
        /// <summary>
        /// 聊天按钮点击 - 进入聊天界面
        /// </summary>
        private void OnChatClicked()
        {
            Debug.Log("💬 Chat button clicked - entering chat interface");
            // 跳转到聊天界面（复用ConnectionConfirm状态）
            TransitionToState(MRSceneState.ConnectionConfirm);
        }
        
        private void OnHistoryClicked()
        {
            Debug.Log("🕐 History button clicked");
            TransitionToState(MRSceneState.HealingHistory);
        }
        
        private void OnStartHealingClicked()
        {
            Debug.Log("🌟 Start Healing button clicked");
            
            // 1. 从 MRController 获取最新的焦虑评估
            if (mrController == null)
            {
                Debug.LogError("❌ MRController not found!");
                ShowHealingSuggestion("系统错误，请稍后再试");
                return;
            }
            
            string anxietyLevel = mrController.GetCurrentAnxietyLevel();
            
            if (string.IsNullOrEmpty(anxietyLevel))
            {
                // 如果没有评估数据，提示用户先记录日记
                ShowHealingSuggestion("请先在移动端记录今日心情，我才能为你定制疗愈方案哦~");
                Debug.LogWarning("⚠️ No anxiety assessment data found. User needs to record diary first.");
                return;
            }
            
            // 2. 显示引导提示
            switch (anxietyLevel)
            {
                case "light":
                    ShowHealingSuggestion("检测到轻度焦虑，让我们一起做个深呼吸吧~ ☀️");
                    break;
                case "moderate":
                    ShowHealingSuggestion("需要更多关照，我们先呼吸，然后一起安慰Nomi~ ☀️💧");
                    break;
                case "severe":
                    ShowHealingSuggestion("这次我们要完整体验疗愈旅程，跟着我一步步来~ ☀️💧🌱");
                    break;
                default:
                    ShowHealingSuggestion("让我们开始疗愈之旅吧！");
                    break;
            }
            
            // 3. 启动引导式疗愈流程
            if (MRSceneStateManager.Instance != null)
            {
                MRSceneStateManager.Instance.StartGuidedHealing(anxietyLevel);
            }
            else
            {
                Debug.LogError("❌ MRSceneStateManager.Instance is null!");
                TransitionToState(MRSceneState.BreathingHealing);
            }
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Refresh the menu display
        /// </summary>
        public void RefreshMenu()
        {
            UpdateDialogue();
            UpdateAnxietyLevel();
        }
        
        /// <summary>
        /// Set a specific dialogue message
        /// </summary>
        public void SetDialogue(string message)
        {
            if (uiManager != null && uiManager.dialogueText != null)
            {
                uiManager.dialogueText.text = message;
            }
        }
        
        /// <summary>
        /// Show healing suggestion
        /// </summary>
        public void ShowHealingSuggestion(string suggestion)
        {
            SetDialogue(suggestion);
        }
        
        // ============================================================================
        // Internal Methods
        // ============================================================================
        
        private void UpdateDialogue()
        {
            if (randomDialogues.Length == 0) return;
            
            string randomDialogue = randomDialogues[Random.Range(0, randomDialogues.Length)];
            SetDialogue(randomDialogue);
        }
        
        private void UpdateAnxietyLevel()
        {
            // Try to get from MR controller
            if (mrController != null)
            {
                // MR controller would provide this
                // For now, use mock data
            }
            
            // Use mock anxiety level
            currentAnxietyLevel = PlayerPrefs.GetString("MockAnxietyLevel", "moderate");
        }
        
        private IEnumerator RotateDialogue()
        {
            while (true)
            {
                yield return new WaitForSeconds(dialogueChangeInterval);
                UpdateDialogue();
            }
        }
        
        private void TransitionToState(MRSceneState newState)
        {
            if (MRSceneStateManager.Instance != null)
            {
                MRSceneStateManager.Instance.TransitionToState(newState);
            }
        }
    }
}

