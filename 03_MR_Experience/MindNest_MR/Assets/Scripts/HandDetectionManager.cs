/*
 * HandDetectionManager.cs
 * =======================
 * 
 * 手部检测管理器
 * 
 * 功能：
 * - 初始化和管理WebCamTexture
 * - 实时处理摄像头画面
 * - 检测画面中的手部区域（基于运动和颜色）
 * - 输出手部中心点位置和边界框
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
    /// 手部检测管理器（支持MediaPipe和简化检测双模式）
    /// </summary>
    public class HandDetectionManager : MonoBehaviour
    {
        [Header("检测模式")]
        [Tooltip("启用MediaPipe精确检测（需要ONNX模型）")]
        public bool useMediaPipe = false;
        
        [Header("MediaPipe组件")]
        public MediaPipeHandsManager mediaPipeManager;
        public LandmarkGestureRecognizer landmarkRecognizer;
        
        [Header("配置")]
        public GestureConfig config = new GestureConfig();
        
        [Header("调试")]
        public bool enableDebugLog = true;
        public bool showDebugVisualization = false;
        
        // ============================================================================
        // 摄像头相关
        // ============================================================================
        
        private WebCamTexture webCamTexture;
        private Texture2D processTexture;
        private Color32[] currentPixels;
        private Color32[] previousPixels;
        
        // ============================================================================
        // 手部检测数据
        // ============================================================================
        
        private HandData[] hands = new HandData[2];  // 支持双手检测
        private List<Vector2> motionPoints = new List<Vector2>();
        
        // ============================================================================
        // 状态
        // ============================================================================
        
        private bool isInitialized = false;
        private bool isRunning = false;
        private float lastProcessTime = 0f;
        private float processInterval;
        
        // ============================================================================
        // 事件
        // ============================================================================
        
        public System.Action<HandData[]> OnHandsDetected;
        
        // ============================================================================
        // Unity生命周期
        // ============================================================================
        
        void Start()
        {
            // 初始化手部数据
            hands[0] = new HandData(0);
            hands[1] = new HandData(1);
            
            // 计算处理间隔
            processInterval = 1f / config.targetFPS;
            
            // 初始化MediaPipe（如果启用）
            InitializeMediaPipe();
            
            LogInfo($"HandDetectionManager initialized (MediaPipe: {useMediaPipe})");
        }
        
        void Update()
        {
            if (!isRunning) return;
            
            // 控制处理频率
            if (Time.time - lastProcessTime >= processInterval)
            {
                if (useMediaPipe && mediaPipeManager != null && mediaPipeManager.IsUsingMediaPipe())
                {
                    ProcessFrameWithMediaPipe();
                }
                else
                {
                    ProcessFrame();  // 使用简化检测
                }
                lastProcessTime = Time.time;
            }
        }
        
        void OnDestroy()
        {
            StopDetection();
        }
        
        // ============================================================================
        // MediaPipe集成
        // ============================================================================
        
        private void InitializeMediaPipe()
        {
            if (!useMediaPipe)
            {
                LogInfo("MediaPipe disabled, using simplified detection");
                return;
            }
            
            // 创建MediaPipe管理器（如果还没有）
            if (mediaPipeManager == null)
            {
                GameObject mpObj = new GameObject("MediaPipeHandsManager");
                mpObj.transform.SetParent(transform);
                mediaPipeManager = mpObj.AddComponent<MediaPipeHandsManager>();
                mediaPipeManager.useMediaPipe = true;
                mediaPipeManager.useGPU = true;
                mediaPipeManager.enableDebugLog = enableDebugLog;
            }
            
            // 创建关键点识别器（如果还没有）
            if (landmarkRecognizer == null && mediaPipeManager != null)
            {
                GameObject lrObj = new GameObject("LandmarkGestureRecognizer");
                lrObj.transform.SetParent(transform);
                landmarkRecognizer = lrObj.AddComponent<LandmarkGestureRecognizer>();
                landmarkRecognizer.enableDebugLog = enableDebugLog;
            }
            
            LogInfo("MediaPipe components initialized");
        }
        
        /// <summary>
        /// 使用MediaPipe处理帧
        /// </summary>
        private void ProcessFrameWithMediaPipe()
        {
            if (webCamTexture == null || !webCamTexture.isPlaying) return;
            if (mediaPipeManager == null || !mediaPipeManager.IsInitialized()) return;
            
            // 将WebCamTexture转换为Texture2D
            if (processTexture == null)
            {
                processTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGB24, false);
            }
            
            processTexture.SetPixels32(webCamTexture.GetPixels32());
            processTexture.Apply();
            
            // 使用MediaPipe处理
            HandLandmarks[] landmarks = mediaPipeManager.ProcessFrame(processTexture);
            
            // 将关键点数据转换为HandData格式（保持兼容性）
            ConvertLandmarksToHandData(landmarks);
            
            // 同时使用关键点识别器
            if (landmarkRecognizer != null)
            {
                landmarkRecognizer.ProcessLandmarks(landmarks);
            }
            
            // 触发事件
            OnHandsDetected?.Invoke(hands);
        }
        
        /// <summary>
        /// 将MediaPipe关键点转换为HandData
        /// </summary>
        private void ConvertLandmarksToHandData(HandLandmarks[] landmarks)
        {
            for (int i = 0; i < 2; i++)
            {
                if (i < landmarks.Length && landmarks[i].isValid)
                {
                    Vector3 palmCenter = landmarks[i].GetPalmCenter();
                    Vector2 position = new Vector2(palmCenter.x, palmCenter.y);
                    
                    // 计算边界框（简化：使用手腕到中指的距离）
                    Vector3 wrist = landmarks[i].GetWrist();
                    Vector3 middleTip = landmarks[i].GetFingerTip(2);
                    float size = Vector3.Distance(wrist, middleTip);
                    
                    Rect bbox = new Rect(position.x - size/2, position.y - size/2, size, size);
                    float area = size * size;
                    
                    hands[i].Update(position, area, bbox, landmarks[i].confidence);
                }
                else
                {
                    hands[i].MarkAsLost();
                }
            }
        }
        
        /// <summary>
        /// 切换检测模式
        /// </summary>
        public void SwitchDetectionMode(bool enableMediaPipe)
        {
            if (useMediaPipe == enableMediaPipe) return;
            
            useMediaPipe = enableMediaPipe;
            
            if (useMediaPipe)
            {
                InitializeMediaPipe();
            }
            
            LogInfo($"Detection mode switched to: {(useMediaPipe ? "MediaPipe" : "Simplified")}");
        }
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 启动手部检测
        /// </summary>
        public IEnumerator StartDetection()
        {
            if (isRunning)
            {
                LogWarning("Detection already running");
                yield break;
            }
            
            LogInfo("Starting hand detection...");
            
            // 请求摄像头权限
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                LogError("Camera permission denied");
                yield break;
            }
            
            // 初始化摄像头
            yield return InitializeCamera();
            
            if (isInitialized)
            {
                isRunning = true;
                LogInfo("Hand detection started successfully");
            }
        }
        
        /// <summary>
        /// 停止手部检测
        /// </summary>
        public void StopDetection()
        {
            if (!isRunning) return;
            
            isRunning = false;
            
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }
            
            if (processTexture != null)
            {
                Destroy(processTexture);
                processTexture = null;
            }
            
            isInitialized = false;
            LogInfo("Hand detection stopped");
        }
        
        /// <summary>
        /// 获取当前检测到的手部数据
        /// </summary>
        public HandData[] GetHands()
        {
            return hands;
        }
        
        /// <summary>
        /// 获取摄像头纹理（用于UI显示）
        /// </summary>
        public WebCamTexture GetCameraTexture()
        {
            return webCamTexture;
        }
        
        /// <summary>
        /// 检查是否正在运行
        /// </summary>
        public bool IsRunning()
        {
            return isRunning;
        }
        
        // ============================================================================
        // 摄像头初始化
        // ============================================================================
        
        private IEnumerator InitializeCamera()
        {
            // 获取可用摄像头
            WebCamDevice[] devices = WebCamTexture.devices;
            
            if (devices.Length == 0)
            {
                LogError("No camera found");
                yield break;
            }
            
            LogInfo($"Found {devices.Length} camera(s)");
            
            // 使用第一个摄像头
            string deviceName = devices[0].name;
            LogInfo($"Using camera: {deviceName}");
            
            // 创建WebCamTexture
            webCamTexture = new WebCamTexture(deviceName, config.cameraWidth, config.cameraHeight, config.targetFPS);
            webCamTexture.Play();
            
            // 等待摄像头启动
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!webCamTexture.didUpdateThisFrame && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!webCamTexture.didUpdateThisFrame)
            {
                LogError("Camera failed to start");
                yield break;
            }
            
            // 创建处理纹理
            processTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            currentPixels = new Color32[webCamTexture.width * webCamTexture.height];
            previousPixels = new Color32[webCamTexture.width * webCamTexture.height];
            
            isInitialized = true;
            LogInfo($"Camera initialized: {webCamTexture.width}x{webCamTexture.height}");
        }
        
        // ============================================================================
        // 帧处理
        // ============================================================================
        
        private void ProcessFrame()
        {
            if (!webCamTexture.didUpdateThisFrame) return;
            
            // 获取当前帧像素数据
            webCamTexture.GetPixels32(currentPixels);
            
            // 检测运动和手部区域
            DetectMotionAndHands();
            
            // 保存当前帧作为下一帧的参考
            System.Array.Copy(currentPixels, previousPixels, currentPixels.Length);
            
            // 触发事件
            OnHandsDetected?.Invoke(hands);
        }
        
        // ============================================================================
        // 手部检测算法
        // ============================================================================
        
        private void DetectMotionAndHands()
        {
            motionPoints.Clear();
            
            int width = webCamTexture.width;
            int height = webCamTexture.height;
            
            // 简化算法：检测画面中的运动区域
            // 跳跃采样以提高性能（每8像素采样一次）
            int step = 8;
            
            for (int y = 0; y < height; y += step)
            {
                for (int x = 0; x < width; x += step)
                {
                    int index = y * width + x;
                    
                    // 计算颜色差异（运动检测）
                    Color32 current = currentPixels[index];
                    Color32 previous = previousPixels[index];
                    
                    int diff = Mathf.Abs(current.r - previous.r) + 
                               Mathf.Abs(current.g - previous.g) + 
                               Mathf.Abs(current.b - previous.b);
                    
                    // 如果检测到运动，并且颜色在肤色范围内
                    if (diff > config.motionThreshold * 3 && IsSkinColor(current))
                    {
                        motionPoints.Add(new Vector2(x, y));
                    }
                }
            }
            
            // 基于运动点聚类检测手部
            ClusterAndDetectHands();
        }
        
        /// <summary>
        /// 简化的肤色检测
        /// </summary>
        private bool IsSkinColor(Color32 color)
        {
            // 简化的肤色判断（RGB范围）
            // 这个范围适用于大多数肤色
            int r = color.r;
            int g = color.g;
            int b = color.b;
            
            // 基本条件：R > G > B，且R值较高
            bool condition1 = r > 95 && g > 40 && b > 20;
            bool condition2 = r > g && r > b;
            bool condition3 = Mathf.Max(r, Mathf.Max(g, b)) - Mathf.Min(r, Mathf.Min(g, b)) > 15;
            bool condition4 = Mathf.Abs(r - g) > 15;
            
            return condition1 && condition2 && condition3 && condition4;
        }
        
        /// <summary>
        /// 聚类并检测手部
        /// </summary>
        private void ClusterAndDetectHands()
        {
            if (motionPoints.Count < 10)
            {
                // 运动点太少，标记手部丢失
                hands[0].MarkAsLost();
                hands[1].MarkAsLost();
                return;
            }
            
            // 简化版本：使用K-means聚类（k=2）检测最多两只手
            List<List<Vector2>> clusters = SimpleTwoCluster(motionPoints);
            
            // 更新手部数据
            for (int i = 0; i < Mathf.Min(clusters.Count, 2); i++)
            {
                if (clusters[i].Count < 5) continue;
                
                // 计算手部中心点和边界框
                Vector2 center = CalculateCenter(clusters[i]);
                Rect bounds = CalculateBounds(clusters[i]);
                float area = bounds.width * bounds.height;
                
                // 检查区域大小是否合理
                if (area >= config.minHandArea && area <= config.maxHandArea)
                {
                    hands[i].Update(center, area, bounds, 0.8f);
                }
                else
                {
                    hands[i].MarkAsLost();
                }
            }
            
            // 标记未检测到的手
            if (clusters.Count < 2)
            {
                hands[1].MarkAsLost();
            }
            
            // 检查手部超时
            for (int i = 0; i < hands.Length; i++)
            {
                if (hands[i].isDetected && Time.time - hands[i].lastSeenTime > config.handLostTimeout)
                {
                    hands[i].MarkAsLost();
                }
            }
        }
        
        /// <summary>
        /// 简化的二聚类算法
        /// </summary>
        private List<List<Vector2>> SimpleTwoCluster(List<Vector2> points)
        {
            List<List<Vector2>> clusters = new List<List<Vector2>>();
            
            if (points.Count == 0) return clusters;
            
            // 找到最左和最右的点作为初始聚类中心
            Vector2 leftmost = points[0];
            Vector2 rightmost = points[0];
            
            foreach (Vector2 p in points)
            {
                if (p.x < leftmost.x) leftmost = p;
                if (p.x > rightmost.x) rightmost = p;
            }
            
            // 如果两个点太近，只有一个簇
            if (Vector2.Distance(leftmost, rightmost) < 100f)
            {
                clusters.Add(new List<Vector2>(points));
                return clusters;
            }
            
            // 分配点到最近的聚类中心
            List<Vector2> cluster1 = new List<Vector2>();
            List<Vector2> cluster2 = new List<Vector2>();
            
            foreach (Vector2 p in points)
            {
                float dist1 = Vector2.Distance(p, leftmost);
                float dist2 = Vector2.Distance(p, rightmost);
                
                if (dist1 < dist2)
                    cluster1.Add(p);
                else
                    cluster2.Add(p);
            }
            
            if (cluster1.Count > 0) clusters.Add(cluster1);
            if (cluster2.Count > 0) clusters.Add(cluster2);
            
            return clusters;
        }
        
        /// <summary>
        /// 计算点集的中心
        /// </summary>
        private Vector2 CalculateCenter(List<Vector2> points)
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2 p in points)
            {
                sum += p;
            }
            return sum / points.Count;
        }
        
        /// <summary>
        /// 计算点集的边界框
        /// </summary>
        private Rect CalculateBounds(List<Vector2> points)
        {
            if (points.Count == 0) return new Rect(0, 0, 0, 0);
            
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            
            foreach (Vector2 p in points)
            {
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        
        // ============================================================================
        // 日志工具
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[HandDetector] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[HandDetector] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[HandDetector] {message}");
        }
    }
}

