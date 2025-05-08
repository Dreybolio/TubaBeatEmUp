using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSelector : MonoBehaviour
{
    [Header("Pointers")]
    [SerializeField] private Image statusImage;
    [SerializeField] private TextMeshProUGUI textPlayerName;
    [SerializeField] private TextMeshProUGUI textCharacterName;
    [SerializeField] private TextMeshProUGUI textCharacterClass;
    [SerializeField] private TextMeshProUGUI textCharacterLevel;
    [SerializeField] private TextMeshProUGUI textPressToJoin;


    [Header("Sprites")]
    [SerializeField] private Sprite sprStatusReady;
    [SerializeField] private Sprite sprStatusNotReady;

    private Coroutine flickerTextRoutine;
    public Player Player { get; private set; }

    public void ConnectPlayer(Player player)
    {
        // Enable UI Elements
        textPlayerName.enabled = true;
        textCharacterName.enabled = true;
        textCharacterClass.enabled = true;
        textCharacterLevel.enabled = true;
        statusImage.enabled = true;

        // Set text fields
        textPlayerName.text = player.name;

        if (flickerTextRoutine != null)
        {
            StopCoroutine(flickerTextRoutine);
        }
        Player = player;
    }

    public void AwaitConnection()
    {
        // Disable UI Elements
        textPlayerName.enabled = false;
        textCharacterName.enabled = false;
        textCharacterClass.enabled = false;
        textCharacterLevel.enabled = false;
        statusImage.enabled = false;

        // Start the press to join flashing
        if (flickerTextRoutine == null)
        {
            flickerTextRoutine = StartCoroutine(C_FlickerText(textPressToJoin, 0.60f));
        }
        Player = null;
    }

    public void OnNavigateLeft()
    {

    }

    public void OnNavigateRight()
    {

    }

    public void OnConfirm()
    {

    }

    public void OnCancel()
    {

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
