using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [Header("Pointers")]
    [SerializeField] private AudioSource musicSource;
    [Header("Prefabs")]
    [SerializeField] private AudioSource sfxPrefab;

    private List<AudioSource> _sfxLoopingSources = new();

    public void PlaySound(AudioClip clip, float volume = 1, bool allowPitchVariance = false)
    {
        if (clip == null) return;
        float pitch = 1;
        if (allowPitchVariance)
        {
            pitch = Random.Range(0.90f, 1.10f);
        }

        AudioSource src = Instantiate(sfxPrefab, transform);
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.Play();

        Destroy(src.gameObject, clip.length);
    }
    public void PlaySound(AudioClip[] clips, float volume = 1, bool allowPitchVariance = false)
    {
        if (clips == null || clips.Length == 0) return;
        int rand = Random.Range(0, clips.Length);
        PlaySound(clips[rand], volume, allowPitchVariance);
    }

    public AudioSource PlaySoundLooping(AudioClip clip, float volume = 1, bool allowPitchVariance = false)
    {
        float pitch = 1;
        if (allowPitchVariance)
        {
            pitch = Random.Range(0.90f, 1.10f);
        }

        AudioSource src = Instantiate(sfxPrefab, transform);
        src.clip = clip;
        src.volume = volume;
        src.pitch = pitch;
        src.loop = true;
        src.Play();

        // Returns the created source so that the calling code can stop this themselves.
        return src;
    }

    public void StopSoundLooping(AudioSource src)
    {
        // Stop and destroy a passed-in source.
        if (src == null) return;
        _sfxLoopingSources.Remove(src);
        src.Stop();
        Destroy(src.gameObject);
    }

    public void StopSoundLooping(AudioClip clip)
    {
        // Stop and destroy all sources playing a certain clip
        var matches = _sfxLoopingSources
            .Where(src => src.clip == clip)
            .ToList();
        _sfxLoopingSources = _sfxLoopingSources
            .Except(matches)
            .ToList();
        foreach (var src in matches)
        {
            src.Stop();
            Destroy(src.gameObject);
        }
    }

    public void PlayMusic(AudioClip music, bool loop = true, float volume = 1)
    {
        musicSource.clip = music;
        musicSource.loop = loop;
        musicSource.volume = volume;
        musicSource.Play();
    }
    public void PlayMusicWithIntro(AudioClip intro, AudioClip music, float volume = 1)
    {
        StartCoroutine(C_PlayMusicWithIntro(intro, music, volume));
    }
    private IEnumerator C_PlayMusicWithIntro(AudioClip intro, AudioClip music, float volume = 1)
    {
        musicSource.volume = volume;

        musicSource.clip = intro;
        musicSource.loop = false;
        musicSource.Play();
        yield return new WaitForSeconds(intro.length);
        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.Play();
    }
    public void FadeOutMusic(float time)
    {
        StartCoroutine(C_FadeOutMusic(time));
    }
    private IEnumerator C_FadeOutMusic(float time)
    {
        float startVol = musicSource.volume;
        float timeElapsed = 0;
        while (timeElapsed < time)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0, timeElapsed / time);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        musicSource.Stop();
    }
}
