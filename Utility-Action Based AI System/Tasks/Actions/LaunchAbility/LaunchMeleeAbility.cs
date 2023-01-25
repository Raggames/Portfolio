using System.Collections.Generic;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class LaunchMeleeAbility : LaunchAbilityAction
    {
        public bool UseEffectRange = false;
        public float NotFocusMultiplier = .7f; // si la cible est différente du focus, moins de chances de la selectionner
        public float NotInSightMultiplier = 1f;

        public Utility EnemiesLifeUtility;
        public Utility EnemiesDistanceUtility;
        public Utility CurrentCooldownUtility;

        private List<Utility> utilities;
        private List<Utility> distanceAndLifeUtilities;

        private List<float> computedUtilitiesTemp = new List<float>();
        private List<AiTarget> inRangeTargets = new List<AiTarget>();
        private List<AiTarget> outRangeTargets = new List<AiTarget>();

        public override bool IsOutOfAnyAiTargetRange
        {
            get
            {
                return inRangeTargets.Count == 0;
            }
        }

        public override int OutOfRangeTargetsCount
        {
            get
            {
                return outRangeTargets.Count;
            }
        }

        private void Awake()
        {
            utilities = new List<Utility>() { EnemiesLifeUtility, EnemiesDistanceUtility, CurrentCooldownUtility };
            distanceAndLifeUtilities = new List<Utility>() { EnemiesLifeUtility, EnemiesDistanceUtility };
        }

        public override int ComputePriority()
        {
            CurrentAiTarget = null;

            if (!Ability.IsLaunchable())
                return 0;

            inRangeTargets.Clear();
            outRangeTargets.Clear();

            for (int i = 0; i < aiAgentEntity.targetHandlingSubsystem.AiTargets.Count; ++i)
            {
                if (Ability.IsPositionInRange(aiAgentEntity.targetHandlingSubsystem.AiTargets[i].Entity.transform.position, UseEffectRange ? Ability.EffectRange : Ability.Range))
                {
                    inRangeTargets.Add(aiAgentEntity.targetHandlingSubsystem.AiTargets[i]);
                }
                else
                {
                    outRangeTargets.Add(aiAgentEntity.targetHandlingSubsystem.AiTargets[i]);
                }
            }

            // gestion de swap de focus ? Coefficienter le fait d'outrepasser le focus choisi par SetFocusAction en fonction de la menace ?
            if (inRangeTargets.Count > 0)
            {
                computedUtilitiesTemp.Clear();

                for (int i = 0; i < inRangeTargets.Count; ++i)
                {                   
                    EnemiesLifeUtility.ComputeResult(inRangeTargets[i].Entity.CurrentLifePurcent, 100f);
                    EnemiesDistanceUtility.ComputeResult(Vector3.Distance(inRangeTargets[i].Entity.transform.position, aiAgentEntity.transform.position), Mathf.Min(Ability.Range, aiAgentEntity.targetHandlingSubsystem.VisionRange));
                    float utility = OptionEvaluator.GetAverage(distanceAndLifeUtilities);

                    if(aiAgentEntity.targetHandlingSubsystem.HasFocus && aiAgentEntity.targetHandlingSubsystem.Focus != inRangeTargets[i])
                    {
                        utility *= NotFocusMultiplier;
                    }

                    if (!inRangeTargets[i].InSight)
                    {
                        utility *= NotInSightMultiplier;
                    }

                    computedUtilitiesTemp.Add(utility);
                }

                int choosenIndex = Randomizer.Index(computedUtilitiesTemp);
                CurrentAiTarget = inRangeTargets[choosenIndex];

                EnemiesLifeUtility.ComputeResult(inRangeTargets[choosenIndex].Entity.CurrentLifePurcent, 100f); 
                EnemiesDistanceUtility.ComputeResult(Vector3.Distance(inRangeTargets[choosenIndex].Entity.transform.position, aiAgentEntity.transform.position), Mathf.Min(Ability.Range, aiAgentEntity.targetHandlingSubsystem.VisionRange));
                CurrentCooldownUtility.ComputeResult(Ability.CurrentCooldown, Ability.CurrentCooldownTime);

                return Mathf.RoundToInt(OptionEvaluator.GetAverage(utilities) * 100f);
            }
            else
            {
                return 0;

/*
                computedUtilitiesTemp.Clear();
                for (int i = 0; i < outRangeTargets.Count; ++i)
                {
                    EnemiesLifeUtility.ComputeResult(outRangeTargets[i].Entity.CurrentLifePurcent, 100f);
                    EnemiesDistanceUtility.ComputeResult(Vector3.Distance(outRangeTargets[i].Entity.transform.position, aiAgentEntity.transform.position), Mathf.Min(Ability.Range, aiAgentEntity.targetHandlingSubsystem.VisionRange));

                    float utility = OptionEvaluator.GetAverage(distanceAndLifeUtilities);
                    if (aiAgentEntity.targetHandlingSubsystem.HasFocus && aiAgentEntity.targetHandlingSubsystem.Focus != outRangeTargets[i])
                    {
                        utility *= NotFocusMultiplier;
                    }
                    computedUtilitiesTemp.Add(utility);
                }

                int choosenIndex = Randomizer.Index(computedUtilitiesTemp);

                if (choosenIndex < 0)
                    choosenIndex = 0;

                CurrentAiTarget = outRangeTargets[choosenIndex];

                EnemiesLifeUtility.ComputeResult(outRangeTargets[choosenIndex].Entity.CurrentLifePurcent, 100f);
                EnemiesDistanceUtility.ComputeResult(Vector3.Distance(outRangeTargets[choosenIndex].Entity.transform.position, aiAgentEntity.transform.position), Mathf.Min(Ability.Range, aiAgentEntity.targetHandlingSubsystem.VisionRange));
                CurrentCooldownUtility.ComputeResult(Ability.CurrentCooldown, Ability.CurrentCooldownTime);

                return Mathf.RoundToInt(OptionEvaluator.GetAverage(utilities) * 100f * OutRangeMultiplier);*/
            }
        }
    }
}
