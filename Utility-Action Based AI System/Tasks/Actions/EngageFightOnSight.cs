using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAndMagic.Systems.IA
{
    public class EngageFightOnSight : GenericAiAction
    {
        public override int ComputePriority()
        {
            if (brain.IsFighting)
                return 0;

            for (int i = 0; i < aiAgentEntity.targetHandlingSubsystem.AiTargets.Count; ++i)
            {
                if (aiAgentEntity.targetHandlingSubsystem.AiTargets[i].InSight && aiAgentEntity.targetHandlingSubsystem.AiTargets[i].Entity.IsAlive)
                {
                    return 100;
                }
            }

            return 0;
        }

        public override void Execute()
        {
            if (brain.CurrentAction != null)
            {
                brain.InterruptCurrentAction();
            }

            brain.IsFighting = true;
            aiAgentEntity.aiAgent.StartBrain();
        }
    }
}
