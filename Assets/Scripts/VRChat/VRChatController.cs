using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using GroqApiLibrary;

namespace VRChat
{
    /// <summary>
    /// VR Chat Controller - Manages chat conversations with Groq AI in VR
    /// Attach to an empty GameObject in your scene and assign UI references
    /// </summary>
    public class VRChatController : MonoBehaviour
    {
        [Header("Groq API Configuration")]
        [SerializeField] private string apiKey = "YOUR_GROQ_API_KEY";
        [SerializeField] private string model = "llama-3.3-70b-versatile";
        [SerializeField] [TextArea(3, 5)] private string systemPrompt = "You are a helpful, friendly AI assistant in a VR environment. Be concise but thorough. Keep responses under 100 words unless asked for more detail.";
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private int maxTokens = 1024;

        [Header("UI References")]
        [SerializeField] private ScrollRect chatScrollRect;
        [SerializeField] private RectTransform contentContainer;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Message Prefabs")]
        [SerializeField] private GameObject userMessagePrefab;
        [SerializeField] private GameObject aiMessagePrefab;
        [SerializeField] private GameObject typingIndicatorPrefab;

        [Header("Appearance Settings")]
        [SerializeField] private Color userBubbleColor = new Color(0.2f, 0.5f, 0.85f, 1f);
        [SerializeField] private Color aiBubbleColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        [SerializeField] private Color userTextColor = Color.white;
        [SerializeField] private Color aiTextColor = new Color(0.95f, 0.95f, 0.95f, 1f);

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip sendSound;
        [SerializeField] private AudioClip receiveSound;

        // Events for TTS integration
        public System.Action<string> OnAIResponseReceived;
        public System.Action<string> OnUserMessageSent;

        private GroqApiClient groqApi;
        private List<JsonObject> conversationHistory = new List<JsonObject>();
        private bool isProcessing = false;
        private GameObject currentTypingIndicator;

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            InitializeAPI();
            SetupUI();
            InitializeConversation();
            Debug.Log("[VRChatController] Ready for VR chat!");
        }

        private void OnDestroy()
        {
            groqApi?.Dispose();
        }

        #endregion

        #region Initialization

        private void ValidateReferences()
        {
            if (chatScrollRect == null) Debug.LogWarning("[VRChatController] Chat ScrollRect not assigned!");
            if (contentContainer == null) Debug.LogWarning("[VRChatController] Content Container not assigned!");
            if (inputField == null) Debug.LogWarning("[VRChatController] Input Field not assigned!");
            if (sendButton == null) Debug.LogWarning("[VRChatController] Send Button not assigned!");
            if (userMessagePrefab == null) Debug.LogWarning("[VRChatController] User Message Prefab not assigned!");
            if (aiMessagePrefab == null) Debug.LogWarning("[VRChatController] AI Message Prefab not assigned!");
        }

        private void InitializeAPI()
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GROQ_API_KEY")
            {
                Debug.LogError("[VRChatController] Please set your Groq API key in the Inspector!");
                return;
            }

            groqApi = new GroqApiClient(apiKey);
            Debug.Log($"[VRChatController] Groq API initialized with model: {model}");
        }

        private void SetupUI()
        {
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(OnSendButtonClicked);
            }

            if (inputField != null)
            {
                inputField.onSubmit.AddListener(OnInputSubmit);
            }
        }

        private void InitializeConversation()
        {
            conversationHistory.Clear();
            
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                conversationHistory.Add(new JsonObject
                {
                    ["role"] = "system",
                    ["content"] = systemPrompt
                });
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Send a message to the AI (can be called from voice input or UI)
        /// </summary>
        public void SendMessage(string message)
        {
            if (isProcessing || string.IsNullOrWhiteSpace(message)) return;

            // Clear input if using UI
            if (inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            // Play send sound
            if (audioSource != null && sendSound != null)
            {
                audioSource.PlayOneShot(sendSound);
            }

            // Create user message bubble
            CreateUserBubble(message);

            // Add to conversation history
            conversationHistory.Add(new JsonObject
            {
                ["role"] = "user",
                ["content"] = message
            });

            // Notify listeners
            OnUserMessageSent?.Invoke(message);

            // Send to API
            StartCoroutine(ProcessChatRequest());
        }

        /// <summary>
        /// Clear the conversation and UI
        /// </summary>
        public void ClearConversation()
        {
            // Clear UI
            if (contentContainer != null)
            {
                foreach (Transform child in contentContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Reset conversation
            InitializeConversation();

            Debug.Log("[VRChatController] Conversation cleared");
        }

        /// <summary>
        /// Set a new system prompt (for changing AI personality)
        /// </summary>
        public void SetSystemPrompt(string newPrompt)
        {
            systemPrompt = newPrompt;
            InitializeConversation();
        }

        #endregion

        #region UI Event Handlers

        private void OnSendButtonClicked()
        {
            if (inputField != null && !string.IsNullOrWhiteSpace(inputField.text))
            {
                SendMessage(inputField.text);
            }
        }

        private void OnInputSubmit(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                SendMessage(text);
            }
        }

        #endregion

        #region Message Bubble Creation

        private void CreateUserBubble(string message)
        {
            if (userMessagePrefab == null || contentContainer == null) return;

            GameObject bubble = Instantiate(userMessagePrefab, contentContainer);
            bubble.SetActive(true);

            var messageBubble = bubble.GetComponent<VRMessageBubble>();
            if (messageBubble != null)
            {
                messageBubble.SetMessage(message, true, userBubbleColor, userTextColor);
            }
            else
            {
                // Fallback: try to find TMP text directly
                var tmpText = bubble.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = message;
            }

            ScrollToBottom();
        }

        private VRMessageBubble CreateAIBubble()
        {
            if (aiMessagePrefab == null || contentContainer == null) return null;

            GameObject bubble = Instantiate(aiMessagePrefab, contentContainer);
            bubble.SetActive(true);

            var messageBubble = bubble.GetComponent<VRMessageBubble>();
            if (messageBubble != null)
            {
                messageBubble.SetMessage("", false, aiBubbleColor, aiTextColor);
            }

            ScrollToBottom();
            return messageBubble;
        }

        private void ShowTypingIndicator()
        {
            if (typingIndicatorPrefab != null && contentContainer != null)
            {
                currentTypingIndicator = Instantiate(typingIndicatorPrefab, contentContainer);
                currentTypingIndicator.SetActive(true);
                ScrollToBottom();
            }
        }

        private void HideTypingIndicator()
        {
            if (currentTypingIndicator != null)
            {
                Destroy(currentTypingIndicator);
                currentTypingIndicator = null;
            }
        }

        #endregion

        #region API Communication

        private IEnumerator ProcessChatRequest()
        {
            isProcessing = true;

            // Disable send button
            if (sendButton != null) sendButton.interactable = false;

            // Show typing indicator
            ShowTypingIndicator();

            // Build request
            var messagesArray = new JsonArray();
            foreach (var msg in conversationHistory)
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

            // Make API call
            System.Threading.Tasks.Task<JsonObject> apiTask = null;
            string responseText = "";
            bool hasError = false;

            try
            {
                apiTask = groqApi.CreateChatCompletionAsync(request);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VRChatController] API call failed: {e.Message}");
                hasError = true;
            }

            if (apiTask != null)
            {
                // Wait for completion
                while (!apiTask.IsCompleted)
                {
                    yield return null;
                }

                if (apiTask.IsFaulted)
                {
                    Debug.LogError($"[VRChatController] API Error: {apiTask.Exception?.InnerException?.Message}");
                    responseText = "Sorry, I encountered an error. Please try again.";
                    hasError = true;
                }
                else if (apiTask.IsCompletedSuccessfully)
                {
                    var result = apiTask.Result;
                    responseText = result?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
                    Debug.Log($"[VRChatController] Response received: {(responseText.Length > 50 ? responseText.Substring(0, 50) + "..." : responseText)}");
                }
            }

            // Hide typing indicator
            HideTypingIndicator();

            // Create AI bubble with response
            if (!string.IsNullOrEmpty(responseText))
            {
                var aiBubble = CreateAIBubble();
                if (aiBubble != null)
                {
                    aiBubble.SetMessage(responseText, false, aiBubbleColor, aiTextColor);
                }

                // Add to history if no error
                if (!hasError)
                {
                    conversationHistory.Add(new JsonObject
                    {
                        ["role"] = "assistant",
                        ["content"] = responseText
                    });
                }

                // Play receive sound
                if (audioSource != null && receiveSound != null)
                {
                    audioSource.PlayOneShot(receiveSound);
                }

                // Notify listeners (for TTS)
                OnAIResponseReceived?.Invoke(responseText);
            }

            // Re-enable send button
            if (sendButton != null) sendButton.interactable = true;

            isProcessing = false;

            // Refocus input field
            if (inputField != null)
            {
                inputField.Select();
                inputField.ActivateInputField();
            }
        }

        #endregion

        #region Helpers

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

        #endregion
    }
}

