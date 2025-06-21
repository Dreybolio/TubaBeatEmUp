using System.Collections;
using UnityEngine;

public class StatusEffectDazed : StatusEffect
{
    private float _lastHalvedSpeed;
    private static ParticleObject ParticleStunnedPrefab {
        get
        {
            if (_particle == null)
            {
                // Load Prefab File (Assets/Prefabs/Particles/Resources/StunnedVFX.prefab)
                _particle = Resources.Load<ParticleObject>("StunnedVFX");
                if (_particle == null) Debug.LogError("Error: Failed to load asset at Resources/StunnedVFX");
            }
            return _particle;
        }
    }
    private static ParticleObject _particle;

    private ParticleObject particleStunnedInstance;

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
        particleStunnedInstance = GameObject.Instantiate(ParticleStunnedPrefab, c.transform);
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
        if (particleStunnedInstance != null)
        {
            particleStunnedInstance.DestroySelf();
        }
    }

}
