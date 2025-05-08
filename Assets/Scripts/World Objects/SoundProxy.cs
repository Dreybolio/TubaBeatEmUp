using UnityEngine;

public class SoundProxy : MonoBehaviour
{
    [SerializeField] private AudioClip intro;
    [SerializeField] private new AudioClip audio;
    [SerializeField] private float volume = 0.7f;
    [SerializeField] private bool pitchVariance;
    [SerializeField] private bool loop;

    public void PlayMusic()
    {
        SoundManager.Instance.PlayMusic(audio, loop, volume);
    }

    public void PlayMusicWithIntro()
    {
        SoundManager.Instance.PlayMusicWithIntro(intro, audio, volume);
    }

    private void PlaySound()
    {
        SoundManager.Instance.PlaySound(audio, volume, pitchVariance);
    }
}
