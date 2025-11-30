using UnityEngine;
using Unity.Netcode;
using System;

public class Health : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private int maxLives = 2;

    public NetworkVariable<int> NetLives = new NetworkVariable<int>(2);

    private int _localLives;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer ballRenderer;
    [SerializeField] private Color healthyColor = Color.white;
    [SerializeField] private Color damagedColor = new Color(1f, 0.5f, 0.5f);

    public static event Action<int> OnPlayerDied;

    private void Start()
    {
        if (gameConfig.CurrentGameMode == GameModeType.LocalSplitScreen)
        {
            _localLives = maxLives;
            UpdateVisuals(_localLives);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetLives.Value = maxLives;
        }
        NetLives.OnValueChanged += (oldVal, newVal) => UpdateVisuals(newVal);
        UpdateVisuals(NetLives.Value);
    }

    public void TakeDamage()
    {
        int currentHealth = 0;

        if (gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            if (!IsServer) return; 
            NetLives.Value--;
            currentHealth = NetLives.Value;
        }
        else 
        {
            _localLives--;
            currentHealth = _localLives;
            UpdateVisuals(_localLives); 
        }
        Debug.Log($"Jugador recibió daño. Vidas: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("¡JUGADOR ELIMINADO!");

        // En local podríamos pasar el ID del jugador, pero por ahora un evento genérico
        // En un futuro podrías pasar GetComponent<PlayerVisuals>().PlayerNumber
        OnPlayerDied?.Invoke(0);

        gameObject.SetActive(false);
    }

    private void UpdateVisuals(int lives)
    {
        if (ballRenderer == null) return;

        if (lives >= 2) ballRenderer.color = healthyColor;
        else if (lives == 1) ballRenderer.color = damagedColor;
    }
}