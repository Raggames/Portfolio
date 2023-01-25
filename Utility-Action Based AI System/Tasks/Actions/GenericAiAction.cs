using Sirenix.OdinInspector;
using System;
using Unity.Collections;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public enum ActionState
    {
        Idle,
        Executing,
        Interrupted,
    }

    public abstract class GenericAiAction : MonoBehaviour
    {
        [Sirenix.OdinInspector.ReadOnly] public ActionState ActionState;
        /// <summary>
        /// Base/Offset Priority
        /// </summary>
        [FoldoutGroup("---- BASE PARAMETERS ----")]  public int BasePriority = 0;
        [FoldoutGroup("---- BASE PARAMETERS ----")] public float Multiplier = 1;
        [FoldoutGroup("---- BASE PARAMETERS ----")] public bool IsOverridable = true;

        /// <summary>
        /// The priority needed from another action to override this action/compound
        /// </summary>
        [FoldoutGroup("---- BASE PARAMETERS ----")] [ShowIf("IsOverridable", true)] public int OverridePriorityThreshold = 0;

        [FoldoutGroup("---- COOLDOWN AND CHARGES ----")] public int CurrentCooldown = 0;
        [FoldoutGroup("---- COOLDOWN AND CHARGES ----")] public int MaxCooldown = 0;

        [FoldoutGroup("---- COOLDOWN AND CHARGES ----")] public int CurrentCharges = 0;
        [FoldoutGroup("---- COOLDOWN AND CHARGES ----")] public int MaxCharges = 0;
        [FoldoutGroup("---- COOLDOWN AND CHARGES ----")] public int ChargeReloadTime = 10;
        [FoldoutGroup("---- COOLDOWN AND CHARGES ----")] public int CurrentChargeReload = 0;

        [Sirenix.OdinInspector.ReadOnly] [ShowInInspector] private int _currentPriority;
        
        public int CurrentPriority
        {
            get
            {
                if (_currentPriority > 0)
                {
                    return Mathf.RoundToInt(_currentPriority * Multiplier) + BasePriority;
                }
                return 0;
            }
            set
            {
                _currentPriority = value;
            }
        }

        protected Brain brain { get; set; }
        protected AiAgentEntity aiAgentEntity { get; set; }

        /// <summary>
        /// Méthode de vérification des conditions d'interruption de l'action
        /// </summary>
        /// <returns></returns>
        public delegate bool InterruptionConditionHandler();
        public InterruptionConditionHandler CheckInterruptionCondition;
        /// <summary>
        /// Méthode déclenchée par le OnEndAction si != null. Sera consummé après utilisation.
        /// </summary>
        public Action CustomOnEndedAction;
        /// <summary>
        /// Méthode déclenchée par le InterruptAction si != null. Sera consummé après utilisation.
        /// </summary>
        public Action CustomOnInterruptedAction;


        public void SetCooldown() 
        {
            CurrentCooldown = MaxCooldown;
        }

        public void SetCooldown(int cooldown)
        {
            CurrentCooldown = cooldown;
        }

        public void UseCharge()
        {
            CurrentCharges--;
        }

        public bool CheckCooldown()
        {            
            if (CurrentCooldown == 0)
            {                
                return true;
            }
            else
            {
                CurrentCooldown--;
                return false;
            }
        }

        public bool CheckCharges()
        {
            if (MaxCharges == 0) // pas de logique de charge pour cette action
                return true;

            if(CurrentCharges < MaxCharges)
            {
                CurrentChargeReload++;

                if(CurrentChargeReload > ChargeReloadTime)
                {
                    CurrentChargeReload = 0;
                    CurrentCharges++;
                }

                if (CurrentCharges == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public virtual void Initialize(Brain brain, AiAgentEntity context)
        {
            this.brain = brain;
            this.aiAgentEntity = context;
        }

        /// <summary>
        /// Appelé après l'initialisation de toutes les actions
        /// </summary>
        public void OnEndInitialize()
        {

        }

        private void OnEnable()
        {
            EnableAction();
        }

        private void OnDisable()
        {
            DisableAction();
        }

        public virtual void EnableAction() { }
        public virtual void DisableAction() { }

        public abstract void Execute();

        public virtual bool CheckPreconditions() { return true; }

        /// <summary>
        /// Calculate the priority of using this action to be compared with other actions with higher cost taken first function
        /// </summary>
        /// <returns></returns>
        public virtual int ComputePriority()
        {
            return BasePriority;
        }

        public virtual bool MatchInteruptionCondition()
        {
            return false;
        }

        /// <summary>
        /// Méthode à appeler pour terminer une action
        /// </summary>
        protected virtual void EndAction()
        {
            if (CustomOnEndedAction != null)
            {
                CustomOnEndedAction.Invoke();
                CustomOnEndedAction = null;
            }
            else
            {
                brain.StopAction(this);
            }
        }

        public virtual void OnInterrupt()
        {

        }
    }
}
