using SteamAndMagic.Entities;
using System;
using System.Linq;
using UnityEngine;

public class NpcAbilityController : AbilityController
{

    public override void Init(Entity owner)
    {
        Owner = owner;
        //OwnerNpc = owner as Npc;

        for (int i = 0; i < Abilities.Length; ++i)
        {
            var instance = Instantiate(Abilities[i], Vector3.zero, Quaternion.identity, owner.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.gameObject.name = Abilities[i].name;

            Abilities[i] = instance;
            Abilities[i].enabled = true;
            Abilities[i].gameObject.SetActive(true);
            Abilities[i].Init(this, i, owner);
        }
    }

    public override void Server_SelectAbility(Ability ability, Entity target, Vector3 position)
    {
        if (ability != null && Abilities.Contains(ability))
        {
            switch (CurrentAbility.AimingMode)
            {
                case AbilityAimingMode.Target:
                    ServerRequest_StartCast_OnEntity(CurrentAbility, target, 0, false);
                    break;
                case AbilityAimingMode.Self:
                    ServerRequest_StartCast_OnEntity(CurrentAbility, Owner, 0, false);
                    break;
                case AbilityAimingMode.AoE:
                case AbilityAimingMode.Directionnal:
                case AbilityAimingMode.FreeAimingPoint:
                    ServerRequest_StartCast_OnPosition(CurrentAbility, position, 0, false);
                    break;
                case AbilityAimingMode.Cleave:
                    ServerRequest_StartCast_OnPosition(CurrentAbility, transform.position, 0, false);
                    break;
            }
        }
        else Debug.LogError("Cannot launch ability on " + this);
    }

    /* public override void ExecuteSkillLoop()
     {
         if (CurrentAbility != null)
         {
             if (CurrentAbility.CurrentState == AbilityState.Executing)
             {
                 switch (CurrentAbility.AimingMode)
                 {
                     case AbilityAimingMode.Target:
                         if (Owner.CurrentEntityTarget != null)
                         {
                             Request_ExecuteSkillLoop_OnEntity(CurrentAbility, Owner.CurrentEntityTarget);
                         }
                         else
                         {
                             Debug.LogError("No target !");
                         }
                         break;
                     case AbilityAimingMode.AoE:
                         Request_ExecuteSkillLoop_OnPosition(CurrentAbility, Owner.CurrentPositionTarget);
                         break;
                     case AbilityAimingMode.Directionnal:
                         Request_ExecuteSkillLoop_OnPosition(CurrentAbility, Owner.CurrentPositionTarget + Owner.transform.position);
                         break;
                     case AbilityAimingMode.Cleave:
                         break;
                 }
             }
         }*/
    /*
            else if (Owner is Npc)
            {
                Request_ExecuteSkill(currentSkill, (Owner as Npc).Aim());
            }*/
}



