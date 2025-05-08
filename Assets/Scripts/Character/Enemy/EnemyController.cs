using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : Character
{
    [Header("Attacks")]
    [SerializeField] private float lightAttackDistance = 1.0f;
    [SerializeField] private int lightAttackDamage = 2;
    [SerializeField] private float lightAttackAirborneKnockbackForce = 0.4f;


    [SerializeField] private float heavyAttackDistance = 1.0f;
    [SerializeField] private int heavyAttackDamage = 3;
    [SerializeField] private float heavyAttackKnockbackForce = 0.8f;
    [SerializeField] private float heavyAttackAirborneKnockbackForce = 1.2f;

    [Header("AI")]
    [SerializeField] private AIBehaviours aiBehaviour;
    [SerializeField] private int aiDifficulty = 1;
    [SerializeField] private float stunImmunityResetTimer = 2.5f;
    [SerializeField] private int stunsUntilImmunity = 5;
    [SerializeField] private int squadID = 0;

    // Vars
    private Vector2 _movementInput;
    private ActionType _currAction;
    private bool _attackHitFramePassed = false;
    private bool _actionAnimFinished = false;
    private int _stunCount = 0;
    private float _prevStunTime = 0;

    // Anim
    private int _animActionID_I, _animCancelAction_T;
    void Start()
    {
        TryFindTarget();
        AssignExtraAnimationIDs();
        CharacterInit();
        SquadManager.Instance.AddToSquad(this, squadID);
        StartCoroutine(C_AILoop());
    }

    private void OnEnable()
    {
        model.AnimListener.OnLightHitFrame += LightAttackHitboxCheck;     // Event01: Light Attack Hit Frame
        model.AnimListener.OnHeavyHitFrame += HeavyAttackHitboxCheck;     // Event02: Heavy Attack Hit Frame
        model.AnimListener.OnActionAnimOver += ActionAnimFinished;         // Event03: Attack Over Event

        // Events with oneself
        OnLoseControl += CancelAction;
        OnHurtStun += HurtStunCounter;
        OnDeath += RemoveSelfFromSquad;
    }
    private void OnDisable()
    {
        model.AnimListener.OnLightHitFrame += LightAttackHitboxCheck;     // Event01: Light Attack Hit Frame
        model.AnimListener.OnHeavyHitFrame += HeavyAttackHitboxCheck;     // Event02: Heavy Attack Hit Frame
        model.AnimListener.OnActionAnimOver -= ActionAnimFinished;         // Event03: Attack Over Event

        // Events with oneself
        OnLoseControl -= CancelAction;
        OnHurtStun -= HurtStunCounter;
        OnDeath -= RemoveSelfFromSquad;
    }

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity(false);
        Move(_movementInput, speed, false);
        if (_hasControl)
        {
            if (Target != null)
                TurnFaceTarget(Target);
            else
                TurnFaceMoveDir();
        }
        // Else, you don't choose your look direction
        ApplyVelocity();

        // Reset the stun immunity timer if it's been too long
        if (_stunCount > 0 && (_prevStunTime + stunImmunityResetTimer) < Time.time)
        {
            // it's been too long, reset all
            _stunCount = 0;
            _stunImmunity = false;
        }
    }

    private void FixedUpdate()
    {
        if (Target == null)
        {
            TryFindTarget();
        }
    }

    private IEnumerator C_AILoop()
    {
        while (Health > 0)
        {
            AI_Decision decision = aiBehaviour.MakeDecision(SquadRole, aiDifficulty);
            switch (decision.action)
            {
                case AI_Action.IDLE:
                    _movementInput = Vector2.zero;
                    break;

                case AI_Action.ATTACK:
                    _movementInput = Vector2.zero;
                    if (Random.Range(0, 2) == 0)
                        Attack(ActionType.ATTACK_HEAVY);
                    else
                        Attack(ActionType.ATTACK_LIGHT);
                    break;

                case AI_Action.MOVE:
                    _movementInput = decision.data;
                    break;
            }
            yield return null;
        }
    }

    public void Attack(ActionType type)
    {
        if (!_hasControl) return;
        if (_currAction == ActionType.NONE)
        {
            // Nothing currently being done, do attack
            _currAction = type;
            _attackHitFramePassed = false;
            _actionAnimFinished = false;
            model.Animator.SetInteger(_animActionID_I, (int)type);        
        }
    }

    public void ActionAnimFinished()
    {
        // Animation awaiting used for enemy combo attacks
        /*
         *  TODO: Replace this with an effective enemy Combo System
         */
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.NONE);
        _currAction = ActionType.NONE;
        _actionAnimFinished = true;
    }

    /*
    * Only triggered if some game event happens, e.g. Being knocked prone or dying. Basically an emergency "oh shit we need to stop doing this NOW"
    */
    public void CancelAction()
    {
        // Stop this attack from happening
        _currAction = ActionType.NONE;
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.NONE);
        // Panic cancel this animation
        model.Animator.SetTrigger(_animCancelAction_T);
    }

    /*
     *  Triggered by Animation System during light attack swing animation
     */
    public void LightAttackHitboxCheck()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(lightAttackDistance);
        foreach (var c in hits)
        {
            bool killedPlayer = c.Key.Character.Damage(lightAttackDamage, c.Value);
            if (!c.Key.Character.Grounded)
            {
                c.Key.Character.Knockback(_facingRight, lightAttackAirborneKnockbackForce);
            }
            else
            {
                c.Key.Character.HurtStun();
            }
            Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + lightAttackDamage + " damage.");
        }
        _attackHitFramePassed = true;
    }

    /*
    *  Triggered by Animation System during light attack swing animation
    */
    public void HeavyAttackHitboxCheck()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(heavyAttackDistance);
        foreach (var c in hits)
        {
            c.Key.Character.Damage(heavyAttackDamage, c.Value);
            c.Key.Character.Knockback(_facingRight, c.Key.Character.Grounded ? heavyAttackKnockbackForce : heavyAttackAirborneKnockbackForce);
            Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + heavyAttackDamage + " damage.");
        }
        _attackHitFramePassed = true;
    }

    private void HurtStunCounter()
    {
        _prevStunTime = Time.time;
        _stunCount++;
        _stunImmunity = (_stunCount > stunsUntilImmunity);
    }

    private void RemoveSelfFromSquad()
    {
        SquadManager.Instance.RemoveFromSquad(this, squadID);
    }

    private void TryFindTarget()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            Target = player.transform;
            aiBehaviour.target = Target;
        }
    }
    public void AssignExtraAnimationIDs()
    {
        _animActionID_I = Animator.StringToHash("ActionID");
        _animCancelAction_T = Animator.StringToHash("CancelAction");
    }
}
