using UnityEngine;

public class Coin : Item
{
    public override void OnPickUp(GameObject player)
    {
        //Debug.Log($"In Coin.cs {player}");
        //CharacterHitbox hitbox;
        //if (player.TryGetComponent(out hitbox))
        //{
        //    Debug.Log($"In Coin.cs {hitbox}");
        //    PlayerController p = hitbox.Character as PlayerController;
        //    if (p != null)
        //    {
        //        Debug.Log($"In Coin.cs {p}");
        //        p.Player.ChangeCoinAmount(1);
        //        Destroy(gameObject);
        //    }
        //}

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.Player.AddCoins(1);
            Destroy(gameObject);
        }
    
    }
}
