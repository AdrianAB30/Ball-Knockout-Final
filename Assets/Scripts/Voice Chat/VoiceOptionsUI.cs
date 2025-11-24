using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Vivox;
using System.Linq;
using System;

public class VoiceOptionsUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown inputDropdown;
    [SerializeField] private TMP_Dropdown outputDropdown;

    private List<VivoxInputDevice> inputDevices;
    private List<VivoxOutputDevice> outputDevices;

    void Start()
    {
        inputDropdown.onValueChanged.AddListener(OnInputDeviceChanged);
        outputDropdown.onValueChanged.AddListener(OnOutputDeviceChanged);

        if (VivoxManager.Instance.IsInitialized)
        {
            Debug.Log("VoiceOptionsUI: Vivox ya estaba inicializado. Rellenando listas...");
            PopulateDeviceDropdowns();
        }
        else
        {
            Debug.Log("VoiceOptionsUI: Vivox no está listo. Suscribiendo al evento...");
            VivoxManager.OnVivoxInitialized += PopulateDeviceDropdowns;
        }
    }
    private void OnDestroy()
    {
        VivoxManager.OnVivoxInitialized -= PopulateDeviceDropdowns;
    }

    private void PopulateDeviceDropdowns()
    {
        Debug.Log("Vivox inicializado. Rellenando listas de dispositivos...");

        inputDevices = VivoxManager.Instance.GetInputDevices();
        inputDropdown.ClearOptions();
        inputDropdown.AddOptions(inputDevices.Select(d => d.DeviceName).ToList());

        outputDevices = VivoxManager.Instance.GetOutputDevices();
        outputDropdown.ClearOptions();
        outputDropdown.AddOptions(outputDevices.Select(d => d.DeviceName).ToList());

        inputDropdown.SetValueWithoutNotify(inputDevices.IndexOf(VivoxService.Instance.ActiveInputDevice));
        outputDropdown.SetValueWithoutNotify(outputDevices.IndexOf(VivoxService.Instance.ActiveOutputDevice));
    }

    public void OnInputDeviceChanged(int index)
    {
        if (inputDevices == null || index < 0 || index >= inputDevices.Count) return;

        VivoxInputDevice selectedDevice = inputDevices[index];
        VivoxManager.Instance.SetInputDevice(selectedDevice);
    }

    public void OnOutputDeviceChanged(int index)
    {
        if (outputDevices == null || index < 0 || index >= outputDevices.Count) return;

        VivoxOutputDevice selectedDevice = outputDevices[index];
        VivoxManager.Instance.SetOutputDevice(selectedDevice);
    }
}