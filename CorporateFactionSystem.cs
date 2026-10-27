using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanepunk
{
    /// <summary>
    /// Corporate faction system: reputation, contracts, espionage, territory, warfare.
    /// </summary>
    public class CorporateFactionSystem : MonoBehaviour
    {
        public static CorporateFactionSystem Instance { get; private set; }

        [Header("Faction Config")]
        [SerializeField] private float repDecayPerYear = 2f;
        [SerializeField] private int maxActiveContracts = 3;
        [SerializeField] private float territoryIncomeMultiplier = 1.5f;

        public event Action<Corporation, int> OnRepChanged;
        public event Action<Contract> OnContractOffered;
        public event Action<Contract> OnContractCompleted;
        public event Action<Corporation, Corporation, WarDeclaration> OnWarDeclared;
        public event Action<Corporation, Territory> OnTerritoryCaptured;
        public event Action<EspionageOperation> OnEspionageCompleted;

        // State
        private Dictionary<Corporation, FactionState> _factionStates = new();
        private List<Contract> _activeContracts = new();
        private List<Contract> _availableContracts = new();
        private List<WarDeclaration> _activeWars = new();
        private List<EspionageOperation> _activeOperations = new();
        private Dictionary<TerritoryKey, Territory> _territories = new();

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        private void Start()
        {
            InitializeFactions();
            GenerateTerritories();
            GenerateInitialContracts();
        }

        private void InitializeFactions()
        {
            foreach (Corporation corp in Enum.GetValues(typeof(Corporation)))
            {
                if (corp == Corporation.None) continue;

                var corpData = ArcanepunkDataLoader.GetCorp(corp);
                _factionStates[corp] = new FactionState
                {
                    Corporation = corpData,
                    PlayerReputation = 0,
                    GlobalInfluence = 100,
                    Resources = new Dictionary<ResourceType, long>
                    {
                        { ResourceType.Credits, 10000000 },
                        { ResourceType.Aether, 50000 },
                        { ResourceType.Compute, 10000 },
                        { ResourceType.AdminTokens, 100 }
                    },
                    Territories = new List<Territory>(),
                    ActiveContracts = new List<Contract>(),
                    EspionageTargets = new List<Corporation>(),
                    DiplomaticStance = new Dictionary<Corporation, DiplomaticStance>()
                };

                // Initialize diplomatic stances from data
                foreach (var rel in corpData.Relations)
                {
                    _factionStates[corp].DiplomaticStance[rel.TargetCorp] = StandingToStance(rel.Standing);
                }
            }
        }

        private DiplomaticStance StandingToStance(int standing)
        {
            if (standing >= 80) return DiplomaticStance.Allied;
            if (standing >= 30) return DiplomaticStance.Friendly;
            if (standing >= -30) return DiplomaticStance.Neutral;
            if (standing >= -80) return DiplomaticStance.Hostile;
            return DiplomaticStance.War;
        }

        private void GenerateTerritories()
        {
            // Physical territories
            _territories[new TerritoryKey(TerritoryType.SurfaceDistrict, "NeoShanghai_Central")] = new Territory
            {
                Id = "NeoShanghai_Central",
                Name = "Neo-Shanghai Central",
                Type = TerritoryType.SurfaceDistrict,
                OwnerCorp = Corporation.AetherCorp,
                Wealth = 1000,
                StrategicValue = 80,
                Defenses = 60,
                Description = "Corporate headquarters district. High aether concentration."
            };

            _territories[new TerritoryKey(TerritoryType.SurfaceDistrict, "NeoShanghai_Industrial")] = new Territory
            {
                Id = "NeoShanghai_Industrial",
                Name = "Industrial Sector 7",
                Type = TerritoryType.SurfaceDistrict,
                OwnerCorp = Corporation.IronClad,
                Wealth = 800,
                StrategicValue = 70,
                Defenses = 80,
                Description = "Manufacturing heart. Heavy militia presence."
            };

            _territories[new TerritoryKey(TerritoryType.SurfaceDistrict, "Prism_District")] = new Territory
            {
                Id = "Prism_District",
                Name = "Prism Entertainment District",
                Type = TerritoryType.SurfaceDistrict,
                OwnerCorp = Corporation.NeonDynasty,
                Wealth = 1200,
                StrategicValue = 50,
                Defenses = 40,
                Description = "Media, fashion, memetic warfare center."
            };

            // Orbital territories
            _territories[new TerritoryKey(TerritoryType.OrbitalStation, "AetherRing_Alpha")] = new Territory
            {
                Id = "AetherRing_Alpha",
                Name = "AetherCorp Orbital Ring Alpha",
                Type = TerritoryType.OrbitalStation,
                OwnerCorp = Corporation.AetherCorp,
                Wealth = 2000,
                StrategicValue = 100,
                Defenses = 90,
                Description = "Primary aether collection array. Heavily fortified."
            };

            _territories[new TerritoryKey(TerritoryType.OrbitalStation, "EventHorizon_Station")] = new Territory
            {
                Id = "EventHorizon_Station",
                Name = "Void Industries Event Horizon",
                Type = TerritoryType.OrbitalStation,
                OwnerCorp = Corporation.VoidIndustries,
                Wealth = 1500,
                StrategicValue = 90,
                Defenses = 95,
                Description = "Black site research station. Reality anchors unstable."
            };

            // Cyberspace territories
            _territories[new TerritoryKey(TerritoryType.CyberspaceSector, "Synapse_Core")] = new Territory
            {
                Id = "Synapse_Core",
                Name = "The Synapse Core",
                Type = TerritoryType.CyberspaceSector,
                OwnerCorp = Corporation.SynapseCollective,
                Wealth = 3000,
                StrategicValue = 100,
                Defenses = 85,
                Description = "Distributed consciousness nexus. ICE density extreme."
            };

            _territories[new TerritoryKey(TerritoryType.CyberspaceSector, "Neon_Net")] = new Territory
            {
                Id = "Neon_Net",
                Name = "Neon Dynasty Memetic Sphere",
                Type = TerritoryType.CyberspaceSector,
                OwnerCorp = Corporation.NeonDynasty,
                Wealth = 1800,
                StrategicValue = 70,
                Defenses = 60,
                Description = "Viral content farms. Narrative control nodes."
            };

            // Assign to faction states
            foreach (var kvp in _territories)
            {
                if (_factionStates.ContainsKey(kvp.Value.OwnerCorp))
                {
                    _factionStates[kvp.Value.OwnerCorp].Territories.Add(kvp.Value);
                }
            }
        }

        private void GenerateInitialContracts()
        {
            // Generic contract templates
            var templates = new[]
            {
                new ContractTemplate
                {
                    Type = ContractType.DataTheft,
                    Title = "Corporate Espionage: {Target} R&D",
                    Description = "Infiltrate {Target} servers and extract project data.",
                    BaseReward = 50000,
                    Difficulty = 3,
                    RequiredTraits = new[] { "ice_breaker" },
                    MinStage = RealmStage.Circuit
                },
                new ContractTemplate
                {
                    Type = ContractType.Assassination,
                    Title = "Elimination: {Target} Executive",
                    Description = "Terminate high-value target. Clean, deniable.",
                    BaseReward = 200000,
                    Difficulty = 5,
                    RequiredTraits = new[] { "ghost_protocol", "silent_kill" },
                    MinStage = RealmStage.Core
                },
                new ContractTemplate
                {
                    Type = ContractType.Sabotage,
                    Title = "Infrastructure Sabotage: {Target} {Asset}",
                    Description = "Destroy or disable key infrastructure.",
                    BaseReward = 100000,
                    Difficulty = 4,
                    RequiredTraits = new[] { "black_ice_breaker", "daemon_combat" },
                    MinStage = RealmStage.Core
                },
                new ContractTemplate
                {
                    Type = ContractType.Netrun,
                    Title = "Deep Netrun: {Target} Core",
                    Description = "Penetrate to core database. Extract or plant.",
                    BaseReward = 150000,
                    Difficulty = 4,
                    RequiredTraits = new[] { "packet_transmission", "trace_eraser" },
                    MinStage = RealmStage.Core
                },
                new ContractTemplate
                {
                    Type = ContractType.OrbitalStrike,
                    Title = "Orbital Strike Coordination: {Target}",
                    Description = "Designate target for kinetic bombardment.",
                    BaseReward = 500000,
                    Difficulty = 6,
                    RequiredTraits = new[] { "rod_from_god", "strike_authorized" },
                    MinStage = RealmStage.Satellite
                },
                new ContractTemplate
                {
                    Type = ContractType.ZeroDayHunt,
                    Title = "Zero-Day Exploit Acquisition",
                    Description = "Find, verify, and deliver unpatched reality exploit.",
                    BaseReward = 1000000,
                    Difficulty = 8,
                    RequiredTraits = new[] { "reality_fuzzer", "exploit_crafting" },
                    MinStage = RealmStage.Network
                }
            };

            // Generate contracts for each hostile corp
            foreach (var corp in Enum.GetValues(typeof(Corporation)))
            {
                if (corp == Corporation.None) continue;

                foreach (var template in templates)
                {
                    if (ArcanepunkEngine.Instance?.Player?.CurrentStage >= template.MinStage)
                    {
                        var contract = CreateContractFromTemplate(template, (Corporation)corp);
                        _availableContracts.Add(contract);
                    }
                }
            }

            // Sort by reward
            _availableContracts.Sort((a, b) => b.Reward.CompareTo(a.Reward));
        }

        private Contract CreateContractFromTemplate(ContractTemplate template, Corporation targetCorp)
        {
            var player = ArcanepunkEngine.Instance?.Player;
            float repMult = player != null ? 1f + GetCorpRep(targetCorp) / 100f : 1f;

            return new Contract
            {
                Id = Guid.NewGuid().ToString(),
                Template = template,
                TargetCorporation = targetCorp,
                Title = template.Title.Replace("{Target}", targetCorp.ToString()).Replace("{Asset}", "Asset"),
                Description = template.Description.Replace("{Target}", targetCorp.ToString()).Replace("{Asset}", "Asset"),
                Reward = Mathf.RoundToInt(template.BaseReward * repMult),
                Difficulty = template.Difficulty,
                RequiredTraits = template.RequiredTraits,
                State = ContractState.Available,
                IssuedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(30),
                ClientCorporation = DetermineClientCorp(targetCorp),
                Objectives = GenerateObjectives(template.Type, targetCorp)
            };
        }

        private Corporation DetermineClientCorp(Corporation target)
        {
            // Find a corp hostile to target
            var candidates = new List<Corporation>();
            foreach (Corporation c in Enum.GetValues(typeof(Corporation)))
            {
                if (c == Corporation.None || c == target) continue;
                var stance = _factionStates[c].DiplomaticStance.GetValueOrDefault(target, DiplomaticStance.Neutral);
                if (stance == DiplomaticStance.Hostile || stance == DiplomaticStance.War)
                    candidates.Add(c);
            }

            return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : Corporation.AetherCorp;
        }

        private ContractObjective[] GenerateObjectives(ContractType type, Corporation target)
        {
            switch (type)
            {
                case ContractType.DataTheft:
                    return new[] {
                        new ContractObjective { Description = "Infiltrate target network", IsCompleted = false },
                        new ContractObjective { Description = "Locate R&D database", IsCompleted = false },
                        new ContractObjective { Description = "Extract project data", IsCompleted = false },
                        new ContractObjective { Description = "Escape without trace", IsCompleted = false }
                    };
                case ContractType.Assassination:
                    return new[] {
                        new ContractObjective { Description = "Identify target location", IsCompleted = false },
                        new ContractObjective { Description = "Bypass security", IsCompleted = false },
                        new ContractObjective { Description = "Execute termination", IsCompleted = false },
                        new ContractObjective { Description = "Extract cleanly", IsCompleted = false }
                    };
                case ContractType.Netrun:
                    return new[] {
                        new ContractObjective { Description = "Establish beachhead", IsCompleted = false },
                        new ContractObjective { Description = "Navigate ICE layers", IsCompleted = false },
                        new ContractObjective { Description = "Reach core database", IsCompleted = false },
                        new ContractObjective { Description = "Complete objective", IsCompleted = false }
                    };
                default:
                    return new[] {
                        new ContractObjective { Description = "Primary objective", IsCompleted = false },
                        new ContractObjective { Description = "Secondary objective", IsCompleted = false },
                        new ContractObjective { Description = "Extract", IsCompleted = false }
                    };
            }
        }

        // ==================== PUBLIC API ====================
        public int GetCorpRep(Corporation corp)
        {
            if (corp == Corporation.None) return 0;
            return ArcanepunkEngine.Instance?.GetCorpRep(corp) ?? 0;
        }

        public void ModifyPlayerRep(Corporation corp, int amount)
        {
            ArcanepunkEngine.Instance?.ModifyCorpRep(corp, amount);
            OnRepChanged?.Invoke(corp, GetCorpRep(corp));

            // Update faction state
            if (_factionStates.ContainsKey(corp))
            {
                _factionStates[corp].PlayerReputation = GetCorpRep(corp);
            }
        }

        public Contract[] GetAvailableContracts()
        {
            return _availableContracts.Where(c => c.State == ContractState.Available && c.ExpiresAt > DateTime.Now).ToArray();
        }

        public Contract[] GetActiveContracts()
        {
            return _activeContracts.Where(c => c.State == ContractState.Active).ToArray();
        }

        public bool AcceptContract(string contractId)
        {
            var contract = _availableContracts.FirstOrDefault(c => c.Id == contractId);
            if (contract == null || contract.State != ContractState.Available) return false;
            if (_activeContracts.Count >= maxActiveContracts) return false;

            // Check requirements
            var player = ArcanepunkEngine.Instance?.Player;
            if (player != null)
            {
                foreach (var trait in contract.RequiredTraits)
                {
                    if (!Array.Exists(player.ActiveTraits, t => t == trait)) return false;
                }
            }

            contract.State = ContractState.Active;
            contract.AcceptedAt = DateTime.Now;
            _activeContracts.Add(contract);
            _availableContracts.Remove(contract);

            OnContractOffered?.Invoke(contract);
            return true;
        }

        public void CompleteContractObjective(string contractId, int objectiveIndex)
        {
            var contract = _activeContracts.FirstOrDefault(c => c.Id == contractId);
            if (contract == null || objectiveIndex >= contract.Objectives.Length) return;

            contract.Objectives[objectiveIndex].IsCompleted = true;

            // Check if all complete
            if (contract.Objectives.All(o => o.IsCompleted))
            {
                FinishContract(contract, true);
            }
        }

        private void FinishContract(Contract contract, bool success)
        {
            contract.State = success ? ContractState.Completed : ContractState.Failed;
            contract.CompletedAt = DateTime.Now;

            if (success)
            {
                // Grant reward
                ArcanepunkEngine.Instance?.GrantResource(ResourceType.Credits, contract.Reward);
                ArcanepunkEngine.Instance?.GrantResource(ResourceType.Reputation, contract.Reward / 1000);

                // Rep with client
                ModifyPlayerRep(contract.ClientCorporation, 10);
                // Rep loss with target
                ModifyPlayerRep(contract.TargetCorporation, -20);

                OnContractCompleted?.Invoke(contract);
            }
            else
            {
                // Penalty
                ModifyPlayerRep(contract.ClientCorporation, -10);
            }

            _activeContracts.Remove(contract);
        }

        // ==================== ESPIONAGE ====================
        public EspionageOperation StartEspionage(Corporation target, EspionageType type, int investment)
        {
            var player = ArcanepunkEngine.Instance?.Player;
            if (player == null) return null;

            // Check if already operating against target
            if (_activeOperations.Any(o => o.TargetCorp == target && o.State == EspionageState.Active))
                return null;

            var op = new EspionageOperation
            {
                Id = Guid.NewGuid().ToString(),
                TargetCorp = target,
                Type = type,
                InvestorCorp = player.Corp != Corporation.None ? player.Corp : Corporation.AetherCorp,
                Investment = investment,
                State = EspionageState.Active,
                StartedAt = DateTime.Now,
                Progress = 0f,
                SuccessChance = CalculateEspionageSuccessChance(player, target, type, investment),
                Discoveries = new List<EspionageDiscovery>()
            };

            _activeOperations.Add(op);

            // Simulate progress over time
            StartCoroutine(SimulateEspionage(op));

            return op;
        }

        private System.Collections.IEnumerator SimulateEspionage(EspionageOperation op)
        {
            float duration = GetEspionageDuration(op.Type);
            float elapsed = 0f;

            while (elapsed < duration && op.State == EspionageState.Active)
            {
                yield return new WaitForSeconds(1f); // 1 second = 1 day in-game
                elapsed += 1f;
                op.Progress = elapsed / duration;

                // Random events
                if (UnityEngine.Random.value < 0.1f)
                {
                    GenerateEspionageEvent(op);
                }
            }

            if (op.State == EspionageState.Active)
            {
                ResolveEspionage(op);
            }
        }

        private void GenerateEspionageEvent(EspionageOperation op)
        {
            // Chance of discovery, counter-intel, complication
            var events = new[]
            {
                "Discovered patrol route change",
                "Intercepted encrypted communique",
                "Found vulnerability in perimeter ICE",
                "Agent compromised, extracted",
                "False lead, wasted time",
                "Target aware, increased security"
            };

            var discovery = new EspionageDiscovery
            {
                Timestamp = DateTime.Now,
                Description = events[UnityEngine.Random.Range(0, events.Length)],
                Severity = UnityEngine.Random.Range(1, 4)
            };

            op.Discoveries.Add(discovery);

            // Adjust success chance
            if (discovery.Severity >= 3) op.SuccessChance *= 0.9f;
            else if (discovery.Severity <= 1) op.SuccessChance *= 1.05f;
        }

        private void ResolveEspionage(EspionageOperation op)
        {
            bool success = UnityEngine.Random.value < op.SuccessChance;
            op.State = success ? EspionageState.Success : EspionageState.Failed;
            op.CompletedAt = DateTime.Now;

            if (success)
            {
                GenerateEspionageRewards(op);
            }
            else
            {
                // Consequences
                var player = ArcanepunkEngine.Instance?.Player;
                if (player != null)
                {
                    ModifyPlayerRep(op.TargetCorp, -15);
                    player.Humanity = Mathf.Max(0, player.Humanity - 5);
                }
            }

            OnEspionageCompleted?.Invoke(op);
            _activeOperations.Remove(op);
        }

        private void GenerateEspionageRewards(EspionageOperation op)
        {
            var rewards = new List<EspionageReward>();

            switch (op.Type)
            {
                case EspionageType.IntelGathering:
                    rewards.Add(new EspionageReward { Type = "Intel", Description = "Strategic plans acquired", Value = 100000 });
                    rewards.Add(new EspionageReward { Type = "Rep", Description = "Client reputation increased", Value = 20 });
                    break;
                case EspionageType.TechnologyTheft:
                    rewards.Add(new EspionageReward { Type = "Circuit", Description = "Stolen spell-circuit schematic", Value = 50000 });
                    rewards.Add(new EspionageReward { Type = "Module", Description = "Cybernetic design specs", Value = 200000 });
                    break;
                case EspionageType.Sabotage:
                    rewards.Add(new EspionageReward { Type = "Credits", Description = "Sabotage contract paid", Value = 500000 });
                    rewards.Add(new EspionageReward { Type = "Chaos", Description = "Target operations disrupted", Value = 0 });
                    break;
                case EspionageType.Assassination:
                    rewards.Add(new EspionageReward { Type = "Credits", Description = "Bounty collected", Value = 1000000 });
                    rewards.Add(new EspionageReward { Type = "Fear", Description = "Target corp destabilized", Value = 0 });
                    break;
            }

            op.Rewards = rewards;

            // Apply rewards
            foreach (var reward in rewards)
            {
                ApplyEspionageReward(reward);
            }
        }

        private void ApplyEspionageReward(EspionageReward reward)
        {
            var player = ArcanepunkEngine.Instance?.Player;
            if (player == null) return;

            switch (reward.Type)
            {
                case "Credits":
                    ArcanepunkEngine.Instance.GrantResource(ResourceType.Credits, reward.Value);
                    break;
                case "Rep":
                    ModifyPlayerRep(player.Corp, reward.Value);
                    break;
                case "Circuit":
                    // Would grant specific circuit
                    break;
                case "Module":
                    // Would grant module blueprint
                    break;
            }
        }

        private float CalculateEspionageSuccessChance(PlayerState player, Corporation target, EspionageType type, int investment)
        {
            float baseChance = 0.3f;

            // Stat bonuses
            baseChance += player.CurrentStats.Intellect / 10000f;
            baseChance += player.CurrentStats.Integration / 10000f;
            baseChance += player.CurrentStats.Agility / 10000f;

            // Trait bonuses
            if (Array.Exists(player.ActiveTraits, t => t == "ice_breaker")) baseChance += 0.15f;
            if (Array.Exists(player.ActiveTraits, t => t == "trace_eraser")) baseChance += 0.1f;
            if (Array.Exists(player.ActiveTraits, t => t == "ghost_protocol")) baseChance += 0.1f;
            if (Array.Exists(player.ActiveTraits, t => t == "black_ice_native")) baseChance += 0.2f;

            // Daemon bonus
            if (player.ActiveDaemon?.Abilities.Contains("stealth") == true) baseChance += 0.1f;

            // Investment bonus
            baseChance += investment / 1000000f * 0.1f;

            // Target difficulty
            var targetState = _factionStates.GetValueOrDefault(target);
            if (targetState != null)
            {
                baseChance -= targetState.Corporation.StatBonuses.Intellect / 100f * 0.01f;
            }

            return Mathf.Clamp01(baseChance);
        }

        private float GetEspionageDuration(EspionageType type)
        {
            switch (type)
            {
                case EspionageType.IntelGathering: return 7f; // days
                case EspionageType.TechnologyTheft: return 14f;
                case EspionageType.Sabotage: return 10f;
                case EspionageType.Assassination: return 21f;
                default: return 7f;
            }
        }

        // ==================== WARFARE ====================
        public WarDeclaration DeclareWar(Corporation attacker, Corporation defender, string casusBelli)
        {
            var war = new WarDeclaration
            {
                Id = Guid.NewGuid().ToString(),
                Attacker = attacker,
                Defender = defender,
                CasusBelli = casusBelli,
                DeclaredAt = DateTime.Now,
                State = WarState.Active,
                AttackerScore = 0,
                DefenderScore = 0,
                Battles = new List<WarBattle>()
            };

            _activeWars.Add(war);

            // Update diplomatic stances
            _factionStates[attacker].DiplomaticStance[defender] = DiplomaticStance.War;
            _factionStates[defender].DiplomaticStance[attacker] = DiplomaticStance.War;

            OnWarDeclared?.Invoke(attacker, defender, war);
            return war;
        }

        public void ResolveWarBattle(WarDeclaration war, WarBattle battle)
        {
            war.Battles.Add(battle);

            if (battle.AttackerVictory)
            {
                war.AttackerScore += battle.StrategicValue;
                // Chance to capture territory
                if (UnityEngine.Random.value < 0.3f && battle.TerritoryId != null)
                {
                    CaptureTerritory(war.Attacker, battle.TerritoryId);
                }
            }
            else
            {
                war.DefenderScore += battle.StrategicValue;
            }

            // Check war end
            if (war.AttackerScore >= 100 || war.DefenderScore >= 100)
            {
                EndWar(war);
            }
        }

        private void CaptureTerritory(Corporation newOwner, string territoryId)
        {
            if (!_territories.TryGetValue(new TerritoryKey(TerritoryType.SurfaceDistrict, territoryId), out var territory))
                return;

            var oldOwner = territory.OwnerCorp;
            territory.OwnerCorp = newOwner;

            // Update faction states
            _factionStates[oldOwner].Territories.Remove(territory);
            _factionStates[newOwner].Territories.Add(territory);

            OnTerritoryCaptured?.Invoke(newOwner, territory);
        }

        private void EndWar(WarDeclaration war)
        {
            war.State = war.AttackerScore > war.DefenderScore ? WarState.AttackerVictory : WarState.DefenderVictory;
            war.EndedAt = DateTime.Now;

            // Post-war effects
            var loser = war.State == WarState.AttackerVictory ? war.Defender : war.Attacker;
            var winner = war.State == WarState.AttackerVictory ? war.Attacker : war.Defender;

            // Reparations, territory transfers, etc.
            ModifyPlayerRep(loser, -50);
            ModifyPlayerRep(winner, 30);

            _activeWars.Remove(war);
        }

        // ==================== TERRITORY INCOME ====================
        public long CalculateTerritoryIncome(Corporation corp)
        {
            if (!_factionStates.ContainsKey(corp)) return 0;

            long income = 0;
            foreach (var territory in _factionStates[corp].Territories)
            {
                income += (long)(territory.Wealth * territoryIncomeMultiplier);
            }
            return income;
        }

        // ==================== HELPERS ====================
        private void Update()
        {
            // Process active operations, wars, contracts
        }

        // ==================== DATA STRUCTURES ====================
        public enum DiplomaticStance { Allied, Friendly, Neutral, Hostile, War }
        public enum ContractType { DataTheft, Assassination, Sabotage, Netrun, OrbitalStrike, ZeroDayHunt, Protection, Smuggling }
        public enum ContractState { Available, Active, Completed, Failed, Expired }
        public enum EspionageType { IntelGathering, TechnologyTheft, Sabotage, Assassination, CounterIntel }
        public enum EspionageState { Active, Success, Failed, Compromised }
        public enum WarState { Active, AttackerVictory, DefenderVictory, Ceasefire }

        [Serializable]
        public class FactionState
        {
            public CorporationData Corporation;
            public int PlayerReputation;
            public int GlobalInfluence;
            public Dictionary<ResourceType, long> Resources;
            public List<Territory> Territories;
            public List<Contract> ActiveContracts;
            public List<Corporation> EspionageTargets;
            public Dictionary<Corporation, DiplomaticStance> DiplomaticStance;
        }

        [Serializable]
        public class ContractTemplate
        {
            public ContractType Type;
            public string Title;
            public string Description;
            public int BaseReward;
            public int Difficulty;
            public string[] RequiredTraits;
            public RealmStage MinStage;
        }

        [Serializable]
        public class Contract
        {
            public string Id;
            public ContractTemplate Template;
            public Corporation TargetCorporation;
            public Corporation ClientCorporation;
            public string Title;
            public string Description;
            public int Reward;
            public int Difficulty;
            public string[] RequiredTraits;
            public ContractState State;
            public DateTime IssuedAt;
            public DateTime ExpiresAt;
            public DateTime AcceptedAt;
            public DateTime CompletedAt;
            public ContractObjective[] Objectives;
        }

        [Serializable]
        public class ContractObjective
        {
            public string Description;
            public bool IsCompleted;
        }

        [Serializable]
        public class TerritoryKey : IEquatable<TerritoryKey>
        {
            public TerritoryType Type;
            public string Id;

            public TerritoryKey(TerritoryType type, string id)
            {
                Type = type;
                Id = id;
            }

            public bool Equals(TerritoryKey other)
            {
                return Type == other.Type && Id == other.Id;
            }

            public override bool Equals(object obj) => obj is TerritoryKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Type, Id);
        }

        public enum TerritoryType { SurfaceDistrict, OrbitalStation, CyberspaceSector, PlanetaryBody, VoidAnchor }

        [Serializable]
        public class Territory
        {
            public string Id;
            public string Name;
            public TerritoryType Type;
            public Corporation OwnerCorp;
            public int Wealth;
            public int StrategicValue;
            public int Defenses;
            public string Description;
        }

        [Serializable]
        public class EspionageOperation
        {
            public string Id;
            public Corporation TargetCorp;
            public Corporation InvestorCorp;
            public EspionageType Type;
            public int Investment;
            public EspionageState State;
            public DateTime StartedAt;
            public float Progress;
            public float SuccessChance;
            public List<EspionageDiscovery> Discoveries;
            public List<EspionageReward> Rewards;
            public DateTime CompletedAt;
        }

        [Serializable]
        public class EspionageDiscovery
        {
            public DateTime Timestamp;
            public string Description;
            public int Severity; // 1-3
        }

        [Serializable]
        public class EspionageReward
        {
            public string Type;
            public string Description;
            public int Value;
        }

        [Serializable]
        public class WarDeclaration
        {
            public string Id;
            public Corporation Attacker;
            public Corporation Defender;
            public string CasusBelli;
            public DateTime DeclaredAt;
            public WarState State;
            public int AttackerScore;
            public int DefenderScore;
            public List<WarBattle> Battles;
            public DateTime EndedAt;
        }

        [Serializable]
        public class WarBattle
        {
            public string Id;
            public DateTime Timestamp;
            public string TerritoryId;
            public bool AttackerVictory;
            public int StrategicValue;
            public int AttackerLosses;
            public int DefenderLosses;
        }
    }
}