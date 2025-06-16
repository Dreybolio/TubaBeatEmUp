using System.Collections;
using UnityEngine;

public class StatusEffectDazed : StatusEffect
{
    private float _lastHalvedSpeed;

    public StatusEffectDazed(float duration)
    {
        Duration = duration;
        OnStart += Start;
        OnTick += Tick;
        OnEnd += End;
    }
    public void Start(Character c)
    {
        Debug.Log("Applying Dazed Effect");
        c.TargetSpeed /= 2f;
        _lastHalvedSpeed = c.TargetSpeed;
    }
    public void Tick(Character c)
    {
        if (_lastHalvedSpeed != c.TargetSpeed)
        {
            Debug.Log("Updating Dazed Effect");
            c.TargetSpeed /= 2f;
            _lastHalvedSpeed = c.TargetSpeed;
        }
    }

    public void End(Character c)
    {
        Debug.Log("Ending Dazed Effect");
        c.TargetSpeed = c.DefaultSpeed;
    }

}
