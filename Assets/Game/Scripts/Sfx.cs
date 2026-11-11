using System.Collections.Generic;
using UnityEngine;

namespace MichiCosmico
{
    /// <summary>Efectos de sonido sintetizados en runtime (sin archivos de audio).</summary>
    public static class Sfx
    {
        const int SampleRate = 44100;

        static AudioSource source;
        static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        public static void Init(AudioSource audioSource)
        {
            source = audioSource;
        }

        public static void Play(string id, float volume = 0.7f, float pitch = 1f)
        {
            if (source == null) return;
            var clip = Get(id);
            if (clip == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        static AudioClip Get(string id)
        {
            if (Clips.TryGetValue(id, out var cached) && cached != null) return cached;

            AudioClip clip = id switch
            {
                "pickup" => Sweep("pickup", 880f, 1760f, 0.14f, 0.55f),
                "dash" => Sweep("dash", 280f, 880f, 0.11f, 0.35f),
                "hit" => Sweep("hit", 220f, 55f, 0.35f, 0.9f, square: true),
                "win" => Arpeggio("win", new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.10f),
                "lose" => Arpeggio("lose", new[] { 392.00f, 329.63f, 261.63f, 196.00f }, 0.14f),
                _ => null
            };

            Clips[id] = clip;
            return clip;
        }

        static AudioClip Sweep(string name, float from, float to, float duration, float volume, bool square = false)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
            var data = new float[count];
            float phase = 0f;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float freq = Mathf.Lerp(from, to, t * (2f - t));
                phase += 2f * Mathf.PI * freq / SampleRate;

                float sample = Mathf.Sin(phase);
                if (square) sample = Mathf.Sign(sample) * 0.55f;

                float attack = Mathf.Min(1f, i / (SampleRate * 0.006f));
                float envelope = attack * Mathf.Pow(1f - t, 1.8f);
                data[i] = sample * envelope * volume;
            }

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Arpeggio(string name, float[] notes, float noteDuration)
        {
            int perNote = Mathf.Max(1, Mathf.CeilToInt(SampleRate * noteDuration));
            int count = perNote * notes.Length;
            var data = new float[count];

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                for (int i = 0; i < perNote; i++)
                {
                    float t = i / (float)perNote;
                    float attack = Mathf.Min(1f, i / (SampleRate * 0.004f));
                    float envelope = attack * Mathf.Pow(1f - t, 1.3f);

                    float sample = Mathf.Sin(2f * Mathf.PI * freq * i / SampleRate) * 0.7f
                                 + Mathf.Sin(2f * Mathf.PI * freq * 2f * i / SampleRate) * 0.2f;

                    data[n * perNote + i] += sample * envelope * 0.5f;
                }
            }

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
