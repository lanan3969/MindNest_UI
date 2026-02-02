/*
 * TreeViewController.cs
 * =====================
 * 
 * Tree Focus View System
 * 
 * Implements focused tree view with:
 * - Camera zoom to tree
 * - Season color changes (Spring, Summer, Autumn, Winter)
 * - Floating light orbs for task activation
 * - Season-based particle color updates
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MindNest.MR
{
    /// <summary>
    /// Controls tree view interactions
    /// </summary>
    public class TreeViewController : MonoBehaviour
    {
        [Header("References")]
        public MRUIManager uiManager;
        public ParticleTreeSystem treeSystem;
        public FloatingOrbSystem orbSystem;
        public Camera mainCamera;
        public Transform treeTransform;
        public MindNestMRController mrController;
        public MRSceneStateManager stateManager;
        
        [Header("Camera Settings")]
        [Tooltip("Normal camera position")]
        public Vector3 normalCameraPosition = new Vector3(0, 1.5f, 0);
        
        [Tooltip("Focused camera position (wide elevated view like tree_final.html)")]
        public Vector3 focusedCameraPosition = new Vector3(0f, 2.8f, -3f); // Elevated side view of tree
        
        [Tooltip("Camera transition duration")]
        public float cameraTransitionDuration = 1.0f;
        
        [Header("Season Colors")]
        public Color springColor = new Color(1f, 0.8f, 0.9f, 0.8f); // Pink/white
        public Color summerColor = new Color(0.6f, 1f, 0.4f, 0.8f); // Green
        public Color autumnColor = new Color(1f, 0.6f, 0.2f, 0.8f); // Orange
        public Color winterColor = new Color(0.9f, 0.95f, 1f, 0.8f); // White/blue
        
        [Header("Light Orbs")]
        [Tooltip("Number of floating orbs")]
        public int orbCount = 5;
        
        [Tooltip("Orb orbit radius")]
        public float orbRadius = 1.5f;
        
        [Tooltip("Orb height offset")]
        public float orbHeightOffset = 1.0f;
        
        [Tooltip("Orb rotation speed")]
        public float orbRotationSpeed = 10f;
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private bool isFocused = false;
        private List<GameObject> orbs = new List<GameObject>();
        private Coroutine cameraCoroutine;
        private string currentSeason = "Spring";
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            Debug.Log("🌳 TreeViewController: Initializing");
            
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            if (mainCamera != null)
            {
                normalCameraPosition = mainCamera.transform.position;
            }
            
            SetupUI();
            CreateOrbs();
            HideOrbs();
        }
        
        void Update()
        {
            if (isFocused)
            {
                AnimateOrbs();
                DetectOrbClick();
            }
        }
        
        // ============================================================================
        // Setup
        // ============================================================================
        
        private void SetupUI()
        {
            if (uiManager == null)
            {
                Debug.LogWarning("⚠️ TreeViewController: UIManager is null!");
                return;
            }
            
            Debug.Log("🌳 TreeViewController: Setting up UI...");
            
            // Season dropdown
            if (uiManager.seasonDropdown != null)
            {
                Debug.Log($"✅ Season dropdown found! Options count: {uiManager.seasonDropdown.options.Count}");
                foreach (var option in uiManager.seasonDropdown.options)
                {
                    Debug.Log($"   - Option: {option.text}");
                }
                
                uiManager.seasonDropdown.onValueChanged.AddListener(OnSeasonChanged);
                uiManager.seasonDropdown.value = 0; // Default to Default (golden)
                
                Debug.Log($"🌸 Initial season set to: {uiManager.seasonDropdown.options[0].text}");
            }
            else
            {
                Debug.LogError("❌ Season dropdown is NULL!");
            }
            
            // Reset orbs button
            if (uiManager.resetOrbsButton != null)
            {
                uiManager.resetOrbsButton.onClick.AddListener(OnResetOrbsClicked);
            }
            else
            {
                Debug.LogWarning("⚠️ Reset orbs button is NULL!");
            }
            
            // Close button
            if (uiManager.closeTreeButton != null)
            {
                uiManager.closeTreeButton.onClick.AddListener(OnCloseClicked);
            }
            else
            {
                Debug.LogWarning("⚠️ Close tree button is NULL!");
            }
            
            Debug.Log("✅ TreeViewController: UI setup complete!");
        }
        
        private void CreateOrbs()
        {
            // Orb creation is now handled by FloatingOrbSystem
            // This method is kept for compatibility but does nothing
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Focus on tree (zoom in)
        /// </summary>
        public void FocusOnTree()
        {
            Debug.Log("🌳 Focusing on tree");
            
            isFocused = true;
            
            // Move camera to focused position
            if (cameraCoroutine != null)
            {
                StopCoroutine(cameraCoroutine);
            }
            cameraCoroutine = StartCoroutine(MoveCameraToPosition(focusedCameraPosition));
            
            // Show orbs
            ShowOrbs();
        }
        
        /// <summary>
        /// Unfocus tree (zoom out)
        /// </summary>
        public void UnfocusTree()
        {
            Debug.Log("🌳 Unfocusing tree");
            
            isFocused = false;
            
            // Move camera back to normal position
            if (cameraCoroutine != null)
            {
                StopCoroutine(cameraCoroutine);
            }
            cameraCoroutine = StartCoroutine(MoveCameraToPosition(normalCameraPosition));
            
            // Hide orbs
            HideOrbs();
        }
        
        // ============================================================================
        // Camera Movement
        // ============================================================================
        
        private IEnumerator MoveCameraToPosition(Vector3 targetPosition)
        {
            if (mainCamera == null) yield break;
            
            Vector3 startPosition = mainCamera.transform.position;
            Quaternion startRotation = mainCamera.transform.rotation;
            
            // Calculate target rotation (look at tree center, not base)
            Quaternion targetRotation = startRotation;
            if (treeTransform != null && isFocused)
            {
                // Look at mid-height of tree (around 8m up from base)
                Vector3 treeCenter = treeTransform.position + new Vector3(0, 3f, 0);
                Vector3 lookDirection = treeCenter - targetPosition;
                targetRotation = Quaternion.LookRotation(lookDirection);
            }
            
            float elapsed = 0f;
            
            while (elapsed < cameraTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / cameraTransitionDuration;
                
                // Ease in-out
                t = t * t * (3f - 2f * t);
                
                mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                
                yield return null;
            }
            
            mainCamera.transform.position = targetPosition;
            mainCamera.transform.rotation = targetRotation;
        }
        
        // ============================================================================
        // Orb Management
        // ============================================================================
        
        private void ShowOrbs()
        {
            if (orbSystem != null)
            {
                orbSystem.SpawnOrbs();
            }
        }
        
        private void HideOrbs()
        {
            if (orbSystem != null)
            {
                orbSystem.ClearOrbs();
            }
        }
        
        private void AnimateOrbs()
        {
            if (treeTransform == null) return;
            
            float time = Time.time * orbRotationSpeed;
            
            for (int i = 0; i < orbs.Count; i++)
            {
                if (orbs[i] == null || !orbs[i].activeSelf) continue;
                
                // Rotate around tree
                float angle = (i / (float)orbCount) * 360f * Mathf.Deg2Rad + time * Mathf.Deg2Rad;
                Vector3 position = treeTransform.position + new Vector3(
                    Mathf.Cos(angle) * orbRadius,
                    orbHeightOffset + Mathf.Sin(time * 0.5f + i) * 0.3f, // Vertical bobbing
                    Mathf.Sin(angle) * orbRadius
                );
                
                orbs[i].transform.position = position;
                
                // Pulse scale
                float scale = 0.15f + Mathf.Sin(time + i) * 0.02f;
                orbs[i].transform.localScale = new Vector3(scale, scale, scale);
            }
        }
        
        private void DetectOrbClick()
        {
            // Check for mouse click or touch
            bool inputDetected = false;
            Ray ray = new Ray();
            
            if (Input.GetMouseButtonDown(0))
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                inputDetected = true;
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                ray = mainCamera.ScreenPointToRay(Input.GetTouch(0).position);
                inputDetected = true;
            }
            
            if (!inputDetected) return;
            
            // Raycast to detect orb
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.collider != null && hit.collider.gameObject.name.StartsWith("LightOrb_"))
                {
                    OnOrbClicked(hit.collider.gameObject);
                }
            }
        }
        
        private void OnOrbClicked(GameObject orb)
        {
            if (orbSystem == null) return;
            
            Debug.Log($"💡 Orb clicked: {orb.name}");
            
            // Get current anxiety level
            string anxietyLevel = GetCurrentAnxietyLevel();
            
            // Get task from orb BEFORE removing it
            string task = orbSystem.GetOrbTask(orb);
            
            // Always create explosion effect
            orbSystem.RemoveOrb(orb);
            
            // Only show task if anxiety is moderate or severe (behavioral activation needed)
            if (anxietyLevel == "moderate" || anxietyLevel == "severe")
            {
                if (!string.IsNullOrEmpty(task))
                {
                    Debug.Log($"📋 High anxiety detected - showing task: {task}");
                    
                    // Show task overlay (better UI than toast)
                    int nutrients = orbSystem.GetNutrientsPerTask();
                    string taskMessage = $"{task}\n\nComplete this activity to earn {nutrients} nutrients for your tree!";
                    
                    if (uiManager != null)
                    {
                        uiManager.ShowTaskOverlay(taskMessage);
                    }
                    else
                    {
                        // Fallback: log to console if UI manager not available
                        Debug.LogWarning($"⚠️ UI Manager not available, cannot show task: {taskMessage}");
                    }
                    
                    // Store task in PlayerPrefs for tracking
                    int tasksActivated = PlayerPrefs.GetInt("TasksActivated", 0);
                    tasksActivated++;
                    PlayerPrefs.SetInt("TasksActivated", tasksActivated);
                    PlayerPrefs.SetString($"Task_{tasksActivated}", task);
                    PlayerPrefs.Save();
                    
                    Debug.Log($"✅ Task activated: {task} (Total: {tasksActivated})");
                }
            }
            else
            {
                // Low anxiety: just explosion effect, no task needed
                Debug.Log($"💫 Orb dispersed (anxiety level: {anxietyLevel} - no task needed)");
                if (uiManager != null)
                {
                    uiManager.ShowTaskOverlay("✨ Beautiful energy released!\n\nYou're doing great!");
                }
            }
        }
        
        private string GetCurrentAnxietyLevel()
        {
            // Try to get from MR controller
            if (mrController != null)
            {
                string level = mrController.GetCurrentAnxietyLevel();
                if (!string.IsNullOrEmpty(level))
                {
                    return level;
                }
            }
            
            // Fallback to PlayerPrefs
            return PlayerPrefs.GetString("CurrentAnxietyLevel", "moderate");
        }
        
        // ============================================================================
        // Season Management
        // ============================================================================
        
        private void OnSeasonChanged(int seasonIndex)
        {
            string[] seasons = { "Default", "Spring", "Summer", "Autumn", "Winter" };
            
            Debug.Log($"🌸 OnSeasonChanged called! Index: {seasonIndex}");
            
            if (seasonIndex >= 0 && seasonIndex < seasons.Length)
            {
                currentSeason = seasons[seasonIndex];
                Debug.Log($"🌸 Changing season to: {currentSeason}");
                Debug.Log($"🌳 TreeSystem reference: {(treeSystem != null ? "OK" : "NULL")}");
                
                ApplySeasonColors(currentSeason);
                
                Debug.Log($"✅ Season change complete: {currentSeason}");
            }
            else
            {
                Debug.LogError($"❌ Invalid season index: {seasonIndex} (must be 0-4)");
            }
        }
        
        /// <summary>
        /// 应用季节颜色到粒子树系统
        /// </summary>
        /// <param name="season">季节名称</param>
        private void ApplySeasonColors(string season)
        {
            Debug.Log($"🎨 ApplySeasonColors called for: {season}");
            
            if (treeSystem == null)
            {
                Debug.LogError("❌ TreeSystem is NULL! Cannot update season!");
                Debug.LogError("   Please assign ParticleTreeSystem reference in Inspector!");
                return;
            }
            
            Debug.Log($"✅ TreeSystem found! Calling UpdateSeason({season})...");
            
            // 使用ParticleTreeSystem的新UpdateSeason方法
            // 该方法会平滑过渡到tree_final.html中定义的四季颜色
            treeSystem.UpdateSeason(season);
            
            Debug.Log($"✅ UpdateSeason({season}) called successfully!");
        }
        
        // ============================================================================
        // UI Callbacks
        // ============================================================================
        
        private void OnResetOrbsClicked()
        {
            Debug.Log("🔄 Resetting orbs");
            ShowOrbs();
            if (uiManager != null)
            {
                uiManager.ShowTaskOverlay("🔄 Orbs Reset\n\nNew tasks are ready for you!");
            }
        }
        
        private void OnCloseClicked()
        {
            Debug.Log("❌ Closing tree view / Finish button clicked");
            
            if (MRSceneStateManager.Instance != null)
            {
                // 检查是否在引导式疗愈流程中
                if (MRSceneStateManager.Instance.IsInGuidedHealing())
                {
                    // 如果是引导式疗愈，完成流程
                    Debug.Log("🏁 Finishing guided healing flow from tree view");
                    MRSceneStateManager.Instance.FinishHealingFlow();
                }
                else
                {
                    // 否则直接返回主菜单
                    Debug.Log("↩️ Returning to main menu (independent access)");
                    MRSceneStateManager.Instance.ReturnToMainMenu();
                }
            }
        }
        
        // ============================================================================
        // Helper Methods
        // ============================================================================
        
        // ShowToast method removed - all notifications now use TaskOverlay for consistency
        
        // ============================================================================
        // Cleanup
        // ============================================================================
        
        void OnDestroy()
        {
            foreach (GameObject orb in orbs)
            {
                if (orb != null)
                {
                    Destroy(orb);
                }
            }
            orbs.Clear();
        }
    }
}

