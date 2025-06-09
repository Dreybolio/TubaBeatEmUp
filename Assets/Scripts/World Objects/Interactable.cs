using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    public bool onlyOnce = false;
    public UnityEvent<Player> OnInteract;

    private List<PlayerController> playersInArea = new();
    bool _interacted = false;

    private void OnDisable()
    {
        foreach (PlayerController player in playersInArea)
        {
            player.Player.OnInteract -= Interacted;
        }
    }

    public void ListenForPlayer(GameObject p)
    {
        PlayerController player;
        if (p.TryGetComponent(out player))
        {
            // This is indeed a player
            if (!playersInArea.Contains(player))
            {
                playersInArea.Add(player);
                player.Player.OnInteract += Interacted;
            }
        }
    }

    public void StopListeningForPlayer(GameObject p)
    {
        PlayerController player;
        if (p.TryGetComponent(out player))
        {
            // This is indeed a player
            if (playersInArea.Contains(player))
            {
                playersInArea.Remove(player);
                player.Player.OnInteract -= Interacted;
            }
        }
    }
    
    public void Interacted(Player player)
    {
        if (_interacted) return;

        OnInteract?.Invoke(player);
        if (onlyOnce)
        {
            _interacted = true;
            Destroy(gameObject, 0.25f);
        }
    }
}
