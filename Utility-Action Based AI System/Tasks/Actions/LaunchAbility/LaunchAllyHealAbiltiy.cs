using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAndMagic.Systems.IA
{
    class LaunchAllyHealAbiltiy : LaunchAbilityAction
    {
        public override int ComputePriority()
        {
            if (!Ability.IsLaunchable())
                return 0;

            int count = 0;
            for (int i = 0; i < aiAgentEntity.targetHandlingSubsystem.AiTargets.Count; ++i)
            {
                if (aiAgentEntity.targetHandlingSubsystem.AiTargets[i].InRange)
                {
                    count++;
                }
            }

            return 0;
        }
    }
}
