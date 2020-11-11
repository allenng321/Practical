using UnityEngine;
using Core.Characters;
using Assets.Scripts.Extensions;
using System;

public class NpcAttackState : NpcAbstractState
{
    #region Private & Const Variables

    private enum AttackType 
    { 
        Light,
        //Heavy
    }

    #endregion

    #region Public & Protected Variables

    #endregion

    #region Constructors

    public NpcAttackState(NpcCharacter npc
        , INpcAnimatorController animatorController
        , INpcStateController npcStateController) 
        : base(npc, animatorController, npcStateController) 
    {
        StateName = "Attack";
        ChooseAttackType();
    }

    #endregion

    #region Private Methods

    // TODO might be deleted after finishing Attack method on NpcController
    private void RotateTowardsTarget(GameObject target)
    {
        var targetDirection = (target.transform.position - NpcTransform.position);

        // Rotate only around y-axis
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection)
            .With(x: NpcTransform.rotation.x, z: NpcTransform.rotation.z);

        // Smoothly rotate towards the target point.
        NpcTransform.rotation =
            Quaternion.Slerp(NpcTransform.rotation, targetRotation, Npc.TurnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Selects how to attack (Light, Heavy, etc.)
    /// </summary>
    private void ChooseAttackType() 
    {
        // TODO: Works random right now, should calculated
        var random = new System.Random();
        var type = typeof(AttackType);
        var attackTypes = type.GetEnumValues();
        var randomIndex = random.Next(attackTypes.Length);
        var randomAttackType = attackTypes.GetValue(randomIndex);

        switch (randomAttackType) 
        {
            case AttackType.Light:
                NpcAnimatorController.LightAttack();
                break;
            //case AttackType.Heavy:
              //  break;
        }
    }

    #endregion

    #region Public & Protected Methods		

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override INpcState DoAction()
    {
        RotateTowardsTarget(Npc.Target);
        ChooseAttackType();
        NpcAnimatorController.Tick();

        return GetNextState();
    }   

    #endregion
}
