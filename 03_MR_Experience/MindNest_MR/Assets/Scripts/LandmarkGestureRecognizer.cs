/*
 * LandmarkGestureRecognizer.cs
 * ============================
 * 
 * 基于21个手部关键点的手势识别器
 * 
 * 功能：
 * - 使用MediaPipe输出的21个关键点
 * - 更精确地识别6种手势
 * - 基于几何特征和手指状态判断
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using UnityEngine;
using System.Collections.Generic;

namespace MindNest.MR
{
    /// <summary>
    /// 基于关键点的手势识别器
    /// </summary>
    public class LandmarkGestureRecognizer : MonoBehaviour
    {
        [Header("配置")]
        public GestureConfig config = new GestureConfig();
        
        [Header("Nomi位置")]
        public Transform nomiTransform;
        public Camera mainCamera;
        
        [Header("调试")]
        public bool enableDebugLog = true;
        
        // ============================================================================
        // 手指状态枚举
        // ============================================================================
        
        private enum FingerState
        {
            Closed,     // 弯曲
            Open,       // 伸直
            Partial     // 部分伸直
        }
        
        // ============================================================================
        // 轨迹数据
        // ============================================================================
        
        private List<Vector2> hand0Trajectory = new List<Vector2>();
        private List<Vector2> hand1Trajectory = new List<Vector2>();
        private List<float> trajectoryTimestamps = new List<float>();
        private int trajectoryMaxLength = 60;  // 3秒@20FPS
        
        // ============================================================================
        // 手势状态
        // ============================================================================
        
        // 戳戳
        private bool pokeApproaching = false;
        private Vector2 pokeStartPos;
        
        // 挥手
        private int waveCount = 0;
        private float lastWaveX = 0f;
        
        // 投喂
        private bool feedStarted = false;
        private Vector2 feedStartPos;
        
        // ============================================================================
        // 事件
        // ============================================================================
        
        public System.Action<GestureEvent> OnGestureRecognized;
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 处理关键点数据并识别手势
        /// </summary>
        public void ProcessLandmarks(HandLandmarks[] hands)
        {
            if (hands == null || hands.Length == 0) return;
            
            // 更新轨迹
            UpdateTrajectory(hands);
            
            // 识别手势
            RecognizeGesturesFromLandmarks(hands);
        }
        
        /// <summary>
        /// 重置识别器
        /// </summary>
        public void ResetRecognizer()
        {
            ClearTrajectory();
            ResetGestureStates();
        }
        
        // ============================================================================
        // 轨迹管理
        // ============================================================================
        
        private void UpdateTrajectory(HandLandmarks[] hands)
        {
            // 记录手掌中心轨迹
            if (hands[0].isValid)
            {
                Vector3 palmCenter = hands[0].GetPalmCenter();
                hand0Trajectory.Add(new Vector2(palmCenter.x, palmCenter.y));
            }
            
            if (hands.Length > 1 && hands[1].isValid)
            {
                Vector3 palmCenter = hands[1].GetPalmCenter();
                hand1Trajectory.Add(new Vector2(palmCenter.x, palmCenter.y));
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
            waveCount = 0;
            feedStarted = false;
        }
        
        // ============================================================================
        // 手势识别
        // ============================================================================
        
        private void RecognizeGesturesFromLandmarks(HandLandmarks[] hands)
        {
            Vector2 nomiScreenPos = GetNomiScreenPosition();
            
            // 双手手势
            if (hands.Length >= 2 && hands[0].isValid && hands[1].isValid)
            {
                if (RecognizeHugFromLandmarks(hands, nomiScreenPos))
                    return;
                    
                if (RecognizeHeartFromLandmarks(hands, nomiScreenPos))
                    return;
            }
            
            // 单手手势（使用第一只有效的手）
            HandLandmarks activeHand = hands[0].isValid ? hands[0] : (hands.Length > 1 ? hands[1] : null);
            
            if (activeHand != null && activeHand.isValid)
            {
                if (RecognizePokeFromLandmarks(activeHand, nomiScreenPos))
                    return;
                    
                if (RecognizeFeedFromLandmarks(activeHand, nomiScreenPos))
                    return;
                    
                if (RecognizeWaveFromLandmarks(activeHand, nomiScreenPos))
                    return;
                    
                if (RecognizeStrokeFromLandmarks(activeHand, nomiScreenPos))
                    return;
            }
        }
        
        // ============================================================================
        // 抚摸识别
        // ============================================================================
        
        private bool RecognizeStrokeFromLandmarks(HandLandmarks hand, Vector2 nomiPos)
        {
            // 1. 检查手是否在Nomi附近
            Vector3 palmCenter = hand.GetPalmCenter();
            float distance = Vector2.Distance(new Vector2(palmCenter.x, palmCenter.y), nomiPos);
            
            if (distance > config.nomiInteractionRadius)
                return false;
            
            // 2. 检查手掌是否平展（所有手指都伸展）
            FingerState[] fingerStates = GetFingerStates(hand);
            int openFingerCount = 0;
            for (int i = 0; i < fingerStates.Length; i++)
            {
                if (fingerStates[i] == FingerState.Open)
                    openFingerCount++;
            }
            
            if (openFingerCount < 4)  // 至少4个手指伸展
                return false;
            
            // 3. 检查水平移动速度
            if (hand0Trajectory.Count < 10) return false;
            
            Vector2 recentStart = hand0Trajectory[hand0Trajectory.Count - 10];
            Vector2 recentEnd = hand0Trajectory[hand0Trajectory.Count - 1];
            Vector2 movement = recentEnd - recentStart;
            float speed = movement.magnitude / (Time.time - trajectoryTimestamps[trajectoryTimestamps.Count - 10]);
            
            if (speed >= config.strokeSpeedMin && speed <= config.strokeSpeedMax)
            {
                // 识别成功
                TriggerGesture(GestureType.Stroke, palmCenter, 0.9f);
                return true;
            }
            
            return false;
        }
        
        // ============================================================================
        // 戳戳识别
        // ============================================================================
        
        private bool RecognizePokeFromLandmarks(HandLandmarks hand, Vector2 nomiPos)
        {
            // 1. 检查手势：食指伸直，其他手指弯曲
            FingerState[] fingerStates = GetFingerStates(hand);
            
            bool isPointingGesture = (fingerStates[1] == FingerState.Open) &&  // 食指伸直
                                      (fingerStates[0] == FingerState.Closed || fingerStates[0] == FingerState.Partial) &&  // 拇指
                                      (fingerStates[2] == FingerState.Closed) &&  // 中指
                                      (fingerStates[3] == FingerState.Closed) &&  // 无名指
                                      (fingerStates[4] == FingerState.Closed);    // 小指
            
            if (!isPointingGesture)
                return false;
            
            // 2. 检查食指尖是否快速靠近Nomi
            Vector3 indexTip = hand.GetFingerTip(1);  // 食指尖
            Vector2 indexPos2D = new Vector2(indexTip.x, indexTip.y);
            float distance = Vector2.Distance(indexPos2D, nomiPos);
            
            if (hand0Trajectory.Count < 5) return false;
            
            Vector2 prevPos = hand0Trajectory[hand0Trajectory.Count - 5];
            Vector2 currPos = hand0Trajectory[hand0Trajectory.Count - 1];
            float speed = (currPos - prevPos).magnitude / 0.25f;  // 5帧约0.25秒
            
            // 快速接近
            if (!pokeApproaching && speed > config.pokeSpeedThreshold && distance < config.nomiInteractionRadius)
            {
                pokeApproaching = true;
                pokeStartPos = indexPos2D;
                return false;
            }
            
            // 检测回退
            if (pokeApproaching)
            {
                if (Vector2.Distance(indexPos2D, pokeStartPos) > config.pokeRetractDist)
                {
                    TriggerGesture(GestureType.Poke, indexPos2D, 0.95f);
                    pokeApproaching = false;
                    return true;
                }
            }
            
            return false;
        }
        
        // ============================================================================
        // 投喂识别
        // ============================================================================
        
        private bool RecognizeFeedFromLandmarks(HandLandmarks hand, Vector2 nomiPos)
        {
            Vector3 palmCenter = hand.GetPalmCenter();
            Vector2 palmPos2D = new Vector2(palmCenter.x, palmCenter.y);
            
            // 1. 检查手掌朝向（手腕应该低于指尖，表示手心向上）
            Vector3 wrist = hand.GetWrist();
            float avgFingerHeight = 0f;
            for (int i = 0; i < 5; i++)
            {
                avgFingerHeight += hand.GetFingerTip(i).y;
            }
            avgFingerHeight /= 5f;
            
            bool palmUp = wrist.y < avgFingerHeight;
            
            if (!palmUp) return false;
            
            // 2. 检测从下往上移动
            if (!feedStarted && palmPos2D.y < nomiPos.y - config.feedMinVerticalDist / 2f)
            {
                feedStarted = true;
                feedStartPos = palmPos2D;
                return false;
            }
            
            if (feedStarted)
            {
                float verticalMovement = palmPos2D.y - feedStartPos.y;
                float distance = Vector2.Distance(palmPos2D, nomiPos);
                
                if (verticalMovement > config.feedMinVerticalDist && distance < config.nomiInteractionRadius)
                {
                    TriggerGesture(GestureType.Feed, palmPos2D, 0.9f);
                    feedStarted = false;
                    return true;
                }
            }
            
            return false;
        }
        
        // ============================================================================
        // 抱抱识别
        // ============================================================================
        
        private bool RecognizeHugFromLandmarks(HandLandmarks[] hands, Vector2 nomiPos)
        {
            Vector3 palm0 = hands[0].GetPalmCenter();
            Vector3 palm1 = hands[1].GetPalmCenter();
            
            Vector2 palm0_2D = new Vector2(palm0.x, palm0.y);
            Vector2 palm1_2D = new Vector2(palm1.x, palm1.y);
            
            // 1. 检查两只手在Nomi两侧
            bool handsOnSides = (palm0_2D.x < nomiPos.x && palm1_2D.x > nomiPos.x) ||
                                (palm1_2D.x < nomiPos.x && palm0_2D.x > nomiPos.x);
            
            if (!handsOnSides) return false;
            
            // 2. 检查手掌是否展开（抱抱姿势）
            FingerState[] fingers0 = GetFingerStates(hands[0]);
            FingerState[] fingers1 = GetFingerStates(hands[1]);
            
            int openCount0 = 0, openCount1 = 0;
            for (int i = 0; i < 5; i++)
            {
                if (fingers0[i] != FingerState.Closed) openCount0++;
                if (fingers1[i] != FingerState.Closed) openCount1++;
            }
            
            // 至少有部分手指展开
            if (openCount0 < 2 || openCount1 < 2) return false;
            
            // 3. 检查双手距离Nomi都很近
            float dist0 = Vector2.Distance(palm0_2D, nomiPos);
            float dist1 = Vector2.Distance(palm1_2D, nomiPos);
            
            if (dist0 < config.hugApproachDist && dist1 < config.hugApproachDist)
            {
                Vector2 center = (palm0_2D + palm1_2D) / 2f;
                TriggerGesture(GestureType.Hug, center, 0.95f);
                return true;
            }
            
            return false;
        }
        
        // ============================================================================
        // 挥手识别
        // ============================================================================
        
        private bool RecognizeWaveFromLandmarks(HandLandmarks hand, Vector2 nomiPos)
        {
            Vector3 palmCenter = hand.GetPalmCenter();
            Vector2 palmPos2D = new Vector2(palmCenter.x, palmCenter.y);
            float distance = Vector2.Distance(palmPos2D, nomiPos);
            
            if (distance > config.nomiInteractionRadius * 1.5f)
                return false;
            
            // 1. 检查手掌是否展开
            FingerState[] fingerStates = GetFingerStates(hand);
            int openCount = 0;
            for (int i = 0; i < 5; i++)
            {
                if (fingerStates[i] != FingerState.Closed)
                    openCount++;
            }
            
            if (openCount < 4) return false;
            
            // 2. 检测左右摆动
            if (hand0Trajectory.Count < 5) return false;
            
            float currentX = palmPos2D.x;
            float direction = Mathf.Sign(currentX - lastWaveX);
            
            if (Mathf.Abs(currentX - lastWaveX) > 30f && lastWaveX != 0)
            {
                waveCount++;
            }
            
            lastWaveX = currentX;
            
            if (waveCount >= config.waveMinCycles)
            {
                TriggerGesture(GestureType.Wave, palmPos2D, 0.85f);
                waveCount = 0;
                return true;
            }
            
            return false;
        }
        
        // ============================================================================
        // 比心识别
        // ============================================================================
        
        private bool RecognizeHeartFromLandmarks(HandLandmarks[] hands, Vector2 nomiPos)
        {
            // 1. 获取两手的拇指尖和食指尖
            Vector3 thumb0 = hands[0].GetFingerTip(0);  // 拇指
            Vector3 index0 = hands[0].GetFingerTip(1);  // 食指
            Vector3 thumb1 = hands[1].GetFingerTip(0);
            Vector3 index1 = hands[1].GetFingerTip(1);
            
            // 2. 检查拇指和食指是否弯曲靠近（形成心形的一半）
            float dist0 = Vector3.Distance(thumb0, index0);
            float dist1 = Vector3.Distance(thumb1, index1);
            
            // 两只手的拇指-食指距离都应该较小
            if (dist0 > 100f || dist1 > 100f)
                return false;
            
            // 3. 检查两只手的指尖是否靠近（形成完整的心形）
            Vector3 hand0Center = (thumb0 + index0) / 2f;
            Vector3 hand1Center = (thumb1 + index1) / 2f;
            float handDistance = Vector3.Distance(hand0Center, hand1Center);
            
            if (handDistance > config.heartHandDistance || handDistance < config.heartMinDistance)
                return false;
            
            // 4. 检查是否在Nomi附近
            Vector2 heartCenter = new Vector2((hand0Center.x + hand1Center.x) / 2f, 
                                              (hand0Center.y + hand1Center.y) / 2f);
            float distToNomi = Vector2.Distance(heartCenter, nomiPos);
            
            if (distToNomi < config.nomiInteractionRadius * 1.2f)
            {
                TriggerGesture(GestureType.Heart, heartCenter, 0.9f);
                return true;
            }
            
            return false;
        }
        
        // ============================================================================
        // 辅助函数
        // ============================================================================
        
        /// <summary>
        /// 获取每个手指的状态（伸直/弯曲）
        /// </summary>
        private FingerState[] GetFingerStates(HandLandmarks hand)
        {
            FingerState[] states = new FingerState[5];
            
            // 对每个手指，计算指尖到手腕的距离
            Vector3 wrist = hand.GetWrist();
            
            for (int i = 0; i < 5; i++)
            {
                Vector3 fingerTip = hand.GetFingerTip(i);
                Vector3 fingerBase = hand.landmarks[1 + i * 4];  // 手指根部关键点
                
                float tipToWristDist = Vector3.Distance(fingerTip, wrist);
                float baseToWristDist = Vector3.Distance(fingerBase, wrist);
                
                float ratio = tipToWristDist / (baseToWristDist + 0.001f);
                
                if (ratio > 2.0f)
                    states[i] = FingerState.Open;
                else if (ratio > 1.3f)
                    states[i] = FingerState.Partial;
                else
                    states[i] = FingerState.Closed;
            }
            
            return states;
        }
        
        private Vector2 GetNomiScreenPosition()
        {
            if (nomiTransform != null && mainCamera != null)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(nomiTransform.position);
                return new Vector2(screenPos.x, screenPos.y);
            }
            
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
        
        private void TriggerGesture(GestureType type, Vector3 position, float confidence)
        {
            GestureEvent gestureEvent = new GestureEvent(type, confidence, new Vector2(position.x, position.y));
            
            LogInfo($"Landmark gesture recognized: {gestureEvent.description} (confidence: {confidence:F2})");
            
            OnGestureRecognized?.Invoke(gestureEvent);
            
            ResetGestureStates();
            ClearTrajectory();
        }
        
        private void LogInfo(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[LandmarkGestureRecognizer] {message}");
            }
        }
    }
}

