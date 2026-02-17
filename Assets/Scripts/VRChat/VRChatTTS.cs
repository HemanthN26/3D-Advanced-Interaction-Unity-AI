using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VRChat
{
    /// <summary>
    /// Available voices for Groq Orpheus TTS (Canopy Labs)
    /// </summary>
    public enum TTSVoice
    {
        // Female voices
        Autumn,
        Diana,
        Hannah,
        
        // Male voices
        Austin,
        Daniel,
        Troy
    }

    /// <summary>
    /// Vocal direction presets for Orpheus TTS expressiveness
    /// </summary>
    public enum TTSTone
    {
        None,           // Natural conversational cadence
        Friendly,       // Warm and approachable
        Cheerful,       // Upbeat and positive
        FriendlyCheerful, // Combination of friendly and cheerful
        Professional,   // Business-like and formal
        Calm,           // Relaxed and soothing
        Excited,        // Energetic and enthusiastic
        Warm            // Comforting and welcoming
    }

    /// <summary>
    /// VR Chat TTS Manager - Handles text-to-speech for AI responses
    /// Features: Audio queuing, speaking indicator, escape handling
    /// </summary>
    public class VRChatTTS : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string apiKey = "YOUR_GROQ_API_KEY";
        
        [Header("Voice Settings")]
        [SerializeField] private TTSVoice selectedVoice = TTSVoice.Troy;
        [SerializeField] private TTSTone tone = TTSTone.FriendlyCheerful;
        [SerializeField] [Range(0.5f, 2f)] private float volume = 1f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Behavior")]
        [SerializeField] private bool autoCreateAudioSource = true;
        [SerializeField] private bool queueMessages = true; // Queue if already speaking
        [SerializeField] private int maxQueueSize = 5;

        // Events
        public event Action OnSpeakingStarted;
        public event Action OnSpeakingFinished;
        public event Action<string> OnSpeakingText;
        public event Action<string> OnError;

        // State
        public bool IsSpeaking => isSpeaking;
        public bool IsProcessing => isProcessing;
        public int QueueCount => speechQueue.Count;
        
        /// <summary>
        /// Get the AudioSource used for TTS playback (for lip sync integration)
        /// </summary>
        public AudioSource AudioSource => audioSource;

        private const string API_URL = "https://api.groq.com/openai/v1/audio/speech";
        private const string MODEL = "canopylabs/orpheus-v1-english";
        private const string RESPONSE_FORMAT = "wav";
        private const int MAX_INPUT_LENGTH = 200; // Orpheus has 200 character limit

        private Queue<string> speechQueue = new Queue<string>();
        private bool isSpeaking = false;
        private bool isProcessing = false;
        private HttpClient httpClient;
        private Coroutine currentSpeechCoroutine;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeAudioSource();
            InitializeHttpClient();
        }

        private void OnDestroy()
        {
            httpClient?.Dispose();
            StopAllCoroutines();
        }

        private void Update()
        {
            // Check if current audio finished playing
            if (isSpeaking && audioSource != null && !audioSource.isPlaying && !isProcessing)
            {
                isSpeaking = false;
                OnSpeakingFinished?.Invoke();
                
                // Process next in queue
                ProcessQueue();
            }
        }

        #endregion

        #region Initialization

        private void InitializeAudioSource()
        {
            if (audioSource == null && autoCreateAudioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound for UI
                Debug.Log("[VRChatTTS] Created AudioSource");
            }

            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
        }

        private void InitializeHttpClient()
        {
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Speak the given text. Will queue if already speaking (when queueMessages is true)
        /// </summary>
        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                Debug.LogWarning("[VRChatTTS] Cannot speak empty text");
                return;
            }

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_GROQ_API_KEY")
            {
                Debug.LogError("[VRChatTTS] API key not set!");
                OnError?.Invoke("API key not configured");
                return;
            }

            // Clean the text
            string cleanedText = CleanTextForTTS(text);

            // Split into chunks if text exceeds Orpheus limit (200 chars)
            List<string> chunks = SplitTextIntoChunks(cleanedText, MAX_INPUT_LENGTH);

            foreach (string chunk in chunks)
            {
                if (isSpeaking || isProcessing)
                {
                    if (queueMessages && speechQueue.Count < maxQueueSize)
                    {
                        speechQueue.Enqueue(chunk);
                        Debug.Log($"[VRChatTTS] Queued speech ({speechQueue.Count} in queue)");
                    }
                    else if (!queueMessages)
                    {
                        // Stop current and speak new
                        Stop();
                        StartSpeech(chunk);
                    }
                    else
                    {
                        Debug.LogWarning("[VRChatTTS] Queue full, dropping message");
                    }
                }
                else
                {
                    StartSpeech(chunk);
                }
            }
        }

        /// <summary>
        /// Speak text as a coroutine (useful for waiting until done)
        /// </summary>
        public IEnumerator SpeakAndWait(string text)
        {
            Speak(text);
            
            // Wait for processing to start
            yield return new WaitUntil(() => isProcessing || isSpeaking);
            
            // Wait for speaking to finish
            yield return new WaitWhile(() => isProcessing || isSpeaking);
        }

        /// <summary>
        /// Stop current speech and clear queue
        /// </summary>
        public void Stop()
        {
            if (currentSpeechCoroutine != null)
            {
                StopCoroutine(currentSpeechCoroutine);
                currentSpeechCoroutine = null;
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            speechQueue.Clear();
            isSpeaking = false;
            isProcessing = false;

            OnSpeakingFinished?.Invoke();
        }

        /// <summary>
        /// Change voice at runtime
        /// </summary>
        public void SetVoice(TTSVoice voice)
        {
            selectedVoice = voice;
            Debug.Log($"[VRChatTTS] Voice changed to: {voice}");
        }

        /// <summary>
        /// Set API key at runtime
        /// </summary>
        public void SetApiKey(string key)
        {
            apiKey = key;
        }

        #endregion

        #region Private Methods

        private void StartSpeech(string text)
        {
            currentSpeechCoroutine = StartCoroutine(GenerateAndPlaySpeech(text));
        }

        private void ProcessQueue()
        {
            if (speechQueue.Count > 0 && !isSpeaking && !isProcessing)
            {
                string nextText = speechQueue.Dequeue();
                StartSpeech(nextText);
            }
        }

        private IEnumerator GenerateAndPlaySpeech(string text)
        {
            isProcessing = true;
            Debug.Log($"[VRChatTTS] Generating speech for: {(text.Length > 50 ? text.Substring(0, 50) + "..." : text)}");

            OnSpeakingText?.Invoke(text);

            // Make API request
            Task<byte[]> apiTask = null;
            byte[] audioData = null;
            string error = null;

            try
            {
                apiTask = RequestTTSAudio(text);
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            if (apiTask != null)
            {
                // Wait for API response
                while (!apiTask.IsCompleted)
                {
                    yield return null;
                }

                if (apiTask.IsFaulted)
                {
                    error = apiTask.Exception?.InnerException?.Message ?? "Unknown error";
                }
                else if (apiTask.IsCompletedSuccessfully)
                {
                    audioData = apiTask.Result;
                }
            }

            isProcessing = false;

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"[VRChatTTS] Error: {error}");
                OnError?.Invoke(error);
                ProcessQueue(); // Try next in queue
                yield break;
            }

            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogError("[VRChatTTS] No audio data received");
                OnError?.Invoke("No audio data received");
                ProcessQueue();
                yield break;
            }

            // Create and play audio clip
            try
            {
                AudioClip clip = CreateClipFromWav(audioData);
                
                if (clip != null && audioSource != null)
                {
                    audioSource.clip = clip;
                    audioSource.volume = volume;
                    audioSource.Play();
                    
                    isSpeaking = true;
                    OnSpeakingStarted?.Invoke();
                    
                    Debug.Log($"[VRChatTTS] Playing audio ({clip.length:F1}s)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[VRChatTTS] Error creating audio clip: {e.Message}");
                OnError?.Invoke($"Audio decode error: {e.Message}");
                ProcessQueue();
            }
        }

        private async Task<byte[]> RequestTTSAudio(string text)
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            string voiceName = GetVoiceName(selectedVoice);
            
            // Build JSON with proper escaping
            var requestObj = new
            {
                model = MODEL,
                voice = voiceName,
                input = text,
                response_format = RESPONSE_FORMAT
            };

            // Add vocal direction for expressiveness
            string toneDirection = GetToneDirection(tone);
            string inputWithTone = toneDirection + text;

            string json = JsonUtility.ToJson(new TTSRequest
            {
                model = MODEL,
                voice = voiceName,
                input = inputWithTone,
                response_format = RESPONSE_FORMAT
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(API_URL, content);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"TTS API error ({response.StatusCode}): {errorBody}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }

        private string GetVoiceName(TTSVoice voice)
        {
            // Orpheus uses lowercase voice names
            // Handle invalid enum values (from old PlayAI enum saved in scenes)
            int voiceIndex = (int)voice;
            if (voiceIndex < 0 || voiceIndex > 5)
            {
                Debug.LogWarning($"[VRChatTTS] Invalid voice enum value {voiceIndex}, defaulting to 'troy'");
                return "troy";
            }
            return voice.ToString().ToLower();
        }

        private string GetToneDirection(TTSTone selectedTone)
        {
            return selectedTone switch
            {
                TTSTone.Friendly => "[friendly] ",
                TTSTone.Cheerful => "[cheerful] ",
                TTSTone.FriendlyCheerful => "[friendly] [cheerful] ",
                TTSTone.Professional => "[professionally] ",
                TTSTone.Calm => "[calm] ",
                TTSTone.Excited => "[excited] ",
                TTSTone.Warm => "[warm] ",
                _ => "" // None = natural conversational cadence
            };
        }

        /// <summary>
        /// Set tone at runtime
        /// </summary>
        public void SetTone(TTSTone newTone)
        {
            tone = newTone;
            Debug.Log($"[VRChatTTS] Tone changed to: {newTone}");
        }

        private string CleanTextForTTS(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Remove markdown formatting
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1"); // Bold
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "$1"); // Italic
            text = System.Text.RegularExpressions.Regex.Replace(text, @"`(.+?)`", "$1"); // Code
            text = System.Text.RegularExpressions.Regex.Replace(text, @"```[\s\S]*?```", ""); // Code blocks
            
            // Remove URLs
            text = System.Text.RegularExpressions.Regex.Replace(text, @"https?://\S+", "");

            // Remove special characters that might cause issues
            text = text.Replace("\n", " ");
            text = text.Replace("\r", " ");
            text = text.Replace("\t", " ");
            text = text.Replace("\"", "'");
            text = text.Replace("\\", "");

            // Collapse multiple spaces
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            // Trim
            text = text.Trim();

            return text;
        }

        /// <summary>
        /// Split text into chunks that fit within the Orpheus character limit.
        /// Tries to split at sentence boundaries for natural speech.
        /// </summary>
        private List<string> SplitTextIntoChunks(string text, int maxLength)
        {
            var chunks = new List<string>();
            
            if (string.IsNullOrEmpty(text))
                return chunks;

            if (text.Length <= maxLength)
            {
                chunks.Add(text);
                return chunks;
            }

            // Split by sentences first
            var sentenceEnders = new[] { ". ", "! ", "? ", ".\n", "!\n", "?\n" };
            int currentStart = 0;
            string currentChunk = "";

            while (currentStart < text.Length)
            {
                // Find the next sentence boundary
                int nextEnd = -1;
                foreach (var ender in sentenceEnders)
                {
                    int pos = text.IndexOf(ender, currentStart);
                    if (pos >= 0 && (nextEnd < 0 || pos < nextEnd))
                    {
                        nextEnd = pos + ender.Length;
                    }
                }

                string segment;
                if (nextEnd < 0 || nextEnd > text.Length)
                {
                    // No more sentence boundaries, take the rest
                    segment = text.Substring(currentStart);
                    currentStart = text.Length;
                }
                else
                {
                    segment = text.Substring(currentStart, nextEnd - currentStart);
                    currentStart = nextEnd;
                }

                // Check if adding this segment would exceed the limit
                if ((currentChunk + segment).Length <= maxLength)
                {
                    currentChunk += segment;
                }
                else
                {
                    // Save current chunk if not empty
                    if (!string.IsNullOrWhiteSpace(currentChunk))
                    {
                        chunks.Add(currentChunk.Trim());
                    }

                    // If segment itself is too long, split it by words
                    if (segment.Length > maxLength)
                    {
                        var words = segment.Split(' ');
                        currentChunk = "";
                        foreach (var word in words)
                        {
                            if ((currentChunk + " " + word).Trim().Length <= maxLength)
                            {
                                currentChunk = (currentChunk + " " + word).Trim();
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(currentChunk))
                                {
                                    chunks.Add(currentChunk.Trim());
                                }
                                // If single word is too long, truncate it
                                currentChunk = word.Length > maxLength ? word.Substring(0, maxLength) : word;
                            }
                        }
                    }
                    else
                    {
                        currentChunk = segment;
                    }
                }
            }

            // Don't forget the last chunk
            if (!string.IsNullOrWhiteSpace(currentChunk))
            {
                chunks.Add(currentChunk.Trim());
            }

            return chunks;
        }

        private AudioClip CreateClipFromWav(byte[] wav)
        {
            if (wav.Length < 44)
            {
                throw new Exception("WAV data too short");
            }

            // Parse WAV header
            int channels = BitConverter.ToInt16(wav, 22);
            int sampleRate = BitConverter.ToInt32(wav, 24);
            int bitsPerSample = BitConverter.ToInt16(wav, 34);

            if (channels < 1 || channels > 2)
            {
                throw new Exception($"Unsupported channel count: {channels}");
            }

            if (bitsPerSample != 16)
            {
                throw new Exception($"Unsupported bits per sample: {bitsPerSample}");
            }

            // Find data chunk
            int dataStart = FindDataChunk(wav) + 8;
            int dataLength = wav.Length - dataStart;
            int sampleCount = dataLength / (bitsPerSample / 8);

            if (sampleCount <= 0)
            {
                throw new Exception("No audio samples found");
            }

            // Convert to float samples
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                int sampleIndex = dataStart + i * 2;
                if (sampleIndex + 1 < wav.Length)
                {
                    short sample = BitConverter.ToInt16(wav, sampleIndex);
                    samples[i] = sample / 32768f;
                }
            }

            // Create AudioClip
            int samplesPerChannel = sampleCount / channels;
            AudioClip clip = AudioClip.Create("TTS_Audio", samplesPerChannel, channels, sampleRate, false);
            clip.SetData(samples, 0);

            return clip;
        }

        private int FindDataChunk(byte[] wav)
        {
            for (int i = 12; i < wav.Length - 8; i++)
            {
                if (wav[i] == 'd' && wav[i + 1] == 'a' && wav[i + 2] == 't' && wav[i + 3] == 'a')
                {
                    return i;
                }
            }
            throw new Exception("DATA chunk not found in WAV file");
        }

        #endregion

        /// <summary>
        /// JSON serializable request object
        /// </summary>
        [Serializable]
        private class TTSRequest
        {
            public string model;
            public string voice;
            public string input;
            public string response_format;
        }
    }
}

