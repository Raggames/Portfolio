using Assets.SteamAndMagic.Scripts.Managers;
using Sirenix.OdinInspector;
using SteamAndMagic.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public enum DecisionMode
    {
        Best,
        WeightedRandom,
    }

    public class Brain : AbstractBrain<AiAgent>
    {
        public AiAgentEntity AiAgentEntity => BrainOwner.Context.AgentEntity;
        public WorkingMemory WorkingMemory = new WorkingMemory();

        public DecisionMode DecisionMode;
        [Header("---- RUNTIME VARIABLES ----")]
        public bool IsFighting;
        public bool CanSearchTarget = true;

        [Header("Actions")]
        public bool IsRunningAction;
        [ReadOnly] public string CurrentActionDebug;
        public GenericAiAction CurrentAction;

        [HorizontalGroup("A")] public List<GenericAiAction> GenericAiActions = new List<GenericAiAction>();
        [HorizontalGroup("A"), ShowInInspector, ReadOnly] private List<float> tempActionsPriorities = new List<float>();

        public virtual void Initialize(IBrainOwner<AiAgent> context)
        {
            this.BrainOwner = context;

            GameObject brainContainer = new GameObject("Brain Container");
            brainContainer.transform.SetParent(AiAgentEntity.transform);

            for (int i = 0; i < GenericAiActions.Count; ++i)
            {
                GenericAiActions[i] = Instantiate(GenericAiActions[i], brainContainer.transform);
                GenericAiActions[i].Initialize(this, AiAgentEntity);
            }

            for (int i = 0; i < GenericAiActions.Count; ++i)
            {
                GenericAiActions[i].OnEndInitialize();
            }
        }

        public void UpdateBrainLogic()
        {
            if (CurrentAction != null)
            {
                if (CurrentAction.MatchInteruptionCondition())
                {
                    //Debug.LogError("Action " + CurrentAction + " match interruption conditions.");
                    if (CurrentAction.CustomOnInterruptedAction != null)
                    {
                        CurrentAction.CustomOnInterruptedAction.Invoke();
                        CurrentAction.CustomOnInterruptedAction = null;
                    }
                    else
                    {
                        InterruptCurrentAction();
                    }
                }
            }

            if (CurrentAction == null || CurrentAction.IsOverridable)
            {
                int bestActionPriority = -1;
                int choosenActionIndex = -1;
                int totalPriority = 0;

                for (int i = 0; i < GenericAiActions.Count; ++i)
                {
                    if (GenericAiActions[i].CheckCooldown() && GenericAiActions[i].CheckCharges())
                    {
                        GenericAiActions[i].CurrentPriority = GenericAiActions[i].ComputePriority();
                    }
                    else
                    {
                        GenericAiActions[i].CurrentPriority = 0;
                    }

                    totalPriority += GenericAiActions[i].CurrentPriority;

                    if (GenericAiActions[i].CurrentPriority > bestActionPriority)
                    {
                        bestActionPriority = GenericAiActions[i].CurrentPriority;
                        choosenActionIndex = i;
                    }
                }

                if (DecisionMode == DecisionMode.WeightedRandom)
                {
                    tempActionsPriorities.Clear();
                    GenericAiActions.Sort((a, b) => b.CurrentPriority.CompareTo(a.CurrentPriority));
                    for (int i = 0; i < GenericAiActions.Count; ++i)
                    {
                        if (GenericAiActions[i].CurrentPriority > 0)
                            tempActionsPriorities.Add(GenericAiActions[i].CurrentPriority);
                    }

                    // On choisit l'index parmis les priorités supérieures à 0
                    choosenActionIndex = Randomizer.Index(tempActionsPriorities);
                }

                if (choosenActionIndex != -1 && GenericAiActions[choosenActionIndex].CurrentPriority > 0)//&& GoalTasks[taskIndex].CurrentActionCompute != null)
                {
                    // Gestion randomisation des choix si option selectionnée

                    if (CurrentAction == null)
                    {
                        StartAction(GenericAiActions[choosenActionIndex], GenericAiActions[choosenActionIndex].MatchInteruptionCondition);
                    }
                    else if (bestActionPriority > Mathf.Max(CurrentAction.CurrentPriority, CurrentAction.OverridePriorityThreshold))
                    {
                        InterruptCurrentAction();
                        StartAction(GenericAiActions[choosenActionIndex], GenericAiActions[choosenActionIndex].MatchInteruptionCondition);
                    }
                }
            }
        }

        public void StartAction(GenericAiAction aiAction, GenericAiAction.InterruptionConditionHandler interruptionHandler, Action onCustomEndedAction = null, Action onCustomInterruptedAction = null)
        {
            IsRunningAction = true;

            CurrentActionDebug = aiAction.GetType().ToString();
            //Debug.Log("Starting " + CurrentActionDebug);

            CurrentAction = aiAction;
            CurrentAction.ActionState = ActionState.Executing;
            CurrentAction.CheckInterruptionCondition = interruptionHandler;

            if (onCustomEndedAction != null)
            {
                CurrentAction.CustomOnEndedAction = onCustomEndedAction;
            }

            if (onCustomInterruptedAction != null)
            {
                CurrentAction.CustomOnInterruptedAction = onCustomInterruptedAction;
            }

            if (aiAction.MaxCooldown > 0)
                aiAction.SetCooldown();

            if (aiAction.MaxCharges > 0)
                aiAction.UseCharge();

            aiAction.Execute();
        }

        public void StopAction(GenericAiAction toStop)
        {
            toStop.ActionState = ActionState.Idle;
            IsRunningAction = false;
            CurrentAction = null;
            CurrentActionDebug = "None";
        }

        public void InterruptCurrentAction()
        {
            if (CurrentAction != null)
            {
                //Debug.Log("Interrupting " + CurrentActionDebug);

                CurrentAction.ActionState = ActionState.Interrupted;
                CurrentAction.OnInterrupt();
                StopAction(CurrentAction); // interrupt
            }
            else
            {
                Debug.LogError("Cannot interrupt this action because it is not currently executing");
            }
        }
    }
}
