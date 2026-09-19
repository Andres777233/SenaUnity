using UnityEngine;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.Player;
using Popayork.UI;
using Popayork.Vehicles;

namespace Popayork.Missions
{
    public enum Mission2State
    {
        Intro = 0,
        Route = 1,
        CheckpointDone = 2,
        Arrived = 3
    }

    public class Mission2Controller : MonoBehaviour
    {
        public static Mission2Controller Instance { get; private set; }

        [SerializeField] private Mission2Config config;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private AgentPool pool;
        [SerializeField] private MissionUI missionUI;
        [SerializeField] private CompassUI compass;

        private Mission2State state;
        private int checkpointIndex;
        private bool fightSpawned;
        private float promptCooldown;
        private PlayerController player;
        private HorseController horse;

        public Mission2State State
        {
            get { return state; }
        }

        public int CheckpointIndex
        {
            get { return checkpointIndex; }
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
            state = Mission2State.Intro;
            if (missionUI != null)
            {
                missionUI.ShowIntro();
            }
        }

        public void Bind(Mission2Config missionConfig, WaveManager manager, AgentPool agentPool, MissionUI ui, CompassUI compassUI)
        {
            config = missionConfig;
            waveManager = manager;
            pool = agentPool;
            missionUI = ui;
            compass = compassUI;
        }

        public void StartRoute()
        {
            if (config == null || config.checkpoints == null || config.checkpoints.Length == 0)
            {
                return;
            }
            state = Mission2State.Route;
            checkpointIndex = 0;
            fightSpawned = false;
            promptCooldown = 0f;
            player = FindAnyObjectByType<PlayerController>();
            horse = FindAnyObjectByType<HorseController>();
            if (missionUI != null)
            {
                missionUI.ShowObjective("A caballo hasta el Morro de Tulcán");
            }
            Say("¡Al Morro, parceros! Los caballos esperan. Pulsa E para montar.");
        }

        // Un solo paso de ruta sin asignaciones: lo usa Update y el Verify.
        public void Simulate(float dt)
        {
            if (state != Mission2State.Route || config == null)
            {
                return;
            }
            promptCooldown = Mathf.Max(0f, promptCooldown - dt);
            UpdateMountPrompt();
            if (checkpointIndex >= config.checkpoints.Length)
            {
                return;
            }
            CheckpointDef cp = config.checkpoints[checkpointIndex];
            Vector3 target = player != null ? player.transform.position : transform.position;
            if (horse != null && horse.IsMounted)
            {
                target = horse.transform.position;
            }
            Vector3 diff = target - cp.position;
            diff.y = 0f;
            bool inside = diff.sqrMagnitude <= cp.radius * cp.radius;
            if (cp.kind == CheckpointKind.Fight)
            {
                SimulateFight(dt, cp, inside);
            }
            else if (inside)
            {
                AdvanceCheckpoint(cp);
            }
            if (missionUI != null && config.checkpoints.Length > 0)
            {
                missionUI.SetProgress((float)checkpointIndex / config.checkpoints.Length);
            }
        }

        private void SimulateFight(float dt, CheckpointDef cp, bool inside)
        {
            if (!fightSpawned && inside)
            {
                fightSpawned = true;
                if (cp.fightWave != null && waveManager != null)
                {
                    waveManager.SpawnWaveNow(cp.fightWave);
                }
                Say("¡Emboscada! ¡A pie, parce, a pie! (E para desmontar)");
            }
            if (fightSpawned && pool != null && CountHostiles() == 0)
            {
                fightSpawned = false;
                AdvanceCheckpoint(cp);
            }
        }

        private int CountHostiles()
        {
            if (pool == null)
            {
                return 0;
            }
            int n = 0;
            for (int i = 0; i < pool.ActiveCount; i++)
            {
                AgentBrain brain = pool.GetActive(i);
                if (brain != null && brain.IsHostile && !brain.Health.IsDead)
                {
                    n++;
                }
            }
            return n;
        }

        private void AdvanceCheckpoint(CheckpointDef cp)
        {
            checkpointIndex++;
            fightSpawned = false;
            Say("Checkpoint: " + cp.nombre + " (" + checkpointIndex + "/" + config.checkpoints.Length + ")");
            if (checkpointIndex >= config.checkpoints.Length)
            {
                state = Mission2State.Arrived;
                SaveArrival();
                if (missionUI != null)
                {
                    missionUI.ShowResult(true, "¡LLEGAMOS AL MORRO!\nLa defensa sigue en la Fase 5B, parce.");
                }
            }
        }

        private void UpdateMountPrompt()
        {
            if (promptCooldown > 0f || horse == null)
            {
                return;
            }
            if (horse.IsRiderNear())
            {
                promptCooldown = 5f;
                Say("Pulsa E para montar el caballo.");
            }
        }

        private void SaveArrival()
        {
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            if (!save.completedMissions.Contains("Mision2_Ruta"))
            {
                save.completedMissions.Add("Mision2_Ruta");
            }
            SaveSystem.Save(save);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReloadSave();
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
            SceneLoader.Load(GameConfig.Mission2Scene);
        }

        public void ToMenu()
        {
            SceneLoader.Load(GameConfig.MainMenuScene);
        }
    }
}
