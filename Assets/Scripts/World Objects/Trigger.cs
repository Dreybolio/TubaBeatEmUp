using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class Trigger : MonoBehaviour
{
    [Header("Generic")]
    [SerializeField] protected LayerMask triggerableLayers;
    [SerializeField] protected bool triggerOnce = false;

    public UnityEvent<GameObject> OnTriggered;          // Invoke when a player enters this trigger
    public UnityEvent<GameObject> OnAllInTrigger;       // Invoke when all players currently in this trigger
    public UnityEvent<GameObject> OnAllTriggeredOnce;   // Invoke when all players have triggered this at least once
    public UnityEvent<GameObject> OnTriggeredExit;      // Invoke when a player exits this trigger

    private Dictionary<Player, bool> _inTrigger = new();
    private Dictionary<Player, bool> _hasTriggeredOnce = new();
    private bool _triggered = false;
    private bool _allTriggeredOnce = false;

    private void Start()
    {
        foreach (var player in PlayerManager.Instance.Players)
        {
            _inTrigger.Add(player, false);
            _hasTriggeredOnce.Add(player, false);
        }
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        int thisLayer = 1 << other.gameObject.layer;
        if ((triggerableLayers & thisLayer) != 0)
        {
            // A player has collided with this
            OnTriggered?.Invoke(other.gameObject);
            if (triggerOnce)
            {
                _triggered = true;
                Destroy(gameObject, 0.25f);
            }
            else
            {
                // "All Trigger" Player Checkers
                CharacterHitbox playerHitbox;
                if (other.gameObject.TryGetComponent(out playerHitbox))
                {
                    PlayerController player = playerHitbox.Character as PlayerController;
                    if (player != null) 
                    {
                        _inTrigger[player.Player] = true;
                        _hasTriggeredOnce[player.Player] = true;

                        if (!_inTrigger.Values.Contains(false))
                        {
                            Debug.LogWarning("NOTE: I am leaving Trigger Alls in a buggy state. Please fix them before using them!");
                            OnAllInTrigger?.Invoke(other.gameObject);
                        }
                        if (!_allTriggeredOnce && !_hasTriggeredOnce.Values.Contains(false))
                        {
                            _allTriggeredOnce = true;
                            OnAllTriggeredOnce?.Invoke(other.gameObject);
                        }
                    }
                }
            }

        }
    }

    protected void OnTriggerExit(Collider other)
    {
        if (_triggered) return;
        int thisLayer = 1 << other.gameObject.layer;
        if ((triggerableLayers & thisLayer) != 0)
        {
            // A player has collided with this
            OnTriggeredExit?.Invoke(other.gameObject);

            CharacterHitbox playerHitbox;
            if (other.gameObject.TryGetComponent(out playerHitbox))
            {
                PlayerController player = playerHitbox.Character as PlayerController;
                if (player != null)
                {
                    _inTrigger[player.Player] = false;
                }
            }
        }
    }
}
