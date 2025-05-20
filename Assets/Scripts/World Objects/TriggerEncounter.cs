using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerEncounter : Trigger
{
    [Tooltip("Enemies that spawn once per player")]
    [SerializeField] private List<EnemySpawn> dynamicScaleEnemies;
    [Tooltip("Enemies that spawn once if there's one or two players, and twice if there's three or four")]
    [SerializeField] private List<EnemySpawn> dynamicHalfScaleEnemies;
    [Tooltip("Enemies that spwan only once in all cases")]
    [SerializeField] private List<EnemySpawn> staticEnemies;

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
        foreach (var enemy in dynamicScaleEnemies)
        {
            for (int i = 0; i < PlayerManager.Instance.Players.Count; i++)
            {
                Character c = Instantiate(enemy.prefab, enemy.spawnPos.position, Quaternion.identity);
                c.OnDeath += EnemyKilled;
                _totalEnemies++;
            }
        }
        foreach (var enemy in dynamicHalfScaleEnemies)
        {
            for (int i = 0; i < (PlayerManager.Instance.Players.Count + 1) / 2; i++)
            {
                Character c = Instantiate(enemy.prefab, enemy.spawnPos.position, Quaternion.identity);
                c.OnDeath += EnemyKilled;
                _totalEnemies++;
            }
        }
        foreach (var enemy in staticEnemies)
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