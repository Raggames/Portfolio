using Assets.SteamAndMagic.Scripts.Managers;
using SteamAndMagic.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SteamAndMagic.Systems.IA
{
    public class FocusAndMoveToTargetOnAgressedAction : CompoundAction
    {
        /// <summary>
        /// La portée à laquelle l'agent va chercher des alliés et les entrainer avec lui dans le combat lorsqu'il est agressé
        /// </summary>
        public int AgressionPackRange = 0;

        public override int ComputePriority()
        {
            return 0;
        }

        public override void EnableAction()
        {
            base.EnableAction();
            EncounterManagerEventHandler.OnEntityJoinEncounter += EncounterManagerEventHandler_OnEntityJoinEncounter;
            EncounterManager.OnEncounterStart += EncounterManager_OnEncounterStart;
            EncounterManager.OnEncounterEnd += EncounterManager_OnEncounterEnd;
            AiAgentEntityEventHandler.OnAiAgentEngagedByEnemy += AiAgentEntityEventHandler_OnAiAgentEngagedByEnemy;
        }

        public override void DisableAction()
        {
            base.DisableAction();
            EncounterManager.OnEncounterStart -= EncounterManager_OnEncounterStart;
            EncounterManager.OnEncounterEnd -= EncounterManager_OnEncounterEnd;
            AiAgentEntityEventHandler.OnAiAgentEngagedByEnemy -= AiAgentEntityEventHandler_OnAiAgentEngagedByEnemy;
            EncounterManagerEventHandler.OnEntityJoinEncounter -= EncounterManagerEventHandler_OnEntityJoinEncounter;
        }

        private void EncounterManagerEventHandler_OnEntityJoinEncounter(Entity arg1, Encounter arg2)
        {
            if (!GameServer.IsMaster)
            {
                return;
            }

            if(arg1 == aiAgentEntity && !brain.IsFighting)
            {
                // Si rien trouvé on passe quand meme en combat et on avise
                brain.IsFighting = true;
                aiAgentEntity.aiAgent.StartBrain();

                List<Entity> enemies = arg2.GetEnemisEntities(aiAgentEntity.Team);
                enemies.Sort((a, b) => Vector3.Distance(aiAgentEntity.transform.position, a.transform.position).CompareTo(Vector3.Distance(aiAgentEntity.transform.position, b.transform.position)));

                for (int i = 0; i < enemies.Count; ++i)
                {
                    if (!enemies[i].IsDead)
                    {
                        StartFightWith(enemies[i]);
                        AiAgentEntityEventHandler.AiAgentEngagedByEnemyRequest(aiAgentEntity, enemies[i]);
                        return;
                    }
                }


                // Notifier qu'on vient d'etre attaqué et qu'on engage le combat
            }
        }

        private void EncounterManager_OnEncounterStart(Entity attacker, List<Entity> involvedEntities)
        {
            if (!GameServer.IsMaster)
            {
                return;
            }

            if (involvedEntities.Contains(aiAgentEntity))
            {
                if(attacker != aiAgentEntity)
                {
                    StartFightWith(attacker);

                    // Notifier qu'on vient d'etre attaqué et qu'on engage le combat
                    AiAgentEntityEventHandler.AiAgentEngagedByEnemyRequest(aiAgentEntity, attacker);
                }
                else
                {
                    brain.IsFighting = true;
                }
            }
        }

        public override void Initialize(Brain brain, AiAgentEntity context)
        {
            base.Initialize(brain, context);
            brain.IsFighting = false;
        }

        private void StartFightWith(Entity launcher)
        {
            if (brain.CurrentAction != null)
            {
                brain.InterruptCurrentAction();
            }

            aiAgentEntity.targetHandlingSubsystem.ForceFocus(launcher);
            brain.StartAction(Sequence[0], Sequence[0].MatchInteruptionCondition); // Sequence[0] = MoveToFocusAction

            brain.IsFighting = true;
        }
        /*
                private void GameServer_OnAfterDamageCallback(Entity launcher, Entity target, string skillKey, global::Systems.DamageEffect.Damage[] damages, long dotHotId = -1)
                {
                    if (target == brain.BrainOwner.Context.AgentEntity && launcher.Team != target.Team && !(bool)brain.WorkingMemory.GetValue(WorkingMemoryDataType.IsFighting))
                    {
                        StartFightWith(launcher);

                        // Notifier qu'on vient d'etre attaqué et qu'on engage le combat
                        AiAgentEntityEventHandler.AiAgentEngagedByEnemyRequest(aiAgentEntity, launcher);
                    }
                }*/

        private void AiAgentEntityEventHandler_OnAiAgentEngagedByEnemy(Entity agressor, AiAgentEntity agressed)
        {
            if (!GameServer.IsMaster)
            {
                return;
            }

            if (AgressionPackRange != 0 && agressed != aiAgentEntity && agressed.Team == aiAgentEntity.Team && !brain.IsFighting && Vector3.Distance(aiAgentEntity.transform.position, agressed.transform.position) <= AgressionPackRange)
            {
                StartFightWith(agressor);
            }
        }

        private void EncounterManager_OnEncounterEnd(List<Entity> involvedEntities)
        {
            if (!GameServer.IsMaster)
            {
                return;
            }

            if (involvedEntities.Contains(aiAgentEntity))
                brain.IsFighting = false;
        }
    }
}
