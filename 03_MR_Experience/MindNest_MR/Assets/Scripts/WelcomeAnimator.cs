/*
 * WelcomeAnimator.cs
 * ==================
 * 
 * Nomi Welcome Entrance Animation
 * 
 * One small Nomi approaches from far away, growing larger as it gets closer.
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using System.Collections;
using UnityEngine;

namespace MindNest.MR
{
    /// <summary>
    /// Handles Nomi's entrance animation
    /// </summary>
    public class WelcomeAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Duration of the approach animation")]
        public float approachDuration = 5.0f;
        
        [Tooltip("Auto-transition delay after animation completes")]
        public float autoTransitionDelay = 1.0f;
        
        [Header("References")]
        [Tooltip("Main Nomi billboard to animate")]
        public GameObject mainNomiBillboard;
        
        [Tooltip("Nomi material")]
        public Material nomiMaterial;
        
        [Tooltip("Starting expression texture")]
        public string startExpression = "welcome";
        
        [Header("Spawn Settings")]
        [Tooltip("Starting distance from camera")]
        public float startDistance = 15f;
        
        [Tooltip("Starting scale multiplier")]
        public float startScaleMultiplier = 0.15f;
        
        [Tooltip("Target position for Nomi")]
        public Vector3 targetPosition = new Vector3(0f, 1.2f, 2.0f);
        
        [Tooltip("Target scale for Nomi")]
        public Vector3 targetScale = new Vector3(2f, 2f, 1f);
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private Vector3 startPosition;
        private Vector3 startScale;
        private Coroutine animationCoroutine;
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Start the welcome animation
        /// </summary>
        public void StartWelcomeAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            
            animationCoroutine = StartCoroutine(PlayWelcomeAnimation());
        }
        
        /// <summary>
        /// Stop the animation
        /// </summary>
        public void StopWelcomeAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            
            // Ensure Nomi is at final position
            if (mainNomiBillboard != null)
            {
                mainNomiBillboard.transform.position = targetPosition;
                mainNomiBillboard.transform.localScale = targetScale;
                mainNomiBillboard.SetActive(true);
            }
        }
        
        // ============================================================================
        // Animation Coroutine
        // ============================================================================
        
        private IEnumerator PlayWelcomeAnimation()
        {
            Debug.Log("Starting Nomi welcome animation");
            
            if (mainNomiBillboard == null)
            {
                Debug.LogError("Main Nomi Billboard not assigned!");
                yield break;
            }
            
            // Load welcome expression
            Texture2D welcomeTexture = Resources.Load<Texture2D>($"Expressions/{startExpression}");
            if (welcomeTexture != null && nomiMaterial != null)
            {
                nomiMaterial.mainTexture = welcomeTexture;
            }
            
            // Calculate start position (far from camera, on same line to target)
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("Main Camera not found!");
                yield break;
            }
            
            Vector3 cameraPos = mainCamera.transform.position;
            Vector3 directionToTarget = (targetPosition - cameraPos).normalized;
            startPosition = cameraPos + directionToTarget * startDistance;
            startScale = targetScale * startScaleMultiplier;
            
            // Set initial state
            mainNomiBillboard.transform.position = startPosition;
            mainNomiBillboard.transform.localScale = startScale;
            mainNomiBillboard.SetActive(true);
            
            Debug.Log($"Nomi approaching from {startPosition} to {targetPosition}");
            
            // Get BillboardBehavior for scale updates
            BillboardBehavior billboard = mainNomiBillboard.GetComponent<BillboardBehavior>();
            
            // Animate approach
            float elapsed = 0f;
            while (elapsed < approachDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / approachDuration;
                
                // Easing function (ease out cubic for smooth deceleration)
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                
                // Animate position
                mainNomiBillboard.transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
                
                // Animate scale
                Vector3 currentScale = Vector3.Lerp(startScale, targetScale, eased);
                mainNomiBillboard.transform.localScale = currentScale;
                
                // 动态更新BillboardBehavior的initialScale
                if (billboard != null)
                {
                    billboard.UpdateBaseScale(currentScale);
                }
                
                yield return null;
            }
            
            // Ensure final state
            mainNomiBillboard.transform.position = targetPosition;
            mainNomiBillboard.transform.localScale = targetScale;
            
            // 最终更新
            if (billboard != null)
            {
                billboard.UpdateBaseScale(targetScale);
            }
            
            Debug.Log("Main Nomi revealed!");
            
            // Auto-transition to customization
            yield return new WaitForSeconds(autoTransitionDelay);
            
            Debug.Log("Welcome animation complete, transitioning to Customization");
            
            if (MRSceneStateManager.Instance != null)
            {
                MRSceneStateManager.Instance.TransitionToState(MRSceneState.Customization);
            }
        }
    }
}

