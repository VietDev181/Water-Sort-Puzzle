using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioService : MonoBehaviour, IAudioService
{
    private const string BGM_KEY = "BGMVolume";
    private const string SFX_KEY = "SFXVolume";

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("Clips")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip gameBGM;
    [SerializeField] private AudioClip mainMenuBGM;
    [SerializeField] private AudioClip gameOverBGM;

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    void Awake()
    {
        LoadVolume();
    }

    public void PlayClickSFX()
    {
        if (clickClip != null)
        {
            sfxSource.PlayOneShot(clickClip);
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    public void PlayGameBGM()
    {
        bgmSource.clip = gameBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayMainMenuBGM()
    {
        bgmSource.clip = mainMenuBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlayGameOverBGM()
    {
        bgmSource.clip = gameBGM;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void SetBGMVolume(float value)
    {
        mixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(BGM_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    public float GetBGMVolume()
    {
        return PlayerPrefs.GetFloat(BGM_KEY, 1f);
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFX_KEY, 1f);
    }

    private void LoadVolume()
    {
        SetBGMVolume(GetBGMVolume());
        SetSFXVolume(GetSFXVolume());
    }
}
