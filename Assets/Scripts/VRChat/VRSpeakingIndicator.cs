using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRChat
{
    /// <summary>
    /// Visual indicator that shows when AI is speaking
    /// Can be used as a pulsing icon, animated text, or sound wave visualization
    /// </summary>
    public class VRSpeakingIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Image[] soundBars; // Optional sound wave bars

        [Header("Animation Settings")]
        [SerializeField] private AnimationType animationType = AnimationType.Pulse;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMinScale = 0.8f;
        [SerializeField] private float pulseMaxScale = 1.2f;

        [Header("Colors")]
        [SerializeField] private Color speakingColor = new Color(0.4f, 0.85f, 0.7f, 1f);
        [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Header("Text")]
        [SerializeField] private string speakingText = "AI Speaking...";
        [SerializeField] private string idleText = "";

        public enum AnimationType
        {
            Pulse,
            Rotate,
            SoundWave,
            Fade
        }

        private RectTransform rectTransform;
        private Vector3 originalScale;
        private float animationTime;
        private bool isSpeaking = false;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                originalScale = rectTransform.localScale;
            }

            // Auto-find components if not assigned
            if (iconImage == null) iconImage = GetComponent<Image>();
            if (statusText == null) statusText = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            isSpeaking = true;
            animationTime = 0f;
            
            if (statusText != null) statusText.text = speakingText;
            if (iconImage != null) iconImage.color = speakingColor;
        }

        private void OnDisable()
        {
            isSpeaking = false;
            
            // Reset to original state
            if (rectTransform != null) rectTransform.localScale = originalScale;
            if (statusText != null) statusText.text = idleText;
            if (iconImage != null) iconImage.color = idleColor;
        }

        private void Update()
        {
            if (!isSpeaking) return;

            animationTime += Time.deltaTime;

            switch (animationType)
            {
                case AnimationType.Pulse:
                    AnimatePulse();
                    break;
                case AnimationType.Rotate:
                    AnimateRotate();
                    break;
                case AnimationType.SoundWave:
                    AnimateSoundWave();
                    break;
                case AnimationType.Fade:
                    AnimateFade();
                    break;
            }
        }

        private void AnimatePulse()
        {
            if (rectTransform == null) return;

            float scale = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
                (Mathf.Sin(animationTime * pulseSpeed * Mathf.PI) + 1f) / 2f);
            
            rectTransform.localScale = originalScale * scale;
        }

        private void AnimateRotate()
        {
            if (rectTransform == null) return;

            rectTransform.Rotate(0, 0, -pulseSpeed * 50f * Time.deltaTime);
        }

        private void AnimateSoundWave()
        {
            if (soundBars == null || soundBars.Length == 0) return;

            for (int i = 0; i < soundBars.Length; i++)
            {
                if (soundBars[i] == null) continue;

                float offset = i * 0.3f;
                float height = Mathf.Abs(Mathf.Sin((animationTime + offset) * pulseSpeed * Mathf.PI));
                
                var rect = soundBars[i].rectTransform;
                if (rect != null)
                {
                    Vector2 size = rect.sizeDelta;
                    size.y = Mathf.Lerp(5f, 25f, height);
                    rect.sizeDelta = size;
                }
            }
        }

        private void AnimateFade()
        {
            if (iconImage == null) return;

            float alpha = Mathf.Lerp(0.3f, 1f, 
                (Mathf.Sin(animationTime * pulseSpeed * Mathf.PI) + 1f) / 2f);
            
            Color color = iconImage.color;
            color.a = alpha;
            iconImage.color = color;
        }

        /// <summary>
        /// Create a simple speaking indicator at runtime
        /// </summary>
        public static VRSpeakingIndicator CreateIndicator(Transform parent, Vector2 position)
        {
            GameObject indicatorObj = new GameObject("SpeakingIndicator");
            indicatorObj.transform.SetParent(parent, false);

            RectTransform rect = indicatorObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150, 40);

            // Background
            Image bg = indicatorObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Add horizontal layout
            HorizontalLayoutGroup hlg = indicatorObj.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(indicatorObj.transform, false);
            
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(20, 20);
            
            Image icon = iconObj.AddComponent<Image>();
            icon.color = new Color(0.4f, 0.85f, 0.7f, 1f);
            
            LayoutElement iconLayout = iconObj.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 20;
            iconLayout.preferredHeight = 20;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(indicatorObj.transform, false);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "AI Speaking...";
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            // Add indicator component
            VRSpeakingIndicator indicator = indicatorObj.AddComponent<VRSpeakingIndicator>();
            indicator.iconImage = icon;
            indicator.statusText = text;

            // Start disabled
            indicatorObj.SetActive(false);

            return indicator;
        }
    }
}

