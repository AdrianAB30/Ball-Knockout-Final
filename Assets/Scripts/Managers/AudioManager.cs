using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private AudioMixer myAudioMixer;

    [Header("UI Text References")]
    [SerializeField] private TMP_Text masterText;
    [SerializeField] private TMP_Text musicText;
    [SerializeField] private TMP_Text sfxText;

    [Header("Dotween Audio")]
    [SerializeField] private AudioSource[] backgroundAudios;
    [SerializeField] private float fadeDuration;
    [SerializeField] private float maxVolume;


    [Header("Visuals")]
    [SerializeField] private UIAnimationData uiAnimations;

    private int currentIndex = 0;

    private void Start()
    {
        LoadVolume();
        if (backgroundAudios.Length > 0)
        {
            PlayNextSong();
        }
    }

    public void LoadVolume()
    {
        ApplyVolume("MasterVolume", audioSettings.masterVolume, masterText);
        ApplyVolume("MusicVolume", audioSettings.musicVolume, musicText);
        ApplyVolume("SfxVolume", audioSettings.sfxVolume, sfxText);
    }

    public void ChangeMasterVolume(float amount)
    {
        audioSettings.masterVolume = Mathf.Clamp01(audioSettings.masterVolume + amount);
        ApplyVolume("MasterVolume", audioSettings.masterVolume, masterText);

        TriggerAnimation(masterText.gameObject);
    }

    public void ChangeMusicVolume(float amount)
    {
        audioSettings.musicVolume = Mathf.Clamp01(audioSettings.musicVolume + amount);
        ApplyVolume("MusicVolume", audioSettings.musicVolume, musicText);

        TriggerAnimation(musicText.gameObject);
    }

    public void ChangeSfxVolume(float amount)
    {
        audioSettings.sfxVolume = Mathf.Clamp01(audioSettings.sfxVolume + amount);
        ApplyVolume("SfxVolume", audioSettings.sfxVolume, sfxText);

        TriggerAnimation(sfxText.gameObject);
    }
    private void TriggerAnimation(GameObject textObj)
    {
        GameObject btn = EventSystem.current.currentSelectedGameObject;

        if (btn != null)
        {
            uiAnimations.AnimateButtonPunch(btn, btn.transform.localScale);
        }

        if (textObj != null)
        {
            uiAnimations.AnimateTextPop(textObj);
        }
    }

    private void ApplyVolume(string mixerParam, float volume, TMP_Text textUI)
    {
        float dbVolume = volume <= 0.001f ? -80f : Mathf.Log10(volume) * 20;
        myAudioMixer.SetFloat(mixerParam, dbVolume);

        if (textUI != null)
        {
            textUI.text = Mathf.RoundToInt(volume * 100) + "%";
        }
    }

    private void PlayNextSong()
    {
        if (currentIndex >= backgroundAudios.Length)
        {
            currentIndex = 0;
        }
        AudioSource currentAudio = backgroundAudios[currentIndex];
        currentAudio.volume = 0;
        currentAudio.Play();

        currentAudio.DOFade(maxVolume, fadeDuration).OnComplete(() =>
        {
            currentAudio.DOFade(0, fadeDuration).SetDelay(currentAudio.clip.length - fadeDuration).OnComplete(() =>
            {
                currentAudio.Stop();
                ++currentIndex;
                PlayNextSong();
            });
        });
    }
   
}