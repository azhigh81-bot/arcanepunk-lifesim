using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arcanepunk
{
    /// <summary>
    /// Event engine: processes Events.json, handles choices, consequences, branching narratives.
    /// </summary>
    public class EventEngine : MonoBehaviour
    {
        public static EventEngine Instance { get; private set; }

        [Header("Event Config")]
        [SerializeField] private float eventCheckInterval = 1f; // Years between checks
        [SerializeField] private int maxConcurrentEvents = 3;
        [SerializeField] private bool debugLogEvents = true;

        public event Action<GameEvent> OnEventTriggered;
        public event Action<GameEvent, EventChoice> OnChoiceMade;
        public event Action<GameEvent, EventOutcome> OnEventResolved;

        // State
        private List<GameEvent> _allEvents = new();
        private List<ActiveEvent> _activeEvents = new();
        private Dictionary<string, int> _eventHistory = new(); // eventId -> times triggered
        private float _yearTimer = 0f;
        private PlayerState _player;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        private void Start()
        {
            _player = ArcanepunkEngine.Instance?.Player;
            LoadEvents();
        }

        private void LoadEvents()
        {
            _allEvents = ArcanepunkDataLoader.GetAllEvents();
            if (debugLogEvents)
            {
                Debug.Log($"[EventEngine] Loaded {_allEvents.Count} events");
            }
        }

        private void Update()
        {
            if (_player == null) return;

            _yearTimer += Time.deltaTime;
            if (_yearTimer >= eventCheckInterval)
            {
                _yearTimer = 0f;
                CheckAndTriggerEvents();
            }

            // Update active events (timeouts, etc.)
            UpdateActiveEvents();
        }

        private void CheckAndTriggerEvents()
        {
            if (_activeEvents.Count >= maxConcurrentEvents) return;

            var candidates = _allEvents
                .Where(e => CanTriggerEvent(e))
                .OrderByDescending(e => CalculateEventWeight(e))
                .ToList();

            foreach (var eventData in candidates)
            {
                if (_activeEvents.Count >= maxConcurrentEvents) break;

                if (UnityEngine.Random.value < eventData.TriggerChance)
                {
                    TriggerEvent(eventData);
                }
            }
        }

        private bool CanTriggerEvent(GameEvent eventData)
        {
            // Check stage requirement
            if (_player.CurrentStage < eventData.MinStage) return false;

            // Check cooldown
            if (_eventHistory.TryGetValue(eventData.Id, out int count))
            {
                if (count >= eventData.MaxTriggers) return false;
                if (eventData.CooldownYears > 0 && _player.Age - _eventHistory.GetValueOrDefault(eventData.Id + "_lastAge", 0) < eventData.CooldownYears)
                    return false;
            }

            // Check prerequisites
            if (!CheckPrerequisites(eventData.Prerequisites)) return false;

            // Check corp reputation requirements
            if (eventData.RequiredCorpRep != null)
            {
                foreach (var req in eventData.RequiredCorpRep)
                {
                    if (_player.GetCorpRep(req.Key) < req.Value) return false;
                }
            }

            // Check trait requirements
            if (eventData.RequiredTraits != null)
            {
                foreach (var trait in eventData.RequiredTraits)
                {
                    if (!_player.ActiveTraits.Contains(trait)) return false;
                }
            }

            // Check stat requirements
            if (eventData.RequiredStats != null)
            {
                foreach (var statReq in eventData.RequiredStats)
                {
                    int playerStat = GetPlayerStat(statReq.Key);
                    if (playerStat < statReq.Value) return false;
                }
            }

            return true;
        }

        private bool CheckPrerequisites(List<string> prereqs)
        {
            if (prereqs == null || prereqs.Count == 0) return true;

            foreach (var prereq in prereqs)
            {
                // Format: "event:event_id:outcome:outcome_id" or "flag:flag_name" or "corp_rep:corp:value"
                var parts = prereq.Split(':');
                if (parts.Length < 2) continue;

                switch (parts[0])
                {
                    case "event":
                        if (parts.Length >= 4)
                        {
                            string eventId = parts[1];
                            string outcomeId = parts[3];
                            // Would need to track event outcomes
                        }
                        break;
                    case "flag":
                        if (!_player.StoryFlags.Contains(parts[1])) return false;
                        break;
                    case "corp_rep":
                        if (Enum.TryParse(parts[1], out Corporation corp))
                        {
                            if (_player.GetCorpRep(corp) < int.Parse(parts[2])) return false;
                        }
                        break;
                }
            }
            return true;
        }

        private float CalculateEventWeight(GameEvent eventData)
        {
            float weight = 1f;

            // Rarity
            weight *= eventData.Rarity switch
            {
                EventRarity.Common => 1f,
                EventRarity.Uncommon => 0.5f,
                EventRarity.Rare => 0.2f,
                EventRarity.Epic => 0.1f,
                EventRarity.Legendary => 0.05f,
                _ => 1f
            };

            // Stage relevance (higher weight for current stage)
            int stageDiff = Math.Abs((int)_player.CurrentStage - (int)eventData.MinStage);
            weight *= 1f / (1f + stageDiff * 0.5f);

            // Corp alignment
            if (eventData.FavoredCorporation != Corporation.None)
            {
                int rep = _player.GetCorpRep(eventData.FavoredCorporation);
                weight *= 1f + rep / 200f;
            }

            return weight;
        }

        private void TriggerEvent(GameEvent eventData)
        {
            var activeEvent = new ActiveEvent
            {
                EventData = eventData,
                TriggeredAt = DateTime.Now,
                TriggeredAge = _player.Age,
                State = EventState.Active,
                AvailableChoices = new List<EventChoice>(eventData.Choices)
            };

            // Filter choices by requirements
            activeEvent.AvailableChoices = activeEvent.AvailableChoices
                .Where(c => CheckChoiceRequirements(c))
                .ToList();

            if (activeEvent.AvailableChoices.Count == 0)
            {
                // No valid choices, auto-resolve with default
                AutoResolveEvent(activeEvent);
                return;
            }

            _activeEvents.Add(activeEvent);

            // Track history
            _eventHistory[eventData.Id] = _eventHistory.GetValueOrDefault(eventData.Id, 0) + 1;
            _eventHistory[eventData.Id + "_lastAge"] = _player.Age;

            OnEventTriggered?.Invoke(eventData);

            if (debugLogEvents)
            {
                Debug.Log($"[EventEngine] Triggered: {eventData.Title} (Stage {eventData.MinStage})");
            }
        }

        private bool CheckChoiceRequirements(EventChoice choice)
        {
            if (choice.Requirements == null || choice.Requirements.Count == 0) return true;

            foreach (var req in choice.Requirements)
            {
                var parts = req.Split(':');
                if (parts.Length < 2) continue;

                switch (parts[0])
                {
                    case "stat":
                        if (GetPlayerStat(parts[1]) < int.Parse(parts[2])) return false;
                        break;
                    case "resource":
                        if (!HasResource(parts[1], int.Parse(parts[2]))) return false;
                        break;
                    case "trait":
                        if (!_player.ActiveTraits.Contains(parts[1])) return false;
                        break;
                    case "corp_rep":
                        if (Enum.TryParse(parts[1], out Corporation corp))
                        {
                            if (_player.GetCorpRep(corp) < int.Parse(parts[2])) return false;
                        }
                        break;
                    case "daemon":
                        if (_player.ActiveDaemon == null || _player.ActiveDaemon.BaseProgram.Id != parts[1]) return false;
                        break;
                }
            }
            return true;
        }

        private int GetPlayerStat(string statName)
        {
            return statName.ToLower() switch
            {
                "might" => _player.CurrentStats.Might,
                "agility" => _player.CurrentStats.Agility,
                "intellect" => _player.CurrentStats.Intellect,
                "willpower" => _player.CurrentStats.Willpower,
                "integration" => _player.CurrentStats.Integration,
                "humanity" => _player.Humanity,
                _ => 0
            };
        }

        private bool HasResource(string resource, int amount)
        {
            return resource.ToLower() switch
            {
                "credits" => _player.Resources.Credits >= amount,
                "aether" => _player.Resources.Aether >= amount,
                "bandwidth" => _player.Resources.Bandwidth >= amount,
                "compute" => _player.Resources.Compute >= amount,
                "admintokens" => _player.Resources.AdminTokens >= amount,
                _ => false
            };
        }

        public void MakeChoice(string eventId, string choiceId)
        {
            var activeEvent = _activeEvents.FirstOrDefault(e => e.EventData.Id == eventId);
            if (activeEvent == null) return;

            var choice = activeEvent.AvailableChoices.FirstOrDefault(c => c.Id == choiceId);
            if (choice == null) return;

            // Apply immediate costs
            ApplyChoiceCosts(choice);

            // Resolve outcome
            var outcome = choice.Outcomes[UnityEngine.Random.Range(0, choice.Outcomes.Count)];
            ResolveOutcome(activeEvent, choice, outcome);

            OnChoiceMade?.Invoke(activeEvent.EventData, choice);
            OnEventResolved?.Invoke(activeEvent.EventData, outcome);

            _activeEvents.Remove(activeEvent);
        }

        private void ApplyChoiceCosts(EventChoice choice)
        {
            if (choice.Costs == null) return;

            foreach (var cost in choice.Costs)
            {
                var parts = cost.Split(':');
                if (parts.Length < 2) continue;

                switch (parts[0])
                {
                    case "credits":
                        _player.Resources.Credits -= int.Parse(parts[1]);
                        break;
                    case "aether":
                        _player.Resources.Aether -= int.Parse(parts[1]);
                        break;
                    case "bandwidth":
                        _player.Resources.Bandwidth -= int.Parse(parts[1]);
                        break;
                    case "compute":
                        _player.Resources.Compute -= int.Parse(parts[1]);
                        break;
                    case "humanity":
                        _player.Humanity = Mathf.Max(0, _player.Humanity - int.Parse(parts[1]));
                        break;
                    case "hp":
                        _player.CurrentHP = Mathf.Max(0, _player.CurrentHP - int.Parse(parts[1]));
                        break;
                }
            }
        }

        private void ResolveOutcome(ActiveEvent activeEvent, EventChoice choice, EventOutcome outcome)
        {
            // Apply rewards
            if (outcome.Rewards != null)
            {
                foreach (var reward in outcome.Rewards)
                {
                    ApplyReward(reward);
                }
            }

            // Apply penalties
            if (outcome.Penalties != null)
            {
                foreach (var penalty in outcome.Penalties)
                {
                    ApplyPenalty(penalty);
                }
            }

            // Apply stat changes
            if (outcome.StatChanges != null)
            {
                foreach (var statChange in outcome.StatChanges)
                {
                    ApplyStatChange(statChange);
                }
            }

            // Apply trait changes
            if (outcome.TraitChanges != null)
            {
                foreach (var traitChange in outcome.TraitChanges)
                {
                    if (traitChange.StartsWith("+"))
                        _player.ActiveTraits.Add(traitChange.Substring(1));
                    else if (traitChange.StartsWith("-"))
                        _player.ActiveTraits.Remove(traitChange.Substring(1));
                }
            }

            // Apply story flags
            if (outcome.StoryFlags != null)
            {
                foreach (var flag in outcome.StoryFlags)
                {
                    if (flag.StartsWith("+"))
                        _player.StoryFlags.Add(flag.Substring(1));
                    else if (flag.StartsWith("-"))
                        _player.StoryFlags.Remove(flag.Substring(1));
                }
            }

            // Corp rep changes
            if (outcome.CorpRepChanges != null)
            {
                foreach (var repChange in outcome.CorpRepChanges)
                {
                    var parts = repChange.Split(':');
                    if (parts.Length == 2 && Enum.TryParse(parts[0], out Corporation corp))
                    {
                        ArcanepunkEngine.Instance.ModifyCorpRep(corp, int.Parse(parts[1]));
                    }
                }
            }

            // Trigger follow-up events
            if (outcome.TriggerEvents != null)
            {
                foreach (var triggerEventId in outcome.TriggerEvents)
                {
                    var followUp = _allEvents.FirstOrDefault(e => e.Id == triggerEventId);
                    if (followUp != null)
                    {
                        TriggerEvent(followUp);
                    }
                }
            }
        }

        private void ApplyReward(string reward)
        {
            var parts = reward.Split(':');
            if (parts.Length < 2) return;

            switch (parts[0])
            {
                case "credits":
                    _player.Resources.Credits += int.Parse(parts[1]);
                    break;
                case "aether":
                    _player.Resources.Aether += int.Parse(parts[1]);
                    break;
                case "bandwidth":
                    _player.Resources.Bandwidth += int.Parse(parts[1]);
                    break;
                case "compute":
                    _player.Resources.Compute += int.Parse(parts[1]);
                    break;
                case "admintokens":
                    _player.Resources.AdminTokens += int.Parse(parts[1]);
                    break;
                case "circuit":
                    // Grant circuit knowledge
                    _player.KnownCircuits.Add(parts[1]);
                    break;
                case "module":
                    // Grant module blueprint
                    break;
                case "daemon":
                    // Grant daemon
                    DaemonManagementSystem.Instance.AcquireDaemon(
                        ArcanepunkDataLoader.GetDaemon(parts[1]));
                    break;
            }
        }

        private void ApplyPenalty(string penalty)
        {
            var parts = penalty.Split(':');
            if (parts.Length < 2) return;

            switch (parts[0])
            {
                case "credits":
                    _player.Resources.Credits = Math.Max(0, _player.Resources.Credits - int.Parse(parts[1]));
                    break;
                case "aether":
                    _player.Resources.Aether = Math.Max(0, _player.Resources.Aether - int.Parse(parts[1]));
                    break;
                case "humanity":
                    _player.Humanity = Math.Max(0, _player.Humanity - int.Parse(parts[1]));
                    break;
                case "hp":
                    _player.CurrentHP = Math.Max(0, _player.CurrentHP - int.Parse(parts[1]));
                    break;
                case "corp_rep":
                    if (Enum.TryParse(parts[1], out Corporation corp))
                    {
                        ArcanepunkEngine.Instance.ModifyCorpRep(corp, -int.Parse(parts[2]));
                    }
                    break;
            }
        }

        private void ApplyStatChange(string statChange)
        {
            var parts = statChange.Split(':');
            if (parts.Length < 2) return;

            string stat = parts[0];
            int amount = int.Parse(parts[1]);

            switch (stat.ToLower())
            {
                case "might": _player.CurrentStats.Might += amount; break;
                case "agility": _player.CurrentStats.Agility += amount; break;
                case "intellect": _player.CurrentStats.Intellect += amount; break;
                case "willpower": _player.CurrentStats.Willpower += amount; break;
                case "integration": _player.CurrentStats.Integration += amount; break;
            }
        }

        private void AutoResolveEvent(ActiveEvent activeEvent)
        {
            // Pick random outcome from first choice
            var choice = activeEvent.EventData.Choices.FirstOrDefault();
            if (choice != null && choice.Outcomes.Count > 0)
            {
                var outcome = choice.Outcomes[0];
                ResolveOutcome(activeEvent, choice, outcome);
                OnEventResolved?.Invoke(activeEvent.EventData, outcome);
            }

            _activeEvents.Remove(activeEvent);
        }

        private void UpdateActiveEvents()
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
            {
                var evt = _activeEvents[i];
                // Check for timeouts
                if (evt.EventData.DurationYears > 0)
                {
                    if (_player.Age - evt.TriggeredAge >= evt.EventData.DurationYears)
                    {
                        // Timeout - auto resolve with worst outcome
                        AutoResolveEvent(evt);
                    }
                }
            }
        }

        // ==================== FORCED EVENTS ====================
        public void ForceTriggerEvent(string eventId)
        {
            var eventData = _allEvents.FirstOrDefault(e => e.Id == eventId);
            if (eventData != null)
            {
                TriggerEvent(eventData);
            }
        }

        public ActiveEvent[] GetActiveEvents()
        {
            return _activeEvents.ToArray();
        }

        // ==================== DATA STRUCTURES ====================
        public enum EventState { Active, Resolved, Expired, Failed }
        public enum EventRarity { Common, Uncommon, Rare, Epic, Legendary }

        [Serializable]
        public class GameEvent
        {
            public string Id;
            public string Title;
            public string Description;
            public RealmStage MinStage;
            public float TriggerChance;
            public int MaxTriggers;
            public int CooldownYears;
            public int DurationYears;
            public EventRarity Rarity;
            public Corporation FavoredCorporation;
            public List<string> Prerequisites;
            public Dictionary<Corporation, int> RequiredCorpRep;
            public string[] RequiredTraits;
            public Dictionary<string, int> RequiredStats;
            public EventChoice[] Choices;
        }

        [Serializable]
        public class ActiveEvent
        {
            public GameEvent EventData;
            public DateTime TriggeredAt;
            public int TriggeredAge;
            public EventState State;
            public List<EventChoice> AvailableChoices;
        }

        [Serializable]
        public class EventChoice
        {
            public string Id;
            public string Text;
            public string Description;
            public List<string> Requirements;
            public string[] Costs;
            public EventOutcome[] Outcomes;
        }

        [Serializable]
        public class EventOutcome
        {
            public string Id;
            public string Description;
            public float Weight;
            public string[] Rewards;
            public string[] Penalties;
            public string[] StatChanges;
            public string[] TraitChanges;
            public string[] StoryFlags;
            public string[] CorpRepChanges;
            public string[] TriggerEvents;
        }
    }
}