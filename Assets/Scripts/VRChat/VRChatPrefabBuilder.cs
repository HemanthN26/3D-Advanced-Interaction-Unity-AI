using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRChat
{
    /// <summary>
    /// Utility to create VR Chat message prefabs at runtime
    /// Attach to a GameObject and call BuildPrefabs() or use [ContextMenu] in editor
    /// </summary>
    public class VRChatPrefabBuilder : MonoBehaviour
    {
        [Header("Output References")]
        public GameObject userMessagePrefab;
        public GameObject aiMessagePrefab;
        public GameObject typingIndicatorPrefab;

        [Header("Theme Colors")]
        [SerializeField] private Color userBubbleColor = new Color(0.2f, 0.5f, 0.85f, 1f);
        [SerializeField] private Color aiBubbleColor = new Color(0.2f, 0.2f, 0.25f, 1f);
        [SerializeField] private Color textColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        [SerializeField] private Color userAvatarColor = new Color(0.3f, 0.6f, 0.9f, 1f);
        [SerializeField] private Color aiAvatarColor = new Color(0.4f, 0.85f, 0.7f, 1f);

        [Header("Sizing")]
        [SerializeField] private float maxBubbleWidth = 500f;
        [SerializeField] private float fontSize = 18f;
        [SerializeField] private float avatarSize = 40f;
        [SerializeField] private float bubblePadding = 16f;

        [Header("Font")]
        [SerializeField] private TMP_FontAsset font;

        private void Start()
        {
            // Auto-build prefabs on start if not already created
            if (userMessagePrefab == null || aiMessagePrefab == null)
            {
                BuildPrefabs();
            }
        }

        [ContextMenu("Build Chat Prefabs")]
        public void BuildPrefabs()
        {
            // Find font if not assigned
            if (font == null)
            {
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (font == null)
                {
                    TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts.Length > 0) font = fonts[0];
                }
            }

            // Create prefabs
            userMessagePrefab = CreateMessagePrefab("UserMessagePrefab", true);
            aiMessagePrefab = CreateMessagePrefab("AIMessagePrefab", false);
            typingIndicatorPrefab = CreateTypingIndicator();

            // Connect to VRChatController if present
            var chatController = GetComponent<VRChatController>();
            if (chatController != null)
            {
                SetChatControllerPrefabs(chatController);
            }

            Debug.Log("[VRChatPrefabBuilder] Prefabs created successfully!");
        }

        private GameObject CreateMessagePrefab(string name, bool isUser)
        {
            // Root container
            GameObject root = new GameObject(name);
            root.transform.SetParent(transform, false);
            root.SetActive(false); // Prefabs start inactive

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0, 0);

            // Layout group for row alignment
            HorizontalLayoutGroup hlg = root.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = isUser ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            hlg.reverseArrangement = isUser;
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            LayoutElement rootLayout = root.AddComponent<LayoutElement>();
            rootLayout.minHeight = avatarSize + 20f;
            rootLayout.flexibleWidth = 1;

            // Avatar
            GameObject avatar = CreateAvatar(root.transform, isUser);

            // Message bubble container
            GameObject bubble = CreateBubble(root.transform, isUser);

            // Add VRMessageBubble component and wire references
            VRMessageBubble messageBubble = root.AddComponent<VRMessageBubble>();
            SetupMessageBubbleReferences(messageBubble, root, bubble, avatar);

            return root;
        }

        private GameObject CreateAvatar(Transform parent, bool isUser)
        {
            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(parent, false);

            RectTransform rect = avatar.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(avatarSize, avatarSize);

            Image img = avatar.AddComponent<Image>();
            img.color = isUser ? userAvatarColor : aiAvatarColor;
            img.raycastTarget = false;

            LayoutElement layout = avatar.AddComponent<LayoutElement>();
            layout.preferredWidth = avatarSize;
            layout.preferredHeight = avatarSize;
            layout.flexibleWidth = 0;
            layout.flexibleHeight = 0;

            // Add label inside avatar
            GameObject label = new GameObject("Label");
            label.transform.SetParent(avatar.transform, false);

            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = isUser ? "U" : "AI";
            labelText.fontSize = avatarSize * 0.4f;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;
            labelText.raycastTarget = false;
            if (font != null) labelText.font = font;

            return avatar;
        }

        private GameObject CreateBubble(Transform parent, bool isUser)
        {
            // Bubble background
            GameObject bubble = new GameObject("Bubble");
            bubble.transform.SetParent(parent, false);

            RectTransform bubbleRect = bubble.AddComponent<RectTransform>();
            
            Image bubbleImg = bubble.AddComponent<Image>();
            bubbleImg.color = isUser ? userBubbleColor : aiBubbleColor;
            bubbleImg.raycastTarget = false;

            // Vertical layout for content
            VerticalLayoutGroup vlg = bubble.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset((int)bubblePadding, (int)bubblePadding, (int)bubblePadding * 2/3, (int)bubblePadding * 2/3);
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            LayoutElement bubbleLayout = bubble.AddComponent<LayoutElement>();
            bubbleLayout.preferredWidth = 200f;
            bubbleLayout.flexibleWidth = 0;

            ContentSizeFitter fitter = bubble.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Sender label
            GameObject senderLabel = new GameObject("SenderLabel");
            senderLabel.transform.SetParent(bubble.transform, false);

            TextMeshProUGUI senderText = senderLabel.AddComponent<TextMeshProUGUI>();
            senderText.text = isUser ? "You" : "AI Assistant";
            senderText.fontSize = fontSize * 0.7f;
            senderText.color = new Color(textColor.r, textColor.g, textColor.b, 0.7f);
            senderText.fontStyle = FontStyles.Bold;
            senderText.raycastTarget = false;
            if (font != null) senderText.font = font;

            // Message text
            GameObject messageObj = new GameObject("MessageText");
            messageObj.transform.SetParent(bubble.transform, false);

            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = "";
            messageText.fontSize = fontSize;
            messageText.color = textColor;
            messageText.enableWordWrapping = true;
            messageText.overflowMode = TextOverflowModes.Overflow;
            messageText.raycastTarget = false;
            if (font != null) messageText.font = font;

            LayoutElement textLayout = messageObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = maxBubbleWidth - bubblePadding * 2;
            textLayout.flexibleWidth = 0;

            return bubble;
        }

        private GameObject CreateTypingIndicator()
        {
            // Root
            GameObject indicator = new GameObject("TypingIndicator");
            indicator.transform.SetParent(transform, false);
            indicator.SetActive(false);

            RectTransform rootRect = indicator.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0, 0);

            HorizontalLayoutGroup hlg = indicator.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.spacing = 10f;
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            LayoutElement rootLayout = indicator.AddComponent<LayoutElement>();
            rootLayout.minHeight = avatarSize + 20f;

            // Avatar
            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(indicator.transform, false);

            RectTransform avatarRect = avatar.AddComponent<RectTransform>();
            avatarRect.sizeDelta = new Vector2(avatarSize, avatarSize);

            Image avatarImg = avatar.AddComponent<Image>();
            avatarImg.color = aiAvatarColor;

            LayoutElement avatarLayout = avatar.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = avatarSize;
            avatarLayout.preferredHeight = avatarSize;

            // Dots container
            GameObject dotsContainer = new GameObject("DotsContainer");
            dotsContainer.transform.SetParent(indicator.transform, false);

            RectTransform dotsRect = dotsContainer.AddComponent<RectTransform>();
            dotsRect.sizeDelta = new Vector2(60, 30);

            Image dotsImg = dotsContainer.AddComponent<Image>();
            dotsImg.color = aiBubbleColor;

            LayoutElement dotsLayout = dotsContainer.AddComponent<LayoutElement>();
            dotsLayout.preferredWidth = 60;
            dotsLayout.preferredHeight = 30;

            // Add typing indicator script
            VRTypingIndicator typingScript = indicator.AddComponent<VRTypingIndicator>();

            // Create dots
            HorizontalLayoutGroup dotsHlg = dotsContainer.AddComponent<HorizontalLayoutGroup>();
            dotsHlg.childAlignment = TextAnchor.MiddleCenter;
            dotsHlg.spacing = 6;
            dotsHlg.padding = new RectOffset(12, 12, 6, 6);
            dotsHlg.childControlWidth = false;
            dotsHlg.childControlHeight = false;

            Image[] dots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject dot = new GameObject($"Dot{i}");
                dot.transform.SetParent(dotsContainer.transform, false);

                RectTransform dotRect = dot.AddComponent<RectTransform>();
                dotRect.sizeDelta = new Vector2(8, 8);

                Image dotImg = dot.AddComponent<Image>();
                dotImg.color = textColor;

                LayoutElement dotLayout = dot.AddComponent<LayoutElement>();
                dotLayout.preferredWidth = 8;
                dotLayout.preferredHeight = 8;

                dots[i] = dotImg;
            }

            // Set dots on typing indicator using reflection
            var dotsField = typeof(VRTypingIndicator).GetField("dots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            dotsField?.SetValue(typingScript, dots);

            return indicator;
        }

        private void SetupMessageBubbleReferences(VRMessageBubble component, GameObject root, GameObject bubble, GameObject avatar)
        {
            var type = typeof(VRMessageBubble);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            // Set bubble background
            var bgField = type.GetField("bubbleBackground", flags);
            bgField?.SetValue(component, bubble.GetComponent<Image>());

            // Set message text
            var textField = type.GetField("messageText", flags);
            var messageText = bubble.transform.Find("MessageText")?.GetComponent<TextMeshProUGUI>();
            textField?.SetValue(component, messageText);

            // Set sender label
            var labelField = type.GetField("senderLabel", flags);
            var senderLabel = bubble.transform.Find("SenderLabel")?.GetComponent<TextMeshProUGUI>();
            labelField?.SetValue(component, senderLabel);

            // Set avatar
            var avatarField = type.GetField("avatarImage", flags);
            avatarField?.SetValue(component, avatar.GetComponent<Image>());

            // Set horizontal layout
            var hlgField = type.GetField("horizontalLayout", flags);
            hlgField?.SetValue(component, root.GetComponent<HorizontalLayoutGroup>());

            // Set bubble layout element
            var layoutField = type.GetField("bubbleLayoutElement", flags);
            layoutField?.SetValue(component, bubble.GetComponent<LayoutElement>());

            // Set content size fitter
            var fitterField = type.GetField("contentSizeFitter", flags);
            fitterField?.SetValue(component, bubble.GetComponent<ContentSizeFitter>());

            // Set max width
            var widthField = type.GetField("maxBubbleWidth", flags);
            widthField?.SetValue(component, maxBubbleWidth);
        }

        private void SetChatControllerPrefabs(VRChatController controller)
        {
            var type = typeof(VRChatController);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            var userPrefabField = type.GetField("userMessagePrefab", flags);
            userPrefabField?.SetValue(controller, userMessagePrefab);

            var aiPrefabField = type.GetField("aiMessagePrefab", flags);
            aiPrefabField?.SetValue(controller, aiMessagePrefab);

            var typingField = type.GetField("typingIndicatorPrefab", flags);
            typingField?.SetValue(controller, typingIndicatorPrefab);
        }
    }
}

