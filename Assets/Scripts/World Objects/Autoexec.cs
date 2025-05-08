using System;
using UnityEngine;
using UnityEngine.Events;

public class Autoexec : MonoBehaviour
{
    public UnityEvent OnStart;

    private void Start()
    {
        OnStart?.Invoke();
    }
}