using AdvancedSceneManager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerSelectorManager : MonoBehaviour
{
    [SerializeField] private PlayerSelector[] playerSelectors;

    private LinkedList<CharacterData> unlockedCharacters = new();

    // Trigger this event to start the game
    public UnityEvent OnStartGame;

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

    private void Start()
    {
        for (int i = 0; i < playerSelectors.Length; i++)
        {
            playerSelectors[i].AwaitConnection();
            playerSelectors[i].OnStatusChanged += TryStartGame;
        }
        foreach(var cha in GameData.Instance.GetUnlockedCharacters())
        {
            unlockedCharacters.AddLast(cha);
        }
    }

    public void AssignPlayerToSlot(Player player)
    {
        for (int i = 0; i < playerSelectors.Length; i++)
        {
            if (playerSelectors[i].Player == null)
            {
                // Get the first unselected character
                playerSelectors[i].SetAsSelected(GetNextCharacter(unlockedCharacters.Last.Value));
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

    private void TryStartGame()
    {
        foreach (var player in PlayerManager.Instance.Players)
        {
            // Check if each player has an assigned character
            if (player.CharacterData == null)
                return;
        }
        // All selected, move to game
        OnStartGame?.Invoke();
    }

    public CharacterData GetNextCharacter(CharacterData character)
    {
        LinkedListNode<CharacterData> node = unlockedCharacters.Find(character).Next;
        // If this character has already been selected by another, skip this
        Player[] alreadySelected = PlayerManager.Instance.Players.Where(c => c.CharacterData == character).ToArray();
        if (alreadySelected.Length > 0)
        {
            // There already exists this character selected, skip this
            return GetNextCharacter(alreadySelected[0].CharacterData);
        }
        return node != null ? node.Value : unlockedCharacters.First.Value;
    }
    public CharacterData GetPreviousCharacter(CharacterData character)
    {
        LinkedListNode<CharacterData> node = unlockedCharacters.Find(character).Previous;
        // If this character has already been selected by another, skip this
        Player[] alreadySelected = PlayerManager.Instance.Players.Where(c => c.CharacterData == character).ToArray();
        if (alreadySelected.Length > 0)
        {
            // There already exists this character selected, skip this
            return GetPreviousCharacter(alreadySelected[0].CharacterData);
        }
        return node != null ? node.Value : unlockedCharacters.Last.Value;
    }
}
