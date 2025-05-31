using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class PlayerController : Character
{
    [Header("PlayerController")]
    [Header("Actions - Light")]
    [SerializeField] private float lightAttackDistance = 1.0f;
    [SerializeField] private int lightAttackDamage = 2;
    [SerializeField] private float lightAttackAirborneKnockbackForce = 0.4f;

    [Header("Actions - Heavy")]
    [SerializeField] private float heavyAttackDistance = 1.0f;
    [SerializeField] private int heavyAttackDamage = 3;
    [SerializeField] private float heavyAttackKnockbackForce = 0.8f;
    [SerializeField] private float heavyAttackAirborneKnockbackForce = 1.2f;

    [Header("Actions - Dash")]
    [SerializeField] private float dashMagicCost = 30f;
    [SerializeField] private float dashMagicRegenCooldown = 0.75f;
    [SerializeField] protected float dashSpeed = 5.0f;
    [SerializeField] private float dashLength = 0.30f;
    [SerializeField] private float dashCooldown = 0.15f;

    [Header("Actions - Drill")]
    [SerializeField] private float drillMagicCost = 55f;
    [SerializeField] private float drillMagicRegenCooldown = 0.75f;
    // Note: Drill uses same damage stat as Heavy Attack

    [Header("Magic Stats")]
    public float MaxMagic;
    [SerializeField] private float magicRegenRate = 1.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip sndSpecial;

    [Header("Player Icons")]
    public Sprite Icon;

    // Pointers assigned at runtime
    [NonSerialized] public Player Player;

    // Events
    public event CharacterEvent OnUseMagic;
    public event CharacterEvent OnMagicChanged;
    protected event CharacterEvent OnActionFinished;

    // Public Vars
    public float Magic { get; private set; }
    protected bool _doMagicRegen = true;

    // Vars
    private ActionType _currAction;
    private ActionType _actionBuffer;
    private float _dashCooldown = 0f;
    private float _magicRegenCooldown = 0f;
    private bool _attacksAlwaysKnockback = false;
    private bool _actionAnimFinished = false;
    private List<ActionType> _actionHistory = new();
    private Coroutine _actionBufferRoutine;

    // Anim
    protected int _animActionID_I, _animCancelAction_T;

    protected void ControllerInit()
    {
        Magic = MaxMagic;
        AssignExtraAnimationIDs();
        GameUI.Instance.AddPlayerUI(this);
        CharacterInit();
    }
    public void AssignPlayer(Player player)
    {
        Player = player;
        Player.OnPlayerAction += DoAction;
    }

    private void OnEnable()
    {
        model.AnimListener.OnLightHitFrame += LightAttackHitFrame;    // Event01: Light Action Hit Frame
        model.AnimListener.OnHeavyHitFrame += HeavyAttackHitFrame;    // Event02: Heavy Action Hit Frame
        model.AnimListener.OnActionAnimOver += ActionAnimFinished;     // Event03: Action animation over (any)

        // Establish events with oneself
        OnLoseControl += CancelAction;
    }
    private void OnDisable()
    {
        if (Player != null)
        {
            Player.OnPlayerAction -= DoAction;
        }
        model.AnimListener.OnLightHitFrame -= LightAttackHitFrame;
        model.AnimListener.OnHeavyHitFrame -= HeavyAttackHitFrame;
        model.AnimListener.OnActionAnimOver -= ActionAnimFinished;

        // Unsubscribe from events with oneself
        OnLoseControl -= CancelAction;
    }

    private void Update()
    {
        GroundedCheck();
        JumpAndGravity(Player.GetPlayerJump());
        if (Player != null && Player.GetPlayerJumpReleased())
        {
            InterruptJump();
        }
        Vector2 moveVec = Player != null ? Player.GetPlayerMovement().normalized : Vector2.zero;
        Move(moveVec, speed);
        if (_hasControl)
            TurnFaceMoveDir();
        // else, you don't get to choose what direction you face!
        ApplyVelocity();

        // Player-Specific Cooldowns
        if (_doMagicRegen)
        {
            if (_magicRegenCooldown > 0)
            {
                _magicRegenCooldown -= Time.deltaTime;
            }
            else
            {
                RegenMagic();
            }
        }

        if (_dashCooldown > 0)
        {
            _dashCooldown -= Time.deltaTime;
        }
    }
    public void DoAction(ActionType type)
    {
        if (!_hasControl) return;
        if (_currAction == ActionType.NONE)
        {
            // Nothing in buffer, do attack
            _actionAnimFinished = false;

            // If this attack uses magic, verify you can actually do it.
            switch(type) {
                case ActionType.ATTACK_LIGHT:
                    LightAttack();
                    break;
                case ActionType.ATTACK_HEAVY:
                    HeavyAttack();
                    break;
                case ActionType.SPECIAL:
                    Special();
                    break;
                case ActionType.DASH:
                    // Only dash if off cooldown
                    if (_dashCooldown <= 0) Dash(); else return;
                    break;
            }
            _currAction = type;
            // The attack buffer routine should always be empty (or otherwise at the very last stages) when this is called, but just in case this will prevent a re-run
            _actionBufferRoutine = StartCoroutine(C_ActionBuffer());
        }
        else
        {
            // Action is currently happening. Store this data in the buffer.
            // Only do this if the hit frame has passed to avoid awkward long buffers
             _actionBuffer = type;
        }
    }

    private IEnumerator C_ActionBuffer()
    {
        // Wait for the current animation to be over, and if there is something in the Action Buffer then do that
        yield return new WaitUntil(() => _actionAnimFinished);
        if (_actionBuffer != ActionType.NONE)
        {
            // Clear the buffer (while storing the data) then attack with it
            ActionType temp = _actionBuffer;
            _actionBuffer = ActionType.NONE;
            // Reset current so it doesn't think this is another buffer call.
            _actionHistory.Add(_currAction);
            _currAction = ActionType.NONE;
            // Set self to be null, then recall
            DoAction(temp);
        }
        else
        {
            // Stop the attacking entirely.
            _currAction = ActionType.NONE;
            _actionHistory.Clear();
            model.Animator.SetInteger(_animActionID_I, (int)ActionType.NONE);
        }
    }
    /*
     * Only triggered if some game event happens, e.g. Being knocked prone or dying. Basically an emergency "oh shit we need to stop doing this NOW"
     */
    public void CancelAction()
    {
        // Cancelling an action counts as finishing it. Important for reverting variables
        OnActionFinished?.Invoke();
        // Stop the next attack from happening
        if (_actionBufferRoutine != null)
            StopCoroutine(_actionBufferRoutine);
        // Stop this attack from happening
        _currAction = ActionType.NONE;
        _actionBuffer = ActionType.NONE;
        _actionHistory.Clear();
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.NONE);
        // Panic cancel this animation
        model.Animator.SetTrigger(_animCancelAction_T);
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

    public void HeavyAttackEnd()
    {
        // Stop listening for self
        OnActionFinished -= HeavyAttackEnd;
        // Revert Variables
        _hasMovement = true;
    }

    // Override these by class-specific definitions
    public abstract void Special();
    public abstract bool CanAffordSpecial();
    public abstract void DashSpecial();
    public abstract bool CanAffordDashSpecial();

    public void Dash()
    {
        if (SpendMagicOnAction(dashMagicCost))
        {
            StartCoroutine(C_Dash());
        }
        else
        {
            ActionAnimFinished();
        }
    }

    private IEnumerator C_Dash()
    {
        ActionType actionOverride = ActionType.NONE;
        void OverrideAction(ActionType action)
        {
            actionOverride = action;
        }

        model.Animator.SetInteger(_animActionID_I, (int)ActionType.DASH);
        // Disable normal movement and manually set speed
        _doMagicRegen = false;
        _hasMovement = false;
        _canBeHit = false;
        _speed = _facingRight ? new(dashSpeed, 0) : new(-dashSpeed, 0);

        // For action overrides: Start listening for Player to turn this into
        // ALSO: Stop listening to the normal function for a bit, we don't want to Player buffer here
        if (Player != null)
        {
            Player.OnPlayerAction -= DoAction;
            Player.OnPlayerAction += OverrideAction;
        }

        float timer = 0f;
        while (timer < dashLength)
        {
            if (actionOverride == ActionType.ATTACK_LIGHT)
            {
                // Override this dash into a spin
                if (Player != null)
                {
                    Player.OnPlayerAction += DoAction;
                    Player.OnPlayerAction -= OverrideAction;
                }
                _doMagicRegen = true;
                PauseMagicRegenForTime(dashMagicRegenCooldown);
                SpinAttack();
                yield break;
            }
            else if (actionOverride == ActionType.ATTACK_HEAVY)
            {
                // Note: Normal cost function happens in Drill function, but need to check here to make sure the dash should indeed be ended.
                if (Magic < drillMagicCost)
                {
                    // No magic for override
                    actionOverride = ActionType.NONE;
                }
                else
                {
                    // Override this dash into a drill
                    if (Player != null)
                    {
                        Player.OnPlayerAction += DoAction;
                        Player.OnPlayerAction -= OverrideAction;
                    }
                    Drill();
                    yield break;
                }
            }
            else if (actionOverride == ActionType.SPECIAL)
            {
                if (!CanAffordDashSpecial())
                {
                    actionOverride = ActionType.NONE;
                }
                else
                {
                    // Override this dash into a Dash Special
                    if (Player != null)
                    {
                        Player.OnPlayerAction += DoAction;
                        Player.OnPlayerAction -= OverrideAction;
                    }
                    DashSpecial();
                    yield break;
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // Window for overrides is over
        if (Player != null)
        {
            Player.OnPlayerAction += DoAction;
            Player.OnPlayerAction -= OverrideAction;
        }

        // Dash over, return to normal parameters
        _hasMovement = true;
        _canBeHit = true;
        _doMagicRegen = true;
        PauseMagicRegenForTime(dashMagicRegenCooldown);

        ActionAnimFinished();
    }
    #region Spin Attack
    public void SpinAttack()
    {
        // Because this action is not triggered normally, action type must be set here
        _currAction = ActionType.ATTACK_SPIN;
        model.Animator.SetInteger(_animActionID_I, (int)ActionType.ATTACK_SPIN);
        _hasMovement = false;
        _canBeHit = false;
        // Do dash speeds as this happens
        _speed = _facingRight ? new(dashSpeed, 0) : new(-dashSpeed, 0);
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
    #endregion

    #region Drill Attack
    public void Drill()
    {
        if (SpendMagicOnAction(drillMagicCost))
        {
            // Because this action is not triggered normally, action type must be set here
            _currAction = ActionType.ATTACK_DRILL;
            model.Animator.SetInteger(_animActionID_I, (int)ActionType.ATTACK_DRILL);
            // Drill always knockbacks
            _attacksAlwaysKnockback = true;
            // Stop movement during a heavy attack
            _hasMovement = false;
            _canBeHit = false;
            _doMagicRegen = false;
            // Do dash speeds as this happens
            _speed = _facingRight ? new(dashSpeed, 0) : new(-dashSpeed, 0);
            // Revert variables when done
            OnActionFinished += DrillEnd;
        }
        else
        {
            ActionAnimFinished();
        }
    }

    public void DrillEnd()
    {
        // Stop listening for self
        OnActionFinished -= DrillEnd;
        // Revert Variables
        _hasMovement = true;
        _canBeHit = true;
        _doMagicRegen = true;
        _attacksAlwaysKnockback = false;
        PauseMagicRegenForTime(drillMagicRegenCooldown);
    }
    #endregion
    /*
     *  Triggered by Animation System during light attack swing animation
     */
    public void LightAttackHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(lightAttackDistance);
        foreach (var c in hits)
        {
            bool killedEnemy = c.Key.Character.Damage(lightAttackDamage, c.Value);
            if (!c.Key.Character.Grounded || killedEnemy || _attacksAlwaysKnockback)
            {
                c.Key.Character.Knockback(_facingRight, lightAttackAirborneKnockbackForce);
            }
            else
            {
                // Character Grounded and we didn't kill them. Apply small stun
                c.Key.Character.HurtStun();
            }
            Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + lightAttackDamage + " damage.");
        }
    }

    /*
    *  Triggered by Animation System during heavy attack swing animation
    */
    public void HeavyAttackHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(heavyAttackDistance);
        bool didDoubleHeavy = false;
        if (_actionHistory.Count > 0 && _actionHistory[^1] == ActionType.ATTACK_HEAVY)
        {
            // Combo success: Double Heavy
            didDoubleHeavy = true;
            _actionHistory.Clear();
        }
        foreach (var c in hits)
        {
            bool killedEnemy = c.Key.Character.Damage(heavyAttackDamage, c.Value);
            if (!c.Key.Character.Grounded)
            {
                c.Key.Character.Knockback(_facingRight, heavyAttackAirborneKnockbackForce * (didDoubleHeavy ? 1 : -1)); // Single Heavy knocks downwards
            }
            else if (didDoubleHeavy || killedEnemy || _attacksAlwaysKnockback)
            {
                // Did double heavy and grounded, or this blow killed something. Do knockback!
                c.Key.Character.Knockback(_facingRight, heavyAttackKnockbackForce);
            }
            else
            {
                // Character grounded, not dead, and we're not doing a combo. Small stun.
                c.Key.Character.HurtStun();
            }
            Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + heavyAttackDamage + " damage.");
        }
    }

    public void ActionAnimFinished()
    {
        _actionAnimFinished = true;
        OnActionFinished?.Invoke();
    }
    protected bool SpendMagicOnAction(float cost)
    {
        if (Magic < cost)
        {
            // Can't do this, not enough magic
            model.Animator.SetInteger(_animActionID_I, (int)ActionType.NONE);
            return false;
        }
        else
        {
            Magic -= cost;
            OnMagicChanged?.Invoke();
            return true;
        }
    }

    public void PauseMagicRegenForTime(float time)
    {
        _magicRegenCooldown = Mathf.Max(_magicRegenCooldown, time);
    }

    private void RegenMagic()
    {
        Magic += magicRegenRate + Time.deltaTime;
        if (Magic > MaxMagic) Magic = MaxMagic;
        OnMagicChanged?.Invoke();
    }
    
    public void AssignExtraAnimationIDs()
    {
        _animActionID_I = Animator.StringToHash("ActionID");
        _animCancelAction_T = Animator.StringToHash("CancelAction");
    }
}
