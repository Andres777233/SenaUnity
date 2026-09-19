using UnityEngine;

namespace Popayork.Weapons
{
    [RequireComponent(typeof(AudioSource))]
    public class WeaponAudio : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip shotClip;
        private AudioClip dryClip;
        private AudioClip reloadClip;
        private AudioClip hitClip;
        private float pitch = 1.0f;

        private void Awake()
        {
            EnsureSource();
            source.playOnAwake = false;
            source.spatialBlend = 0.0f;
        }

        private void EnsureSource()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
        }

        public void Configure(float shotPitch)
        {
            EnsureSource();
            pitch = Mathf.Clamp(shotPitch, 0.5f, 2.0f);
            shotClip = Synth(0.16f, 220.0f * pitch, 90.0f * pitch, 0.5f);
            dryClip = Synth(0.06f, 1200.0f, 900.0f, 0.25f);
            reloadClip = Synth(0.12f, 500.0f, 700.0f, 0.3f);
            hitClip = Synth(0.08f, 1500.0f, 1800.0f, 0.3f);
        }

        public void PlayShot()
        {
            EnsureSource();
            if (source != null && shotClip != null)
            {
                source.PlayOneShot(shotClip);
            }
        }

        public void PlayDry()
        {
            EnsureSource();
            if (source != null && dryClip != null)
            {
                source.PlayOneShot(dryClip);
            }
        }

        public void PlayReload()
        {
            EnsureSource();
            if (source != null && reloadClip != null)
            {
                source.PlayOneShot(reloadClip);
            }
        }

        public void PlayHit()
        {
            EnsureSource();
            if (source != null && hitClip != null)
            {
                source.PlayOneShot(hitClip);
            }
        }

        // Sonido placeholder 100% procedural: barrido de seno con decaimiento.
        private static AudioClip Synth(float seconds, float freqStart, float freqEnd, float volume)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
            float[] data = new float[samples];
            float phase = 0.0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(freqStart, freqEnd, t);
                phase += 2.0f * Mathf.PI * freq / rate;
                float envelope = (1.0f - t) * (1.0f - t);
                float noise = (Mathf.Sin(phase * 3.7f) * 0.25f);
                data[i] = (Mathf.Sin(phase) + noise) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create("PopayorkSynth", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
