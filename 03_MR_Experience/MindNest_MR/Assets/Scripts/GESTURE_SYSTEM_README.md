# 手势识别利他疗愈系统 - 使用说明

## 系统概述

本系统将原有的简单点击交互升级为基于摄像头的手势识别交互，支持6种不同的手势，为用户提供更丰富的疗愈体验。

## 新增文件

### 核心组件

1. **GestureData.cs** - 手势数据结构定义
   - `GestureType` 枚举：定义6种手势类型
   - `HandData` 类：手部检测数据
   - `GestureEvent` 类：手势事件数据
   - `GestureConfig` 类：手势识别配置参数

2. **HandDetectionManager.cs** - 手部检测管理器
   - 管理WebCamTexture摄像头
   - 实时检测画面中的手部区域
   - 基于运动和肤色的简化检测算法
   - 支持双手检测

3. **GestureRecognizer.cs** - 手势识别器
   - 识别6种手势：抚摸、戳戳、投喂、抱抱、挥手、比心
   - 基于手部轨迹和运动模式的规则识别
   - 状态机管理手势追踪
   - 触发手势识别事件

4. **GesturePromptUI.cs** - 手势提示UI控制器
   - 显示当前需要做的手势
   - 摄像头预览窗口
   - 手势识别进度显示
   - 成功/失败视觉反馈

### 修改文件

5. **AltruisticHealingController.cs** - 集成手势识别
   - 移除原有的鼠标点击检测
   - 集成手势识别系统
   - 根据不同手势给予不同奖励
   - 随机提示用户下一个手势

6. **MRUIManager.cs** - 添加手势UI引用
   - 添加手势UI元素的公共引用

7. **MindNestAutoBuilder.cs** - 初始化手势系统
   - 在场景初始化时自动配置手势系统
   - 连接所有组件依赖

## 手势说明

### 支持的6种手势

| 手势 | 描述 | 识别规则 | 奖励养料 | Nomi表情 |
|------|------|----------|----------|----------|
| **抚摸** (Stroke) | 手在Nomi附近缓慢水平移动 | 在交互半径内，速度0.5-3单位/帧，持续0.8秒 | +10 | happy.png |
| **戳戳** (Poke) | 手快速靠近Nomi后退回 | 速度>8单位/帧，快速接近后退回>50px | +10 | surprise.png |
| **投喂** (Feed) | 手从下往上移动到Nomi | 从Nomi下方150px向上移动到交互半径内 | +15 | eating.png |
| **抱抱** (Hug) | 双手从两侧靠近Nomi | 双手分别在Nomi左右，同时接近<120px | +20 | love.png |
| **挥手** (Wave) | 手在Nomi附近左右摆动 | 速度>4单位/帧，3次以上方向变化 | +5 | welcome.png |
| **比心** (Heart) | 双手在Nomi上方靠近 | 双手距离20-80px，在同一高度（±50px） | +20 | celebrate.png |

### 手势完成流程

1. 进入利他疗愈模式
2. 系统启动摄像头并显示预览
3. 随机提示一个手势（如"请做出 [抚摸] 手势"）
4. 用户对着摄像头做出相应手势
5. 系统识别成功后：
   - Nomi表情变化
   - 显示成功反馈和奖励
   - 更新进度
6. 提示下一个手势
7. 完成5个手势后结束，获得总养料奖励

## 使用方法

### 在Unity中运行

1. **打开场景**
   - 打开 `Assets/Scenes/SampleScene.unity`

2. **确保摄像头权限**
   - Windows: 系统会自动请求摄像头权限
   - 第一次运行时允许访问摄像头

3. **进入游戏**
   - 点击Play按钮
   - 等待场景自动构建完成
   - 按照Welcome流程进行
   - 在主菜单选择"利他疗愈"

4. **体验手势识别**
   - 左上角会显示摄像头预览
   - 屏幕中央显示当前需要做的手势
   - 按提示做出相应手势
   - 观察Nomi的反应

### 配置参数

可以在以下位置调整配置：

#### HandDetectionManager.cs
```csharp
public GestureConfig config = new GestureConfig();
```

参数说明：
- `cameraWidth/Height`: 摄像头分辨率 (默认640x480)
- `targetFPS`: 处理帧率 (默认20FPS)
- `minHandArea/maxHandArea`: 手部区域面积阈值
- `motionThreshold`: 运动检测阈值

#### GestureRecognizer.cs
手势识别阈值都定义在`GestureConfig`中，可以根据实际情况调整：
- `strokeMinDuration`: 抚摸最小持续时间
- `pokeSpeedThreshold`: 戳戳速度阈值
- `feedMinVerticalDist`: 投喂最小垂直距离
- 等等...

#### AltruisticHealingController.cs
```csharp
public int requiredGestures = 5;  // 需要完成的手势数量
public GestureType[] availableGestures;  // 可选的手势类型
```

## 性能优化建议

### 已实现的优化

1. **降采样处理**
   - 摄像头设置为640x480分辨率
   - 手部检测时跳跃采样（每8像素）
   - 处理频率限制在20FPS

2. **协程异步处理**
   - 摄像头初始化使用协程
   - 避免阻塞主线程

3. **轨迹长度限制**
   - 最多保留100个轨迹点
   - 自动清理旧数据

4. **手部丢失超时**
   - 0.5秒未检测到手部则标记为丢失
   - 避免无效计算

### 进一步优化建议

1. **光照条件**
   - 确保环境光线充足
   - 避免强背光

2. **背景简化**
   - 纯色或简单背景效果更好
   - 避免复杂图案干扰

3. **手势调试**
   - 启用`enableDebugLog = true`查看识别日志
   - 调整配置参数以适应不同用户

## 调试工具

### 启用调试日志

在各个组件的Inspector中设置：
```csharp
enableDebugLog = true  // 启用详细日志
```

### 查看识别状态

日志输出示例：
```
[HandDetector] Camera initialized: 640x480
[GestureRecognizer] Gesture recognized: 抚摸 (confidence: 0.85)
[GesturePromptUI] Prompt updated: 戳戳
```

### 常见问题排查

**问题1: 摄像头无法启动**
- 检查摄像头权限
- 确认没有其他程序占用摄像头
- 查看Console错误信息

**问题2: 手势识别不准确**
- 调整光线条件
- 简化背景
- 降低识别阈值（修改GestureConfig）
- 动作更明显一些

**问题3: 帧率过低**
- 降低摄像头分辨率
- 减少targetFPS
- 增大采样跳跃步长

**问题4: 双手手势难以识别**
- 确保两只手都在镜头内
- 手部距离适中（不要太近或太远）
- 背景对比度要好

## 扩展建议

### 升级到ML模型

如果简化的手部检测效果不理想，可以考虑：

1. **集成MediaPipe Hands**
   - 下载预训练的ONNX模型
   - 使用Unity Barracuda进行推理
   - 获得21个手部关键点
   - 更准确的手势识别

2. **添加更多手势**
   - 在`GestureType`枚举中添加新类型
   - 在`GestureRecognizer`中实现识别逻辑
   - 添加对应的表情和奖励

3. **手势动画**
   - 显示手势动画教学
   - 实时显示手部骨架
   - 手势完成动画效果

### 混合输入模式

保留鼠标点击作为备用：
```csharp
public bool enableFallbackClick = true;  // 启用点击后备方案
```

## 文件结构

```
Assets/
├── Scripts/
│   ├── GestureData.cs                      (NEW)
│   ├── HandDetectionManager.cs             (NEW)
│   ├── GestureRecognizer.cs                (NEW)
│   ├── GesturePromptUI.cs                  (NEW)
│   ├── AltruisticHealingController.cs      (MODIFIED)
│   ├── MRUIManager.cs                      (MODIFIED)
│   ├── MindNestAutoBuilder.cs              (MODIFIED)
│   └── GESTURE_SYSTEM_README.md            (NEW - 本文件)
│
├── Resources/
│   ├── GestureIcons/                       (NEW)
│   │   ├── README.txt
│   │   ├── stroke.png                      (待添加)
│   │   ├── poke.png                        (待添加)
│   │   ├── feed.png                        (待添加)
│   │   ├── hug.png                         (待添加)
│   │   ├── wave.png                        (待添加)
│   │   └── heart.png                       (待添加)
│   │
│   └── Expressions/                        (已存在)
│       ├── happy.png
│       ├── surprise.png
│       ├── eating.png
│       ├── love.png
│       ├── welcome.png
│       └── celebrate.png
```

## 总结

本手势识别系统为MindNest MR体验提供了更加自然和丰富的交互方式。通过识别6种不同的手势，用户可以以更多样化的方式与Nomi互动，获得更好的疗愈体验。

系统采用了简化但实用的计算机视觉算法，在保证性能的同时提供了较好的识别准确率。未来可以根据需要升级到更强大的ML模型来进一步提升体验。

---

**作者**: MindNest Team  
**日期**: 2026-01-29  
**版本**: 1.0.0

