using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class LocalPlayerData
{
    public InputDevice Device;
    public int PlayerIndex;   
    public int TeamId;        
}

[CreateAssetMenu(fileName = "LocalMatchConfig", menuName = "ScriptableObjects/Local Match Config", order = 5)]
public class LocalMatchConfigurationSO : ScriptableObject
{
    [System.NonSerialized] public List<LocalPlayerData> Players = new List<LocalPlayerData>();

    public void ResetData()
    {
        Players.Clear();
    }

    public void AddPlayer(InputDevice device, int index, int teamId)
    {
        Players.Add(new LocalPlayerData 
        { 
            Device = device, 
            PlayerIndex = index, 
            TeamId = teamId 
        });
    }
}