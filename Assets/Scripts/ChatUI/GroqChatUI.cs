using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using GroqApiLibrary;

namespace GroqChat.UI
{
    public class GroqChatUI : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string apiKey = "YOUR_API_KEY_HERE";
        [SerializeField] private string model = "llama-3.3-70b-versatile";
        [SerializeField] private string systemPrompt = "You are a helpful, friendly AI assistant. Be concise but thorough in your responses.";
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxTokens = 2048;

        [Header("UI References")]
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private RectTransform chatContent;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject aiMessagePrefab;
        [SerializeField] private GameObject typingIndicator;

        [Header("Appearance")]
        [SerializeField] private Color userBubbleColor = new Color(0.2f, 0.6f, 0.9f, 1f);
        [SerializeField] private Color aiBubbleColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        [SerializeField] private Color userTextColor = Color.white;
        [SerializeField] private Color aiTextColor = new Color(0.92f, 0.92f, 0.92f, 1f);

        private GroqApiClient groqApi;
        private List<JsonObject> messageHistory = new List<JsonObject>();
        private bool isProcessing = false;
        private ChatMessageBubble currentAIBubble;

        private void Start()
        {
            Debug.Log("[GroqChatUI] Starting...");
            InitializeAPI();
            SetupUI();
            AddSystemMessage();
            Debug.Log("[GroqChatUI] Ready! Type a message and click Send.");
        }

        private void InitializeAPI()
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            {
                Debug.LogError("[GroqChatUI] Please set your Groq API key!");
                return;
            }
            groqApi = new GroqApiClient(apiKey);
            Debug.Log("[GroqChatUI] API initialized with model: " + model);
        }

        private void SetupUI()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendClicked);
                Debug.Log("[GroqChatUI] Send button connected");
            }
            else
            {
                Debug.LogWarning("[GroqChatUI] Send button is null!");
            }

            if (inputField != null)
            {
                inputField.onSubmit.AddListener(OnInputSubmit);
                inputField.onEndEdit.AddListener(OnEndEdit);
                // Delay activation to ensure UI is fully ready
                StartCoroutine(DelayedFocusInput());
                Debug.Log("[GroqChatUI] Input field connected");
            }
            else
            {
                Debug.LogWarning("[GroqChatUI] Input field is null!");
            }

            if (typingIndicator != null)
                typingIndicator.SetActive(false);
            
            if (chatContent == null)
                Debug.LogWarning("[GroqChatUI] Chat content is null!");
            
            if (userMessagePrefab == null)
                Debug.LogWarning("[GroqChatUI] User message prefab is null!");
                
            if (aiMessagePrefab == null)
                Debug.LogWarning("[GroqChatUI] AI message prefab is null!");
        }

        private void AddSystemMessage()
        {
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messageHistory.Add(new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = systemPrompt
                });
            }
        }

        private IEnumerator DelayedFocusInput()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            if (inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
                Debug.Log("[GroqChatUI] Input field focused");
            }
        }

        private void OnInputSubmit(string text)
        {
            // This is called when Enter is pressed
            if (!string.IsNullOrEmpty(text))
            {
                SendMessage();
            }
        }

        private void OnEndEdit(string text)
        {
            // Check if Enter was pressed (not just losing focus)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    SendMessage();
                }
            }
        }

        private void OnSendClicked()
        {
            SendMessage();
        }

        public void SendMessage()
        {
            Debug.Log("[GroqChatUI] SendMessage called");
            
            if (isProcessing) 
            {
                Debug.Log("[GroqChatUI] Still processing, ignoring...");
                return;
            }

            if (inputField == null)
            {
                Debug.LogError("[GroqChatUI] Input field is null!");
                return;
            }

            string userMessage = inputField.text.Trim();
            Debug.Log("[GroqChatUI] User message: " + userMessage);
            
            if (string.IsNullOrEmpty(userMessage)) 
            {
                Debug.Log("[GroqChatUI] Empty message, ignoring...");
                return;
            }

            // Clear input
            inputField.text = "";
            inputField.ActivateInputField();

            // Add user message to UI
            Debug.Log("[GroqChatUI] Creating user bubble...");
            CreateUserBubble(userMessage);

            // Add to history
            messageHistory.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = userMessage
            });

            // Send to API
            Debug.Log("[GroqChatUI] Starting API request coroutine...");
            StartCoroutine(SendChatRequest());
        }

        private void CreateUserBubble(string message)
        {
            if (userMessagePrefab == null) 
            {
                Debug.LogError("[GroqChatUI] userMessagePrefab is null!");
                return;
            }

            GameObject bubble = Instantiate(userMessagePrefab, chatContent);
            bubble.SetActive(true); // Activate the instantiated bubble
            bubble.transform.localScale = Vector3.one;
            
            ChatMessageBubble messageBubble = bubble.GetComponent<ChatMessageBubble>();
            if (messageBubble != null)
            {
                messageBubble.SetMessage(message, true, userBubbleColor, userTextColor);
            }

            ScrollToBottom();
        }

        private ChatMessageBubble CreateAIBubble()
        {
            if (aiMessagePrefab == null) 
            {
                Debug.LogError("[GroqChatUI] aiMessagePrefab is null!");
                return null;
            }

            GameObject bubble = Instantiate(aiMessagePrefab, chatContent);
            bubble.SetActive(true); // Activate the instantiated bubble
            bubble.transform.localScale = Vector3.one;
            
            ChatMessageBubble messageBubble = bubble.GetComponent<ChatMessageBubble>();
            if (messageBubble != null)
            {
                messageBubble.SetMessage("", false, aiBubbleColor, aiTextColor);
            }

            ScrollToBottom();
            return messageBubble;
        }

        private IEnumerator SendChatRequest()
        {
            isProcessing = true;
            Debug.Log("[GroqChatUI] Sending message to API...");

            // Show typing indicator
            if (typingIndicator != null)
                typingIndicator.SetActive(true);

            // Disable input
            if (sendButton != null)
                sendButton.interactable = false;

            // Create AI bubble for streaming
            currentAIBubble = CreateAIBubble();

            // Build messages array
            var messagesArray = new JsonArray();
            foreach (var msg in messageHistory)
            {
                var clonedMsg = JsonNode.Parse(msg.ToJsonString())!.AsObject();
                messagesArray.Add(clonedMsg);
            }

            var request = new JsonObject
            {
                ["model"] = model,
                ["messages"] = messagesArray,
                ["temperature"] = temperature,
                ["max_completion_tokens"] = maxTokens
            };

            string fullResponse = "";
            bool hasError = false;

            // Try non-streaming first for reliability
            System.Threading.Tasks.Task<JsonObject> apiTask = null;
            
            try
            {
                Debug.Log("[GroqChatUI] Calling Groq API...");
                apiTask = groqApi.CreateChatCompletionAsync(request);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[GroqChatUI] Failed to start API call: " + e.Message);
                hasError = true;
            }

            if (apiTask != null)
            {
                // Wait for task to complete
                while (!apiTask.IsCompleted)
                {
                    yield return null;
                }

                if (apiTask.IsFaulted)
                {
                    Debug.LogError("[GroqChatUI] API Error: " + apiTask.Exception?.InnerException?.Message);
                    hasError = true;
                    
                    if (currentAIBubble != null)
                    {
                        currentAIBubble.SetMessage("Error: " + apiTask.Exception?.InnerException?.Message, false, aiBubbleColor, aiTextColor);
                    }
                }
                else if (apiTask.IsCompletedSuccessfully)
                {
                    var result = apiTask.Result;
                    Debug.Log("[GroqChatUI] Got response from API");
                    
                    try
                    {
                        fullResponse = result?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
                        Debug.Log("[GroqChatUI] Response: " + (fullResponse.Length > 100 ? fullResponse.Substring(0, 100) + "..." : fullResponse));
                        
                        if (currentAIBubble != null && !string.IsNullOrEmpty(fullResponse))
                        {
                            currentAIBubble.SetMessage(fullResponse, false, aiBubbleColor, aiTextColor);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[GroqChatUI] Error parsing response: " + e.Message);
                        Debug.LogError("[GroqChatUI] Raw response: " + result?.ToJsonString());
                        hasError = true;
                    }
                }
            }

            // Add to history
            if (!string.IsNullOrEmpty(fullResponse) && !hasError)
            {
                messageHistory.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = fullResponse
                });
            }

            // Hide typing indicator
            if (typingIndicator != null)
                typingIndicator.SetActive(false);

            // Re-enable input
            if (sendButton != null)
                sendButton.interactable = true;

            isProcessing = false;
            currentAIBubble = null;

            ScrollToBottom();

            if (inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            StartCoroutine(ScrollToBottomDelayed());
        }

        private IEnumerator ScrollToBottomDelayed()
        {
            yield return new WaitForEndOfFrame();
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        public void ClearChat()
        {
            // Clear UI
            foreach (Transform child in chatContent)
            {
                Destroy(child.gameObject);
            }

            // Clear history but keep system message
            messageHistory.Clear();
            AddSystemMessage();
        }

        private void OnDestroy()
        {
            groqApi?.Dispose();
        }
    }
}

