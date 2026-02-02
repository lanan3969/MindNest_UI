/*
 * GestureRecognizer.cs
 * ====================
 * 
 * 手势识别器
 * 
 * 功能：
 * - 接收手部检测数据
 * - 追踪手部运动轨迹
 * - 识别6种手势（抚摸、戳戳、投喂、抱抱、挥手、比心）
 * - 输出手势事件和置信度
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MindNest.MR
{
    /// <summary>
    /// 手势识别器（支持简化检测和关键点检测双模式）
    /// </summary>
    public class GestureRecognizer : MonoBehaviour
    {
        [Header("识别模式")]
        [Tooltip("启用关键点识别器（更精确）")]
        public bool useLandmarkRecognizer = false;
        
        [Header("关键点识别器")]
        public LandmarkGestureRecognizer landmarkRecognizer;
        
        [Header("配置")]
        public GestureConfig config = new GestureConfig();
        
        [Header("Nomi位置")]
        public Transform nomiTransform;  // Nomi的Transform（用于计算相对位置）
        public Camera mainCamera;
        
        [Header("调试")]
        public bool enableDebugLog = true;
        
        // ============================================================================
        // 手势状态
        // ============================================================================
        
        private enum GestureState
        {
            Idle,
            Tracking,
            Recognized
        }
        
        #pragma warning disable 0414
        private GestureState currentState = GestureState.Idle;
        private GestureType trackingGesture = GestureType.None;
        #pragma warning restore 0414
        
        // ============================================================================
        // 轨迹数据
        // ============================================================================
        
        private List<Vector2> hand0Trajectory = new List<Vector2>();  // 左手轨迹
        private List<Vector2> hand1Trajectory = new List<Vector2>();  // 右手轨迹
        private List<float> trajectoryTimestamps = new List<float>(); // 时间戳
        
        #pragma warning disable 0414
        private float trajectoryStartTime = 0f;
        #pragma warning restore 0414
        private int trajectoryMaxLength = 100;  // 最多保留100个轨迹点
        
        // ============================================================================
        // 手势特定状态
        // ============================================================================
        
        // 戳戳状态
        private bool pokeApproaching = false;
        private Vector2 pokeStartPos;
        
        // 挥手状态
        private int waveDirectionChanges = 0;
        private float lastWaveDirectionX = 0f;
        
        // 抚摸状态
        private float strokeStartTime = 0f;
        private bool strokeInProgress = false;
        
        // 投喂状态
        private Vector2 feedStartPos;
        private bool feedStarted = false;
        
        // ============================================================================
        // 事件
        // ============================================================================
        
        public System.Action<GestureEvent> OnGestureRecognized;
        
        // ============================================================================
        // Unity生命周期
        // ============================================================================
        
        void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // 连接关键点识别器事件
            if (landmarkRecognizer != null)
            {
                landmarkRecognizer.OnGestureRecognized += HandleLandmarkGesture;
                landmarkRecognizer.nomiTransform = nomiTransform;
                landmarkRecognizer.mainCamera = mainCamera;
                landmarkRecognizer.config = config;
            }
            
            LogInfo($"GestureRecognizer initialized (Landmark: {useLandmarkRecognizer})");
        }
        
        void OnDestroy()
        {
            if (landmarkRecognizer != null)
            {
                landmarkRecognizer.OnGestureRecognized -= HandleLandmarkGesture;
            }
        }
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 处理手部数据（由HandDetectionManager调用）
        /// </summary>
        public void ProcessHandData(HandData[] hands)
        {
            if (hands == null || hands.Length == 0) return;
            
            // 如果使用关键点识别器，优先使用它
            // 注意：关键点识别器会直接从HandDetectionManager的LandmarkRecognizer接收数据
            // 这里只处理简化模式的识别
            
            if (!useLandmarkRecognizer)
            {
                // 更新轨迹
                UpdateTrajectory(hands);
                
                // 识别手势（简化模式）
                RecognizeGestures(hands);
            }
        }
        
        /// <summary>
        /// 处理来自关键点识别器的手势
        /// </summary>
        private void HandleLandmarkGesture(GestureEvent gestureEvent)
        {
            LogInfo($"Landmark gesture received: {gestureEvent.description}");
            
            // 直接转发事件
            OnGestureRecognized?.Invoke(gestureEvent);
        }
        
        /// <summary>
        /// 重置识别器状态
        /// </summary>
        public void ResetRecognizer()
        {
            ClearTrajectory();
            currentState = GestureState.Idle;
            trackingGesture = GestureType.None;
            ResetGestureStates();
            
            LogInfo("Recognizer reset");
        }
        
        // ============================================================================
        // 轨迹管理
        // ============================================================================
        
        private void UpdateTrajectory(HandData[] hands)
        {
            // 记录手部轨迹
            if (hands[0].isDetected)
            {
                hand0Trajectory.Add(hands[0].position);
            }
            
            if (hands[1].isDetected)
            {
                hand1Trajectory.Add(hands[1].position);
            }
            
            trajectoryTimestamps.Add(Time.time);
            
            // 限制轨迹长度
            while (hand0Trajectory.Count > trajectoryMaxLength)
            {
                hand0Trajectory.RemoveAt(0);
            }
            while (hand1Trajectory.Count > trajectoryMaxLength)
            {
                hand1Trajectory.RemoveAt(0);
            }
            while (trajectoryTimestamps.Count > trajectoryMaxLength)
            {
                trajectoryTimestamps.RemoveAt(0);
            }
        }
        
        private void ClearTrajectory()
        {
            hand0Trajectory.Clear();
            hand1Trajectory.Clear();
            trajectoryTimestamps.Clear();
        }
        
        private void ResetGestureStates()
        {
            pokeApproaching = false;
            waveDirectionChanges = 0;
            lastWaveDirectionX = 0f;
            strokeInProgress = false;
            feedStarted = false;
        }
        
        // ============================================================================
        // 手势识别
        // ============================================================================
        
        private void RecognizeGestures(HandData[] hands)
        {
            // 获取Nomi在屏幕上的位置
            Vector2 nomiScreenPos = GetNomiScreenPosition();
            
            // 优先级顺序检测手势
            // 1. 双手手势（抱抱、比心）
            if (hands[0].isDetected && hands[1].isDetected)
            {
                if (RecognizeHug(hands, nomiScreenPos))
                    return;
                    
                if (RecognizeHeart(hands, nomiScreenPos))
                    return;
            }
            
            // 2. 单手手势（使用任一检测到的手）
            HandData activeHand = hands[0].isDetected ? hands[0] : hands[1];
            
            if (activeHand.isDetected)
            {
                if (RecognizePoke(activeHand, nomiScreenPos))
                    return;
                    
                if (RecognizeFeed(activeHand, nomiScreenPos))
                    return;
                    
                if (RecognizeWave(activeHand, nomiScreenPos))
                    return;
                    
                if (RecognizeStroke(activeHand, nomiScreenPos))
                    return;
            }
        }
        
        // ============================================================================
        // 抚摸识别
        // ============================================================================
        
        private bool RecognizeStroke(HandData hand, Vector2 nomiPos)
        {
            // 检查手是否在Nomi附近
            float distance = Vector2.Distance(hand.position, nomiPos);
            if (distance > config.nomiInteractionRadius)
            {
                strokeInProgress = false;
                return false;
            }
            
            // 检查速度范围
            float speed = hand.velocity.magnitude;
            if (speed < config.strokeSpeedMin || speed > config.strokeSpeedMax)
            {
                strokeInProgress = false;
                return false;
            }
            
            // 开始追踪抚摸手势
            if (!strokeInProgress)
            {
                strokeInProgress = true;
                strokeStartTime = Time.time;
                return false;
            }
            
            // 检查持续时间
            float duration = Time.time - strokeStartTime;
            if (duration >= config.strokeMinDuration)
            {
                // 识别成功
                TriggerGesture(GestureType.Stroke, hand.position, 0.85f);
                strokeInProgress = false;
                return true;
            }
            
            return false;
        }
        
        // ============================================================================
        // 戳戳识别
        // ============================================================================
        
        private bool RecognizePoke(HandData hand, Vector2 nomiPos)
        {
            float distance = Vector2.Distance(hand.position, nomiPos);
            float speed = hand.velocity.magnitude;
            
            // 阶段1：检测快速接近
            if (!pokeApproaching && speed > config.pokeSpeedThreshold && distance < config.nomiInteractionRadius * 2f)
            {
                Vector2 directionToNomi = (nomiPos - hand.position).normalized;
                float dotProduct = Vector2.Dot(hand.velocity.normalized, directionToNomi);
                
                if (dotProduct > 0.7f)  // 方向朝向Nomi
                {
                    pokeApproaching = true;
                    pokeStartPos = hand.position;
                    return false;
                }
            }
            
            // 阶段2：检测回退
            if (pokeApproaching)
            {
                Vector2 directionFromStart = hand.position - pokeStartPos;
                
                // 检查是否回退
                if (directionFromStart.magnitude > config.pokeRetractDist)
                {
                    Vector2 directionAwayFromNomi = (hand.position - nomiPos).normalized;
                    float dotProduct = Vector2.Dot(hand.velocity.normalized, directionAwayFromNomi);
                    
                    if (dotProduct > 0.5f)  // 方向远离Nomi
                    {
                        // 识别成功
                        TriggerGesture(GestureType.Poke, pokeStartPos, 0.9f);
                        pokeApproaching = false;
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        // ============================================================================
        // 投喂识别
        // ============================================================================
        
        private bool RecognizeFeed(HandData hand, Vector2 nomiPos)
        {
            float distance = Vector2.Distance(hand.position, nomiPos);
            
            // 阶段1：检测手在Nomi下方
            if (!feedStarted && hand.position.y < nomiPos.y - config.feedMinVerticalDist / 2f)
            {
                feedStarted = true;
                feedStartPos = hand.position;
                return false;
            }
            
            // 阶段2：检测向上移动到Nomi附近
            if (feedStarted)
            {
                float verticalMovement = hand.position.y - feedStartPos.y;
                
                if (verticalMovement > config.feedMinVerticalDist && distance < config.nomiInteractionRadius)
                {
                    // 识别成功
                    TriggerGesture(GestureType.Feed, hand.position, 0.8f);
                    feedStarted = false;
                    return true;
                }
                
                // 超时重置
                if (Vector2.Distance(hand.position, feedStartPos) > config.nomiInteractionRadius * 3f)
                {
                    feedStarted = false;
                }
            }
            
            return false;
        }
        
        // ============================================================================
        // 抱抱识别
        // ============================================================================
        
        private bool RecognizeHug(HandData[] hands, Vector2 nomiPos)
        {
            Vector2 hand0Pos = hands[0].position;
            Vector2 hand1Pos = hands[1].position;
            
            // 检查两手是否在Nomi两侧
            bool hand0Left = hand0Pos.x < nomiPos.x;
            bool hand1Right = hand1Pos.x > nomiPos.x;
            bool hand1Left = hand1Pos.x < nomiPos.x;
            bool hand0Right = hand0Pos.x > nomiPos.x;
            
            bool handsOnOpposites = (hand0Left && hand1Right) || (hand1Left && hand0Right);
            
            if (!handsOnOpposites) return false;
            
            // 检查两手之间的距离
            float handDistance = Vector2.Distance(hand0Pos, hand1Pos);
            
            // 检查两手是否都靠近Nomi
            float dist0 = Vector2.Distance(hand0Pos, nomiPos);
            float dist1 = Vector2.Distance(hand1Pos, nomiPos);
            
            if (dist0 < config.hugApproachDist && dist1 < config.hugApproachDist && 
                handDistance < config.hugHandDistance)
            {
                // 识别成功
                Vector2 center = (hand0Pos + hand1Pos) / 2f;
                TriggerGesture(GestureType.Hug, center, 0.9f);
                return true;
            }
            
            return false;
        }
        
        // ============================================================================
        // 挥手识别
        // ============================================================================
        
        private bool RecognizeWave(HandData hand, Vector2 nomiPos)
        {
            float distance = Vector2.Distance(hand.position, nomiPos);
            if (distance > config.nomiInteractionRadius * 1.5f) return false;
            
            float speed = hand.velocity.magnitude;
            if (speed < config.waveSpeedThreshold) return false;
            
            // 检测水平方向变化
            float currentDirectionX = Mathf.Sign(hand.velocity.x);
            
            if (lastWaveDirectionX != 0 && currentDirectionX != 0 && currentDirectionX != lastWaveDirectionX)
            {
                waveDirectionChanges++;
            }
            
            lastWaveDirectionX = currentDirectionX;
            
            // 检查是否完成足够的往复
            if (waveDirectionChanges >= config.waveMinCycles)
            {
                // 识别成功
                TriggerGesture(GestureType.Wave, hand.position, 0.8f);
                waveDirectionChanges = 0;
                return true;
            }
            
            return false;
        }
        
        // ============================================================================
        // 比心识别
        // ============================================================================
        
        private bool RecognizeHeart(HandData[] hands, Vector2 nomiPos)
        {
            Vector2 hand0Pos = hands[0].position;
            Vector2 hand1Pos = hands[1].position;
            
            // 检查两手距离
            float handDistance = Vector2.Distance(hand0Pos, hand1Pos);
            
            // 两手应该靠近但不太近
            if (handDistance < config.heartMinDistance || handDistance > config.heartHandDistance)
                return false;
            
            // 检查两手是否在Nomi上方附近
            Vector2 center = (hand0Pos + hand1Pos) / 2f;
            float distanceToNomi = Vector2.Distance(center, nomiPos);
            
            if (distanceToNomi > config.nomiInteractionRadius * 1.2f)
                return false;
            
            // 检查两手相对位置（应该在差不多的高度）
            float heightDiff = Mathf.Abs(hand0Pos.y - hand1Pos.y);
            if (heightDiff > 50f) return false;
            
            // 识别成功
            TriggerGesture(GestureType.Heart, center, 0.85f);
            return true;
        }
        
        // ============================================================================
        // 手势触发
        // ============================================================================
        
        private void TriggerGesture(GestureType type, Vector2 position, float confidence)
        {
            GestureEvent gestureEvent = new GestureEvent(type, confidence, position);
            
            LogInfo($"Gesture recognized: {gestureEvent.description} (confidence: {confidence:F2})");
            
            // 触发事件
            OnGestureRecognized?.Invoke(gestureEvent);
            
            // 重置状态
            ResetGestureStates();
            ClearTrajectory();
        }
        
        // ============================================================================
        // 辅助函数
        // ============================================================================
        
        /// <summary>
        /// 获取Nomi在屏幕上的位置
        /// </summary>
        private Vector2 GetNomiScreenPosition()
        {
            if (nomiTransform != null && mainCamera != null)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(nomiTransform.position);
                return new Vector2(screenPos.x, screenPos.y);
            }
            
            // 如果没有Nomi Transform，使用配置的归一化位置
            return new Vector2(
                Screen.width * config.nomiScreenPosition.x,
                Screen.height * config.nomiScreenPosition.y
            );
        }
        
        // ============================================================================
        // 日志工具
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[GestureRecognizer] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[GestureRecognizer] {message}");
        }
    }
}

