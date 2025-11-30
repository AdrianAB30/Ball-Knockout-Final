using UnityEngine;
using TMPro;
using Unity.Netcode;

public class PlayerVisuals : NetworkBehaviour
{
    [Header("Referencias Visuales")]
    [SerializeField] private SpriteRenderer ballRenderer;
    [SerializeField] private TextMeshProUGUI playerLabel;
    [SerializeField] private Canvas infoCanvas;

    [Header("Sprites de Equipo")]
    [SerializeField] private Sprite teamProSprite; 
    [SerializeField] private Sprite teamNoobSprite;

    [Header("Colores Texto")]
    [SerializeField] private Color teamProColor = Color.red;
    [SerializeField] private Color teamNoobColor = Color.blue;

    public NetworkVariable<int> NetTeamId = new NetworkVariable<int>(1);
    public NetworkVariable<int> NetPlayerNumber = new NetworkVariable<int>(1);

    private Camera _mainCam;

    private void Awake()
    {
        _mainCam = Camera.main;
        if (_mainCam != null && infoCanvas != null)
        {
            infoCanvas.worldCamera = _mainCam;
        }
    }

    public override void OnNetworkSpawn()
    {
        NetTeamId.OnValueChanged += (old, current) => UpdateVisuals(current, NetPlayerNumber.Value);
        NetPlayerNumber.OnValueChanged += (old, current) => UpdateVisuals(NetTeamId.Value, current);

        UpdateVisuals(NetTeamId.Value, NetPlayerNumber.Value);
    }

    private void LateUpdate()
    {
        if (infoCanvas != null && _mainCam != null)
        {
            infoCanvas.transform.rotation = _mainCam.transform.rotation;
        }
    }

    public void SetLocalInfo(int playerNumber, int teamId)
    {
        UpdateVisuals(teamId, playerNumber);
    }

    public void SetNetworkInfo(int playerNumber, int teamId)
    {
        if (IsServer)
        {
            NetPlayerNumber.Value = playerNumber;
            NetTeamId.Value = teamId;
        }
    }

    private void UpdateVisuals(int teamId, int playerNumber)
    {
        if (playerLabel != null) playerLabel.text = $"P{playerNumber}";

        if (teamId == 1) 
        {
            if (ballRenderer != null) ballRenderer.sprite = teamProSprite;
            if (playerLabel != null) playerLabel.color = teamProColor;
        }
        else 
        {
            if (ballRenderer != null) ballRenderer.sprite = teamNoobSprite;
            if (playerLabel != null) playerLabel.color = teamNoobColor;
        }
    }
}