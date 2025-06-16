using System;
using UnityEngine;

public abstract class StatusEffect
{
    public float Duration;
    public float Timer = 0;

    public Action<Character> OnStart;
    public Action<Character> OnTick;
    public Action<Character> OnEnd;

}