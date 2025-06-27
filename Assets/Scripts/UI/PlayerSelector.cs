using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelector : MonoBehaviour
{
    [Header("Pointers")]
    [SerializeField] private Image statusImage;
    [SerializeField] private Image arrowLeftImage;
    [SerializeField] private Image arrowRightImage;
    [SerializeField] private TextMeshProUGUI textPlayerName;
    [SerializeField] private TextMeshProUGUI textCharacterName;
    [SerializeField] private TextMeshProUGUI textCharacterClass;
    [SerializeField] private TextMeshProUGUI textCharacterLevel;
    [SerializeField] private TextMeshProUGUI textPressToJoin;
    [SerializeField] private RectTransform modelParent;

    [Header("Pointers - External")]
    [SerializeField] private PlayerSelectorManager psManager;

    [Header("Sprites")]
    [SerializeField] private Sprite sprStatusReady;
    [SerializeField] private Sprite sprStatusNotReady;

    [Header("Audio")]
    [SerializeField] private AudioClip sfxConnect;
    [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private AudioClip sfxNavigate;

    private Coroutine flickerTextRoutine;
    public Player Player { get; private set; }
    public CharacterData CurrentSelected { get; private set; }
    private CharacterModel _displayModel;

    // Events
    public delegate void SelectorEvent(PlayerSelector selector, bool ready);
    public event SelectorEvent OnStatusChanged;

    public void OnDisable()
    {
        if (Player != null)
        {
            Player.OnUINavigate -= OnNavigate;
            Player.OnUIConfirm -= OnConfirm;
            Player.OnUICancel -= OnCancel;
        }
    }
    public void ConnectPlayer(Player player)
    {
        // Enable UI Elements
        textPlayerName.enabled = true;
        textCharacterName.enabled = true;
        textCharacterClass.enabled = true;
        textCharacterLevel.enabled = true;
        statusImage.enabled = true;
        arrowLeftImage.enabled = true;
        arrowRightImage.enabled = true;

        // Set text fields
        textPlayerName.text = player.name;

        if (flickerTextRoutine != null)
        {
            StopCoroutine(flickerTextRoutine);
            textPressToJoin.enabled = false;
        }
        Player = player;

        // Navigation and Confirming enabled
        Player.OnUINavigate += OnNavigate;
        Player.OnUIConfirm += OnConfirm;

        // Play Sound
        SoundManager.Instance.PlaySound(sfxConnect, 0.8f);
    }

    public void AwaitConnection()
    {
        // Disable UI Elements
        textPlayerName.enabled = false;
        textCharacterName.enabled = false;
        textCharacterClass.enabled = false;
        textCharacterLevel.enabled = false;
        statusImage.enabled = false;
        arrowLeftImage.enabled = false;
        arrowRightImage.enabled = false;

        // Start the press to join flashing
        if (flickerTextRoutine == null)
        {
            flickerTextRoutine = StartCoroutine(C_FlickerText(textPressToJoin, 0.60f));
        }
        Player = null;
    }
    public void OnNavigate(Vector2 vec)
    {
        if (vec.x > 0.5f)
        {
            CharacterData next = psManager.GetNextCharacter(CurrentSelected);
            SoundManager.Instance.PlaySound(sfxNavigate, 0.8f);
            SetAsSelected(next);
        }
        else if (vec.x < 0.5f)
        {
            CharacterData prev = psManager.GetPreviousCharacter(CurrentSelected);
            SoundManager.Instance.PlaySound(sfxNavigate, 0.8f);
            SetAsSelected(prev);
        }
    }

    public void SetAsSelected(CharacterData cd)
    {
        // Don't change selected if changing to what we already are.
        if (cd == CurrentSelected) return;

        if (_displayModel != null)
        {
            Destroy(_displayModel.gameObject);
        }
        _displayModel = Instantiate(cd.modelPrefab, modelParent);
        textCharacterName.text = cd.name;
        textCharacterClass.text = cd.description;
        textCharacterLevel.text = GameData.Instance.GetCharacterSaveData(cd).level.ToString();

        CurrentSelected = cd; 
    }

    public void OnConfirm()
    {
        Player.CharacterData = CurrentSelected;
        statusImage.sprite = sprStatusReady;
        arrowLeftImage.enabled = false;
        arrowRightImage.enabled = false;

        // Disable navigation, enable cancelling
        Player.OnUINavigate -= OnNavigate;
        Player.OnUIConfirm -= OnConfirm;
        Player.OnUICancel += OnCancel;

        SoundManager.Instance.PlaySound(sfxSelect, 0.8f);

        OnStatusChanged?.Invoke(this, true);
    }

    public void OnCancel()
    {
        Player.CharacterData = null;
        statusImage.sprite = sprStatusNotReady;
        arrowLeftImage.enabled = true;
        arrowRightImage.enabled = true;

        // Enable navigation, enable confirming
        Player.OnUINavigate += OnNavigate;
        Player.OnUIConfirm += OnConfirm;
        Player.OnUICancel -= OnCancel;

        OnStatusChanged?.Invoke(this, false);
    }

    private IEnumerator C_FlickerText(TextMeshProUGUI text, float interval)
    {
        while (true)
        {
            text.enabled = true;
            yield return new WaitForSeconds(interval);
            text.enabled = false;
            yield return new WaitForSeconds(interval);
        }
    }
}
