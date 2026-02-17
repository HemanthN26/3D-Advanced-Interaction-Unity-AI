using UnityEngine;
using UnityEngine.UI;

namespace VRChat
{
    /// <summary>
    /// Animated typing indicator with bouncing dots
    /// </summary>
    public class VRTypingIndicator : MonoBehaviour
    {
        [Header("Dot References")]
        [SerializeField] private Image[] dots;

        [Header("Animation Settings")]
        [SerializeField] private float bounceHeight = 5f;
        [SerializeField] private float bounceSpeed = 3f;
        [SerializeField] private float dotDelay = 0.2f;

        [Header("Colors")]
        [SerializeField] private Color dotColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        private RectTransform[] dotRects;
        private float[] basePosY;

        private void Awake()
        {
            InitializeDots();
        }

        private void InitializeDots()
        {
            if (dots == null || dots.Length == 0) return;

            dotRects = new RectTransform[dots.Length];
            basePosY = new float[dots.Length];

            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] != null)
                {
                    dots[i].color = dotColor;
                    dotRects[i] = dots[i].GetComponent<RectTransform>();
                    if (dotRects[i] != null)
                    {
                        basePosY[i] = dotRects[i].anchoredPosition.y;
                    }
                }
            }
        }

        private void Update()
        {
            AnimateDots();
        }

        private void AnimateDots()
        {
            if (dotRects == null) return;

            for (int i = 0; i < dotRects.Length; i++)
            {
                if (dotRects[i] == null) continue;

                float offset = i * dotDelay;
                float wave = Mathf.Sin((Time.time - offset) * bounceSpeed * Mathf.PI);
                float bounce = Mathf.Abs(wave) * bounceHeight;

                Vector2 pos = dotRects[i].anchoredPosition;
                pos.y = basePosY[i] + bounce;
                dotRects[i].anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Create dots programmatically if not assigned
        /// </summary>
        [ContextMenu("Create Dots")]
        public void CreateDots()
        {
            // Clear existing
            foreach (Transform child in transform)
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            dots = new Image[3];
            
            for (int i = 0; i < 3; i++)
            {
                GameObject dotObj = new GameObject($"Dot{i}");
                dotObj.transform.SetParent(transform, false);

                RectTransform rect = dotObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(8, 8);
                rect.anchoredPosition = new Vector2(-12 + (i * 12), 0);

                Image img = dotObj.AddComponent<Image>();
                img.color = dotColor;

                dots[i] = img;
            }

            InitializeDots();
        }
    }
}

