# MediaPipe Hands ONNX 模型文件

## 需要的模型文件

本文件夹需要放置以下ONNX模型文件：

1. **palm_detection.onnx** (~1.5MB) - 手掌检测模型
2. **hand_landmark.onnx** (~4.3MB) - 手部21个关键点检测模型

## 获取方法

### 方法1：从预转换的ONNX模型库下载（推荐）

访问以下链接下载预转换的ONNX模型：

- [MediaPipe Hand Landmarker ONNX](https://github.com/google/mediapipe/tree/master/mediapipe/modules/hand_landmark)
- [ONNX Model Zoo - Hand Detection](https://github.com/onnx/models)

### 方法2：从TFLite转换

如果找不到预转换的ONNX模型，可以从TFLite模型转换：

```bash
# 1. 安装转换工具
pip install tf2onnx tensorflow

# 2. 下载MediaPipe TFLite模型
wget https://storage.googleapis.com/mediapipe-models/hand_landmarker/hand_landmarker/float16/latest/hand_landmarker.task

# 3. 解压task文件得到tflite模型
unzip hand_landmarker.task

# 4. 转换为ONNX
python -m tf2onnx.convert \
    --tflite palm_detection.tflite \
    --output palm_detection.onnx \
    --opset 13

python -m tf2onnx.convert \
    --tflite hand_landmark.tflite \
    --output hand_landmark.onnx \
    --opset 13
```

### 方法3：使用简化方案（无需ONNX模型）

如果无法获取ONNX模型，系统会自动回退到简化的手部检测方案。

在 `HandDetectionManager.cs` 中设置：
```csharp
public bool useMediaPipe = false;  // 使用简化检测
```

## 模型规格

### palm_detection.onnx
- **输入**: RGB图像 (192x192x3)
- **输出**: 手掌边界框和置信度
- **格式**: ONNX Opset 13+

### hand_landmark.onnx
- **输入**: RGB图像 (224x224x3)
- **输出**: 21个手部关键点 (x, y, z坐标)
- **格式**: ONNX Opset 13+

## 21个关键点定义

```
0: WRIST (手腕)
1-4: THUMB (拇指根-拇指尖)
5-8: INDEX (食指根-食指尖)
9-12: MIDDLE (中指根-中指尖)
13-16: RING (无名指根-无名指尖)
17-20: PINKY (小指根-小指尖)
```

## 使用说明

1. 将下载的ONNX模型文件放在本文件夹
2. 确保文件名正确：`palm_detection.onnx` 和 `hand_landmark.onnx`
3. 在Unity Editor中刷新Assets
4. 运行项目，系统会自动加载模型

## 故障排除

### 问题1：模型加载失败
- 检查文件名是否正确
- 检查文件是否损坏
- 查看Unity Console错误信息

### 问题2：推理速度慢
- 降低摄像头分辨率
- 减少处理帧率
- 使用量化模型（float16）

### 问题3：找不到模型文件
- 系统会自动回退到简化检测方案
- 查看日志确认回退状态

## 许可证

MediaPipe模型遵循Apache 2.0许可证。
使用前请确认您遵守相关许可证条款。

---

**注意**：如果您无法获取ONNX模型，可以继续使用系统的简化手部检测方案，它不需要任何模型文件。

