using Assets.SteamAndMagic.Scripts.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAndMagic.Systems.IA
{
    public class ReturnToInitialPosition : GenericAiAction
    {
        private void OnEnable()
        {
            EncounterManager.OnEncounterEnd += EncounterManager_OnEncounterEnd;
        }

        private void OnDisable()
        {
            EncounterManager.OnEncounterEnd -= EncounterManager_OnEncounterEnd;
        }

        public override void Execute()
        {
            aiAgentEntity.aiControl.MoveTo(aiAgentEntity.aiControl.InitialPosition, EndAction, "Returning to initialPosition", null, aiAgentEntity.aiControl.RoamSpeed, .5f);
        }

        private void EncounterManager_OnEncounterEnd(List<Entities.Entity> involvedEntities)
        {
            if (GameServer.IsMaster && involvedEntities.Contains(aiAgentEntity))
            {
                if (brain.CurrentAction != null)
                {
                    brain.InterruptCurrentAction();
                }

                brain.StartAction(this, MatchInteruptionCondition);
            }
        }
    }
}
