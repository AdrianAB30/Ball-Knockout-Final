using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;

public class AudioManager : PersistentSingleton<AudioManager>
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSettings audioSettings;
    [SerializeField] private AudioMixer myAudioMixer;

    [Header("UI Text References")]
    [SerializeField] private TMP_Text masterText;
    [SerializeField] private TMP_Text musicText;
    [SerializeField] private TMP_Text sfxText;

    [Header("Music System (Dotween)")]
    [SerializeField] private AudioSource[] backgroundAudios;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float maxVolume = 1f;

    [Header("Audio Libraries (SOs)")]
    [SerializeField] private UIAudioLibrary uiLibrary;
    [SerializeField] private GameplayAudioLibrary gameplayLibrary;

    [Header("SFX Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Visuals")]
    [SerializeField] private UIAnimationData uiAnimations;

    private int currentIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        LoadVolume();
        if (backgroundAudios.Length > 0)
        {
            PlayNextSong();
        }
    }
    public void PlayClick() => PlaySfx(uiLibrary.clickNormal);
    public void PlayBack() => PlaySfx(uiLibrary.clickBack);
    public void PlayHover() => PlaySfx(uiLibrary.hover);
    public void PlayPanelOpen() => PlaySfx(uiLibrary.panelOpen);
    public void PlayPanelClose() => PlaySfx(uiLibrary.panelClose);
    public void PlayError() => PlaySfx(uiLibrary.error);
    public void PlaySuccess() => PlaySfx(uiLibrary.success);

    public void PlayBounce() => PlayRandomSfx(gameplayLibrary.ballBounce);
    public void PlayHit() => PlayRandomSfx(gameplayLibrary.ballHitPlayer);
    public void PlayDash() => PlayRandomSfx(gameplayLibrary.dashSounds);
    public void PlayScore() => PlaySfx(gameplayLibrary.scorePoint);
    public void PlayMatchEnd() => PlaySfx(gameplayLibrary.matchEnd);


    private void PlaySfx(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    private void PlayRandomSfx(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && sfxSource != null)
        {
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
            sfxSource.pitch = 1f;
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
        PlayClick(); 
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
        PlayClick(); 
    }

    private void TriggerAnimation(GameObject textObj)
    {
        GameObject btn = EventSystem.current.currentSelectedGameObject;
        if (btn != null) uiAnimations.AnimateButtonPunch(btn, btn.transform.localScale);
        if (textObj != null) uiAnimations.AnimateTextPop(textObj);
    }

    private void ApplyVolume(string mixerParam, float volume, TMP_Text textUI)
    {
        float dbVolume = volume <= 0.001f ? -80f : Mathf.Log10(volume) * 20;
        myAudioMixer.SetFloat(mixerParam, dbVolume);
        if (textUI != null) textUI.text = Mathf.RoundToInt(volume * 100) + "%";
    }

    private void PlayNextSong()
    {
        if (currentIndex >= backgroundAudios.Length) currentIndex = 0;

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