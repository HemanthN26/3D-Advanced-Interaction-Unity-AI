using UnityEngine;
using VRChat;

/// <summary>
/// Connects VRChatTTS audio to the avatar's AvatarController for lip sync and animations.
/// Attach this to the same GameObject as VRChatTTS or the Avatar.
/// </summary>
public class VRAvatarTTSConnector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The VRChatTTS component that generates speech")]
    [SerializeField] private VRChatTTS ttsManager;
    
    [Tooltip("The AvatarController component on your avatar")]
    [SerializeField] private AvatarController avatarController;
    
    [Tooltip("The AudioSource used by the avatar for lip sync")]
    [SerializeField] private AudioSource avatarAudioSource;

    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindComponents = true;
    [SerializeField] private string avatarName = "6925dbb9bcfe438b18d485f4";

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private AudioSource ttsAudioSource;

    private void Start()
    {
        if (autoFindComponents)
        {
            AutoFindComponents();
        }

        ValidateSetup();
        ConnectTTSToAvatar();
    }

    private void AutoFindComponents()
    {
        // Find VRChatTTS
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<VRChatTTS>();
            if (ttsManager != null && showDebugInfo)
                Debug.Log($"[VRAvatarTTSConnector] Found VRChatTTS on: {ttsManager.gameObject.name}");
        }

        // Find Avatar by name
        if (avatarController == null && !string.IsNullOrEmpty(avatarName))
        {
            GameObject avatarObj = GameObject.Find(avatarName);
            if (avatarObj != null)
            {
                avatarController = avatarObj.GetComponent<AvatarController>();
                if (avatarController != null && showDebugInfo)
                    Debug.Log($"[VRAvatarTTSConnector] Found AvatarController on: {avatarObj.name}");
            }
        }

        // Find Avatar's AudioSource
        if (avatarAudioSource == null && avatarController != null)
        {
            avatarAudioSource = avatarController.GetComponent<AudioSource>();
            if (avatarAudioSource == null)
            {
                avatarAudioSource = avatarController.gameObject.AddComponent<AudioSource>();
                avatarAudioSource.playOnAwake = false;
                avatarAudioSource.spatialBlend = 1f; // 3D sound from avatar
                if (showDebugInfo)
                    Debug.Log("[VRAvatarTTSConnector] Created AudioSource on avatar");
            }
        }
    }

    private void ValidateSetup()
    {
        if (ttsManager == null)
        {
            Debug.LogError("[VRAvatarTTSConnector] VRChatTTS not found! Please assign it in the Inspector.");
            return;
        }

        if (avatarController == null)
        {
            Debug.LogError("[VRAvatarTTSConnector] AvatarController not found! Please assign it or set the correct avatar name.");
            return;
        }

        if (avatarAudioSource == null)
        {
            Debug.LogError("[VRAvatarTTSConnector] Avatar AudioSource not found!");
            return;
        }

        // Get the TTS AudioSource for copying audio
        ttsAudioSource = ttsManager.GetComponent<AudioSource>();
        if (ttsAudioSource == null)
        {
            Debug.LogWarning("[VRAvatarTTSConnector] TTS AudioSource not found on VRChatTTS GameObject");
        }

        // Link the avatar's AudioSource to the AvatarController
        SetAvatarAudioSource();

        if (showDebugInfo)
        {
            Debug.Log("[VRAvatarTTSConnector] Setup complete!");
            Debug.Log($"  - TTS Manager: {ttsManager.gameObject.name}");
            Debug.Log($"  - Avatar: {avatarController.gameObject.name}");
            Debug.Log($"  - Avatar Position: {avatarController.transform.position}");
        }
    }

    private void SetAvatarAudioSource()
    {
        // Use reflection to set the audioSource field on AvatarController
        var audioSourceField = typeof(AvatarController).GetField("audioSource", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (audioSourceField != null)
        {
            audioSourceField.SetValue(avatarController, avatarAudioSource);
            if (showDebugInfo)
                Debug.Log("[VRAvatarTTSConnector] Linked AudioSource to AvatarController");
        }
    }

    private void ConnectTTSToAvatar()
    {
        if (ttsManager == null) return;

        // Subscribe to TTS events
        ttsManager.OnSpeakingStarted += OnTTSSpeakingStarted;
        ttsManager.OnSpeakingFinished += OnTTSSpeakingFinished;

        if (showDebugInfo)
            Debug.Log("[VRAvatarTTSConnector] Subscribed to TTS events");
    }

    private void OnTTSSpeakingStarted()
    {
        if (showDebugInfo)
            Debug.Log("[VRAvatarTTSConnector] TTS Started - Avatar should animate");

        // Copy the audio clip from TTS to avatar
        if (ttsAudioSource != null && avatarAudioSource != null && ttsAudioSource.clip != null)
        {
            avatarAudioSource.clip = ttsAudioSource.clip;
            avatarAudioSource.Play();
            
            // Mute the original TTS audio source so we don't hear double
            // (audio now comes from avatar's position for spatial audio)
            // ttsAudioSource.mute = true; // Uncomment if you want spatial audio from avatar
        }
    }

    private void OnTTSSpeakingFinished()
    {
        if (showDebugInfo)
            Debug.Log("[VRAvatarTTSConnector] TTS Finished - Avatar should stop animating");
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ttsManager != null)
        {
            ttsManager.OnSpeakingStarted -= OnTTSSpeakingStarted;
            ttsManager.OnSpeakingFinished -= OnTTSSpeakingFinished;
        }
    }

    #region Debug Helpers

    [ContextMenu("Debug: Show Avatar Info")]
    public void DebugShowAvatarInfo()
    {
        if (avatarController == null)
        {
            Debug.LogWarning("No avatar controller assigned");
            return;
        }

        var avatar = avatarController.gameObject;
        Debug.Log($"=== AVATAR DEBUG INFO ===");
        Debug.Log($"Name: {avatar.name}");
        Debug.Log($"Active: {avatar.activeSelf} (in hierarchy: {avatar.activeInHierarchy})");
        Debug.Log($"Position: {avatar.transform.position}");
        Debug.Log($"Scale: {avatar.transform.localScale}");
        Debug.Log($"Layer: {LayerMask.LayerToName(avatar.layer)} ({avatar.layer})");

        // Check renderers
        var renderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Debug.Log($"SkinnedMeshRenderers: {renderers.Length}");
        foreach (var r in renderers)
        {
            Debug.Log($"  - {r.gameObject.name}: enabled={r.enabled}, bounds={r.bounds}");
        }
    }

    [ContextMenu("Debug: Move Avatar In Front of Camera")]
    public void DebugMoveAvatarInFrontOfCamera()
    {
        if (avatarController == null)
        {
            Debug.LogWarning("No avatar controller assigned");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No main camera found");
            return;
        }

        // Position avatar 3 meters in front of camera, at same height
        Vector3 newPos = cam.transform.position + cam.transform.forward * 3f;
        newPos.y = cam.transform.position.y - 1.5f; // Slightly below eye level
        
        avatarController.transform.position = newPos;
        
        // Face the camera
        avatarController.transform.LookAt(cam.transform);
        avatarController.transform.rotation = Quaternion.Euler(0, avatarController.transform.eulerAngles.y, 0);

        Debug.Log($"[VRAvatarTTSConnector] Moved avatar to: {newPos}");
    }

    [ContextMenu("Debug: Test Speak")]
    public void DebugTestSpeak()
    {
        if (ttsManager != null)
        {
            ttsManager.Speak("Hello! I am your friendly virtual assistant. Can you see me now?");
        }
    }

    #endregion
}
