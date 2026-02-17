using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

namespace GroqChat.UI
{
    /// <summary>
    /// Automatically builds a ChatGPT-style UI when attached to a scene.
    /// Attach this to an empty GameObject and it will create the entire UI hierarchy.
    /// </summary>
    public class ChatUIBuilder : MonoBehaviour
    {
        [Header("API Settings")]
        [SerializeField] private string apiKey = "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";
        [SerializeField] private string model = "llama-3.3-70b-versatile";

        [Header("Theme Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.13f, 0.13f, 0.14f, 1f);
        [SerializeField] private Color sidebarColor = new Color(0.1f, 0.1f, 0.11f, 1f);
        [SerializeField] private Color inputBackgroundColor = new Color(0.22f, 0.22f, 0.24f, 1f);
        [SerializeField] private Color userBubbleColor = new Color(0.22f, 0.58f, 0.85f, 1f);
        [SerializeField] private Color aiBubbleColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        [SerializeField] private Color accentColor = new Color(0.4f, 0.85f, 0.7f, 1f);
        [SerializeField] private Color textColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        [SerializeField] private Color placeholderColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        [Header("Font Settings")]
        [SerializeField] private TMP_FontAsset font;

        // Created references
        private Canvas mainCanvas;
        private GroqChatUI chatUI;
        private GameObject userMessagePrefab;
        private GameObject aiMessagePrefab;
        private GameObject typingIndicator;

        private void Start()
        {
            BuildUI();
        }

        [ContextMenu("Build Chat UI")]
        public void BuildUI()
        {
            Debug.Log("[ChatUIBuilder] Starting UI build...");

            // Find or load default font
            if (font == null)
            {
                font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (font == null)
                {
                    // Try to find any TMP font asset
                    font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
                }
                if (font == null)
                {
                    TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                    if (fonts.Length > 0) font = fonts[0];
                }
                
                if (font == null)
                {
                    Debug.LogWarning("[ChatUIBuilder] No TMP Font found! Please import TextMesh Pro Essential Resources (Window > TextMeshPro > Import TMP Essential Resources)");
                }
            }

            // Ensure EventSystem exists
            EnsureEventSystem();
            
            CreateCanvas();
            CreateMainLayout();
            CreateMessagePrefabs();
            SetupChatUI();

            Debug.Log("[ChatUIBuilder] UI build complete!");
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                Debug.Log("[ChatUIBuilder] Created EventSystem");
            }
        }

        private void CreateCanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("ChatCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = Vector3.zero;
            canvasObj.transform.localScale = Vector3.one;

            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100; // Ensure it's on top
            mainCanvas.targetDisplay = 0; // Display 1 (index 0)

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure there's a camera in the scene
            EnsureCamera();

            Debug.Log("[ChatUIBuilder] Canvas created");
        }

        private void EnsureCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                // No main camera found, create one
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = backgroundColor;
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5;
                mainCam.nearClipPlane = 0.3f;
                mainCam.farClipPlane = 1000f;
                mainCam.targetDisplay = 0; // Display 1
                camObj.AddComponent<AudioListener>();
                Debug.Log("[ChatUIBuilder] Created Main Camera");
            }
            else
            {
                // Set camera background to match our theme
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = backgroundColor;
                mainCam.targetDisplay = 0; // Ensure Display 1
                Debug.Log("[ChatUIBuilder] Using existing Main Camera");
            }
        }

        private void CreateMainLayout()
        {
            // Main Background
            GameObject background = CreatePanel("Background", mainCanvas.transform);
            background.GetComponent<Image>().color = backgroundColor;
            RectTransform bgRect = background.GetComponent<RectTransform>();
            StretchToFill(bgRect);

            // Main Container (horizontal layout)
            GameObject mainContainer = CreatePanel("MainContainer", background.transform);
            mainContainer.GetComponent<Image>().color = Color.clear;
            StretchToFill(mainContainer.GetComponent<RectTransform>());
            HorizontalLayoutGroup mainLayout = mainContainer.AddComponent<HorizontalLayoutGroup>();
            mainLayout.spacing = 0;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = false;
            mainLayout.childForceExpandHeight = true;

            // Sidebar
            CreateSidebar(mainContainer.transform);

            // Chat Area
            CreateChatArea(mainContainer.transform);
        }

        private void CreateSidebar(Transform parent)
        {
            GameObject sidebar = CreatePanel("Sidebar", parent);
            Image sidebarImg = sidebar.GetComponent<Image>();
            sidebarImg.color = sidebarColor;

            LayoutElement sidebarLayout = sidebar.AddComponent<LayoutElement>();
            sidebarLayout.preferredWidth = 260;
            sidebarLayout.flexibleWidth = 0;

            VerticalLayoutGroup sidebarVL = sidebar.AddComponent<VerticalLayoutGroup>();
            sidebarVL.padding = new RectOffset(16, 16, 20, 20);
            sidebarVL.spacing = 12;
            sidebarVL.childControlWidth = true;
            sidebarVL.childControlHeight = false;
            sidebarVL.childForceExpandWidth = true;
            sidebarVL.childForceExpandHeight = false;

            // New Chat Button
            GameObject newChatBtn = CreateButton("NewChatButton", sidebar.transform, "+ New Chat", accentColor);
            LayoutElement btnLayout = newChatBtn.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 50;

            // Title
            GameObject titleText = CreateText("Title", sidebar.transform, "Groq Chat", 24, FontStyles.Bold);
            LayoutElement titleLayout = titleText.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 40;

            // Model info
            GameObject modelText = CreateText("ModelInfo", sidebar.transform, $"Model: {model}", 12, FontStyles.Normal);
            modelText.GetComponent<TextMeshProUGUI>().color = placeholderColor;

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(sidebar.transform, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.localScale = Vector3.one;
            LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleHeight = 1;

            // Footer
            GameObject footer = CreateText("Footer", sidebar.transform, "Powered by Groq & Llama", 11, FontStyles.Italic);
            footer.GetComponent<TextMeshProUGUI>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
            footer.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        private void CreateChatArea(Transform parent)
        {
            GameObject chatArea = CreatePanel("ChatArea", parent);
            chatArea.GetComponent<Image>().color = Color.clear;

            LayoutElement chatAreaLayout = chatArea.AddComponent<LayoutElement>();
            chatAreaLayout.flexibleWidth = 1;

            VerticalLayoutGroup chatVL = chatArea.AddComponent<VerticalLayoutGroup>();
            chatVL.padding = new RectOffset(0, 0, 0, 0);
            chatVL.spacing = 0;
            chatVL.childControlWidth = true;
            chatVL.childControlHeight = true;
            chatVL.childForceExpandWidth = true;
            chatVL.childForceExpandHeight = false;

            // Header
            CreateChatHeader(chatArea.transform);

            // Messages ScrollView
            CreateMessagesArea(chatArea.transform);

            // Input Area
            CreateInputArea(chatArea.transform);
        }

        private void CreateChatHeader(Transform parent)
        {
            GameObject header = CreatePanel("Header", parent);
            header.GetComponent<Image>().color = new Color(backgroundColor.r * 0.9f, backgroundColor.g * 0.9f, backgroundColor.b * 0.9f, 1f);

            LayoutElement headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 60;
            headerLayout.flexibleHeight = 0;

            HorizontalLayoutGroup headerHL = header.AddComponent<HorizontalLayoutGroup>();
            headerHL.padding = new RectOffset(30, 30, 15, 15);
            headerHL.childAlignment = TextAnchor.MiddleLeft;

            // Header title
            GameObject headerTitle = CreateText("HeaderTitle", header.transform, "Chat with AI", 20, FontStyles.Bold);
            headerTitle.GetComponent<TextMeshProUGUI>().color = textColor;

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header.transform, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.localScale = Vector3.one;
            LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // Clear button
            GameObject clearBtn = CreateButton("ClearButton", header.transform, "Clear", new Color(0.6f, 0.3f, 0.3f, 1f));
            LayoutElement clearLayout = clearBtn.AddComponent<LayoutElement>();
            clearLayout.preferredWidth = 80;
            clearLayout.preferredHeight = 36;
        }

        private ScrollRect scrollRect;
        private RectTransform chatContent;

        private void CreateMessagesArea(Transform parent)
        {
            // Scroll View Container
            GameObject scrollContainer = CreatePanel("MessagesContainer", parent);
            scrollContainer.GetComponent<Image>().color = Color.clear;

            LayoutElement scrollLayout = scrollContainer.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1;

            // Scroll View
            GameObject scrollView = CreatePanel("ScrollView", scrollContainer.transform);
            scrollView.GetComponent<Image>().color = Color.clear;
            StretchToFill(scrollView.GetComponent<RectTransform>());

            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 30f;

            // Viewport
            GameObject viewport = CreatePanel("Viewport", scrollView.transform);
            viewport.GetComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            StretchToFill(viewportRect);
            viewportRect.offsetMin = new Vector2(60, 0);
            viewportRect.offsetMax = new Vector2(-60, 0);

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            chatContent = content.AddComponent<RectTransform>();
            chatContent.localScale = Vector3.one;
            chatContent.anchorMin = new Vector2(0, 1);
            chatContent.anchorMax = new Vector2(1, 1);
            chatContent.pivot = new Vector2(0.5f, 1);
            chatContent.anchoredPosition = Vector2.zero;
            chatContent.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentVL = content.AddComponent<VerticalLayoutGroup>();
            contentVL.padding = new RectOffset(0, 0, 20, 20);
            contentVL.spacing = 16;
            contentVL.childAlignment = TextAnchor.UpperCenter;
            contentVL.childControlWidth = true;
            contentVL.childControlHeight = true;
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = chatContent;

            // Scrollbar
            GameObject scrollbar = CreateScrollbar(scrollView.transform);
            scrollRect.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Create typing indicator
            CreateTypingIndicator(content.transform);
        }

        private void CreateTypingIndicator(Transform parent)
        {
            GameObject indicator = CreatePanel("TypingIndicator", parent);
            indicator.GetComponent<Image>().color = Color.clear;

            HorizontalLayoutGroup indicatorHL = indicator.AddComponent<HorizontalLayoutGroup>();
            indicatorHL.padding = new RectOffset(10, 10, 10, 10);
            indicatorHL.spacing = 8;
            indicatorHL.childAlignment = TextAnchor.MiddleLeft;
            indicatorHL.childControlWidth = false;
            indicatorHL.childControlHeight = false;

            LayoutElement indicatorLayout = indicator.AddComponent<LayoutElement>();
            indicatorLayout.preferredHeight = 50;

            // AI Avatar
            GameObject avatar = CreatePanel("Avatar", indicator.transform);
            avatar.GetComponent<Image>().color = aiBubbleColor;
            RectTransform avatarRect = avatar.GetComponent<RectTransform>();
            avatarRect.sizeDelta = new Vector2(36, 36);
            LayoutElement avatarLayout = avatar.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = 36;
            avatarLayout.preferredHeight = 36;

            // Dots container
            GameObject dotsContainer = CreatePanel("DotsContainer", indicator.transform);
            dotsContainer.GetComponent<Image>().color = aiBubbleColor;
            dotsContainer.GetComponent<Image>().type = Image.Type.Sliced;
            RectTransform dotsRect = dotsContainer.GetComponent<RectTransform>();
            dotsRect.sizeDelta = new Vector2(60, 36);
            LayoutElement dotsLayout = dotsContainer.AddComponent<LayoutElement>();
            dotsLayout.preferredWidth = 60;
            dotsLayout.preferredHeight = 36;

            HorizontalLayoutGroup dotsHL = dotsContainer.AddComponent<HorizontalLayoutGroup>();
            dotsHL.padding = new RectOffset(12, 12, 10, 10);
            dotsHL.spacing = 6;
            dotsHL.childAlignment = TextAnchor.MiddleCenter;
            dotsHL.childControlWidth = false;
            dotsHL.childControlHeight = false;

            // Create 3 dots
            for (int i = 0; i < 3; i++)
            {
                GameObject dot = CreatePanel($"Dot{i}", dotsContainer.transform);
                dot.GetComponent<Image>().color = textColor;
                LayoutElement dotLayout = dot.AddComponent<LayoutElement>();
                dotLayout.preferredWidth = 8;
                dotLayout.preferredHeight = 8;

                // Add animation
                TypingDotAnimator animator = dot.AddComponent<TypingDotAnimator>();
                animator.delay = i * 0.15f;
            }

            typingIndicator = indicator;
            indicator.SetActive(false);
        }

        private TMP_InputField inputFieldRef;
        private Button sendButtonRef;
        private Button clearButtonRef;
        private Button newChatButtonRef;

        private void CreateInputArea(Transform parent)
        {
            GameObject inputArea = CreatePanel("InputArea", parent);
            inputArea.GetComponent<Image>().color = new Color(backgroundColor.r * 0.85f, backgroundColor.g * 0.85f, backgroundColor.b * 0.85f, 1f);

            LayoutElement inputAreaLayout = inputArea.AddComponent<LayoutElement>();
            inputAreaLayout.preferredHeight = 100;
            inputAreaLayout.flexibleHeight = 0;

            HorizontalLayoutGroup inputHL = inputArea.AddComponent<HorizontalLayoutGroup>();
            inputHL.padding = new RectOffset(80, 80, 20, 30);
            inputHL.spacing = 16;
            inputHL.childAlignment = TextAnchor.MiddleCenter;
            inputHL.childControlWidth = true;
            inputHL.childControlHeight = true;
            inputHL.childForceExpandWidth = false;
            inputHL.childForceExpandHeight = false;

            // Input Field Container
            GameObject inputContainer = CreatePanel("InputContainer", inputArea.transform);
            inputContainer.GetComponent<Image>().color = inputBackgroundColor;
            inputContainer.GetComponent<Image>().type = Image.Type.Sliced;

            LayoutElement inputContainerLayout = inputContainer.AddComponent<LayoutElement>();
            inputContainerLayout.flexibleWidth = 1;
            inputContainerLayout.preferredHeight = 50;

            // Add rounded corners effect via child padding
            HorizontalLayoutGroup inputContainerHL = inputContainer.AddComponent<HorizontalLayoutGroup>();
            inputContainerHL.padding = new RectOffset(20, 20, 0, 0);
            inputContainerHL.childAlignment = TextAnchor.MiddleCenter;
            inputContainerHL.childControlWidth = true;
            inputContainerHL.childControlHeight = true;
            inputContainerHL.childForceExpandWidth = true;
            inputContainerHL.childForceExpandHeight = true;

            // TMP Input Field - Using proper setup
            GameObject inputFieldObj = new GameObject("InputField");
            inputFieldObj.transform.SetParent(inputContainer.transform, false);
            RectTransform inputFieldRect = inputFieldObj.AddComponent<RectTransform>();
            inputFieldRect.localScale = Vector3.one;
            StretchToFill(inputFieldRect);

            // Add Image for interaction
            Image inputFieldImage = inputFieldObj.AddComponent<Image>();
            inputFieldImage.color = Color.clear; // Transparent but allows raycasting

            // Text Area (viewport)
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(inputFieldObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.localScale = Vector3.one;
            StretchToFill(textAreaRect);
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -6);
            
            // Add RectMask2D to clip text
            textArea.AddComponent<RectMask2D>();

            // Input Text (must be created BEFORE placeholder for TMP_InputField)
            GameObject inputText = new GameObject("Text");
            inputText.transform.SetParent(textArea.transform, false);
            RectTransform inputTextRect = inputText.AddComponent<RectTransform>();
            inputTextRect.localScale = Vector3.one;
            StretchToFill(inputTextRect);
            
            TextMeshProUGUI inputTMP = inputText.AddComponent<TextMeshProUGUI>();
            inputTMP.text = "";
            inputTMP.fontSize = 16;
            inputTMP.color = textColor;
            inputTMP.alignment = TextAlignmentOptions.Left;
            inputTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            inputTMP.raycastTarget = false;
            if (font != null) inputTMP.font = font;

            // Placeholder
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.localScale = Vector3.one;
            StretchToFill(placeholderRect);
            
            TextMeshProUGUI placeholderTMP = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = "Type a message...";
            placeholderTMP.fontSize = 16;
            placeholderTMP.fontStyle = FontStyles.Italic;
            placeholderTMP.color = placeholderColor;
            placeholderTMP.alignment = TextAlignmentOptions.Left;
            placeholderTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            placeholderTMP.raycastTarget = false;
            if (font != null) placeholderTMP.font = font;

            // Setup TMP_InputField AFTER text components are ready
            inputFieldRef = inputFieldObj.AddComponent<TMP_InputField>();
            inputFieldRef.textViewport = textAreaRect;
            inputFieldRef.textComponent = inputTMP;
            inputFieldRef.placeholder = placeholderTMP;
            inputFieldRef.fontAsset = font;
            inputFieldRef.pointSize = 16;
            inputFieldRef.caretColor = accentColor;
            inputFieldRef.caretWidth = 2;
            inputFieldRef.selectionColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
            inputFieldRef.targetGraphic = inputFieldImage;
            inputFieldRef.interactable = true;
            inputFieldRef.lineType = TMP_InputField.LineType.SingleLine;
            inputFieldRef.contentType = TMP_InputField.ContentType.Standard;
            inputFieldRef.characterLimit = 0; // No limit
            
            // Make sure it's ready
            inputFieldRef.ForceLabelUpdate();

            // Send Button
            GameObject sendBtn = CreateButton("SendButton", inputArea.transform, "Send", accentColor);
            LayoutElement sendBtnLayout = sendBtn.AddComponent<LayoutElement>();
            sendBtnLayout.preferredWidth = 100;
            sendBtnLayout.preferredHeight = 50;
            sendBtnLayout.flexibleWidth = 0;
            sendButtonRef = sendBtn.GetComponent<Button>();
        }

        private void CreateMessagePrefabs()
        {
            userMessagePrefab = CreateMessageBubblePrefab("UserMessagePrefab", true);
            aiMessagePrefab = CreateMessageBubblePrefab("AIMessagePrefab", false);
        }

        private GameObject CreateMessageBubblePrefab(string name, bool isUser)
        {
            GameObject prefab = new GameObject(name);
            prefab.transform.SetParent(transform, false);
            prefab.SetActive(false);

            RectTransform prefabRect = prefab.AddComponent<RectTransform>();
            prefabRect.localScale = Vector3.one;
            prefabRect.sizeDelta = new Vector2(0, 0);

            // Main horizontal layout
            HorizontalLayoutGroup mainHL = prefab.AddComponent<HorizontalLayoutGroup>();
            mainHL.padding = new RectOffset(0, 0, 0, 0);
            mainHL.spacing = 12;
            mainHL.childAlignment = isUser ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            mainHL.reverseArrangement = isUser;
            mainHL.childControlWidth = false;
            mainHL.childControlHeight = true;
            mainHL.childForceExpandWidth = false;
            mainHL.childForceExpandHeight = false;

            LayoutElement mainLayout = prefab.AddComponent<LayoutElement>();
            mainLayout.flexibleWidth = 1;

            // Avatar
            GameObject avatar = CreatePanel("Avatar", prefab.transform);
            avatar.GetComponent<Image>().color = isUser ? userBubbleColor : aiBubbleColor;
            LayoutElement avatarLayout = avatar.AddComponent<LayoutElement>();
            avatarLayout.preferredWidth = 36;
            avatarLayout.preferredHeight = 36;
            avatarLayout.flexibleWidth = 0;

            // Bubble
            GameObject bubble = CreatePanel("Bubble", prefab.transform);
            bubble.GetComponent<Image>().color = isUser ? userBubbleColor : aiBubbleColor;

            LayoutElement bubbleLayout = bubble.AddComponent<LayoutElement>();
            bubbleLayout.preferredWidth = 200;
            bubbleLayout.flexibleWidth = 0;

            VerticalLayoutGroup bubbleVL = bubble.AddComponent<VerticalLayoutGroup>();
            bubbleVL.padding = new RectOffset(16, 16, 12, 12);
            bubbleVL.childControlWidth = true;
            bubbleVL.childControlHeight = true;
            bubbleVL.childForceExpandWidth = true;
            bubbleVL.childForceExpandHeight = false;

            ContentSizeFitter bubbleFitter = bubble.AddComponent<ContentSizeFitter>();
            bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Message Text
            GameObject messageText = CreateText("MessageText", bubble.transform, "", 15, FontStyles.Normal);
            TextMeshProUGUI tmp = messageText.GetComponent<TextMeshProUGUI>();
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;

            LayoutElement textLayout = messageText.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 500;
            textLayout.flexibleWidth = 0;

            // Add ChatMessageBubble component
            ChatMessageBubble bubbleComponent = prefab.AddComponent<ChatMessageBubble>();

            // Use reflection or serializedObject to set private fields (in editor)
            // For runtime, we'll set these via a setup method
            SetupBubbleComponent(bubbleComponent, bubble.GetComponent<Image>(), tmp, avatar.GetComponent<Image>(), bubbleLayout, mainHL, bubbleFitter);

            return prefab;
        }

        private void SetupBubbleComponent(ChatMessageBubble component, Image bubbleBg, TextMeshProUGUI text, Image avatar, LayoutElement layout, HorizontalLayoutGroup hl, ContentSizeFitter fitter)
        {
            // Using reflection to set serialized fields
            var type = typeof(ChatMessageBubble);

            var bgField = type.GetField("bubbleBackground", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bgField?.SetValue(component, bubbleBg);

            var textField = type.GetField("messageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            textField?.SetValue(component, text);

            var avatarField = type.GetField("avatarImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            avatarField?.SetValue(component, avatar);

            var layoutField = type.GetField("layoutElement", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            layoutField?.SetValue(component, layout);

            var hlField = type.GetField("horizontalLayout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            hlField?.SetValue(component, hl);

            var fitterField = type.GetField("contentFitter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fitterField?.SetValue(component, fitter);
        }

        private void SetupChatUI()
        {
            // Add GroqChatUI component
            chatUI = gameObject.AddComponent<GroqChatUI>();

            // Set references using reflection
            var type = typeof(GroqChatUI);

            SetPrivateField(type, chatUI, "apiKey", apiKey);
            SetPrivateField(type, chatUI, "model", model);
            SetPrivateField(type, chatUI, "chatScrollRect", scrollRect);
            SetPrivateField(type, chatUI, "chatContent", chatContent);
            SetPrivateField(type, chatUI, "inputField", inputFieldRef);
            SetPrivateField(type, chatUI, "sendButton", sendButtonRef);
            SetPrivateField(type, chatUI, "userMessagePrefab", userMessagePrefab);
            SetPrivateField(type, chatUI, "aiMessagePrefab", aiMessagePrefab);
            SetPrivateField(type, chatUI, "typingIndicator", typingIndicator);
            SetPrivateField(type, chatUI, "userBubbleColor", userBubbleColor);
            SetPrivateField(type, chatUI, "aiBubbleColor", aiBubbleColor);
            SetPrivateField(type, chatUI, "userTextColor", Color.white);
            SetPrivateField(type, chatUI, "aiTextColor", textColor);

            // Setup clear button
            if (clearButtonRef != null)
            {
                clearButtonRef.onClick.AddListener(() => chatUI.ClearChat());
            }

            // Find and setup clear button
            var clearBtn = mainCanvas.GetComponentInChildren<Button>(true);
            foreach (var btn in mainCanvas.GetComponentsInChildren<Button>(true))
            {
                if (btn.name == "ClearButton")
                {
                    btn.onClick.AddListener(() => chatUI.ClearChat());
                }
                else if (btn.name == "NewChatButton")
                {
                    btn.onClick.AddListener(() => chatUI.ClearChat());
                }
            }
        }

        private void SetPrivateField(System.Type type, object obj, string fieldName, object value)
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        #region Helper Methods

        private GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;
            Image img = panel.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = true;
            return panel;
        }

        private GameObject CreateText(string name, Transform parent, string text, int fontSize, FontStyles style)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.localPosition = Vector3.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = textColor;
            tmp.raycastTarget = false;
            if (font != null) tmp.font = font;

            return textObj;
        }

        private GameObject CreateButton(string name, Transform parent, string text, Color bgColor)
        {
            GameObject button = CreatePanel(name, parent);
            button.GetComponent<Image>().color = bgColor;

            Button btn = button.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;

            // Button text
            GameObject btnText = CreateText("Text", button.transform, text, 14, FontStyles.Bold);
            btnText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            btnText.GetComponent<TextMeshProUGUI>().color = Color.white;
            StretchToFill(btnText.GetComponent<RectTransform>());

            return button;
        }

        private GameObject CreateScrollbar(Transform parent)
        {
            GameObject scrollbar = new GameObject("Scrollbar");
            scrollbar.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
            scrollbarRect.localScale = Vector3.one;
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(8, 0);
            scrollbarRect.anchoredPosition = new Vector2(-4, 0);

            Image scrollbarBg = scrollbar.AddComponent<Image>();
            scrollbarBg.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);

            Scrollbar sb = scrollbar.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;

            // Sliding Area
            GameObject slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbar.transform, false);
            RectTransform slidingRect = slidingArea.AddComponent<RectTransform>();
            StretchToFill(slidingRect);

            // Handle
            GameObject handle = CreatePanel("Handle", slidingArea.transform);
            handle.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            StretchToFill(handleRect);

            sb.handleRect = handleRect;
            sb.targetGraphic = handle.GetComponent<Image>();

            return scrollbar;
        }

        private void StretchToFill(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        #endregion
    }

    /// <summary>
    /// Simple animator for typing indicator dots
    /// </summary>
    public class TypingDotAnimator : MonoBehaviour
    {
        public float delay = 0f;
        private float animationSpeed = 2f;
        private RectTransform rectTransform;
        private float baseY;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            baseY = rectTransform.anchoredPosition.y;
        }

        private void Update()
        {
            float wave = Mathf.Sin((Time.time - delay) * animationSpeed * Mathf.PI);
            float offset = Mathf.Abs(wave) * 4f;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, baseY + offset);
        }
    }
}

