/*
 * ParticleTreeSystem.cs
 * =====================
 * 
 * Unity-native particle tree system inspired by tree_final.html
 * 
 * Features:
 * - Dynamic growth based on nutrient level
 * - Procedural branch generation using LineRenderer
 * - Particle emission from branch endpoints with golden glow
 * - Multi-stage growth progression (Sapling -> Young -> Mature -> Ancient)
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using UnityEngine;
using System.Collections.Generic;

namespace MindNest.MR
{
    /// <summary>
    /// Particle-based tree system that grows with user's accumulated nutrients
    /// </summary>
    public class ParticleTreeSystem : MonoBehaviour
    {
        // ============================================================================
        // Configuration Parameters
        // ============================================================================
        
        [Header("Growth Configuration")]
        [Tooltip("Current nutrient level (drives all growth)")]
        [SerializeField] private int currentNutrients = 0;
        
        [Tooltip("Growth stage milestones")]
        public int[] growthMilestones = { 0, 300, 700, 1200 }; // Sapling, Young, Mature, Ancient
        
        [Header("Trunk Settings")]
        [Tooltip("Base trunk height")]
        public float baseTrunkHeight = 2.0f;
        
        [Tooltip("Trunk color")]
        public Color trunkColor = new Color(0.38f, 0.25f, 0.13f); // #604020
        
        [Tooltip("Trunk width")]
        public float trunkWidth = 0.1f;
        
        [Header("Branch Settings")]
        [Tooltip("Maximum number of branches")]
        public int maxBranches = 8;
        
        [Tooltip("Branch length")]
        public float branchLength = 2.5f;  // 增加树枝长度
        
        [Tooltip("Branch width")]
        public float branchWidth = 0.08f;  // 增加树枝粗细，更容易看到
        
        [Header("Particle Settings")]
        [Tooltip("Golden glow particle color")]
        public Color particleColor = new Color(1f, 0.93f, 0.7f, 0.8f); // #FFEEB3
        
        [Tooltip("Base particle emission rate (particles per second)")]
        public float baseEmissionRate = 10f;
        
        [Tooltip("Max particles for MR performance")]
        public int maxParticles = 500;
        
        [Tooltip("Particle size range")]
        public Vector2 particleSizeRange = new Vector2(0.1f, 0.3f);
        
        [Tooltip("Particle lifetime")]
        public float particleLifetime = 2.0f;
        
        // ============================================================================
        // Internal Components
        // ============================================================================
        
        private LineRenderer trunkRenderer;
        private List<BranchData> branches = new List<BranchData>();
        private ParticleSystem leafParticleSystem;
        private int currentGrowthStage = 0;
        
        // ============================================================================
        // Data Structures
        // ============================================================================
        
        private class BranchData
        {
            public LineRenderer renderer;
            public Vector3 startPoint;
            public Vector3 endPoint;
            public float angle;
            public float heightOffset;
        }
        
        /// <summary>
        /// 季节颜色配置（包含两种混合颜色）
        /// </summary>
        private class SeasonColors
        {
            public Color color1;
            public Color color2;
            
            public SeasonColors(Color c1, Color c2)
            {
                color1 = c1;
                color2 = c2;
            }
        }
        
        // ============================================================================
        // 四季颜色映射（参考 tree_final.html）
        // ============================================================================
        
        /// <summary>
        /// 四季粒子颜色映射表
        /// </summary>
        private Dictionary<string, SeasonColors> seasonColorMap = new Dictionary<string, SeasonColors>()
        {
            { "Default", new SeasonColors(
                new Color(1f, 0.93f, 0.7f, 0.8f),  // 金色 #FFEEB3
                new Color(1f, 0.87f, 0.6f, 0.8f)   // 深金色 #FFDD99
            )},
            { "Spring", new SeasonColors(
                new Color(0x98/255f, 0xFB/255f, 0x98/255f, 0.8f),  // 浅绿 #98FB98
                new Color(0xFF/255f, 0xB6/255f, 0xC1/255f, 0.8f)   // 粉色 #FFB6C1 (樱花感)
            )},
            { "Summer", new SeasonColors(
                new Color(0x2E/255f, 0x8B/255f, 0x57/255f, 0.8f),  // 深绿 #2E8B57
                new Color(0x3C/255f, 0xB3/255f, 0x71/255f, 0.8f)   // 浅绿 #3CB371 (繁茂感)
            )},
            { "Autumn", new SeasonColors(
                new Color(0xFF/255f, 0xA5/255f, 0x00/255f, 0.8f),  // 橙色 #FFA500
                new Color(0xDC/255f, 0x14/255f, 0x3C/255f, 0.8f)   // 红色 #DC143C (落叶感)
            )},
            { "Winter", new SeasonColors(
                new Color(0xFF/255f, 0xFF/255f, 0xFF/255f, 0.8f),  // 白色 #FFFFFF
                new Color(0xAD/255f, 0xD8/255f, 0xE6/255f, 0.8f)   // 淡蓝色 #ADD8E6 (浅蓝雪花感)
            )}
        };
        
        // 当前正在进行的季节过渡协程
        private Coroutine currentSeasonTransition;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Awake()
        {
            // 使用Awake而不是Start，确保在其他脚本调用前完成初始化
            InitializeTreeComponents();
        }
        
        void Start()
        {
            // 初始化为Default季节的颜色（金色混合）
            if (seasonColorMap.ContainsKey("Default"))
            {
                SeasonColors defaultColors = seasonColorMap["Default"];
                SetSeasonColors(defaultColors.color1, defaultColors.color2);
            }
            
            // Start时更新一次生长状态
            UpdateTreeGrowth();
        }
        
        // ============================================================================
        // Initialization
        // ============================================================================
        
        /// <summary>
        /// Initialize all tree components (trunk, particles, etc.)
        /// </summary>
        private void InitializeTreeComponents()
        {
            // Create trunk
            CreateTrunk();
            
            // Create particle system
            CreateParticleSystem();
            
            Debug.Log("[ParticleTreeSystem] Initialized");
        }
        
        /// <summary>
        /// Create the main trunk using LineRenderer
        /// </summary>
        private void CreateTrunk()
        {
            GameObject trunkObj = new GameObject("Trunk");
            trunkObj.transform.SetParent(transform);
            trunkObj.transform.localPosition = Vector3.zero;
            
            trunkRenderer = trunkObj.AddComponent<LineRenderer>();
            Material trunkMat = new Material(Shader.Find("Unlit/Color"));
            trunkMat.color = trunkColor;
            trunkRenderer.material = trunkMat;
            trunkRenderer.startColor = trunkColor;
            trunkRenderer.endColor = trunkColor;
            trunkRenderer.startWidth = trunkWidth;
            trunkRenderer.endWidth = trunkWidth * 0.5f;
            trunkRenderer.positionCount = 2;
            trunkRenderer.useWorldSpace = false;
            
            // Set trunk positions
            trunkRenderer.SetPosition(0, Vector3.zero);
            trunkRenderer.SetPosition(1, new Vector3(0, baseTrunkHeight, 0));
        }
        
        /// <summary>
        /// Create the particle system for glowing light points
        /// </summary>
        private void CreateParticleSystem()
        {
            GameObject particleObj = new GameObject("TreeParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.zero;
            
            leafParticleSystem = particleObj.AddComponent<ParticleSystem>();
            
            // Main module
            var main = leafParticleSystem.main;
            main.startLifetime = particleLifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);  // 更慢的速度，像叶子
            main.startSize = new ParticleSystem.MinMaxCurve(particleSizeRange.x, particleSizeRange.y);
            main.startColor = particleColor;
            main.gravityModifier = -0.05f; // 轻微向上漂浮（不是火焰效果）
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Emission module
            var emission = leafParticleSystem.emission;
            emission.rateOverTime = baseEmissionRate;
            
            // Shape module (emit from tree crown area)
            var shape = leafParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.5f;  // 更大的发射半径，形成树冠效果
            shape.position = new Vector3(0, baseTrunkHeight * 0.8f, 0);
            
            // Color over lifetime (fade out)
            var colorOverLifetime = leafParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(particleColor, 0.0f),
                    new GradientColorKey(particleColor, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = gradient;
            
            // Size over lifetime (shrink)
            var sizeOverLifetime = leafParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0.0f, 1.0f);
            sizeCurve.AddKey(1.0f, 0.3f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);
            
            // Velocity over lifetime (outward drift, not upward jet)
            var velocity = leafParticleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);  // 水平扩散
            velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); // 轻微向上
            velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);  // 水平扩散
            
            // Renderer
            var renderer = leafParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetInt("_BlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            renderer.material.SetInt("_ZWrite", 0);
        }
        
        // ============================================================================
        // Growth Management
        // ============================================================================
        
        /// <summary>
        /// Set the nutrient level and trigger growth update
        /// </summary>
        /// <param name="nutrients">Total accumulated nutrients</param>
        public void SetNutrientLevel(int nutrients)
        {
            currentNutrients = nutrients;
            UpdateTreeGrowth();
        }
        
        /// <summary>
        /// Change particle colors for seasonal theming - 支持两种颜色混合
        /// </summary>
        /// <param name="color1">第一种颜色</param>
        /// <param name="color2">第二种颜色</param>
        public void SetSeasonColors(Color color1, Color color2)
        {
            if (leafParticleSystem == null) return;
            
            // 使用Gradient在两种颜色之间混合
            var main = leafParticleSystem.main;
            
            // 创建包含两种颜色的渐变
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color1, 0.0f),    // 开始：颜色1
                    new GradientColorKey(color2, 0.5f),    // 中间：颜色2
                    new GradientColorKey(color1, 1.0f)     // 结束：颜色1
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(color1.a, 0.0f),
                    new GradientAlphaKey(color2.a, 0.5f),
                    new GradientAlphaKey(color1.a, 1.0f)
                }
            );
            
            // 设置粒子的起始颜色为渐变（这样每个粒子会随机选择渐变中的一个颜色）
            main.startColor = new ParticleSystem.MinMaxGradient(colorGradient);
            
            // 更新生命周期颜色渐变（淡入淡出效果）
            var colorOverLifetime = leafParticleSystem.colorOverLifetime;
            if (colorOverLifetime.enabled)
            {
                Gradient lifetimeGradient = new Gradient();
                // 混合两种颜色
                Color mixedColor = Color.Lerp(color1, color2, 0.5f);
                
                lifetimeGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(mixedColor, 0.0f),
                        new GradientColorKey(mixedColor, 1.0f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.0f, 0.0f),      // 开始时透明
                        new GradientAlphaKey(0.8f, 0.2f),      // 快速淡入
                        new GradientAlphaKey(0.8f, 0.8f),      // 保持可见
                        new GradientAlphaKey(0.0f, 1.0f)       // 结束时淡出
                    }
                );
                colorOverLifetime.color = lifetimeGradient;
            }
            
            Debug.Log($"🌸 Tree season colors changed: color1={color1}, color2={color2}");
        }
        
        /// <summary>
        /// Show or hide the tree
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// 更新树的季节表现（颜色渐变过渡）
        /// </summary>
        /// <param name="season">季节名称: Spring（春）、Summer（夏）、Autumn（秋）、Winter（冬）</param>
        public void UpdateSeason(string season)
        {
            if (!seasonColorMap.ContainsKey(season))
            {
                Debug.LogWarning($"⚠️ 未知的季节: {season}");
                return;
            }
            
            SeasonColors colors = seasonColorMap[season];
            
            // 停止之前的过渡协程（如果有）
            if (currentSeasonTransition != null)
            {
                StopCoroutine(currentSeasonTransition);
            }
            
            // 使用渐变过渡（2秒平滑切换）
            currentSeasonTransition = StartCoroutine(TransitionSeasonColors(colors, 2.0f));
            
            Debug.Log($"🌸 切换到{season}季节：color1={colors.color1}, color2={colors.color2}");
        }
        
        /// <summary>
        /// 协程：平滑过渡粒子颜色（混合两种颜色）
        /// </summary>
        /// <param name="targetColors">目标季节颜色（包含color1和color2）</param>
        /// <param name="duration">过渡时长（秒）</param>
        private System.Collections.IEnumerator TransitionSeasonColors(SeasonColors targetColors, float duration)
        {
            // 获取当前粒子系统的颜色（如果存在的话）
            Color startColor1 = particleColor;
            Color startColor2 = particleColor;
            
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 使用Ease-Out曲线实现平滑过渡
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                
                // 平滑过渡到两种目标颜色
                Color newColor1 = Color.Lerp(startColor1, targetColors.color1, smoothT);
                Color newColor2 = Color.Lerp(startColor2, targetColors.color2, smoothT);
                
                // 应用两种颜色的混合效果
                SetSeasonColors(newColor1, newColor2);
                
                yield return null;
            }
            
            // 最终设置为目标颜色
            SetSeasonColors(targetColors.color1, targetColors.color2);
            
            currentSeasonTransition = null;
        }
        
        /// <summary>
        /// Update tree appearance based on current nutrient level
        /// </summary>
        private void UpdateTreeGrowth()
        {
            // Determine growth stage
            int newStage = CalculateGrowthStage(currentNutrients);
            
            if (newStage != currentGrowthStage)
            {
                currentGrowthStage = newStage;
                RegenerateBranches();
            }
            
            // Update particle emission based on nutrient level
            var emission = leafParticleSystem.emission;
            float emissionRate = baseEmissionRate + (currentNutrients / 10f);
            emission.rateOverTime = Mathf.Min(emissionRate, maxParticles / particleLifetime);
            
            // Update trunk height
            float heightMultiplier = 1.0f + (currentGrowthStage * 0.2f);
            Vector3 trunkTop = new Vector3(0, baseTrunkHeight * heightMultiplier, 0);
            trunkRenderer.SetPosition(1, trunkTop);
            
            // Update particle emission position (spread across tree crown)
            var shape = leafParticleSystem.shape;
            shape.position = trunkTop * 0.9f;
            shape.radius = 1.0f + (currentGrowthStage * 0.5f);  // 随着生长扩大树冠
            
            Debug.Log($"[ParticleTreeSystem] Growth updated: Nutrients={currentNutrients}, Stage={currentGrowthStage}, Emission={emissionRate:F1}/s");
        }
        
        /// <summary>
        /// Calculate growth stage based on nutrient milestones
        /// </summary>
        private int CalculateGrowthStage(int nutrients)
        {
            for (int i = growthMilestones.Length - 1; i >= 0; i--)
            {
                if (nutrients >= growthMilestones[i])
                {
                    return i;
                }
            }
            return 0;
        }
        
        /// <summary>
        /// Regenerate branches based on current growth stage
        /// </summary>
        private void RegenerateBranches()
        {
            // Clear existing branches
            foreach (var branch in branches)
            {
                if (branch.renderer != null)
                {
                    Destroy(branch.renderer.gameObject);
                }
            }
            branches.Clear();
            
            // Determine branch count based on stage
            int branchCount = 0;
            switch (currentGrowthStage)
            {
                case 0: branchCount = 0; break; // Sapling - no branches
                case 1: branchCount = 3; break; // Young
                case 2: branchCount = 6; break; // Mature
                case 3: branchCount = maxBranches; break; // Ancient
            }
            
            // Create branches
            float heightMultiplier = 1.0f + (currentGrowthStage * 0.2f);
            float trunkHeight = baseTrunkHeight * heightMultiplier;
            
            for (int i = 0; i < branchCount; i++)
            {
                CreateBranch(i, branchCount, trunkHeight);
            }
            
            Debug.Log($"[ParticleTreeSystem] Regenerated {branchCount} branches for stage {currentGrowthStage}");
        }
        
        /// <summary>
        /// Create an individual branch
        /// </summary>
        private void CreateBranch(int index, int totalBranches, float trunkHeight)
        {
            GameObject branchObj = new GameObject($"Branch_{index}");
            branchObj.transform.SetParent(transform);
            branchObj.transform.localPosition = Vector3.zero;
            
            LineRenderer branchRenderer = branchObj.AddComponent<LineRenderer>();
            Material branchMat = new Material(Shader.Find("Unlit/Color"));
            branchMat.color = trunkColor;
            branchRenderer.material = branchMat;
            branchRenderer.startColor = trunkColor;
            branchRenderer.endColor = trunkColor;
            branchRenderer.startWidth = branchWidth;
            branchRenderer.endWidth = branchWidth * 0.3f;
            branchRenderer.positionCount = 2;
            branchRenderer.useWorldSpace = false;
            
            // Calculate branch position and angle
            float angle = (index / (float)totalBranches) * Mathf.PI * 2f;
            float heightRatio = 0.5f + (index % 3) * 0.15f; // Vary height
            float heightOffset = trunkHeight * heightRatio;
            
            Vector3 startPoint = new Vector3(0, heightOffset, 0);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)).normalized;
            Vector3 endPoint = startPoint + direction * branchLength;
            
            branchRenderer.SetPosition(0, startPoint);
            branchRenderer.SetPosition(1, endPoint);
            
            // Store branch data
            BranchData branchData = new BranchData
            {
                renderer = branchRenderer,
                startPoint = startPoint,
                endPoint = endPoint,
                angle = angle,
                heightOffset = heightOffset
            };
            branches.Add(branchData);
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Get current nutrient level
        /// </summary>
        public int GetNutrientLevel()
        {
            return currentNutrients;
        }
        
        /// <summary>
        /// Get current growth stage
        /// </summary>
        public int GetGrowthStage()
        {
            return currentGrowthStage;
        }
    }
}

