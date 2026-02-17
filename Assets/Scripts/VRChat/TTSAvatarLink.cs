using UnityEngine;
using VRChat;

/// <summary>
/// Links TTS audio to the avatar so it animates while speaking.
/// Add this component to your avatar GameObject.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TTSAvatarLink : MonoBehaviour
{
    [Header("Auto-Find (leave empty to auto-detect)")]
    [Tooltip("The VRChatTTS component - will auto-find if empty")]
    [SerializeField] private VRChatTTS ttsManager;

    [Header("Settings")]
    [Tooltip("Play audio from avatar position (3D sound) instead of TTS source")]
    [SerializeField] private bool playFromAvatar = true;
    
    [Tooltip("Mute the original TTS audio source when playing from avatar")]
    [SerializeField] private bool muteOriginalTTS = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private AudioSource avatarAudioSource;
    private AudioSource ttsAudioSource;
    private AvatarController avatarController;
    private bool isConnected = false;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        Log("Initializing TTSAvatarLink...");

        // Get avatar's AudioSource
        avatarAudioSource = GetComponent<AudioSource>();
        if (avatarAudioSource == null)
        {
            avatarAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure avatar audio source
        avatarAudioSource.playOnAwake = false;
        avatarAudioSource.loop = false;
        avatarAudioSource.spatialBlend = playFromAvatar ? 1f : 0f; // 3D if playing from avatar

        // Get AvatarController
        avatarController = GetComponent<AvatarController>();
        if (avatarController != null)
        {
            // Make sure AvatarController uses our AudioSource
            var audioField = typeof(AvatarController).GetField("audioSource",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            audioField?.SetValue(avatarController, avatarAudioSource);
            Log("Linked AudioSource to AvatarController");
        }
        else
        {
            Log("No AvatarController found - lip sync won't work");
        }

        // Find TTS Manager
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<VRChatTTS>();
        }

        if (ttsManager == null)
        {
            Debug.LogError("[TTSAvatarLink] Could not find VRChatTTS! Make sure it exists in the scene.");
            return;
        }

        Log($"Found VRChatTTS on: {ttsManager.gameObject.name}");

        // Get TTS AudioSource
        ttsAudioSource = ttsManager.GetComponent<AudioSource>();

        // Subscribe to TTS events
        ttsManager.OnSpeakingStarted += OnTTSSpeakingStarted;
        ttsManager.OnSpeakingFinished += OnTTSSpeakingFinished;
        ttsManager.OnSpeakingText += OnTTSSpeakingText;

        isConnected = true;
        Log("✓ TTSAvatarLink initialized successfully!");
    }

    private void OnTTSSpeakingStarted()
    {
        Log("TTS Speaking Started - Avatar should start animating");

        if (playFromAvatar && ttsAudioSource != null)
        {
            // Small delay to let TTS load the clip
            StartCoroutine(PlayAudioFromAvatar());
        }
    }

    private System.Collections.IEnumerator PlayAudioFromAvatar()
    {
        // Wait a frame for the audio clip to be set
        yield return null;
        yield return null; // Extra frame to be safe

        if (ttsAudioSource != null && ttsAudioSource.clip != null)
        {
            // Copy the audio clip to avatar
            avatarAudioSource.clip = ttsAudioSource.clip;
            avatarAudioSource.Play();

            if (muteOriginalTTS)
            {
                ttsAudioSource.mute = true;
            }

            Log($"Playing audio from avatar ({avatarAudioSource.clip.length:F1}s)");
        }
        else
        {
            Log("No audio clip available from TTS");
        }
    }

    private void OnTTSSpeakingFinished()
    {
        Log("TTS Speaking Finished - Avatar should stop animating");

        if (muteOriginalTTS && ttsAudioSource != null)
        {
            ttsAudioSource.mute = false;
        }
    }

    private void OnTTSSpeakingText(string text)
    {
        Log($"Speaking: {(text.Length > 50 ? text.Substring(0, 50) + "..." : text)}");
    }

    private void Update()
    {
        // Sync audio state - if TTS is playing but avatar isn't, start avatar
        if (isConnected && playFromAvatar && ttsAudioSource != null)
        {
            // Check if TTS has a new clip that we haven't played yet
            if (ttsAudioSource.isPlaying && !avatarAudioSource.isPlaying)
            {
                if (ttsAudioSource.clip != null && ttsAudioSource.clip != avatarAudioSource.clip)
                {
                    avatarAudioSource.clip = ttsAudioSource.clip;
                    avatarAudioSource.time = ttsAudioSource.time; // Sync playback position
                    avatarAudioSource.Play();
                    
                    if (muteOriginalTTS)
                    {
                        ttsAudioSource.mute = true;
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ttsManager != null)
        {
            ttsManager.OnSpeakingStarted -= OnTTSSpeakingStarted;
            ttsManager.OnSpeakingFinished -= OnTTSSpeakingFinished;
            ttsManager.OnSpeakingText -= OnTTSSpeakingText;
        }

        // Unmute TTS if we muted it
        if (ttsAudioSource != null)
        {
            ttsAudioSource.mute = false;
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[TTSAvatarLink] {message}");
        }
    }

    /// <summary>
    /// Manually trigger avatar to speak (for testing)
    /// </summary>
    [ContextMenu("Test - Say Hello")]
    public void TestSpeak()
    {
        if (ttsManager != null)
        {
            ttsManager.Speak("Hello! I am your virtual assistant. Can you see my lips moving while I talk?");
        }
        else
        {
            Debug.LogWarning("[TTSAvatarLink] TTS Manager not found!");
        }
    }

    /// <summary>
    /// Check if everything is set up correctly
    /// </summary>
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        Debug.Log("=== TTSAvatarLink Validation ===");
        
        Debug.Log($"TTS Manager: {(ttsManager != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Avatar AudioSource: {(avatarAudioSource != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"TTS AudioSource: {(ttsAudioSource != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"AvatarController: {(avatarController != null ? "✓ Found" : "✗ Missing")}");
        Debug.Log($"Is Connected: {isConnected}");

        if (avatarController != null)
        {
            var faceMeshField = typeof(AvatarController).GetField("faceMesh",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var faceMesh = faceMeshField?.GetValue(avatarController) as SkinnedMeshRenderer;
            
            if (faceMesh != null)
            {
                Debug.Log($"Face Mesh: ✓ {faceMesh.name} ({faceMesh.sharedMesh?.blendShapeCount ?? 0} blendshapes)");
            }
            else
            {
                Debug.Log("Face Mesh: ✗ Not assigned!");
            }
        }

        Debug.Log("=== End Validation ===");
    }
}
