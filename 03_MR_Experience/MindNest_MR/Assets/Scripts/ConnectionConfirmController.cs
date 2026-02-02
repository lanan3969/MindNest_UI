/*
 * ConnectionConfirmController.cs
 * ==============================
 * 
 * Manages the listening/conversation stage with AI integration
 * 
 * Features:
 * - Text input for user messages
 * - Scrollable chat history (user + AI responses)
 * - DashScope Qwen API integration for natural conversation
 * - Nomi expression updates based on conversation mood
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
    [Serializable]
    public class ChatMessage
    {
        public string role; // "user" or "assistant"
        public string content;
        public string timestamp;
    }

    /// <summary>
    /// Handles conversation stage with AI
    /// </summary>
    public class ConnectionConfirmController : MonoBehaviour
    {
        [Header("References")]
        public MRUIManager uiManager;
        public MindNestMRController mrController;
        public MRSceneStateManager stateManager;
        public SpeechBubbleController speechBubble;
        
        [Header("API Settings")]
        public string apiBaseUrl = "http://localhost:8000";
        public string userId = "user_demo_001";
        
        [Header("Chat Settings")]
        public int maxMessages = 10;
        public float typingDelay = 0.5f;
        
        // ============================================================================
        // Internal State
        // ============================================================================
        
        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private bool isProcessing = false;
        
        // ============================================================================
        // Unity Lifecycle
        // ============================================================================
        
        void Start()
        {
            Debug.Log("💬 ConnectionConfirmController: Initializing");
            
            // Setup UI listeners
            if (uiManager != null)
            {
                if (uiManager.sendButton != null)
                {
                    uiManager.sendButton.onClick.AddListener(OnSendMessage);
                }
                
                if (uiManager.chatInputField != null)
                {
                    uiManager.chatInputField.onEndEdit.AddListener((text) => {
                        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                        {
                            OnSendMessage();
                        }
                    });
                }
            }
            
            Debug.Log("✅ ConnectionConfirmController: Ready");
        }
        
        // ============================================================================
        // Public Interface
        // ============================================================================
        
        public void StartConversation()
        {
            Debug.Log("💬 Starting conversation with Nomi");
            
            // Clear previous chat
            ClearChatHistory();
            
            // Add initial AI greeting
            string greeting = "Hi! How are you feeling today?";
            
            // Show greeting in speech bubble
            if (speechBubble != null)
            {
                speechBubble.ShowBubble(greeting);
            }
            
            AddMessageToUI("assistant", greeting);
        }
        
        // ============================================================================
        // Message Handling
        // ============================================================================
        
        private void OnSendMessage()
        {
            if (uiManager == null || uiManager.chatInputField == null) return;
            
            string userMessage = uiManager.chatInputField.text;
            if (string.IsNullOrWhiteSpace(userMessage)) return;
            
            if (isProcessing)
            {
                Debug.LogWarning("⚠️ Already processing a message");
                return;
            }
            
            // Add user message to UI
            AddMessageToUI("user", userMessage);
            
            // Clear input field
            uiManager.chatInputField.text = "";
            
            // Send to AI
            StartCoroutine(SendMessageToAI(userMessage));
        }
        
        private IEnumerator SendMessageToAI(string userMessage)
        {
            isProcessing = true;
            
            // Show "thinking..." indicator
            if (uiManager.listeningText != null)
            {
                uiManager.listeningText.gameObject.SetActive(true);
                uiManager.listeningText.text = "Nomi is thinking...";
            }
            
            // Build conversation history for context
            List<ModelScopeMessage> messages = new List<ModelScopeMessage>();
            
            // Add system message
            messages.Add(new ModelScopeMessage
            {
                role = "system",
                content = "You are Nomi, a caring and supportive companion who helps people with anxiety and emotional well-being. " +
                         "You are empathetic, warm, and encouraging. Keep responses concise (2-3 sentences) and supportive. " +
                         "Ask gentle follow-up questions to understand their feelings better."
            });
            
            // Add recent conversation history (last 5 messages)
            int startIndex = Mathf.Max(0, chatHistory.Count - 5);
            for (int i = startIndex; i < chatHistory.Count; i++)
            {
                messages.Add(new ModelScopeMessage
                {
                    role = chatHistory[i].role,
                    content = chatHistory[i].content
                });
            }
            
            // Add current user message
            messages.Add(new ModelScopeMessage
            {
                role = "user",
                content = userMessage
            });
            
            // Prepare ModelScope API request
            string url = "https://api-inference.modelscope.cn/v1/chat/completions";
            
            ModelScopeRequest requestData = new ModelScopeRequest
            {
                model = "Qwen/Qwen2.5-7B-Instruct",
                messages = messages.ToArray()
            };
            
            string jsonData = JsonUtility.ToJson(requestData);
            
            // Manual JSON construction for proper formatting (JsonUtility has limitations)
            jsonData = "{\"model\":\"Qwen/Qwen2.5-7B-Instruct\",\"messages\":[";
            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) jsonData += ",";
                jsonData += "{\"role\":\"" + messages[i].role + "\",\"content\":\"" + EscapeJson(messages[i].content) + "\"}";
            }
            jsonData += "]}";
            
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer ms-e9f00c7a-59bf-486e-8962-6627c1056556");
            
            Debug.Log($"📤 Sending message to ModelScope API: {userMessage}");
            
            yield return request.SendWebRequest();
            
            // Hide "thinking..." indicator
            if (uiManager.listeningText != null)
            {
                uiManager.listeningText.gameObject.SetActive(false);
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"📥 ModelScope response: {responseText}");
                
                try
                {
                    // Parse ModelScope response
                    ModelScopeResponse response = JsonUtility.FromJson<ModelScopeResponse>(responseText);
                    
                    string aiReply = "";
                    if (response.choices != null && response.choices.Length > 0 && 
                        response.choices[0].message != null)
                    {
                        aiReply = response.choices[0].message.content;
                    }
                    
                    if (string.IsNullOrEmpty(aiReply))
                    {
                        aiReply = "I'm here to listen. Tell me more about how you're feeling.";
                    }
                    
                    // Show 3D speech bubble next to Nomi
                    if (speechBubble != null)
                    {
                        speechBubble.ShowBubble(aiReply);
                    }
                    
                    // Add AI response to UI (keep chat history)
                    AddMessageToUI("assistant", aiReply);
                    
                    // Analyze conversation sentiment and update anxiety level
                    UpdateAnxietyLevel(userMessage, aiReply);
                    
                    Debug.Log($"✅ Nomi responded: {aiReply}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Failed to parse ModelScope response: {e.Message}\nResponse: {responseText}");
                    AddMessageToUI("assistant", "I understand. Could you tell me more about that?");
                }
            }
            else
            {
                Debug.LogError($"❌ ModelScope API request failed: {request.error}\nStatus: {request.responseCode}");
                AddMessageToUI("assistant", "I'm having trouble connecting right now, but I'm here for you. Please continue.");
            }
            
            isProcessing = false;
        }
        
        private string EscapeJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }
        
        /// <summary>
        /// Simple local sentiment analysis to determine anxiety level
        /// </summary>
        private void UpdateAnxietyLevel(string userMessage, string aiReply)
        {
            string lowerUser = userMessage.ToLower();
            
            // High anxiety keywords
            string[] severeKeywords = { "panic", "terrified", "overwhelming", "can't breathe", "death", "suicide", 
                                       "hopeless", "unbearable", "dying", "scared", "crisis" };
            
            // Moderate anxiety keywords
            string[] moderateKeywords = { "anxious", "worried", "nervous", "stressed", "fear", "concern", 
                                         "difficult", "struggle", "upset", "sad", "lonely" };
            
            // Check for severe anxiety
            foreach (string keyword in severeKeywords)
            {
                if (lowerUser.Contains(keyword))
                {
                    SetAnxietyLevel("severe");
                    UpdateNomiExpression("anxiety");
                    return;
                }
            }
            
            // Check for moderate anxiety
            foreach (string keyword in moderateKeywords)
            {
                if (lowerUser.Contains(keyword))
                {
                    SetAnxietyLevel("moderate");
                    UpdateNomiExpression("sad");
                    return;
                }
            }
            
            // Default to light anxiety if no keywords matched
            SetAnxietyLevel("light");
            UpdateNomiExpression("happy");
        }
        
        private void SetAnxietyLevel(string level)
        {
            PlayerPrefs.SetString("CurrentAnxietyLevel", level);
            PlayerPrefs.Save();
            Debug.Log($"🎭 Anxiety level set to: {level}");
        }
        
        private void UpdateNomiExpression(string expression)
        {
            if (mrController != null)
            {
                mrController.SetExpression(expression);
            }
        }
        
        private string BuildConversationContext()
        {
            string context = "";
            int startIndex = Mathf.Max(0, chatHistory.Count - 5);
            
            for (int i = startIndex; i < chatHistory.Count; i++)
            {
                string role = chatHistory[i].role == "user" ? "User" : "Nomi";
                context += $"{role}: {chatHistory[i].content}\n";
            }
            
            return context;
        }
        
        private void AddMessageToUI(string role, string content)
        {
            // Add to history
            chatHistory.Add(new ChatMessage
            {
                role = role,
                content = content,
                timestamp = DateTime.Now.ToString("HH:mm:ss")
            });
            
            // REMOVED: Chat UI display logic - messages are now shown via 3D SpeechBubbleController only
            // No longer creating message bubbles in 2D UI panel
            // The chat history is preserved in memory for context
        }
        
        // REMOVED: CreateMessageBubble method - no longer needed
        // Messages are displayed via 3D SpeechBubbleController instead of 2D UI bubbles
        // Chat history is maintained in memory only for conversation context
        
        private void ClearChatHistory()
        {
            chatHistory.Clear();
            
            // REMOVED: UI cleanup logic - no longer creating 2D message bubbles
            // Chat history clearing only affects the in-memory conversation context
        }
    }
    
    // ============================================================================
    // Data Structures for API
    // ============================================================================
    
    // ============================================================================
    // ModelScope API Data Structures
    // ============================================================================
    
    [Serializable]
    public class ModelScopeRequest
    {
        public string model;
        public ModelScopeMessage[] messages;
    }
    
    [Serializable]
    public class ModelScopeMessage
    {
        public string role;
        public string content;
    }
    
    [Serializable]
    public class ModelScopeResponse
    {
        public ModelScopeChoice[] choices;
    }
    
    [Serializable]
    public class ModelScopeChoice
    {
        public ModelScopeMessageContent message;
    }
    
    [Serializable]
    public class ModelScopeMessageContent
    {
        public string content;
    }
}

