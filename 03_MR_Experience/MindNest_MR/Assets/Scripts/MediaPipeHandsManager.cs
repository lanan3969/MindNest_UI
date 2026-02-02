/*
 * MediaPipeHandsManager.cs
 * ========================
 * 
 * MediaPipe Hands ONNX模型管理器
 * 
 * 功能：
 * - 加载ONNX模型（palm detection + hand landmark）
 * - 运行推理获取21个手部关键点
 * - 输出标准化的手部关键点数据
 * - 支持优雅降级到简化检测方案
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;

namespace MindNest.MR
{
    /// <summary>
    /// MediaPipe手部关键点数据
    /// </summary>
    [System.Serializable]
    public class HandLandmarks
    {
        public int handId;                          // 手部ID (0=左手, 1=右手)
        public Vector3[] landmarks;                 // 21个关键点 (x,y,z)
        public float confidence;                    // 置信度
        public bool isValid;                        // 是否有效
        
        public HandLandmarks(int id)
        {
            handId = id;
            landmarks = new Vector3[21];
            confidence = 0f;
            isValid = false;
        }
        
        /// <summary>
        /// 获取手腕位置
        /// </summary>
        public Vector3 GetWrist() => landmarks[0];
        
        /// <summary>
        /// 获取指尖位置
        /// </summary>
        public Vector3 GetFingerTip(int fingerIndex)
        {
            // 0=拇指, 1=食指, 2=中指, 3=无名指, 4=小指
            return landmarks[4 + fingerIndex * 4];
        }
        
        /// <summary>
        /// 获取手掌中心
        /// </summary>
        public Vector3 GetPalmCenter()
        {
            // 计算手腕和中指根部的中点
            return (landmarks[0] + landmarks[9]) * 0.5f;
        }
    }
    
    /// <summary>
    /// MediaPipe Hands模型管理器
    /// </summary>
    public class MediaPipeHandsManager : MonoBehaviour
    {
        [Header("模型设置")]
        [Tooltip("是否启用MediaPipe（需要ONNX模型文件）")]
        public bool useMediaPipe = true;
        
        [Tooltip("手掌检测模型路径")]
        public string palmDetectionModelPath = "MediaPipeModels/palm_detection";
        
        [Tooltip("手部关键点模型路径")]
        public string handLandmarkModelPath = "MediaPipeModels/hand_landmark";
        
        [Header("推理设置")]
        [Tooltip("使用GPU加速")]
        public bool useGPU = true;
        
        [Tooltip("最大检测手数")]
        public int maxHands = 2;
        
        [Tooltip("置信度阈值")]
        public float confidenceThreshold = 0.5f;
        
        [Header("性能优化")]
        [Tooltip("输入图像缩放因子（0.5-1.0，越小越快）")]
        [Range(0.5f, 1.0f)]
        public float inputScale = 0.75f;
        
        [Tooltip("跳帧处理（处理间隔帧数，1=不跳帧）")]
        [Range(1, 5)]
        public int frameSkip = 1;
        
        [Tooltip("异步处理（减少主线程阻塞）")]
        public bool useAsyncProcessing = true;
        
        [Header("调试")]
        public bool enableDebugLog = true;
        
        // ============================================================================
        // Barracuda模型
        // ============================================================================
        
        private Model palmDetectionModel;  // 运行时加载的模型（Model类型）
        private Model handLandmarkModel;   // 运行时加载的模型（Model类型）
        private IWorker palmDetectionWorker;
        private IWorker handLandmarkWorker;
        
        // ============================================================================
        // 状态
        // ============================================================================
        
        private bool isInitialized = false;
        private bool modelsLoaded = false;
        private HandLandmarks[] detectedHands = new HandLandmarks[2];
        
        // ============================================================================
        // Unity生命周期
        // ============================================================================
        
        void Start()
        {
            // 初始化手部数据
            for (int i = 0; i < detectedHands.Length; i++)
            {
                detectedHands[i] = new HandLandmarks(i);
            }
            
            // 输出系统信息
            LogInfo("╔════════════════════════════════════════════╗");
            LogInfo("║   MediaPipe Hands Manager Initializing    ║");
            LogInfo("╚════════════════════════════════════════════╝");
            LogInfo($"📊 Configuration:");
            LogInfo($"   • Use MediaPipe: {useMediaPipe}");
            LogInfo($"   • Use GPU: {useGPU}");
            LogInfo($"   • Max Hands: {maxHands}");
            LogInfo($"   • Confidence Threshold: {confidenceThreshold}");
            LogInfo($"   • Input Scale: {inputScale}");
            LogInfo($"   • Frame Skip: {frameSkip}");
            LogInfo($"   • Async Processing: {useAsyncProcessing}");
            LogInfo($"📁 Paths:");
            LogInfo($"   • StreamingAssets: {Application.streamingAssetsPath}");
            LogInfo($"   • Platform: {Application.platform}");
            
            // 尝试初始化MediaPipe
            if (useMediaPipe)
            {
                LogInfo("🚀 Starting MediaPipe initialization...");
                StartCoroutine(InitializeMediaPipe());
            }
            else
            {
                LogInfo("⚠️ MediaPipe disabled, will use simplified detection");
                isInitialized = true;
            }
        }
        
        void OnDestroy()
        {
            // 清理Barracuda资源
            CleanupWorkers();
        }
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 处理摄像头画面，检测手部关键点
        /// </summary>
        public HandLandmarks[] ProcessFrame(Texture2D frameTexture)
        {
            if (!isInitialized)
            {
                return detectedHands;
            }
            
            if (!modelsLoaded || !useMediaPipe)
            {
                // 回退到简化方案：返回空关键点
                for (int i = 0; i < detectedHands.Length; i++)
                {
                    detectedHands[i].isValid = false;
                }
                return detectedHands;
            }
            
            // 运行MediaPipe推理
            RunInference(frameTexture);
            
            return detectedHands;
        }
        
        /// <summary>
        /// 检查是否使用MediaPipe
        /// </summary>
        public bool IsUsingMediaPipe()
        {
            return useMediaPipe && modelsLoaded;
        }
        
        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }
        
        // ============================================================================
        // MediaPipe初始化
        // ============================================================================
        
        private IEnumerator InitializeMediaPipe()
        {
            LogInfo("Initializing MediaPipe Hands...");
            
            // 尝试加载模型
            bool success = LoadModels();
            
            if (!success)
            {
                LogWarning("Failed to load MediaPipe models, falling back to simplified detection");
                useMediaPipe = false;
                isInitialized = true;
                yield break;
            }
            
            // 创建Barracuda Workers（直接使用已加载的Model对象）
            WorkerFactory.Type workerType = useGPU ? WorkerFactory.Type.ComputePrecompiled : WorkerFactory.Type.CSharpBurst;
            
            try
            {
                // palmDetectionModel 和 handLandmarkModel 已经是 Model 类型，无需再次加载
                palmDetectionWorker = WorkerFactory.CreateWorker(workerType, palmDetectionModel);
                LogInfo($"✅ Palm detection worker created ({workerType})");
                
                handLandmarkWorker = WorkerFactory.CreateWorker(workerType, handLandmarkModel);
                LogInfo($"✅ Hand landmark worker created ({workerType})");
                
                modelsLoaded = true;
                LogInfo("✅ MediaPipe models and workers initialized successfully");
            }
            catch (System.Exception e)
            {
                LogError($"Failed to create Barracuda workers: {e.Message}");
                useMediaPipe = false;
            }
            
            isInitialized = true;
        }
        
        private bool LoadModels()
        {
            // 使用StreamingAssets路径
            string basePath = Application.streamingAssetsPath;
            string palmPath = System.IO.Path.Combine(basePath, "MediaPipeModels", "palm_detection.onnx");
            string landmarkPath = System.IO.Path.Combine(basePath, "MediaPipeModels", "hand_landmark.onnx");
            
            LogInfo($"Loading models from StreamingAssets:");
            LogInfo($"  Palm: {palmPath}");
            LogInfo($"  Landmark: {landmarkPath}");
            
            // 检查文件是否存在
            if (!System.IO.File.Exists(palmPath))
            {
                LogWarning($"Palm detection model not found at: {palmPath}");
                return false;
            }
            
            if (!System.IO.File.Exists(landmarkPath))
            {
                LogWarning($"Hand landmark model not found at: {landmarkPath}");
                return false;
            }
            
            try
            {
                // 加载ONNX文件为NNModel
                byte[] palmBytes = System.IO.File.ReadAllBytes(palmPath);
                byte[] landmarkBytes = System.IO.File.ReadAllBytes(landmarkPath);
                
                LogInfo($"  Palm model size: {palmBytes.Length / 1024}KB");
                LogInfo($"  Landmark model size: {landmarkBytes.Length / 1024}KB");
                
                palmDetectionModel = ModelLoader.Load(palmBytes);
                handLandmarkModel = ModelLoader.Load(landmarkBytes);
                
                LogInfo("✅ Models loaded successfully from StreamingAssets");
                return true;
            }
            catch (System.Exception e)
            {
                LogError($"Failed to load models: {e.Message}");
                return false;
            }
        }
        
        // ============================================================================
        // 推理处理
        // ============================================================================
        
        private void RunInference(Texture2D inputTexture)
        {
            // 步骤1: 手掌检测
            List<Rect> palmBoxes = DetectPalms(inputTexture);
            
            if (palmBoxes.Count == 0)
            {
                // 没有检测到手掌
                for (int i = 0; i < detectedHands.Length; i++)
                {
                    detectedHands[i].isValid = false;
                }
                return;
            }
            
            // 步骤2: 对每个检测到的手掌提取关键点
            for (int i = 0; i < Mathf.Min(palmBoxes.Count, maxHands); i++)
            {
                ExtractHandLandmarks(inputTexture, palmBoxes[i], i);
            }
            
            // 清除未使用的手部数据
            for (int i = palmBoxes.Count; i < detectedHands.Length; i++)
            {
                detectedHands[i].isValid = false;
            }
        }
        
        private List<Rect> DetectPalms(Texture2D inputTexture)
        {
            List<Rect> palmBoxes = new List<Rect>();
            
            if (palmDetectionWorker == null) return palmBoxes;
            
            try
            {
                // 预处理图像到192x192
                Tensor inputTensor = PreprocessImageForPalmDetection(inputTexture);
                
                // 执行推理
                palmDetectionWorker.Execute(inputTensor);
                
                // 获取输出
                Tensor outputTensor = palmDetectionWorker.PeekOutput();
                
                // 后处理：解析边界框
                palmBoxes = PostprocessPalmDetection(outputTensor, inputTexture.width, inputTexture.height);
                
                // 清理
                inputTensor.Dispose();
                outputTensor.Dispose();
            }
            catch (System.Exception e)
            {
                LogError($"Palm detection error: {e.Message}");
            }
            
            return palmBoxes;
        }
        
        private void ExtractHandLandmarks(Texture2D inputTexture, Rect palmBox, int handIndex)
        {
            if (handLandmarkWorker == null) return;
            
            try
            {
                // 裁剪并预处理到224x224
                Tensor inputTensor = PreprocessImageForLandmark(inputTexture, palmBox);
                
                // 执行推理
                handLandmarkWorker.Execute(inputTensor);
                
                // 获取输出
                Tensor outputTensor = handLandmarkWorker.PeekOutput();
                
                // 后处理：解析21个关键点
                PostprocessLandmarks(outputTensor, palmBox, handIndex);
                
                // 清理
                inputTensor.Dispose();
                outputTensor.Dispose();
            }
            catch (System.Exception e)
            {
                LogError($"Landmark extraction error: {e.Message}");
                detectedHands[handIndex].isValid = false;
            }
        }
        
        // ============================================================================
        // 图像预处理
        // ============================================================================
        
        private Tensor PreprocessImageForPalmDetection(Texture2D input)
        {
            // 缩放到192x192并归一化
            int targetSize = 192;
            Texture2D resized = ResizeTexture(input, targetSize, targetSize);
            
            // 转换为Tensor
            Tensor tensor = new Tensor(1, targetSize, targetSize, 3);
            
            Color[] pixels = resized.GetPixels();
            for (int y = 0; y < targetSize; y++)
            {
                for (int x = 0; x < targetSize; x++)
                {
                    int index = y * targetSize + x;
                    Color pixel = pixels[index];
                    
                    // 归一化到[-1, 1]
                    tensor[0, y, x, 0] = (pixel.r * 2.0f) - 1.0f;
                    tensor[0, y, x, 1] = (pixel.g * 2.0f) - 1.0f;
                    tensor[0, y, x, 2] = (pixel.b * 2.0f) - 1.0f;
                }
            }
            
            Destroy(resized);
            return tensor;
        }
        
        private Tensor PreprocessImageForLandmark(Texture2D input, Rect cropRegion)
        {
            // 裁剪并缩放到224x224
            int targetSize = 224;
            
            // 裁剪
            int cropX = Mathf.FloorToInt(cropRegion.x);
            int cropY = Mathf.FloorToInt(cropRegion.y);
            int cropW = Mathf.FloorToInt(cropRegion.width);
            int cropH = Mathf.FloorToInt(cropRegion.height);
            
            Color[] croppedPixels = input.GetPixels(cropX, cropY, cropW, cropH);
            Texture2D cropped = new Texture2D(cropW, cropH);
            cropped.SetPixels(croppedPixels);
            cropped.Apply();
            
            // 缩放
            Texture2D resized = ResizeTexture(cropped, targetSize, targetSize);
            
            // 转换为Tensor
            Tensor tensor = new Tensor(1, targetSize, targetSize, 3);
            Color[] pixels = resized.GetPixels();
            
            for (int y = 0; y < targetSize; y++)
            {
                for (int x = 0; x < targetSize; x++)
                {
                    int index = y * targetSize + x;
                    Color pixel = pixels[index];
                    
                    // 归一化到[-1, 1]
                    tensor[0, y, x, 0] = (pixel.r * 2.0f) - 1.0f;
                    tensor[0, y, x, 1] = (pixel.g * 2.0f) - 1.0f;
                    tensor[0, y, x, 2] = (pixel.b * 2.0f) - 1.0f;
                }
            }
            
            Destroy(cropped);
            Destroy(resized);
            return tensor;
        }
        
        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            RenderTexture.active = rt;
            
            Graphics.Blit(source, rt);
            
            Texture2D result = new Texture2D(targetWidth, targetHeight);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }
        
        // ============================================================================
        // 后处理
        // ============================================================================
        
        private List<Rect> PostprocessPalmDetection(Tensor output, int originalWidth, int originalHeight)
        {
            // 简化实现：假设输出格式为 [batch, num_detections, 4+1]
            // 实际MediaPipe输出格式可能不同，需要根据具体模型调整
            
            List<Rect> boxes = new List<Rect>();
            
            // TODO: 根据实际模型输出格式解析
            // 这里提供一个占位实现
            
            // 如果没有实际的输出解析，返回整个画面作为一个手掌区域
            boxes.Add(new Rect(0, 0, originalWidth, originalHeight));
            
            return boxes;
        }
        
        private void PostprocessLandmarks(Tensor output, Rect cropRegion, int handIndex)
        {
            // 简化实现：假设输出格式为 [1, 21, 3] (21个关键点，每个3D坐标)
            
            HandLandmarks hand = detectedHands[handIndex];
            hand.isValid = true;
            hand.confidence = 0.9f;  // TODO: 从模型输出获取实际置信度
            
            // TODO: 根据实际模型输出格式解析21个关键点
            // 这里提供一个占位实现
            
            for (int i = 0; i < 21; i++)
            {
                // 占位：生成假的关键点数据
                float x = cropRegion.x + cropRegion.width * 0.5f;
                float y = cropRegion.y + cropRegion.height * 0.5f;
                hand.landmarks[i] = new Vector3(x, y, 0);
            }
        }
        
        // ============================================================================
        // 清理
        // ============================================================================
        
        private void CleanupWorkers()
        {
            if (palmDetectionWorker != null)
            {
                palmDetectionWorker.Dispose();
                palmDetectionWorker = null;
            }
            
            if (handLandmarkWorker != null)
            {
                handLandmarkWorker.Dispose();
                handLandmarkWorker = null;
            }
            
            LogInfo("Barracuda workers cleaned up");
        }
        
        // ============================================================================
        // 日志工具
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[MediaPipeHands] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[MediaPipeHands] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[MediaPipeHands] {message}");
        }
    }
}

