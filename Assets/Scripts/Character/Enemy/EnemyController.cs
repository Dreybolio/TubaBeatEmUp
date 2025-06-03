using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AIBehaviours))]
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
    public int SquadID = 0;

    [Header("Combos - Light")]
    [SerializeField] private AttackCombo comboDoubleLight;
    [SerializeField] private AttackCombo comboTripleLight;
    [SerializeField] private AttackCombo comboSpin;
    [Header("Combos - Heavy")]
    [SerializeField] private AttackCombo comboDoubleHeavy;
    private List<AttackCombo> combos;

    // Vars for AI (Ignored when PlayerController inhereits this)
    [NonSerialized] public Character Target;
    [NonSerialized] public AI_SquadRole SquadRole;

    // Events
    protected event CharacterEvent OnActionFinished;

    // Vars
    private Vector2 _movementInput;
    private ActionType _currAction;
    private AttackCombo _currCombo;
    private bool _attackHitFramePassed = false;
    private bool _attacksAlwaysKnockback = false;
    private bool _actionAnimFinished = false;
    private int _stunCount = 0;
    private float _prevStunTime = 0;

    // Anim
    private int _animActionID_I, _animCancelAction_T;
    void Start()
    {
        AttackCombo[] comboArr = { comboDoubleLight, comboTripleLight, comboDoubleHeavy, comboSpin };
        combos = comboArr.Where(c => c.enabled).ToList();
        AssignExtraAnimationIDs();
        AssignComboData();
        CharacterInit();
        EnemyManager.Instance.Enemies.Add(this);
        EnemyManager.Instance.AddToSquad(this, SquadID);
        aiBehaviour.Enemy = this;
        StartCoroutine(C_AILoop());
    }

    private void OnEnable()
    {
        model.AnimListener.OnLightHitFrame += LightAttackHitFrame;     // Event01: Light Attack Hit Frame
        model.AnimListener.OnHeavyHitFrame += HeavyAttackHitFrame;     // Event02: Heavy Attack Hit Frame
        model.AnimListener.OnActionAnimOver += ActionAnimFinished;         // Event03: Attack Over Event

        // Events with oneself
        OnLoseControl += CancelAction;
        OnHurtStun += HurtStunCounter;
        OnDeath += EnemyDeath;
    }
    private void OnDisable()
    {
        model.AnimListener.OnLightHitFrame += LightAttackHitFrame;     // Event01: Light Attack Hit Frame
        model.AnimListener.OnHeavyHitFrame += HeavyAttackHitFrame;     // Event02: Heavy Attack Hit Frame
        model.AnimListener.OnActionAnimOver -= ActionAnimFinished;         // Event03: Attack Over Event

        // Events with oneself
        OnLoseControl -= CancelAction;
        OnHurtStun -= HurtStunCounter;
        OnDeath += EnemyDeath;
    }

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity(false, jumpHeight);
        Move(_movementInput, speed, false);
        if (_doFaceMoveDir && _hasControl)
        {
            if (Target != null)
                TurnFaceTarget(Target.transform);
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
        foreach (var combo in combos)
        {
            if (combo.timer > 0)
                combo.timer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (Target == null || Target.Health <= 0)
        {
            Debug.Log("Target Invalid: Forcing EnemyManager to rebalance targets");
            EnemyManager.Instance.BalanceSquad(SquadID);
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

            switch (decision.actionOverride)
            {
                case AI_Action_Override.NONE:
                    break;
                case AI_Action_Override.CHANGEROLE_OFFENSE:
                    EnemyManager.Instance.AddRoleOverride(this, AI_SquadRole.OFFENSE);
                    break;
                default:
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
    public void LightAttack()
    {
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.ATTACK_LIGHT);
    }

    public void HeavyAttack()
    {
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.ATTACK_HEAVY);
        // Stop movement during a heavy attack
        _hasMovement = false;
        if (Grounded) _speed.Set(0, 0); // Stop speed if grounded
        OnActionFinished += HeavyAttackEnd;
    }
    private void HeavyAttackEnd()
    {
        OnActionFinished -= HeavyAttackEnd;
        _hasMovement = true;
    }

    public void SpinAttack()
    {
        // Because this action is not triggered normally, action type must be set here
        _currAction = ActionType.ATTACK_SPIN;
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.ATTACK_SPIN);
        _hasMovement = false;
        _canBeHit = false;
        // Do dash speeds as this happens
        _speed = _facingRight ? new(speed, 0) : new(-speed, 0);
        // Revert variables when done
        OnActionFinished += SpinAttackEnd;
    }

    public void SpinAttackEnd()
    {
        // Stop listening for self
        OnActionFinished -= SpinAttackEnd;
        // Revert Variables
        _speed = Vector2.zero;
        _hasMovement = true;
        _canBeHit = true;
    }

    public void ActionAnimFinished()
    {
        // Any changed variables get reset
        _actionAnimFinished = true;
        OnActionFinished?.Invoke();

        if (_currCombo != null)
        {
            // Continue this active combo
            if (!DoNextSequenceInCombo(_currCombo))
            {
                EndCombo();
            }
            return;
        }

        // Check for valid combos with the current action
        List<AttackCombo> valid = combos.Where(c => c.prerequisite == _currAction && c.timer <= 0).ToList();
        if (valid.Count == 1)
        {
            // Do the only valid combo
            AttackCombo selected = valid[0];
            _currCombo = selected;
            if (!DoNextSequenceInCombo(selected))
            {
                EndCombo();
            }
            return;
        }
        else if (valid.Count > 1)
        {
            // Roll a favourability die to determine which combo executes
            int sum = valid.Sum(c => c.favourability);
            int rand = Random.Range(0, sum);
            int cumulative = 0;
            AttackCombo selected = valid.First(c =>
            {
                cumulative += c.favourability;
                return rand < cumulative;
            });
            _currCombo = selected;
            if (!DoNextSequenceInCombo(selected))
            {
                EndCombo();
            }
            return;
        }
        else
        {
            // No valid combos were selected do nothing now
            EndCombo();
        }
    }

    public bool DoNextSequenceInCombo(AttackCombo combo)
    {
        if (combo == null)
        {
            Debug.LogWarning("Tried to execute a null combo!");
            return false;
        }
        if (combo.sequence >= combo.actions.Count)
        {
            // Combo is finished, stop doing it
            combo.sequence = 0;
            combo.timer = combo.cooldown;
            return false;
        }

        if (combo.sequence == combo.actions.Count - 1)
        {
            // This is the combo finisher, do special actions
            _attacksAlwaysKnockback = combo.knockbackOnLastHit;
        }
        // Do the next action
        combo.actions[combo.sequence].Invoke();
        combo.sequence++;
        return true;
    }

    private void EndCombo()
    {
        if (_currCombo != null && _currCombo.knockbackOnLastHit)
        {
            _attacksAlwaysKnockback = false;
        }
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.NONE);
        _currCombo = null;
        _currAction = ActionType.NONE;
    }

    /*
    * Only triggered if some game event happens, e.g. Being knocked prone or dying. Basically an emergency "oh shit we need to stop doing this NOW"
    */
    public void CancelAction()
    {
        // Stop this attack from happening
        _currAction = ActionType.NONE;
        _currCombo = null;
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.NONE);
        // Panic cancel this animation
        model.Animator.SetTrigger(_animCancelAction_T);
    }

    /*
     *  Triggered by Animation System during light attack swing animation
     */
    public void LightAttackHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(forwardXPoint.position, transform.right * transform.localScale.x, lightAttackDistance);
        foreach (var c in hits)
        {
            bool killedPlayer = c.Key.Character.Damage(lightAttackDamage, c.Value);
            if (!c.Key.Character.Grounded || _attacksAlwaysKnockback)
            {
                c.Key.Character.Knockback(_facingRight, lightAttackAirborneKnockbackForce);
            }
            else
            {
                c.Key.Character.HurtStun();
            }
            // Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + lightAttackDamage + " damage.");
        }
        _attackHitFramePassed = true;
    }

    /*
    *  Triggered by Animation System during light attack swing animation
    */
    public void HeavyAttackHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(forwardXPoint.position, transform.right * transform.localScale.x, heavyAttackDistance);
        foreach (var c in hits)
        {
            c.Key.Character.Damage(heavyAttackDamage, c.Value);
            if (!c.Key.Character.Grounded || _attacksAlwaysKnockback)
            {
                c.Key.Character.Knockback(_facingRight, c.Key.Character.Grounded ? heavyAttackKnockbackForce : heavyAttackAirborneKnockbackForce);
            }
            else
            {
                c.Key.Character.HurtStun();
            }
            // Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + heavyAttackDamage + " damage.");
        }
        _attackHitFramePassed = true;
    }

    private void EnemyDeath()
    {
        StartCoroutine(C_BlinkOut());
        EnemyManager.Instance.Enemies.Remove(this);
        EnemyManager.Instance.RemoveFromSquad(this, SquadID);
    }

    private void HurtStunCounter()
    {
        _prevStunTime = Time.time;
        _stunCount++;
        _stunImmunity = (_stunCount > stunsUntilImmunity);
    }

    public void RequestChangeTarget(Transform newTarget)
    {
        EnemyManager.Instance.AddTargetOverride(this, newTarget);
    }

    public void AssignExtraAnimationIDs()
    {
        _animActionID_I = Animator.StringToHash("ActionID");
        _animCancelAction_T = Animator.StringToHash("CancelAction");
    }
    public void AssignComboData()
    {
        // Light combos - Note, these always start with a light attack, so that first light attack isn't internally "part of" the combo
        // Double Light
        comboDoubleLight.actions.Add(LightAttack);
        comboDoubleLight.prerequisite = ActionType.ATTACK_LIGHT;
        // Triple Light
        comboTripleLight.actions.Add(LightAttack);
        comboTripleLight.actions.Add(LightAttack);
        comboTripleLight.prerequisite = ActionType.ATTACK_LIGHT;
        // Spin
        comboSpin.actions.Add(SpinAttack);
        comboSpin.prerequisite = ActionType.ATTACK_LIGHT;
        comboSpin.knockbackOnLastHit = true;

        // Heavy combos - These always start with a heavy attack
        comboDoubleHeavy.actions.Add(HeavyAttack);
        comboDoubleHeavy.prerequisite = ActionType.ATTACK_HEAVY;
        comboDoubleHeavy.knockbackOnLastHit = true;
    }
}

// ** The reason this is a class and not a struct is that structs are pass-by-value, which is a memory problem.
// ** Classes are pass-by-reference which is what we want
[Serializable]
public class AttackCombo
{
    public bool enabled = false;
    public float cooldown = 5f;
    public int favourability = 1;
    [NonSerialized] public ActionType prerequisite = ActionType.NONE;
    [NonSerialized] public float timer = 0;
    [NonSerialized] public int sequence = 0;
    [NonSerialized] public List<Action> actions = new();
    [NonSerialized] public bool knockbackOnLastHit;
}