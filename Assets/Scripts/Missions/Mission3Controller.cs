using UnityEngine;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Player;
using Popayork.UI;

namespace Popayork.Missions
{
    public enum Mission3State
    {
        Intro = 0,
        Riding = 1,
        Finished = 2
    }

    public class Mission3Controller : MonoBehaviour
    {
        public static Mission3Controller Instance { get; private set; }

        [SerializeField] private Mission3Config config;
        [SerializeField] private MissionUI missionUI;

        private Mission3State state;
        private int gateIndex;
        private PlayerHealth playerHealth;
        private Vehicles.CardboardController carton;

        public Mission3State State
        {
            get { return state; }
        }

        public int GateIndex
        {
            get { return gateIndex; }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            state = Mission3State.Intro;
            if (missionUI != null)
            {
                missionUI.ShowIntro();
            }
        }

        public void Bind(Mission3Config missionConfig, MissionUI ui)
        {
            config = missionConfig;
            missionUI = ui;
        }

        public void StartDescent()
        {
            if (config == null)
            {
                return;
            }
            state = Mission3State.Riding;
            gateIndex = 0;
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (missionUI != null)
            {
                missionUI.ShowObjective("¡Deslízate en cartón hasta el río!");
            }
            Say("¡Nos tiramos, parce! ¡Al río!");
        }

        // Un solo paso sin asignaciones: lo usa Update y el Verify.
        public void Simulate(float dt)
        {
            if (state != Mission3State.Riding || config == null)
            {
                return;
            }
            Vector3 target = transform.position;
            if (carton == null)
            {
                carton = FindAnyObjectByType<Vehicles.CardboardController>();
            }
            if (carton != null)
            {
                target = carton.transform.position;
            }
            if (config.gates != null && gateIndex < config.gates.Length)
            {
                CheckpointDef gate = config.gates[gateIndex];
                Vector3 diff = target - gate.position;
                diff.y = 0f;
                if (diff.sqrMagnitude <= gate.radius * gate.radius)
                {
                    if (playerHealth == null)
                    {
                        playerHealth = FindAnyObjectByType<PlayerHealth>();
                    }
                    if (playerHealth != null)
                    {
                        playerHealth.SetSpawn(gate.position + Vector3.up, 0f);
                        playerHealth.Refill();
                    }
                    gateIndex++;
                    Say("Checkpoint: " + gate.nombre + " (" + gateIndex + "/" + config.gates.Length + ")");
                }
            }
            Vector3 river = target - config.riverCenter;
            river.y = 0f;
            if (river.sqrMagnitude <= config.riverRadius * config.riverRadius)
            {
                EndFinished();
                return;
            }
            if (missionUI != null)
            {
                missionUI.SetProgress(TrackProgress(target));
            }
        }

        private float TrackProgress(Vector3 target)
        {
            if (config.routePoints == null || config.routePoints.Length < 2)
            {
                return 0f;
            }
            float best = 0f;
            float total = 0f;
            for (int i = 0; i < config.routePoints.Length - 1; i++)
            {
                Vector3 a = config.routePoints[i];
                Vector3 b = config.routePoints[i + 1];
                float segLen = (b - a).magnitude;
                Vector3 ab = b - a;
                float denom = ab.sqrMagnitude;
                float t = 0f;
                if (denom > 0.000001f)
                {
                    t = Mathf.Clamp01(Vector3.Dot(target - a, ab) / denom);
                }
                if (t >= 1f)
                {
                    best = total + segLen;
                }
                else if (t > 0f)
                {
                    float candidate = total + segLen * t;
                    if (candidate > best)
                    {
                        best = candidate;
                    }
                }
                total += segLen;
            }
            return total > 0f ? Mathf.Clamp01(best / total) : 0f;
        }

        private void EndFinished()
        {
            state = Mission3State.Finished;
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            if (!save.completedMissions.Contains(GameConfig.Mission3Scene))
            {
                save.completedMissions.Add(GameConfig.Mission3Scene);
            }
            if (save.completedMissions.Contains(GameConfig.Mission1Scene)
                && save.completedMissions.Contains(GameConfig.Mission2Scene)
                && !save.completedMissions.Contains("Campania_Completa"))
            {
                save.completedMissions.Add("Campania_Completa");
            }
            SaveSystem.Save(save);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReloadSave();
            }
            if (missionUI != null)
            {
                missionUI.ShowResult(true, "¡AL RÍO, PARCE!\nDescenso completado. Fin de la campaña (Fase 7: pulido).");
            }
        }

        private void Say(string line)
        {
            var ui = SubtitleSystem.Instance;
            if (ui != null)
            {
                ui.ShowLine(line, 3f);
            }
        }

        private void Update()
        {
            Simulate(Time.deltaTime);
        }

        public void Retry()
        {
            SceneLoader.Load(GameConfig.Mission3Scene);
        }

        public void ToMenu()
        {
            SceneLoader.Load(GameConfig.MainMenuScene);
        }
    }
}
