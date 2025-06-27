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
    [SerializeField] private float specialSpeedAtMin = 4.5f;
    [SerializeField] private float specialSpeedAtMax = 6.0f;
    [SerializeField] private float specialTimeToMaxSpeed = 1.8f;
    [SerializeField] private float specialAccerlation = 10.0f;
    [SerializeField] private float specialTurnClamp = 1.0f;

    [Header("Special - Spinner Ref")]
    [SerializeField] private Transform specialSpinRef;

    [Header("Dash Special - Attack Stats")]
    [SerializeField] private float dashSpecialMagicCost = 60.0f;
    [SerializeField] private float dashSpecialMagicRegenCooldown = 0.75f;
    [SerializeField] private float dashSpecialDistance = 1.0f;
    [SerializeField] private int dashSpecialDamage = 3;
    [SerializeField] private float dashSpecialAirborneKnockbackForce = 1.4f;
    [SerializeField] private float dashSpecialDazedDuration = 4f;

    [Header("Dash Special - Movement Stats")]
    [SerializeField] private float dashSpecialSpeed = 6.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxSpinSwish;

    [Header("Particles")]
    [SerializeField] private ParticleObject particleSlashPrefab;

    // Vars
    private float _specialSpeed = 4.5f;
    private float _specialSpeedTimer = 1.8f;

    // Anim
    private int _animSpecialTwirl_T, _animSpecialSlide_T;
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
        model.Animator.ResetTrigger(_animCancelAction_T);
        model.Animator.SetTrigger(_animSpecialTwirl_T);
        _doMagicRegen = false;

        AudioSource spinAudioSrc = SoundManager.Instance.PlaySoundLooping(sfxSpinSwish, 0.7f);

        bool hitFramePassed = false;
        void HitFramePassed() { hitFramePassed = true; }

        bool prematureFinish = false;
        void PrematureFinish()
        {
            if (prematureFinish) return; // Don't ever do this twice
            Debug.Log("Rogue: Premature Finish of Special");
            // Unsub from events now 
            model.AnimListener.OnEvent01 -= AnimFinished;
            model.AnimListener.OnSpecialHitFrame -= HitFramePassed;
            OnLoseControl -= PrematureFinish;
            OnActionFinished -= PrematureFinish;

            //Stop Audio - Because we don't have access to the source in this function we stop the entire Audio Clip
            SoundManager.Instance.StopSoundLooping(spinAudioSrc);

            // Reset Variables
            _doMagicRegen = true;
            _doFaceMoveDir = true;
            _allowJump = true;
            model.Animator.ResetTrigger(_animSpecialTwirl_T);
            model.Animator.SetTrigger(_animCancelAction_T);
            OverrideMove(null);
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
        OnActionFinished += PrematureFinish;
        // Do a slash every hit frame. Target 12 slashes
        _specialSpeed = specialSpeedAtMin;
        _specialSpeedTimer = 0;

        float panicTimer = 5f;

        int i = 0;
        do
        {
            if (prematureFinish) yield break;
            SpawnParticle(ParticleID.DUST_PUFF);
            if (hitFramePassed)
            {
                // Do a slash - the angle of the spin reference will be the angle this attack happens in
                Vector3 dir = specialSpinRef.transform.right;
                dir.Normalize();
                SpecialHitFrame(dir);
                Vector3 particleSpawn = transform.position + dir + transform.up * 0.5f;
                ParticleObject particle = Instantiate(particleSlashPrefab, particleSpawn, Quaternion.identity);
                particle.transform.right = dir;
                hitFramePassed = false;
                panicTimer = 5f;
                i++;
            }
            // Increase speed according to timer progress
            _specialSpeed = Mathf.Lerp(specialSpeedAtMin, specialSpeedAtMax, _specialSpeedTimer / specialTimeToMaxSpeed);
            if (_specialSpeedTimer < specialTimeToMaxSpeed)
            {
                _specialSpeedTimer += Time.deltaTime;
            }

            // If something goes horribly wrong, this will at least prevent a softlock. Hopefully this doesn't happen.
            panicTimer -= Time.deltaTime;
            if (panicTimer < 0) PrematureFinish();

            yield return null;
        }
        while (i < 12);

        bool animFinished = false;
        void AnimFinished() { animFinished = true; }

        model.AnimListener.OnEvent01 += AnimFinished;
        // NOTE TO SELF: A WaitUntil() doesn't work here because it needs to be interruptable.
        panicTimer = 5f;
        while (!animFinished)
        {
            if (prematureFinish) yield break;
            panicTimer -= Time.deltaTime;
            if (panicTimer < 0) PrematureFinish();
            yield return null;
        }

        SoundManager.Instance.StopSoundLooping(spinAudioSrc);

        // Animation finished, move on
        model.AnimListener.OnEvent01 -= AnimFinished;
        model.AnimListener.OnSpecialHitFrame -= HitFramePassed;
        OnLoseControl -= PrematureFinish;
        OnActionFinished -= PrematureFinish;

        // Reset Variables
        _doMagicRegen = true;
        _doFaceMoveDir = true;
        _allowJump = true;
        model.Animator.ResetTrigger(_animSpecialTwirl_T);
        model.Animator.SetTrigger(_animCancelAction_T);
        OverrideMove(null);
        PauseMagicRegenForTime(specialMagicRegenCooldown);
        AnimatorOverrideSetEnabled(false);
        ActionAnimFinished();
    }
    public void SpecialHitFrame(Vector3 direction)
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(forwardXPoint.position, direction, dashSpecialDistance);
        foreach (var c in hits)
        {
            SoundManager.Instance.PlaySound(sfxLightHit, 1f, true);
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
        Vector2 targetVector = moveInput * _specialSpeed;
        Vector2 currentVector = new(controller.velocity.x, controller.velocity.z);
        if (targetVector != Vector2.zero)
        {
            // Clamp targetVector such that you may only rotate by a certain clamped value
            Vector2 targetDir = targetVector.normalized;
            Vector2 currDir = currentVector.normalized;

            float angle = Vector2.SignedAngle(currDir, targetDir);
            float maxStep = specialTurnClamp * Time.deltaTime;  // Max rotation per frame
            float turnAngle = Mathf.Clamp(angle, -maxStep, maxStep);
            Vector2 newDir = Quaternion.Euler(0, 0, turnAngle) * currDir; // Rotate as much as the maxStep and add that to the current direciton
            targetVector = newDir * targetVector.magnitude;
        }
        if (HasControl)
        {
            // Set goal speed
            if (Vector2.Distance(currentVector, targetVector) < -0.1f || Vector2.Distance(currentVector, targetVector) > 0.1f)
            {
                Speed = Vector2.Lerp(currentVector, targetVector, Time.deltaTime * specialAccerlation);
                Speed = Vector2.ClampMagnitude(Speed, _specialSpeed);
                Speed.Set(Mathf.Round(Speed.x * 1000f) / 1000f, Mathf.Round(Speed.y * 1000f) / 1000f);
            }
            else
            {
                Speed = targetVector;
            }
        }
    }
    #endregion

    #region Dash Special Attack
    public override void DashSpecial()
    {
        if (SpendMagicOnAction(dashSpecialMagicCost))
        {
            model.AnimListener.OnDashSpecialHitFrame += DashSpecialHitFrame;
            // Because this action is not triggered normally, action type must be set here
            AnimatorOverrideSetEnabled(true);
            model.Animator.ResetTrigger(_animCancelAction_T);
            model.Animator.SetTrigger(_animSpecialSlide_T);
            // Stop movement during a heavy attack
            _hasMovement = false;
            _canBeHit = false;
            _doMagicRegen = false;
            // Do dash speeds as this happens
            Speed = _facingRight ? new(dashSpecialSpeed, 0) : new(-dashSpecialSpeed, 0);
            // Revert variables when done
            OnActionFinished += DashSpecialEnd;
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


    public void DashSpecialEnd()
    {
        // Stop listening for self
        OnActionFinished -= DashSpecialEnd;
        model.AnimListener.OnDashSpecialHitFrame -= DashSpecialHitFrame;
        // Revert Variables
        _hasMovement = true;
        _canBeHit = true;
        _doMagicRegen = true;
        model.Animator.ResetTrigger(_animSpecialSlide_T);
        AnimatorOverrideSetEnabled(false);
        PauseMagicRegenForTime(dashSpecialMagicRegenCooldown);
    }

    public void DashSpecialHitFrame()
    {
        List<KeyValuePair<CharacterHitbox, Vector3>> hits = ForwardAttackHitboxCollisions(forwardXPoint.position, transform.right * transform.localScale.x, specialDistance);
        foreach (var c in hits)
        {
            SoundManager.Instance.PlaySound(sfxLightHit, 1f, true);
            bool killedEnemy = c.Key.Character.Damage(dashSpecialDamage, c.Value);
            if (!killedEnemy) c.Key.Character.AddStatusEffect(new StatusEffectDazed(dashSpecialDazedDuration));
            // Apply Status Effect
            if (!c.Key.Character.Grounded || killedEnemy || _attacksAlwaysKnockback)
            {
                c.Key.Character.Knockback(_facingRight, dashSpecialAirborneKnockbackForce);
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


    private void AssignClassAnimationIDs()
    {
        _animSpecialTwirl_T = Animator.StringToHash("Override_A");
        _animSpecialSlide_T = Animator.StringToHash("Override_B");
    }
}
