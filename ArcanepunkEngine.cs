using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arcanepunk
{
    /// <summary>
    /// Core progression engine for Arcanepunk world.
    /// Handles: YearTick, Breakthroughs, Tribulations, Resource management, Corp/Module/Daemon systems.
    /// </summary>
    public class ArcanepunkEngine : MonoBehaviour
    {
        // ==================== SINGLETON ====================
        public static ArcanepunkEngine Instance { get; private set; }

        // ==================== STATE ====================
        public PlayerState Player { get; private set; }
        public bool IsInTribulation { get; private set; }
        public TribulationInstance ActiveTribulation { get; private set; }
        public BreakthroughInstance PendingBreakthrough { get; private set; }

        // ==================== EVENTS ====================
        public event Action<PlayerState> OnYearTick;
        public event Action<RealmLevel> OnBreakthroughStarted;
        public event Action<RealmLevel, bool> OnBreakthroughCompleted; // realm, success
        public event Action<TribulationInstance> OnTribulationStarted;
        public event Action<TribulationInstance, bool> OnTribulationEnded; // instance, success
        public event Action<string, long> OnResourceChanged; // resourceId, newAmount
        public event Action<string, int> OnStatChanged; // statId, newValue
        public event Action<string> OnTraitGained;
        public event Action<string> OnTraitLost;
        public event Action<Corporation, int> OnCorpRepChanged; // corp, newRep
        public event Action<CyberneticModule> OnModuleInstalled;
        public event Action<CyberneticModule> OnModuleRemoved;
        public event Action<DaemonProgram> OnDaemonBonded;
        public event Action<DaemonProgram> OnDaemonLost;

        // ==================== CONFIG ====================
        [Header("Progression")]
        [SerializeField] private float baseAetherPerYear = 100f;
        [SerializeField] private float baseCreditsPerYear = 10000f;
        [SerializeField] private int maxActiveCircuits = 6;
        [SerializeField] private int maxInstalledModules = 6;
        [SerializeField] private int maxActiveDaemons = 3;

        [Header("Tribulation")]
        [SerializeField] private float baseTribulationSurvivalChance = 0.5f;
        [SerializeField] private int tribulationCooldownDays = 30;

        // ==================== INTERNAL ====================
        private Dictionary<ResourceType, long> _resourceRates = new();
        private Dictionary<string, int> _activeEffectModifiers = new();
        private List<TribulationInstance> _tribulationHistory = new();
        private List<BreakthroughInstance> _breakthroughHistory = new();
        private float _timeSinceLastTribulation = 0f;
        private bool _initialized = false;

        // ==================== UNITY LIFECYCLE ====================
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            InitializeEngine();
        }

        private void Update()
        {
            if (!_initialized || Player == null) return;

            // Real-time resource regen (per second)
            RegenResources(Time.deltaTime);

            // Tribulation timer
            if (IsInTribulation && ActiveTribulation != null)
            {
                ActiveTribulation.RemainingTime -= Time.deltaTime;
                if (ActiveTribulation.RemainingTime <= 0f)
                {
                    ResolveTribulation(false, "Time expired");
                }
            }
        }

        // ==================== INITIALIZATION ====================
        public void InitializeEngine()
        {
            if (_initialized) return;

            // Ensure data loaded
            ArcanepunkDataLoader.Initialize();

            // Initialize resource rates
            _resourceRates[ResourceType.Aether] = Mathf.RoundToInt(baseAetherPerYear / 365f / 24f / 60f / 60f); // per second
            _resourceRates[ResourceType.Credits] = Mathf.RoundToInt(baseCreditsPerYear / 365f / 24f / 60f / 60f);
            _resourceRates[ResourceType.Bandwidth] = 0;
            _resourceRates[ResourceType.Compute] = 0;
            _resourceRates[ResourceType.Reputation] = 0;
            _resourceRates[ResourceType.AdminTokens] = 0;
            _resourceRates[ResourceType.Entropy] = 0;

            _initialized = true;
            Debug.Log("[ArcanepunkEngine] Initialized");
        }

        // ==================== PLAYER STATE MANAGEMENT ====================
        public void CreateNewCharacter(string name, ArcanepunkClass classType, Corporation corp = Corporation.None)
        {
            var classData = ArcanepunkDataLoader.GetClass(classType);
            var realm = ArcanepunkDataLoader.GetRealm(RealmStage.Circuit, 1);

            Player = new PlayerState
            {
                CharacterId = Guid.NewGuid().ToString(),
                CharacterName = name,
                Class = classType,
                Subclass = "",
                Corp = corp,
                Level = 1,
                CurrentStage = RealmStage.Circuit,
                CurrentRealmIndex = 1,
                Experience = 0,
                BaseStats = classData.BaseStats,
                CurrentStats = classData.BaseStats,
                ActiveTraits = new List<string>(classData.StartingTraits).ToArray(),
                KnownCircuits = new List<string>(classData.StartingSpellCircuits).ToArray(),
                EquippedCircuits = new List<string>(classData.StartingSpellCircuits).ToArray(),
                Resources = new Dictionary<ResourceType, long>
                {
                    { ResourceType.Credits, 10000 },
                    { ResourceType.Aether, 500 },
                    { ResourceType.Bandwidth, 100 },
                    { ResourceType.Compute, 50 },
                    { ResourceType.Reputation, 0 },
                    { ResourceType.AdminTokens, 0 },
                    { ResourceType.Entropy, 0 }
                },
                CorpReputation = new int[5],
                InstalledModules = new CyberneticModule[6],
                ActiveDaemon = null,
                Flags = new Dictionary<string, int>(),
                Karma = 0,
                Humanity = 100,
                PlayTimeSeconds = 0,
                LastSave = DateTime.Now
            };

            // Apply corp starting rep
            if (corp != Corporation.None)
            {
                Player.CorpReputation[(int)corp] = 30;
            }

            // Grant starting daemon
            if (classData.StartingSpellCircuits.Length > 0)
            {
                var daemonId = classType == ArcanepunkClass.Neurunner ? "daemon_spawn_basic" : "shield_daemon_basic";
                BondDaemon(daemonId);
            }

            Debug.Log($"[ArcanepunkEngine] New character: {name} ({classType}) for {corp}");
        }

        public void LoadCharacter(PlayerState state)
        {
            Player = state;
            RecalculateStats();
            Debug.Log($"[ArcanepunkEngine] Loaded character: {state.CharacterName}");
        }

        public PlayerState SaveCharacter()
        {
            Player.LastSave = DateTime.Now;
            Player.PlayTimeSeconds += (long)(Time.time - _sessionStartTime);
            return Player;
        }

        private float _sessionStartTime;

        private void OnApplicationPause(bool pause)
        {
            if (!pause) _sessionStartTime = Time.time;
        }

        // ==================== YEAR TICK ====================
        public void AdvanceYear(int years = 1)
        {
            for (int i = 0; i < years; i++)
            {
                YearTick();
            }
        }

        public void YearTick()
        {
            if (Player == null) return;

            Player.Level++;
            Player.PlayTimeSeconds += 365 * 24 * 60 * 60;

            // Stat growth from class
            var classData = ArcanepunkDataLoader.GetClass(Player.Class);
            Player.BaseStats += classData.GrowthPerLevel;

            // Realm stat gains (if just broke through)
            var currentRealm = ArcanepunkDataLoader.GetRealm(Player.CurrentStage, Player.CurrentRealmIndex);
            if (currentRealm != null && Player.Flags.GetValueOrDefault("realm_just_broken") == 1)
            {
                Player.BaseStats += currentRealm.StatGains;
                Player.Flags["realm_just_broken"] = 0;
            }

            RecalculateStats();

            // Resource income
            GrantResource(ResourceType.Aether, CalculateAetherIncome());
            GrantResource(ResourceType.Credits, CalculateCreditsIncome());
            GrantResource(ResourceType.Bandwidth, CalculateBandwidthIncome());
            GrantResource(ResourceType.Compute, CalculateComputeIncome());

            // Corp reputation decay
            DecayCorpReputation();

            // Daemon loyalty drift
            DriftDaemonLoyalty();

            // Humanity/Integration balance
            CheckCyberpsychosisRisk();

            // Age effects (Stage 1 only)
            if (Player.CurrentStage == RealmStage.Circuit)
            {
                ApplyAgingEffects();
            }

            // Check breakthrough
            CheckBreakthroughReadiness();

            OnYearTick?.Invoke(Player);

            Debug.Log($"[ArcanepunkEngine] Year {Player.Level} - Stage: {Player.CurrentStage} Realm: {Player.CurrentRealmIndex}");
        }

        private void RecalculateStats()
        {
            Player.CurrentStats = Player.BaseStats;

            // Module bonuses
            foreach (var module in Player.InstalledModules)
            {
                if (module != null)
                {
                    Player.CurrentStats += module.StatBonuses;
                }
            }

            // Trait modifiers (simplified)
            foreach (var trait in Player.ActiveTraits)
            {
                ApplyTraitModifiers(trait);
            }

            // Active daemon bonuses
            if (Player.ActiveDaemon != null)
            {
                // Daemon provides passive bonuses based on type
            }
        }

        private void ApplyTraitModifiers(string traitId)
        {
            // Specific trait modifiers would be defined in data
            // For now, placeholder
        }

        private long CalculateAetherIncome()
        {
            long income = 100;
            income += Player.CurrentStats.Intellect / 10;
            income += Player.CurrentStats.Willpower / 10;
            income += Player.CurrentStats.Integration / 5;

            // Corp bonuses
            if (Player.Corp == Corporation.AetherCorp) income *= 2;
            if (Player.Corp == Corporation.SynapseCollective) income = (long)(income * 1.5);

            // Module bonuses
            foreach (var module in Player.InstalledModules)
            {
                if (module?.GrantedTraits.Contains("aether_reservoir") == true) income += 50;
                if (module?.GrantedTraits.Contains("internal_server") == true) income += 100;
                if (module?.GrantedTraits.Contains("cloud_immortality") == true) income += 500;
            }

            // Daemon bonuses
            if (Player.ActiveDaemon?.Abilities.Contains("bandwidth_manage") == true) income += 20;

            return income;
        }

        private long CalculateCreditsIncome()
        {
            long income = 10000;
            income += Player.Level * 100;
            income += Player.CurrentStats.Presence * 50;

            // Corp salary
            int corpRep = Player.Corp != Corporation.None ? Player.CorpReputation[(int)Player.Corp] : 0;
            income += corpRep * 100;

            return income;
        }

        private long CalculateBandwidthIncome()
        {
            long income = 0;
            income += Player.CurrentStats.Intellect / 5;
            income += Player.CurrentStats.Integration / 5;

            foreach (var module in Player.InstalledModules)
            {
                if (module?.GrantedTraits.Contains("mesh_consciousness") == true) income += 100;
                if (module?.GrantedTraits.Contains("satellite_uplink") == true) income += 50;
            }

            return income;
        }

        private long CalculateComputeIncome()
        {
            long income = 0;
            income += Player.CurrentStats.Intellect / 10;

            foreach (var module in Player.InstalledModules)
            {
                if (module?.GrantedTraits.Contains("infinite_compute") == true) income += 1000;
                if (module?.GrantedTraits.Contains("distributed_processing") == true) income += 200;
            }

            return income;
        }

        private void DecayCorpReputation()
        {
            for (int i = 0; i < 5; i++)
            {
                if (Player.CorpReputation[i] > 0)
                {
                    Player.CorpReputation[i] = Mathf.Max(0, Player.CorpReputation[i] - 2);
                    OnCorpRepChanged?.Invoke((Corporation)i, Player.CorpReputation[i]);
                }
                else if (Player.CorpReputation[i] < 0)
                {
                    Player.CorpReputation[i] = Mathf.Min(0, Player.CorpReputation[i] + 1);
                    OnCorpRepChanged?.Invoke((Corporation)i, Player.CorpReputation[i]);
                }
            }
        }

        private void DriftDaemonLoyalty()
        {
            if (Player.ActiveDaemon == null) return;

            // Loyalty drifts toward base personality value
            int baseLoyalty = Player.ActiveDaemon.Loyalty;
            int currentLoyalty = Player.Flags.GetValueOrDefault($"daemon_loyalty_{Player.ActiveDaemon.Id}", baseLoyalty);

            if (currentLoyalty > baseLoyalty) currentLoyalty--;
            else if (currentLoyalty < baseLoyalty) currentLoyalty++;

            Player.Flags[$"daemon_loyalty_{Player.ActiveDaemon.Id}"] = currentLoyalty;

            // Betrayal check
            if (currentLoyalty <= 0)
            {
                LoseDaemon("Betrayal - loyalty reached 0");
            }
        }

        private void CheckCyberpsychosisRisk()
        {
            if (Player.Humanity <= 30 && Player.CurrentStats.Integration > 200)
            {
                // Trigger cyberpsychosis event
                var evt = ArcanepunkDataLoader.GetEvent("evt_cyberpsychosis_check");
                if (evt != null)
                {
                    EventEngine.Instance.QueueEvent(evt, Player);
                }
            }
        }

        private void ApplyAgingEffects()
        {
            // Stage 1: Normal aging
            if (Player.Level > 50)
            {
                Player.BaseStats.Might = Mathf.Max(1, Player.BaseStats.Might - 1);
                Player.BaseStats.Agility = Mathf.Max(1, Player.BaseStats.Agility - 1);
                Player.BaseStats.Vitality = Mathf.Max(1, Player.BaseStats.Vitality - 1);
            }
        }

        // ==================== RESOURCE MANAGEMENT ====================
        public void GrantResource(ResourceType type, long amount)
        {
            if (amount <= 0) return;

            if (!Player.Resources.ContainsKey(type)) Player.Resources[type] = 0;
            Player.Resources[type] += amount;

            OnResourceChanged?.Invoke(type.ToString(), Player.Resources[type]);
        }

        public bool SpendResource(ResourceType type, long amount)
        {
            if (!Player.Resources.ContainsKey(type) || Player.Resources[type] < amount) return false;

            Player.Resources[type] -= amount;
            OnResourceChanged?.Invoke(type.ToString(), Player.Resources[type]);
            return true;
        }

        public long GetResource(ResourceType type)
        {
            return Player.Resources.GetValueOrDefault(type, 0);
        }

        private void RegenResources(float deltaTime)
        {
            // Passive regen handled in YearTick for simplicity
            // Could add real-time regen here for Aether/Bandwidth
        }

        // ==================== BREAKTHROUGH SYSTEM ====================
        private void CheckBreakthroughReadiness()
        {
            if (IsInTribulation || PendingBreakthrough != null) return;

            var nextRealm = ArcanepunkDataLoader.GetNextRealm(Player);
            if (nextRealm == null) return; // Max level

            if (ArcanepunkDataLoader.CanBreakthrough(Player, nextRealm))
            {
                float chance = ArcanepunkDataLoader.GetBreakthroughChance(Player, nextRealm);
                QueueBreakthrough(nextRealm, chance);
            }
        }

        public void QueueBreakthrough(RealmLevel realm, float chance)
        {
            PendingBreakthrough = new BreakthroughInstance
            {
                Realm = realm,
                BaseChance = chance,
                ModifiedChance = chance,
                QueuedAt = DateTime.Now,
                PreparationModifiers = new Dictionary<string, float>()
            };

            // Fire event for player preparation
            var evt = ArcanepunkDataLoader.GetEvent($"evt_breakthrough_{realm.Stage.ToString().ToLower()}_{realm.Index}");
            if (evt != null)
            {
                EventEngine.Instance.QueueEvent(evt, Player, onComplete: (success) => {
                    if (success) StartTribulation(realm);
                });
            }
            else
            {
                // Generic breakthrough event
                StartTribulation(realm);
            }

            OnBreakthroughStarted?.Invoke(realm);
        }

        private void StartTribulation(RealmLevel realm)
        {
            PendingBreakthrough = null;
            IsInTribulation = true;

            var trib = realm.Tribulation;
            ActiveTribulation = new TribulationInstance
            {
                Realm = realm,
                TribulationData = trib,
                StartTime = DateTime.Now,
                RemainingTime = CalculateTribulationDuration(trib),
                ThreatsDefeated = 0,
                TotalThreats = trib.Threats.Length,
                PlayerStateSnapshot = ClonePlayerState(Player)
            };

            _tribulationHistory.Add(ActiveTribulation);
            _timeSinceLastTribulation = 0f;

            OnTribulationStarted?.Invoke(ActiveTribulation);

            // Fire tribulation event
            var evt = CreateTribulationEvent(trib);
            EventEngine.Instance.QueueEvent(evt, Player, onComplete: (success) => {
                ResolveTribulation(success, success ? "Completed" : "Failed");
            });

            Debug.Log($"[ArcanepunkEngine] Tribulation started: {trib.Name} (Difficulty: {trib.Difficulty}/10)");
        }

        private float CalculateTribulationDuration(TribulationData trib)
        {
            // Base 5 minutes per difficulty, max 30 minutes
            return Mathf.Clamp(trib.Difficulty * 300f, 60f, 1800f);
        }

        private EventData CreateTribulationEvent(TribulationData trib)
        {
            // Generate procedural tribulation event based on threats
            var evt = new EventData
            {
                Id = $"trib_{trib.Id}",
                Title = trib.Name,
                Description = trib.Description,
                MinStage = (int)trib.Type, // Hack: reuse for difficulty
                MaxStage = 5,
                Tags = new[] { "Tribulation", trib.Type.ToString() },
                IsRepeatable = false,
                CooldownDays = 0,
                Choices = GenerateTribulationChoices(trib)
            };
            return evt;
        }

        private ChoiceData[] GenerateTribulationChoices(TribulationData trib)
        {
            var choices = new List<ChoiceData>();

            // Combat approach
            choices.Add(new ChoiceData
            {
                Text = "Face it head-on. Combat protocol engaged.",
                RequiredStats = new int[] { 100, 100, 50, 100, 0, 100 },
                Effects = new EffectData[]
                {
                    new EffectData { Type = "TribulationApproach", TargetId = "Combat", Amount = 1 },
                    new EffectData { Type = "Stat", TargetId = "Might", Amount = 20, IsPermanent = false }
                }
            });

            // Hacking approach
            choices.Add(new ChoiceData
            {
                Text = "Hack the threat. Subvert from within.",
                RequiredStats = new int[] { 0, 50, 150, 100, 0, 150 },
                Effects = new EffectData[]
                {
                    new EffectData { Type = "TribulationApproach", TargetId = "Hacking", Amount = 1 },
                    new EffectData { Type = "Stat", TargetId = "Intellect", Amount = 20, IsPermanent = false }
                }
            });

            // Evasion/Stealth approach
            choices.Add(new ChoiceData
            {
                Text = "Evade. Don't fight what you can avoid.",
                RequiredStats = new int[] { 0, 150, 50, 50, 0, 100 },
                Effects = new EffectData[]
                {
                    new EffectData { Type = "TribulationApproach", TargetId = "Evasion", Amount = 1 },
                    new EffectData { Type = "Stat", TargetId = "Agility", Amount = 20, IsPermanent = false }
                }
            });

            // Social/Diplomatic (if applicable)
            if (trib.Type == TribulationType.SystemAudit || trib.Type == TribulationType.DDoSReality)
            {
                choices.Add(new ChoiceData
                {
                    Text = "Negotiate. Every system has an admin.",
                    RequiredStats = new int[] { 0, 0, 100, 100, 200, 50 },
                    Effects = new EffectData[]
                    {
                        new EffectData { Type = "TribulationApproach", TargetId = "Diplomacy", Amount = 1 },
                        new EffectData { Type = "Stat", TargetId = "Presence", Amount = 20, IsPermanent = false }
                    }
                });
            }

            return choices.ToArray();
        }

        public void ResolveTribulation(bool success, string reason)
        {
            if (!IsInTribulation || ActiveTribulation == null) return;

            var realm = ActiveTribulation.Realm;
            var trib = ActiveTribulation.TribulationData;

            IsInTribulation = false;
            _timeSinceLastTribulation = 0f;

            if (success)
            {
                // Grant rewards
                foreach (var reward in trib.SuccessRewards)
                {
                    ApplyReward(reward);
                }

                // Advance realm
                AdvanceRealm(realm);

                // Mark realm as completed
                Player.Flags[$"realm_completed_{realm.Stage}_{realm.Index}"] = 1;
                Player.Flags["realm_just_broken"] = 1;

                _breakthroughHistory.Add(new BreakthroughInstance
                {
                    Realm = realm,
                    Success = true,
                    CompletedAt = DateTime.Now,
                    Approach = GetTribulationApproach()
                });

                Debug.Log($"[ArcanepunkEngine] Tribulation PASSED: {trib.Name}");
            }
            else
            {
                // Apply consequences
                foreach (var consequence in trib.FailureConsequences)
                {
                    ApplyConsequence(consequence);
                }

                _breakthroughHistory.Add(new BreakthroughInstance
                {
                    Realm = realm,
                    Success = false,
                    CompletedAt = DateTime.Now,
                    FailureReason = reason
                });

                Debug.Log($"[ArcanepunkEngine] Tribulation FAILED: {trib.Name} - {reason}");
            }

            ActiveTribulation = null;
            OnBreakthroughCompleted?.Invoke(realm, success);
            OnTribulationEnded?.Invoke(ActiveTribulation, success);
        }

        private string GetTribulationApproach()
        {
            // Would be set by event choice
            return Player.Flags.GetValueOrDefault("tribulation_approach", "Combat").ToString();
        }

        private void AdvanceRealm(RealmLevel realm)
        {
            if (realm.Index < 7)
            {
                Player.CurrentRealmIndex = realm.Index + 1;
            }
            else
            {
                // Next stage
                Player.CurrentStage = (RealmStage)((int)realm.Stage + 1);
                Player.CurrentRealmIndex = 1;

                // Stage transition effects
                OnStageTransition(realm.Stage, Player.CurrentStage);
            }

            // Grant realm traits
            foreach (var trait in realm.GrantedTraits)
            {
                GainTrait(trait);
            }

            // Unlock mechanics (handled by systems listening to events)
            // Unlock resources
            foreach (var resource in realm.UnlockedResources)
            {
                if (!Player.Resources.ContainsKey(resource))
                {
                    Player.Resources[resource] = 0;
                }
            }
        }

        private void OnStageTransition(RealmStage from, RealmStage to)
        {
            Debug.Log($"[ArcanepunkEngine] STAGE TRANSITION: {from} -> {to}");

            // Stage-specific unlocks
            switch (to)
            {
                case RealmStage.Core:
                    // Unlock daemon hosting, internal server
                    break;
                case RealmStage.Satellite:
                    // Unlock orbital mechanics
                    break;
                case RealmStage.Network:
                    // Unlock cloud mechanics
                    break;
                case RealmStage.SourceCode:
                    // Unlock kernel access
                    break;
            }
        }

        private void ApplyReward(RewardData reward)
        {
            switch (reward.Type)
            {
                case "Trait":
                    GainTrait(reward.Id);
                    break;
                case "Resource":
                    if (Enum.TryParse(reward.Id, out ResourceType rt))
                        GrantResource(rt, reward.Amount);
                    break;
                case "Item":
                    // Add to inventory
                    break;
                case "Unlock":
                    Player.Flags[reward.Id] = 1;
                    break;
                case "Reputation":
                    if (Enum.TryParse(reward.Id, out Corporation corp))
                        ModifyCorpRep(corp, reward.Amount);
                    break;
            }
        }

        private void ApplyConsequence(ConsequenceData consequence)
        {
            switch (consequence.Type)
            {
                case "StatDrain":
                    // Parse stat from description
                    break;
                case "TraitLoss":
                    LoseTrait(consequence.Description);
                    break;
                case "LevelDown":
                    Player.Level = Mathf.Max(1, Player.Level - consequence.Severity);
                    break;
                case "Corruption":
                    GainTrait(consequence.Description.Replace("Gain ", "").Replace(" - ", "_"));
                    break;
                case "Death":
                    HandleDeath();
                    break;
                case "Debt":
                    SpendResource(ResourceType.Credits, consequence.Severity * 10000);
                    break;
            }
        }

        private void HandleDeath()
        {
            // New Game+ logic
            Debug.Log("[ArcanepunkEngine] CHARACTER DEATH - New Game+ available");
            // Would trigger NG+ flow
        }

        // ==================== TRAIT MANAGEMENT ====================
        public void GainTrait(string traitId)
        {
            if (Array.Exists(Player.ActiveTraits, t => t == traitId)) return;

            var newTraits = new List<string>(Player.ActiveTraits) { traitId };
            Player.ActiveTraits = newTraits.ToArray();
            OnTraitGained?.Invoke(traitId);

            // Apply immediate effects
            ApplyTraitEffects(traitId, true);
        }

        public void LoseTrait(string traitId)
        {
            var traits = new List<string>(Player.ActiveTraits);
            if (traits.Remove(traitId))
            {
                Player.ActiveTraits = traits.ToArray();
                OnTraitLost?.Invoke(traitId);
                ApplyTraitEffects(traitId, false);
            }
        }

        private void ApplyTraitEffects(string traitId, bool gaining)
        {
            // Trait-specific effects
            switch (traitId)
            {
                case "neural_link":
                    // Already handled by stats
                    break;
                case "daemon_bond":
                    if (gaining) UnlockDaemonSlot();
                    break;
                case "chrome_perfection":
                    Player.Humanity = Mathf.Max(0, Player.Humanity - 20);
                    break;
            }
        }

        // ==================== CORPORATION MANAGEMENT ====================
        public void ModifyCorpRep(Corporation corp, int amount)
        {
            int idx = (int)corp;
            if (idx < 0 || idx >= 5) return;

            Player.CorpReputation[idx] = Mathf.Clamp(Player.CorpReputation[idx] + amount, -100, 100);
            OnCorpRepChanged?.Invoke(corp, Player.CorpReputation[idx]);

            // Check for status changes
            CheckCorpStatusChanges(corp);
        }

        public int GetCorpRep(Corporation corp)
        {
            int idx = (int)corp;
            return idx >= 0 && idx < 5 ? Player.CorpReputation[idx] : 0;
        }

        public void JoinCorporation(Corporation corp)
        {
            if (Player.Corp == corp) return;

            // Leave old corp
            if (Player.Corp != Corporation.None)
            {
                LeaveCorporation();
            }

            Player.Corp = corp;
            Player.CorpReputation[(int)corp] = Mathf.Max(Player.CorpReputation[(int)corp], 30);

            // Grant corp starter traits
            var corpData = ArcanepunkDataLoader.GetCorp(corp);
            foreach (var trait in corpData.ExclusiveTraits)
            {
                GainTrait(trait);
            }

            OnCorpRepChanged?.Invoke(corp, Player.CorpReputation[(int)corp]);
        }

        public void LeaveCorporation()
        {
            if (Player.Corp == Corporation.None) return;

            // Lose exclusive traits
            var corpData = ArcanepunkDataLoader.GetCorp(Player.Corp);
            foreach (var trait in corpData.ExclusiveTraits)
            {
                LoseTrait(trait);
            }

            Player.Corp = Corporation.None;
        }

        private void CheckCorpStatusChanges(Corporation corp)
        {
            int rep = GetCorpRep(corp);
            var corpData = ArcanepunkDataLoader.GetCorp(corp);

            // Could trigger events at thresholds: -50, 0, 50, 80, 100
        }

        // ==================== CYBERNETIC MODULES ====================
        public bool InstallModule(string moduleId, int slotIndex = -1)
        {
            var module = ArcanepunkDataLoader.GetModule(moduleId);
            if (module == null) return false;

            // Check requirements
            if (!CanInstallModule(module)) return false;

            // Find slot
            int targetSlot = slotIndex;
            if (targetSlot == -1)
            {
                // Auto-find empty slot of correct type
                targetSlot = FindEmptyModuleSlot(module.Slot);
                if (targetSlot == -1) return false; // No space
            }

            // Remove existing module in slot
            if (Player.InstalledModules[targetSlot] != null)
            {
                RemoveModule(targetSlot);
            }

            // Install
            Player.InstalledModules[targetSlot] = module;
            Player.CurrentStats += module.StatBonuses;

            // Deduct costs
            SpendResource(ResourceType.Credits, module.CreditsCost);
            SpendResource(ResourceType.Aether, module.AetherCost);

            // Grant traits/circuits
            foreach (var trait in module.GrantedTraits) GainTrait(trait);
            foreach (var circuitId in module.GrantedCircuits) LearnCircuit(circuitId);

            OnModuleInstalled?.Invoke(module);
            RecalculateStats();

            Debug.Log($"[ArcanepunkEngine] Installed {module.Name} in slot {targetSlot}");
            return true;
        }

        public void RemoveModule(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= Player.InstalledModules.Length) return;

            var module = Player.InstalledModules[slotIndex];
            if (module == null) return;

            Player.CurrentStats -= module.StatBonuses;

            // Remove traits/circuits (only if not provided by other sources)
            foreach (var trait in module.GrantedTraits)
            {
                if (!IsTraitProvidedElsewhere(trait, module))
                    LoseTrait(trait);
            }

            Player.InstalledModules[slotIndex] = null;
            OnModuleRemoved?.Invoke(module);
            RecalculateStats();
        }

        private bool CanInstallModule(CyberneticModule module)
        {
            // Check integration capacity
            int totalIntegration = Player.InstalledModules.Where(m => m != null).Sum(m => m.IntegrationCost);
            if (totalIntegration + module.IntegrationCost > Player.CurrentStats.Integration) return false;

            // Check corp restriction
            if (module.CorpRestricted.Length > 0 && Player.Corp != Corporation.None)
            {
                if (!module.CorpRestricted.Contains(Player.Corp)) return false;
            }

            // Check slot availability
            if (FindEmptyModuleSlot(module.Slot) == -1 && !HasReplaceableModule(module.Slot)) return false;

            // Check tier prerequisites (simplified)
            return true;
        }

        private int FindEmptyModuleSlot(string slotType)
        {
            for (int i = 0; i < Player.InstalledModules.Length; i++)
            {
                if (Player.InstalledModules[i] == null) return i;
            }
            return -1;
        }

        private bool HasReplaceableModule(string slotType)
        {
            // Could implement replacement logic
            return false;
        }

        private bool IsTraitProvidedElsewhere(string traitId, CyberneticModule excludeModule)
        {
            foreach (var module in Player.InstalledModules)
            {
                if (module != null && module != excludeModule && module.GrantedTraits.Contains(traitId))
                    return true;
            }
            return false;
        }

        // ==================== SPELL CIRCUITS ====================
        public void LearnCircuit(string circuitId)
        {
            if (Array.Exists(Player.KnownCircuits, c => c == circuitId)) return;

            var circuit = ArcanepunkDataLoader.GetCircuit(circuitId);
            if (circuit == null) return;

            // Check corp restriction
            if (circuit.CorpRestricted.Length > 0 && Player.Corp != Corporation.None)
            {
                if (!circuit.CorpRestricted.Contains(Player.Corp)) return;
            }

            var newCircuits = new List<string>(Player.KnownCircuits) { circuitId };
            Player.KnownCircuits = newCircuits.ToArray();

            Debug.Log($"[ArcanepunkEngine] Learned circuit: {circuit.Name}");
        }

        public void EquipCircuit(string circuitId)
        {
            if (!Array.Exists(Player.KnownCircuits, c => c == circuitId)) return;
            if (Array.Exists(Player.EquippedCircuits, c => c == circuitId)) return;

            if (Player.EquippedCircuits.Length >= maxActiveCircuits) return;

            var newEquipped = new List<string>(Player.EquippedCircuits) { circuitId };
            Player.EquippedCircuits = newEquipped.ToArray();
        }

        public void UnequipCircuit(string circuitId)
        {
            var equipped = new List<string>(Player.EquippedCircuits);
            equipped.Remove(circuitId);
            Player.EquippedCircuits = equipped.ToArray();
        }

        // ==================== DAEMON MANAGEMENT ====================
        public void BondDaemon(string daemonId)
        {
            var daemon = ArcanepunkDataLoader.GetDaemon(daemonId);
            if (daemon == null) return;

            // Check if already bonded
            if (Player.ActiveDaemon?.Id == daemonId) return;

            // Release current daemon
            if (Player.ActiveDaemon != null)
            {
                ReleaseDaemon();
            }

            Player.ActiveDaemon = daemon;
            Player.Flags[$"daemon_loyalty_{daemonId}"] = daemon.Loyalty;
            Player.Flags[$"daemon_bonded_{daemonId}"] = 1;

            OnDaemonBonded?.Invoke(daemon);
            Debug.Log($"[ArcanepunkEngine] Bonded daemon: {daemon.Name} ({daemon.Personality})");
        }

        public void ReleaseDaemon()
        {
            if (Player.ActiveDaemon == null) return;

            var daemon = Player.ActiveDaemon;
            Player.ActiveDaemon = null;
            Player.Flags.Remove($"daemon_loyalty_{daemon.Id}");

            OnDaemonLost?.Invoke(daemon);
            Debug.Log($"[ArcanepunkEngine] Released daemon: {daemon.Name}");
        }

        private void LoseDaemon(string reason)
        {
            if (Player.ActiveDaemon == null) return;

            var daemon = Player.ActiveDaemon;
            ReleaseDaemon();

            // Trauma effect
            GainTrait($"daemon_loss_trauma_{daemon.Id}");
            Player.Humanity = Mathf.Max(0, Player.Humanity - 10);

            Debug.Log($"[ArcanepunkEngine] Daemon lost: {daemon.Name} - {reason}");
        }

        private void UnlockDaemonSlot()
        {
            maxActiveDaemons = Mathf.Min(5, maxActiveDaemons + 1);
        }

        // ==================== UTILITY ====================
        private PlayerState ClonePlayerState(PlayerState source)
        {
            // Shallow clone for tribulation snapshot
            return new PlayerState
            {
                CharacterId = source.CharacterId,
                CurrentStats = source.CurrentStats,
                Resources = new Dictionary<ResourceType, long>(source.Resources),
                ActiveTraits = (string[])source.ActiveTraits.Clone(),
                EquippedCircuits = (string[])source.EquippedCircuits.Clone(),
                Humanity = source.Humanity
            };
        }

        // ==================== DEBUG / CHEAT ====================
        [ContextMenu("Force Year Tick")]
        public void DebugYearTick() => YearTick();

        [ContextMenu("Force Breakthrough Check")]
        public void DebugBreakthroughCheck() => CheckBreakthroughReadiness();

        [ContextMenu("Print Player State")]
        public void DebugPrintState()
        {
            Debug.Log($"=== {Player.CharacterName} ===");
            Debug.Log($"Level: {Player.Level} | Stage: {Player.CurrentStage} Realm: {Player.CurrentRealmIndex}");
            Debug.Log($"Stats: M:{Player.CurrentStats.Might} A:{Player.CurrentStats.Agility} I:{Player.CurrentStats.Intellect} W:{Player.CurrentStats.Willpower} P:{Player.CurrentStats.Presence} In:{Player.CurrentStats.Integration}");
            Debug.Log($"Resources: Aether:{GetResource(ResourceType.Aether)} Credits:{GetResource(ResourceType.Credits)}");
            Debug.Log($"Corp: {Player.Corp} | Traits: {Player.ActiveTraits.Length} | Circuits: {Player.KnownCircuits.Length}/{Player.EquippedCircuits.Length}");
        }
    }

    // ==================== SUPPORT CLASSES ====================
    [Serializable]
    public class TribulationInstance
    {
        public RealmLevel Realm;
        public TribulationData TribulationData;
        public DateTime StartTime;
        public float RemainingTime;
        public int ThreatsDefeated;
        public int TotalThreats;
        public PlayerState PlayerStateSnapshot;
        public string Approach;
    }

    [Serializable]
    public class BreakthroughInstance
    {
        public RealmLevel Realm;
        public float BaseChance;
        public float ModifiedChance;
        public DateTime QueuedAt;
        public Dictionary<string, float> PreparationModifiers;
        public bool Success;
        public DateTime CompletedAt;
        public string Approach;
        public string FailureReason;
    }
}