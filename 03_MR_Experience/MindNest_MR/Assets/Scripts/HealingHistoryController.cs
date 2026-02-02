/*
 * HealingHistoryController.cs
 * ============================
 * 
 * Healing History Display System
 * 
 * Fetches and displays healing history from backend:
 * - Today's overview (breathing duration, healing method)
 * - Scrollable history list with date, time, anxiety level
 * - Recall button to view past Nomi expressions
 * 
 * Author: MindNest Team
 * Date: 2026-01-28
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace MindNest.MR
{
    /// <summary>
    /// Displays healing history
    /// </summary>
    public class HealingHistoryController : MonoBehaviour
    {
        [Header("References")]
        public MRUIManager uiManager;
        public MindNestMRController mrController;
        public MRSceneStateManager stateManager;
        
        [Header("API Settings")]
        [Tooltip("Backend API base URL")]
        public string apiBaseUrl = "http://localhost:8000";
        
        [Tooltip("User ID")]
        public string userId = "user_demo_001";
        
        [Header("Mock Data")]
        [Tooltip("Use mock data if backend unavailable")]
        public bool useMockData = true;
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private List<GameObject> historyCards = new List<GameObject>();
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            Debug.Log("📜 HealingHistoryController: Initializing");
            SetupUI();
        }
        
        // ============================================================================
        // Setup
        // ============================================================================
        
        private void SetupUI()
        {
            if (uiManager != null && uiManager.backFromHistoryButton != null)
            {
                uiManager.backFromHistoryButton.onClick.AddListener(OnBackClicked);
            }
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        /// <summary>
        /// Load and display history
        /// </summary>
        public void LoadHistory()
        {
            Debug.Log("📜 Loading healing history");
            
            // Clear existing cards
            ClearHistoryCards();
            
            if (useMockData)
            {
                LoadMockHistory();
            }
            else
            {
                StartCoroutine(FetchHistoryFromBackend());
            }
        }
        
        // ============================================================================
        // Backend Integration
        // ============================================================================
        
        private IEnumerator FetchHistoryFromBackend()
        {
            string url = $"{apiBaseUrl}/api/v1/history/{userId}";
            
            Debug.Log($"📡 Fetching history from: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"✅ History received: {jsonResponse}");
                    
                    ParseAndDisplayHistory(jsonResponse);
                }
                else
                {
                    Debug.LogWarning($"⚠️ Failed to fetch history: {request.error}");
                    Debug.Log("Using mock data instead");
                    LoadMockHistory();
                }
            }
        }
        
        private void ParseAndDisplayHistory(string json)
        {
            try
            {
                // Parse JSON response
                // Expected format: { "history": [ { "date": "2026-01-28", "time": "16:15", "anxiety_score": 3.5, "anxiety_level": "moderate", "expression": "sad" }, ... ] }
                
                HistoryResponse response = JsonUtility.FromJson<HistoryResponse>(json);
                
                if (response != null && response.history != null)
                {
                    foreach (HistoryEntry entry in response.history)
                    {
                        CreateHistoryCard(entry);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error parsing history JSON: {e.Message}");
                LoadMockHistory();
            }
        }
        
        // ============================================================================
        // Mock Data
        // ============================================================================
        
        private void LoadMockHistory()
        {
            Debug.Log("📋 Loading mock history data");
            
            // Create mock entries
            HistoryEntry[] mockEntries = new HistoryEntry[]
            {
                new HistoryEntry
                {
                    date = "11/28",
                    time = "16:15",
                    anxiety_level = "A little bit anxious",
                    anxiety_score = 3.5f,
                    expression = "sad"
                },
                new HistoryEntry
                {
                    date = "11/28",
                    time = "14:11",
                    anxiety_level = "Feeling calm",
                    anxiety_score = 2.1f,
                    expression = "meditation"
                },
                new HistoryEntry
                {
                    date = "11/27",
                    time = "18:22",
                    anxiety_level = "Moderately anxious",
                    anxiety_score = 5.2f,
                    expression = "thinking"
                },
                new HistoryEntry
                {
                    date = "11/27",
                    time = "12:05",
                    anxiety_level = "Very anxious",
                    anxiety_score = 7.8f,
                    expression = "deadline"
                },
                new HistoryEntry
                {
                    date = "11/26",
                    time = "20:30",
                    anxiety_level = "Peaceful",
                    anxiety_score = 1.5f,
                    expression = "happy"
                }
            };
            
            foreach (HistoryEntry entry in mockEntries)
            {
                CreateHistoryCard(entry);
            }
        }
        
        // ============================================================================
        // UI Creation
        // ============================================================================
        
        private void CreateHistoryCard(HistoryEntry entry)
        {
            if (uiManager == null || uiManager.historyContentParent == null) return;
            
            GameObject card = uiManager.CreateHistoryCard(entry.date, entry.time, entry.anxiety_level);
            
            if (card != null)
            {
                // Position card in scroll view
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0, 1);
                cardRect.anchorMax = new Vector2(1, 1);
                cardRect.pivot = new Vector2(0.5f, 1);
                cardRect.anchoredPosition = new Vector2(0, -historyCards.Count * 130f);
                
                // Setup recall button
                Button recallButton = card.GetComponentInChildren<Button>();
                if (recallButton != null)
                {
                    string expression = entry.expression;
                    recallButton.onClick.AddListener(() => OnRecallClicked(expression));
                }
                
                historyCards.Add(card);
            }
            
            // Update scroll content size
            UpdateScrollContentSize();
        }
        
        private void UpdateScrollContentSize()
        {
            if (uiManager == null || uiManager.historyScrollRect == null) return;
            
            RectTransform contentRect = uiManager.historyScrollRect.content;
            if (contentRect != null)
            {
                float totalHeight = historyCards.Count * 130f;
                contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Max(totalHeight, 400f));
            }
        }
        
        private void ClearHistoryCards()
        {
            foreach (GameObject card in historyCards)
            {
                if (card != null)
                {
                    Destroy(card);
                }
            }
            historyCards.Clear();
        }
        
        // ============================================================================
        // Button Callbacks
        // ============================================================================
        
        private void OnRecallClicked(string expression)
        {
            Debug.Log($"🔙 Recall clicked for expression: {expression}");
            
            // Load and display the expression on Nomi
            if (mrController != null)
            {
                mrController.SetExpression(expression);
            }
            
            // Show message
            if (uiManager != null && uiManager.dialogueText != null)
            {
                uiManager.dialogueText.text = $"Recalling {expression} expression...";
            }
        }
        
        private void OnBackClicked()
        {
            Debug.Log("⬅️ Back button clicked");
            
            if (MRSceneStateManager.Instance != null)
            {
                MRSceneStateManager.Instance.ReturnToMainMenu();
            }
        }
        
        // ============================================================================
        // Data Classes
        // ============================================================================
        
        [Serializable]
        public class HistoryResponse
        {
            public HistoryEntry[] history;
        }
        
        [Serializable]
        public class HistoryEntry
        {
            public string date;
            public string time;
            public float anxiety_score;
            public string anxiety_level;
            public string expression;
        }
        
        // ============================================================================
        // Cleanup
        // ============================================================================
        
        void OnDestroy()
        {
            ClearHistoryCards();
        }
    }
}

