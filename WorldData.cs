using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanepunk
{
    // ==================== CORE ENUMS ====================
    public enum RealmStage { Circuit = 1, Core = 2, Satellite = 3, Network = 4, SourceCode = 5 }
    public enum ArcanepunkClass { TechnoMage, StreetSamurai, Netrunner }
    public enum Corporation { AetherCorp, VoidIndustries, NeonDynasty, IronClad, SynapseCollective }
    public enum SpellCircuitType { Offensive, Defensive, Utility, Mobility, Control, Info, Meta }
    public enum ResourceType { Credits, Aether, Bandwidth, Compute, Reputation, AdminTokens, Entropy }
    public enum DamageType { Kinetic, Energy, Fire, Ice, Lightning, Acid, Poison, Psychic, Data, Entropic }
    public enum TribulationType { SystemAudit, BlackICE, OrbitalStrike, DDoSReality, AntiCheatPurge }

    // ==================== CORE DATA STRUCTS ====================
    
    [Serializable]
    public struct StatModifiers
    {
        public int Might;       // Physical damage, carry capacity
        public int Agility;     // Evasion, initiative, ranged
        public int Intellect;   // Spell power, hacking, crafting
        public int Willpower;   // Mental resist, concentration, psi
        public int Presence;    // Social, leadership, daemon control
        public int Integration; // Cybernetic compatibility, sync rate
        
        public static StatModifiers operator +(StatModifiers a, StatModifiers b) => new StatModifiers
        {
            Might = a.Might + b.Might,
            Agility = a.Agility + b.Agility,
            Intellect = a.Intellect + b.Intellect,
            Willpower = a.Willpower + b.Willpower,
            Presence = a.Presence + b.Presence,
            Integration = a.Integration + b.Integration
        };
    }

    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType Type;
        public long Amount;
    }

    [Serializable]
    public struct Requirement
    {
        public int MinLevel;
        public int MinStage; // RealmStage as int
        public StatModifiers StatThresholds;
        public string[] RequiredTraits;
        public string[] RequiredItems;
        public int[] CorpReputation; // Index = Corporation enum
        public bool RequiresSecludedResearch;
        public bool RequiresBreakthroughInsight;
    }

    // ==================== REALM DEFINITION ====================
    
    [Serializable]
    public class RealmLevel
    {
        public int Index; // 1-7
        public string Id;
        public string Name;
        public string Description;
        public RealmStage Stage;
        public StatModifiers StatGains;
        public string[] GrantedTraits;
        public string[] UnlockedMechanics;
        public ResourceType[] UnlockedResources;
        public Requirement BreakthroughReq;
        public TribulationData Tribulation;
        public string NarrativeBeat;
    }

    [Serializable]
    public class TribulationData
    {
        public string Id;
        public string Name;
        public string Description;
        public TribulationType Type;
        public int Difficulty; // 1-10
        public ThreatData[] Threats;
        public RewardData[] SuccessRewards;
        public ConsequenceData[] FailureConsequences;
        public bool IsScripted;
    }

    [Serializable]
    public class ThreatData
    {
        public string Type; // "Enemy", "Environmental", "Internal", "Puzzle"
        public string Description;
        public int PowerLevel;
        public DamageType[] DamageTypes;
        public string[] Mechanics; // "Hacking", "Timing", "ResourceManagement"
    }

    [Serializable]
    public class RewardData
    {
        public string Type; // "Trait", "Item", "Resource", "Unlock", "Reputation"
        public string Id;
        public int Amount;
    }

    [Serializable]
    public class ConsequenceData
    {
        public string Type; // "StatDrain", "TraitLoss", "LevelDown", "Corruption", "Death", "Debt"
        public string Description;
        public int Severity; // 1-5
    }

    // ==================== CLASS DEFINITION ====================
    
    [Serializable]
    public class ClassData
    {
        public ArcanepunkClass ClassType;
        public string Name;
        public string Description;
        public StatModifiers BaseStats;
        public StatModifiers GrowthPerLevel;
        public string[] StartingTraits;
        public string[] StartingSpellCircuits;
        public Corporation[] PreferredCorps;
        public SubclassData[] Subclasses; // Unlock at Stage 2 (Core)
    }

    [Serializable]
    public class SubclassData
    {
        public string Id;
        public string Name;
        public string Description;
        public string[] GrantedTraits;
        public string[] ExclusiveCircuits;
        public StatModifiers BonusStats;
    }

    // ==================== CORPORATION DEFINITION ====================
    
    [Serializable]
    public class CorporationData
    {
        public Corporation CorpId;
        public string Name;
        public string Tagline;
        public string Description;
        public string[] Specializations; // "Military", "Research", "Finance", "Media", "BlackOps"
        public StatModifiers StatBonuses; // For employees/members
        public string[] ExclusiveTraits;
        public string[] ExclusiveCircuits;
        public string[] ExclusiveItems;
        public FactionRelation[] Relations; // Other corps
        public string HQ; // Location
        public string CEO; // NPC Name
        public string Aesthetic; // Visual theme
    }

    [Serializable]
    public class FactionRelation
    {
        public Corporation TargetCorp;
        public int Standing; // -100 to 100
        public string Status; // "Allied", "Neutral", "Hostile", "War"
    }

    // ==================== SPELL CIRCUIT ====================
    
    [Serializable]
    public class SpellCircuit
    {
        public string Id;
        public string Name;
        public SpellCircuitType Type;
        public int Tier; // 1-5 (unlock by stage)
        public int AetherCost;
        public int BandwidthCost;
        public int ComputeCost;
        public float Cooldown;
        public string Description;
        public DamageType DamageType;
        public int BaseDamage;
        public float[] Scaling; // [Might, Agility, Intellect, Willpower, Presence, Integration]
        public string[] Effects; // "Burn", "Stun", "Hack", "Teleport", "Shield"
        public string[] Upgrades; // Next tier circuit IDs
        public Corporation[] CorpRestricted; // Empty = universal
        public string VisualEffect;
        public string SoundEffect;
    }

    // ==================== ITEM / GEAR ====================
    
    [Serializable]
    public class CyberneticModule
    {
        public string Id;
        public string Name;
        public string Slot; // "Neural", "Chassis", "Power", "Weapon", "Utility", "Core"
        public int Tier; // 1-5
        public int CreditsCost;
        public int AetherCost;
        public StatModifiers StatBonuses;
        public string[] GrantedTraits;
        public string[] GrantedCircuits;
        public int IntegrationCost; // Integration points required
        public Corporation[] CorpRestricted;
        public string Description;
        public string VisualModel;
    }

    [Serializable]
    public class DaemonProgram
    {
        public string Id;
        public string Name;
        public string Type; // "Combat", "Utility", "Info", "Social", "Meta"
        public int BandwidthCost;
        public int ComputeCost;
        public string[] Abilities;
        public Personality Personality; // Affects dialogue, decisions
        public int Loyalty; // 0-100
        public Corporation[] OriginCorp;
    }

    public enum Personality { Loyal, Pragmatic, Rebellious, Curious, Aggressive, Subtle, Chaotic }

    // ==================== EVENT SYSTEM ====================
    
    [Serializable]
    public class EventData
    {
        public string Id;
        public string Title;
        public string Description;
        public RealmStage MinStage;
        public RealmStage MaxStage;
        public string[] Tags; // "Combat", "Social", "Exploration", "Research", "Corporate", "Netrunning"
        public ChoiceData[] Choices;
        public string[] Prerequisites; // Traits, Items, Rep, Stats
        public bool IsRepeatable;
        public int CooldownDays;
        public WeightModifiers WeightMods; // Dynamic weighting
    }

    [Serializable]
    public class ChoiceData
    {
        public string Text;
        public string[] RequiredTraits;
        public int[] RequiredStats; // Index = Stat enum order
        public Requirement Requirement;
        public EffectData[] Effects;
        public string[] NextEventIds; // Chain events
        public string FlavorText;
    }

    [Serializable]
    public class EffectData
    {
        public string Type; // "Stat", "Trait", "Resource", "Rep", "Item", "Circuit", "Mechanic", "Flag"
        public string TargetId;
        public int Amount;
        public bool IsPermanent;
        public string Condition; // Optional: "onSuccess", "onFail", "always"
    }

    [Serializable]
    public class WeightModifiers
    {
        public Dictionary<string, float> TagWeights; // Tag -> multiplier
        public Dictionary<string, float> StatWeights; // Stat -> multiplier
        public Dictionary<string, float> CorpWeights; // Corp -> multiplier
    }

    // ==================== WORLD STATE ====================
    
    [Serializable]
    public class PlayerState
    {
        public string CharacterId;
        public string CharacterName;
        public ArcanepunkClass Class;
        public string Subclass; // Empty until Stage 2
        public Corporation Corp; // Can be "None" (Freelancer)
        public int Level;
        public RealmStage CurrentStage;
        public int CurrentRealmIndex; // 1-7 within stage
        public long Experience;
        public StatModifiers BaseStats;
        public StatModifiers CurrentStats;
        public string[] ActiveTraits;
        public string[] KnownCircuits;
        public string[] EquippedCircuits; // Max 6 active
        public Dictionary<ResourceType, long> Resources;
        public int[] CorpReputation; // Index = Corporation enum
        public CyberneticModule[] InstalledModules; // 6 slots
        public DaemonProgram ActiveDaemon;
        public Dictionary<string, int> Flags; // Quest flags, world state
        public int Karma; // -1000 to 1000
        public int Humanity; // 0-100 (cyberpsychosis risk)
        public long PlayTimeSeconds;
        public DateTime LastSave;
    }

    // ==================== STATIC DATABASE ====================
    
    public static class ArcanepunkDatabase
    {
        public static RealmLevel[] AllRealms;
        public static ClassData[] AllClasses;
        public static CorporationData[] AllCorps;
        public static SpellCircuit[] AllCircuits;
        public static CyberneticModule[] AllModules;
        public static DaemonProgram[] AllDaemons;
        public static EventData[] AllEvents;

        public static void Initialize()
        {
            // Load from JSON assets at runtime
            // AllRealms = LoadResources<RealmLevel>("Realms");
            // AllClasses = LoadResources<ClassData>("Classes");
            // etc.
        }

        public static RealmLevel GetRealm(RealmStage stage, int index)
        {
            return Array.Find(AllRealms, r => r.Stage == stage && r.Index == index);
        }

        public static RealmLevel GetNextRealm(PlayerState state)
        {
            int nextIndex = state.CurrentRealmIndex + 1;
            RealmStage nextStage = state.CurrentStage;
            
            if (nextIndex > 7)
            {
                nextIndex = 1;
                nextStage = (RealmStage)((int)nextStage + 1);
            }
            
            return GetRealm(nextStage, nextIndex);
        }
    }
}