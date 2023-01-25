using SteamAndMagic.Systems.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA.Actions
{
    public class FindPointToCapture : GenericAiAction
    {
        public CapturableAreaModule[] capturableAreaModules = null;

        public int pointThreshold = 20;

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);
            capturableAreaModules = GameObject.FindObjectsOfType<CapturableAreaModule>();
        }

        public override void Execute()
        {
            List<CapturableAreaModule> pointsOfInterest = new List<CapturableAreaModule>();

            for(int i = 0; i < capturableAreaModules.Length; ++i)
            {
                if(capturableAreaModules[i].CurrentPossessingTeam != aiAgentEntity.Team || capturableAreaModules[i].TeamPointsCounter[aiAgentEntity.Team] <= pointThreshold)
                {
                    pointsOfInterest.Add(capturableAreaModules[i]);
                }
            }

            if(pointsOfInterest.Count > 0)
            {
                int index = -1;
                float dist = float.MaxValue;

                for(int i = 0; i < pointsOfInterest.Count; ++i)
                {
                    float currentDist = (pointsOfInterest[i].transform.position - aiAgentEntity.transform.position).sqrMagnitude;
                    if(currentDist < dist)
                    {
                        dist = currentDist;
                        index = i;
                    }
                }

                brain.WorkingMemory.WriteValue(WorkingMemoryDataType.CapturablePointFocus, pointsOfInterest[index]);
            }
            else
            {
                brain.WorkingMemory.WriteValue(WorkingMemoryDataType.CapturablePointFocus, null);
            }

            EndAction();
        }
    }
}
