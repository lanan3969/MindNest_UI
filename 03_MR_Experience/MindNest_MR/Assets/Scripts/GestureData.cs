/*
 * GestureData.cs
 * ==============
 * 
 * 手势识别数据结构定义
 * 
 * 定义：
 * - 手势类型枚举
 * - 手部检测数据结构
 * - 手势事件数据结构
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using UnityEngine;
using System;

namespace MindNest.MR
{
    /// <summary>
    /// 手势类型枚举
    /// </summary>
    public enum GestureType
    {
        None,      // 无手势
        Stroke,    // 抚摸
        Poke,      // 戳戳
        Feed,      // 投喂
        Hug,       // 抱抱
        Wave,      // 挥手
        Heart      // 比心
    }
    
    /// <summary>
    /// 手部检测数据
    /// </summary>
    [Serializable]
    public class HandData
    {
        public int handId;                    // 手部ID（0=左手，1=右手）
        public Vector2 position;              // 手部中心位置（屏幕坐标）
        public Vector2 velocity;              // 移动速度
        public float area;                    // 手部区域面积
        public Rect boundingBox;              // 边界框
        public float confidence;              // 置信度（0-1）
        public bool isDetected;               // 是否检测到
        public float lastSeenTime;            // 最后检测时间
        
        public HandData(int id)
        {
            handId = id;
            position = Vector2.zero;
            velocity = Vector2.zero;
            area = 0f;
            boundingBox = new Rect(0, 0, 0, 0);
            confidence = 0f;
            isDetected = false;
            lastSeenTime = 0f;
        }
        
        /// <summary>
        /// 更新手部数据
        /// </summary>
        public void Update(Vector2 newPos, float newArea, Rect newBounds, float newConfidence)
        {
            // 计算速度
            velocity = newPos - position;
            
            // 更新位置和其他数据
            position = newPos;
            area = newArea;
            boundingBox = newBounds;
            confidence = newConfidence;
            isDetected = true;
            lastSeenTime = Time.time;
        }
        
        /// <summary>
        /// 标记为未检测到
        /// </summary>
        public void MarkAsLost()
        {
            isDetected = false;
            confidence = 0f;
        }
    }
    
    /// <summary>
    /// 手势事件数据
    /// </summary>
    [Serializable]
    public class GestureEvent
    {
        public GestureType gestureType;       // 手势类型
        public float confidence;              // 置信度（0-1）
        public Vector2 position;              // 手势发生位置
        public float timestamp;               // 时间戳
        public string description;            // 手势描述
        
        public GestureEvent(GestureType type, float conf, Vector2 pos)
        {
            gestureType = type;
            confidence = conf;
            position = pos;
            timestamp = Time.time;
            description = GetGestureDescription(type);
        }
        
        /// <summary>
        /// 获取手势的中文描述
        /// </summary>
        public static string GetGestureDescription(GestureType type)
        {
            switch (type)
            {
                case GestureType.Stroke:
                    return "抚摸";
                case GestureType.Poke:
                    return "戳戳";
                case GestureType.Feed:
                    return "投喂";
                case GestureType.Hug:
                    return "抱抱";
                case GestureType.Wave:
                    return "挥手";
                case GestureType.Heart:
                    return "比心";
                default:
                    return "无";
            }
        }
        
        /// <summary>
        /// 获取手势图标资源名称
        /// </summary>
        public static string GetGestureIconName(GestureType type)
        {
            switch (type)
            {
                case GestureType.Stroke:
                    return "stroke";
                case GestureType.Poke:
                    return "poke";
                case GestureType.Feed:
                    return "feed";
                case GestureType.Hug:
                    return "hug";
                case GestureType.Wave:
                    return "wave";
                case GestureType.Heart:
                    return "heart";
                default:
                    return "";
            }
        }
        
        /// <summary>
        /// 获取手势对应的Nomi表情
        /// </summary>
        public static string GetGestureExpression(GestureType type)
        {
            switch (type)
            {
                case GestureType.Stroke:
                    return "happy";
                case GestureType.Poke:
                    return "surprise";
                case GestureType.Feed:
                    return "eating";
                case GestureType.Hug:
                    return "love";
                case GestureType.Wave:
                    return "welcome";
                case GestureType.Heart:
                    return "celebrate";
                default:
                    return "happy";
            }
        }
        
        /// <summary>
        /// 获取手势的养料奖励
        /// </summary>
        public static int GetGestureReward(GestureType type)
        {
            switch (type)
            {
                case GestureType.Stroke:
                case GestureType.Poke:
                    return 10;  // 简单手势：10分
                case GestureType.Feed:
                    return 15;  // 中等手势：15分
                case GestureType.Hug:
                case GestureType.Heart:
                    return 20;  // 复杂手势：20分
                case GestureType.Wave:
                    return 5;   // 打招呼：5分
                default:
                    return 0;
            }
        }
    }
    
    /// <summary>
    /// 手势识别配置
    /// </summary>
    [Serializable]
    public class GestureConfig
    {
        [Header("摄像头设置")]
        public int cameraWidth = 640;
        public int cameraHeight = 480;
        public int targetFPS = 20;
        
        [Header("手部检测阈值")]
        public float minHandArea = 500f;           // 最小手部区域面积
        public float maxHandArea = 50000f;         // 最大手部区域面积
        public float motionThreshold = 2f;         // 运动阈值（像素）
        public float handLostTimeout = 0.5f;       // 手部丢失超时（秒）
        
        [Header("手势识别阈值")]
        public float strokeMinDuration = 0.8f;     // 抚摸最小持续时间
        public float strokeSpeedMin = 0.5f;        // 抚摸最小速度
        public float strokeSpeedMax = 3.0f;        // 抚摸最大速度
        
        public float pokeSpeedThreshold = 8.0f;    // 戳戳速度阈值
        public float pokeRetractDist = 50f;        // 戳戳回退距离
        
        public float feedMinVerticalDist = 150f;   // 投喂最小垂直距离
        
        public float hugHandDistance = 250f;       // 抱抱双手距离
        public float hugApproachDist = 120f;       // 抱抱接近距离
        
        public int waveMinCycles = 3;              // 挥手最小次数
        public float waveSpeedThreshold = 4.0f;    // 挥手速度阈值
        
        public float heartHandDistance = 80f;      // 比心双手距离
        public float heartMinDistance = 20f;       // 比心最小距离
        
        [Header("Nomi位置设置")]
        public Vector2 nomiScreenPosition = new Vector2(0.5f, 0.5f);  // Nomi在屏幕上的归一化位置
        public float nomiInteractionRadius = 150f;  // Nomi交互半径（像素）
    }
}

