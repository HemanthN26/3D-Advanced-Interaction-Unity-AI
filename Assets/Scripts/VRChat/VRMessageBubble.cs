using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRChat
{
    /// <summary>
    /// VR Message Bubble - Displays a chat message with styling for user or AI
    /// </summary>
    public class VRMessageBubble : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image bubbleBackground;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private TextMeshProUGUI senderLabel;

        [Header("Layout")]
        [SerializeField] private HorizontalLayoutGroup horizontalLayout;
        [SerializeField] private LayoutElement bubbleLayoutElement;
        [SerializeField] private ContentSizeFitter contentSizeFitter;

        [Header("Settings")]
        [SerializeField] private float maxBubbleWidth = 500f;
        [SerializeField] private float padding = 20f;

        [Header("Avatars")]
        [SerializeField] private Sprite userAvatarSprite;
        [SerializeField] private Sprite aiAvatarSprite;
        [SerializeField] private Color userAvatarColor = new Color(0.2f, 0.5f, 0.85f, 1f);
        [SerializeField] private Color aiAvatarColor = new Color(0.4f, 0.85f, 0.7f, 1f);

        private bool isUserMessage;

        /// <summary>
        /// Set the message content and styling
        /// </summary>
        public void SetMessage(string message, bool isUser, Color bubbleColor, Color textColor)
        {
            isUserMessage = isUser;

            // Set message text
            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = textColor;
            }

            // Set bubble background color
            if (bubbleBackground != null)
            {
                bubbleBackground.color = bubbleColor;
            }

            // Set avatar
            if (avatarImage != null)
            {
                if (isUser && userAvatarSprite != null)
                {
                    avatarImage.sprite = userAvatarSprite;
                }
                else if (!isUser && aiAvatarSprite != null)
                {
                    avatarImage.sprite = aiAvatarSprite;
                }
                
                avatarImage.color = isUser ? userAvatarColor : aiAvatarColor;
            }

            // Set sender label
            if (senderLabel != null)
            {
                senderLabel.text = isUser ? "You" : "AI";
                senderLabel.color = isUser ? userAvatarColor : aiAvatarColor;
            }

            // Configure layout alignment
            ConfigureAlignment(isUser);

            // Update layout
            UpdateLayout();
        }

        /// <summary>
        /// Append text to the message (useful for streaming responses)
        /// </summary>
        public void AppendText(string text)
        {
            if (messageText != null)
            {
                messageText.text += text;
                UpdateLayout();
            }
        }

        /// <summary>
        /// Get the current message text
        /// </summary>
        public string GetMessage()
        {
            return messageText != null ? messageText.text : "";
        }

        private void ConfigureAlignment(bool isUser)
        {
            if (horizontalLayout != null)
            {
                // User messages align right, AI messages align left
                horizontalLayout.childAlignment = isUser ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                horizontalLayout.reverseArrangement = isUser;
            }
        }

        private void UpdateLayout()
        {
            if (bubbleLayoutElement != null && messageText != null)
            {
                // Calculate preferred width based on text
                float preferredWidth = messageText.preferredWidth + padding * 2;
                bubbleLayoutElement.preferredWidth = Mathf.Min(preferredWidth, maxBubbleWidth);
            }

            // Force layout rebuild
            if (contentSizeFitter != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }

            // Also rebuild parent if exists
            var parentRect = transform.parent as RectTransform;
            if (parentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }

        private void OnEnable()
        {
            // Ensure layout is updated when enabled
            Invoke(nameof(UpdateLayout), 0.01f);
        }

        /// <summary>
        /// Auto-setup references if not assigned (editor utility)
        /// </summary>
        [ContextMenu("Auto Setup References")]
        private void AutoSetupReferences()
        {
            if (bubbleBackground == null)
                bubbleBackground = GetComponentInChildren<Image>();
            
            if (messageText == null)
                messageText = GetComponentInChildren<TextMeshProUGUI>();

            if (horizontalLayout == null)
                horizontalLayout = GetComponent<HorizontalLayoutGroup>();

            if (bubbleLayoutElement == null)
                bubbleLayoutElement = GetComponentInChildren<LayoutElement>();

            if (contentSizeFitter == null)
                contentSizeFitter = GetComponentInChildren<ContentSizeFitter>();

            Debug.Log("[VRMessageBubble] Auto-setup complete!");
        }
    }
}

