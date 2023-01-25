using SteamAndMagic.Systems.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamAndMagic.Systems.IA
{
    public class MoveToCapturePoint : GenericAiAction
    {
        public int tempCapturePointProgressionPoints;
        public CapturableAreaModule tempCapturablePoint;

        public override void Execute()
        {
            tempCapturablePoint = (CapturableAreaModule)brain.WorkingMemory.GetValue(WorkingMemoryDataType.CapturablePointFocus);
            tempCapturePointProgressionPoints = (int)tempCapturablePoint.TeamPointsCounter[aiAgentEntity.Team];

            aiAgentEntity.aiControl.MoveTo(
                 tempCapturablePoint.transform.position,
                 EndAction,
                 "move to capture point",
                 tempCapturablePoint.transform,
                 aiAgentEntity.aiControl.Speed);
        }

        public override bool MatchInteruptionCondition()
        {
            return tempCapturablePoint.CurrentPossessingTeam == aiAgentEntity.Team && tempCapturablePoint.TeamPointsCounter[aiAgentEntity.Team] == tempCapturablePoint.MaxPossessionPoint;
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();

            aiAgentEntity.aiControl.Stop();
        }
    }
}
