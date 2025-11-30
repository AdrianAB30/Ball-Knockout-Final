using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameplaySfx", menuName = "ScriptableObjects/GameplaySfx", order = 8)]
public class GameplayAudioLibrary : ScriptableObject
{
    [Header("Físicas")]
    public AudioClip[] ballBounce;
    public AudioClip[] ballHitPlayer;

    [Header("Acciones")]
    public AudioClip[] dashSounds;
    public AudioClip scorePoint;
    public AudioClip matchEnd;

}