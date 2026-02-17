using UnityEngine;

public class AvatarSpeech : MonoBehaviour
{
    public SkinnedMeshRenderer faceMesh;   // assign head mesh here
    public AudioSource audioSource;        // assign AudioSource here

    public void Speak(AudioClip clip)
    {
        audioSource.clip = clip;   // put audio into the AudioSource
        audioSource.Play();        // start playing the audio
        // LipSync system will animate jaw automatically
    }
}



