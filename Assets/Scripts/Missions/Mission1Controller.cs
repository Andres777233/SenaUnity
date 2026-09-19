using UnityEngine;
using Popayork.Core;
using Popayork.Enemies;
using Popayork.UI;

namespace Popayork.Missions
{
    public enum MissionState
    {
        Intro = 0,
        Playing = 1,
        Victory = 2,
        Defeat = 3
    }

    public class Mission1Controller : MonoBehaviour
    {
        public static Mission1Controller Instance { get; private set; }

        [SerializeField] private Mission1Config config;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private AgentPool pool;
        [SerializeField] private Transform towerPoint;
        [SerializeField] private MissionUI missionUI;

        private MissionState state;
        private float timeLeft;
        private int waveIndex;
        private bool waveSpawned;
        private int kills;
        private int totalEnemies;
        private float warnCooldown;

        public MissionState State
        {
            get { return state; }
        }

        public int WaveIndex
        {
            get { return waveIndex; }
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
            state = MissionState.Intro;
            if (missionUI != null)
            {
                missionUI.ShowIntro();
            }
        }

        private void OnEnable()
        {
            GameEvents.TargetHit += OnTargetHit;
        }

        private void OnDisable()
        {
            GameEvents.TargetHit -= OnTargetHit;
        }

        private void Update()
        {
            Simulate(Time.deltaTime);
        }

        private void OnTargetHit(bool killed)
        {
            if (state == MissionState.Playing && killed)
            {
                kills++;
            }
        }

        public void Bind(Mission1Config missionConfig, WaveManager manager, AgentPool agentPool, Transform tower, MissionUI ui)
        {
            config = missionConfig;
            waveManager = manager;
            pool = agentPool;
            towerPoint = tower;
            missionUI = ui;
        }

        public void StartMission()
        {
            if (config == null || config.waves == null || config.waves.Length == 0)
            {
                return;
            }
            state = MissionState.Playing;
            timeLeft = config.missionTime;
            waveIndex = 0;
            kills = 0;
            totalEnemies = Mathf.Max(1, config.TotalPolice());
            warnCooldown = 0f;
            SpawnAllies();
            waveManager.StartWave(config.waves[0]);
            waveSpawned = true;
            if (missionUI != null)
            {
                missionUI.ShowObjective("Defiende la Torre del Reloj");
            }
            SayCheckpoint("¡Empieza el caos! ¡A la Torre, parceros!");
        }

        // Un solo paso de misión sin asignaciones: lo usa Update y el Verify.
        public void Simulate(float dt)
        {
            if (state != MissionState.Playing || config == null)
            {
                return;
            }
            timeLeft -= dt;
            warnCooldown = Mathf.Max(0f, warnCooldown - dt);
            if (missionUI != null && totalEnemies > 0)
            {
                missionUI.SetProgress((float)kills / totalEnemies);
            }
            if (IsTowerBreached())
            {
                EndDefeat();
                return;
            }
            WarnIfClose();
            if (waveSpawned && !waveManager.IsRunning && CountPolice() == 0)
            {
                SayCheckpoint("Checkpoint: oleada " + (waveIndex + 1) + " frenada. ¡Aguante!");
                waveIndex++;
                if (waveIndex >= config.waves.Length)
                {
                    EndVictory();
                    return;
                }
                waveManager.StartWave(config.waves[waveIndex]);
            }
            if (timeLeft <= 0f)
            {
                EndVictory();
            }
        }

        public void Retry()
        {
            SceneLoader.Load(GameConfig.Mission1Scene);
        }

        public void ToMenu()
        {
            SceneLoader.Load(GameConfig.MainMenuScene);
        }

        private void SpawnAllies()
        {
            if (config.alliesWave == null || pool == null)
            {
                return;
            }
            Vector3 rally = towerPoint != null ? towerPoint.position : transform.position;
            int n = config.alliesWave.entries != null ? config.alliesWave.entries.Length : 0;
            for (int e = 0; e < n; e++)
            {
                for (int i = 0; i < config.alliesWave.entries[e].count; i++)
                {
                    if (pool.ActiveCount >= WaveManager.MaxActiveAgents)
                    {
                        return;
                    }
                    Vector3 spot = rally + new Vector3(-8f + 4f * i, 0.5f, -10f - 2f * e);
                    pool.Spawn(config.alliesWave.entries[e].faction, spot, rally);
                }
            }
        }

        private int CountPolice()
        {
            if (pool == null)
            {
                return 0;
            }
            return pool.CountActive(Faction.Police);
        }

        private bool IsTowerBreached()
        {
            if (pool == null || towerPoint == null || config == null)
            {
                return false;
            }
            float radiusSq = config.captureRadius * config.captureRadius;
            int count = pool.ActiveCount;
            for (int i = 0; i < count; i++)
            {
                AgentBrain brain = pool.GetActive(i);
                if (brain == null || brain.Faction != Faction.Police || brain.Health.IsDead)
                {
                    continue;
                }
                Vector3 diff = brain.transform.position - towerPoint.position;
                diff.y = 0f;
                if (diff.sqrMagnitude <= radiusSq)
                {
                    return true;
                }
            }
            return false;
        }

        private void WarnIfClose()
        {
            if (warnCooldown > 0f || pool == null || towerPoint == null || config == null)
            {
                return;
            }
            float radiusSq = config.warningRadius * config.warningRadius;
            int count = pool.ActiveCount;
            for (int i = 0; i < count; i++)
            {
                AgentBrain brain = pool.GetActive(i);
                if (brain == null || brain.Faction != Faction.Police || brain.Health.IsDead)
                {
                    continue;
                }
                Vector3 diff = brain.transform.position - towerPoint.position;
                diff.y = 0f;
                if (diff.sqrMagnitude <= radiusSq)
                {
                    warnCooldown = 6f;
                    SayCheckpoint("¡Vienen por la Torre, parce! ¡Atájenlos!");
                    return;
                }
            }
        }

        private void EndVictory()
        {
            state = MissionState.Victory;
            waveManager.StopWave();
            SaveProgress();
            if (missionUI != null)
            {
                missionUI.ShowResult(true, "¡AGUANTAMOS, PARCE!\nLa Torre sigue en pie. Misión 2 desbloqueada.");
            }
        }

        private void EndDefeat()
        {
            state = MissionState.Defeat;
            waveManager.StopWave();
            if (missionUI != null)
            {
                missionUI.ShowResult(false, "LA TOMBA LLEGÓ A LA TORRE\nReintenta, que esto no se queda así.");
            }
        }

        private void SaveProgress()
        {
            SaveData save = SaveSystem.LoadFreshWithoutWriting();
            if (!save.completedMissions.Contains(GameConfig.Mission1Scene))
            {
                save.completedMissions.Add(GameConfig.Mission1Scene);
            }
            if (!save.unlockedMissions.Contains(GameConfig.Mission2Scene))
            {
                save.unlockedMissions.Add(GameConfig.Mission2Scene);
            }
            SaveSystem.Save(save);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReloadSave();
            }
        }

        private void SayCheckpoint(string line)
        {
            var ui = SubtitleSystem.Instance;
            if (ui != null)
            {
                ui.ShowLine(line, 3f);
            }
        }
    }
}
