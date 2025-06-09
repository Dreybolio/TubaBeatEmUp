using AdvancedSceneManager;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

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
        foreach (Player p in PlayerManager.Instance.Players)
        {
            p.OnUIConfirmPlayer -= AssignPlayerToSlot;
        }
    }

    private void Start()
    {
        foreach (Player p in PlayerManager.Instance.Players)
        {
            // Unassign all current player values if returning, as they should assume to start null
            p.CharacterData = null;
            // Scan for existing players in game and allow them to join without PlayerManager's OnPlayerJoin
            p.OnUIConfirmPlayer += AssignPlayerToSlot;
        }

        for (int i = 0; i < playerSelectors.Length; i++)
        {
            playerSelectors[i].AwaitConnection();
            playerSelectors[i].OnStatusChanged += OnSelectorStatusChanged;
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
                player.OnUIConfirmPlayer -= AssignPlayerToSlot;
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
                player.OnUIConfirmPlayer += AssignPlayerToSlot;
                break;
            }
        }
    }

    private void OnSelectorStatusChanged(PlayerSelector selector, bool ready)
    {
        if (ready)
        {
            // If any other selectors are hovering this character, then forcefully move them off this character
            for (int i = 0; i < playerSelectors.Length; i++)
            {
                if (playerSelectors[i] != selector)
                {
                    // For each selector that DIDN'T trigger this event
                    if (playerSelectors[i].CurrentSelected == selector.CurrentSelected)
                    {
                        // This is the same as the current selected, force it onto the next character
                        playerSelectors[i].SetAsSelected(GetNextCharacter(selector.CurrentSelected));
                    }
                }
            }

            // Try to start the game
            foreach (var player in PlayerManager.Instance.Players)
            {
                // Check if each player has an assigned character
                if (player.CharacterData == null)
                    return;
            }
            // All selected, move to game
            OnStartGame?.Invoke();
        }
    }

    public CharacterData GetNextCharacter(CharacterData character)
    {
        LinkedListNode<CharacterData> node = unlockedCharacters.Find(character).Next;
        if (node == null) node = unlockedCharacters.First;
        // If this character has already been selected by another, skip this
        Player[] alreadySelected = PlayerManager.Instance.Players.Where(c => c.CharacterData == node.Value).ToArray();
        if (alreadySelected.Length > 0)
        {
            // There already exists this character selected, skip this
            Debug.Log($"Character {alreadySelected[0].CharacterData.name} has already been selected!");
            return GetNextCharacter(node.Value);
        }
        return node.Value;
    }
    public CharacterData GetPreviousCharacter(CharacterData character)
    {
        LinkedListNode<CharacterData> node = unlockedCharacters.Find(character).Previous;
        if (node == null) node = unlockedCharacters.Last;
        // If this character has already been selected by another, skip this
        Player[] alreadySelected = PlayerManager.Instance.Players.Where(c => c.CharacterData == node.Value).ToArray();
        if (alreadySelected.Length > 0)
        {
            // There already exists this character selected, skip this
            return GetPreviousCharacter(node.Value);
        }
        return node.Value;
    }
}
