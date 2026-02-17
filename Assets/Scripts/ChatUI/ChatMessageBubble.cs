using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GroqChat.UI
{
    public class ChatMessageBubble : MonoBehaviour
    {
        [SerializeField] private Image bubbleBackground;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image avatarImage;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private HorizontalLayoutGroup horizontalLayout;
        [SerializeField] private ContentSizeFitter contentFitter;

        [Header("Avatar Sprites")]
        [SerializeField] private Sprite userAvatar;
        [SerializeField] private Sprite aiAvatar;

        [Header("Settings")]
        [SerializeField] private float maxWidth = 600f;
        [SerializeField] private float padding = 20f;

        private bool isUserMessage;

        public void SetMessage(string message, bool isUser, Color bubbleColor, Color textColor)
        {
            isUserMessage = isUser;

            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = textColor;
            }

            if (bubbleBackground != null)
            {
                bubbleBackground.color = bubbleColor;
            }

            if (avatarImage != null)
            {
                avatarImage.sprite = isUser ? userAvatar : aiAvatar;
            }

            // Align bubble based on sender
            if (horizontalLayout != null)
            {
                if (isUser)
                {
                    horizontalLayout.childAlignment = TextAnchor.UpperRight;
                    horizontalLayout.reverseArrangement = true;
                }
                else
                {
                    horizontalLayout.childAlignment = TextAnchor.UpperLeft;
                    horizontalLayout.reverseArrangement = false;
                }
            }

            // Update layout
            UpdateLayout();
        }

        public void AppendText(string text)
        {
            if (messageText != null)
            {
                messageText.text += text;
                UpdateLayout();
            }
        }

        private void UpdateLayout()
        {
            if (layoutElement != null && messageText != null)
            {
                // Calculate preferred width
                float preferredWidth = messageText.preferredWidth + padding * 2;
                layoutElement.preferredWidth = Mathf.Min(preferredWidth, maxWidth);
            }

            // Force layout rebuild
            if (contentFitter != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
            }
        }

        private void OnEnable()
        {
            UpdateLayout();
        }
    }
}

