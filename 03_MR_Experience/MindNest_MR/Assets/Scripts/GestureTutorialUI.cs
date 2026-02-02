/*
 * GestureTutorialUI.cs
 * ====================
 * 
 * 手势教学UI控制器
 * 
 * 功能：
 * - 显示手势教学面板
 * - 展示手势动画/说明
 * - 用户可跟随练习
 * - 集成到疗愈流程
 * 
 * Author: MindNest Team
 * Date: 2026-01-29
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MindNest.MR
{
    /// <summary>
    /// 手势教学UI控制器
    /// </summary>
    public class GestureTutorialUI : MonoBehaviour
    {
        [Header("UI元素")]
        public GameObject tutorialPanel;
        public Text titleText;
        public Text descriptionText;
        public Image gestureAnimationImage;
        public Button skipButton;
        public Button tryButton;
        
        [Header("设置")]
        public float animationFPS = 10f;
        public bool showTutorialOnFirstTime = true;
        
        [Header("调试")]
        public bool enableDebugLog = true;
        
        // ============================================================================
        // 手势教学数据
        // ============================================================================
        
        private class GestureTutorialData
        {
            public GestureType gestureType;
            public string title;
            public string[] steps;
            public Sprite[] animationFrames;
        }
        
        private GestureTutorialData currentTutorial;
        private Coroutine animationCoroutine;
        
        // ============================================================================
        // 事件
        // ============================================================================
        
        public System.Action OnTutorialSkipped;
        public System.Action OnTutorialCompleted;
        
        // ============================================================================
        // Unity生命周期
        // ============================================================================
        
        void Start()
        {
            SetupUI();
            
            // 默认隐藏教学面板
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }
        
        // ============================================================================
        // 公共接口
        // ============================================================================
        
        /// <summary>
        /// 显示指定手势的教学
        /// </summary>
        public void ShowTutorial(GestureType gestureType)
        {
            // 检查是否需要显示教学
            if (!ShouldShowTutorial(gestureType))
            {
                LogInfo($"Tutorial for {gestureType} already shown, skipping");
                OnTutorialCompleted?.Invoke();
                return;
            }
            
            // 加载教学数据
            currentTutorial = LoadTutorialData(gestureType);
            
            if (currentTutorial == null)
            {
                LogWarning($"Tutorial data not found for {gestureType}");
                OnTutorialCompleted?.Invoke();
                return;
            }
            
            // 显示教学面板
            DisplayTutorial();
            
            // 标记已显示
            MarkTutorialAsShown(gestureType);
        }
        
        /// <summary>
        /// 隐藏教学面板
        /// </summary>
        public void HideTutorial()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
            
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
        }
        
        /// <summary>
        /// 创建教学UI
        /// </summary>
        public void CreateTutorialUI(Canvas parentCanvas)
        {
            if (parentCanvas == null)
            {
                LogError("Parent canvas is null");
                return;
            }
            
            LogInfo("Creating tutorial UI...");
            
            // 创建教学面板
            GameObject panel = new GameObject("Tutorial_Panel");
            panel.transform.SetParent(parentCanvas.transform, false);
            tutorialPanel = panel;
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // 半透明背景
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.8f);
            
            // 创建内容容器
            GameObject content = CreateContentContainer(panel.transform);
            
            // 创建标题
            CreateTitleText(content.transform);
            
            // 创建动画显示区域
            CreateAnimationDisplay(content.transform);
            
            // 创建说明文字
            CreateDescriptionText(content.transform);
            
            // 创建按钮
            CreateButtons(content.transform);
            
            LogInfo("Tutorial UI created successfully");
        }
        
        // ============================================================================
        // UI创建
        // ============================================================================
        
        private void SetupUI()
        {
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipButtonClicked);
            }
            
            if (tryButton != null)
            {
                tryButton.onClick.AddListener(OnTryButtonClicked);
            }
        }
        
        private GameObject CreateContentContainer(Transform parent)
        {
            GameObject content = new GameObject("Tutorial_Content");
            content.transform.SetParent(parent, false);
            
            RectTransform rect = content.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 700);
            rect.anchoredPosition = Vector2.zero;
            
            Image bg = content.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            
            return content;
        }
        
        private void CreateTitleText(Transform parent)
        {
            GameObject titleObj = new GameObject("Tutorial_Title");
            titleObj.transform.SetParent(parent, false);
            
            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(540, 60);
            rect.anchoredPosition = new Vector2(0, -20);
            
            titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "手势教学";
        }
        
        private void CreateAnimationDisplay(Transform parent)
        {
            GameObject animObj = new GameObject("Tutorial_Animation");
            animObj.transform.SetParent(parent, false);
            
            RectTransform rect = animObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.6f);
            rect.anchorMax = new Vector2(0.5f, 0.6f);
            rect.sizeDelta = new Vector2(300, 300);
            rect.anchoredPosition = Vector2.zero;
            
            gestureAnimationImage = animObj.AddComponent<Image>();
            gestureAnimationImage.color = Color.white;
        }
        
        private void CreateDescriptionText(Transform parent)
        {
            GameObject descObj = new GameObject("Tutorial_Description");
            descObj.transform.SetParent(parent, false);
            
            RectTransform rect = descObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.3f);
            rect.anchorMax = new Vector2(0.5f, 0.3f);
            rect.sizeDelta = new Vector2(540, 200);
            rect.anchoredPosition = Vector2.zero;
            
            descriptionText = descObj.AddComponent<Text>();
            descriptionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descriptionText.fontSize = 20;
            descriptionText.alignment = TextAnchor.UpperLeft;
            descriptionText.color = Color.white;
            descriptionText.text = "";
        }
        
        private void CreateButtons(Transform parent)
        {
            // 跳过按钮
            GameObject skipObj = CreateButton(parent, "跳过教程", new Vector2(-100, -300));
            skipButton = skipObj.GetComponent<Button>();
            skipButton.onClick.AddListener(OnSkipButtonClicked);
            
            // 开始练习按钮
            GameObject tryObj = CreateButton(parent, "开始练习", new Vector2(100, -300));
            tryButton = tryObj.GetComponent<Button>();
            tryButton.onClick.AddListener(OnTryButtonClicked);
        }
        
        private GameObject CreateButton(Transform parent, string text, Vector2 position)
        {
            GameObject btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent, false);
            
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(150, 50);
            rect.anchoredPosition = position;
            
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.4f, 0.6f, 0.9f);
            
            Button btn = btnObj.AddComponent<Button>();
            
            // 按钮文字
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            Text btnText = textObj.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 20;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = text;
            
            return btnObj;
        }
        
        // ============================================================================
        // 教学数据
        // ============================================================================
        
        private GestureTutorialData LoadTutorialData(GestureType gestureType)
        {
            GestureTutorialData data = new GestureTutorialData();
            data.gestureType = gestureType;
            data.title = $"如何做 [{GestureEvent.GetGestureDescription(gestureType)}] 手势";
            
            // 加载教学步骤
            data.steps = GetGestureSteps(gestureType);
            
            // 尝试加载动画帧（如果有的话）
            data.animationFrames = LoadAnimationFrames(gestureType);
            
            return data;
        }
        
        private string[] GetGestureSteps(GestureType gestureType)
        {
            switch (gestureType)
            {
                case GestureType.Stroke:
                    return new string[]
                    {
                        "1. 将手掌展开，五指自然分开",
                        "2. 手掌平行于屏幕",
                        "3. 在Nomi附近缓慢水平移动",
                        "4. 保持动作1-2秒"
                    };
                    
                case GestureType.Poke:
                    return new string[]
                    {
                        "1. 伸出食指，其他手指弯曲",
                        "2. 快速将食指指向Nomi",
                        "3. 快速回退手指",
                        "4. 整个动作要迅速完成"
                    };
                    
                case GestureType.Feed:
                    return new string[]
                    {
                        "1. 手掌向上，像捧着东西",
                        "2. 手从Nomi下方开始",
                        "3. 缓慢向上移动到Nomi位置",
                        "4. 保持手掌向上的姿势"
                    };
                    
                case GestureType.Hug:
                    return new string[]
                    {
                        "1. 伸出双手，手掌朝向Nomi",
                        "2. 双手分别在Nomi左右两侧",
                        "3. 同时向Nomi靠近",
                        "4. 像给Nomi一个拥抱"
                    };
                    
                case GestureType.Wave:
                    return new string[]
                    {
                        "1. 抬起手，手掌展开",
                        "2. 在Nomi附近左右摆动",
                        "3. 摆动3次以上",
                        "4. 动作要清晰明显"
                    };
                    
                case GestureType.Heart:
                    return new string[]
                    {
                        "1. 伸出双手到Nomi上方",
                        "2. 拇指和食指靠近",
                        "3. 两只手形成心形",
                        "4. 保持姿势1-2秒"
                    };
                    
                default:
                    return new string[] { "暂无教学步骤" };
            }
        }
        
        private Sprite[] LoadAnimationFrames(GestureType gestureType)
        {
            string gestureName = GestureEvent.GetGestureIconName(gestureType);
            string folderPath = $"GestureTutorials/{gestureName}_tutorial";
            
            // 尝试加载动画帧
            // 实际项目中，这里可以加载一系列图片
            // 现在返回null，表示没有动画
            
            return null;
        }
        
        // ============================================================================
        // 教学显示
        // ============================================================================
        
        private void DisplayTutorial()
        {
            if (tutorialPanel == null || currentTutorial == null)
                return;
            
            // 显示面板
            tutorialPanel.SetActive(true);
            
            // 设置标题
            if (titleText != null)
            {
                titleText.text = currentTutorial.title;
            }
            
            // 设置说明文字
            if (descriptionText != null)
            {
                descriptionText.text = string.Join("\n\n", currentTutorial.steps);
            }
            
            // 播放动画（如果有）
            if (currentTutorial.animationFrames != null && currentTutorial.animationFrames.Length > 0)
            {
                animationCoroutine = StartCoroutine(PlayAnimationLoop());
            }
            else
            {
                // 没有动画，显示占位图标
                if (gestureAnimationImage != null)
                {
                    string iconName = GestureEvent.GetGestureIconName(currentTutorial.gestureType);
                    Texture2D icon = Resources.Load<Texture2D>($"GestureIcons/{iconName}");
                    
                    if (icon != null)
                    {
                        gestureAnimationImage.sprite = Sprite.Create(
                            icon,
                            new Rect(0, 0, icon.width, icon.height),
                            new Vector2(0.5f, 0.5f)
                        );
                    }
                }
            }
        }
        
        private IEnumerator PlayAnimationLoop()
        {
            int frameIndex = 0;
            float frameDuration = 1f / animationFPS;
            
            while (true)
            {
                if (gestureAnimationImage != null && currentTutorial.animationFrames != null)
                {
                    gestureAnimationImage.sprite = currentTutorial.animationFrames[frameIndex];
                    frameIndex = (frameIndex + 1) % currentTutorial.animationFrames.Length;
                }
                
                yield return new WaitForSeconds(frameDuration);
            }
        }
        
        // ============================================================================
        // 按钮事件
        // ============================================================================
        
        private void OnSkipButtonClicked()
        {
            LogInfo("Tutorial skipped");
            HideTutorial();
            OnTutorialSkipped?.Invoke();
        }
        
        private void OnTryButtonClicked()
        {
            LogInfo("Tutorial completed, user ready to try");
            HideTutorial();
            OnTutorialCompleted?.Invoke();
        }
        
        // ============================================================================
        // 教学记录
        // ============================================================================
        
        private bool ShouldShowTutorial(GestureType gestureType)
        {
            if (!showTutorialOnFirstTime)
                return false;
            
            string key = $"Tutorial_Shown_{gestureType}";
            return !PlayerPrefs.HasKey(key);
        }
        
        private void MarkTutorialAsShown(GestureType gestureType)
        {
            string key = $"Tutorial_Shown_{gestureType}";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
        
        // ============================================================================
        // 日志工具
        // ============================================================================
        
        private void LogInfo(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[GestureTutorialUI] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[GestureTutorialUI] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GestureTutorialUI] {message}");
        }
    }
}

