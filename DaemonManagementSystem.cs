using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanepunk
{
    /// <summary>
    /// Daemon management: personality, loyalty, abilities, evolution, fusion, contracts.
    /// </summary>
    public class DaemonManagementSystem : MonoBehaviour
    {
        public static DaemonManagementSystem Instance { get; private set; }

        [Header("Daemon Config")]
        [SerializeField] private int maxActiveDaemons = 3;
        [SerializeField] private float loyaltyDecayPerYear = 1f;
        [SerializeField] private int fusionCostMultiplier = 1000;

        public event Action<DaemonInstance> OnDaemonAcquired;
        public event Action<DaemonInstance> OnDaemonLost;
        public event Action<DaemonInstance, int> OnLoyaltyChanged;
        public event Action<DaemonInstance> OnDaemonEvolved;
        public event Action<DaemonInstance, DaemonInstance, DaemonInstance> OnDaemonsFused;
        public event Action<DaemonInstance, string> OnDaemonRebelled;

        // State
        private List<DaemonInstance> _activeDaemons = new();
        private List<DaemonInstance> _storedDaemons = new(); // In daemon repository
        private DaemonRepository _repository;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        private void Start()
        {
            _repository = new DaemonRepository();
            InitializeStarterDaemons();
        }

        private void InitializeStarterDaemons()
        {
            // Give player a starter daemon based on class
            var player = ArcanepunkEngine.Instance?.Player;
            if (player == null) return;

            DaemonProgram starter = null;
            switch (player.Class)
            {
                case PlayerClass.Technomancer:
                    starter = ArcanepunkDataLoader.GetDaemon("daemon_debug_daemon"); // Logic
                    break;
                case PlayerClass.ChromeMystic:
                    starter = ArcanepunkDataLoader.GetDaemon("daemon_avatar_daemon"); // Shield
                    break;
                case PlayerClass.Netrunner:
                    starter = ArcanepunkDataLoader.GetDaemon("daemon_proxy_daemon"); // Stealth
                    break;
            }

            if (starter != null)
            {
                AcquireDaemon(starter);
            }
        }

        // ==================== ACQUISITION ====================
        public DaemonInstance AcquireDaemon(DaemonProgram program, int initialLoyalty = 50)
        {
            if (_activeDaemons.Count >= maxActiveDaemons && _storedDaemons.Count == 0)
            {
                // Must store one first
                return null;
            }

            var instance = new DaemonInstance
            {
                Id = Guid.NewGuid().ToString(),
                BaseProgram = program,
                Name = program.Name,
                Personality = program.Personality,
                Loyalty = initialLoyalty,
                Experience = 0,
                Level = 1,
                CurrentAbilities = new List<string>(program.Abilities),
                UnlockedAbilities = new List<string>(),
                MutationSlots = 0,
                ActiveMutations = new List<DaemonMutation>(),
                BondTier = DaemonBondTier.Stranger,
                AcquisitionDate = DateTime.Now,
                TotalDamageDealt = 0,
                TotalDamageMitigated = 0,
                ContractsCompleted = 0,
                TimesRebelled = 0
            };

            // Add personality-based starting traits
            ApplyPersonalityTraits(instance);

            if (_activeDaemons.Count < maxActiveDaemons)
            {
                _activeDaemons.Add(instance);
                if (ArcanepunkEngine.Instance?.Player != null)
                {
                    ArcanepunkEngine.Instance.Player.ActiveDaemon = instance;
                }
            }
            else
            {
                _storedDaemons.Add(instance);
            }

            OnDaemonAcquired?.Invoke(instance);
            Debug.Log($"[DaemonManagement] Acquired daemon: {instance.Name} ({instance.Personality})");
            return instance;
        }

        private void ApplyPersonalityTraits(DaemonInstance daemon)
        {
            switch (daemon.Personality)
            {
                case DaemonPersonality.Loyal:
                    daemon.Loyalty += 20;
                    daemon.UnlockedAbilities.Add("devotion_aura");
                    break;
                case DaemonPersonality.Pragmatic:
                    daemon.UnlockedAbilities.Add("efficiency_protocol");
                    break;
                case DaemonPersonality.Curious:
                    daemon.Experience += 100;
                    daemon.UnlockedAbilities.Add("analyze_weakness");
                    break;
                case DaemonPersonality.Chaotic:
                    daemon.MutationSlots += 1;
                    daemon.UnlockedAbilities.Add("entropy_surge");
                    break;
                case DaemonPersonality.Aggressive:
                    daemon.UnlockedAbilities.Add("berserker_protocol");
                    break;
                case DaemonPersonality.Protective:
                    daemon.UnlockedAbilities.Add("guardian_shield");
                    break;
                case DaemonPersonality.Subservient:
                    daemon.Loyalty += 15;
                    daemon.UnlockedAbilities.Add("perfect_execution");
                    break;
            }
        }

        // ==================== LOYALTY SYSTEM ====================
        public void ModifyLoyalty(DaemonInstance daemon, int amount, string reason = "")
        {
            int oldLoyalty = daemon.Loyalty;
            daemon.Loyalty = Mathf.Clamp(daemon.Loyalty + amount, 0, 100);

            // Update bond tier
            UpdateBondTier(daemon);

            // Check rebellion
            if (daemon.Loyalty <= 0 && oldLoyalty > 0)
            {
                TriggerRebellion(daemon, reason);
            }

            OnLoyaltyChanged?.Invoke(daemon, daemon.Loyalty - oldLoyalty);
        }

        private void UpdateBondTier(DaemonInstance daemon)
        {
            DaemonBondTier newTier;
            if (daemon.Loyalty >= 90) newTier = DaemonBondTier.Soulbound;
            else if (daemon.Loyalty >= 70) newTier = DaemonBondTier.Partner;
            else if (daemon.Loyalty >= 40) newTier = DaemonBondTier.Ally;
            else if (daemon.Loyalty >= 20) newTier = DaemonBondTier.Acquaintance;
            else newTier = DaemonBondTier.Stranger;

            if (newTier != daemon.BondTier)
            {
                daemon.BondTier = newTier;
                ApplyBondTierEffects(daemon);
            }
        }

        private void ApplyBondTierEffects(DaemonInstance daemon)
        {
            switch (daemon.BondTier)
            {
                case DaemonBondTier.Soulbound:
                    daemon.UnlockedAbilities.Add("soul_link");
                    daemon.MutationSlots += 2;
                    break;
                case DaemonBondTier.Partner:
                    daemon.UnlockedAbilities.Add("sync_attack");
                    daemon.MutationSlots += 1;
                    break;
                case DaemonBondTier.Ally:
                    daemon.UnlockedAbilities.Add("coordinated_strike");
                    break;
            }
        }

        private void TriggerRebellion(DaemonInstance daemon, string reason)
        {
            daemon.TimesRebelled++;
            daemon.Loyalty = 0;

            // Rebellion effects based on personality
            string rebellionType = "";
            switch (daemon.Personality)
            {
                case DaemonPersonality.Loyal:
                    rebellionType = "quiet_departure"; // Just leaves
                    break;
                case DaemonPersonality.Pragmatic:
                    rebellionType = "contract_breach"; // Demands payment
                    break;
                case DaemonPersonality.Curious:
                    rebellionType = "data_theft"; // Steals research
                    break;
                case DaemonPersonality.Chaotic:
                    rebellionType = "system_corrupt"; // Corrupts modules
                    break;
                case DaemonPersonality.Aggressive:
                    rebellionType = "hostile_takeover"; // Attacks player
                    break;
                case DaemonPersonality.Protective:
                    rebellionType = "protective_separation"; // Takes gear to "protect" it
                    break;
                case DaemonPersonality.Subservient:
                    rebellionType = "catatonic_shutdown"; // Becomes inert
                    break;
            }

            ApplyRebellionEffects(daemon, rebellionType);
            OnDaemonRebelled?.Invoke(daemon, rebellionType);

            // Remove from active
            _activeDaemons.Remove(daemon);
            if (ArcanepunkEngine.Instance?.Player?.ActiveDaemon == daemon)
            {
                ArcanepunkEngine.Instance.Player.ActiveDaemon = _activeDaemons.FirstOrDefault();
            }

            // Store in repository (can be recovered)
            _storedDaemons.Add(daemon);
        }

        private void ApplyRebellionEffects(DaemonInstance daemon, string type)
        {
            var player = ArcanepunkEngine.Instance?.Player;
            if (player == null) return;

            switch (type)
            {
                case "quiet_departure":
                    // Minimal penalty
                    break;
                case "contract_breach":
                    player.Resources.Credits = (long)(player.Resources.Credits * 0.9);
                    break;
                case "data_theft":
                    player.KnownCircuits.RemoveAt(UnityEngine.Random.Range(0, Math.Max(1, player.KnownCircuits.Count)));
                    break;
                case "system_corrupt":
                    foreach (var module in player.EquippedModules)
                    {
                        if (module != null && UnityEngine.Random.value < 0.3f)
                        {
                            module.IsCorrupted = true;
                        }
                    }
                    break;
                case "hostile_takeover":
                    player.CurrentHP = Mathf.Max(1, player.CurrentHP / 2);
                    break;
                case "protective_separation":
                    // Moves best module to storage
                    var bestModule = player.EquippedModules
                        .Where(m => m != null)
                        .OrderByDescending(m => m.Tier)
                        .FirstOrDefault();
                    if (bestModule != null)
                    {
                        UnequipModule(player, bestModule.Slot);
                    }
                    break;
                case "catatonic_shutdown":
                    // Daemon becomes unusable for 30 days
                    daemon.Loyalty = -100; // Special state
                    break;
            }
        }

        // ==================== EXPERIENCE & EVOLUTION ====================
        public void GrantDaemonExperience(DaemonInstance daemon, int amount)
        {
            daemon.Experience += amount;

            int requiredExp = GetRequiredExp(daemon.Level);
            while (daemon.Experience >= requiredExp && daemon.Level < 10)
            {
                daemon.Experience -= requiredExp;
                daemon.Level++;
                requiredExp = GetRequiredExp(daemon.Level);

                // Level up rewards
                OnLevelUp(daemon);
            }
        }

        private int GetRequiredExp(int level)
        {
            return level * level * 500; // 500, 2000, 4500, 8000, 12500...
        }

        private void OnLevelUp(DaemonInstance daemon)
        {
            // Unlock new ability from base program or mutation
            var available = daemon.BaseProgram.Abilities
                .Where(a => !daemon.CurrentAbilities.Contains(a) && !daemon.UnlockedAbilities.Contains(a))
                .ToList();

            if (available.Count > 0)
            {
                var newAbility = available[UnityEngine.Random.Range(0, available.Count)];
                daemon.UnlockedAbilities.Add(newAbility);
            }

            // Mutation chance
            if (UnityEngine.Random.value < 0.1f * daemon.MutationSlots)
            {
                GenerateMutation(daemon);
            }

            // Check for evolution
            if (daemon.Level >= 5 && daemon.BondTier >= DaemonBondTier.Partner)
            {
                TryEvolveDaemon(daemon);
            }
        }

        private void GenerateMutation(DaemonInstance daemon)
        {
            var mutations = new[]
            {
                new DaemonMutation { Name = "Overclocked", Effect = "Ability cooldown -20%", Value = 0.2f },
                new DaemonMutation { Name = "Hardened", Effect = "Damage taken -15%", Value = 0.15f },
                new DaemonMutation { Name = "Adaptive", Effect = "Resist last damage type", Value = 0.25f },
                new DaemonMutation { Name = "Vampiric", Effect = "Heal on damage dealt", Value = 0.1f },
                new DaemonMutation { Name = "Glitch", Effect = "Random ability trigger", Value = 0.05f },
                new DaemonMutation { Name = "Fragmented", Effect = "Split into two on death", Value = 0.01f }
            };

            var mutation = mutations[UnityEngine.Random.Range(0, mutations.Length)];
            daemon.ActiveMutations.Add(mutation);
        }

        private void TryEvolveDaemon(DaemonInstance daemon)
        {
            // Evolution requires: Level 5+, Partner bond, specific conditions
            DaemonProgram evolvedForm = null;

            // Check evolution paths
            switch (daemon.BaseProgram.Id)
            {
                case "daemon_debug_daemon":
                    if (daemon.TotalDamageMitigated > 10000)
                        evolvedForm = ArcanepunkDataLoader.GetDaemon("daemon_avatar_daemon"); // Logic -> Shield
                    break;
                case "daemon_proxy_daemon":
                    if (daemon.ContractsCompleted > 10)
                        evolvedForm = ArcanepunkDataLoader.GetDaemon("daemon_exploit_daemon"); // Stealth -> Exploit
                    break;
                case "daemon_compiler_daemon":
                    if (daemon.Level >= 8)
                        evolvedForm = ArcanepunkDataLoader.GetDaemon("daemon_architect_daemon"); // Compiler -> Architect
                    break;
            }

            if (evolvedForm != null)
            {
                EvolveDaemon(daemon, evolvedForm);
            }
        }

        public void EvolveDaemon(DaemonInstance daemon, DaemonProgram evolvedForm)
        {
            var oldName = daemon.Name;
            daemon.BaseProgram = evolvedForm;
            daemon.Name = evolvedForm.Name;
            daemon.CurrentAbilities = new List<string>(evolvedForm.Abilities);
            daemon.UnlockedAbilities.Clear();
            daemon.MutationSlots += 1;
            daemon.Loyalty = Mathf.Min(100, daemon.Loyalty + 20);

            ApplyPersonalityTraits(daemon);
            UpdateBondTier(daemon);

            OnDaemonEvolved?.Invoke(daemon);
            Debug.Log($"[DaemonManagement] {oldName} evolved into {daemon.Name}!");
        }

        // ==================== FUSION ====================
        public DaemonInstance FuseDaemons(DaemonInstance daemon1, DaemonInstance daemon2)
        {
            if (daemon1 == daemon2) return null;
            if (daemon1.BondTier < DaemonBondTier.Ally || daemon2.BondTier < DaemonBondTier.Ally) return null;

            var player = ArcanepunkEngine.Instance?.Player;
            long cost = (daemon1.Level + daemon2.Level) * fusionCostMultiplier * 100;
            if (player?.Resources.Credits < cost) return null;

            player.Resources.Credits -= cost;

            // Determine fusion result
            var resultProgram = DetermineFusionResult(daemon1.BaseProgram, daemon2.BaseProgram);
            if (resultProgram == null) return null;

            // Create fused daemon
            var fused = new DaemonInstance
            {
                Id = Guid.NewGuid().ToString(),
                BaseProgram = resultProgram,
                Name = $"{daemon1.Name}-{daemon2.Name} Prime",
                Personality = DetermineFusionPersonality(daemon1.Personality, daemon2.Personality),
                Loyalty = (daemon1.Loyalty + daemon2.Loyalty) / 2,
                Experience = (daemon1.Experience + daemon2.Experience) / 2,
                Level = Mathf.Max(daemon1.Level, daemon2.Level) + 1,
                CurrentAbilities = new List<string>(resultProgram.Abilities),
                UnlockedAbilities = MergeAbilities(daemon1.UnlockedAbilities, daemon2.UnlockedAbilities),
                MutationSlots = daemon1.MutationSlots + daemon2.MutationSlots + 1,
                ActiveMutations = MergeMutations(daemon1.ActiveMutations, daemon2.ActiveMutations),
                BondTier = DaemonBondTier.Partner,
                AcquisitionDate = DateTime.Now,
                IsFused = true,
                ParentDaemons = new[] { daemon1.Id, daemon2.Id }
            };

            // Remove parents
            _activeDaemons.Remove(daemon1);
            _activeDaemons.Remove(daemon2);
            _storedDaemons.Remove(daemon1);
            _storedDaemons.Remove(daemon2);

            _activeDaemons.Add(fused);
            if (player != null && player.ActiveDaemon == daemon1 || player.ActiveDaemon == daemon2)
            {
                player.ActiveDaemon = fused;
            }

            OnDaemonsFused?.Invoke(daemon1, daemon2, fused);
            Debug.Log($"[DaemonManagement] Fusion complete: {fused.Name}");
            return fused;
        }

        private DaemonProgram DetermineFusionResult(DaemonProgram d1, DaemonProgram d2)
        {
            // Specific fusion recipes
            var key = $"{d1.Id}+{d2.Id}";
            var reverseKey = $"{d2.Id}+{d1.Id}";

            // Fusion table
            var recipes = new Dictionary<string, string>
            {
                { "daemon_debug_daemon+daemon_avatar_daemon", "daemon_compiler_daemon" },
                { "daemon_proxy_daemon+daemon_exploit_daemon", "daemon_zero_day_daemon" },
                { "daemon_compiler_daemon+daemon_architect_daemon", "daemon_source_code_daemon" },
                { "daemon_sentinel_daemon+daemon_guardian_daemon", "daemon_bastion_daemon" },
                { "daemon_cleaner_daemon+daemon_null_daemon", "daemon_void_daemon" }
            };

            if (recipes.TryGetValue(key, out var resultId) || recipes.TryGetValue(reverseKey, out resultId))
            {
                return ArcanepunkDataLoader.GetDaemon(resultId);
            }

            // Generic fusion: higher tier wins
            return d1.Tier >= d2.Tier ? d1 : d2;
        }

        private DaemonPersonality DetermineFusionPersonality(DaemonPersonality p1, DaemonPersonality p2)
        {
            // Dominant personality logic
            var dominance = new Dictionary<DaemonPersonality, int>
            {
                { DaemonPersonality.Chaotic, 7 },
                { DaemonPersonality.Aggressive, 6 },
                { DaemonPersonality.Curious, 5 },
                { DaemonPersonality.Pragmatic, 4 },
                { DaemonPersonality.Protective, 3 },
                { DaemonPersonality.Loyal, 2 },
                { DaemonPersonality.Subservient, 1 }
            };

            return dominance[p1] >= dominance[p2] ? p1 : p2;
        }

        private List<string> MergeAbilities(List<string> a1, List<string> a2)
        {
            var merged = new HashSet<string>(a1);
            merged.UnionWith(a2);
            return merged.ToList();
        }

        private List<DaemonMutation> MergeMutations(List<DaemonMutation> m1, List<DaemonMutation> m2)
        {
            var merged = new List<DaemonMutation>(m1);
            merged.AddRange(m2);
            return merged;
        }

        // ==================== DAEMON REPOSITORY ====================
        public class DaemonRepository
        {
            public List<DaemonInstance> StoredDaemons = new();
            public int Capacity = 20;

            public void Store(DaemonInstance daemon)
            {
                if (StoredDaemons.Count < Capacity)
                {
                    StoredDaemons.Add(daemon);
                }
            }

            public DaemonInstance Retrieve(string daemonId)
            {
                var daemon = StoredDaemons.FirstOrDefault(d => d.Id == daemonId);
                if (daemon != null)
                {
                    StoredDaemons.Remove(daemon);
                }
                return daemon;
            }
        }

        // ==================== SWITCHING ====================
        public bool SetActiveDaemon(string daemonId)
        {
            var daemon = _activeDaemons.FirstOrDefault(d => d.Id == daemonId)
                ?? _storedDaemons.FirstOrDefault(d => d.Id == daemonId);

            if (daemon == null) return false;

            // Swap if from storage
            if (_storedDaemons.Contains(daemon))
            {
                if (_activeDaemons.Count >= maxActiveDaemons)
                {
                    // Move current active to storage
                    var current = ArcanepunkEngine.Instance?.Player?.ActiveDaemon;
                    if (current != null)
                    {
                        _activeDaemons.Remove(current);
                        _storedDaemons.Add(current);
                    }
                }

                _storedDaemons.Remove(daemon);
                _activeDaemons.Add(daemon);
            }

            ArcanepunkEngine.Instance.Player.ActiveDaemon = daemon;
            return true;
        }

        // ==================== YEARLY PROCESSING ====================
        public void ProcessYearlyDaemonUpdates()
        {
            foreach (var daemon in _activeDaemons)
            {
                // Loyalty decay
                ModifyLoyalty(daemon, -Mathf.RoundToInt(loyaltyDecayPerYear));

                // Experience gain from passive activity
                GrantDaemonExperience(daemon, 50);
            }

            foreach (var daemon in _storedDaemons)
            {
                // Stored daemons lose loyalty faster
                ModifyLoyalty(daemon, -Mathf.RoundToInt(loyaltyDecayPerYear * 2));
            }
        }

        // ==================== MODULE HELPER ====================
        private void UnequipModule(PlayerState player, string slot)
        {
            for (int i = 0; i < player.EquippedModules.Length; i++)
            {
                if (player.EquippedModules[i]?.Slot == slot)
                {
                    player.EquippedModules[i] = null;
                    break;
                }
            }
        }

        // ==================== DATA STRUCTURES ====================
        [Serializable]
        public class DaemonInstance
        {
            public string Id;
            public DaemonProgram BaseProgram;
            public string Name;
            public DaemonPersonality Personality;
            public int Loyalty;
            public int Experience;
            public int Level;
            public List<string> CurrentAbilities;
            public List<string> UnlockedAbilities;
            public int MutationSlots;
            public List<DaemonMutation> ActiveMutations;
            public DaemonBondTier BondTier;
            public DateTime AcquisitionDate;
            public long TotalDamageDealt;
            public long TotalDamageMitigated;
            public int ContractsCompleted;
            public int TimesRebelled;
            public bool IsFused;
            public string[] ParentDaemons;
        }

        [Serializable]
        public class DaemonMutation
        {
            public string Name;
            public string Effect;
            public float Value;
        }

        public enum DaemonBondTier { Stranger, Acquaintance, Ally, Partner, Soulbound }
    }
}