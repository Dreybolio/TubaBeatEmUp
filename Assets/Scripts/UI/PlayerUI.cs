using System;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Pointers")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider magicSlider;
    [SerializeField] private Image playerIcon;

    public void SetHealth(float percent)
    {
        healthSlider.value = percent;
    }
    public void SetMagic(float percent)
    {
        magicSlider.value = percent;
    }
    public void Initialize(PlayerController player)
    {
        playerIcon.sprite = player.Icon;
        player.OnHurt += _ => SetHealth(player.Health / (float)player.MaxHealth);
        player.OnMagicChanged += () => SetMagic(player.Magic / player.MaxMagic);
    }
}
