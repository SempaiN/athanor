using System;
using UnityEngine;

namespace Athanor.Game
{
    /// Audio placeholder 100% procedural: SFX sintetizados y ambiente en loop.
    /// Cuando lleguen los assets reales (music_lab_loop.ogg, sfx_*.wav) se cargan en su lugar.
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource sfx;
        AudioSource ambient;

        AudioClip clickClip, buyClip, discoverClip, achievementClip, prestigeClip;

        const int Rate = 44100;

        void Awake()
        {
            Instance = this;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            ambient = gameObject.AddComponent<AudioSource>();
            ambient.playOnAwake = false;
            ambient.loop = true;
            ambient.volume = 0.10f;

            clickClip = Tone("click", new[] { 660f, 880f }, 0.07f, 6f, 0.5f);
            buyClip = Tone("buy", new[] { 523f, 659f }, 0.16f, 5f, 0.5f);
            discoverClip = Arpeggio("discover", new[] { 440f, 554f, 659f }, 0.09f, 0.5f);
            achievementClip = Arpeggio("achievement", new[] { 523f, 659f, 784f }, 0.11f, 0.5f);
            prestigeClip = Tone("prestige", new[] { 220f, 277f, 330f, 440f }, 0.9f, 2.5f, 0.6f);

            ambient.clip = AmbientLoop();
            ApplySoundSetting();
        }

        bool SoundOn => GameController.Instance == null || !GameController.Instance.State.SoundOff;

        public void ApplySoundSetting()
        {
            if (SoundOn) { if (!ambient.isPlaying) ambient.Play(); }
            else ambient.Stop();
        }

        void Play(AudioClip clip, float volume = 0.5f)
        {
            if (SoundOn && clip != null) sfx.PlayOneShot(clip, volume);
        }

        public void Click() => Play(clickClip, 0.35f);
        public void Buy() => Play(buyClip, 0.5f);
        public void Discover() => Play(discoverClip, 0.55f);
        public void Achievement() => Play(achievementClip, 0.6f);
        public void Prestige() => Play(prestigeClip, 0.7f);

        // ---- Síntesis ----

        /// Acorde simple con caída exponencial.
        static AudioClip Tone(string name, float[] freqs, float duration, float decay, float gain)
        {
            int n = (int)(Rate * duration);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-decay * t / duration);
                float v = 0;
                foreach (var f in freqs)
                    v += Mathf.Sin(2 * Mathf.PI * f * t);
                data[i] = v / freqs.Length * env * gain;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// Notas en secuencia (campanita ascendente).
        static AudioClip Arpeggio(string name, float[] freqs, float noteDur, float gain)
        {
            int perNote = (int)(Rate * noteDur);
            int n = perNote * freqs.Length + Rate / 8; // colita de release
            var data = new float[n];
            for (int note = 0; note < freqs.Length; note++)
            {
                for (int i = 0; i < perNote * 2 && note * perNote + i < n; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Exp(-8f * t / noteDur / 2f);
                    data[note * perNote + i] += Mathf.Sin(2 * Mathf.PI * freqs[note] * t) * env * gain;
                }
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// Pad místico en loop: dos senos graves con batido lento + brillo tenue.
        static AudioClip AmbientLoop()
        {
            const float seconds = 12f;
            int n = (int)(Rate * seconds);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * t / seconds); // loop perfecto
                float v =
                    Mathf.Sin(2 * Mathf.PI * 110f * t) * 0.5f +
                    Mathf.Sin(2 * Mathf.PI * 164.8f * t) * 0.3f +
                    Mathf.Sin(2 * Mathf.PI * 329.6f * t) * 0.08f * lfo;
                data[i] = v * (0.35f + 0.3f * lfo);
            }
            var clip = AudioClip.Create("ambient", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
