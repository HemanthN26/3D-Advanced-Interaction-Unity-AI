using UnityEngine;
using UnityEngine.UI;

namespace VRChat
{
    /// <summary>
    /// Visual indicator that pulses when recording voice input.
    /// Attach to a UI Image or 3D object.
    /// </summary>
    public class RecordingIndicator : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float pulseSpeed = 3f;
        [SerializeField] private float minScale = 0.8f;
        [SerializeField] private float maxScale = 1.2f;
        [SerializeField] private float minAlpha = 0.5f;
        [SerializeField] private float maxAlpha = 1f;

        [Header("Colors")]
        [SerializeField] private Color recordingColor = new Color(1f, 0.2f, 0.2f, 1f); // Red
        [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray

        [Header("Optional Components")]
        [SerializeField] private Image uiImage;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private TMPro.TextMeshProUGUI statusText;

        private Vector3 originalScale;
        private bool isRecording = false;
        private Material material;

        void Awake()
        {
            originalScale = transform.localScale;

            // Auto-find components
            if (uiImage == null) uiImage = GetComponent<Image>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

            // Get material for 3D objects
            if (meshRenderer != null)
            {
                material = meshRenderer.material;
            }

            // Set initial state
            SetRecording(false);
        }

        void Update()
        {
            if (isRecording)
            {
                // Pulse animation
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                
                // Scale pulse
                float scale = Mathf.Lerp(minScale, maxScale, pulse);
                transform.localScale = originalScale * scale;

                // Alpha pulse
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, pulse);
                SetAlpha(alpha);
            }
        }

        public void SetRecording(bool recording)
        {
            isRecording = recording;

            if (recording)
            {
                // Show and animate
                gameObject.SetActive(true);
                SetColor(recordingColor);
                if (statusText != null) statusText.text = "● Recording...";
            }
            else
            {
                // Reset scale
                transform.localScale = originalScale;
                
                // Hide or show idle state
                // gameObject.SetActive(false); // Uncomment to hide when not recording
                SetColor(idleColor);
                if (statusText != null) statusText.text = "Hold Grip to Speak";
            }
        }

        private void SetColor(Color color)
        {
            if (uiImage != null) uiImage.color = color;
            if (spriteRenderer != null) spriteRenderer.color = color;
            if (material != null) material.color = color;
        }

        private void SetAlpha(float alpha)
        {
            if (uiImage != null)
            {
                var c = uiImage.color;
                c.a = alpha;
                uiImage.color = c;
            }
            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = alpha;
                spriteRenderer.color = c;
            }
            if (material != null)
            {
                var c = material.color;
                c.a = alpha;
                material.color = c;
            }
        }

        void OnEnable()
        {
            // Subscribe to STT events
            var stt = FindFirstObjectByType<VRSpeechToText>();
            if (stt != null)
            {
                stt.OnRecordingStarted += OnRecordingStarted;
                stt.OnRecordingStopped += OnRecordingStopped;
            }
        }

        void OnDisable()
        {
            var stt = FindFirstObjectByType<VRSpeechToText>();
            if (stt != null)
            {
                stt.OnRecordingStarted -= OnRecordingStarted;
                stt.OnRecordingStopped -= OnRecordingStopped;
            }
        }

        private void OnRecordingStarted() => SetRecording(true);
        private void OnRecordingStopped() => SetRecording(false);
    }
}
