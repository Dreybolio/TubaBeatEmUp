using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClassRogue: PlayerController
{
    [Header("Rogue")]
    [Header("Special - Attack Stats")]
    [SerializeField] private float specialMagicCost = 60.0f;
    [SerializeField] private float specialMagicRegenCooldown = 0.75f;
    [SerializeField] private float specialDistance = 1.75f;
    [SerializeField] private int specialDamage = 2;
    [SerializeField] private float specialAirborneKnockbackForce = 1.4f;

    [Header("Special - Movement Stats")]
    [SerializeField] private float specialSpeed = 4.5f;
    [SerializeField] private float specialAccerlation = 10.0f;
    [SerializeField] private float specialTurnClamp = 1.0f;

    [Header("Special - Spinner Ref")]
    [SerializeField] private Transform specialSpinRef;

    [Header("Dash Special - Attack Stats")]
    [SerializeField] private float dashSpecialMagicCost = 60.0f;
    [SerializeField] private float dashSpecialMagicRegenCooldown = 0.75f;
    [SerializeField] private float dashSpecialDistance = 2.0f;
    [SerializeField] private int dashSpecialDamage = 3;
    [SerializeField] private float dashSpecialKnockbackForce = 3.0f;

    [Header("Particles")]
    [SerializeField] private ParticleObject particleSlashPrefab;

    // Vars

    // Anim
    protected int _animSpecialTwirl_T;
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
        // Use the override controller
        AnimatorOverrideSetEnabled(true);
        OverrideMove(SpecialMove);
        model.Animator.SetTrigger(_animSpecialTwirl_T);
        _doMagicRegen = false;

        bool hitFramePassed = false;
        void HitFramePassed() { hitFramePassed = true; }

        bool prematureFinish = false;
        void PrematureFinish()
        {
            // Unsub from events now 
            model.AnimListener.OnSpecialHitFrame -= HitFramePassed;
            OnLoseControl -= PrematureFinish;
            model.AnimListener.OnEvent01 -= AnimFinished;

            // Reset vars
            _doMagicRegen = true;
            _doFaceMoveDir = true;
            _allowJump = true;
            model.Animator.ResetTrigger(_animSpecialTwirl_T);
            PauseMagicRegenForTime(specialMagicRegenCooldown);
            AnimatorOverrideSetEnabled(false);
            ActionAnimFinished();
            prematureFinish = true;
        }
        // Don't turn to face movedir while in this attack. Also, go much faster
        _doFaceMoveDir = false;
        _allowJump = false;

        model.AnimListener.OnSpecialHitFrame += HitFramePassed;
        OnLoseControl += PrematureFinish;
        // Do a slash every hit frame. Target 12 slashes
        int i = 0;
        do
        {
            if (prematureFinish) yield break;

            if (hitFramePassed)
            {
                // Do a slash - the angle of the spin reference will be the angle this attack happens in
                Vector3 dir = specialSpinRef.transform.right;
                dir.Normalize();
                SpecialHitFrame(dir);
                Vector3 particleSpawn = transform.position + dir + transform.up * 0.5f;
                print($"Rot: {Mathf.Rad2Deg * specialSpinRef.transform.rotation.y}");
                ParticleObject particle = Instantiate(particleSlashPrefab, particleSpawn, Quaternion.identity);
                particle.transform.right = dir;
                hitFramePassed = false;
                i++;
            }
            yield return null;
        }
        while (i < 12);

        bool animFinished = false;
        void AnimFinished() { animFinished = true; }

        model.AnimListener.OnEvent01 += AnimFinished;
        // NOTE TO SELF: A WaitUntil() doesn't work here because it needs to be interruptable.
        while (!animFinished)
        {
            if (prematureFinish) yield break;
            yield return null;
        }

        // Animation finished, move on
        model.AnimListener.OnEvent01 -= AnimFinished;
        model.AnimListener.OnSpecialHitFrame -= HitFramePassed;
        OnLoseControl -= PrematureFinish;

        // Reset Variables
        _doMagicRegen = true;
        _doFaceMoveDir = true;
        _allowJump = true;
        model.Animator.ResetTrigger(_animSpecialTwirl_T);
        OverrideMove(null);
        PauseMagicRegenForTime(specialMagicRegenCooldown);
        AnimatorOverrideSetEnabled(false);
        ActionAnimFinished();
    }
    public void SpecialHitFrame(Vector3 direction)
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(forwardXPoint.position, direction, specialDistance);
        foreach (var c in hits)
        {
            bool killedEnemy = c.Key.Character.Damage(specialDamage, c.Value);
            if (!c.Key.Character.Grounded || killedEnemy || _attacksAlwaysKnockback)
            {
                c.Key.Character.Knockback(_facingRight, specialAirborneKnockbackForce);
            }
            else
            {
                // Character Grounded and we didn't kill them. Apply small stun
                c.Key.Character.HurtStun();
            }
            // Debug.Log("Character " + name + " has hit Character " + c.Key.Character.name + " for " + specialDamage + " damage.");
        }
    }
    #endregion

    #region Special Movement Overrides
    // While special is in effect, this gets called instead
    private void SpecialMove(Vector2 moveInput)
    {
        // Move function that uses internal values.
        // Have slipperier acceleration and don't allow direction changing beyond a threshold
        Vector2 targetVector = moveInput * specialSpeed;
        Vector2 currentVector = new(controller.velocity.x, controller.velocity.z);
        if (_hasControl)
        {
            // Set goal speed
            if (Vector2.Distance(currentVector, targetVector) < -0.1f || Vector2.Distance(currentVector, targetVector) > 0.1f)
            {
                _speed = Vector2.Lerp(currentVector, targetVector, Time.deltaTime * specialAccerlation);
                _speed = Vector2.ClampMagnitude(_speed, specialSpeed);
                _speed.Set(Mathf.Round(_speed.x * 1000f) / 1000f, Mathf.Round(_speed.y * 1000f) / 1000f);
            }
            else
            {
                _speed = targetVector;
            }
        }
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

    public override bool CanAffordDashSpecial()
    {
        return Magic >= dashSpecialMagicCost;
    }

    private IEnumerator C_DashSpecial()
    {
        model.Animator.SetTrigger(_animCancelAction_T); // <-- Set a cancel trigger to interrupt the previous dash animation
        yield return null;
        ActionAnimFinished();
    }
    #endregion


    private void AssignClassAnimationIDs()
    {
        _animSpecialTwirl_T = Animator.StringToHash("Override_A");
    }
}
