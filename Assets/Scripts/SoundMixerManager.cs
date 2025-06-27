using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    public void SetMasterVolume(float vol)
    {
        audioMixer.SetFloat("masterVolume", vol);
    }
    public void SetSFXVolume(float vol)
    {
        audioMixer.SetFloat("sfxVolume", vol);
    }
    public void SetMusicVolume(float vol)
    {
        audioMixer.SetFloat("musicVolume", vol);
    }
}
