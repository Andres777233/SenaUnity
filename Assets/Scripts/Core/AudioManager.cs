using UnityEngine;
using UnityEngine.SceneManagement;

namespace Popayork.Core
{
    // Audio 100% procedural (PLACEHOLDER Fase 7): música de menú/misión,
    // pasos, clic, explosión y ambiente. Sin assets externos.
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource music;
        private AudioSource sfx;
        private AudioSource ambient;
        private AudioClip menuMusic;
        private AudioClip missionMusic;
        private AudioClip stepClip;
        private AudioClip clickClip;
        private AudioClip boomClip;
        private AudioClip windClip;
        private string currentTrack = string.Empty;
        private bool stepHigh;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = 0.45f;
            sfx = gameObject.AddComponent<AudioSource>();
            ambient = gameObject.AddComponent<AudioSource>();
            ambient.loop = true;
            ambient.volume = 0.25f;
            menuMusic = MusicPad();
            missionMusic = MusicDrive();
            stepClip = Tick(0.07f, 900f, 0.25f);
            clickClip = Tick(0.06f, 1400f, 0.3f);
            boomClip = Sweep(0.7f, 120f, 40f, 0.6f);
            windClip = NoiseLoop(4f, 0.18f);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool menu = scene.name == GameConfig.MainMenuScene || scene.name == GameConfig.CampaignScene;
            PlayTrack(menu ? "menu" : "mission");
            if (!menu && ambient != null && windClip != null && !ambient.isPlaying)
            {
                ambient.clip = windClip;
                ambient.Play();
            }
        }

        public void PlayTrack(string track)
        {
            if (currentTrack == track || music == null)
            {
                return;
            }
            currentTrack = track;
            music.clip = track == "menu" ? menuMusic : missionMusic;
            if (music.clip != null)
            {
                music.Play();
            }
        }

        public void PlayStep()
        {
            if (sfx == null || stepClip == null)
            {
                return;
            }
            stepHigh = !stepHigh;
            sfx.pitch = stepHigh ? 1.1f : 0.9f;
            sfx.PlayOneShot(stepClip);
            sfx.pitch = 1f;
        }

        public void PlayClick()
        {
            if (sfx != null && clickClip != null)
            {
                sfx.PlayOneShot(clickClip);
            }
        }

        public void PlayBoom()
        {
            if (sfx != null && boomClip != null)
            {
                sfx.PlayOneShot(boomClip);
            }
        }

        private static AudioClip Tick(float seconds, float freq, float volume)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = (1f - t) * (1f - t);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / rate) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create("PopayorkTick", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Sweep(float seconds, float from, float to, float volume)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
            float[] data = new float[samples];
            float phase = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(from, to, t);
                phase += 2f * Mathf.PI * freq / rate;
                float envelope = (1f - t) * (1f - t);
                data[i] = (Mathf.Sin(phase) + Mathf.Sin(phase * 2.7f) * 0.3f) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create("PopayorkBoom", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip NoiseLoop(float seconds, float volume)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
            float[] data = new float[samples];
            float last = 0f;
            for (int i = 0; i < samples; i++)
            {
                float white = (Mathf.Sin(i * 12.9898f) * 43758.5453f) % 1f;
                last = last * 0.98f + white * 0.02f;
                data[i] = last * volume * 2f;
            }
            data[samples - 1] = data[0];
            AudioClip clip = AudioClip.Create("PopayorkWind", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip MusicPad()
        {
            float[] chords = new float[] { 220f, 174.6f, 261.6f, 196f };
            return MusicLoop(8f, chords, 0.12f, false);
        }

        private static AudioClip MusicDrive()
        {
            float[] chords = new float[] { 110f, 110f, 130.8f, 98f };
            return MusicLoop(8f, chords, 0.2f, true);
        }

        private static AudioClip MusicLoop(float seconds, float[] roots, float volume, bool pulse)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(seconds * rate));
            float[] data = new float[samples];
            int steps = roots.Length;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                int step = Mathf.Min(steps - 1, (int)(t * steps));
                float root = roots[step];
                float local = (t * steps) - step;
                float env = pulse ? (local < 0.5f ? 1f : 0.4f) : (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI * 2f * steps));
                float v = Mathf.Sin(2f * Mathf.PI * root * i / rate)
                    + 0.5f * Mathf.Sin(2f * Mathf.PI * root * 1.5f * i / rate)
                    + 0.25f * Mathf.Sin(2f * Mathf.PI * root * 2f * i / rate);
                data[i] = v * env * volume * 0.5f;
            }
            AudioClip clip = AudioClip.Create("PopayorkMusic", samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
