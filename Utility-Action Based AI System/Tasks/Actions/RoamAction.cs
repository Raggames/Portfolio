
namespace SteamAndMagic.Systems.IA
{
    public class RoamAction : GenericAiAction
    {
        public int RoamPriority = 5;
        public override int ComputePriority()
        {
            CurrentPriority = 0;

            if (!aiAgentEntity.CanDoMovement)
                return 0;

            if (brain.IsFighting)
                return 0;

            if (!aiAgentEntity.roamingSubsystem.IsRoaming || !aiAgentEntity.IsMoving)
            {
                CurrentPriority = RoamPriority;
            }

            return CurrentPriority;
        }

        public override void Execute()
        {
            aiAgentEntity.roamingSubsystem.StartRoaming();
        }

        public override bool MatchInteruptionCondition()
        {
           /* for (int i = 0; i < aiAgentEntity.targetHandlingSubsystem.AiTargets.Count; ++i)
            {
                if (aiAgentEntity.targetHandlingSubsystem.AiTargets[i].InSight && aiAgentEntity.targetHandlingSubsystem.AiTargets[i].Entity.IsAlive)
                {
                    return true;
                }
            }*/

            return false;
        }

        /*
                private void OnTargetHandlingSubsystemEnteredRange(Entity owner, AiTarget target)
                {
                    if (owner == context && context.roamingSubsystem.IsRoaming)
                    {
                        for(int i = 0; i < context.targetHandlingSubsystem.AiTargets.Count; ++i)
                        {
                            if(context.targetHandlingSubsystem.AiTargets[i].InSight && context.targetHandlingSubsystem.AiTargets[i].Entity.IsAlive)
                            {
                                brain.InterruptAction(this);
                                break;
                            }
                        }
                    }
                }

                private void OnTargetHandlingSubsystemLeftRange(Entity owner, AiTarget target)
                {
                    *//*if (owner == context && context.targetHandlingSubsystem.AiTargets.Count == 0)
                    {
                        if (!context.roamingSubsystem.IsRoaming)
                        {
                            context.roamingSubsystem.StartRoaming();
                        }
                    }*//*
                }*/

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.roamingSubsystem.StopRoaming();
        }

    }
}
