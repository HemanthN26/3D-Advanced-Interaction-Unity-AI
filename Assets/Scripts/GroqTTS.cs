using NaughtyAttributes;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Available voices for Groq Orpheus TTS (Canopy Labs)
/// </summary>
public enum OrpheusVoice
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

public class GroqTTS : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private const string apiUrl = "https://api.groq.com/openai/v1/audio/speech";
    [SerializeField] private string apiKey = "your_groq_api_key_here";
    private const string model = "canopylabs/orpheus-v1-english";
    [SerializeField] private OrpheusVoice selectedVoice = OrpheusVoice.Troy;
    private const string responseFormat = "wav";
    private const int maxInputLength = 200; // Orpheus has 200 character limit
    [SerializeField] private string prompt = "I love building and shipping new features for our students!";

    [Button]
    private async void Generate()
    {
        await GenerateAndPlaySpeech(prompt);
    }

    public async Task GenerateAndPlaySpeech(string text)
    {
        // Split text into chunks if it exceeds Orpheus limit (200 chars)
        var chunks = SplitTextIntoChunks(text, maxInputLength);
        
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        foreach (var chunk in chunks)
        {
            // Escape text for JSON
            string escapedText = chunk.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
            var json = $"{{\"model\":\"{model}\",\"voice\":\"{GetVoiceName(selectedVoice)}\",\"input\":\"{escapedText}\",\"response_format\":\"{responseFormat}\"}}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"TTS API call failed: {response.StatusCode}");
                    Debug.LogError(await response.Content.ReadAsStringAsync());
                    return;
                }

                byte[] audioData = await response.Content.ReadAsByteArrayAsync();
                AudioClip clip = CreateClipFromWav(audioData);
                audioSource.clip = clip;
                audioSource.Play();
                
                // Wait for audio to finish before playing next chunk
                while (audioSource.isPlaying)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error generating TTS: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Split text into chunks that fit within the Orpheus character limit.
    /// Tries to split at sentence boundaries for natural speech.
    /// </summary>
    private System.Collections.Generic.List<string> SplitTextIntoChunks(string text, int maxLength)
    {
        var chunks = new System.Collections.Generic.List<string>();
        
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

    private string GetVoiceName(OrpheusVoice voice)
    {
        // Orpheus uses lowercase voice names
        return voice.ToString().ToLower();
    }

    private AudioClip CreateClipFromWav(byte[] wav)
    {
        int channels = BitConverter.ToInt16(wav, 22);
        int sampleRate = BitConverter.ToInt32(wav, 24);
        int bitsPerSample = BitConverter.ToInt16(wav, 34);

        int dataStart = FindDataChunk(wav) + 8;
        int sampleCount = (wav.Length - dataStart) / (bitsPerSample / 8);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            int sampleIndex = dataStart + i * 2; // assuming 16-bit PCM
            short sample = BitConverter.ToInt16(wav, sampleIndex);
            samples[i] = sample / 32768f;
        }

        AudioClip clip = AudioClip.Create("GroqTTS_Audio", sampleCount / channels, channels, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private int FindDataChunk(byte[] wav)
    {
        for (int i = 12; i < wav.Length - 4; i++)
        {
            if (wav[i] == 'd' && wav[i + 1] == 'a' && wav[i + 2] == 't' && wav[i + 3] == 'a')
            {
                return i;
            }
        }
        throw new Exception("DATA chunk not found in WAV");
    }
}
