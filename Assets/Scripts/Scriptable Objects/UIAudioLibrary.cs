using UnityEngine;

[CreateAssetMenu(fileName = "UiSfx", menuName = "ScriptableObjects/UiSfx", order = 7)]
public class UIAudioLibrary : ScriptableObject
{
    [Header("Botones")]
    public AudioClip clickNormal;
    public AudioClip clickBack;
    public AudioClip hover;

    [Header("Paneles")]
    public AudioClip panelOpen;
    public AudioClip panelClose;

    [Header("Sistema")]
    public AudioClip error;
    public AudioClip success;
}