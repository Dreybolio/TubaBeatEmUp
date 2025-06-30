using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClassBarbarian : PlayerController
{
    [Header("Barbarian")]
    [Header("Special - Attack Stats")]
    [SerializeField] private float specialStaminaCost = 60.0f;
    [SerializeField] private float specialStaminaRegenCooldown = 0.75f;
    [SerializeField] private float specialDistance = 1.75f;
    [SerializeField] private int specialDamageBase = 2;
    [SerializeField] private float specialKnockbackForce = 1.1f;
    [SerializeField] private float specialAirborneKnockbackForce = 1.4f;
    public int SpecialDamage { get; private set; }

    [Header("Special - Movement Stats")]
    [SerializeField] private float specialRiseHeight = 1.5f;
    [SerializeField] private float specialRiseTime = 0.50f;
    [SerializeField] private float specialRiseHorizSpeed = 3f;
    [SerializeField] private float specialSlamHorizSpeed = 4.5f;
    [SerializeField] private float specialSlamVertSpeed = -3f;
    [SerializeField] private float specialNormalAcceleration = 3f;
    [SerializeField] private float specialFastAcceleration = 30f;
    [SerializeField] private float specialCustomGravity = -10f;

    [Header("Dash Special - Attack Stats")]
    [SerializeField] private float dashSpecialStaminaCost = 60.0f;
    [SerializeField] private float dashSpecialStaminaRegenCooldown = 0.75f;
    [SerializeField] private float dashSpecialDistance = 2.0f;
    [SerializeField] private int dashSpecialDamageBase = 3;
    [SerializeField] private float dashSpecialKnockbackForce = 3.0f;
    public int DashSpecialDamage { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioClip sfxRise;
    [SerializeField] private AudioClip sfxSlam;

    [Header("Particles")]
    [SerializeField] private ParticleObject particleCircleSlashPrefab;

    // Vars
    private float _specialAccel;
    private float _specialTargetSpeed;

    // Anim
    protected int _animSpecialRise_T, _animSpecialSlam_T, _animSpecialRollOut_T, _animSpecialBash_T, _animSpecialBashRecovery_T;
    void Start()
    {
        AssignClassAnimationIDs();
        ControllerInit();
    }

    #region Special Attack
    public override void Special()
    {
        if (SpendStaminaOnAction(specialStaminaCost))
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
        return Stamina >= specialStaminaCost;
    }

    private IEnumerator C_Special()
    {
        // Only use the override layer for animation
        AnimatorOverrideSetEnabled(true);
        model.Animator.ResetTrigger(_animCancelAction_T);
        model.Animator.SetTrigger(_animSpecialRise_T);
        OverrideJumpGravity(SpecialJumpGravity);
        OverrideMove(SpecialMove);
        OverrideApplyVelocity(SpecialApplyVelocity);
        _doStaminaRegen = false;
        float timer = 0f;
        if (Grounded)
        {
            // Rise for .50s
            SpawnParticle(ParticleID.DUST_PUFF);
            _verticalVelocity = Mathf.Sqrt(specialRiseHeight * -2f * specialCustomGravity);
            SoundManager.Instance.PlaySound(sfxRise, 0.5f, true);
            while (timer < specialRiseTime)
            {
                // If in the first third of the rise, have extra acceleration
                _specialTargetSpeed = specialRiseHorizSpeed;
                _specialAccel = timer < (specialRiseTime / 3f) ? specialFastAcceleration : specialNormalAcceleration;

                _verticalVelocity += specialCustomGravity * Time.deltaTime;
            
                timer += Time.deltaTime;
                yield return null;
            }
            // Reached jump apex, pause midair for a few frames

            timer = 0f;
            while (timer < 0.1f)
            {
                _verticalVelocity = Mathf.Lerp(_verticalVelocity, 0, Time.deltaTime * _specialAccel); 
                timer += Time.deltaTime;
                yield return null;
            }
            // Slam down
            model.Animator.ResetTrigger(_animSpecialRise_T);
        }

        model.Animator.SetTrigger(_animSpecialSlam_T);
        timer = 0f;
        do
        {
            _specialTargetSpeed = specialSlamHorizSpeed;
            _specialAccel = timer < (0.25f) ? specialFastAcceleration : specialNormalAcceleration;
            _verticalVelocity = specialSlamVertSpeed;
            timer += Time.deltaTime;
            yield return null;
        }
        while (!Grounded);
        // No longer grounded, move on
        // Do shockwave
        SoundManager.Instance.PlaySound(sfxSlam, 0.5f, true);
        SpecialHitFrame();
        Vector3 particleSpawn = new(transform.position.x, transform.position.y + 0.1f, transform.position.z);
        Instantiate(particleCircleSlashPrefab, particleSpawn, Quaternion.identity);
        // Roll out of attack
        HasControl = false;
        model.Animator.ResetTrigger(_animSpecialSlam_T);
        model.Animator.SetTrigger(_animSpecialRollOut_T);
        yield return new WaitForSeconds(0.30f);
        
        // Stop rolling, reset variables
        model.Animator.ResetTrigger(_animSpecialRollOut_T);
        HasControl = true;

        // Set a magic regen cooldown
        _doStaminaRegen = true;
        PauseStaminaRegenForTime(specialStaminaRegenCooldown);

        // Finish by indicating end of attack

        OverrideJumpGravity(null);
        OverrideMove(null);
        OverrideApplyVelocity(null);
        AnimatorOverrideSetEnabled(false);
        ActionAnimFinished();
    }

    public void SpecialHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = CircleAttackHitboxCollisions(centerPoint.position, specialDistance);
        foreach (var c in hits)
        {
            SoundManager.Instance.PlaySound(sfxHeavyHit, 1f, true);
            bool killedEnemy = c.Key.Character.Damage(SpecialDamage, c.Value);
            bool knockRight = c.Key.transform.position.x > transform.position.x;
            if (!c.Key.Character.Grounded)
            {
                c.Key.Character.Knockback(knockRight, specialAirborneKnockbackForce);
            }
            else
            {
                // Character Grounded. Do regular knockback
                c.Key.Character.Knockback(knockRight, specialKnockbackForce);
            }
            //Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + specialDamage + " damage.");
        }
    }
    #endregion

    #region Special Movement Overrides
    // While special is in effect, this gets called instead
    private void SpecialJumpGravity(bool tryJump, float height)
    {
        // Do nothing, as we want no jumping and no gravity
    }
    private void SpecialMove(Vector2 moveInput)
    {
        // Move funciton that uses internal values.
        // Acceleration goes high when we first jump, and when we first start slamming. Once we touch down, lose all control.
        Vector2 targetVector = moveInput * _specialTargetSpeed;
        Vector2 currentVector = new(controller.velocity.x, controller.velocity.z);
        if (HasControl)
        {
            // Set goal speed
            if (Vector2.Distance(currentVector, targetVector) < -0.1f || Vector2.Distance(currentVector, targetVector) > 0.1f)
            {
                Speed = Vector2.Lerp(currentVector, targetVector, Time.deltaTime * _specialAccel);
                Speed = Vector2.ClampMagnitude(Speed, _specialTargetSpeed);
                Speed.Set(Mathf.Round(Speed.x * 1000f) / 1000f, Mathf.Round(Speed.y * 1000f) / 1000f);
            }
            else
            {
                Speed = targetVector;
            }
        }
    }

    // While special is in effect, this gets called instead
    private void SpecialApplyVelocity()
    {
        // Apply internal velocity values
        controller.Move(new Vector3(Speed.x, _verticalVelocity, Speed.y) * Time.deltaTime);
    }
    #endregion

    #region Dash Special Attack
    public override void DashSpecial()
    {
        if (SpendStaminaOnAction(dashSpecialStaminaCost))
        {
            StartCoroutine(C_DashSpecial());
        }
        else
        {
            ActionAnimFinished();
        }
    }

    public override bool CanAffordDashSpecial()
    {
        return Stamina >= dashSpecialStaminaCost;
    }

    private IEnumerator C_DashSpecial()
    {
        // Use the override controller
        model.Animator.SetTrigger(_animCancelAction_T); // <-- Set a cancel trigger to interrupt the previous dash animation
        AnimatorOverrideSetEnabled(true);
        model.Animator.ResetTrigger(_animCancelAction_T);
        model.Animator.SetTrigger(_animSpecialBash_T);
        _doStaminaRegen = false;
        _hasMovement = false;
        _canBeHit = false;
        // Do dash speeds as this happens
        Speed = _facingRight ? new(dashSpeed, 0) : new(-dashSpeed, 0);

        bool hitFramePassed = false;
        void HitFramePassed()
        {
            hitFramePassed = true;
        }
        model.AnimListener.OnDashSpecialHitFrame += HitFramePassed;

        yield return new WaitUntil(() => hitFramePassed);

        model.AnimListener.OnDashSpecialHitFrame -= HitFramePassed;

        yield return new WaitUntil(() => Grounded);

        // Hit frame passed and grounded, do attack now.
        Speed = Vector2.zero;
        Vector3 particleSpawn = forwardXPoint.position;
        Instantiate(particleCircleSlashPrefab, particleSpawn, Quaternion.identity);
        SoundManager.Instance.PlaySound(sfxSlam, 0.5f, true);
        DashSpecialHitFrame();

        // Start recovery animation
        model.Animator.ResetTrigger(_animSpecialBash_T);
        model.Animator.SetTrigger(_animSpecialBashRecovery_T);
        yield return new WaitForSeconds(1.0f);

        // Reset Variables
        model.Animator.ResetTrigger(_animSpecialBashRecovery_T);
        HasControl = true;
        _doStaminaRegen = true;
        _hasMovement = true;
        _canBeHit = true;
        PauseStaminaRegenForTime(dashSpecialStaminaRegenCooldown);
        AnimatorOverrideSetEnabled(false);
        model.Animator.ResetTrigger(_animCancelAction_T);
        ActionAnimFinished();
    }

    public void DashSpecialHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = CircleAttackHitboxCollisions(forwardXPoint.position, dashSpecialDistance);
        foreach (var c in hits)
        {
            SoundManager.Instance.PlaySound(sfxHeavyHit, 1f, true);
            bool killedEnemy = c.Key.Character.Damage(DashSpecialDamage, c.Value);
            bool knockRight = c.Key.transform.position.x > forwardXPoint.position.x;
            c.Key.Character.Knockback(knockRight, dashSpecialKnockbackForce);

            // Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + dashSpecialDamage + " damage.");
        }
    }
    #endregion

    protected override void CalculateLevelledClassStats()
    {
        // Base: 2, Added Per Level: ~ 1.32
        SpecialDamage = Mathf.FloorToInt(specialDamageBase + Player.Level / 10 + Player.AttackLevel * 1.20f);
        // Base: 3, Added Per Level: ~1.43
        DashSpecialDamage = Mathf.FloorToInt(dashSpecialDamageBase + Player.Level / 10 + Player.AttackLevel * 1.25f);
    }

    private void AssignClassAnimationIDs()
    {
        _animSpecialRise_T = Animator.StringToHash("Override_A");
        _animSpecialSlam_T = Animator.StringToHash("Override_B");
        _animSpecialBash_T = Animator.StringToHash("Override_C");
        _animSpecialBashRecovery_T = Animator.StringToHash("Override_D");
        _animSpecialRollOut_T = Animator.StringToHash("Override_E");
    }
}
