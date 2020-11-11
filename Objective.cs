using System;
using System.Collections.Generic;
using Attributes;
using Core.Characters;
using Core.Events.Game;
using Core.Inventory.Items;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

namespace Core.Quest
{
    /// <summary>
    /// Container with common information and methods that every objective has
    /// </summary>
    /// <inheritdoc cref="ISerializationCallbackReceiver"/>
    [Serializable]
    public abstract class BaseObjective
    {
        /// <summary>
        /// Description of the Objective
        /// </summary>
        [Header("General Information")]
        public string Description;
        /// <summary>
        /// Checker that represents if the Objective is Completed
        /// </summary>
        public bool Completed;
        /// <summary>
        /// Checker that represents if the Objective is Failed
        /// </summary>
        public bool Failed;
        /// <summary>
        /// Tracker of Current Amount of Killed NPCs / Collected Items
        /// ** SHOULD BE RELOCATED TO A SEPARATE CLASS FOR KillObjective and ItemCollectiveObjective **
        /// </summary>
        public int CurrentAmount;
        /// <summary>
        /// Tracker of Required Amount of Killed NPCs / Collected Items
        /// ** SHOULD BE RELOCATED TO A SEPARATE CLASS FOR KillObjective and ItemCollectiveObjective **
        /// </summary>
        public int RequiredAmount;
        /// <summary>
        /// Indicator that represents if this objective has a time limit
        /// </summary>
        public bool HasTimer;
        /// <summary>
        /// Time that is given to complete this objective
        /// </summary>
        [ConditionalHide(nameof(HasTimer))] public float Timer;
        /// <summary>
        /// Reference to the UI <see cref="TextMeshProUGUI"/> Object that contains Time left
        /// </summary>
        [ConditionalHide(nameof(HasTimer))] public TextMeshProUGUI TimerText;
        
        [Header("Objective Order")]
        public int ObjectiveOrder;
        public bool IsOptional;
        public bool ShowObjective = false;
        
        /// <summary>
        /// Assigns a <see cref="TimerText"/> variable to the appropriate component
        /// </summary>
        private void AssignTextUI()
        {
            if (!HasTimer) return;
            TimerText = GameObject.Find("GameManager").GetComponent<QuestManager>().uiQuestTimerText;
        }
    }
    
    /// <summary>
    /// Container with all the possible objectives for one quest and set of indicators of objectives completion status
    /// </summary>
    /// <seealso cref="KillObjective"/>
    /// <seealso cref="ItemCollectObjective"/>
    /// <seealso cref="EscortObjective"/>
    [Serializable]
    public class Objective
    {
        /// <summary>
        /// Flag that indicates if the objectives order in the quest matters
        /// </summary>
        public bool objectiveOrderMatters;
        /// <summary>
        /// List of all <see cref="KillObjective"/> for the Quest
        /// </summary>
        public List<KillObjective> killObjectives = new List<KillObjective>();
        /// <summary>
        /// List of all <see cref="ItemCollectObjective"/> for the Quest
        /// </summary>
        public List<ItemCollectObjective> itemCollectObjectives = new List<ItemCollectObjective>();
        /// <summary>
        /// List of all <see cref="EscortObjective"/> for the Quest
        /// </summary>
        public List<EscortObjective> escortObjectives = new List<EscortObjective>();
        /// <summary>
        /// List of all <see cref="ExploreObjective"/> fpr the Quest
        /// </summary>
        [Separator("Explore Objectives")]
        public List<ExploreObjective> exploreObjectives = new List<ExploreObjective>();
        /// <summary>
        /// Indicator that specifies if we need tos how the debug information to the Editor
        /// </summary>
        [Header("Objective Information")]
        [HideInInspector] public bool showDebugInfo;
        /// <summary>
        /// Flag that shows if all objectives for the quest are completed
        /// </summary>
#if UNITY_EDITOR
        [ConditionalHide(nameof(showDebugInfo), false)]
#endif
        public bool objectivesCompleted;
        /// <summary>
        /// Flag that shows if any objective is failed
        /// </summary>
#if UNITY_EDITOR
        [ConditionalHide(nameof(showDebugInfo), false)]
#endif
        public bool objectivesFailed;
    }

    [Serializable]
    public class EscortObjective : BaseObjective, ISerializationCallbackReceiver
    {

        /// <summary>
        /// A object that is the health bar of the npc
        /// </summary>
        public GameObject escortHealthBar;

        /// <summary>
        /// Reference to the <see cref="NpcCharacterData"/> of the NPC that should be escorted
        [Header("Objective Specific")]
        public NpcCharacter npc;
        
        // /// <summary>
        // /// Location where NPC should be escorted to
        // /// </summary>
        public Collision escortPointCollision;
        /// <summary>
        /// Reference to the <see cref="Collision"/> object of the NPC
        /// </summary>
        public Collision npcRb;
        private Objective _objective;

        /// <summary>
        /// Constructor that initializes all EscortObjective variables
        /// </summary>
        /// <param name="npc">Reference to the <see cref="NpcCharacterData"/> of the NPC that should be escorted</param>
        /// <param name="description">Description of the Objective</param>
        /// <param name="failed">Checker if the Objective is Failed</param>
        /// <param name="completed">Checker if the Objective is Completed</param>
        /// <param name="npcRb">Reference to the <see cref="Collision"/> object of the NPC</param>
        /// <param name="escortPointCollision">Location where NPC should be escorted to</param>
        /// <param name="hasTimer">Defines if the Objective has a time limit</param>
        /// <param name="timer">Time that is given to complete the Objective</param>
        public EscortObjective(NpcCharacter npc, string description, bool failed, bool completed, Collision npcRb, Collision escortPointCollision, bool hasTimer = false, float timer = 1f)
        {
            this.npc = npc;
            Failed = failed;
            HasTimer = hasTimer;
            Timer = timer;
            Description = description;
            Completed = completed;
            this.npcRb = npcRb;
            this.escortPointCollision = escortPointCollision;
        }

        /// <summary>
        /// Performs all checks whether NPC is escorted and updates appropriate variables
        /// </summary>
        public void NpcEscorted()
        {
            NpcCharacter.OnTookDamage -= UpdateEscortHealthBars;
            NpcCharacter.OnTookDamage += UpdateEscortHealthBars;
            
            Failed = npc.IsDead;
            
            // Do collision check
        }

        /// <summary>
        /// Updates the health bar UIs for every escort objective
        /// </summary>
        public void UpdateEscortHealthBars()
        {
            escortHealthBar.SetActive(true);
                
            var filledHbObject = escortHealthBar.transform.GetChild(0).GetChild(0);
            filledHbObject.localScale = new Vector3(npc.HealthPoints / npc.InitHp, filledHbObject.localScale.y, filledHbObject.localScale.z);

            if (npc.HealthPoints <= 0 || !ShowObjective) {escortHealthBar.SetActive(false);}
        }
        
        /// <summary>
        /// Decrements timer left on the Timer if HasTimer is set to true and updates Failed flag on the outcome
        /// </summary>
        /// <returns>If the timer ran out</returns>
        public bool TimerCheck()
        {
            if (!HasTimer) return false;
            return HasTimer && Timer - Mathf.Epsilon <= 0f ? Failed = true : Failed = false;
        }
        
        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="TimerCheck"/>
        /// </summary>
        public void OnBeforeSerialize()
        {
            TimerCheck();
        }
        
        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="TimerCheck"/>
        /// </summary>
        public void OnAfterDeserialize()
        {
            TimerCheck();
        }
    }
    
    /// <summary>
    /// Objective that tracks the information about NPC that should be killed
    /// </summary>
    /// <inheritdoc cref="BaseObjective"/>
    /// <inheritdoc cref="ISerializationCallbackReceiver"/>
    /// <seealso cref="NpcCharacterData"/>
    [Serializable]
    public class KillObjective : BaseObjective, ISerializationCallbackReceiver
    {        
        /// <summary>
        /// Reference to the <see cref="NpcCharacterData"/> of an NPC that should be killed
        /// </summary>
        [Header("Objective Specific")]
        public NpcCharacterData npc;
        
        /// <summary>
        /// Constructor that initializes all variables of the KillObjective
        /// </summary>
        /// <param name="npc">Reference to the <see cref="NpcCharacterData"/> of an NPC that should be killed</param>
        /// <param name="description">Description of the Objective</param>
        /// <param name="currentAmount">Current Amount of NPCs that are killed</param>
        /// <param name="requiredAmount">Required Amount of NPCs to be killed</param>
        /// <param name="completed">Checker if the Objective is Completed</param>
        /// <param name="hasTimer">Defines if the Objective has a time limit</param>
        /// <param name="timer">Time that is given to complete the Objective</param>
        public KillObjective(NpcCharacterData npc, string description, bool completed, int currentAmount, int requiredAmount, bool hasTimer = false, float timer = 1f)
        {
            this.npc = npc;
            Description = description;
            HasTimer = hasTimer;
            Timer = timer;
            Completed = completed;
            CurrentAmount = currentAmount;
            RequiredAmount = requiredAmount;
        }

        /// <summary>
        /// Ensure the OnjectiveCompleted is called only once
        /// </summary>
        private bool isCalledIOOC;
        /// <summary>
        /// Performs all the necessary checks about the status of the NPC that should be killed and updates appropriate variables
        /// </summary>
        public void NpcDied()
        {
            if (!ShowObjective)
            {
                NpcCharacter.npcDeath -= IncrementCurrentAmount;
                return;
            }
            
            if (CurrentAmount < RequiredAmount)
            {
                Completed = false;
                isCalledIOOC = false;
                
                // Unsubbing to the event before subbing, prevent IncrementCurrentAmount being called hundred of times
                NpcCharacter.npcDeath -= IncrementCurrentAmount;
                NpcCharacter.npcDeath += IncrementCurrentAmount; // Subscribe to the event
                return;
            }
            
            Completed = true;
            if (!isCalledIOOC)
            {

                GameEvents.Instance.InvokeOnObjectiveCompleted();
                isCalledIOOC = true;
            }

            NpcCharacter.npcDeath -= IncrementCurrentAmount; // Unsubscribe to the event
        }

        /// <summary>
        /// Increments Current Amount of Required Kills
        /// </summary>
        private void IncrementCurrentAmount(NpcCharacter npcCharacter)
        {
            if (npcCharacter.GetNpcData() == npc)
            {
                CurrentAmount++;
            }
        }
        
        /// <summary>
        /// Decrements timer left on the Timer if HasTimer is set to true and updates Failed flag on the outcome
        /// </summary>
        /// <returns>If the timer ran out</returns>
        private bool TimerCheck()
        {
            if (!HasTimer) return false;
            
            return HasTimer && Timer - Mathf.Epsilon <= 0f ? Failed = true : Failed = false;
        }

        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="TimerCheck"/>
        /// </summary>
        public void OnBeforeSerialize()
        {
            TimerCheck();
        }
        
        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="TimerCheck"/>
        /// </summary>
        public void OnAfterDeserialize()
        {
            TimerCheck();
        }
    }
    
    /// <summary>
    /// Objective that keeps track of an Item that Player should collect
    /// </summary>
    /// <inheritdoc cref="BaseObjective"/>
    /// <inheritdoc cref="ISerializationCallbackReceiver"/>
    /// <seealso cref="ItemObject"/>
    /// <seealso cref="QuestObject"/>    
    [Serializable]
    public class ItemCollectObjective : BaseObjective, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Item that Player should collect
        /// </summary>
        /// <seealso cref="ItemObject"/>
        public ItemObject item;

        /// <summary>
        /// Reference to the <see cref="QuestObject"/> that his Objective is attached to
        /// </summary>
        [HideInInspector] public QuestObject quest;
        
        /// <summary>
        /// Constructor that initializes all the ItemCollectObjective variables
        /// </summary>
        /// <param name="item">Item that Player should collect</param>
        /// <param name="description">Description of the Objective</param>
        /// <param name="completed">Checker if the Objective is Completed</param>
        /// <param name="currentAmount">Current Amount of Items that Player should collect</param>
        /// <param name="requiredAmount">Required Amount of Items that Player should collect</param>
        /// <param name="hasTimer">Defines if the Objective has a time limit</param>
        /// <param name="timer">Time that is given to complete the Objective</param>
        public ItemCollectObjective(ItemObject item, string description, bool completed, int currentAmount, int requiredAmount, bool hasTimer = false, float timer = 1f)
        {
            this.item = item;
            Description = description;
            HasTimer = hasTimer;
            Timer = timer;
            Completed = completed;
            CurrentAmount = currentAmount;
            RequiredAmount = requiredAmount;
        }
        
        /// <summary>
        /// Performs all the checks if the Item is collected in required amounts and updates appropriate variables
        /// </summary>
        public void ItemCollected()
        {
            if (!ShowObjective) return;
        
            GameEvents.Instance.printIn("working");
            
            if (CurrentAmount < RequiredAmount)
            {
                CurrentAmount = quest.databases.playerInventory.ItemAmountOfItem(item);
                return;
            }

            CurrentAmount = RequiredAmount;
            Completed = true;
            
            GameEvents.Instance.InvokeOnObjectiveCompleted();

        }
        
        /// <summary>
        /// Decrements timer left on the Timer if HasTimer is set to true and updates Failed flag on the outcome
        /// </summary>
        /// <returns>If the timer ran out</returns>
        private bool TimerCheck()
        {
            if (!HasTimer) return false;
            return HasTimer && Timer - Mathf.Epsilon <= 0f ? Failed = true : Failed = false;
        }
        
        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="ItemCollected"/>
        /// </summary>
        /// <seealso cref="TimerCheck"/>
        public void OnBeforeSerialize()
        {
            TimerCheck();
        }

        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="ItemCollected"/>
        /// </summary>
        /// <seealso cref="TimerCheck"/>
        public void OnAfterDeserialize()
        {
            TimerCheck();
        }
    }

    /// <summary>
    /// Objective that keeps track of the location that player should explore
    /// </summary>
    /// <inheritdoc cref="BaseObjective"/>
    /// <inheritdoc cref="ISerializationCallbackReceiver"/>
    /// <seealso cref="Collider"/>
    [Serializable]
    public class ExploreObjective : BaseObjective, ISerializationCallbackReceiver
    {
        /// <summary>
        /// <see cref="Collider"/> reference to the Location that Player should explore
        /// </summary>
        public Collider Location;
        
        /// <summary>
        /// Constructor that initializes all the variables of the ExploreObjective
        /// </summary>
        /// <param name="location"><see cref="Collider"/> reference to the Location that Player should explore</param>
        /// <param name="description">Description of the Objective</param>
        /// <param name="completed">Checker if the Objective is Completed</param>
        /// <param name="hasTimer">Defines if the Objective has a time limit</param>
        /// <param name="timer">Time that is given to complete the Objective</param>
        /// <param name="showObjective">Defines if the objective should be shown</param>
        public ExploreObjective(Collider location, string description, bool completed, bool hasTimer = false, float timer = 1f, bool showObjective = false)
        {
            Location = location;
            Description = description;
            Completed = completed;
            HasTimer = hasTimer;
            Timer = timer;
            ShowObjective = showObjective;
        }
        
        public void OnArrivedDestination(Collider collider)
        {
            if (collider != Location || !ShowObjective) return;
            
            Completed = true;
            GameEvents.Instance.InvokeOnObjectiveCompleted();
        }
        
        
        /// <summary>
        /// Decrements timer left on the Timer if HasTimer is set to true and updates Failed flag on the outcome
        /// </summary>
        /// <returns>If the timer ran out</returns>
        private bool TimerCheck()
        {
            if (!HasTimer) return false;
            return HasTimer && Timer - Mathf.Epsilon <= 0f ? Failed = true : Failed = false;
        }
        
        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="TimerCheck"/>
        /// </summary>
        public void OnBeforeSerialize()
        {
            TimerCheck();
        }
        
        /// <summary>
        /// Required by Unity <see cref="ISerializationCallbackReceiver"/>
        /// Calls the <see cref="TimerCheck"/>
        /// </summary>
        public void OnAfterDeserialize()
        {
            TimerCheck();
        }
    }
}