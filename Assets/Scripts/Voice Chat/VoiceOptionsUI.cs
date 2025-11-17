using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Vivox;
using System.Linq;
public class VoiceOptionsUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown inputDeviceDropdown;
    [SerializeField] private TMP_Dropdown outputDeviceDropdown;
    [SerializeField] private Button muteButton;
    [SerializeField] private TextMeshProUGUI muteButtonText;

    private List<VivoxInputDevice> _inputDevices;
    private List<VivoxOutputDevice> _outputDevices;    
    
    void Start()
    {
        if (VivoxManager.Instance == null)
        {
            gameObject.SetActive(false);
            return;
        }
        // Suscribirse a los eventos del SDK
        VivoxService.Instance.AvailableInputDevicesChanged += RefreshInputDevices;
        VivoxService.Instance.AvailableOutputDevicesChanged += RefreshOutputDevices;

        //Botones de la UI
        muteButton.onClick.AddListener(OnMuteClicked);
        inputDeviceDropdown.onValueChanged.AddListener(OnInputDeviceSelected);
        outputDeviceDropdown.onValueChanged.AddListener(OnOutputDeviceSelected);

        // Cargar estado inicial
        RefreshInputDevices();
        RefreshOutputDevices();
        UpdateMuteButtonText();
    }

    void OnDestroy()
    {
        // Limpiar suscripciones
        if (VivoxService.Instance != null)
        {
            VivoxService.Instance.AvailableInputDevicesChanged -= RefreshInputDevices;
            VivoxService.Instance.AvailableOutputDevicesChanged -= RefreshOutputDevices;
        }
    }

    private void RefreshInputDevices()
    {
        _inputDevices = VivoxService.Instance.AvailableInputDevices.ToList();
        inputDeviceDropdown.ClearOptions();

        if (_inputDevices.Count == 0) return;

        var options = _inputDevices.Select(d => d.DeviceName).ToList();
        inputDeviceDropdown.AddOptions(options);

        // Seleccionar el activo
        var activeDevice = VivoxService.Instance.ActiveInputDevice;
        if (activeDevice != null)
        {
            inputDeviceDropdown.value = _inputDevices.FindIndex(d => d.DeviceID == activeDevice.DeviceID);
        }
    }

    private void RefreshOutputDevices()
    {
        _outputDevices = VivoxService.Instance.AvailableOutputDevices.ToList();
        outputDeviceDropdown.ClearOptions();

        if (_outputDevices.Count == 0) return;

        var options = _outputDevices.Select(d => d.DeviceName).ToList();
        outputDeviceDropdown.AddOptions(options);

        // Seleccionar el activo
        var activeDevice = VivoxService.Instance.ActiveOutputDevice;
        if (activeDevice != null)
        {
            outputDeviceDropdown.value = _outputDevices.FindIndex(d => d.DeviceID == activeDevice.DeviceID);
        }
    }

    private void OnInputDeviceSelected(int index)
    {
        if (index >= 0 && index < _inputDevices.Count)
        {
            VivoxManager.Instance.SelectInputDevice(_inputDevices[index].DeviceID);
        }
    }
    private void OnOutputDeviceSelected(int index)
    {
        if (index >= 0 && index < _outputDevices.Count)
        {
            VivoxManager.Instance.SelectOutputDevice(_outputDevices[index].DeviceID);
        }
    }

    private void OnMuteClicked()
    {
        VivoxManager.Instance.ToggleMute();
        UpdateMuteButtonText();
    }
    private void UpdateMuteButtonText()
    {
        if (muteButtonText != null)
        {
            muteButtonText.text = VivoxManager.Instance.IsMuted ? "Unmute" : "Mute";
        }
    }
}
