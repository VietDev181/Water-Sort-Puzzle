using UnityEngine;

public interface IAudioService
{
    void PlayClickSFX();
    void PlayGameBGM();
    void PlayMainMenuBGM();
    void PlayGameOverBGM();

    void PlaySFX(AudioClip clip);
    void PlaySFX(AudioClip clip, float volumeScale);

    void SetBGMVolume(float value);
    void SetSFXVolume(float value);

    float GetBGMVolume();
    float GetSFXVolume();
}
