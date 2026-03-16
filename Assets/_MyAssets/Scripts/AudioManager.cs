using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] AudioSource ambientSource, effectsSource, musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void PlaySfx(AudioClip clip, float volume = 1.0f)
    {
        if (effectsSource == null || clip == null)
        {
            return;
        }

        effectsSource.PlayOneShot(clip, volume);
    }

    public void PlaySfxSimple(AudioClip clip)
    {
        PlaySfx(clip, 1.0f);
    }

    public void PlaySfxWaitForComplete(AudioClip clip)
    {
        if (clip != null)
        {
            StartCoroutine(PlaySfxAndSignalComplete(clip));
        }
    }

    private IEnumerator PlaySfxAndSignalComplete(AudioClip clip)
    {
        PlaySfx(clip, 1.0f);
        yield return new WaitForSeconds(clip.length);
        DialogueManager.Instance.CompleteDialogueEvent();
    }

    public void SetAmbientVolume(float volume)
    {
        if (ambientSource != null)
        {
            ambientSource.volume = volume;
        }
    }

    public void FadeAmbientVolume(float targetVolume, float duration)
    {
        if (ambientSource != null)
        {
            StartCoroutine(FadeVolumeCoroutine(ambientSource, targetVolume, duration));
        }
    }

    public void FadeInAmbientVolume(float duration)
    {
        FadeAmbientVolume(1.0f, duration);
    }

    public void FadeOutAmbientVolume(float duration)
    {
        FadeAmbientVolume(0.0f, duration);
    }

    public void FadeAmbientVolumeSimple(float targetVolume)
    {
        FadeAmbientVolume(targetVolume, 2f);
    }

    IEnumerator FadeVolumeCoroutine(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float timer = 0.0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }

        source.volume = targetVolume;
    }

    public void ChangeAmbienceClip(AudioClip newClip)
    {
        if (ambientSource != null)
        {
            ambientSource.clip = newClip;
            ambientSource.Play();
        }
    }
}
