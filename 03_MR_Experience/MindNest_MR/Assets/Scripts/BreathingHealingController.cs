/*
 * BreathingHealingController.cs
 * ==============================
 * 
 * Breathing Exercise System (4-7-8 Method)
 * 
 * Implements breathing healing exercise with particle sphere animation:
 * - Inhale: 4 seconds (sphere contracts)
 * - Hold: 7 seconds (sphere pauses, gentle pulse)
 * - Exhale: 8 seconds (sphere expands)
 * - 5 cycles total
 * - Nutrient reward on completion
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using System.Collections;
using UnityEngine;

namespace MindNest.MR
{
    /// <summary>
    /// Controls 4-7-8 breathing healing exercise
    /// </summary>
    public class BreathingHealingController : MonoBehaviour
    {
        [Header("References")]
        public MRUIManager uiManager;
        public Camera mainCamera;
        public MindNestMRController mrController;
        public MRSceneStateManager stateManager;
        
        [Header("Exercise Settings")]
        [Tooltip("准备阶段时长（秒）")]
        public float prepareDuration = 5f;
        
        [Tooltip("吸气时长（秒）")]
        public float inhaleDuration = 4f;
        
        [Tooltip("屏息时长（秒）")]
        public float holdDuration = 7f;
        
        [Tooltip("呼气时长（秒）")]
        public float exhaleDuration = 8f;
        
        [Tooltip("总呼吸周期数 - 4个周期共76秒")]
        public int totalCycles = 4;  // 改为4个周期，总计76秒（4 * 19秒）
        
        [Tooltip("完成奖励的营养值")]
        public int nutrientsReward = 30;
        
        [Header("Particle Sphere Settings")]
        [Tooltip("Number of particles in sphere")]
        public int particleCount = 800;
        
        [Tooltip("Minimum sphere radius (inhale)")]
        public float minRadius = 1.5f;
        
        [Tooltip("Maximum sphere radius (exhale)")]
        public float maxRadius = 3.0f;
        
        [Tooltip("Sphere position offset from camera")]
        public Vector3 sphereOffset = new Vector3(0, 0, 3f); // Directly in front of camera
        
        [Header("Particle Colors")]
        public Color inhaleColor = new Color(0.3f, 0.6f, 1f, 0.8f); // Blue
        public Color holdColor = new Color(1f, 0.9f, 0.3f, 0.8f);   // Yellow
        public Color exhaleColor = new Color(0.4f, 1f, 0.5f, 0.8f); // Green
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private enum BreathPhase { Inhale, Hold, Exhale }
        
        private GameObject particleSphereObj;
        private ParticleSystem breathingParticleSystem;
        private ParticleSystem.Particle[] particles;
        
        private bool isExercising = false;
        private Coroutine exerciseCoroutine;
        private int currentCycle = 0;
        private BreathPhase currentPhase = BreathPhase.Inhale;
        
        private float currentRadius;
        private Vector3 sphereWorldPosition;
        
        // === 全局倒计时相关 ===
        private float exerciseStartTime = 0f;  // 练习开始时间
        
        // 计算属性：总呼吸时长
        private float TotalBreathingDuration => totalCycles * (inhaleDuration + holdDuration + exhaleDuration);
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            Debug.Log("🫁 BreathingHealingController: Initializing (4-7-8 Method)");
            
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            CreateParticleSphere();
            SetupUI();
        }
        
        void Update()
        {
            if (isExercising && particles != null && breathingParticleSystem != null)
            {
                UpdateParticleSphere();
                UpdateGlobalTimer();  // 更新全局倒计时
            }
        }
        
        // ============================================================================
        // Setup
        // ============================================================================
        
        private void SetupUI()
        {
            if (uiManager != null && uiManager.finishBreathingButton != null)
            {
                uiManager.finishBreathingButton.onClick.AddListener(OnFinishClicked);
            }
        }
        
        private void CreateParticleSphere()
        {
            // Create particle system object
            particleSphereObj = new GameObject("BreathingParticleSphere");
            particleSphereObj.transform.SetParent(transform);
            
            // Add particle system component
            breathingParticleSystem = particleSphereObj.AddComponent<ParticleSystem>();
            
            var main = breathingParticleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = particleCount;
            main.startLifetime = Mathf.Infinity; // Particles never die
            main.startSpeed = 0f;
            main.startSize = 0.08f;
            main.startColor = inhaleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = breathingParticleSystem.emission;
            emission.enabled = false; // We'll manually control particles
            
            var renderer = breathingParticleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.SetColor("_Color", inhaleColor);
            renderer.material.SetFloat("_Mode", 3); // Transparent mode
            renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            renderer.material.SetInt("_ZWrite", 0);
            renderer.material.EnableKeyword("_ALPHABLEND_ON");
            renderer.material.renderQueue = 3000;
            
            // Initialize particle array
            particles = new ParticleSystem.Particle[particleCount];
            currentRadius = minRadius;
            
            // Hide initially
            particleSphereObj.SetActive(false);
            
            Debug.Log($"✅ Created breathing particle sphere with {particleCount} particles");
        }
        
        private void InitializeParticles()
        {
            // Calculate sphere position (directly in front of camera)
            if (mainCamera != null)
            {
                // Place sphere in front of camera at eye level
                sphereWorldPosition = mainCamera.transform.position + mainCamera.transform.forward * sphereOffset.z;
                sphereWorldPosition.y = mainCamera.transform.position.y + sphereOffset.y;
            }
            else
            {
                sphereWorldPosition = sphereOffset;
            }
            
            // Distribute particles evenly on sphere surface using Fibonacci sphere algorithm
            float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;
            float angleIncrement = Mathf.PI * 2f * goldenRatio;
            
            for (int i = 0; i < particleCount; i++)
            {
                float t = (float)i / particleCount;
                float inclination = Mathf.Acos(1f - 2f * t);
                float azimuth = angleIncrement * i;
                
                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);
                
                Vector3 direction = new Vector3(x, y, z).normalized;
                
                particles[i].position = sphereWorldPosition + direction * currentRadius;
                particles[i].startColor = inhaleColor;
                particles[i].startSize = 0.08f;
                particles[i].remainingLifetime = Mathf.Infinity;
                particles[i].velocity = Vector3.zero;
            }
            
            breathingParticleSystem.SetParticles(particles, particleCount);
        }
        
        private void UpdateParticleSphere()
        {
            // Update sphere center position (follow camera, stay in front)
            if (mainCamera != null)
            {
                sphereWorldPosition = mainCamera.transform.position + mainCamera.transform.forward * sphereOffset.z;
                sphereWorldPosition.y = mainCamera.transform.position.y + sphereOffset.y;
            }
            
            // Update particle positions based on current radius
            for (int i = 0; i < particleCount; i++)
            {
                Vector3 direction = (particles[i].position - sphereWorldPosition).normalized;
                particles[i].position = sphereWorldPosition + direction * currentRadius;
            }
            
            breathingParticleSystem.SetParticles(particles, particleCount);
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        public void StartExercise()
        {
            if (isExercising)
            {
                Debug.LogWarning("⚠️ Breathing exercise already in progress");
                return;
            }
            
            Debug.Log("🫁 Starting 4-7-8 breathing exercise");
            
            // Show particle sphere
            if (particleSphereObj != null)
            {
                particleSphereObj.SetActive(true);
            }
            
            // Initialize particles
            InitializeParticles();
            
            // Start exercise coroutine
            currentCycle = 0;
            isExercising = true;
            exerciseCoroutine = StartCoroutine(ExerciseRoutine());
        }
        
        public void StopExercise()
        {
            if (exerciseCoroutine != null)
            {
                StopCoroutine(exerciseCoroutine);
                exerciseCoroutine = null;
            }
            
            isExercising = false;
            
            // Hide particle sphere
            if (particleSphereObj != null)
            {
                particleSphereObj.SetActive(false);
            }
            
            Debug.Log("🫁 Breathing exercise stopped");
        }
        
        // ============================================================================
        // Exercise Logic
        // ============================================================================
        
        private IEnumerator ExerciseRoutine()
        {
            // === 准备阶段：5秒倒计时 ===
            Debug.Log($"🫁 准备阶段开始：{prepareDuration}秒");
            exerciseStartTime = Time.time;  // 记录开始时间
            yield return StartCoroutine(PreparePhase());
            
            // === 4个完整呼吸周期（每周期19秒，共76秒） ===
            Debug.Log($"🫁 开始{totalCycles}个呼吸周期（4-7-8呼吸法）");
            
            for (int cycle = 1; cycle <= totalCycles; cycle++)
            {
                currentCycle = cycle;
                
                // 吸气 → 屏息 → 呼气
                yield return StartCoroutine(InhalePhase());
                yield return StartCoroutine(HoldPhase());
                yield return StartCoroutine(ExhalePhase());
                
                Debug.Log($"✅ 第{cycle}/{totalCycles}周期完成");
            }
            
            // 练习完成
            OnExerciseComplete();
        }
        
        /// <summary>
        /// 准备阶段：5秒倒计时提示
        /// </summary>
        private IEnumerator PreparePhase()
        {
            UpdateUI("准备中", prepareDuration, 0, totalCycles);
            
            float elapsed = 0f;
            
            while (elapsed < prepareDuration)
            {
                elapsed += Time.deltaTime;
                float remaining = prepareDuration - elapsed;
                
                // 更新UI提示
                int seconds = Mathf.CeilToInt(remaining);
                if (uiManager != null && uiManager.breathingTimerText != null)
                {
                    uiManager.breathingTimerText.text = $"准备开始\n{seconds}秒后开始呼吸练习";
                    uiManager.breathingTimerText.fontSize = 40;
                }
                
                yield return null;
            }
        }
        
        private IEnumerator InhalePhase()
        {
            currentPhase = BreathPhase.Inhale;
            UpdateUI("Inhale", inhaleDuration, currentCycle, totalCycles);
            
            float elapsed = 0f;
            float startRadius = currentRadius;
            
            // Change particle color to blue
            UpdateParticleColor(inhaleColor);
            
            while (elapsed < inhaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / inhaleDuration;
                
                // Ease out cubic for smooth deceleration
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                currentRadius = Mathf.Lerp(startRadius, minRadius, eased);
                
                // Update timer
                float remaining = inhaleDuration - elapsed;
                UpdateTimer(remaining);
                
                yield return null;
            }
            
            currentRadius = minRadius;
        }
        
        private IEnumerator HoldPhase()
        {
            currentPhase = BreathPhase.Hold;
            UpdateUI("Hold", holdDuration, currentCycle, totalCycles);
            
            float elapsed = 0f;
            float baseRadius = currentRadius;
            
            // Change particle color to yellow
            UpdateParticleColor(holdColor);
            
            while (elapsed < holdDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / holdDuration;
                
                // Gentle pulsing effect
                float pulse = Mathf.Sin(t * Mathf.PI * 4f) * 0.05f; // 4 pulses during hold
                currentRadius = baseRadius * (1f + pulse);
                
                // Update timer
                float remaining = holdDuration - elapsed;
                UpdateTimer(remaining);
                
                yield return null;
            }
            
            currentRadius = baseRadius;
        }
        
        private IEnumerator ExhalePhase()
        {
            currentPhase = BreathPhase.Exhale;
            UpdateUI("Exhale", exhaleDuration, currentCycle, totalCycles);
            
            float elapsed = 0f;
            float startRadius = currentRadius;
            
            // Change particle color to green
            UpdateParticleColor(exhaleColor);
            
            while (elapsed < exhaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / exhaleDuration;
                
                // Ease out cubic for smooth deceleration
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                currentRadius = Mathf.Lerp(startRadius, maxRadius, eased);
                
                // Update timer
                float remaining = exhaleDuration - elapsed;
                UpdateTimer(remaining);
                
                yield return null;
            }
            
            currentRadius = maxRadius;
        }
        
        private void UpdateParticleColor(Color targetColor)
        {
            if (particles == null || breathingParticleSystem == null) return;
            
            for (int i = 0; i < particleCount; i++)
            {
                particles[i].startColor = targetColor;
            }
            
            breathingParticleSystem.SetParticles(particles, particleCount);
        }
        
        private void UpdateUI(string phaseText, float phaseDuration, int cycle, int totalCycles)
        {
            if (uiManager == null || uiManager.breathingTimerText == null) return;
            
            string text = $"{phaseText}\nCycle {cycle}/{totalCycles}";
            uiManager.breathingTimerText.text = text;
            uiManager.breathingTimerText.fontSize = 48;
        }
        
        private void UpdateTimer(float remainingSeconds)
        {
            if (uiManager == null || uiManager.breathingTimerText == null) return;
            
            string phaseText = currentPhase == BreathPhase.Inhale ? "Inhale" :
                              currentPhase == BreathPhase.Hold ? "Hold" : "Exhale";
            
            int seconds = Mathf.CeilToInt(remainingSeconds);
            string text = $"{phaseText}\n{seconds}s\nCycle {currentCycle}/{totalCycles}";
            
            uiManager.breathingTimerText.text = text;
        }
        
        private void OnExerciseComplete()
        {
            Debug.Log("✅ Breathing exercise complete!");
            
            isExercising = false;
            
            // Award nutrients
            if (mrController != null)
            {
                mrController.AddNutrients(nutrientsReward);
            }
            
            // 检查是否应该显示 Next 按钮
            bool showNext = MRSceneStateManager.Instance != null && 
                            MRSceneStateManager.Instance.ShouldShowNextButton();
            
            Debug.Log($"🔍 Should show Next button: {showNext}");
            
            // Update UI
            if (uiManager != null && uiManager.finishBreathingButton != null)
            {
                // 动态修改按钮文字
                UnityEngine.UI.Text buttonText = uiManager.finishBreathingButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (buttonText != null)
                {
                    buttonText.text = showNext ? "Next →" : "Finish";
                    Debug.Log($"✅ Button text set to: {buttonText.text}");
                }
                else
                {
                    Debug.LogWarning("⚠️ Button Text component not found");
                }
                
                // 显示完成提示
                if (uiManager.breathingTimerText != null)
                {
                    string nextHint = showNext ? "\n点击 Next 继续下一步" : "\n点击 Finish 返回主界面";
                    uiManager.breathingTimerText.text = $"Complete!\nEarned {nutrientsReward} nutrients{nextHint}";
                }
            }
            
            // 不自动返回，让用户点击按钮
            // StartCoroutine(ReturnToMainMenuAfterDelay(2f));
        }
        
        private IEnumerator ReturnToMainMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            StopExercise();
            
            if (stateManager != null)
            {
                stateManager.TransitionToState(MRSceneState.MainMenu);
            }
        }
        
        // ============================================================================
        // Event Handlers
        // ============================================================================
        
        private void OnFinishClicked()
        {
            Debug.Log("⏹️ User clicked Finish/Next button");
            StopExercise();
            
            // 检查是引导式疗愈还是独立访问
            if (MRSceneStateManager.Instance != null)
            {
                if (MRSceneStateManager.Instance.ShouldShowNextButton())
                {
                    // 进入下一环节
                    Debug.Log("➡️ Continuing to next healing step");
                    MRSceneStateManager.Instance.ContinueHealingFlow();
                }
                else
                {
                    // 完成疗愈流程
                    Debug.Log("🏁 Finishing healing flow");
                    MRSceneStateManager.Instance.FinishHealingFlow();
                }
            }
            else
            {
                // Fallback: 直接返回主菜单
                Debug.LogWarning("⚠️ MRSceneStateManager.Instance is null, returning to main menu");
                if (stateManager != null)
                {
                    stateManager.TransitionToState(MRSceneState.MainMenu);
                }
            }
        }
        
        // ============================================================================
        // 全局倒计时更新
        // ============================================================================
        
        /// <summary>
        /// 更新全局倒计时显示
        /// </summary>
        private void UpdateGlobalTimer()
        {
            if (!isExercising) return;
            
            // 计算总剩余时间
            float totalElapsed = Time.time - exerciseStartTime;
            float totalDuration = prepareDuration + TotalBreathingDuration;
            float remaining = Mathf.Max(0, totalDuration - totalElapsed);
            
            // 更新全局倒计时UI
            if (uiManager != null && uiManager.globalTimerText != null)
            {
                int minutes = Mathf.FloorToInt(remaining / 60f);
                int seconds = Mathf.FloorToInt(remaining % 60f);
                uiManager.globalTimerText.text = $"剩余: {minutes:00}:{seconds:00}";
            }
        }
    }
}
