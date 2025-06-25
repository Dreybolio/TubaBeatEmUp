using UnityEngine;

public class CharacterAnimListener : MonoBehaviour
{
    public delegate void AnimationEvent();
    public event AnimationEvent OnLightHitFrame;
    public event AnimationEvent OnHeavyHitFrame;
    public event AnimationEvent OnSpecialHitFrame;
    public event AnimationEvent OnDashSpecialHitFrame;
    public event AnimationEvent OnActionAnimOver;

    public delegate void AnimationEventVal<T>(T val);
    public event AnimationEventVal<ParticleID> OnSpawnParticle;

    // Special events that are generic purpose for some hard-coded animation event
    public event AnimationEvent OnEvent01;

    public void TriggerLightHitFrame()
    {
        OnLightHitFrame?.Invoke();
    }
    public void TriggerHeavyHitFrame()
    {
        OnHeavyHitFrame?.Invoke();
    }
    public void TriggerSpecialHitFrame()
    {
        OnSpecialHitFrame?.Invoke();
    }
    public void TriggerDashSpecialHitFrame()
    {
        OnDashSpecialHitFrame?.Invoke();
    }
    public void TriggerActionAnimOver()
    {
        OnActionAnimOver?.Invoke();
    }
    public void TriggerEvent01()
    {
        OnEvent01?.Invoke();
    }
    public void SpawnParticle(ParticleID pID)
    {
        OnSpawnParticle?.Invoke(pID);
    }
}
