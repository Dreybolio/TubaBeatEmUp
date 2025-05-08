using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClassBarbarian : PlayerController
{
    [Header("Barbarian")]
    [Header("Special - Attack Stats")]
    [SerializeField] private float specialMagicCost = 60.0f;
    [SerializeField] private float specialMagicRegenCooldown = 0.75f;
    [SerializeField] private float specialDistance = 1.75f;
    [SerializeField] private int specialDamage = 2;
    [SerializeField] private float specialKnockbackForce = 1.1f;
    [SerializeField] private float specialAirborneKnockbackForce = 1.4f;
    [Header("Special - Movement Stats")]
    [SerializeField] private float specialRiseHeight = 1.5f;
    [SerializeField] private float specialRiseTime = 0.50f;
    [SerializeField] private float specialRiseHorizSpeed = 3f;
    [SerializeField] private float specialSlamHorizSpeed = 4.5f;
    [SerializeField] private float specialSlamVertSpeed = -3f;
    [SerializeField] private float specialNormalAcceleration = 3f;
    [SerializeField] private float specialFastAcceleration = 30f;
    [SerializeField] private float specialCustomGravity = -10f;
    [Header("Particles")]
    [SerializeField] private ParticleObject particleCircleSlashPrefab;

    // Vars
    private float _specialAccel;
    private float _specialTargetSpeed;

    // Anim
    protected int _animSpecialRise_T, _animSpecialSlam_T, _animSpecialRollOut_T;
    void Start()
    {
        AssignClassAnimationIDs();
        ControllerInit();
    }
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

    private IEnumerator C_Special()
    {
        // Only use the override layer for animation
        AnimatorOverrideSetEnabled(true);
        model.Animator.SetTrigger(_animSpecialRise_T);
        OverrideJumpGravity(SpecialJumpGravity);
        OverrideMove(SpecialMove);
        OverrideApplyVelocity(SpecialApplyVelocity);
        _doMagicRegen = false;
        float timer = 0f;
        if (Grounded)
        {
            // Rise for .50s
            _verticalVelocity = Mathf.Sqrt(specialRiseHeight * -2f * specialCustomGravity);
            while (timer < specialRiseTime)
            {
                // If in the first third of the rise, have extra acceleration
                _specialTargetSpeed = specialRiseHorizSpeed;
                _specialAccel = timer < (specialRiseTime / 3f) ? specialFastAcceleration : specialNormalAcceleration;

                _verticalVelocity += specialCustomGravity * Time.deltaTime;
            
                timer += Time.deltaTime;
                yield return null;
            }
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
        SpecialHitFrame();
        Vector3 particleSpawn = new(transform.position.x, transform.position.y + 0.1f, transform.position.z);
        Instantiate(particleCircleSlashPrefab, particleSpawn, Quaternion.identity);
        // Roll out of attack
        _hasControl = false;
        model.Animator.ResetTrigger(_animSpecialSlam_T);
        model.Animator.SetTrigger(_animSpecialRollOut_T);
        yield return new WaitForSeconds(0.30f);
        
        // Stop rolling, reset variables
        model.Animator.ResetTrigger(_animSpecialRollOut_T);
        _hasControl = true;

        // Set a magic regen cooldown
        _doMagicRegen = true;
        PauseMagicRegenForTime(specialMagicRegenCooldown);

        // Finish by indicating end of attack

        OverrideJumpGravity(null);
        OverrideMove(null);
        OverrideApplyVelocity(null);
        AnimatorOverrideSetEnabled(false);
        ActionAnimFinished();
    }

    public void SpecialHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = CircleAttackHitboxCollisions(specialDistance);
        foreach (var c in hits)
        {
            bool killedEnemy = c.Key.Character.Damage(specialDamage, c.Value);
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
            Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + specialDamage + " damage.");
        }
    }

    // While special is in effect, this gets called instead
    private void SpecialJumpGravity(bool tryJump)
    {
        // Do nothing, as we want no jumping and no gravity
    }
    private void SpecialMove(Vector2 moveInput)
    {
        // Move funciton that uses internal values.
        // Acceleration goes high when we first jump, and when we first start slamming. Once we touch down, lose all control.
        Vector2 targetVector = moveInput * _specialTargetSpeed;
        Vector2 currentVector = new(controller.velocity.x, controller.velocity.z);
        if (_hasControl)
        {
            // Set goal speed
            if (Vector2.Distance(currentVector, targetVector) < -0.1f || Vector2.Distance(currentVector, targetVector) > 0.1f)
            {
                _speed = Vector2.Lerp(currentVector, targetVector, Time.deltaTime * _specialAccel);
                _speed = Vector2.ClampMagnitude(_speed, _specialTargetSpeed);
                _speed.Set(Mathf.Round(_speed.x * 1000f) / 1000f, Mathf.Round(_speed.y * 1000f) / 1000f);
            }
            else
            {
                _speed = targetVector;
            }
        }
    }

    // While special is in effect, this gets called instead
    private void SpecialApplyVelocity()
    {
        print("Apply Velocity");
        // Apply internal velocity values
        controller.Move(new Vector3(_speed.x, _verticalVelocity, _speed.y) * Time.deltaTime);
    }

    private void AssignClassAnimationIDs()
    {
        _animSpecialRise_T = Animator.StringToHash("Override_A");
        _animSpecialSlam_T = Animator.StringToHash("Override_B");
        _animSpecialRollOut_T = Animator.StringToHash("Override_C");
    }
}
