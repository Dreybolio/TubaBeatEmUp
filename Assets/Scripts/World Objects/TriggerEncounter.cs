using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEncounter : Trigger
{
    [SerializeField] private List<EnemySpawn> enemies;

    public UnityEvent OnEncounterFinished;
    private int _totalEnemies;
    private int _enemiesKilled;
    private void OnEnable()
    {
        OnTrigger.AddListener(StartEncounter);
    }
    private void OnDisable()
    {
        OnTrigger.RemoveListener(StartEncounter);
    }

    private void StartEncounter()
    {
        foreach (var enemy in enemies)
        {
            Character c = Instantiate(enemy.prefab, enemy.spawnPos.position, Quaternion.identity);
            c.OnDeath += EnemyKilled;
            _totalEnemies++;
        }
    }

    private void EnemyKilled()
    {
        _enemiesKilled++;
        if (_enemiesKilled >= _totalEnemies)
        {
            // Encounter is over
            OnEncounterFinished?.Invoke();
        }
    }
}

[Serializable]
public struct EnemySpawn
{
    public Character prefab;
    public Transform spawnPos;
}