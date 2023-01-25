using SteamAndMagic.Entities;
using UnityEngine;
using System;

namespace SteamAndMagic.Systems.IA
{
    public class GoToSlaveMasterAction : GenericAiAction
    {
        public Entity SlaveMaster;

        private Vector3 tempFocusPosition;
        public float ThresholdDistance = 5;
        public float StopDistance = 2;
        public float DistanceToMasterRatio = 2.5f;

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);

            SlaveMaster = ((UndeadServantEntity)context).SlaveMaster;
        }

        public override int ComputePriority()
        {
            if (!aiAgentEntity.CanDoMovement)
                return 0;

            float dist = Vector3.Distance(aiAgentEntity.transform.position, SlaveMaster.transform.position);
            if (dist < ThresholdDistance)
                return 0;

            return Mathf.RoundToInt(dist * DistanceToMasterRatio);
        }

        public override void Execute()
        {
            tempFocusPosition = SlaveMaster.transform.position;
            aiAgentEntity.aiControl.MoveTo(SlaveMaster.transform.position, EndAction, "goto slave master", SlaveMaster.transform, aiAgentEntity.aiControl.Speed, StopDistance);
        }

        public override bool MatchInteruptionCondition()
        {
            return SlaveMaster.IsDead
                   || Vector3.Distance(SlaveMaster.transform.position, tempFocusPosition) > 1f
                   || !aiAgentEntity.aiControl.IsMoving;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.aiControl.Stop();
        }
    }
}
