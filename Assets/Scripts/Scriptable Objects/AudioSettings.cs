using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Audio Settings", menuName = "ScriptableObjects/Audio Settings", order = 2)]
public class AudioSettings : ScriptableObject
{
    public float masterVolume = 1;
    public float musicVolume = 1;
    public float sfxVolume = 1;
}