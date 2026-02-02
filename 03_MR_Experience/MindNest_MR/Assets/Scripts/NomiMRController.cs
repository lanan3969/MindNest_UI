/*
 * NomiMRController.cs
 * ====================
 * 
 * Unity MR 通信脚本 - MindNest 后端数据同步
 * 
 * 功能：
 * 1. 每5秒轮询后端 API 获取最新评估数据
 * 2. 解析焦虑分值、Nomi表情、疗愈建议、累计养料
 * 3. 提供视觉更新接口（表情、植物生长、预警效果）
 * 4. 检测离线Mock模式（分值=0.001）
 * 
 * 作者: MindNest Team
 * 日期: 2026-01-27
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace MindNest.MR
{
    /// <summary>
    /// MR端后端数据同步控制器
    /// </summary>
    public class MindNestMRController : MonoBehaviour
    {
        // ============================================================================
        // 配置参数
        // ============================================================================
        
        [Header("API Configuration")]
        [Tooltip("后端API基础URL")]
        public string apiBaseUrl = "http://localhost:8000";
        
        [Tooltip("用户ID")]
        public string userId = "user_demo_001";
        
        [Tooltip("轮询间隔（秒）")]
        public float pollInterval = 5f;
        
        [Header("Visual References (Auto-configured by Builder)")]
        [Tooltip("Nomi 表情材质")]
        public Material nomiMaterial;
        
        [Tooltip("生命树 Transform")]
        public Transform lifeTreeTransform;
        
        [Tooltip("表情资源路径")]
        public string expressionResourcePath = "Expressions";
        
        [Tooltip("植物生长速率（每 100 养料增长倍数）")]
        public float growthRatePerHundred = 0.1f;
        
        [Header("Debug Settings")]
        [Tooltip("是否在控制台输出详细日志")]
        public bool verboseLogging = true;
        
        // ============================================================================
        // 内部状态
        // ============================================================================
        
        private Coroutine pollCoroutine;
        private bool isPolling = false;
        
        // 最新数据缓存
        private float currentScore = 0f;
        private string currentExpression = "";
        private string currentHealingSuggestion = "";
        private int currentTotalNutrients = 0;
        private string currentAnxietyLevel = "";
        
        // ============================================================================
        // Unity 生命周期
        // ============================================================================
        
        void Start()
        {
            LogInfo("🌳 NomiMRController initialized");
            StartPolling();
        }
        
        void OnDestroy()
        {
            StopPolling();
        }
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 开始轮询后端数据
        /// </summary>
        public void StartPolling()
        {
            if (isPolling)
            {
                LogWarning("Polling is already running");
                return;
            }
            
            isPolling = true;
            pollCoroutine = StartCoroutine(PollBackendRoutine());
            LogInfo($"▶️ Started polling {apiBaseUrl}/api/v1/mr_sync/{userId}");
        }
        
        /// <summary>
        /// 停止轮询
        /// </summary>
        public void StopPolling()
        {
            if (pollCoroutine != null)
            {
                StopCoroutine(pollCoroutine);
                pollCoroutine = null;
            }
            
            isPolling = false;
            LogInfo("⏸️ Stopped polling");
        }
        
        /// <summary>
        /// 手动触发一次数据同步
        /// </summary>
        public void SyncNow()
        {
            StartCoroutine(FetchDataFromBackend());
        }
        
        /// <summary>
        /// 手动设置 Nomi 表情（用于历史回顾等场景）
        /// </summary>
        /// <param name="expressionName">表情名称（不含扩展名）</param>
        public void SetExpression(string expressionName)
        {
            currentExpression = expressionName;
            UpdateNomiMood(expressionName);
            LogInfo($"🎭 Manually set expression to: {expressionName}");
        }
        
        /// <summary>
        /// 添加养料（疗愈活动完成后调用）
        /// </summary>
        /// <param name="amount">养料数量</param>
        public void AddNutrients(int amount)
        {
            currentTotalNutrients += amount;
            UpdatePlantGrowth(currentTotalNutrients);
            LogInfo($"🌱 Added {amount} nutrients, total: {currentTotalNutrients}");
        }
        
        /// <summary>
        /// 获取当前焦虑等级
        /// </summary>
        public string GetCurrentAnxietyLevel()
        {
            return currentAnxietyLevel;
        }
        
        /// <summary>
        /// 获取当前总养料
        /// </summary>
        public int GetCurrentNutrients()
        {
            return currentTotalNutrients;
        }
        
        // ============================================================================
        // 核心轮询逻辑
        // ============================================================================
        
        /// <summary>
        /// 轮询协程
        /// </summary>
        private IEnumerator PollBackendRoutine()
        {
            while (isPolling)
            {
                yield return FetchDataFromBackend();
                yield return new WaitForSeconds(pollInterval);
            }
        }
        
        /// <summary>
        /// 从后端获取数据
        /// </summary>
        private IEnumerator FetchDataFromBackend()
        {
            string url = $"{apiBaseUrl}/api/v1/mr_sync/{userId}";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // 发送请求
                yield return request.SendWebRequest();
                
                // 检查网络错误
                if (request.result == UnityWebRequest.Result.ConnectionError || 
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    LogError($"❌ API Request Failed: {request.error}");
                    LogError($"   Response Code: {request.responseCode}");
                    yield break;
                }
                
                // 解析JSON响应
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    MRSyncResponse response = JsonUtility.FromJson<MRSyncResponse>(jsonResponse);
                    
                    // 更新缓存数据
                    currentScore = response.score;
                    currentExpression = response.expression;
                    currentHealingSuggestion = response.healing_suggestion;
                    currentTotalNutrients = response.total_nutrients;
                    currentAnxietyLevel = response.anxiety_level;
                    
                    // 🔧 特殊检测：离线 Mock 模式
                    if (Mathf.Approximately(currentScore, 0.001f))
                    {
                        Debug.LogWarning("⚠️ 正在使用离线 Mock 评估模式");
                    }
                    
                    // 日志输出
                    LogInfo($"✅ Data Synced | Score: {currentScore:F2} | Expression: {currentExpression} | Nutrients: {currentTotalNutrients}");
                    
                    // 触发视觉更新
                    ApplyVisualUpdates(response);
                }
                catch (Exception e)
                {
                    LogError($"❌ JSON Parsing Error: {e.Message}");
                }
            }
        }
        
        // ============================================================================
        // 视觉更新接口（供继承或扩展）
        // ============================================================================
        
        /// <summary>
        /// 应用所有视觉更新
        /// </summary>
        /// <param name="response">后端响应数据</param>
        private void ApplyVisualUpdates(MRSyncResponse response)
        {
            // 1. 更新 Nomi 情绪表情
            UpdateNomiMood(response.expression);
            
            // 2. 更新植物生长状态
            UpdatePlantGrowth(response.total_nutrients);
            
            // 3. 处理焦虑预警（如果分值过高）
            if (response.score >= 7.0f)
            {
                HandleAnxietyAlert(response.score);
            }
        }
        
        /// <summary>
        /// 【视觉接口 1】更新 Nomi 的情绪表情
        /// </summary>
        /// <param name="expression">表情文件名（如 "happy.png"）</param>
        protected virtual void UpdateNomiMood(string expression)
        {
            // 移除文件扩展名（Resources.Load 不需要扩展名）
            string expressionName = expression.Replace(".png", "").Replace(".jpg", "");
            
            // 从 Resources 加载表情贴图
            Texture2D expressionTexture = Resources.Load<Texture2D>($"{expressionResourcePath}/{expressionName}");
            
            if (expressionTexture != null && nomiMaterial != null)
            {
                // 更新材质贴图
                nomiMaterial.mainTexture = expressionTexture;
                LogInfo($"🎭 表情已切换: {expression}");
            }
            else
            {
                if (expressionTexture == null)
                {
                    LogWarning($"⚠️ 未找到表情: Resources/{expressionResourcePath}/{expressionName}");
                }
                if (nomiMaterial == null)
                {
                    LogWarning($"⚠️ Nomi Material 未配置");
                }
            }
        }
        
        /// <summary>
        /// 【视觉接口 2】根据总养料更新植物生长
        /// </summary>
        /// <param name="totalNutrients">累计养料总额</param>
        protected virtual void UpdatePlantGrowth(int totalNutrients)
        {
            if (lifeTreeTransform == null)
            {
                LogWarning("⚠️ Life Tree Transform 未配置");
                return;
            }
            
            // 尝试获取 ParticleTreeSystem 组件
            ParticleTreeSystem treeSystem = lifeTreeTransform.GetComponent<ParticleTreeSystem>();
            
            if (treeSystem != null)
            {
                // 使用新的粒子树系统
                treeSystem.SetNutrientLevel(totalNutrients);
                LogInfo($"🌱 粒子树已生长: {totalNutrients} 养料 → Stage {treeSystem.GetGrowthStage()}");
            }
            else
            {
                // Fallback: 旧的缩放逻辑（兼容性）
                float growthMultiplier = 1.0f + (totalNutrients / 100f) * growthRatePerHundred;
                growthMultiplier = Mathf.Clamp(growthMultiplier, 0.5f, 5.0f);
                Vector3 targetScale = new Vector3(0.5f, 1f, 0.5f) * growthMultiplier;
                lifeTreeTransform.localScale = targetScale;
                
                LogWarning($"⚠️ ParticleTreeSystem not found, using fallback scaling");
                LogInfo($"🌱 植物已生长: {totalNutrients} 养料 → {growthMultiplier:F2}x 倍数");
            }
        }
        
        /// <summary>
        /// 【视觉接口 3】处理焦虑分数过高的预警效果
        /// </summary>
        /// <param name="score">焦虑分值 [0-10]</param>
        protected virtual void HandleAnxietyAlert(float score)
        {
            // 🎨 占位方法：触发高焦虑预警视觉效果
            // 示例：
            // - 屏幕边缘红色脉冲光晕
            // - Nomi 角色发出关切动画
            // - UI 显示疗愈引导提示
            
            LogWarning($"⚠️ [Placeholder] HandleAnxietyAlert: High anxiety detected (Score: {score:F2})");
            
            // TODO: 实现焦虑预警效果
            // 例如：
            // vfxAlert.Play();
            // uiAlertPanel.SetActive(true);
            // AudioSource.PlayOneShot(alertSound);
        }
        
        // ============================================================================
        // 日志工具
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[NomiMR] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[NomiMR] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[NomiMR] {message}");
        }
        
        // ============================================================================
        // 数据模型（JSON反序列化）
        // ============================================================================
        
        /// <summary>
        /// MR同步响应数据结构
        /// </summary>
        [Serializable]
        private class MRSyncResponse
        {
            public float score;                 // 焦虑分值
            public string expression;           // Nomi表情文件名
            public string healing_suggestion;   // 疗愈建议
            public int total_nutrients;         // 累计养料总额
            public string anxiety_level;        // 焦虑等级：light/moderate/severe
            public string timestamp;            // 时间戳
        }
    }
}
