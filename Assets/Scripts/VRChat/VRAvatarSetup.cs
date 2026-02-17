using UnityEngine;
using VRChat;

/// <summary>
/// Automatically sets up the avatar in VR_Room scene.
/// Fixes position, disables music, and connects to TTS.
/// Add this to any GameObject and it will configure everything on Start.
/// </summary>
public class VRAvatarSetup : MonoBehaviour
{
    [Header("Avatar Settings")]
    [Tooltip("Name of the avatar GameObject to find")]
    [SerializeField] private string avatarName = "6925dbb9bcfe438b18d485f4";
    
    [Tooltip("Distance in front of camera to place avatar")]
    [SerializeField] private float distanceFromCamera = 3f;
    
    [Tooltip("Height offset from camera (negative = below eye level)")]
    [SerializeField] private float heightOffset = -1.5f;

    [Header("Audio Settings")]
    [Tooltip("Stop any music playing on avatar's AudioSource")]
    [SerializeField] private bool disableAvatarMusic = true;
    
    [Tooltip("Connect avatar to TTS for lip sync")]
    [SerializeField] private bool connectToTTS = true;

    [Header("References (Auto-found if empty)")]
    [SerializeField] private GameObject avatarObject;
    [SerializeField] private VRChatTTS ttsManager;
    [SerializeField] private AvatarController avatarController;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool logDebugInfo = true;

    private AudioSource avatarAudioSource;
    private AudioSource ttsAudioSource;

    private void Start()
    {
        SetupAvatar();
    }

    [ContextMenu("Setup Avatar Now")]
    public void SetupAvatar()
    {
        Log("=== VR Avatar Setup Starting ===");

        // Step 1: Find Avatar
        if (!FindAvatar()) return;

        // Step 2: Position Avatar
        PositionAvatarInFrontOfCamera();

        // Step 3: Fix Audio
        SetupAudio();

        // Step 4: Connect to TTS
        if (connectToTTS)
        {
            ConnectToTTS();
        }

        // Step 5: Validate Setup
        ValidateSetup();

        Log("=== VR Avatar Setup Complete ===");
    }

    private bool FindAvatar()
    {
        if (avatarObject == null)
        {
            avatarObject = GameObject.Find(avatarName);
        }

        if (avatarObject == null)
        {
            Debug.LogError($"[VRAvatarSetup] Could not find avatar '{avatarName}'!");
            return false;
        }

        Log($"Found avatar: {avatarObject.name}");
        Log($"Avatar position: {avatarObject.transform.position}");
        Log($"Avatar active: {avatarObject.activeInHierarchy}");

        // Get components
        avatarController = avatarObject.GetComponent<AvatarController>();
        avatarAudioSource = avatarObject.GetComponent<AudioSource>();

        if (avatarController == null)
        {
            Debug.LogWarning("[VRAvatarSetup] Avatar has no AvatarController - animations won't work");
        }
        else
        {
            // Fix broken mesh references
            FixBrokenMeshReferences();
        }

        if (avatarAudioSource == null)
        {
            Log("Adding AudioSource to avatar");
            avatarAudioSource = avatarObject.AddComponent<AudioSource>();
            avatarAudioSource.playOnAwake = false;
            avatarAudioSource.spatialBlend = 1f; // 3D sound
        }

        return true;
    }

    private void FixBrokenMeshReferences()
    {
        if (avatarController == null || avatarObject == null) return;

        // Get the faceMesh field
        var faceMeshField = typeof(AvatarController).GetField("faceMesh",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (faceMeshField == null) return;

        var currentFaceMesh = faceMeshField.GetValue(avatarController) as SkinnedMeshRenderer;

        // Check if the mesh reference is broken
        bool needsFix = currentFaceMesh == null || currentFaceMesh.sharedMesh == null;

        if (needsFix)
        {
            Log("Face mesh reference is broken - attempting to fix...");

            // Find Renderer_Head child which contains the face mesh with blendshapes
            Transform headRenderer = avatarObject.transform.Find("Renderer_Head");
            
            if (headRenderer == null)
            {
                // Search recursively
                var allRenderers = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var renderer in allRenderers)
                {
                    if (renderer.name.Contains("Head") && renderer.sharedMesh != null && renderer.sharedMesh.blendShapeCount > 0)
                    {
                        headRenderer = renderer.transform;
                        break;
                    }
                }
            }

            if (headRenderer != null)
            {
                var headMesh = headRenderer.GetComponent<SkinnedMeshRenderer>();
                if (headMesh != null && headMesh.sharedMesh != null)
                {
                    faceMeshField.SetValue(avatarController, headMesh);
                    Log($"Fixed faceMesh reference -> {headMesh.name} (blendshapes: {headMesh.sharedMesh.blendShapeCount})");

                    // Also fix SimpleLipSync if present
                    var simpleLipSync = avatarObject.GetComponent<SimpleLipSync>();
                    if (simpleLipSync != null)
                    {
                        var lipSyncFaceMesh = typeof(SimpleLipSync).GetField("faceMesh",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        lipSyncFaceMesh?.SetValue(simpleLipSync, headMesh);
                        Log("Fixed SimpleLipSync faceMesh reference");
                    }

                    // Also fix AvatarSpeech if present
                    var avatarSpeech = avatarObject.GetComponent<AvatarSpeech>();
                    if (avatarSpeech != null)
                    {
                        var speechFaceMesh = typeof(AvatarSpeech).GetField("faceMesh",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        speechFaceMesh?.SetValue(avatarSpeech, headMesh);
                        Log("Fixed AvatarSpeech faceMesh reference");
                    }
                }
                else
                {
                    Debug.LogWarning($"[VRAvatarSetup] Found Renderer_Head but mesh is null or has no blendshapes");
                }
            }
            else
            {
                Debug.LogWarning("[VRAvatarSetup] Could not find Renderer_Head - listing all SkinnedMeshRenderers:");
                var allRenderers = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var r in allRenderers)
                {
                    int blendCount = r.sharedMesh?.blendShapeCount ?? 0;
                    Debug.Log($"  - {r.name}: mesh={r.sharedMesh?.name ?? "null"}, blendshapes={blendCount}");
                }
            }
        }
        else
        {
            Log($"Face mesh OK: {currentFaceMesh.name}, blendshapes: {currentFaceMesh.sharedMesh?.blendShapeCount ?? 0}");
        }
    }

    private void PositionAvatarInFrontOfCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            // Try to find any camera
            cam = FindObjectOfType<Camera>();
        }

        if (cam == null)
        {
            Debug.LogWarning("[VRAvatarSetup] No camera found - cannot position avatar");
            return;
        }

        // Calculate position in front of camera
        Vector3 cameraPos = cam.transform.position;
        Vector3 cameraForward = cam.transform.forward;
        cameraForward.y = 0; // Keep horizontal
        cameraForward.Normalize();

        Vector3 newPosition = cameraPos + (cameraForward * distanceFromCamera);
        newPosition.y = cameraPos.y + heightOffset;

        // Set position
        avatarObject.transform.position = newPosition;

        // Face the camera
        Vector3 lookDirection = cameraPos - newPosition;
        lookDirection.y = 0; // Keep upright
        if (lookDirection != Vector3.zero)
        {
            avatarObject.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        Log($"Positioned avatar at: {newPosition}");
        Log($"Camera is at: {cameraPos}");
    }

    private void SetupAudio()
    {
        if (avatarAudioSource == null) return;

        if (disableAvatarMusic)
        {
            // Stop any playing audio (like background music)
            if (avatarAudioSource.isPlaying)
            {
                avatarAudioSource.Stop();
                Log("Stopped avatar music");
            }

            // Disable play on awake
            avatarAudioSource.playOnAwake = false;

            // Clear any pre-assigned clip/resource
            avatarAudioSource.clip = null;

            Log("Disabled avatar music playback");
        }

        // Configure for TTS
        avatarAudioSource.loop = false;
        avatarAudioSource.volume = 1f;
    }

    private void ConnectToTTS()
    {
        // Find TTS Manager
        if (ttsManager == null)
        {
            ttsManager = FindObjectOfType<VRChatTTS>();
        }

        if (ttsManager == null)
        {
            Debug.LogWarning("[VRAvatarSetup] VRChatTTS not found - TTS connection skipped");
            return;
        }

        Log($"Found TTS Manager on: {ttsManager.gameObject.name}");

        // Get TTS AudioSource
        ttsAudioSource = ttsManager.GetComponent<AudioSource>();

        // Link avatar's AudioSource to AvatarController for lip sync
        if (avatarController != null)
        {
            var audioField = typeof(AvatarController).GetField("audioSource",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (audioField != null)
            {
                audioField.SetValue(avatarController, avatarAudioSource);
                Log("Linked AudioSource to AvatarController for lip sync");
            }
        }

        // Subscribe to TTS events
        ttsManager.OnSpeakingStarted += OnTTSStarted;
        ttsManager.OnSpeakingFinished += OnTTSFinished;

        Log("Connected to TTS events");
    }

    private void OnTTSStarted()
    {
        Log("TTS Started - Syncing audio to avatar");

        // Copy audio from TTS to avatar
        if (ttsAudioSource != null && avatarAudioSource != null && ttsAudioSource.clip != null)
        {
            avatarAudioSource.clip = ttsAudioSource.clip;
            avatarAudioSource.Play();

            // Optional: Mute TTS source so audio only comes from avatar
            // ttsAudioSource.mute = true;
        }
    }

    private void OnTTSFinished()
    {
        Log("TTS Finished");
    }

    private void ValidateSetup()
    {
        Log("--- Validation ---");

        // Check avatar visibility
        var renderers = avatarObject.GetComponentsInChildren<Renderer>(true);
        int enabledRenderers = 0;
        foreach (var r in renderers)
        {
            if (r.enabled) enabledRenderers++;
        }
        Log($"Renderers: {enabledRenderers}/{renderers.Length} enabled");

        // Check if in camera view
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            Vector3 viewportPos = cam.WorldToViewportPoint(avatarObject.transform.position);
            bool inView = viewportPos.x >= 0 && viewportPos.x <= 1 &&
                          viewportPos.y >= 0 && viewportPos.y <= 1 &&
                          viewportPos.z > 0;
            Log($"Avatar in camera view: {inView} (viewport: {viewportPos})");
        }

        // Check blendshapes
        if (avatarController != null)
        {
            var faceMeshField = typeof(AvatarController).GetField("faceMesh",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (faceMeshField != null)
            {
                var faceMesh = faceMeshField.GetValue(avatarController) as SkinnedMeshRenderer;
                if (faceMesh != null)
                {
                    int blendShapeCount = faceMesh.sharedMesh?.blendShapeCount ?? 0;
                    Log($"Face mesh blendshapes: {blendShapeCount}");

                    if (blendShapeCount == 0)
                    {
                        Debug.LogWarning("[VRAvatarSetup] Face mesh has NO blendshapes! Lip sync won't work.");
                    }
                }
                else
                {
                    Debug.LogWarning("[VRAvatarSetup] faceMesh is NULL on AvatarController!");
                }
            }
        }

        // Check AudioSource
        if (avatarAudioSource != null)
        {
            Log($"AudioSource: clip={avatarAudioSource.clip?.name ?? "none"}, playOnAwake={avatarAudioSource.playOnAwake}");
        }
    }

    private void Log(string message)
    {
        if (logDebugInfo)
        {
            Debug.Log($"[VRAvatarSetup] {message}");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (ttsManager != null)
        {
            ttsManager.OnSpeakingStarted -= OnTTSStarted;
            ttsManager.OnSpeakingFinished -= OnTTSFinished;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || avatarObject == null) return;

        // Draw avatar position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(avatarObject.transform.position, 0.5f);

        // Draw line to camera
        Camera cam = Camera.main ?? FindObjectOfType<Camera>();
        if (cam != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(avatarObject.transform.position, cam.transform.position);
        }
    }
}
