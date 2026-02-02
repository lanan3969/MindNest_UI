# MindNest手势识别增强系统

## 系统概述

本增强系统为MindNest利他疗愈功能提供了三大核心增强：

1. **MediaPipe Hands集成** - 基于21个手部关键点的精确识别
2. **手势教学系统** - 用户首次遇到手势时的可视化教学
3. **鼠标点击备用方案** - 手势识别失败时的智能回退

## ✨ 新增功能

### 1. MediaPipe Hands精确识别

**文件**: `MediaPipeHandsManager.cs`, `LandmarkGestureRecognizer.cs`

**功能**:
- 使用ONNX模型进行手部关键点检测（21个关键点）
- 更高的识别准确率（目标>85%）
- 支持复杂手势识别（比心、抱抱等）

**使用方法**:
```csharp
// 在HandDetectionManager中启用MediaPipe
handDetector.useMediaPipe = true;
```

**要求**:
- Unity Barracuda包（已在manifest.json中配置）
- ONNX模型文件（放在`StreamingAssets/MediaPipeModels/`）
  - `palm_detection.onnx` (~1.5MB)
  - `hand_landmark.onnx` (~4.3MB)

**注意**: 如果没有ONNX模型，系统会自动回退到简化检测方案。

### 2. 手势教学系统

**文件**: `GestureTutorialUI.cs`

**功能**:
- 用户第一次遇到某个手势时自动显示教学
- 分步骤说明手势动作
- 可选的动画演示（序列帧）
- 记忆用户已学习的手势

**教学内容** (已内置):
- **抚摸**: 手掌展开，缓慢水平移动
- **戳戳**: 食指伸直，快速前后移动
- **投喂**: 手掌向上，从下往上移动
- **抱抱**: 双手从两侧靠近Nomi
- **挥手**: 手掌展开，左右摆动3次以上
- **比心**: 双手拇指和食指靠近形成心形

**自定义教学**:
将教学动画放在`Resources/GestureTutorials/{gesture}_tutorial/`文件夹。

### 3. 鼠标点击备用方案

**文件**: `InteractionModeManager.cs`, `AltruisticHealingController.cs`

**功能**:
- 4种交互模式：仅手势、仅点击、混合、智能回退
- 自动追踪手势识别成功率
- 连续失败3次后建议切换到点击模式
- 用户可随时手动切换模式

**交互模式**:
```csharp
public enum InteractionMode
{
    GestureOnly,      // 仅手势识别
    ClickOnly,        // 仅鼠标点击
    Hybrid,           // 两种都可用
    AutoFallback      // 智能回退（推荐）
}
```

**默认模式**: `AutoFallback` - 优先使用手势，失败后自动切换。

## 📁 文件结构

### 新增核心文件

```
Assets/Scripts/
├── MediaPipeHandsManager.cs          # MediaPipe模型管理器
├── LandmarkGestureRecognizer.cs      # 基于关键点的手势识别
├── InteractionModeManager.cs         # 交互模式管理
├── GestureTutorialUI.cs              # 手势教学UI
├── ENHANCED_GESTURE_SYSTEM_README.md # 本文档
└── (已修改的文件)
    ├── HandDetectionManager.cs       # 集成MediaPipe
    ├── GestureRecognizer.cs          # 支持关键点输入
    ├── GesturePromptUI.cs            # 添加模式切换UI
    ├── AltruisticHealingController.cs # 集成教学和点击
    └── MindNestAutoBuilder.cs        # 自动初始化增强系统
```

### 资源文件

```
Assets/StreamingAssets/MediaPipeModels/
├── README.md                         # 模型获取说明
├── palm_detection.onnx               # (需下载) 手掌检测模型
└── hand_landmark.onnx                # (需下载) 关键点检测模型

Assets/Resources/GestureTutorials/
├── README.md                         # 教学资源说明
└── {gesture}_tutorial/               # (可选) 手势教学动画
    ├── frame_0.png
    ├── frame_1.png
    └── description.txt
```

## 🚀 快速开始

### 基础使用（无需额外配置）

系统已默认配置为**简化检测+智能回退模式**，无需任何额外设置即可使用：

1. 运行场景
2. 进入利他疗愈模式
3. 系统会显示手势教学（首次）
4. 使用摄像头进行手势识别
5. 如果识别失败，会自动建议切换到点击模式

### 启用MediaPipe精确识别

**前提条件**:
1. 下载ONNX模型文件（参见`StreamingAssets/MediaPipeModels/README.md`）
2. 将模型放在`StreamingAssets/MediaPipeModels/`文件夹

**启用方法**:
```csharp
// 在运行时或Inspector中设置
handDetectionManager.useMediaPipe = true;
gestureRecognizer.useLandmarkRecognizer = true;
```

### 自定义教学动画

1. 创建序列帧图片（PNG格式，300x300推荐）
2. 放在`Resources/GestureTutorials/{gesture}_tutorial/`
3. 命名为`frame_0.png`, `frame_1.png`等
4. 系统会自动加载并循环播放

## 🎮 用户体验流程

### 手势识别流程（默认模式）

```
1. 用户进入利他疗愈模式
   ↓
2. 系统请求第一个手势（例如：抚摸）
   ↓
3. [首次] 显示手势教学面板
   - 显示手势图标
   - 显示分步骤说明
   - 用户可跳过或开始练习
   ↓
4. 摄像头开启，手部检测开始
   ↓
5. 用户做出手势
   ↓
6. 识别成功？
   - 是 → Nomi变开心，显示鼓励消息，请求下一个手势
   - 否 → 记录失败次数
   ↓
7. 连续失败3次？
   - 是 → 显示建议："建议切换到点击模式"
   - 否 → 继续等待手势
   ↓
8. 用户可随时点击模式切换按钮
   ↓
9. 完成5个手势 → 疗愈成功，获得养料
```

### 点击备用流程

```
1. 用户点击"切换模式"按钮
   ↓
2. 模式切换到"仅点击"
   ↓
3. 摄像头关闭，手势检测停止
   ↓
4. 提示文字变为"点击Nomi以安慰它"
   ↓
5. 用户点击Nomi
   ↓
6. Nomi变开心，计数增加
   ↓
7. 完成5次点击 → 疗愈成功（奖励较少）
```

## ⚙️ 配置参数

### HandDetectionManager

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `useMediaPipe` | bool | false | 是否启用MediaPipe |
| `config.cameraWidth` | int | 640 | 摄像头宽度 |
| `config.cameraHeight` | int | 480 | 摄像头高度 |
| `config.targetFPS` | int | 20 | 处理帧率 |

### InteractionModeManager

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `currentMode` | InteractionMode | AutoFallback | 当前交互模式 |
| `failureThreshold` | int | 3 | 连续失败阈值 |
| `fallbackCooldown` | float | 10f | 回退冷却时间（秒） |

### GestureTutorialUI

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `showTutorialOnFirstTime` | bool | true | 首次显示教学 |
| `animationFPS` | float | 10f | 动画播放帧率 |

### AltruisticHealingController

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `requiredGestures` | int | 5 | 所需手势数量 |
| `totalNutrientsReward` | int | 100 | 总养料奖励 |
| `gestureCooldown` | float | 2.0f | 手势间隔时间 |

## 📊 性能指标

### 目标性能

| 指标 | 目标值 | 当前状态 |
|------|--------|----------|
| MediaPipe推理时间 | <50ms | ⚠️ 需实际测试 |
| 整体帧率 | ≥20FPS | ✅ 已优化 |
| 内存占用 | <500MB | ✅ 满足要求 |
| 手势识别准确率 | >85% | ⚠️ 需用户测试 |

### 性能优化建议

1. **MediaPipe模式**:
   - 使用GPU加速（默认启用）
   - 降低摄像头分辨率到640x480
   - 使用量化模型（float16）

2. **简化模式**:
   - 调整`pixelSampleStep`以减少采样点
   - 降低`targetFPS`到15FPS
   - 减少轨迹记录长度

3. **通用优化**:
   - 禁用详细日志（`enableDebugLog = false`）
   - 减少UI更新频率
   - 使用对象池管理临时对象

## 🐛 故障排除

### 问题1: MediaPipe模型加载失败

**症状**: 控制台显示"Model not found"

**解决方案**:
1. 检查模型文件是否在`StreamingAssets/MediaPipeModels/`
2. 文件名是否正确（`palm_detection.onnx`, `hand_landmark.onnx`）
3. 如果没有模型，设置`useMediaPipe = false`使用简化模式

### 问题2: 摄像头无法启动

**症状**: 黑屏或无摄像头画面

**解决方案**:
1. 检查摄像头权限
2. 尝试重启Unity Editor
3. 检查`WebCamTexture.devices`是否检测到设备

### 问题3: 手势识别不准确

**症状**: 手势总是识别失败

**解决方案**:
1. 调整光线环境（避免过亮或过暗）
2. 调整手部检测的HSV颜色范围
3. 增加手势持续时间要求
4. 切换到点击模式作为备用

### 问题4: 教学面板不显示

**症状**: 首次没有显示教学

**解决方案**:
1. 检查`showTutorialOnFirstTime = true`
2. 清除PlayerPrefs: `PlayerPrefs.DeleteAll()`
3. 确保`GestureTutorialUI`组件已正确挂载

## 🔧 开发者指南

### 添加新手势

1. **在GestureType枚举中添加类型**:
```csharp
public enum GestureType
{
    // ... 现有手势
    MyNewGesture  // 新手势
}
```

2. **在GestureEvent中添加描述**:
```csharp
case GestureType.MyNewGesture:
    return "我的新手势";
```

3. **实现识别逻辑**:
   - 简化模式：在`GestureRecognizer.cs`中添加`RecognizeMyNewGesture()`
   - 关键点模式：在`LandmarkGestureRecognizer.cs`中添加

4. **添加教学说明**:
```csharp
// 在GestureTutorialUI.GetGestureSteps()中添加
case GestureType.MyNewGesture:
    return new string[]
    {
        "1. 第一步说明",
        "2. 第二步说明",
        // ...
    };
```

5. **添加手势图标**:
   将图标PNG放在`Resources/GestureIcons/mynewgesture.png`

### 扩展识别算法

**添加自定义关键点检测**:
```csharp
private bool RecognizeMyGesture(HandLandmarks hand)
{
    // 获取关键点
    Vector3 wrist = hand.GetWrist();
    Vector3 indexTip = hand.GetFingerTip(1);
    
    // 计算几何特征
    float distance = Vector3.Distance(wrist, indexTip);
    
    // 判断条件
    if (distance > threshold)
    {
        TriggerGesture(GestureType.MyNewGesture, indexTip, 0.9f);
        return true;
    }
    
    return false;
}
```

## 📝 API参考

### InteractionModeManager

```csharp
// 记录手势成功
interactionModeManager.RecordGestureSuccess();

// 记录手势失败
interactionModeManager.RecordGestureFailure();

// 切换模式
interactionModeManager.SwitchMode(InteractionMode.ClickOnly);

// 检查当前模式
bool gestureEnabled = interactionModeManager.IsGestureEnabled();
bool clickEnabled = interactionModeManager.IsClickEnabled();

// 获取成功率
float rate = interactionModeManager.GetGestureSuccessRate();
```

### GestureTutorialUI

```csharp
// 显示教学
gestureTutorialUI.ShowTutorial(GestureType.Stroke);

// 隐藏教学
gestureTutorialUI.HideTutorial();

// 监听事件
gestureTutorialUI.OnTutorialCompleted += () => {
    Debug.Log("用户完成教学");
};
```

### MediaPipeHandsManager

```csharp
// 处理帧
HandLandmarks[] hands = mediaPipeManager.ProcessFrame(frameTexture);

// 检查状态
bool isUsing = mediaPipeManager.IsUsingMediaPipe();
bool isInit = mediaPipeManager.IsInitialized();
```

## 🎯 未来改进方向

### P2优先级（中期）

- [ ] 完整的手势教学动画资源
- [ ] 手势识别准确率统计面板
- [ ] 更详细的错误提示和引导
- [ ] 多语言支持（教学文本）

### P3优先级（长期）

- [ ] 手部骨架可视化调试工具
- [ ] 自定义手势录制功能
- [ ] 机器学习模型在线更新
- [ ] VR/AR设备手势识别适配

## 📜 许可证和致谢

**Unity Barracuda**: Unity Technologies
**MediaPipe**: Google (Apache 2.0 License)
**MindNest Team**: 2026

---

## 📞 技术支持

如有问题，请查看：
1. Unity Console日志
2. `StreamingAssets/MediaPipeModels/README.md`
3. `Resources/GestureTutorials/README.md`
4. 本文档的故障排除部分

**最后更新**: 2026-01-29
**版本**: 1.0.0 Enhanced Gesture System

