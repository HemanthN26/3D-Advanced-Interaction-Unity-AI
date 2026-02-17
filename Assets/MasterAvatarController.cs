using UnityEngine;
using System.Collections;

public class AvatarController : MonoBehaviour
{
    [Header("TTS Audio Source (for lip sync & gestures)")]
    [Tooltip("Assign the AudioSource used for TTS speech. Animations will only play when this is playing.")]
    public AudioSource ttsAudioSource;
    
    [Header("Background Music (optional)")]
    [Tooltip("Optional: Background music AudioSource. Set volume lower (0.1-0.3 recommended).")]
    public AudioSource backgroundMusicSource;
    [Range(0f, 1f)]
    public float backgroundMusicVolume = 0.15f;

    [Header("Lip Sync Settings")]
    public SkinnedMeshRenderer faceMesh;
    public int jawOpenBlendShape = 0;
    [Range(0f, 10f)]
    public float mouthOpenAmount = 3f;     // max jaw open (1-5 is safe)
    [Range(0.1f, 5f)]
    public float lipSyncSensitivity = 1f;  // sensitivity (0.5-2 is subtle)
    [Range(1f, 30f)]
    public float lipSyncSmoothing = 10f;   // smoothing speed
    private float jawValue = 0f;
    private float targetJawValue = 0f;
    private float[] audioSamples = new float[256];

    [Header("Breathing (Always Active)")]
    public Transform chestBone;
    public float breathAmount = 2f;
    public float breathSpeed = 1f;
    private Quaternion chestOriginalRot;

    [Header("Head Movement (During TTS)")]
    public Transform headBone;
    public float headMovementAmount = 2f;
    public float headMovementSpeed = 0.7f;
    private Quaternion headOriginalRot;

    [Header("Body Sway (During TTS)")]
    public Transform spineBone;
    public float swayAmount = 15f;
    public float swaySpeed = 15f;
    private Quaternion spineOriginalRot;

    [Header("Hands (Root Transforms)")]
    public Transform handSway;      // the swaying hand root
    public Transform handDown;      // the static lowered hand
    public float handAmplitude = 15f;
    public float handSpeed = 1f;
    private Quaternion handSwayOriginalRot;
    private Quaternion handDownOriginalRot;

    [Header("Arm Bones for Swaying Hand")]
    public Transform shoulderBone;
    public Transform elbowBone;
    public Transform wristBone;     // palm bone

    public float shoulderSwing = 5f;       // shoulder rotation amount
    public float elbowBendAmount = 20f;    // elbow bending amount
    public float palmRotateAmount = 20f;   // rotate palm toward user

    private Quaternion shoulderOriginalRot;
    private Quaternion elbowOriginalRot;
    private Quaternion wristOriginalRot;

    // Track if TTS is currently playing
    private bool isSpeaking = false;

    [Header("Debug")]
    public bool showDebugLogs = true;


    void Start()
    {
        // Save original rotations
        if (chestBone) chestOriginalRot = chestBone.localRotation;
        if (headBone) headOriginalRot = headBone.localRotation;
        if (spineBone) spineOriginalRot = spineBone.localRotation;

        if (handSway) handSwayOriginalRot = handSway.localRotation;

        if (handDown)
        {
            handDownOriginalRot = handDown.localRotation;

            // LOWER THIS HAND SLIGHTLY
            handDownOriginalRot *= Quaternion.Euler(15f, 0f, 0f);
            handSwayOriginalRot *= Quaternion.Euler(15f, 0f, 0f);
        }

        if (shoulderBone) shoulderOriginalRot = shoulderBone.localRotation;
        if (elbowBone) elbowOriginalRot = elbowBone.localRotation;
        if (wristBone) wristOriginalRot = wristBone.localRotation;

        // Set background music volume
        if (backgroundMusicSource)
        {
            backgroundMusicSource.volume = backgroundMusicVolume;
        }

        // Debug: Log setup status
        if (showDebugLogs)
        {
            Debug.Log($"[AvatarController] Start - TTS AudioSource: {(ttsAudioSource != null ? ttsAudioSource.gameObject.name : "NOT SET")}");
            Debug.Log($"[AvatarController] Start - Face Mesh: {(faceMesh != null ? faceMesh.gameObject.name : "NOT SET")}");
            if (faceMesh != null && faceMesh.sharedMesh != null)
            {
                Debug.Log($"[AvatarController] Face mesh has {faceMesh.sharedMesh.blendShapeCount} blendshapes, using index {jawOpenBlendShape}");
            }
        }
    }


    private bool wasSpeaking = false;

    void Update()
    {
        // Check if TTS is currently speaking
        isSpeaking = ttsAudioSource != null && ttsAudioSource.isPlaying;

        // Debug: Log when speaking state changes
        if (showDebugLogs && isSpeaking != wasSpeaking)
        {
            Debug.Log($"[AvatarController] Speaking state changed: {isSpeaking}");
            wasSpeaking = isSpeaking;
        }

        // Always do breathing (natural idle animation)
        Breathing();

        // Lip sync - only during TTS
        LipSync();

        // Gestures only during TTS speaking
        if (isSpeaking)
        {
            HeadMovement();
            BodySway();
            HandMovement();
        }
        else
        {
            // Smoothly return to idle pose when not speaking
            ReturnToIdle();
        }
    }


    // ------------------------------
    // Lip Sync - Audio Frequency Based
    // ------------------------------
    void LipSync()
    {
        if (!faceMesh) return;

        if (isSpeaking && ttsAudioSource != null)
        {
            // Get actual audio amplitude data
            ttsAudioSource.GetOutputData(audioSamples, 0);
            
            // Calculate loudness from samples
            float sum = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }
            
            // Normalize - keep value very small for subtle movement
            float loudness = Mathf.Clamp01(sum / audioSamples.Length * lipSyncSensitivity);
            
            // Target jaw value - VERY LOW for subtle lip movement
            targetJawValue = loudness * mouthOpenAmount;
        }
        else
        {
            // Close mouth when not speaking
            targetJawValue = 0f;
        }

        // Smooth the jaw movement for natural look
        jawValue = Mathf.Lerp(jawValue, targetJawValue, Time.deltaTime * lipSyncSmoothing);
        
        // HARD SAFETY LIMIT: Never exceed 5 to prevent any face deformation
        jawValue = Mathf.Clamp(jawValue, 0f, 5f);
        
        // Apply to blendshape
        faceMesh.SetBlendShapeWeight(jawOpenBlendShape, jawValue);
    }


    // ------------------------------
    // Breathing (Always Active)
    // ------------------------------
    void Breathing()
    {
        if (!chestBone) return;

        float breathRot = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        chestBone.localRotation = chestOriginalRot * Quaternion.Euler(breathRot, 0, 0);
    }


    // ------------------------------
    // Head movement (During TTS)
    // ------------------------------
    void HeadMovement()
    {
        if (!headBone) return;

        float x = Mathf.Sin(Time.time * headMovementSpeed) * headMovementAmount;
        float y = Mathf.Sin(Time.time * headMovementSpeed * 5f) * headMovementAmount;

        Quaternion targetRot = headOriginalRot * Quaternion.Euler(x, y, 0);
        headBone.localRotation = Quaternion.Lerp(headBone.localRotation, targetRot, Time.deltaTime * 2f);
    }


    // ------------------------------
    // Body sway (During TTS)
    // ------------------------------
    void BodySway()
    {
        if (!spineBone) return;

        float rot = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        Quaternion targetRot = spineOriginalRot * Quaternion.Euler(0, rot, 0);

        spineBone.localRotation = Quaternion.Lerp(spineBone.localRotation, targetRot, Time.deltaTime * 2f);
    }


    // ------------------------------
    // Return to Idle Pose
    // ------------------------------
    void ReturnToIdle()
    {
        float smoothSpeed = Time.deltaTime * 3f;

        // Head returns to original
        if (headBone)
        {
            headBone.localRotation = Quaternion.Lerp(headBone.localRotation, headOriginalRot, smoothSpeed);
        }

        // Spine returns to original
        if (spineBone)
        {
            spineBone.localRotation = Quaternion.Lerp(spineBone.localRotation, spineOriginalRot, smoothSpeed);
        }

        // Arms return to original
        if (shoulderBone)
        {
            shoulderBone.localRotation = Quaternion.Lerp(shoulderBone.localRotation, shoulderOriginalRot, smoothSpeed);
        }
        if (elbowBone)
        {
            elbowBone.localRotation = Quaternion.Lerp(elbowBone.localRotation, elbowOriginalRot, smoothSpeed);
        }
        if (wristBone)
        {
            wristBone.localRotation = Quaternion.Lerp(wristBone.localRotation, wristOriginalRot, smoothSpeed);
        }
        if (handSway)
        {
            handSway.localRotation = Quaternion.Lerp(handSway.localRotation, handSwayOriginalRot, smoothSpeed);
        }
        if (handDown)
        {
            handDown.localRotation = Quaternion.Lerp(handDown.localRotation, handDownOriginalRot, smoothSpeed);
        }
    }


    // ------------------------------
    // Full Arm + Hand Swaying Motion (During TTS)
    // ------------------------------
    void HandMovement()
    {
        float t = Mathf.Sin(Time.time * handSpeed);

        // -----------------------------
        // SHOULDER ROTATION
        // -----------------------------
        if (shoulderBone)
        {
            float sh = t * shoulderSwing;
            shoulderBone.localRotation = shoulderOriginalRot * Quaternion.Euler(0, sh, 0);
        }

        // -----------------------------
        // ELBOW BENDING
        // -----------------------------
        if (elbowBone)
        {
            float bend = Mathf.Abs(t) * elbowBendAmount;
            elbowBone.localRotation = elbowOriginalRot * Quaternion.Euler(bend, 0, 0);
        }

        // -----------------------------
        // PALM / WRIST ROTATION
        // makes the palm face the user
        // -----------------------------
        if (wristBone)
        {
            float palm = t * palmRotateAmount;
            wristBone.localRotation = wristOriginalRot * Quaternion.Euler(0, palm, 0);
        }

        // -----------------------------
        // ROOT HAND SWAY ROTATION
        // -----------------------------
        if (handSway)
        {
            float angle = t * handAmplitude;
            handSway.localRotation = handSwayOriginalRot * Quaternion.Euler(0, angle, 0);
        }

        // -----------------------------
        // STATIC LOWERED HAND
        // -----------------------------
        if (handDown)
        {
            handDown.localRotation = handDownOriginalRot;
        }
    }


    // ------------------------------
    // Public Methods for TTS Integration
    // ------------------------------
    
    /// <summary>
    /// Call this to set the TTS AudioSource at runtime
    /// </summary>
    public void SetTTSAudioSource(AudioSource source)
    {
        ttsAudioSource = source;
        if (showDebugLogs)
        {
            Debug.Log($"[AvatarController] TTS AudioSource SET to: {(source != null ? source.gameObject.name : "null")}");
        }
    }

    /// <summary>
    /// Call this to set background music volume at runtime
    /// </summary>
    public void SetBackgroundMusicVolume(float volume)
    {
        backgroundMusicVolume = Mathf.Clamp01(volume);
        if (backgroundMusicSource)
        {
            backgroundMusicSource.volume = backgroundMusicVolume;
        }
    }

    /// <summary>
    /// Debug: Check current status
    /// </summary>
    [ContextMenu("Debug: Check Status")]
    public void DebugCheckStatus()
    {
        Debug.Log("=== AvatarController Status ===");
        Debug.Log($"TTS AudioSource: {(ttsAudioSource != null ? ttsAudioSource.gameObject.name : "NOT SET!")}");
        Debug.Log($"TTS Is Playing: {(ttsAudioSource != null ? ttsAudioSource.isPlaying.ToString() : "N/A")}");
        Debug.Log($"Face Mesh: {(faceMesh != null ? faceMesh.gameObject.name : "NOT SET!")}");
        if (faceMesh != null && faceMesh.sharedMesh != null)
        {
            Debug.Log($"Blendshape Count: {faceMesh.sharedMesh.blendShapeCount}");
            Debug.Log($"Using Blendshape Index: {jawOpenBlendShape}");
            if (faceMesh.sharedMesh.blendShapeCount > 0)
            {
                for (int i = 0; i < Mathf.Min(5, faceMesh.sharedMesh.blendShapeCount); i++)
                {
                    Debug.Log($"  [{i}] {faceMesh.sharedMesh.GetBlendShapeName(i)}");
                }
                if (faceMesh.sharedMesh.blendShapeCount > 5)
                {
                    Debug.Log($"  ... and {faceMesh.sharedMesh.blendShapeCount - 5} more");
                }
            }
        }
        Debug.Log($"isSpeaking: {isSpeaking}");
        Debug.Log($"Current Jaw Value: {jawValue}");
        Debug.Log("===============================");
    }
}
