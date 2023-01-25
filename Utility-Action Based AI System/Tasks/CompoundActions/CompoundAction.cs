using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public abstract class CompoundAction : GenericAiAction
    {
        public GenericAiAction[] Sequence; // Can Contain either Compound Actions and GenericAiActions wich are primitive actions

        [ShowInInspector] protected int SequenceIndex { get; set; }

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);

            for (int i = 0; i < Sequence.Length; ++i)
            {
                Sequence[i] = Instantiate(Sequence[i], this.transform);
                Sequence[i].Initialize(brain, context);
                Sequence[i].EnableAction();
            }
        }

        public override void DisableAction()
        {
            base.DisableAction();

            for (int i = 0; i < Sequence.Length; ++i)
            {
                Sequence[i].DisableAction();                
            }
        }

        public override void Execute()
        {
            SequenceIndex = 0;
            StartActionAtSequenceIndex();
        }

        public override int ComputePriority()
        {
            int priority = -1;
            for (int i = 0; i < Sequence.Length; ++i)
            {
                Sequence[i].CurrentPriority = Sequence[i].ComputePriority();
                if (Sequence[i].CurrentPriority == 0)
                {
                    return 0;
                }
                else
                {
                    priority += Sequence[i].CurrentPriority;
                }
            }
            return priority;
        }

        public virtual void OnSequenceActionFinished()
        {
            // On incrémente l'index dans le séquence lorsqu'une action se termine 
            SequenceIndex++;

            // Si l'index dépasse la liste, on a terminé la séquence
            if (SequenceIndex >= Sequence.Length)
            {
                EndAction();
                return;
            }

            StartActionAtSequenceIndex();
        }

        protected void StartActionAtSequenceIndex()
        {
            if (Sequence[SequenceIndex].CheckPreconditions())
            {
                brain.StartAction(Sequence[SequenceIndex], Sequence[SequenceIndex].MatchInteruptionCondition, OnSequenceActionFinished);
            }
            else
            {
                brain.InterruptCurrentAction();
            }
        }

        public override bool MatchInteruptionCondition()
        {
            if (SequenceIndex >= Sequence.Length)
            {
                Debug.LogError("??");
                return true;
            }

            if (Sequence[SequenceIndex].CheckInterruptionCondition != null)
            {
                return Sequence[SequenceIndex].CheckInterruptionCondition.Invoke();
            }

            return false;
        }

        // 
        public override void OnInterrupt()
        {
            if (SequenceIndex < Sequence.Length)
            {
                if (Sequence[SequenceIndex].ActionState != ActionState.Interrupted)
                    Sequence[SequenceIndex].OnInterrupt();
                else
                    Debug.LogError("Already Interrupted");
            }

            base.OnInterrupt();
        }
    }
}
