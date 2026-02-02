/*
 * SpeechBubbleController.cs
 * ==========================
 * 
 * 3D Speech Bubble System for Nomi
 * 
 * Features:
 * - Creates floating speech bubbles in 3D space next to Nomi
 * - Auto-adjusts bubble size based on text length
 * - Fade in/out animations
 * - Always faces the camera (billboard behavior)
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
    /// Manages 3D speech bubbles that appear next to Nomi during conversation
    /// </summary>
    public class SpeechBubbleController : MonoBehaviour
    {
        // ============================================================================
        // Configuration
        // ============================================================================
        
        [Header("References")]
        [Tooltip("Nomi's transform (where the bubble will appear)")]
        public Transform nomiTransform;
        
        [Tooltip("Main camera for billboard effect")]
        public Camera mainCamera;
        
        [Header("Bubble Settings")]
        [Tooltip("Offset from Nomi's position (right, up, forward)")]
        public Vector3 bubbleOffset = new Vector3(5f, 5f, 0); // Right and up from Nomi (moved right to avoid blocking character)
        
        [Tooltip("Scale of the bubble canvas")]
        public float bubbleScale = 0.008f;
        
        [Tooltip("How long the bubble stays visible")]
        public float displayDuration = 4f;
        
        [Tooltip("Fade in duration")]
        public float fadeInDuration = 0.3f;
        
        [Tooltip("Fade out duration")]
        public float fadeOutDuration = 0.3f;
        
        [Header("Bubble Style")]
        [Tooltip("Bubble background color")]
        public Color bubbleColor = new Color(1f, 1f, 1f, 0.95f); // White with slight transparency
        
        [Tooltip("Text color")]
        public Color textColor = Color.black;
        
        [Tooltip("Text font size")]
        public int fontSize = 36;
        
        [Tooltip("Maximum bubble width (in canvas units)")]
        public float maxBubbleWidth = 600f;
        
        [Tooltip("Padding around text")]
        public float textPadding = 20f;
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private GameObject currentBubble;
        private CanvasGroup bubbleCanvasGroup;
        private Coroutine displayCoroutine;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            Debug.Log("💬 SpeechBubbleController initialized");
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Display a speech bubble with the given message
        /// </summary>
        /// <param name="message">The text to display</param>
        public void ShowBubble(string message)
        {
            // Clear any existing bubble
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
                displayCoroutine = null;
            }
            
            if (currentBubble != null)
            {
                Destroy(currentBubble);
            }
            
            // Create new bubble
            CreateBubble(message);
            
            // Start display coroutine
            displayCoroutine = StartCoroutine(DisplayBubbleRoutine());
        }
        
        /// <summary>
        /// Immediately hide the current bubble
        /// </summary>
        public void HideBubble()
        {
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
                displayCoroutine = null;
            }
            
            if (currentBubble != null)
            {
                Destroy(currentBubble);
                currentBubble = null;
            }
        }
        
        // ============================================================================
        // Internal Methods
        // ============================================================================
        
        private void CreateBubble(string message)
        {
            if (nomiTransform == null)
            {
                Debug.LogWarning("⚠️ Nomi transform not set, cannot create speech bubble");
                return;
            }
            
            // Create bubble GameObject
            currentBubble = new GameObject("SpeechBubble");
            currentBubble.transform.SetParent(transform);
            
            // Calculate world position (Nomi position + offset)
            Vector3 bubbleWorldPos = nomiTransform.position + bubbleOffset;
            currentBubble.transform.position = bubbleWorldPos;
            
            // Create WorldSpace Canvas
            Canvas canvas = currentBubble.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = currentBubble.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(600, 200); // Initial size, will be adjusted
            canvasRect.localScale = new Vector3(bubbleScale, bubbleScale, bubbleScale);
            
            // Add CanvasGroup for fade animations
            bubbleCanvasGroup = currentBubble.AddComponent<CanvasGroup>();
            bubbleCanvasGroup.alpha = 0f; // Start invisible
            
            // Add GraphicRaycaster (required for canvas)
            currentBubble.AddComponent<GraphicRaycaster>();
            
            // Create background panel
            GameObject bgPanel = new GameObject("Background");
            bgPanel.transform.SetParent(currentBubble.transform, false);
            
            RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
            
            Image bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = bubbleColor;
            
            // Create text GameObject
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bgPanel.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(textPadding, textPadding);
            textRect.offsetMax = new Vector2(-textPadding, -textPadding);
            
            Text text = textObj.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = textColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            
            // Calculate preferred size based on text
            TextGenerator textGen = new TextGenerator();
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(maxBubbleWidth - textPadding * 2, 0));
            float preferredWidth = textGen.GetPreferredWidth(message, settings) + textPadding * 2;
            float preferredHeight = textGen.GetPreferredHeight(message, settings) + textPadding * 2;
            
            // Clamp width and adjust height accordingly
            preferredWidth = Mathf.Min(preferredWidth, maxBubbleWidth);
            preferredHeight = Mathf.Max(preferredHeight, 80); // Minimum height
            
            canvasRect.sizeDelta = new Vector2(preferredWidth, preferredHeight);
            
            Debug.Log($"💬 Speech bubble created: \"{message}\" ({preferredWidth}x{preferredHeight})");
        }
        
        private IEnumerator DisplayBubbleRoutine()
        {
            if (currentBubble == null || bubbleCanvasGroup == null)
            {
                yield break;
            }
            
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                bubbleCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                UpdateBubblePosition();
                yield return null;
            }
            bubbleCanvasGroup.alpha = 1f;
            
            // Display
            elapsed = 0f;
            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;
                UpdateBubblePosition();
                yield return null;
            }
            
            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                bubbleCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeOutDuration));
                UpdateBubblePosition();
                yield return null;
            }
            bubbleCanvasGroup.alpha = 0f;
            
            // Cleanup
            if (currentBubble != null)
            {
                Destroy(currentBubble);
                currentBubble = null;
            }
            
            displayCoroutine = null;
        }
        
        private void UpdateBubblePosition()
        {
            if (currentBubble == null || nomiTransform == null || mainCamera == null)
            {
                return;
            }
            
            // Update position to follow Nomi
            Vector3 bubbleWorldPos = nomiTransform.position + bubbleOffset;
            currentBubble.transform.position = bubbleWorldPos;
            
            // Billboard effect - always face camera
            currentBubble.transform.LookAt(mainCamera.transform);
            currentBubble.transform.Rotate(0, 180, 0); // Flip to face camera correctly
        }
        
        void LateUpdate()
        {
            // Continuously update bubble position if active
            if (currentBubble != null && displayCoroutine != null)
            {
                UpdateBubblePosition();
            }
        }
    }
}

