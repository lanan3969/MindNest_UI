/*
 * NomiCustomizer.cs
 * =================
 * 
 * Nomi Customization System
 * 
 * Handles full customization of Nomi appearance and environment:
 * - Color tinting (5 preset colors)
 * - Accessories (4 types: candy hat, halo, scarf, bowtie)
 * - Scale adjustment (0.8x - 1.5x)
 * - Environment brightness
 * - Volume preference
 * - Theme color
 * 
 * Saves preferences to PlayerPrefs for persistence.
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MindNest.MR
{
    /// <summary>
    /// Manages Nomi customization
    /// </summary>
    public class NomiCustomizer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Main Nomi billboard")]
        public GameObject nomiBillboard;
        
        [Tooltip("Nomi material")]
        public Material nomiMaterial;
        
        [Tooltip("Directional light for brightness control")]
        public Light environmentLight;
        
        [Tooltip("UI Manager reference")]
        public MRUIManager uiManager;
        
        [Tooltip("State Manager reference")]
        public MRSceneStateManager stateManager;
        
        [Header("Customization Settings")]
        [Tooltip("Available Nomi colors")]
        public Color[] nomiColors = new Color[]
        {
            Color.white,
            new Color(0.5f, 0.5f, 0.5f),
            new Color(0.6f, 0.9f, 0.8f),
            new Color(0.7f, 0.8f, 1f),
            new Color(1f, 0.8f, 0.9f)
        };
        
        [Tooltip("Available theme colors")]
        public Color[] themeColors = new Color[]
        {
            Color.white,
            Color.gray,
            new Color(0.6f, 0.9f, 0.8f),
            new Color(0.7f, 0.8f, 1f),
            new Color(1f, 0.8f, 0.9f)
        };
        
        // ============================================================================
        // Current Customization State
        // ============================================================================
        
        private int currentNomiColorIndex = 0;
        private int currentThemeColorIndex = 0;
        private int currentAccessoryIndex = -1; // -1 means no accessory
        private float currentScale = 2.0f;
        private float currentBrightness = 1.0f;
        private float currentVolume = 0.5f;
        
        // ============================================================================
        // Accessory GameObjects
        // ============================================================================
        
        private GameObject[] accessories = new GameObject[4];
        
        // 保存配饰的初始偏移量（用于缩放时重新定位）
        private Vector3[] savedAccessoryOffsets = {
            new Vector3(0.035f, 0.33f, -0.01f),    // Hat - 帽子在头顶
            new Vector3(-0.075f, 0.45f, -0.01f),   // Halo - 光圈在头顶上方
            new Vector3(0, -0.15f, -0.01f),        // Bow - 领结在脖子位置
            new Vector3(1.225f, 0.34f, 0.01f)        // Cape - 斗篷（往左偏移）
        };
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            Debug.Log("🎨 NomiCustomizer: Initializing");
            
            // Load saved preferences
            LoadPreferences();
            
            // Apply loaded customization
            ApplyAllCustomization();
            
            // Setup UI button listeners
            SetupUIListeners();
            
            Debug.Log("✅ NomiCustomizer: Ready");
        }
        
        // ============================================================================
        // UI Setup
        // ============================================================================
        
        private void SetupUIListeners()
        {
            if (uiManager == null) return;
            
            // Brightness slider
            if (uiManager.brightnessSlider != null)
            {
                uiManager.brightnessSlider.value = currentBrightness;
                uiManager.brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            }
            
            // Volume slider
            if (uiManager.volumeSlider != null)
            {
                uiManager.volumeSlider.value = currentVolume;
                uiManager.volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            
            // Scale slider
            if (uiManager.scaleSlider != null)
            {
                uiManager.scaleSlider.value = currentScale;
                uiManager.scaleSlider.onValueChanged.AddListener(OnScaleChanged);
            }
            
            // Theme color buttons
            for (int i = 0; i < uiManager.themeColorButtons.Length && i < themeColors.Length; i++)
            {
                int index = i; // Capture for closure
                if (uiManager.themeColorButtons[i] != null)
                {
                    uiManager.themeColorButtons[i].onClick.AddListener(() => OnThemeColorSelected(index));
                }
            }
            
            // Nomi color buttons
            for (int i = 0; i < uiManager.nomiColorButtons.Length && i < nomiColors.Length; i++)
            {
                int index = i;
                if (uiManager.nomiColorButtons[i] != null)
                {
                    uiManager.nomiColorButtons[i].onClick.AddListener(() => OnNomiColorSelected(index));
                }
            }
            
            // Accessory buttons
            for (int i = 0; i < uiManager.accessoryButtons.Length; i++)
            {
                int index = i;
                if (uiManager.accessoryButtons[i] != null)
                {
                    uiManager.accessoryButtons[i].onClick.AddListener(() => OnAccessorySelected(index));
                }
            }
            
            // Finish button
            if (uiManager.finishCustomizationButton != null)
            {
                uiManager.finishCustomizationButton.onClick.AddListener(OnFinishCustomization);
            }
        }
        
        // ============================================================================
        // UI Callbacks
        // ============================================================================
        
        private void OnBrightnessChanged(float value)
        {
            currentBrightness = value;
            ApplyBrightness();
            Debug.Log($"💡 Brightness changed to: {value}");
        }
        
        private void OnVolumeChanged(float value)
        {
            currentVolume = value;
            Debug.Log($"🔊 Volume changed to: {value}");
        }
        
        private void OnScaleChanged(float value)
        {
            currentScale = value;
            ApplyScale();
            Debug.Log($"📏 Scale changed to: {value}");
        }
        
        private void OnThemeColorSelected(int colorIndex)
        {
            currentThemeColorIndex = colorIndex;
            ApplyThemeColor();
            Debug.Log($"🎨 Theme color changed to: {themeColors[colorIndex]}");
        }
        
        private void OnNomiColorSelected(int colorIndex)
        {
            currentNomiColorIndex = colorIndex;
            ApplyNomiColor();
            Debug.Log($"👻 Nomi color changed to: {nomiColors[colorIndex]}");
        }
        
        private void OnAccessorySelected(int accessoryIndex)
        {
            if (currentAccessoryIndex == accessoryIndex)
            {
                // Toggle off
                currentAccessoryIndex = -1;
            }
            else
            {
                currentAccessoryIndex = accessoryIndex;
            }
            ApplyAccessory();
            Debug.Log($"🎩 Accessory changed to: {accessoryIndex}");
        }
        
        private void OnFinishCustomization()
        {
            Debug.Log("✅ 形象设置完成，保存配置");
            SavePreferences();
            
            // === 新增：首次运行检测 ===
            bool isFirstRun = PlayerPrefs.GetInt("FirstRun_Completed", 0) == 0;
            
            if (isFirstRun)
            {
                // 首次运行：标记完成并强制跳转到聊天
                PlayerPrefs.SetInt("FirstRun_Completed", 1);
                PlayerPrefs.Save();
                
                Debug.Log("🎉 首次运行：跳转到聊天界面");
                
                if (MRSceneStateManager.Instance != null)
                {
                    MRSceneStateManager.Instance.TransitionToState(MRSceneState.ConnectionConfirm);
                }
            }
            else
            {
                // 非首次运行：返回主界面
                Debug.Log("🔄 返回主界面");
                
                if (MRSceneStateManager.Instance != null)
                {
                    MRSceneStateManager.Instance.TransitionToState(MRSceneState.MainMenu);
                }
            }
        }
        
        // ============================================================================
        // Apply Customization
        // ============================================================================
        
        private void ApplyAllCustomization()
        {
            ApplyNomiColor();
            ApplyScale();
            ApplyBrightness();
            ApplyThemeColor();
            ApplyAccessory();
        }
        
        private void ApplyNomiColor()
        {
            if (nomiBillboard == null)
            {
                Debug.LogWarning("⚠️ nomiBillboard is null");
                return;
            }
            
            Renderer renderer = nomiBillboard.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null)
            {
                Debug.LogWarning("⚠️ Nomi renderer or material not found");
                return;
            }
            
            Color selectedColor = nomiColors[currentNomiColorIndex];
            
            // 对于透明PNG纹理，material.color作为色调（tint）
            // 保持原有的alpha值
            Color currentColor = renderer.material.color;
            selectedColor.a = currentColor.a; // 保持当前透明度
            
            // 应用颜色到材质
            renderer.material.color = selectedColor;
            
            // 同步到nomiMaterial引用（如果存在）
            if (nomiMaterial != null)
            {
                nomiMaterial.color = selectedColor;
            }
            
            Debug.Log($"🎨 Nomi color tint changed to: {selectedColor} (PNG will be tinted with this color)");
        }
        
        private void ApplyScale()
        {
            if (nomiBillboard == null)
            {
                Debug.LogError("❌ nomiBillboard is null! Cannot apply scale.");
                return;
            }
            
            Vector3 baseScale = new Vector3(2f, 2f, 1f);
            Vector3 targetScale = baseScale * currentScale;
            
            // 设置transform的localScale
            nomiBillboard.transform.localScale = targetScale;
            
            // 通知BillboardBehavior更新其initialScale
            BillboardBehavior billboard = nomiBillboard.GetComponent<BillboardBehavior>();
            if (billboard != null)
            {
                billboard.UpdateBaseScale(targetScale);
            }
            else
            {
                Debug.LogWarning("⚠️ BillboardBehavior not found on nomiBillboard!");
            }
            
            // 同时更新所有配饰的BillboardBehavior
            UpdateAccessoriesScale();
            
            Debug.Log($"📏 Scale applied: base={baseScale}, multiplier={currentScale}, final={targetScale}");
        }
        
        /// <summary>
        /// 更新配饰的基准缩放（当Nomi缩放改变时）
        /// </summary>
        private void UpdateAccessoriesScale()
        {
            for (int i = 0; i < accessories.Length; i++)
            {
                if (accessories[i] != null && accessories[i].activeSelf)
                {
                    // 🔧 重新设置配饰的 localPosition（确保位置随缩放正确对齐）
                    accessories[i].transform.localPosition = savedAccessoryOffsets[i];
                    
                    BillboardBehavior accBillboard = accessories[i].GetComponent<BillboardBehavior>();
                    if (accBillboard != null)
                    {
                        // 配饰使用localScale，会自动随父对象缩放
                        // 但需要更新BillboardBehavior的initialScale
                        accBillboard.UpdateBaseScale(accessories[i].transform.localScale);
                    }
                }
            }
            
            Debug.Log("📌 Accessories positions updated for new Nomi scale");
        }
        
        private void ApplyBrightness()
        {
            // 1. 调整环境光
            if (environmentLight == null)
            {
                // Try to find directional light
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        environmentLight = light;
                        break;
                    }
                }
            }
            
            if (environmentLight != null)
            {
                environmentLight.intensity = currentBrightness * 2f; // Scale up for visibility
            }
            
            // 2. 调整Nomi材质的亮度
            if (nomiBillboard != null)
            {
                Renderer renderer = nomiBillboard.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Color currentColor = renderer.material.color;
                    
                    // 使用HSV颜色空间调整亮度，保持色相和饱和度
                    Color.RGBToHSV(currentColor, out float h, out float s, out float v);
                    Color newColor = Color.HSVToRGB(h, s, currentBrightness);
                    newColor.a = currentColor.a; // 保持原有透明度
                    
                    renderer.material.color = newColor;
                    
                    // 同步到nomiMaterial引用
                    if (nomiMaterial != null)
                    {
                        nomiMaterial.color = newColor;
                    }
                    
                    Debug.Log($"💡 Brightness applied: {currentBrightness}, Nomi color: {newColor}");
                }
            }
        }
        
        private void ApplyThemeColor()
        {
            if (uiManager == null) return;
            
            Color themeColor = themeColors[currentThemeColorIndex];
            Color panelBgColor = new Color(themeColor.r * 0.3f, themeColor.g * 0.3f, themeColor.b * 0.3f, 0.8f);
            
            // 计算文字颜色（亮色主题用黑字，暗色主题用白字）
            float luminance = 0.299f * themeColor.r + 0.587f * themeColor.g + 0.114f * themeColor.b;
            Color textColor = luminance > 0.6f ? Color.black : Color.white;
            
            // 更新UI Manager的配置
            uiManager.panelBackgroundColor = panelBgColor;
            uiManager.buttonColor = themeColor;
            
            // 遍历所有面板并更新颜色
            GameObject[] panels = {
                uiManager.customizationPanel,
                uiManager.connectionConfirmPanel,
                uiManager.mainMenuPanel,
                uiManager.breathingPanel,
                uiManager.altruisticPanel,
                uiManager.treeControlPanel,
                uiManager.historyPanel
            };
            
            foreach (var panel in panels)
            {
                if (panel != null)
                {
                    Image panelImage = panel.GetComponent<Image>();
                    if (panelImage != null)
                    {
                        panelImage.color = panelBgColor;
                    }
                    
                    // 更新该面板下的所有按钮
                    Button[] buttons = panel.GetComponentsInChildren<Button>(true);
                    foreach (Button btn in buttons)
                    {
                        if (!IsColorButton(btn))
                        {
                            // 更新按钮背景色
                            Image btnImage = btn.GetComponent<Image>();
                            if (btnImage != null)
                            {
                                btnImage.color = themeColor;
                            }
                            
                            // 更新按钮文字颜色
                            Text btnText = btn.GetComponentInChildren<Text>();
                            if (btnText != null)
                            {
                                btnText.color = textColor;
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"🎨 Theme color applied: {themeColor}, text color: {textColor}");
        }
        
        private bool IsColorButton(Button btn)
        {
            // 检查是否是颜色选择按钮（这些按钮应保持原色）
            return btn.name.Contains("ThemeColor") || btn.name.Contains("NomiColor");
        }
        
        private void ApplyAccessory()
        {
            if (nomiBillboard == null) return;
            
            // Hide all accessories first
            for (int i = 0; i < accessories.Length; i++)
            {
                if (accessories[i] != null)
                {
                    accessories[i].SetActive(false);
                }
            }
            
            // Show selected accessory
            if (currentAccessoryIndex >= 0 && currentAccessoryIndex < accessories.Length)
            {
                if (accessories[currentAccessoryIndex] == null)
                {
                    CreateAccessory(currentAccessoryIndex);
                }
                
                if (accessories[currentAccessoryIndex] != null)
                {
                    accessories[currentAccessoryIndex].SetActive(true);
                    
                    // 🔧 创建配饰后立即更新其位置和缩放
                    UpdateAccessoriesScale();
                }
            }
        }
        
        // ============================================================================
        // Accessory Creation
        // ============================================================================
        
        private void CreateAccessory(int accessoryIndex)
        {
            if (nomiBillboard == null) return;
            
            // Accessory file names (matching your PNG files)
            string[] accessoryNames = { "圣诞帽", "光圈", "领结", "斗篷" };
            
            // 配饰偏移使用保存的初始值（确保与 savedAccessoryOffsets 一致）
            Vector3[] accessoryOffsets = savedAccessoryOffsets;
            
            // 配饰缩放（相对于Nomi的localScale）- 调整后更协调
            Vector3[] accessoryScales = {
                new Vector3(1f, 1f, 1),   // Hat（稍微缩小）
                new Vector3(1f, 1f, 1),   // Halo（稍微缩小）
                new Vector3(1f, 1f, 1),   // Bow（缩小）
                new Vector3(9f, 4f, 1)      // Cape（调整比例）
            };
            
            if (accessoryIndex < 0 || accessoryIndex >= accessoryNames.Length) return;
            
            // Load accessory texture from Resources/Accessories/
            string accessoryName = accessoryNames[accessoryIndex];
            Texture2D texture = Resources.Load<Texture2D>($"Accessories/{accessoryName}");
            
            if (texture == null)
            {
                Debug.LogWarning($"⚠️ Accessory texture not found: Accessories/{accessoryName}.png");
                return;
            }
            
            // Create accessory quad
            GameObject accessory = GameObject.CreatePrimitive(PrimitiveType.Quad);
            accessory.name = $"Accessory_{accessoryName}";
            accessory.transform.SetParent(nomiBillboard.transform);
            accessory.transform.localPosition = accessoryOffsets[accessoryIndex];
            accessory.transform.localRotation = Quaternion.identity;
            accessory.transform.localScale = accessoryScales[accessoryIndex];
            
            // Apply texture with transparency
            Renderer renderer = accessory.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.mainTexture = texture;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.color = new Color(1f, 1f, 1f, 1f);
            renderer.material = mat;
            
            // Add Billboard behavior so accessory always faces camera (like Nomi)
            BillboardBehavior billboard = accessory.AddComponent<BillboardBehavior>();
            billboard.enableFloatingAnimation = false; // Accessories shouldn't float independently
            billboard.breatheScale = 0f; // 🔧 禁用呼吸缩放效果，配饰大小跟随Nomi
            
            // 立即更新initialScale（在AddComponent之后、下一帧Start()之前）
            // 使用延迟调用确保BillboardBehavior.Start()已执行
            StartCoroutine(UpdateAccessoryScaleDelayed(billboard, accessory));
            
            // Store reference
            accessories[accessoryIndex] = accessory;
            
            Debug.Log($"✨ Created accessory: {accessoryName}");
        }
        
        /// <summary>
        /// 延迟更新配饰的BillboardBehavior scale
        /// </summary>
        private IEnumerator UpdateAccessoryScaleDelayed(BillboardBehavior billboard, GameObject accessory)
        {
            yield return null; // 等待下一帧，确保Start()已执行
            billboard.UpdateBaseScale(accessory.transform.localScale);
        }
        
        // ============================================================================
        // Persistence
        // ============================================================================
        
        private void SavePreferences()
        {
            PlayerPrefs.SetInt("Nomi_ColorIndex", currentNomiColorIndex);
            PlayerPrefs.SetInt("Theme_ColorIndex", currentThemeColorIndex);
            PlayerPrefs.SetInt("Accessory_Index", currentAccessoryIndex);
            PlayerPrefs.SetFloat("Nomi_Scale", currentScale);
            PlayerPrefs.SetFloat("Env_Brightness", currentBrightness);
            PlayerPrefs.SetFloat("Volume", currentVolume);
            PlayerPrefs.Save();
            
            Debug.Log("💾 Customization preferences saved");
        }
        
        private void LoadPreferences()
        {
            currentNomiColorIndex = PlayerPrefs.GetInt("Nomi_ColorIndex", 0);
            currentThemeColorIndex = PlayerPrefs.GetInt("Theme_ColorIndex", 0);
            currentAccessoryIndex = PlayerPrefs.GetInt("Accessory_Index", -1);
            currentScale = PlayerPrefs.GetFloat("Nomi_Scale", 2.0f);
            currentBrightness = PlayerPrefs.GetFloat("Env_Brightness", 1.0f);
            currentVolume = PlayerPrefs.GetFloat("Volume", 0.5f);
            
            Debug.Log("📂 Customization preferences loaded");
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Show Nomi billboard
        /// </summary>
        public void ShowNomi()
        {
            if (nomiBillboard != null)
            {
                nomiBillboard.SetActive(true);
            }
        }
        
        /// <summary>
        /// Hide Nomi billboard
        /// </summary>
        public void HideNomi()
        {
            if (nomiBillboard != null)
            {
                nomiBillboard.SetActive(false);
            }
        }
    }
}

