using UnityEngine;

public class GameUI : Singleton<GameUI>
{
    [SerializeField] private Transform playerUIRow;
    [SerializeField] private PlayerUI prefabPlayerUI;
    
    public PlayerUI AddPlayerUI(PlayerController character)
    {
        PlayerUI ui = Instantiate(prefabPlayerUI, playerUIRow);
        ui.Initialize(character);
        return ui;
    }
}
