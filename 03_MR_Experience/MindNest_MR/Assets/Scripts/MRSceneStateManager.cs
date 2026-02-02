/*
 * MRSceneStateManager.cs
 * ======================
 * 
 * Central state machine for MindNest MR experience flow
 * 
 * States:
 * - WelcomeScreen: Nomi entrance animation (3 ghost clones flying in)
 * - Customization: Full DIY panel with colors, accessories, sliders
 * - ConnectionConfirm: Voice listening animation
 * - MainMenu: Left sidebar menu + Nomi + Tree
 * - BreathingHealing: Particle ripple animation
 * - AltruisticHealing: Touch Nomi interaction with mood change
 * - TreeView: Focused tree view with season controls
 * - HealingHistory: Scrollable history panel
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using System;
using UnityEngine;

namespace MindNest.MR
{
    /// <summary>
    /// Scene state enumeration
    /// </summary>
    public enum MRSceneState
    {
        WelcomeScreen,
        Customization,
        ConnectionConfirm,
        MainMenu,
        BreathingHealing,
        AltruisticHealing,
        TreeView,
        HealingHistory
    }

    /// <summary>
    /// Central state machine for MR experience flow
    /// </summary>
    public class MRSceneStateManager : MonoBehaviour
    {
        // ============================================================================
        // Singleton Pattern
        // ============================================================================
        
        public static MRSceneStateManager Instance { get; private set; }
        
        // ============================================================================
        // Events
        // ============================================================================
        
        public event Action<MRSceneState, MRSceneState> OnStateChanged;
        
        // ============================================================================
        // State
        // ============================================================================
        
        private MRSceneState currentState;
        private MRSceneState previousState;
        
        // 疗愈路径追踪
        private string currentHealingPath = ""; // "light" | "moderate" | "severe" | ""
        private int healingStepIndex = 0;       // 当前环节索引 (0=呼吸, 1=利他, 2=树)
        private bool isGuidedHealing = false;   // 是否是引导式疗愈（vs 独立访问）
        
        [Header("Debug")]
        [Tooltip("显示状态转换日志")]
        public bool verboseLogging = true;
        
        [Tooltip("显示性能监控（FPS）")]
        public bool showPerformanceMonitor = true;
        
        // ============================================================================
        // Component References (set by other systems)
        // ============================================================================
        
        [Header("Scene References")]
        public MRUIManager uiManager;
        public GameObject nomiBillboard;
        public GameObject lifeTree;
        public WelcomeAnimator welcomeAnimator;
        public NomiCustomizer nomiCustomizer;
        public ConnectionConfirmController connectionConfirmController;
        public MainMenuController mainMenuController;
        public BreathingHealingController breathingController;
        public AltruisticHealingController altruisticController;
        public TreeViewController treeViewController;
        public HealingHistoryController historyController;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Initialize to welcome state
            currentState = MRSceneState.WelcomeScreen;
            previousState = MRSceneState.WelcomeScreen;
        }
        
        void Start()
        {
            LogInfo("🎮 MRSceneStateManager initialized");
            
            // Force enter welcome screen (already set in Awake, so directly enter)
            EnterState(MRSceneState.WelcomeScreen);
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Transition to a new state
        /// </summary>
        public void TransitionToState(MRSceneState newState)
        {
            if (newState == currentState)
            {
                LogWarning($"Already in state {newState}");
                return;
            }
            
            LogInfo($"🔄 State transition: {currentState} → {newState}");
            
            // Exit current state
            ExitState(currentState);
            
            // Update state
            previousState = currentState;
            currentState = newState;
            
            // Enter new state
            EnterState(newState);
            
            // Notify listeners
            OnStateChanged?.Invoke(previousState, currentState);
        }
        
        /// <summary>
        /// Get current state
        /// </summary>
        public MRSceneState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Get previous state
        /// </summary>
        public MRSceneState GetPreviousState()
        {
            return previousState;
        }
        
        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            TransitionToState(MRSceneState.MainMenu);
        }
        
        /// <summary>
        /// 开始引导式疗愈流程
        /// </summary>
        public void StartGuidedHealing(string healingPath)
        {
            currentHealingPath = healingPath;
            healingStepIndex = 0;
            isGuidedHealing = true;
            
            LogInfo($"🌟 Starting guided healing: {healingPath}");
            
            // 固定进入呼吸疗愈
            TransitionToState(MRSceneState.BreathingHealing);
        }
        
        /// <summary>
        /// 继续疗愈流程的下一步
        /// </summary>
        public void ContinueHealingFlow()
        {
            if (!isGuidedHealing)
            {
                LogWarning("Not in guided healing mode, returning to main menu");
                ReturnToMainMenu();
                return;
            }
            
            healingStepIndex++;
            
            LogInfo($"🔄 Continuing healing flow: step {healingStepIndex}, path: {currentHealingPath}");
            
            switch (currentHealingPath)
            {
                case "light":
                    // 轻度：呼吸后直接返回
                    FinishHealingFlow();
                    break;
                    
                case "moderate":
                    // 中度：呼吸→利他→返回
                    if (healingStepIndex == 1)
                        TransitionToState(MRSceneState.AltruisticHealing);
                    else
                        FinishHealingFlow();
                    break;
                    
                case "severe":
                    // 重度：呼吸→利他→树→返回
                    if (healingStepIndex == 1)
                        TransitionToState(MRSceneState.AltruisticHealing);
                    else if (healingStepIndex == 2)
                        TransitionToState(MRSceneState.TreeView);
                    else
                        FinishHealingFlow();
                    break;
                    
                default:
                    LogWarning($"Unknown healing path: {currentHealingPath}");
                    FinishHealingFlow();
                    break;
            }
        }
        
        /// <summary>
        /// 完成疗愈流程并返回主界面
        /// </summary>
        public void FinishHealingFlow()
        {
            LogInfo($"✅ Healing flow completed: {currentHealingPath}");
            
            isGuidedHealing = false;
            currentHealingPath = "";
            healingStepIndex = 0;
            ReturnToMainMenu();
        }
        
        /// <summary>
        /// 检查当前环节是否应该显示 Next 按钮
        /// </summary>
        public bool ShouldShowNextButton()
        {
            if (!isGuidedHealing)
            {
                return false;
            }
            
            switch (currentHealingPath)
            {
                case "light":
                    return false; // 轻度只有呼吸，显示 Finish
                    
                case "moderate":
                    return healingStepIndex == 0; // 呼吸后显示 Next，利他后显示 Finish
                    
                case "severe":
                    return healingStepIndex < 2; // 前两步显示 Next，树后显示 Finish
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 获取当前疗愈路径（用于调试）
        /// </summary>
        public string GetCurrentHealingPath()
        {
            return currentHealingPath;
        }
        
        /// <summary>
        /// 是否在引导式疗愈中
        /// </summary>
        public bool IsInGuidedHealing()
        {
            return isGuidedHealing;
        }
        
        // ============================================================================
        // State Management
        // ============================================================================
        
        private void EnterState(MRSceneState state)
        {
            switch (state)
            {
                case MRSceneState.WelcomeScreen:
                    EnterWelcomeScreen();
                    break;
                    
                case MRSceneState.Customization:
                    EnterCustomization();
                    break;
                    
                case MRSceneState.ConnectionConfirm:
                    EnterConnectionConfirm();
                    break;
                    
                case MRSceneState.MainMenu:
                    EnterMainMenu();
                    break;
                    
                case MRSceneState.BreathingHealing:
                    EnterBreathingHealing();
                    break;
                    
                case MRSceneState.AltruisticHealing:
                    EnterAltruisticHealing();
                    break;
                    
                case MRSceneState.TreeView:
                    EnterTreeView();
                    break;
                    
                case MRSceneState.HealingHistory:
                    EnterHealingHistory();
                    break;
            }
        }
        
        private void ExitState(MRSceneState state)
        {
            switch (state)
            {
                case MRSceneState.WelcomeScreen:
                    ExitWelcomeScreen();
                    break;
                    
                case MRSceneState.Customization:
                    ExitCustomization();
                    break;
                    
                case MRSceneState.ConnectionConfirm:
                    ExitConnectionConfirm();
                    break;
                    
                case MRSceneState.MainMenu:
                    ExitMainMenu();
                    break;
                    
                case MRSceneState.BreathingHealing:
                    ExitBreathingHealing();
                    break;
                    
                case MRSceneState.AltruisticHealing:
                    ExitAltruisticHealing();
                    break;
                    
                case MRSceneState.TreeView:
                    ExitTreeView();
                    break;
                    
                case MRSceneState.HealingHistory:
                    ExitHealingHistory();
                    break;
            }
        }
        
        // ============================================================================
        // State Enter/Exit Methods
        // ============================================================================
        
        private void EnterWelcomeScreen()
        {
            ShowNomi();
            HideTree();
            if (uiManager != null) uiManager.HideAllPanels();
            if (welcomeAnimator != null) welcomeAnimator.StartWelcomeAnimation();
        }
        
        private void ExitWelcomeScreen()
        {
            if (welcomeAnimator != null) welcomeAnimator.StopWelcomeAnimation();
        }
        
        private void EnterCustomization()
        {
            ShowNomi();
            HideTree();
            if (uiManager != null)
            {
                uiManager.HideAllPanels();
                uiManager.ShowCustomizationPanel();
            }
            if (nomiCustomizer != null) nomiCustomizer.ShowNomi();
        }
        
        private void ExitCustomization()
        {
            if (uiManager != null) uiManager.HideCustomizationPanel();
        }
        
        private void EnterConnectionConfirm()
        {
            ShowNomi();
            HideTree();
            if (uiManager != null)
            {
                uiManager.HideAllPanels();
                uiManager.ShowConnectionConfirmPanel();
            }
            
            // Start conversation with Nomi
            if (connectionConfirmController != null)
            {
                connectionConfirmController.StartConversation();
            }
        }
        
        private void ExitConnectionConfirm()
        {
            if (uiManager != null) uiManager.HideConnectionConfirmPanel();
        }
        
        private void EnterMainMenu()
        {
            ShowNomi();
            HideTree(); // Tree hidden until user clicks "View Tree" button
            if (uiManager != null)
            {
                uiManager.HideAllPanels();
                uiManager.ShowMainMenuPanel();
            }
            if (mainMenuController != null) mainMenuController.RefreshMenu();
        }
        
        private void ExitMainMenu()
        {
            if (uiManager != null) uiManager.HideMainMenuPanel();
        }
        
        private void EnterBreathingHealing()
        {
            HideNomi(); // Hide both during breathing exercise
            HideTree();
            if (uiManager != null)
            {
                uiManager.HideAllPanels();
                uiManager.ShowBreathingPanel();
            }
            if (breathingController != null) breathingController.StartExercise();
        }
        
        private void ExitBreathingHealing()
        {
            if (breathingController != null) breathingController.StopExercise();
            if (uiManager != null) uiManager.HideBreathingPanel();
        }
        
        private void EnterAltruisticHealing()
        {
            ShowNomi(); // Show Nomi for comforting interaction
            HideTree();
            if (uiManager != null)
            {
                uiManager.HideAllPanels();
                uiManager.ShowAltruisticPanel();
            }
            if (altruisticController != null) altruisticController.StartAltruisticHealing();
        }
        
        private void ExitAltruisticHealing()
        {
            if (altruisticController != null) altruisticController.StopAltruisticHealing();
            if (uiManager != null) uiManager.HideAltruisticPanel();
        }
        
        private void EnterTreeView()
        {
            HideNomi(); // Show only tree for focused interaction
            ShowTree();
            if (uiManager != null)
            {
                uiManager.HideMainMenuSidebar();
                uiManager.ShowTreeControlPanel();
            }
            if (treeViewController != null) treeViewController.FocusOnTree();
        }
        
        private void ExitTreeView()
        {
            if (treeViewController != null) treeViewController.UnfocusTree();
            if (uiManager != null) uiManager.HideTreeControlPanel();
        }
        
        private void EnterHealingHistory()
        {
            ShowNomi(); // Show Nomi during history review
            HideTree();
            if (uiManager != null)
            {
                uiManager.HideAllPanels();
                uiManager.ShowHistoryPanel();
            }
            if (historyController != null) historyController.LoadHistory();
        }
        
        private void ExitHealingHistory()
        {
            if (uiManager != null) uiManager.HideHistoryPanel();
        }
        
        // ============================================================================
        // Logging
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (verboseLogging) Debug.Log($"[MRStateManager] {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[MRStateManager] {message}");
        }
        
        // ============================================================================
        // Visibility Management
        // ============================================================================
        
        private void ShowNomi()
        {
            if (nomiBillboard != null)
            {
                nomiBillboard.SetActive(true);
                LogInfo("👻 Nomi shown");
            }
        }
        
        private void HideNomi()
        {
            if (nomiBillboard != null)
            {
                nomiBillboard.SetActive(false);
                LogInfo("👻 Nomi hidden");
            }
        }
        
        private void ShowTree()
        {
            if (lifeTree != null)
            {
                lifeTree.SetActive(true);
                LogInfo("🌳 Tree shown");
            }
        }
        
        private void HideTree()
        {
            if (lifeTree != null)
            {
                lifeTree.SetActive(false);
                LogInfo("🌳 Tree hidden");
            }
        }
        
        // ============================================================================
        // 性能监控
        // ============================================================================
        
        /// <summary>
        /// 显示性能监控信息（FPS）
        /// </summary>
        private void OnGUI()
        {
            if (!showPerformanceMonitor) return;
            
            // 计算FPS
            int fps = Mathf.RoundToInt(1.0f / Time.deltaTime);
            
            // 显示FPS
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = fps >= 60 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
            
            GUI.Label(new Rect(10, 10, 150, 30), $"FPS: {fps}", style);
            
            // 性能告警
            if (fps < 60 && verboseLogging)
            {
                Debug.LogWarning($"⚠️ 性能告警：FPS降至 {fps}");
            }
        }
    }
}

