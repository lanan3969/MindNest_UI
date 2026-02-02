/*
 * InteractionModeManager.cs
 * =========================
 * 
 * 交互模式管理器
 * 
 * 功能：
 * - 管理手势识别/鼠标点击两种交互模式
 * - 追踪手势识别成功率
 * - 自动回退到点击模式
 * - 用户手动切换模式
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using UnityEngine;

namespace MindNest.MR
{
    /// <summary>
    /// 交互模式枚举
    /// </summary>
    public enum InteractionMode
    {
        GestureOnly,      // 仅手势识别
        ClickOnly,        // 仅鼠标点击
        Hybrid,           // 混合模式（两种都可用）
        AutoFallback      // 自动回退（默认手势，失败后自动切换到点击）
    }
    
    /// <summary>
    /// 交互模式管理器
    /// </summary>
    public class InteractionModeManager : MonoBehaviour
    {
        [Header("模式设置")]
        [Tooltip("当前交互模式")]
        public InteractionMode currentMode = InteractionMode.AutoFallback;
        
        [Header("自动回退设置")]
        [Tooltip("连续失败次数阈值")]
        public int failureThreshold = 3;
        
        [Tooltip("自动回退冷却时间（秒）")]
        public float fallbackCooldown = 10f;
        
        [Header("统计")]
        [Tooltip("手势成功次数")]
        public int gestureSuccessCount = 0;
        
        [Tooltip("手势失败次数")]
        public int gestureFailureCount = 0;
        
        [Tooltip("点击次数")]
        public int clickCount = 0;
        
        [Header("调试")]
        public bool enableDebugLog = true;
        
        // ============================================================================
        // 内部状态
        // ============================================================================
        
        private int consecutiveFailures = 0;
        private float lastFallbackTime = 0f;
        private bool hasFallenBack = false;
        
        // ============================================================================
        // 事件
        // ============================================================================
        
        public System.Action<InteractionMode> OnModeChanged;
        public System.Action OnFallbackSuggested;
        
        // ============================================================================
        // Unity生命周期
        // ============================================================================
        
        void Start()
        {
            // 从PlayerPrefs加载用户偏好
            LoadUserPreference();
            
            LogInfo($"Interaction Mode Manager initialized: {currentMode}");
        }
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 记录手势成功
        /// </summary>
        public void RecordGestureSuccess()
        {
            gestureSuccessCount++;
            consecutiveFailures = 0;
            
            LogInfo($"Gesture success! Total: {gestureSuccessCount}");
        }
        
        /// <summary>
        /// 记录手势失败
        /// </summary>
        public void RecordGestureFailure()
        {
            gestureFailureCount++;
            consecutiveFailures++;
            
            LogInfo($"Gesture failure! Consecutive: {consecutiveFailures}/{failureThreshold}");
            
            // 检查是否需要回退
            if (currentMode == InteractionMode.AutoFallback && !hasFallenBack)
            {
                if (consecutiveFailures >= failureThreshold)
                {
                    float timeSinceLastFallback = Time.time - lastFallbackTime;
                    
                    if (timeSinceLastFallback > fallbackCooldown)
                    {
                        SuggestFallback();
                    }
                }
            }
        }
        
        /// <summary>
        /// 记录点击交互
        /// </summary>
        public void RecordClick()
        {
            clickCount++;
            LogInfo($"Click recorded! Total: {clickCount}");
        }
        
        /// <summary>
        /// 切换到指定模式
        /// </summary>
        public void SwitchMode(InteractionMode newMode)
        {
            if (currentMode == newMode) return;
            
            InteractionMode oldMode = currentMode;
            currentMode = newMode;
            
            // 保存用户偏好
            SaveUserPreference();
            
            // 触发事件
            OnModeChanged?.Invoke(newMode);
            
            LogInfo($"Mode switched: {oldMode} -> {newMode}");
        }
        
        /// <summary>
        /// 检查当前是否启用手势识别
        /// </summary>
        public bool IsGestureEnabled()
        {
            return currentMode == InteractionMode.GestureOnly || 
                   currentMode == InteractionMode.Hybrid || 
                   (currentMode == InteractionMode.AutoFallback && !hasFallenBack);
        }
        
        /// <summary>
        /// 检查当前是否启用点击
        /// </summary>
        public bool IsClickEnabled()
        {
            return currentMode == InteractionMode.ClickOnly || 
                   currentMode == InteractionMode.Hybrid || 
                   (currentMode == InteractionMode.AutoFallback && hasFallenBack);
        }
        
        /// <summary>
        /// 重置统计
        /// </summary>
        public void ResetStatistics()
        {
            gestureSuccessCount = 0;
            gestureFailureCount = 0;
            clickCount = 0;
            consecutiveFailures = 0;
            hasFallenBack = false;
            
            LogInfo("Statistics reset");
        }
        
        /// <summary>
        /// 获取手势成功率
        /// </summary>
        public float GetGestureSuccessRate()
        {
            int total = gestureSuccessCount + gestureFailureCount;
            if (total == 0) return 0f;
            
            return (float)gestureSuccessCount / total;
        }
        
        // ============================================================================
        // 私有方法
        // ============================================================================
        
        private void SuggestFallback()
        {
            hasFallenBack = true;
            lastFallbackTime = Time.time;
            consecutiveFailures = 0;
            
            LogWarning("Auto-fallback triggered! Suggesting click mode.");
            
            // 触发回退建议事件
            OnFallbackSuggested?.Invoke();
        }
        
        /// <summary>
        /// 加载用户偏好
        /// </summary>
        private void LoadUserPreference()
        {
            if (PlayerPrefs.HasKey("InteractionMode"))
            {
                int savedMode = PlayerPrefs.GetInt("InteractionMode");
                currentMode = (InteractionMode)savedMode;
                LogInfo($"Loaded user preference: {currentMode}");
            }
        }
        
        /// <summary>
        /// 保存用户偏好
        /// </summary>
        private void SaveUserPreference()
        {
            PlayerPrefs.SetInt("InteractionMode", (int)currentMode);
            PlayerPrefs.Save();
            LogInfo("User preference saved");
        }
        
        // ============================================================================
        // 日志工具
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[InteractionModeManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[InteractionModeManager] {message}");
        }
    }
}

