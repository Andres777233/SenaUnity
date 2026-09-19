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
        Arrived = 3,
        Defense = 4,
        Retreat = 5,
        Victory = 6,
        Defeat = 7
    }

    public class Mission2Controller : MonoBehaviour
    {
        public static Mission2Controller Instance { get; private set; }

        [SerializeField] private Mission2Config config;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private WaveManager defenseWaves;
        [SerializeField] private AgentPool pool;
        [SerializeField] private MissionUI missionUI;
        [SerializeField] private CompassUI compass;

        private Mission2State state;
        private int checkpointIndex;
        private bool fightSpawned;
        private float promptCooldown;
        private PlayerController player;
        private HorseController horse;
        private int defenseIndex;
        private bool defenseStarted;
        private float arrivalTimer;
        private int deaths;
        private bool wasDead;
        private bool exitActive;
        private Popayork.Player.PlayerHealth playerHealth;

        public Mission2State State
        {
            get { return state; }
        }

        public int CheckpointIndex
        {
            get { return checkpointIndex; }
        }

        public bool ExitActive
        {
            get { return exitActive; }
        }

        public int Deaths
        {
            get { return deaths; }
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

        public void BindDefense(WaveManager manager)
        {
            defenseWaves = manager;
        }

        private WaveManager ActiveWaves()
        {
            if ((state == Mission2State.Defense || state == Mission2State.Retreat) && defenseWaves != null)
            {
                return defenseWaves;
            }
            return waveManager;
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

        // Un solo paso de misión sin asignaciones: lo usa Update y el Verify.
        public void Simulate(float dt)
        {
            if (config == null)
            {
                return;
            }
            if (state == Mission2State.Route)
            {
                SimulateRoute(dt);
                return;
            }
            if (state == Mission2State.Arrived)
            {
                arrivalTimer -= dt;
                if (arrivalTimer <= 0f)
                {
                    StartDefense();
                }
                return;
            }
            if (state == Mission2State.Defense || state == Mission2State.Retreat)
            {
                SimulateDefense(dt);
            }
        }

        private void SimulateRoute(float dt)
        {
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
                arrivalTimer = config.defenseGrace > 0f ? config.defenseGrace : 5f;
                SaveArrival();
                Say("¡Llegamos al Morro! Atrincherarse, parceros, que ahí vienen.");
                if (missionUI != null)
                {
                    missionUI.ShowObjective("Defiende el Morro de Tulcán");
                }
            }
        }

        public void StartDefense()
        {
            if (config == null || config.defenseWaves == null || config.defenseWaves.Length == 0)
            {
                return;
            }
            state = Mission2State.Defense;
            defenseIndex = 0;
            defenseStarted = true;
            deaths = 0;
            wasDead = false;
            exitActive = false;
            player = FindAnyObjectByType<PlayerController>();
            horse = FindAnyObjectByType<HorseController>();
            playerHealth = FindAnyObjectByType<Popayork.Player.PlayerHealth>();
            SpawnDefenseAllies();
            ActiveWaves().StartWave(config.defenseWaves[0]);
            if (missionUI != null)
            {
                missionUI.ShowObjective("Defiende el Morro de Tulcán");
            }
            Say("¡Ahí viene la tomba con los SMART! ¡Aguanten el Morro!");
        }

        private void SimulateDefense(float dt)
        {
            PollDeaths();
            if (state != Mission2State.Defense && state != Mission2State.Retreat)
            {
                return;
            }
            if (deaths >= config.maxDeaths)
            {
                EndDefeat();
                return;
            }
            if (state == Mission2State.Defense)
            {
                if (defenseStarted && !ActiveWaves().IsRunning && CountHostiles() == 0)
                {
                    defenseIndex++;
                    if (defenseIndex >= config.defenseWaves.Length)
                    {
                        EndVictory("¡LOS HICIMOS CORRER!\nEl Morro es nuestro, parce.");
                        return;
                    }
                    ActiveWaves().StartWave(config.defenseWaves[defenseIndex]);
                    Say("¡Segunda oleada! ¡Que no suban!");
                }
                if (CountHostiles() >= config.retreatThreshold)
                {
                    state = Mission2State.Retreat;
                    exitActive = true;
                    if (missionUI != null)
                    {
                        missionUI.ShowObjective("¡Nos superan! ¡Corre a los cartones!");
                    }
                    Say("¡RETIRADA! ¡A los cartones, parceros, a los cartones!");
                }
            }
            else
            {
                if (IsAtExit())
                {
                    EndVictory("¡DESLIZADA PERFECTA!\nNos fuimos en cartón al río.");
                }
            }
        }

        private void PollDeaths()
        {
            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<Popayork.Player.PlayerHealth>();
            }
            if (playerHealth == null)
            {
                return;
            }
            if (playerHealth.IsDead && !wasDead)
            {
                wasDead = true;
                deaths++;
                Say("¡Me tumbaron! (" + deaths + "/" + config.maxDeaths + ")");
            }
            else if (!playerHealth.IsDead)
            {
                wasDead = false;
            }
        }

        private bool IsAtExit()
        {
            Vector3 target = player != null ? player.transform.position : transform.position;
            if (horse != null && horse.IsMounted)
            {
                target = horse.transform.position;
            }
            Vector3 diff = target - config.exitPoint;
            diff.y = 0f;
            return diff.sqrMagnitude <= config.exitRadius * config.exitRadius;
        }

        private void SpawnDefenseAllies()
        {
            if (pool == null || config == null)
            {
                return;
            }
            Vector3 rally = config.morroTop;
            for (int i = 0; i < 2; i++)
            {
                if (pool.ActiveCount >= WaveManager.MaxActiveAgents)
                {
                    return;
                }
                pool.Spawn(Enemies.Faction.Sena, rally + new Vector3(-4f + 8f * i, 0.5f, -4f), rally);
            }
        }

        private void EndVictory(string message)
        {
            state = Mission2State.Victory;
            ActiveWaves().StopWave();
            SaveVictory();
            if (missionUI != null)
            {
                missionUI.ShowResult(true, message);
            }
        }

        private void EndDefeat()
        {
            state = Mission2State.Defeat;
            ActiveWaves().StopWave();
            if (missionUI != null)
            {
                missionUI.ShowResult(false, "NOS DIERON EN EL MORRO\nReintenta, que esto no se queda así.");
            }
        }

        private void SaveVictory()
        {
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            if (!save.completedMissions.Contains(GameConfig.Mission2Scene))
            {
                save.completedMissions.Add(GameConfig.Mission2Scene);
            }
            if (!save.unlockedMissions.Contains(GameConfig.Mission3Scene))
            {
                save.unlockedMissions.Add(GameConfig.Mission3Scene);
            }
            SaveSystem.Save(save);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReloadSave();
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
