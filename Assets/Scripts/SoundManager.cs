using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [SerializeField] private AudioSource _musicSource, _sfxSource1, _sfxSource2, _sfxSource3;
    public void PlaySound(AudioClip clip, float volume = 1, bool allowPitchVariance = false)
    {
        float pitch = 1;
        if (allowPitchVariance)
        {
            pitch = Random.Range(0.90f, 1.10f);
        }
        // Tries each available SFX source, ignores if all are used.
        if (!_sfxSource1.isPlaying)
        {
            _sfxSource1.pitch = pitch;
            _sfxSource1.PlayOneShot(clip, volume);
            return;
        }
        if (!_sfxSource2.isPlaying)
        {
            _sfxSource2.pitch = pitch;
            _sfxSource2.PlayOneShot(clip, volume);
            return;
        }
        if (!_sfxSource3.isPlaying)
        {
            _sfxSource3.pitch = pitch;
            _sfxSource3.PlayOneShot(clip, volume);
            return;
        }
        // As an emergency, play make a new object for this
        AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }
    public void PlayMusic(AudioClip music, bool loop = true, float volume = 1)
    {
        _musicSource.clip = music;
        _musicSource.loop = loop;
        _musicSource.volume = volume;
        _musicSource.Play();
    }
    public void PlayMusicWithIntro(AudioClip intro, AudioClip music, float volume = 1)
    {
        StartCoroutine(C_PlayMusicWithIntro(intro, music, volume));
    }
    private IEnumerator C_PlayMusicWithIntro(AudioClip intro, AudioClip music, float volume = 1)
    {
        _musicSource.volume = volume;

        _musicSource.clip = intro;
        _musicSource.loop = false;
        _musicSource.Play();
        yield return new WaitForSeconds(intro.length);
        _musicSource.clip = music;
        _musicSource.loop = true;
        _musicSource.Play();
    }
    public void FadeOutMusic(float time)
    {
        StartCoroutine(C_FadeOutMusic(time));
    }
    private IEnumerator C_FadeOutMusic(float time)
    {
        float startVol = _musicSource.volume;
        float timeElapsed = 0;
        while (timeElapsed < time)
        {
            _musicSource.volume = Mathf.Lerp(startVol, 0, timeElapsed / time);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        _musicSource.Stop();
    }
    public static void SetMasterVolume(float volume)
    {
        AudioListener.volume = volume;
    }
    public static float GetMasterVolume()
    {
        return AudioListener.volume;
    }
}
