using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LocalMatchConfig", menuName = "ScriptableObjects/Local Match Config", order = 5)]
public class LocalMatchConfigurationSO : ScriptableObject
{
    [System.NonSerialized] public List<InputDevice> Team1Devices = new List<InputDevice>();
    [System.NonSerialized] public List<InputDevice> Team2Devices = new List<InputDevice>();

    public void ResetData()
    {
        Team1Devices = new List<InputDevice>();
        Team2Devices = new List<InputDevice>();
    }

    public void AddPlayerToTeam(InputDevice device, int teamId)
    {
        if (teamId == 1) Team1Devices.Add(device);
        else if (teamId == 2) Team2Devices.Add(device);
    }
}