using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    private List<PlayerSpawnPoint> spawnPoints;
    public void LocateSpawnpoints()
    {
        PlayerSpawnPoint[] spawns = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        spawnPoints = spawns.OrderBy(s => s.PlayerID).ToList();
    }
    public void SpawnPlayers()
    {
        int i = 0;
        foreach (var player in PlayerManager.Instance.Players)
        {
            Vector3 spawnPoint = spawnPoints[i].transform.position;
            PlayerController physicalPlayer = player.SpawnPlayerController();
            if (physicalPlayer != null)
            {
                physicalPlayer.transform.position = spawnPoint;
            }
        }
    }
}
