using AdvancedSceneManager;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerManager : Singleton<PlayerManager>
{
    public delegate void PlayerJoinEvent(Player player);
    public event PlayerJoinEvent OnPlayerJoin;
    public event PlayerJoinEvent OnPlayerLeft;

    PlayerInputManager inputManager;
    private List<Player> players = new();
    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
    }

    public void PlayerJoin(PlayerInput playerInput)
    {
        Player player = playerInput.GetComponent<Player>();
        player.gameObject.name = $"Player {players.Count}";
        Debug.Log($"Player {player.gameObject.name} with settings {playerInput.user} has joined the game.");
        players.Add(player);
        OnPlayerJoin?.Invoke(player);
    }

    public void PlayerLeft(PlayerInput playerInput)
    {
        Player player = playerInput.GetComponent<Player>();
        Debug.Log($"Player {player.gameObject.name} with settings {playerInput.user} has left the game.");
        players.Remove(player);
        OnPlayerLeft?.Invoke(player);
    }
    public void SetAllPlayersActionMap(string map)
    {
        foreach (var player in players)
        {
            player.SetActionMap(map);
        }
    }
}
