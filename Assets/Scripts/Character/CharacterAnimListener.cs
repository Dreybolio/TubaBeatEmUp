using UnityEngine;

public class CharacterAnimListener : MonoBehaviour
{
    public delegate void AnimationEvent();
    public event AnimationEvent OnLightHitFrame;
    public event AnimationEvent OnHeavyHitFrame;
    public event AnimationEvent OnSpecialHitFrame;
    public event AnimationEvent OnDashSpecialHitFrame;
    public event AnimationEvent OnActionAnimOver;

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
}
