using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arcanepunk
{
    /// <summary>
    /// Runtime data loader for Arcanepunk world assets.
    /// Loads all JSON assets from Resources/Worlds/Arcanepunk/
    /// </summary>
    public static class ArcanepunkDataLoader
    {
        private const string BasePath = "Worlds/Arcanepunk";

        public static RealmLevel[] Realms;
        public static ClassData[] Classes;
        public static CorporationData[] Corporations;
        public static SpellCircuit[] SpellCircuits;
        public static CyberneticModule[] CyberneticModules;
        public static DaemonProgram[] DaemonPrograms;
        public static EventData[] Events;

        private static bool _initialized = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_initialized) return;

            Realms = LoadAll<RealmLevel>("Realms");
            Classes = LoadAll<ClassData>("Classes");
            Corporations = LoadAll<CorporationData>("Corporations");
            SpellCircuits = LoadAll<SpellCircuit>("SpellCircuits");
            CyberneticModules = LoadAll<CyberneticModule>("CyberneticModules");
            DaemonPrograms = LoadAll<DaemonProgram>("DaemonPrograms");
            Events = LoadAll<EventData>("Events");

            // Build lookup dictionaries
            BuildLookups();

            _initialized = true;
            Debug.Log($"[Arcanepunk] Loaded: {Realms.Length} realms, {Classes.Length} classes, {Corporations.Length} corps, {SpellCircuits.Length} circuits, {CyberneticModules.Length} modules, {DaemonPrograms.Length} daemons, {Events.Length} events");
        }

        private static T[] LoadAll<T>(string category) where T : class
        {
            var list = new List<T>();
            var files = Resources.LoadAll<TextAsset>($"{BasePath}/{category}");

            foreach (var file in files)
            {
                try
                {
                    if (file.name.EndsWith("_array"))
                    {
                        // JSON array format
                        var wrapper = JsonUtility.FromJson<Wrapper<T>>($"{{\"items\":{file.text}}}");
                        list.AddRange(wrapper.items);
                    }
                    else
                    {
                        // Single object or custom parsing
                        var item = JsonUtility.FromJson<T>(file.text);
                        list.Add(item);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Arcanepunk] Failed to load {category}/{file.name}: {e.Message}");
                }
            }

            return list.ToArray();
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }

        private static void BuildLookups()
        {
            ArcanepunkDatabase.AllRealms = Realms;
            ArcanepunkDatabase.AllClasses = Classes;
            ArcanepunkDatabase.AllCorps = Corporations;
            ArcanepunkDatabase.AllCircuits = SpellCircuits;
            ArcanepunkDatabase.AllModules = CyberneticModules;
            ArcanepunkDatabase.AllDaemons = DaemonPrograms;
            ArcanepunkDatabase.AllEvents = Events;
        }

        // ==================== QUERY HELPERS ====================

        public static RealmLevel GetRealm(RealmStage stage, int index)
        {
            return Array.Find(Realms, r => r.Stage == stage && r.Index == index);
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

        public static ClassData GetClass(ArcanepunkClass classType)
        {
            return Array.Find(Classes, c => c.ClassType == classType);
        }

        public static CorporationData GetCorp(Corporation corpId)
        {
            return Array.Find(Corporations, c => c.CorpId == corpId);
        }

        public static SpellCircuit GetCircuit(string id)
        {
            return Array.Find(SpellCircuits, c => c.Id == id);
        }

        public static SpellCircuit[] GetCircuitsByType(SpellCircuitType type)
        {
            return Array.FindAll(SpellCircuits, c => c.Type == type);
        }

        public static SpellCircuit[] GetCircuitsByTier(int tier)
        {
            return Array.FindAll(SpellCircuits, c => c.Tier == tier);
        }

        public static CyberneticModule GetModule(string id)
        {
            return Array.Find(CyberneticModules, m => m.Id == id);
        }

        public static CyberneticModule[] GetModulesBySlot(string slot)
        {
            return Array.FindAll(CyberneticModules, m => m.Slot == slot);
        }

        public static DaemonProgram GetDaemon(string id)
        {
            return Array.Find(DaemonPrograms, d => d.Id == id);
        }

        public static EventData GetEvent(string id)
        {
            return Array.Find(Events, e => e.Id == id);
        }

        public static EventData[] GetEventsForStage(RealmStage stage)
        {
            return Array.FindAll(Events, e => e.MinStage <= stage && e.MaxStage >= stage);
        }

        public static EventData[] GetEventsByTag(string tag)
        {
            return Array.FindAll(Events, e => Array.Exists(e.Tags, t => t == tag));
        }

        // ==================== VALIDATION ====================

        public static bool CanBreakthrough(PlayerState state, RealmLevel realm)
        {
            var req = realm.BreakthroughReq;

            if (state.Level < req.MinLevel) return false;
            if ((int)state.CurrentStage < req.MinStage) return false;

            if (state.CurrentStats.Intellect < req.StatThresholds.Intellect) return false;
            if (state.CurrentStats.Willpower < req.StatThresholds.Willpower) return false;
            if (state.CurrentStats.Integration < req.StatThresholds.Integration) return false;

            foreach (var trait in req.RequiredTraits)
            {
                if (!Array.Exists(state.ActiveTraits, t => t == trait)) return false;
            }

            foreach (var item in req.RequiredItems)
            {
                // Check inventory - simplified
            }

            for (int i = 0; i < req.CorpReputation.Length; i++)
            {
                if (state.CorpReputation[i] < req.CorpReputation[i]) return false;
            }

            return true;
        }

        public static float GetBreakthroughChance(PlayerState state, RealmLevel realm)
        {
            float baseChance = realm.Tribulation.Difficulty > 0 ? (10f - realm.Tribulation.Difficulty) * 10f : 50f;

            // Stat bonuses
            baseChance += (state.CurrentStats.Intellect - realm.BreakthroughReq.StatThresholds.Intellect) * 0.1f;
            baseChance += (state.CurrentStats.Willpower - realm.BreakthroughReq.StatThresholds.Willpower) * 0.1f;

            // Trait bonuses
            foreach (var trait in state.ActiveTraits)
            {
                if (trait.Contains("breakthrough") || trait.Contains("insight")) baseChance += 5f;
            }

            // Corp rep bonuses
            foreach (var rep in state.CorpReputation)
            {
                if (rep > 50) baseChance += 2f;
            }

            return Mathf.Clamp(baseChance, 5f, 95f);
        }
    }
}