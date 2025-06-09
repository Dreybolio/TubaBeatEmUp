using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class RevivePlayer : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private Slider slider;
    [SerializeField] private float idealPercent = 0.75f;
    [SerializeField] private float growthRate = 1.15f;
    [SerializeField] private float baseReturn = 0.75f;

    [NonSerialized] public Player ReviveTarget;

    public UnityEvent OnMinigameStart;
    public UnityEvent OnMinigameEnd;
    public UnityEvent OnMinigameCancel;

    private Player _player;
    private float _sliderValue = 0.01f;
    private bool _mingameActive = false;

    private void Start()
    {
        canvas.enabled = false;
    }
    public void StartMinigame(Player player)
    {
        if (ReviveTarget == null) return;
        // Register this player as the active player
        _player = player;
        if (!_mingameActive)
        {
            StartCoroutine(C_StartMinigame());
        }
    }
    private IEnumerator C_StartMinigame()
    {
        Debug.Log("Starting Minigame");
        _player.Controller.SetReviveAnim(true);
        bool cancelSignal = false;
        OnMinigameStart?.Invoke();
        canvas.enabled = true;
        _mingameActive = true;
        // Await the player's first interact input
        bool _interact = false;
        void Interact(Player _){ _interact = true; }
        void InteractEnd(Player _) { _interact = false; }

        void CancelMinigame(Player _)
        {
            Debug.Log("Cancelling Revive Minigame");
            _player.OnInteract -= Interact;
            _player.OnInteractEnd -= InteractEnd;
            _player.OnCancel -= CancelMinigame;
            _player.Controller.SetReviveAnim(false);
            _player = null;
            canvas.enabled = false;
            OnMinigameCancel?.Invoke();
            _mingameActive = false;
            cancelSignal = true;
        }

        _player.OnInteract += Interact;
        _player.OnInteractEnd += InteractEnd;
        _player.OnCancel += CancelMinigame;
        yield return new WaitUntil(() => _interact);
        Debug.Log("Received Interact");
        _player.OnInteract -= Interact;

        // Increase the slider bar
        _sliderValue = 0.01f;
        while (_interact && _sliderValue < 1.0f)
        {
            if (cancelSignal) yield break;
            _sliderValue += growthRate * Time.deltaTime;
            _sliderValue = Mathf.Clamp(_sliderValue, 0.01f, 1.0f);
            slider.value = _sliderValue;
            yield return null;
        }
        Debug.Log("Received Interact End");
        _player.OnCancel -= CancelMinigame;
        _player.OnInteractEnd -= InteractEnd;

        float percentOff = Mathf.Abs(idealPercent - _sliderValue);
        float percent = baseReturn - percentOff;
        ReviveTarget.Controller.Heal(
            Mathf.CeilToInt(ReviveTarget.Controller.MaxHealth * percent)
            );
        _player.Controller.SetReviveAnim(false);
        OnMinigameEnd?.Invoke();

        Destroy(gameObject);
    }
}
