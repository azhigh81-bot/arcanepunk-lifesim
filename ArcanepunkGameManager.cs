using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanepunk
{
    /// <summary>
    /// GameManager: Entry point that initializes and coordinates all Arcanepunk systems.
    /// Attach to a persistent GameObject in the first scene.
    /// </summary>
    public class ArcanepunkGameManager : MonoBehaviour
    {
        public static ArcanepunkGameManager Instance { get; private set; }

        [Header("System References")]
        [SerializeField] private ArcanepunkEngine _engine;
        [SerializeField] private DualLayerCombat _combat;
        [SerializeField] private CorporateFactionSystem _factions;
        [SerializeField] private DaemonManagementSystem _daemons;
        [SerializeField] private EventEngine _events;

        [Header("Game State")]
        public GameState CurrentState = GameState.MainMenu;
        public bool IsPaused = false;

        [Header("UI Panels (assign in inspector)")]
        public GameObject MainMenuPanel;
        public GameObject CharacterCreationPanel;
        public GameObject GameplayPanel;
        public GameObject CombatPanel;
        public GameObject EventPanel;
        public GameObject PauseMenuPanel;

        private Coroutine _gameLoopCoroutine;
        private float _autoSaveInterval = 60f; // seconds
        private float _lastAutoSave;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ensure all systems exist
            EnsureSystems();
        }

        private void EnsureSystems()
        {
            if (_engine == null) _engine = GetComponent<ArcanepunkEngine>() ?? gameObject.AddComponent<ArcanepunkEngine>();
            if (_combat == null) _combat = GetComponent<DualLayerCombat>() ?? gameObject.AddComponent<DualLayerCombat>();
            if (_factions == null) _factions = GetComponent<CorporateFactionSystem>() ?? gameObject.AddComponent<CorporateFactionSystem>();
            if (_daemons == null) _daemons = GetComponent<DaemonManagementSystem>() ?? gameObject.AddComponent<DaemonManagementSystem>();
            if (_events == null) _events = GetComponent<EventEngine>() ?? gameObject.AddComponent<EventEngine>();
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            SetGameState(GameState.MainMenu);
            StartCoroutine(AutoSaveLoop());
        }

        private void EnsureSystems()
        {
            if (_engine == null) _engine = gameObject.AddComponent<ArcanepunkEngine>();
            if (_combat == null) _combat = gameObject.AddComponent<DualLayerCombat>();
            if (_factions == null) _factions = gameObject.AddComponent<CorporateFactionSystem>();
            if (_daemons == null) _daemons = gameObject.AddComponent<DaemonManagementSystem>();
            if (_events == null) _events = gameObject.AddComponent<EventEngine>();
        }

        private IEnumerator AutoSaveLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(_autoSaveInterval);
                if (CurrentState == GameState.Playing && _engine?.Player != null)
                {
                    SaveGame();
                }
            }
        }

        // ==================== STATE MANAGEMENT ====================
        public void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            IsPaused = (newState == GameState.Paused);

            UpdateUIPanels();
            Time.timeScale = IsPaused ? 0f : 1f;

            Debug.Log($"[GameManager] State: {newState}");
        }

        private void UpdateUIPanels()
        {
            // Would show/hide assigned UI panels
            // MainMenuPanel?.SetActive(CurrentState == GameState.MainMenu);
            // CharacterCreationPanel?.SetActive(CurrentState == GameState.CharacterCreation);
            // GameplayPanel?.SetActive(CurrentState == GameState.Playing);
            // CombatPanel?.SetActive(CurrentState == GameState.Combat);
            // EventPanel?.SetActive(CurrentState == GameState.Event);
            // PauseMenuPanel?.SetActive(CurrentState == GameState.Paused);
        }

        // ==================== GAME FLOW ====================
        public void StartNewGame(string characterName, ArcanepunkClass classType, Corporation corp = Corporation.None)
        {
            _engine.CreateNewCharacter(characterName, classType, corp);
            SetGameState(GameState.Playing);
            _lastAutoSave = Time.time;

            // Trigger intro event
            var introEvent = ArcanepunkDataLoader.GetEvent("evt_intro_wakeup");
            if (introEvent != null)
            {
                _events.QueueEvent(introEvent, _engine.Player);
            }
        }

        public void LoadGame(string saveId)
        {
            var saveData = SaveSystem.Load(saveId);
            if (saveData != null)
            {
                _engine.LoadCharacter(saveData.PlayerState);
                // Restore other system states
                SetGameState(GameState.Playing);
            }
        }

        public void SaveGame()
        {
            if (_engine?.Player == null) return;

            var saveData = new SaveData
            {
                SaveId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                PlayerState = _engine.SaveCharacter(),
                FactionStates = _factions?.ExportState(),
                DaemonStates = _daemons?.ExportState(),
                EventHistory = _events?.ExportHistory(),
                Version = Application.version
            };

            SaveSystem.Save(saveData);
            _lastAutoSave = Time.time;
            Debug.Log($"[GameManager] Game saved: {saveData.SaveId}");
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                SetGameState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                SetGameState(GameState.Playing);
            }
        }

        public void ReturnToMainMenu()
        {
            if (CurrentState != GameState.MainMenu)
            {
                SaveGame(); // Auto-save on exit
                SetGameState(GameState.MainMenu);
            }
        }

        public void QuitGame()
        {
            SaveGame();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        // ==================== COMBAT INTEGRATION ====================
        public void StartCombat(Combatant[] attackers, Combatant[] defenders, DualLayerCombat.CombatType type = DualLayerCombat.CombatType.Mixed)
        {
            var combat = _combat.StartCombat(attackers, defenders, type);
            SetGameState(GameState.Combat);
        }

        public void EndCombat(DualLayerCombat.CombatResult result)
        {
            SetGameState(GameState.Playing);

            // Handle post-combat rewards/penalties
            ProcessCombatResult(result);
        }

        private void ProcessCombatResult(DualLayerCombat.CombatResult result)
        {
            var player = _engine.Player;
            if (player == null) return;

            switch (result)
            {
                case DualLayerCombat.CombatResult.AttackerVictory:
                    // Grant XP, credits, possible loot
                    GrantCombatRewards(true);
                    break;
                case DualLayerCombat.CombatResult.DefenderVictory:
                    // Penalties, possible injury
                    ApplyCombatPenalties();
                    break;
            }
        }

        private void GrantCombatRewards(bool isAttacker)
        {
            long xpGain = 100 * _engine.Player.Level;
            long creditGain = 5000 * _engine.Player.Level;

            // Scale by difficulty
            // ...

            _engine.GrantResource(ResourceType.Reputation, xpGain);
            _engine.GrantResource(ResourceType.Credits, creditGain);
        }

        private void ApplyCombatPenalties()
        {
            var player = _engine.Player;
            player.CurrentHP = Mathf.Max(1, player.CurrentHP / 2);
            player.Humanity = Mathf.Max(0, player.Humanity - 5);

            // Possible module damage
            foreach (var module in player.InstalledModules)
            {
                if (module != null && UnityEngine.Random.value < 0.1f)
                {
                    module.IsCorrupted = true;
                }
            }
        }

        // ==================== EVENT INTEGRATION ====================
        public void OnEventTriggered(GameEvent gameEvent)
        {
            if (CurrentState == GameState.Playing)
            {
                SetGameState(GameState.Event);
            }
        }

        public void OnEventResolved(GameEvent gameEvent, EventEngine.EventOutcome outcome)
        {
            if (CurrentState == GameState.Event)
            {
                SetGameState(GameState.Playing);
            }
        }

        // ==================== YEAR ADVANCE ====================
        public void AdvanceTime(int years = 1)
        {
            if (CurrentState != GameState.Playing) return;

            for (int i = 0; i < years; i++)
            {
                _engine.YearTick();

                // Process yearly daemon updates
                _daemons?.ProcessYearlyDaemonUpdates();

                // Check for scheduled events
                CheckScheduledEvents();
            }
        }

        private void CheckScheduledEvents()
        {
            // Birthday events, anniversaries, etc.
            var player = _engine.Player;
            if (player != null)
            {
                if (player.Level == 18 || player.Level == 21 || player.Level % 10 == 0)
                {
                    var milestoneEvent = ArcanepunkDataLoader.GetEvent($"evt_milestone_{player.Level}");
                    if (milestoneEvent != null)
                    {
                        _events.QueueEvent(milestoneEvent, player);
                    }
                }
            }
        }

        // ==================== SYSTEM ACCESSORS ====================
        public ArcanepunkEngine Engine => _engine;
        public DualLayerCombat Combat => _combat;
        public CorporateFactionSystem Factions => _factions;
        public DaemonManagementSystem Daemons => _daemons;
        public EventEngine Events => _events;

        // ==================== DEBUG ====================
        [ContextMenu("Debug: Print All Systems")]
        public void DebugPrintSystems()
        {
            Debug.Log($"Engine: {_engine != null}");
            Debug.Log($"Combat: {_combat != null}");
            Debug.Log($"Factions: {_factions != null}");
            Debug.Log($"Daemons: {_daemons != null}");
            Debug.Log($"Events: {_events != null}");
        }

        [ContextMenu("Debug: Force Year Tick")]
        public void DebugForceYearTick() => AdvanceTime(1);

        [ContextMenu("Debug: Force Combat Test")]
        public void DebugCombatTest()
        {
            var player = _engine.Player;
            if (player == null) return;

            var combatant = new DualLayerCombat.Combatant
            {
                Name = player.CharacterName,
                Team = DualLayerCombat.CombatTeam.Attacker,
                IsPlayerControlled = true,
                Stats = player.CurrentStats,
                CurrentHP = player.MaxHP,
                MaxHP = player.MaxHP,
                Humanity = player.Humanity,
                Bandwidth = (int)player.Resources.GetValueOrDefault(ResourceType.Bandwidth, 1000),
                Aether = (int)player.Resources.GetValueOrDefault(ResourceType.Aether, 1000),
                Compute = (int)player.Resources.GetValueOrDefault(ResourceType.Compute, 500),
                EquippedCircuits = player.EquippedCircuits,
                EquippedModules = player.InstalledModules,
                ActiveDaemon = player.ActiveDaemon,
                ActiveTraits = new List<string>(player.ActiveTraits)
            };

            var enemy = new DualLayerCombat.Combatant
            {
                Name = "Rogue ICE",
                Team = DualLayerCombat.CombatTeam.Defender,
                IsPlayerControlled = false,
                Stats = new StatModifiers { Might = 100, Agility = 150, Intellect = 200, Willpower = 150, Presence = 50, Integration = 200 },
                CurrentHP = 500,
                MaxHP = 500,
                Bandwidth = 2000,
                Aether = 1000,
                Compute = 1000
            };

            StartCombat(new[] { combatant }, new[] { enemy }, DualLayerCombat.CombatType.Mixed);
        }
    }

    // ==================== ENUMS ====================
    public enum GameState
    {
        MainMenu,
        CharacterCreation,
        Playing,
        Combat,
        Event,
        Paused,
        GameOver
    }

    // ==================== SAVE SYSTEM ====================
    [Serializable]
    public class SaveData
    {
        public string SaveId;
        public DateTime Timestamp;
        public PlayerState PlayerState;
        public object FactionStates;
        public object DaemonStates;
        public object EventHistory;
        public string Version;
    }

    public static class SaveSystem
    {
        private const string SAVE_DIR = "Saves";
        private const string SAVE_EXT = ".arcanesave";

        public static void Save(SaveData data)
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, SAVE_DIR);
            System.IO.Directory.CreateDirectory(path);

            string filePath = System.IO.Path.Combine(path, data.SaveId + SAVE_EXT);
            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(filePath, json);
        }

        public static SaveData Load(string saveId)
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, SAVE_DIR, saveId + SAVE_EXT);
            if (!System.IO.File.Exists(path)) return null;

            string json = System.IO.File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }

        public static SaveData[] GetAllSaves()
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, SAVE_DIR);
            if (!System.IO.Directory.Exists(path)) return new SaveData[0];

            var files = System.IO.Directory.GetFiles(path, "*" + SAVE_EXT);
            var saves = new List<SaveData>();

            foreach (var file in files)
            {
                try
                {
                    string json = System.IO.File.ReadAllText(file);
                    var save = JsonUtility.FromJson<SaveData>(json);
                    if (save != null) saves.Add(save);
                }
                catch { }
            }

            return saves.OrderByDescending(s => s.Timestamp).ToArray();
        }
    }
}