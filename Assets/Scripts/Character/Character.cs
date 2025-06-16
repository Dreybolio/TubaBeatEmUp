using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [Header("Movement Data")]
    [SerializeField] protected float walkSpeed = 3.0f;
    [SerializeField] protected float acceleration = 10.0f;
    [SerializeField] protected float airAcceleration = 3.0f;
    [SerializeField] protected float jumpHeight = 1.2f;
    [SerializeField] protected float gravityScale = -15.0f;

    [Header("Game Stats")]
    public int MaxHealth;

    [Header("Ground Detection")]
    [SerializeField] protected Transform groundCheckPos;
    [SerializeField] protected float groundCheckRadius = 0.28f;
    [SerializeField] protected LayerMask groundLayers;

    [Header("Hit Detection")]
    [SerializeField] protected Transform centerPoint;
    [SerializeField] protected Transform forwardXPoint;
    [SerializeField] protected Transform forwardZPoint;
    [SerializeField] protected LayerMask canAttackMask;
    [SerializeField] protected CharacterHitbox hitbox;

    [Header("Model & Animation")]
    [SerializeField] protected CharacterModel model;

    [Header("Audio")]
    [SerializeField] private AudioClip sndHurt;

    [Header("Prefabs")]
    [SerializeField] protected ParticleNumber prefabParticleNum; 

    // Events
    public delegate void CharacterEvent();
    public event CharacterEvent OnDeath;
    public event CharacterEvent OnRevive;
    public event CharacterEvent OnLoseControl;
    public event CharacterEvent OnHurtStun;

    public delegate void CharacterEventVal<T>(T i);
    public event CharacterEventVal<int> OnHurt;
    public event CharacterEventVal<int> OnHeal;


    // Pointers
    protected CharacterController controller;

    // Public Vars
    public bool Grounded { get; protected set; }
    public int Health { get; protected set; }
    [NonSerialized] public float TargetSpeed;
    [NonSerialized] public bool HasControl = true;
    [NonSerialized] public Vector2 Speed;
    public float DefaultSpeed => walkSpeed;
    public List<StatusEffect> StatusEffects { get; private set; }

    // Override Functions
    Action<bool, float> jumpGravityOverride;
    Action<Vector2> moveOverride;
    Action applyVelocityOverride;

    // Vars
    protected float _verticalVelocity;
    protected bool _facingRight = true;
    protected readonly float _terminalVelocity = 53.0f;
    protected bool _hasMovement = true;
    protected bool _doFaceMoveDir = true;
    protected bool _allowJump = true;
    protected bool _canBeHit = true;
    protected bool _stunImmunity = false;
    protected float _damageMult = 1.0f;
    protected Coroutine _knockbackRoutine;
    protected Coroutine _hurtStunRoutine;

    // Anim Layers
    private int _animOverrideLayer;

    // Anim IDs
    protected int _animSpeed_F, _animVertSpeed_F, _animFreefall_B, _animGrounded_B, _animProne_T, _animEndProne_T, _animHurt_T, _animEndHurt_T;

    // Abstract Functions

    protected void CharacterInit()
    {
        StatusEffects = new();
        controller = GetComponent<CharacterController>();
        Health = MaxHealth;
        _animOverrideLayer = model.Animator.GetLayerIndex("Override");
        AssignAnimationIDs();
    }

    public void OverrideJumpGravity(Action<bool, float> newJumpGrav)
    {
        // Call this with null to cancel
        if (jumpGravityOverride != null) Debug.LogWarning("Warning: Overrided Jump Gravity without clearing the previous override");
        jumpGravityOverride = newJumpGrav;
    }
    protected void JumpAndGravity(bool tryJump, float height)
    {
        if (jumpGravityOverride != null)
        {
            jumpGravityOverride.Invoke(tryJump, height);
            return;
        }

        // Animator: We are not jumping or falling
        model.Animator.SetBool(_animFreefall_B, false);
        if (Grounded)
        {
            // Stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }
            if (HasControl && _allowJump && tryJump) // If trying to jump
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(height * -2f * gravityScale);
            }
        }
        else
        {
            model.Animator.SetBool(_animFreefall_B, true);
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += gravityScale * Time.deltaTime;
        }
    }

    protected void InterruptJump()
    {
        if (!Grounded && _verticalVelocity > 0.0f)
        {
            // Is both moving upwards and midair, therefore we are jumping
            _verticalVelocity /= 4.0f;
        }
    }

    protected void GroundedCheck()
    {
        Grounded = Physics.CheckSphere(groundCheckPos.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
        model.Animator.SetBool(_animGrounded_B, Grounded);
    }

    public void OverrideMove(Action<Vector2> newMove)
    {
        // Call this with null to cancel
        if (moveOverride != null) Debug.LogWarning("Warning: Overrided Move without clearing the previous override");
        moveOverride = newMove;
    }
    protected void Move(Vector2 moveInput, float targetSpeed, bool doLerping = true)
    {
        if (moveOverride != null)
        {
            moveOverride.Invoke(moveInput);
            return;
        }
        Vector2 targetVector = moveInput * targetSpeed;
        Vector2 currentVector = new(controller.velocity.x, controller.velocity.z);
        if (HasControl && _hasMovement)
        {
            // Set goal speed
            if (doLerping && (Vector2.Distance(currentVector, targetVector) < -0.1f || Vector2.Distance(currentVector, targetVector) > 0.1f))
            {
                Speed = Vector2.Lerp(currentVector, targetVector, Time.deltaTime * (Grounded ? acceleration : airAcceleration));
                // Clamping is capped far above normal speed limits just to see if funny-ass tech arises
                Speed = Vector2.ClampMagnitude(Speed, TargetSpeed * 3);
                Speed.Set(Mathf.Round(Speed.x * 1000f) / 1000f, Mathf.Round(Speed.y * 1000f) / 1000f);
            }
            else
            {
                Speed = targetVector;
            }
        }
        //Animation
        // Return 0 - 1 on speed
        model.Animator.SetFloat(_animSpeed_F, Speed.magnitude / walkSpeed);
        model.Animator.SetFloat(_animVertSpeed_F, _verticalVelocity);
    }

    protected void TurnFaceMoveDir()
    {
        // Turn Around if needed
        if (_facingRight && Speed.x < 0)
        {
            _facingRight = false;
            transform.localScale = new Vector3(-1, 1, 1);
            hitbox.transform.localScale = new Vector3(-1, 1, 1);    // invert this too to prevent errors
        }
        else if (!_facingRight && Speed.x > 0)
        {
            _facingRight = true;
            transform.localScale = new Vector3(1, 1, 1);
            hitbox.transform.localScale = new Vector3(1, 1, 1);    // invert this too to prevent errors
        }
    }

    protected void TurnFaceTarget(Transform target)
    {
        if (_facingRight && target.position.x < transform.position.x)
        {
            _facingRight = false;
            transform.localScale = new Vector3(-1, 1, 1);
            hitbox.transform.localScale = new Vector3(-1, 1, 1);    // invert this too to prevent errors
        }
        else if (!_facingRight && target.position.x > transform.position.x)
        {
            _facingRight = true;
            transform.localScale = new Vector3(1, 1, 1);
            hitbox.transform.localScale = new Vector3(1, 1, 1);    // invert this too to prevent errors
        }
    }
    public void OverrideApplyVelocity(Action newApplyVelocity)
    {
        // Call this with null to cancel
        if (applyVelocityOverride != null) Debug.LogWarning("Warning: Overrided Apply Velocity without clearing the previous override");
        applyVelocityOverride = newApplyVelocity;
    }
    protected void ApplyVelocity()
    {
        if (applyVelocityOverride != null)
        {
            applyVelocityOverride.Invoke();
            return;
        }
        controller.Move(new Vector3(Speed.x, _verticalVelocity, Speed.y) * Time.deltaTime);
    }

    public void AddStatusEffect(StatusEffect effect)
    {
        if (effect == null) return;
        foreach (var existing in StatusEffects)
        {
            if (existing.GetType() == effect.GetType())
            {
                // This effect already exists in the list, so extend the timer to whichever timer is longer
                float longest = Mathf.Max(existing.Duration, effect.Duration);
                existing.Duration = longest;
                existing.Timer = 0;
                return;
            }
        }
        // This is a new status effect, add it.
        StatusEffects.Add(effect);
        effect.OnStart?.Invoke(this);
    }
    public void TickStatusEffects()
    {
        for (int i = 0; i < StatusEffects.Count; i++)
        {
            StatusEffect effect = StatusEffects[i];
            effect.OnTick?.Invoke(this);
            effect.Timer += Time.deltaTime;
            if (effect.Timer > effect.Duration)
            {
                // This effect is over
                effect.OnEnd?.Invoke(this);
                StatusEffects.RemoveAt(i);
            }
        }
    }

    public bool Damage(int amount, Vector3? hitPt = null)
    {
        if (!_canBeHit || Health <= 0) return false;
        Health -= amount;
        // Spawn a particle
        ParticleNumber particle = Instantiate(prefabParticleNum, (Vector3)(hitPt != null ? hitPt : transform.position), Quaternion.identity);
        particle.SetText(amount.ToString());
        SoundManager.Instance.PlaySound(sndHurt, 0.7f, true);
        OnHurt?.Invoke(amount);
        if (Health <= 0)
        {
            Health = 0;
            Die();
            return true;
        }
        return false;
    }

    public void Heal(int amount)
    {
        bool revived = Health <= 0;
        Health += amount;
        if (Health > MaxHealth)
        {
            Health = MaxHealth;
        }
        // Spawn a particle
        ParticleNumber particle = Instantiate(prefabParticleNum, transform.position, Quaternion.identity);
        particle.SetText(amount.ToString());
        SoundManager.Instance.PlaySound(sndHurt, 0.7f, true);
        OnHeal?.Invoke(amount);

        if (revived)
        {
            OnRevive?.Invoke();
            model.Animator.SetTrigger(_animEndProne_T);
            model.Animator.ResetTrigger(_animProne_T);
            HasControl = true;
            _canBeHit = true;
        }
    }

    public void Die()
    {
        // Events
        OnDeath?.Invoke();
        OnLoseControl?.Invoke();

        Debug.Log("Character " + name + " has died!");
        // Go prone, revoke all control
        model.Animator.ResetTrigger(_animEndProne_T);
        model.Animator.SetTrigger(_animProne_T);
        HasControl = false;
        _canBeHit = false;
        Speed = Vector2.zero;
    }

    protected IEnumerator C_BlinkOut()
    {
        yield return new WaitUntil(() => Grounded);
        yield return new WaitForSeconds(2.0f);
        for (int i = 0; i < 8; i++)
        {
            // Blink 8 times, then disappear
            model.SetVisible(true);
            yield return new WaitForSeconds(0.2f);
            model.SetVisible(false);
            yield return new WaitForSeconds(0.2f);
        }
        Destroy(gameObject);
    }

    public void Knockback(bool dirRight, float knockbackForce)
    {
        if (!_canBeHit || Health <= 0) return;
        if (_knockbackRoutine != null)
            StopCoroutine(_knockbackRoutine);
        _knockbackRoutine = StartCoroutine(C_Knockback(dirRight, knockbackForce));
    }

    private IEnumerator C_Knockback(bool dirRight, float knockbackForce)
    {
        // Trigger relevant event
        OnLoseControl?.Invoke();
        // Revoke control
        _hasMovement = false;
        HasControl = false;
        // Go flying
        model.Animator.ResetTrigger(_animEndProne_T);
        model.Animator.SetTrigger(_animProne_T);
        _verticalVelocity = Mathf.Sign(knockbackForce) * Mathf.Sqrt(Mathf.Abs(knockbackForce) * -2f * gravityScale);
        Speed = new(dirRight ? 2f : -2f, 0f);
        // Give some time to be knocked back
        yield return new WaitForSeconds(0.2f);
        // Wait until grounded again
        yield return new WaitUntil(() => Grounded);
        _canBeHit = false;
        if (knockbackForce >= 0.8f)
        {
            // If a big knock, then do a second mini-hop
            _verticalVelocity = Mathf.Sign(knockbackForce) * Mathf.Sqrt(Mathf.Abs(knockbackForce) / 4f * -2f * gravityScale);
            // Give some time to be knocked back
            yield return new WaitForSeconds(0.2f);
            // Wait until grounded again
            yield return new WaitUntil(() => Grounded);
        }
        // Now grounded. Stop all velocity. Invincible until unproned
        Speed = Vector2.zero;
        // Small stun time
        yield return new WaitForSeconds(0.5f);
        // Give control back
        if (Health <= 0)
        {
            // We're dead. Just end here. Don't get up.
            yield break;
        }
        model.Animator.ResetTrigger(_animProne_T);
        model.Animator.SetTrigger(_animEndProne_T);
        HasControl = true;
        _hasMovement = true;
        _canBeHit = true;
    }

    public void HurtStun()
    {
        if (_stunImmunity) return;
        if (Health <= 0) return; // Don't stun if dead
        if (_hurtStunRoutine != null)
            StopCoroutine(_hurtStunRoutine);
        _hurtStunRoutine = StartCoroutine(C_HurtStun());
    }

    private IEnumerator C_HurtStun()
    {
        // Trigger relevant event
        OnLoseControl?.Invoke();
        OnHurtStun?.Invoke();
        // Revoke control
        HasControl = false;
        Speed = Vector2.zero;
        // Hurt animation
        model.Animator.ResetTrigger(_animEndHurt_T);
        model.Animator.SetTrigger(_animHurt_T);
        // Wait for stun time
        yield return new WaitForSeconds(0.15f);
        if (Health <= 0)
        {
            // We're dead. Just end here. Don't get up.
            yield break;
        }
        model.Animator.ResetTrigger(_animHurt_T);
        model.Animator.SetTrigger(_animEndHurt_T);
        HasControl = true;
    }

    protected List<KeyValuePair<CharacterHitbox, Vector3>> ForwardAttackHitboxCollisions(Vector3 origin, Vector3 direction, float range)
    {
        Collider[] cols = Physics.OverlapSphere(origin, 0.01f, canAttackMask, QueryTriggerInteraction.Collide);
        // Check at the middle, top & bottom of the collider. Raycasts as follows:
        RaycastHit[] raysMid = Physics.RaycastAll(origin, direction, range, canAttackMask, QueryTriggerInteraction.Collide);
        RaycastHit[] rayTop = Physics.RaycastAll(origin + new Vector3(0f, controller.height / 2f, 0f), direction, range, canAttackMask, QueryTriggerInteraction.Collide);
        RaycastHit[] rayBottom = Physics.RaycastAll(origin + new Vector3(0f, -controller.height / 2f, 0f), direction, range, canAttackMask, QueryTriggerInteraction.Collide);
        RaycastHit[] rays = raysMid.Union(rayTop).Union(rayBottom).ToArray();

        var hits = new List<KeyValuePair<CharacterHitbox, Vector3>>();
        var processed = new HashSet<GameObject>();

        foreach (var col in cols)
        {
            if (col.CompareTag("Hitbox") && processed.Add(col.gameObject))
            {
                if (col.TryGetComponent(out CharacterHitbox hitbox))
                {
                    hits.Add(new(hitbox, origin));
                }
            }
        }
        foreach (var ray in rays)
        {
            var hitObject = ray.transform.gameObject;
            if (hitObject.CompareTag("Hitbox") && processed.Add(hitObject))
            {
                if (hitObject.TryGetComponent(out CharacterHitbox hitbox))
                {
                    hits.Add(new(hitbox, ray.point));
                }
            }
        }
        Debug.DrawLine(origin, origin + range * direction, Color.green, 0.5f);

        return hits;
    }
    protected List<KeyValuePair<CharacterHitbox, Vector3>> CircleAttackHitboxCollisions(Vector3 origin, float range)
    {
        // Do 8 rays (in each direction).
        List<RaycastHit> rays = new();
        rays.AddRange(Physics.RaycastAll(origin, transform.right, range, canAttackMask, QueryTriggerInteraction.Collide));    // +X
        rays.AddRange(Physics.RaycastAll(origin, -transform.right, range, canAttackMask, QueryTriggerInteraction.Collide));   // -X
        rays.AddRange(Physics.RaycastAll(origin, transform.forward, range, canAttackMask, QueryTriggerInteraction.Collide));  // +Z
        rays.AddRange(Physics.RaycastAll(origin, -transform.forward, range, canAttackMask, QueryTriggerInteraction.Collide)); // -Z
        rays.AddRange(Physics.RaycastAll(origin, (transform.right + transform.forward).normalized, range, canAttackMask, QueryTriggerInteraction.Collide));   // +X+Z
        rays.AddRange(Physics.RaycastAll(origin, -(transform.right + transform.forward).normalized, range, canAttackMask, QueryTriggerInteraction.Collide));  // -X-Z
        rays.AddRange(Physics.RaycastAll(origin, (-transform.right + transform.forward).normalized, range, canAttackMask, QueryTriggerInteraction.Collide));  // -X+Z
        rays.AddRange(Physics.RaycastAll(origin, (transform.right - transform.forward).normalized, range, canAttackMask, QueryTriggerInteraction.Collide));   // +X-Z

        Collider[] cols = Physics.OverlapSphere(origin, 0.01f, canAttackMask, QueryTriggerInteraction.Collide);

        var hits = new List<KeyValuePair<CharacterHitbox, Vector3>>();
        var processed = new HashSet<GameObject>();

        foreach (var col in cols)
        {
            if (col.CompareTag("Hitbox") && processed.Add(col.gameObject))
            {
                if (col.TryGetComponent(out CharacterHitbox hitbox))
                {
                    hits.Add(new(hitbox, origin));
                }
            }
        }
        foreach (var ray in rays)
        {
            var hitObject = ray.transform.gameObject;
            if (hitObject.CompareTag("Hitbox") && processed.Add(hitObject))
            {
                if (hitObject.TryGetComponent(out CharacterHitbox hitbox))
                {
                    hits.Add(new(hitbox, ray.point));
                }
            }
        }

        return hits;
    }

    protected void AssignAnimationIDs()
    {
        _animSpeed_F = Animator.StringToHash("Speed");
        _animVertSpeed_F = Animator.StringToHash("Vertical Speed");
        _animFreefall_B = Animator.StringToHash("Freefall");
        _animGrounded_B = Animator.StringToHash("Grounded");
        _animProne_T = Animator.StringToHash("Prone");
        _animEndProne_T = Animator.StringToHash("EndProne");
        _animHurt_T = Animator.StringToHash("Hurt");
        _animEndHurt_T = Animator.StringToHash("EndHurt");
    }

    protected void AnimatorOverrideSetEnabled(bool enabled)
    {
        for (int i = 0; i < model.Animator.layerCount; i++)
        {
            if (i == _animOverrideLayer)
            {
                model.Animator.SetLayerWeight(i, enabled ? 1 : 0);
            }
            else
            {
                model.Animator.SetLayerWeight(i, enabled ? 0 : 1);
            }
        }
    }
}
