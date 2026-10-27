using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanepunk
{
    /// <summary>
    /// Dual-layer combat system: Meatspace (physical) + Cyberspace (digital) simultaneous resolution.
    /// </summary>
    public class DualLayerCombat : MonoBehaviour
    {
        public static DualLayerCombat Instance { get; private set; }

        [Header("Combat Config")]
        [SerializeField] private float turnDuration = 3f; // seconds per turn
        [SerializeField] private int maxCombatants = 8;

        public event Action<CombatInstance> OnCombatStarted;
        public event Action<CombatInstance, CombatResult> OnCombatEnded;
        public event Action<CombatAction> OnActionExecuted;
        public event Action<Combatant, DamageInstance> OnDamageDealt;
        public event Action<Combatant> OnCombatantDefeated;

        private CombatInstance _activeCombat;
        private Queue<CombatAction> _actionQueue = new();
        private float _turnTimer;
        private bool _isProcessingTurn;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        public CombatInstance StartCombat(Combatant[] attackers, Combatant[] defenders, CombatType type = CombatType.Mixed)
        {
            _activeCombat = new CombatInstance
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                Attackers = new List<Combatant>(attackers),
                Defenders = new List<Combatant>(defenders),
                StartTime = DateTime.Now,
                Turn = 1,
                Phase = CombatPhase.Initiative,
                InitiativeOrder = new List<Combatant>(),
                CombatLog = new List<CombatLogEntry>()
            };

            // Determine initiative
            CalculateInitiative();

            _turnTimer = turnDuration;
            _isProcessingTurn = true;

            OnCombatStarted?.Invoke(_activeCombat);
            Debug.Log($"[DualLayerCombat] Combat started: {attackers.Length}v{defenders.Length} ({type})");

            return _activeCombat;
        }

        private void CalculateInitiative()
        {
            var allCombatants = new List<Combatant>();
            allCombatants.AddRange(_activeCombat.Attackers);
            allCombatants.AddRange(_activeCombat.Defenders);

            // Initiative = Agility + Intellect/2 + Integration/2 + bonuses
            foreach (var c in allCombatants)
            {
                c.Initiative = c.Stats.Agility + c.Stats.Intellect / 2 + c.Stats.Integration / 2;
                c.Initiative += UnityEngine.Random.Range(-5, 6); // Variance
            }

            _activeCombat.InitiativeOrder = allCombatants.OrderByDescending(c => c.Initiative).ToList();
        }

        private void Update()
        {
            if (_activeCombat == null || !_isProcessingTurn) return;

            _turnTimer -= Time.deltaTime;
            if (_turnTimer <= 0f)
            {
                ProcessTurn();
                _turnTimer = turnDuration;
            }
        }

        private void ProcessTurn()
        {
            if (_activeCombat.InitiativeOrder.Count == 0) return;

            var currentActor = _activeCombat.InitiativeOrder[0];
            _activeCombat.InitiativeOrder.RemoveAt(0);
            _activeCombat.InitiativeOrder.Add(currentActor); // Round robin

            if (!currentActor.IsAlive)
            {
                // Skip dead, process next immediately
                ProcessTurn();
                return;
            }

            // Generate AI action or wait for player input
            var action = currentActor.IsPlayerControlled 
                ? GetPlayerAction(currentActor) 
                : GenerateAIAction(currentActor);

            if (action != null)
            {
                ExecuteAction(action);
            }

            // Check win conditions
            CheckCombatEnd();

            // Next turn
            _activeCombat.Turn++;
            if (_activeCombat.Turn > 100) ForceEndCombat(CombatResult.Draw); // Safety
        }

        private CombatAction GetPlayerAction(Combatant actor)
        {
            // Would integrate with UI system
            // For now, auto-attack
            return new CombatAction
            {
                Actor = actor,
                Type = CombatActionType.Attack,
                Target = _activeCombat.Defenders.FirstOrDefault(d => d.IsAlive),
                Layer = CombatLayer.Meatspace,
                Timestamp = DateTime.Now
            };
        }

        private CombatAction GenerateAIAction(Combatant actor)
        {
            var validTargets = actor.Team == CombatTeam.Attacker 
                ? _activeCombat.Defenders.Where(d => d.IsAlive).ToList()
                : _activeCombat.Attackers.Where(a => a.IsAlive).ToList();

            if (validTargets.Count == 0) return null;

            // Simple AI: prefer layer where target is weakest
            var target = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
            var layer = ChooseOptimalLayer(actor, target);

            return new CombatAction
            {
                Actor = actor,
                Type = ChooseActionType(actor, layer),
                Target = target,
                Layer = layer,
                Timestamp = DateTime.Now
            };
        }

        private CombatLayer ChooseOptimalLayer(Combatant actor, Combatant target)
        {
            // Compare target's defenses in both layers
            float meatDefense = target.Stats.Might + target.Stats.Integration / 2;
            float cyberDefense = target.Stats.Intellect + target.Stats.Willpower + target.Stats.Integration;

            // Actor's offense in each layer
            float meatOffense = actor.Stats.Might + actor.Stats.Agility / 2;
            float cyberOffense = actor.Stats.Intellect + actor.Stats.Integration / 2;

            float meatEffectiveness = meatOffense / Mathf.Max(1, meatDefense);
            float cyberEffectiveness = cyberOffense / Mathf.Max(1, cyberDefense);

            return cyberEffectiveness > meatEffectiveness ? CombatLayer.Cyberspace : CombatLayer.Meatspace;
        }

        private CombatActionType ChooseActionType(Combatant actor, CombatLayer layer)
        {
            // 60% attack, 20% ability, 10% move, 10% defend
            float roll = UnityEngine.Random.value;
            if (roll < 0.6f) return CombatActionType.Attack;
            if (roll < 0.8f) return CombatActionType.Ability;
            if (roll < 0.9f) return CombatActionType.Move;
            return CombatActionType.Defend;
        }

        private void ExecuteAction(CombatAction action)
        {
            if (action.Target == null || !action.Target.IsAlive) return;

            switch (action.Type)
            {
                case CombatActionType.Attack:
                    ExecuteAttack(action);
                    break;
                case CombatActionType.Ability:
                    ExecuteAbility(action);
                    break;
                case CombatActionType.Move:
                    ExecuteMove(action);
                    break;
                case CombatActionType.Defend:
                    ExecuteDefend(action);
                    break;
            }

            OnActionExecuted?.Invoke(action);
            _activeCombat.CombatLog.Add(new CombatLogEntry
            {
                Turn = _activeCombat.Turn,
                Actor = actor.Name,
                Action = action.Type.ToString(),
                Target = action.Target?.Name,
                Layer = action.Layer,
                Timestamp = DateTime.Now
            });
        }

        private void ExecuteAttack(CombatAction action)
        {
            var damage = CalculateDamage(action);
            ApplyDamage(action.Target, damage);

            OnDamageDealt?.Invoke(action.Target, damage);

            if (!action.Target.IsAlive)
            {
                OnCombatantDefeated?.Invoke(action.Target);
            }
        }

        private DamageInstance CalculateDamage(CombatAction action)
        {
            var actor = action.Actor;
            var target = action.Target;

            float baseDamage = 0;
            DamageType damageType = DamageType.Kinetic;
            float penetration = 0;

            if (action.Layer == CombatLayer.Meatspace)
            {
                // Physical attack
                baseDamage = actor.Stats.Might * 1.5f + actor.Stats.Agility * 0.5f;
                damageType = DamageType.Kinetic;

                // Weapon bonuses
                var weaponModule = actor.EquippedModules.FirstOrDefault(m => m.Slot == "Weapon");
                if (weaponModule != null)
                {
                    baseDamage += weaponModule.StatBonuses.Might * 2;
                    damageType = weaponModule.GrantedTraits.Contains("penetrating_shot") ? DamageType.Kinetic : damageType;
                    penetration = weaponModule.GrantedTraits.Contains("penetrating_shot") ? 0.5f : 0;
                }
            }
            else
            {
                // Cyber attack
                baseDamage = actor.Stats.Intellect * 1.5f + actor.Stats.Integration * 0.5f;
                damageType = DamageType.Data;

                // Circuit bonuses
                foreach (var circuitId in actor.EquippedCircuits)
                {
                    var circuit = ArcanepunkDataLoader.GetCircuit(circuitId);
                    if (circuit != null && circuit.Type == SpellCircuitType.Offensive)
                    {
                        baseDamage += circuit.BaseDamage;
                        damageType = circuit.DamageType;
                        penetration += 0.1f;
                    }
                }

                // Daemon assist
                if (actor.ActiveDaemon?.Abilities.Contains("auto_aim") == true)
                {
                    baseDamage *= 1.2f;
                }
            }

            // Defense calculation
            float defense = 0;
            if (action.Layer == CombatLayer.Meatspace)
            {
                defense = target.Stats.Might + target.Stats.Integration / 2;
                if (target.ActiveTraits.Contains("kinetic_barrier")) defense *= 1.5f;
            }
            else
            {
                defense = target.Stats.Intellect + target.Stats.Willpower + target.Stats.Integration;
                if (target.ActiveTraits.Contains("encrypted_mind")) defense *= 1.5f;
                if (target.ActiveTraits.Contains("firewall_master")) defense *= 1.3f;
            }

            defense = Mathf.Max(1, defense * (1 - penetration));

            float finalDamage = Mathf.Max(1, baseDamage - defense / 2);

            // Crit chance
            bool isCrit = UnityEngine.Random.value < (actor.Stats.Agility / 100f * 0.1f + 0.05f);
            if (isCrit) finalDamage *= 2f;

            return new DamageInstance
            {
                Amount = Mathf.RoundToInt(finalDamage),
                Type = damageType,
                Layer = action.Layer,
                IsCritical = isCrit,
                Source = actor.Name,
                Penetration = penetration
            };
        }

        private void ApplyDamage(Combatant target, DamageInstance damage)
        {
            target.CurrentHP -= damage.Amount;

            // Layer-specific effects
            if (damage.Layer == CombatLayer.Cyberspace)
            {
                // Cyber damage can corrupt modules, delete circuits, reduce bandwidth
                if (damage.Type == DamageType.Data)
                {
                    target.Bandwidth = Mathf.Max(0, target.Bandwidth - damage.Amount / 10);
                    if (UnityEngine.Random.value < 0.1f)
                    {
                        CorruptRandomModule(target);
                    }
                }
                else if (damage.Type == DamageType.Entropic)
                {
                    target.Entropy += damage.Amount / 5;
                }
            }
            else
            {
                // Physical damage can damage chassis, reduce humanity
                if (damage.Type == DamageType.Kinetic || damage.Type == DamageType.Fire)
                {
                    target.Humanity = Mathf.Max(0, target.Humanity - damage.Amount / 20);
                }
            }

            // Death check
            if (target.CurrentHP <= 0)
            {
                target.IsAlive = false;
                target.CurrentHP = 0;
            }
        }

        private void CorruptRandomModule(Combatant target)
        {
            var modules = target.EquippedModules.Where(m => m != null).ToList();
            if (modules.Count == 0) return;

            var victim = modules[UnityEngine.Random.Range(0, modules.Count)];
            victim.StatBonuses = new StatModifiers(); // Disable bonuses
            victim.IsCorrupted = true;

            // Log
            _activeCombat.CombatLog.Add(new CombatLogEntry
            {
                Turn = _activeCombat.Turn,
                Actor = "SYSTEM",
                Action = "MODULE_CORRUPTED",
                Target = victim.Name,
                Layer = CombatLayer.Cyberspace
            });
        }

        private void ExecuteAbility(CombatAction action)
        {
            // Use equipped circuit or daemon ability
            var actor = action.Actor;

            if (action.Layer == CombatLayer.Cyberspace && actor.EquippedCircuits.Length > 0)
            {
                var circuitId = actor.EquippedCircuits[UnityEngine.Random.Range(0, actor.EquippedCircuits.Length)];
                var circuit = ArcanepunkDataLoader.GetCircuit(circuitId);

                if (circuit != null && CanAffordCircuit(actor, circuit))
                {
                    SpendCircuitResources(actor, circuit);
                    ApplyCircuitEffects(action.Target, circuit);
                }
            }
            else if (actor.ActiveDaemon != null && actor.ActiveDaemon.Abilities.Length > 0)
            {
                var ability = actor.ActiveDaemon.Abilities[UnityEngine.Random.Range(0, actor.ActiveDaemon.Abilities.Length)];
                ApplyDaemonAbility(action.Target, ability, actor.ActiveDaemon);
            }
        }

        private bool CanAffordCircuit(Combatant actor, SpellCircuit circuit)
        {
            return actor.Aether >= circuit.AetherCost &&
                   actor.Bandwidth >= circuit.BandwidthCost &&
                   actor.Compute >= circuit.ComputeCost;
        }

        private void SpendCircuitResources(Combatant actor, SpellCircuit circuit)
        {
            actor.Aether -= circuit.AetherCost;
            actor.Bandwidth -= circuit.BandwidthCost;
            actor.Compute -= circuit.ComputeCost;
        }

        private void ApplyCircuitEffects(Combatant target, SpellCircuit circuit)
        {
            // Damage
            if (circuit.BaseDamage > 0)
            {
                var damage = new DamageInstance
                {
                    Amount = circuit.BaseDamage,
                    Type = circuit.DamageType,
                    Layer = CombatLayer.Cyberspace,
                    Source = circuit.Name
                };
                ApplyDamage(target, damage);
            }

            // Effects
            foreach (var effect in circuit.Effects)
            {
                ApplyCircuitEffect(target, effect, circuit);
            }
        }

        private void ApplyCircuitEffect(Combatant target, string effect, SpellCircuit circuit)
        {
            switch (effect)
            {
                case "Burn":
                    target.StatusEffects.Add(new StatusEffect { Type = "Burn", Duration = 3, Power = circuit.BaseDamage / 10 });
                    break;
                case "Stun":
                    target.StatusEffects.Add(new StatusEffect { Type = "Stun", Duration = 1, Power = 1 });
                    break;
                case "Hack":
                    target.Bandwidth = Mathf.Max(0, target.Bandwidth - 50);
                    break;
                case "Teleport":
                    // Move target to random position
                    break;
                case "Barrier":
                    target.TemporaryDefense += circuit.BaseDamage / 2;
                    break;
                case "Daemon":
                    // Spawn temporary daemon
                    break;
            }
        }

        private void ApplyDaemonAbility(Combatant target, string ability, DaemonProgram daemon)
        {
            switch (ability)
            {
                case "project_barrier":
                    target.TemporaryDefense += 50;
                    break;
                case "brute_force":
                    target.Bandwidth = Mathf.Max(0, target.Bandwidth - 100);
                    break;
                case "heal_minor":
                    target.CurrentHP = Mathf.Min(target.MaxHP, target.CurrentHP + 20);
                    break;
            }
        }

        private void ExecuteMove(CombatAction action)
        {
            // Position change - affects range, cover, etc.
            action.Actor.Position = action.Target.Position; // Simplified
        }

        private void ExecuteDefend(CombatAction action)
        {
            action.Actor.IsDefending = true;
            action.Actor.DefenseBonus = 1.5f;
        }

        private void CheckCombatEnd()
        {
            bool attackersAlive = _activeCombat.Attackers.Any(a => a.IsAlive);
            bool defendersAlive = _activeCombat.Defenders.Any(d => d.IsAlive);

            if (!attackersAlive || !defendersAlive)
            {
                var result = attackersAlive ? CombatResult.AttackerVictory : CombatResult.DefenderVictory;
                ForceEndCombat(result);
            }
        }

        private void ForceEndCombat(CombatResult result)
        {
            _isProcessingTurn = false;
            var finishedCombat = _activeCombat;
            _activeCombat = null;

            OnCombatEnded?.Invoke(finishedCombat, result);
            Debug.Log($"[DualLayerCombat] Combat ended: {result}");
        }

        // ==================== DATA STRUCTURES ====================
        public enum CombatType { Meatspace, Cyberspace, Mixed }
        public enum CombatPhase { Initiative, Action, Resolution, Ended }
        public enum CombatLayer { Meatspace, Cyberspace, Both }
        public enum CombatTeam { Attacker, Defender }
        public enum CombatActionType { Attack, Ability, Move, Defend, Item, Flee }
        public enum CombatResult { AttackerVictory, DefenderVictory, Draw, Fled }

        [Serializable]
        public class CombatInstance
        {
            public string Id;
            public CombatType Type;
            public List<Combatant> Attackers;
            public List<Combatant> Defenders;
            public DateTime StartTime;
            public int Turn;
            public CombatPhase Phase;
            public List<Combatant> InitiativeOrder;
            public List<CombatLogEntry> CombatLog;
        }

        [Serializable]
        public class Combatant
        {
            public string Name;
            public CombatTeam Team;
            public bool IsPlayerControlled;
            public bool IsAlive = true;
            public int Initiative;
            public StatModifiers Stats;
            public int CurrentHP;
            public int MaxHP;
            public int Humanity = 100;
            public int Bandwidth = 1000;
            public int Aether = 1000;
            public int Compute = 500;
            public int Entropy = 0;
            public List<string> ActiveTraits = new();
            public string[] EquippedCircuits = new string[0];
            public CyberneticModule[] EquippedModules = new CyberneticModule[6];
            public DaemonProgram ActiveDaemon;
            public List<StatusEffect> StatusEffects = new();
            public float TemporaryDefense = 0;
            public float DefenseBonus = 1f;
            public bool IsDefending = false;
            public Vector3 Position;
        }

        [Serializable]
        public class CombatAction
        {
            public Combatant Actor;
            public Combatant Target;
            public CombatActionType Type;
            public CombatLayer Layer;
            public DateTime Timestamp;
            public SpellCircuit CircuitUsed;
            public string DaemonAbilityUsed;
        }

        [Serializable]
        public class DamageInstance
        {
            public int Amount;
            public DamageType Type;
            public CombatLayer Layer;
            public bool IsCritical;
            public string Source;
            public float Penetration;
        }

        [Serializable]
        public class CombatLogEntry
        {
            public int Turn;
            public string Actor;
            public string Action;
            public string Target;
            public CombatLayer Layer;
            public DateTime Timestamp;
        }

        [Serializable]
        public class StatusEffect
        {
            public string Type;
            public int Duration; // turns
            public float Power;
        }
    }
}