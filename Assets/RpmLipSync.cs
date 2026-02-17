using UnityEngine;

public class SimpleLipSync : MonoBehaviour
{
    [Header("Face Mesh")]
    public SkinnedMeshRenderer faceMesh;
    public string jawBlendshape = "mouthOpen"; // exact name from BlendShapes

    [Header("TTS Audio Source")]
    [Tooltip("Assign the AudioSource used for TTS speech. Lip sync only activates when this plays.")]
    public AudioSource ttsAudioSource;

    [Header("Lip Sync Settings")]
    [Range(0.1f, 5f)]
    public float sensitivity = 1f;         // sensitivity (0.5-2 is subtle)
    [Range(0f, 10f)]
    public float maxMouthOpen = 3f;        // max mouth open (1-5 is safe)
    [Range(1f, 30f)]
    public float smoothSpeed = 10f;        // smoothing speed

    private int jawIndex;
    private float currentJawValue = 0f;
    private float targetJawValue = 0f;
    private float[] audioSamples = new float[256];

    void Start()
    {
        jawIndex = faceMesh.sharedMesh.GetBlendShapeIndex(jawBlendshape);
    }

    void Update()
    {
        // Only animate when TTS is playing
        if (ttsAudioSource != null && ttsAudioSource.isPlaying)
        {
            // Get audio output data for frequency analysis
            ttsAudioSource.GetOutputData(audioSamples, 0);
            
            // Calculate loudness from samples
            float sum = 0f;
            for (int i = 0; i < audioSamples.Length; i++)
            {
                sum += Mathf.Abs(audioSamples[i]);
            }
            
            // Normalize and apply sensitivity
            float loudness = Mathf.Clamp01(sum / audioSamples.Length * sensitivity);
            
            // Set target jaw value - SAFETY CLAMP to max 30
            targetJawValue = Mathf.Min(loudness * maxMouthOpen, 30f);
        }
        else
        {
            // Close mouth when not speaking
            targetJawValue = 0f;
        }

        // Smooth the jaw movement
        currentJawValue = Mathf.Lerp(currentJawValue, targetJawValue, Time.deltaTime * smoothSpeed);
        
        // HARD SAFETY LIMIT: Never exceed 5 to prevent any face deformation
        currentJawValue = Mathf.Clamp(currentJawValue, 0f, 5f);
        
        // Apply to blendshape
        faceMesh.SetBlendShapeWeight(jawIndex, currentJawValue);
    }

    /// <summary>
    /// Set the TTS AudioSource at runtime
    /// </summary>
    public void SetTTSAudioSource(AudioSource source)
    {
        ttsAudioSource = source;
    }
}
