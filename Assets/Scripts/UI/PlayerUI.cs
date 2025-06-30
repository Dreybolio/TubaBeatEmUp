using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Pointers")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider magicSlider;
    [SerializeField] private TextMeshProUGUI coins;
    [SerializeField] private Image playerIcon;

    PlayerController _player;

    private void OnDisable()
    {
        if (_player != null)
        {
            _player.OnHurt -= HealthChange;
            _player.OnHeal -= HealthChange;
            _player.OnStaminaChanged -= MagicChange;
        }
    }
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
        _player = player;
        playerIcon.sprite = player.Player.CharacterData.icon;
        player.OnHurt += HealthChange;
        player.OnHeal += HealthChange;
        player.OnStaminaChanged += MagicChange;
        player.Player.OnCoinAmountChanged += CoinChange;
    }

    public void HealthChange(int _)
    {
        SetHealth(_player.Health / (float)_player.MaxHealth);
    }

    public void MagicChange(float _) 
    {
        SetMagic(_player.Stamina / _player.MaxStamina);
    }

    public void CoinChange(int _)
    {
        coins.text = _player.Player.CoinAmount.ToString();
    }
}
