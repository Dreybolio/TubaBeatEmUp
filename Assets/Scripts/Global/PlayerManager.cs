using AdvancedSceneManager;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class PlayerManager : Singleton<PlayerManager>
{
    public delegate void PlayerJoinEvent(Player player);
    public event PlayerJoinEvent OnPlayerJoin;
    public event PlayerJoinEvent OnPlayerLeft;

    PlayerInputManager inputManager;
    [NonSerialized] public List<Player> Players = new();

    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
    }

    public void PlayerJoin(PlayerInput playerInput)
    {
        if (playerInput == null) return;
        if (Players.Count > 4) return;
        Player player = playerInput.GetComponent<Player>();
        print(playerInput.name);
        player.gameObject.name = $"Player {Players.Count}";
        Debug.Log($"Player {player.gameObject.name} with settings {playerInput.user} has joined the game.");
        Players.Add(player);
        OnPlayerJoin?.Invoke(player);
    }

    public void PlayerLeft(PlayerInput playerInput)
    {
        if (playerInput == null) return;
        Player player = playerInput.GetComponent<Player>();
        Debug.Log($"Player {player.gameObject.name} with settings {playerInput.user} has left the game.");
        Players.Remove(player);
        OnPlayerLeft?.Invoke(player);
    }

    public void SetAllPlayersActionMap(string map)
    {
        foreach (var player in Players)
        {
            player.SetActionMap(map);
        }
    }
}
