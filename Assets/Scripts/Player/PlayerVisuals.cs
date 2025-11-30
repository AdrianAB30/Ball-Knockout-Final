using UnityEngine;
using TMPro;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI playerLabel;
    [SerializeField] private Canvas infoCanvas;

    [Header("Configuración")]
    [SerializeField] private Color teamProColor = Color.red;
    [SerializeField] private Color teamNoobColor = Color.blue;

    private Camera _mainCam;
    private Quaternion _initialRotation;

    private void Awake()
    {
        _mainCam = Camera.main;

        if (_mainCam != null && infoCanvas != null)
        {
            infoCanvas.worldCamera = _mainCam;

            _initialRotation = infoCanvas.transform.rotation;
        }
    }

    private void LateUpdate()
    {
        if (infoCanvas == null) return;

        if (_mainCam != null)
        {
            infoCanvas.transform.LookAt(infoCanvas.transform.position + _mainCam.transform.forward);
        }
    }

    public void SetPlayerInfo(int playerNumber, int teamId)
    {
        if (playerLabel == null) return;

        playerLabel.text = $"P{playerNumber}";

        if (teamId == 1)
        {
            playerLabel.color = teamProColor;
        }
        else
        {
            playerLabel.color = teamNoobColor;
        }
    }
}