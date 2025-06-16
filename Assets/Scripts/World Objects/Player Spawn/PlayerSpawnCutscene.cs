using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public abstract class PlayerSpawnCutscene : MonoBehaviour
{
    public UnityEvent OnCutsceneStart;
    public UnityEvent OnCutsceneEnd;
    public abstract void StartCutscene();

    protected List<PlayerController> FindPlayers()
    {
        List<PlayerController> list = PlayerManager.Instance.Players.Select(p => p.Controller).ToList();
        if (list.Contains(null)) return null;
        return list;
    }
}
