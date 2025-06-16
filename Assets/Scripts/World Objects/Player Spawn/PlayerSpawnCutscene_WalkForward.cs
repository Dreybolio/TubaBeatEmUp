using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnCutscene_WalkForward : PlayerSpawnCutscene
{
    [SerializeField] private float walkTime = 1.5f;
    [SerializeField] private Vector2 walkDirection;

    private int _routinesFinished = 0;
    private int _routinesNeeded = 0;
    public override void StartCutscene()
    {
        if (walkDirection == Vector2.zero) return;
        List<PlayerController> players = FindPlayers();
        if (players == null) return;

        OnCutsceneStart?.Invoke();
        _routinesNeeded = players.Count;
        foreach (PlayerController player in players)
        {
            StartCoroutine(ForceWalkForward(player));
        }
    }

    private IEnumerator ForceWalkForward(PlayerController pc)
    {
        pc.HasControl = false;
        pc.Speed = walkDirection.normalized * pc.DefaultSpeed;
        float timer = 0;
        while (timer < walkTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        pc.HasControl = true;

        _routinesFinished++;
        if (_routinesFinished == _routinesNeeded)
        {
            OnCutsceneEnd?.Invoke();
        }
    }
}
