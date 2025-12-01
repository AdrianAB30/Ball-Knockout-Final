using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

public class Health : NetworkBehaviour
{
    [Header("Configuración Global")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private float respawnTime = 1.0f;

    [Header("Resistencia (Golpes antes de romperse)")]
    [SerializeField] private int maxDurability = 3; 

    [Header("Vidas (Corazones UI)")]
    [SerializeField] private int maxLives = 2;

    // --- VARIABLES DE RED (Online) ---
    public NetworkVariable<int> NetLives = new NetworkVariable<int>(2);
    public NetworkVariable<int> NetDurability = new NetworkVariable<int>(3);

    // --- VARIABLES LOCALES ---
    private int _localLives;
    private int _localDurability;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer ballRenderer;
    [SerializeField] private Collider2D myCollider;
    [SerializeField] private Canvas nameCanvas;
    [SerializeField] private Sprite[] proSprites;
    [SerializeField] private Sprite[] noobSprites;

    public int PlayerID { get; private set; }
    private int _currentTeamId = 1;

    public static event Action<int> OnPlayerDied;
    public static event Action<int, int> OnLivesChanged; 

    private void Awake()
    {
        if (myCollider == null) myCollider = GetComponent<Collider2D>();
        if (nameCanvas == null) nameCanvas = GetComponentInChildren<Canvas>();
    }

    private void Start()
    {
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.LocalSplitScreen)
        {
            ResetLocalHealth();
        }
    }

    public void SetTeamId(int teamId, int playerNum)
    {
        _currentTeamId = teamId;
        PlayerID = playerNum;

        int lives = IsSpawned ? NetLives.Value : _localLives;
        OnLivesChanged?.Invoke(PlayerID, lives);

        int durability = IsSpawned ? NetDurability.Value : _localDurability;
        UpdateVisuals(durability);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetLives.Value = maxLives;
            NetDurability.Value = maxDurability;
        }

        NetDurability.OnValueChanged += (oldVal, newVal) => UpdateVisuals(newVal);

        NetLives.OnValueChanged += (oldVal, newVal) =>
        {
            if (PlayerID != 0) OnLivesChanged?.Invoke(PlayerID, newVal);
        };

        UpdateVisuals(NetDurability.Value);
    }

    public void TakeDamage()
    {
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            if (!IsServer) return;

            if (NetDurability.Value > 0) NetDurability.Value--;

            if (NetDurability.Value <= 0) LoseLife();
        }
        else
        {
            if (_localDurability > 0)
            {
                _localDurability--;
                UpdateVisuals(_localDurability); 
            }

            if (_localDurability <= 0) LoseLifeLocal();
        }
    }

    private void LoseLife()
    {
        NetLives.Value--;
        Debug.Log($"¡ROTO! Vidas restantes: {NetLives.Value}");
        StartCoroutine(HandleDeathSequence());
    }

    private void LoseLifeLocal() 
    {
        _localLives--;
        Debug.Log($"¡ROTO LOCAL! Vidas restantes: {_localLives}");
        OnLivesChanged?.Invoke(PlayerID, _localLives); 
        StartCoroutine(HandleDeathSequence());
    }

    private IEnumerator HandleDeathSequence()
    {
        UpdateVisuals(0); 

        var movement = GetComponent<MovementController>();
        var dash = GetComponent<DashController>();
        if (movement) movement.SetDead(true);
        if (dash) dash.SetDead(true);

        yield return new WaitForSeconds(respawnTime);

        Die();
    }

    private void Die()
    {
        OnPlayerDied?.Invoke(PlayerID); 

        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && IsServer)
            DieClientRpc();
        else
            DieLocal();
    }

    [ClientRpc] private void DieClientRpc() => DieLocal();

    private void DieLocal()
    {
        TogglePlayerComponents(false);
    }

    public void Revive()
    {
        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && IsServer)
            ReviveClientRpc();
        else
            ReviveLocal();
    }

    [ClientRpc] private void ReviveClientRpc() => ReviveLocal();

    private void ReviveLocal()
    {
        TogglePlayerComponents(true);

        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && IsServer)
        {
            NetDurability.Value = maxDurability;
        }
        else
        {
            _localDurability = maxDurability;
            UpdateVisuals(_localDurability);
        }

        int currentLives = (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer) ? NetLives.Value : _localLives;
        if (currentLives <= 0) ResetLocalHealth();

        var movement = GetComponent<MovementController>();
        var dash = GetComponent<DashController>();
        if (movement) movement.SetDead(false);
        if (dash) dash.SetDead(false);
    }
    private void TogglePlayerComponents(bool state)
    {
        if (ballRenderer != null) ballRenderer.enabled = state;
        if (nameCanvas != null) nameCanvas.gameObject.SetActive(state);
        if (myCollider != null) myCollider.enabled = state;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (!state) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
            rb.simulated = state;
        }
    }

    private void UpdateVisuals(int currentDurability)
    {
        if (ballRenderer == null) return;
        Sprite[] currentSprites = (_currentTeamId == 1) ? proSprites : noobSprites;

        int spriteIndex = Mathf.Clamp(maxDurability - currentDurability, 0, currentSprites.Length - 1);

        if (currentSprites.Length > spriteIndex)
            ballRenderer.sprite = currentSprites[spriteIndex];
    }

    public void ResetLocalHealth()
    {
        _localLives = maxLives;
        _localDurability = maxDurability;

        if (IsServer) { NetLives.Value = maxLives; NetDurability.Value = maxDurability; }

        UpdateVisuals(maxDurability);
        OnLivesChanged?.Invoke(PlayerID, maxLives);
        TogglePlayerComponents(true);
    }
}