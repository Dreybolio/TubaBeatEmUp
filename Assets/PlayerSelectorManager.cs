using UnityEngine;

public class PlayerSelectorManager : MonoBehaviour
{
    [SerializeField] private PlayerSelector[] playerSelectors;

    private void OnEnable()
    {
        PlayerManager.Instance.OnPlayerJoin += AssignPlayerToSlot;
        PlayerManager.Instance.OnPlayerLeft += RemovePlayerFromSlot;
    }
    private void OnDisable()
    {
        PlayerManager.Instance.OnPlayerJoin -= AssignPlayerToSlot;
        PlayerManager.Instance.OnPlayerLeft -= RemovePlayerFromSlot;
    }

    public void AssignPlayerToSlot(Player player)
    {
        for (int i = 0; i < playerSelectors.Length; i++)
        {
            if (playerSelectors[i].Player == null)
            {
                playerSelectors[i].ConnectPlayer(player);
                break;
            }
        }
    }
    public void RemovePlayerFromSlot(Player player)
    {
        for (int i = 0; i < playerSelectors.Length; i++)
        {
            if (playerSelectors[i].Player == player)
            {
                playerSelectors[i].AwaitConnection();
                break;
            }
        }
    }
}
