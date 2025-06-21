using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    private List<PlayerSpawnPoint> spawnPoints;
    private PlayerSpawnCutscene cutscene;
    public void LocateSpawnpoints()
    {
        PlayerSpawnPoint[] spawns = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);
        spawnPoints = spawns.OrderBy(s => s.PlayerID).ToList();

        cutscene = FindAnyObjectByType<PlayerSpawnCutscene>();
    }
    public void SpawnPlayers()
    {
        if (spawnPoints == null || spawnPoints.Count < PlayerManager.Instance.Players.Count)
        {
            DeveloperConsole.Log("Error: Could not find enough PlayerSpawns to spawn all players!");
            return;
        }
        int i = 0;
        foreach (var player in PlayerManager.Instance.Players)
        {
            Vector3 spawnPoint = spawnPoints[i].transform.position;
            PlayerController physicalPlayer = player.SpawnPlayerController();
            if (physicalPlayer != null)
            {
                physicalPlayer.transform.position = spawnPoint;
                print($"spawnPoint: {spawnPoint}, playerPos: {player.Controller.transform.position}");
            }
        }
    }

    public void PlaySpawnCutscene()
    {
        if (cutscene == null) return;
        cutscene.StartCutscene();
    }
}
