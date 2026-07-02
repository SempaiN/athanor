using System;
using UnityEngine;

namespace Athanor.Game
{
    /// Audio placeholder 100% procedural: SFX sintetizados y ambiente con progresión
    /// de acordes en loop perfecto. Se reemplaza por assets reales cuando existan.
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource sfx;
        AudioSource ambient;

        AudioClip clickClip, buyClip, discoverClip, achievementClip, prestigeClip;

        const int Rate = 44100;
        const float AmbientBaseVolume = 0.10f;

        void Awake()
        {
            Instance = this;

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            ambient = gameObject.AddComponent<AudioSource>();
            ambient.playOnAwake = false;
            ambient.loop = true;

            clickClip = Tone("click", new[] { 660f, 880f }, 0.07f, 6f, 0.5f);
            buyClip = Tone("buy", new[] { 523f, 659f }, 0.16f, 5f, 0.5f);
            discoverClip = Arpeggio("discover", new[] { 440f, 554f, 659f }, 0.09f, 0.5f);
            achievementClip = Arpeggio("achievement", new[] { 523f, 659f, 784f }, 0.11f, 0.5f);
            prestigeClip = Tone("prestige", new[] { 220f, 277f, 330f, 440f }, 0.9f, 2.5f, 0.6f);

            ambient.clip = AmbientLoop();
            ApplySoundSetting();
        }

        Athanor.Domain.GameState S => GameController.Instance != null ? GameController.Instance.State : null;

        bool SoundOn => S == null || !S.SoundOff;
        float MusicVol => S == null ? 1f : S.MusicVolume;
        float SfxVol => S == null ? 1f : S.SfxVolume;

        public void ApplySoundSetting()
        {
            ambient.volume = AmbientBaseVolume * MusicVol;
            if (SoundOn && MusicVol > 0.01f) { if (!ambient.isPlaying) ambient.Play(); }
            else ambient.Stop();
        }

        void Play(AudioClip clip, float volume = 0.5f)
        {
            if (SoundOn && clip != null) sfx.PlayOneShot(clip, volume * SfxVol);
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
            int n = perNote * freqs.Length + Rate / 8;
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

        /// Pad ambiente: progresión Am → F → C → G en registro grave, 24 s, loop perfecto.
        /// Las frecuencias se cuantizan a ciclos enteros del loop para evitar clicks al repetir.
        static AudioClip AmbientLoop()
        {
            const float seconds = 24f;
            const float chordDur = 6f;
            int n = (int)(Rate * seconds);

            float[][] chords =
            {
                Quantize(new[] { 110.0f, 130.8f, 164.8f }, seconds), // Am
                Quantize(new[] {  87.3f, 130.8f, 174.6f }, seconds), // F
                Quantize(new[] { 130.8f, 164.8f, 196.0f }, seconds), // C
                Quantize(new[] {  98.0f, 146.8f, 196.0f }, seconds), // G
            };

            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float slot = t / chordDur;
                int cur = (int)slot % 4;
                int next = (cur + 1) % 4;
                float frac = slot - (int)slot;

                // crossfade cosenoidal en el último cuarto de cada acorde
                float mix = frac < 0.75f ? 0f : (frac - 0.75f) / 0.25f;
                mix = 0.5f - 0.5f * Mathf.Cos(mix * Mathf.PI);

                float v = ChordSample(chords[cur], t) * (1f - mix) +
                          ChordSample(chords[next], t) * mix;

                float breath = 0.75f + 0.25f * Mathf.Sin(2 * Mathf.PI * t / seconds); // loop-safe
                data[i] = v * 0.55f * breath;
            }

            var clip = AudioClip.Create("ambient", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float ChordSample(float[] freqs, float t)
        {
            float v = 0;
            for (int k = 0; k < freqs.Length; k++)
                v += Mathf.Sin(2 * Mathf.PI * freqs[k] * t) * (k == 0 ? 0.5f : 0.3f);
            return v;
        }

        /// Ajusta cada frecuencia al múltiplo entero de ciclos más cercano dentro del loop.
        static float[] Quantize(float[] freqs, float loopSeconds)
        {
            var outp = new float[freqs.Length];
            for (int i = 0; i < freqs.Length; i++)
                outp[i] = Mathf.Round(freqs[i] * loopSeconds) / loopSeconds;
            return outp;
        }
    }
}
