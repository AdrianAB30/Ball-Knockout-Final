using UnityEngine;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    [Header("Referencias de Players")]
    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;

    [Header("HUD de Vidas")]
    [SerializeField] private TMP_Text player1LivesText;
    [SerializeField] private TMP_Text player2LivesText;

    [Header("Configuración")]
    [SerializeField] private int initialLives = 3;

    private int _player1Lives;
    private int _player2Lives;

    private Vector3 _player1StartPos;
    private Vector3 _player2StartPos;

    private void Awake()
    {
        if (player1 != null)
        {
            _player1StartPos = player1.transform.position;
        }

        if (player2 != null)
        {
            _player2StartPos = player2.transform.position;
        }

        _player1Lives = initialLives;
        _player2Lives = initialLives;

        UpdateLivesUI();
    }

    private void UpdateLivesUI()
    {
        if (player1LivesText != null)
        {
            player1LivesText.text = _player1Lives.ToString();
        }

        if (player2LivesText != null)
        {
            player2LivesText.text = _player2Lives.ToString();
        }
    }

    /// <summary>
    /// Llamado por los players cuando salen de la cámara.
    /// </summary>
    public void OnPlayerOutOfBounds(string playerId)
    {
        if (playerId == "P1")
        {
            _player1Lives--;
            Debug.Log($"[Vida] Player 1 murió. Vidas restantes: {_player1Lives}");
            RespawnPlayer(player1, _player1StartPos);
        }
        else if (playerId == "P2")
        {
            _player2Lives--;
            Debug.Log($"[Vida] Player 2 murió. Vidas restantes: {_player2Lives}");
            RespawnPlayer(player2, _player2StartPos);
        }
        else
        {
            Debug.LogWarning($"[Vida] PlayerId desconocido: {playerId}");
            return;
        }

        UpdateLivesUI();
    }

    private void RespawnPlayer(GameObject player, Vector3 startPos)
    {
        if (player == null)
        {
            Debug.LogWarning("[Respawn] Player null, no se puede respawnear");
            return;
        }

        Debug.Log($"[Respawn] Respawneando {player.name} en {startPos}");

        player.transform.position = startPos;

        Rigidbody2D rb2D = player.GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
        }
    }
}