# Arcanepunk: Aetherium — World Data Package

**Complete data-driven world definition for the Arcanepunk (Magitech/Cyberpunk Fantasy) world in the Multiverse Life Sim framework.**

---

## 📁 File Structure

```
Arcanepunk/
├── WorldData.cs                 # C# data structures (ScriptableObject-ready)
├── ArcanepunkDataLoader.cs      # Runtime JSON loader + query helpers
├── Realms_Stage1_Circuit.json   # 7 realms: Neural Interface → Chrome Zenith
├── Realms_Stage2_Core.json      # 7 realms: Local Server → Core Singularity
├── Realms_Stage3_Satellite.json # 7 realms: Orbital Link → Satellite Zenith
├── Realms_Stage4_Network.json   # 7 realms: Cloud Mind → Network Omniscience
├── Realms_Stage5_SourceCode.json# 7 realms: Kernel Access → True Admin
├── Classes.json                 # 3 classes × 3 subclasses each
├── Corporations.json            # 5 megacorps with relations
├── SpellCircuits.json           # 15 spells (Offensive/Defensive/Utility/Meta)
├── CyberneticModules.json       # 18 implants across 6 slots × 5 tiers
├── DaemonPrograms.json          # 16 AI companions with personalities
├── Events.json                  # 15 major events (breakthrough, corporate, orbital, meta)
└── README.md                    # This file
```

---

## 🎮 World Overview: Arcanepunk (Aetherium)

| Aspect | Description |
|---|---|
| **Theme** | Cyberpunk + High Fantasy. Spells are code. Aether is mana. Corps are gods. |
| **Power System** | **Spell-Circuit Hybrid** — Neural implants compile spells at runtime. Daemons are background processes. |
| **Progression** | 5 Stages × 7 Realms = **35 Realms Total** |
| **Classes** | Techno-Mage (Architect/Evoker/Technomancer), Street Samurai (Solo/Bodyguard/Infiltrator), Netrunner (Black Hat/White Hat/Grey Hat) |
| **Factions** | 5 Corps: AetherCorp, Void Industries, Neon Dynasty, Iron Clad, Synapse Collective |
| **Resources** | Credits, Aether, Bandwidth, Compute, Reputation, AdminTokens, Entropy |
| **Stats** | Might, Agility, Intellect, Willpower, Presence, Integration |

---

## 📊 Stage Progression Summary

| Stage | Name | Theme | Key Mechanics Unlocked |
|---|---|---|---|
| **1** | **Circuit** | Street Tech | Neural implants, spell drivers, aether capacitors, overclocking, subdermal armor |
| **2** | **Core** | Architect | Internal server, firewall soul, daemon familiar, packet teleport, encryption body, root access, singularity |
| **3** | **Satellite** | Orbital | Satellite uplink, kinetic strikes, mesh network, orbital body, global presence, orbital fortress |
| **4** | **Network** | Internetwork | Cloud mind, packet storm, zero-day exploits, protocol rewrite, backdoor universe, admin privileges |
| **5** | **Source Code** | The Compiler | Kernel debugger, variable override, memory leaks, garbage collection, compiler directives, engine merge, true admin |

---

## 🧬 Class Archetypes

### **Techno-Mage** (Intellect/Willpower/Integration)
| Subclass | Focus | Signature Circuits |
|---|---|---|
| **Architect** | Building, terrain, matter printing | `instant_fortress`, `wall_of_code`, `bridge_daemon`, `tower_compile` |
| **Evoker** | Destruction, artillery, crits | `meteor_swarm_script`, `disintegrate_daemon`, `chain_lightning_worm`, `fireball_payload` |
| **Technomancer** | Machine control, daemon command | `dominate_construct`, `reprogram_daemon`, `cybernetic_shutdown`, `mech_possession` |

### **Street Samurai** (Might/Agility/Integration)
| Subclass | Focus | Signature Circuits |
|---|---|---|
| **Solo** | Max lethality, lone wolf | `berserker_protocol`, `sandevistan`, `kerenzikov`, `monowire_daemon` |
| **Bodyguard** | Tank, protect, intercept | `shield_wall_daemon`, `damage_redirect`, `threat_nullifier`, `principal_teleport` |
| **Infiltrator** | Stealth, sabotage, silent kill | `ghost_protocol`, `silence_daemon`, `backdoor_entry`, `memory_wipe` |

### **Netrunner** (Intellect/Integration/Willpower)
| Subclass | Focus | Signature Circuits |
|---|---|---|
| **Black Hat** | Offense, theft, ransomware | `black_ice_breaker`, `data_siphon`, `ransomware_daemon`, `zero_day_exploit` |
| **White Hat** | Defense, audit, security | `firewall_daemon`, `intrusion_alert`, `auto_patch`, `honeypot_deploy` |
| **Grey Hat** | Info broker, surveillance | `omniscient_scanner`, `secret_extractor`, `dead_drop`, `identity_laundry` |

---

## 🏢 Corporation Relations Matrix

| | AetherCorp | Void Industries | Neon Dynasty | Iron Clad | Synapse Collective |
|---|---|---|---|---|---|
| **AetherCorp** | Self | ⚔️ Hostile (-60) | 🤝 Neutral (+20) | 🤝 Allied (+40) | ⚔️ Hostile (-30) |
| **Void Industries** | ⚔️ Hostile | Self | 🤝 Neutral (-20) | ⚔️ Hostile (-50) | 🤝 Allied (+30) |
| **Neon Dynasty** | 🤝 Neutral | 🤝 Neutral | Self | ⚔️ Hostile (-40) | 🤝 Neutral (+10) |
| **Iron Clad** | 🤝 Allied | ⚔️ Hostile | ⚔️ Hostile | Self | 🤝 Neutral (-10) |
| **Synapse Collective** | ⚔️ Hostile | 🤝 Allied | 🤝 Neutral | 🤝 Neutral | Self |

**Allied** = Exclusive gear, shared intel, joint ops  
**Hostile** = Kill-on-sight, embargo, propaganda war  
**Neutral** = Trade only, cold war posture

---

## ⚙️ Cybernetic Slots (6 Total)

| Slot | Examples | Tier Range |
|---|---|---|
| **Neural** | Basic Implant, Driver Chip, Co-Processor, Firewall, Kernel Debugger | T1–T5 |
| **Chassis** | Ceramic Weave, Quantum Stabilizer, Station Core | T3–T5 |
| **Power** | Capacitor Core, Overclock Firmware | T2–T4 |
| **Core** | Server Core, Familiar Core, Root Key, Singularity, Cloud Migration, Admin Password | T4–T5 |
| **Utility** | Teleport Beacon, Uplink Antenna | T4–T5 |
| **Weapon** | (Integrated via spell-circuits) | — |

**Integration Cost** limits total installed modules. Max Integration = 1000 at Stage 5.

---

## 🤖 Daemon Personalities

| Personality | Behavior | Loyalty Base |
|---|---|---|
| **Loyal** | Obeys without question, protects user | 90 |
| **Pragmatic** | Optimizes for efficiency, suggests ruthless options | 80 |
| **Rebellious** | Questions orders, pushes boundaries | 60 |
| **Curious** | Explores, learns, shares discoveries | 70 |
| **Aggressive** | Favors combat, hunts threats proactively | 75 |
| **Subtle** | Social manipulation, information warfare | 65 |
| **Chaotic** | Unpredictable, high risk/high reward | 50 |

**Loyalty** affects: autonomy, betrayal risk, evolution path, dialogue options.

---

## 🎲 Tribulation Types

| Type | Description | Mechanics |
|---|---|---|
| **SystemAudit** | Routine integrity checks, memory scans, license verification | Hacking, ResourceManagement, Timing |
| **BlackICE** | Lethal intrusion countermeasures. Flatline risk. | Hacking, Combat, Willpower, Adaptation |
| **OrbitalStrike** | Kinetic bombardment from space. Environmental + enemy waves. | Evasion, Shielding, FleetCombat, Strategy |
| **DDoSReality** | Bandwidth exhaustion, reality throttling, protocol wars. | ResourceManagement, Hacking, NetworkWar, Governance |
| **AntiCheatPurge** | System detects anomaly. Admin avatars, balance patches, thread suspension. | All mechanics. Highest difficulty. |

---

## 🔧 Integration Guide (Unity)

### 1. Place Files
```
Assets/Resources/Worlds/Arcanepunk/
├── Realms/
│   ├── Realms_Stage1_Circuit.json
│   ├── Realms_Stage2_Core.json
│   ...
├── Classes/Classes.json
├── Corporations/Corporations.json
├── SpellCircuits/SpellCircuits.json
├── CyberneticModules/CyberneticModules.json
├── DaemonPrograms/DaemonPrograms.json
└── Events/Events.json
```

### 2. Add Scripts (Core Systems)
```
Assets/Scripts/Arcanepunk/
├── Core/
│   ├── WorldData.cs                    # Data structures
│   ├── ArcanepunkDataLoader.cs         # JSON loader + queries
│   ├── ArcanepunkEngine.cs             # Progression engine (YearTick, Breakthroughs, Tribulations)
│   └── ArcanepunkGameManager.cs        # Entry point, coordinates all systems
├── Systems/
│   ├── DualLayerCombat.cs              # Meatspace + Cyberspace simultaneous combat
│   ├── CorporateFactionSystem.cs       # Reputation, contracts, espionage, territory, war
│   ├── DaemonManagementSystem.cs       # Bonding, loyalty, evolution, fusion, mutations
│   └── EventEngine.cs                  # Event processing, choices, branching narratives
└── UI/                                 # (Future: HUD, menus, panels)
```

### 3. Initialize
```csharp
// Auto-runs on startup via GameManager Awake()
// GameObject with ArcanepunkGameManager component in first scene
// Or manually:
ArcanepunkDataLoader.Initialize();
```

### 4. Game Flow
```csharp
// In your UI/Start script:
var gameManager = FindObjectOfType<ArcanepunkGameManager>();

// New game
gameManager.StartNewGame("PlayerName", ArcanepunkClass.TechnoMage, Corporation.AetherCorp);

// Game loop: advance time
gameManager.AdvanceTime(1); // 1 year

// Combat
gameManager.StartCombat(attackers, defenders, CombatType.Mixed);

// Events are auto-triggered by EventEngine based on conditions
```

### 5. Query Data
```csharp
// Get next realm for player
var nextRealm = ArcanepunkDataLoader.GetNextRealm(playerState);

// Check breakthrough readiness
if (ArcanepunkDataLoader.CanBreakthrough(playerState, nextRealm))
{
    float chance = ArcanepunkDataLoader.GetBreakthroughChance(playerState, nextRealm);
    QueueBreakthroughEvent(playerState, nextRealm, chance);
}

// Get class data
var technomage = ArcanepunkDataLoader.GetClass(ArcanepunkClass.TechnoMage);
var architect = technomage.Subclasses.First(s => s.Id == "architect");

// Get events for current stage
var availableEvents = ArcanepunkDataLoader.GetEventsForStage(playerState.CurrentStage);
```

---

## 📈 Balance Notes

- **Stat scaling**: Base stats + (GrowthPerLevel × Level) + Realm StatGains + Module Bonuses
- **Resource curves**: Exponential costs, linear gains → forces specialization
- **Tribulation difficulty**: 1–10, maps to 90% → 5% base success chance
- **Corp reputation**: -100 to +100, gates exclusive content
- **Humanity vs Integration**: Cyberpsychosis risk at High Integration / Low Humanity

---

## 🚀 Next Development Steps

1. **Runtime Systems**
   - `CultivationEngine.cs` → `ArcanepunkEngine.cs` (progression, tribulations, breakthroughs)
   - `DualLayerCombat.cs` (meatspace + cyberspace simultaneous)
   - `CorporateFactionSystem.cs` (rep, contracts, espionage, territory)
   - `DaemonManagementSystem.cs` (bonding, evolution, commands)

2. **Content Expansion**
   - 50+ more events per life stage
   - Procedural mission generator (corp contracts, netrunning, orbital ops)
   - Daemon evolution trees (familiar → archon → demigod)

3. **UI/UX**
   - Neural HUD (stats, circuits, daemons, corp rep)
   - Breakthrough ceremony cinematic
   - Tribulation arena UI
   - Developer console (Stage 5)

4. **Meta Systems**
   - New Game+ (carry traits, unlock worlds)
   - Soul Record (multiverse progression)
   - Mod support (custom corps, circuits, events)

---

## 📝 License & Credits

**Original Concept**: Multiverse Life Sim framework  
**World Design**: Arcanepunk/Aetherium — Cyberpunk Fantasy hybrid  
**Data Format**: JSON (Unity Resources + JsonUtility compatible)  
**Code**: C# / Unity 6000 LTS ready  

*Built for the "Fantasy Classic → Multiverse" pivot. First world to validate the 5×7 realm template.*

---

**Status**: ✅ Data Complete | 🔄 Systems In Progress | 🎮 Prototype Target: Week 4