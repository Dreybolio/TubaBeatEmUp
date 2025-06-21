using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClassDruid: PlayerController
{
    [Header("Druid")]
    [Header("Special - Attack Stats")]
    [SerializeField] private float specialMagicCost = 60.0f;
    [SerializeField] private float specialMagicRegenCooldown = 0.75f;
    [SerializeField] private float specialDistance = 1.75f;
    [SerializeField] private int specialDamage = 2;
    [SerializeField] private float specialAirborneKnockbackForce = 1.4f;

    [Header("Special - Movement Stats")]

    [Header("Dash Special - Attack Stats")]
    [SerializeField] private float dashSpecialMagicCost = 60.0f;
    [SerializeField] private float dashSpecialMagicRegenCooldown = 0.75f;
    [SerializeField] private float dashSpecialDistance = 1.0f;
    [SerializeField] private int dashSpecialDamage = 3;
    [SerializeField] private float dashSpecialAirborneKnockbackForce = 1.4f;

    [Header("Dash Special - Movement Stats")]

    [Header("Particles")]
    [SerializeField] private ParticleObject particleSlashPrefab;

    // Vars

    // Anim
    void Start()
    {
        AssignClassAnimationIDs();
        ControllerInit();
    }

    #region Special Attack
    public override void Special()
    {
        if (SpendMagicOnAction(specialMagicCost))
        {
            StartCoroutine(C_Special());
        }
        else
        {
            ActionAnimFinished();
        }
    }

    public override bool CanAffordSpecial()
    {
        return Magic >= specialMagicCost;
    }

    private IEnumerator C_Special()
    {
        AnimatorOverrideSetEnabled(true);
        model.Animator.ResetTrigger(_animCancelAction_T);
        yield return null;

        AnimatorOverrideSetEnabled(false);
        ActionAnimFinished();
    }
    #endregion


    #region Dash Special Attack
    public override void DashSpecial()
    {
        if (SpendMagicOnAction(dashSpecialMagicCost))
        {
            StartCoroutine(C_DashSpecial());
        }
        else
        {
            ActionAnimFinished();
        }
    }

    private IEnumerator C_DashSpecial()
    {
        AnimatorOverrideSetEnabled(true);
        model.Animator.ResetTrigger(_animCancelAction_T);
        yield return null;

        AnimatorOverrideSetEnabled(false);
        ActionAnimFinished();
    }

    public override bool CanAffordDashSpecial()
    {
        return Magic >= dashSpecialMagicCost;
    }

    #endregion


    private void AssignClassAnimationIDs()
    {

    }
}
