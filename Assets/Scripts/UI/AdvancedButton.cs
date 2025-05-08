using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AdvancedButton : MonoBehaviour
{
    [Header("Start State")]
    [SerializeField] protected new bool enabled = true;
    [Header("Pointers")]
    [SerializeField] protected Image background;
    [SerializeField] protected TextMeshProUGUI label;
    [SerializeField] protected Image icon;
    [SerializeField] protected Button button;
    [Header("Colours")]
    [SerializeField] protected Color COLOR_ENABLED;
    [SerializeField] protected Color COLOR_DISABLED;
    [Header("Sound")]
    [SerializeField] private AudioClip sndPress;
    public int id { get; protected set; }

    protected void Start()
    {
        if (enabled)
            Enable();
        else Disable();
        if (sndPress != null)
            button.onClick.AddListener(() => PressSound());
    }

    public void AddOnClick(UnityAction<AdvancedButton> action)
    {
        button.onClick.AddListener(() => action(this));
    }
    public void SetText(string text)
    {
        if (label != null)
            label.text = text;
    }
    public void SetID(int id)
    {
        this.id = id;
    }
    public void SetIcon(Sprite spr)
    {
        if (icon != null)
            icon.sprite = spr;
    }
    public void SetColor(Color color)
    {
        if (background != null) 
            background.color = color;
    }
    public void Enable()
    {
        enabled = true;
        button.enabled = true;
        SetColor(COLOR_ENABLED);
    }
    public void Disable()
    {
        enabled = false;
        button.enabled = false;
        SetColor(COLOR_DISABLED);
    }
    private void PressSound()
    {
        SoundManager.Instance.PlaySound(sndPress, .5f, false);
    }
}
