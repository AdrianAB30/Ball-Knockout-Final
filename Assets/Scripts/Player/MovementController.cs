using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class MovementController : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float maxMoveSpeed = 8f;
    [SerializeField] private float stopFriction = 5f;

    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    private bool _isDashing = false;
    private bool _isDead = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
    }

    public void NotifyDashStarted()
    {
        _isDashing = true;
        StartCoroutine(ResetDashFlag());
    }

    private IEnumerator ResetDashFlag()
    {
        yield return new WaitForSeconds(0.2f);
        _isDashing = false;
    }

    private void FixedUpdate()
    {
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner) return;

        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_isDashing || _isDead) return;

        Vector2 moveDir = _input.MoveDirection;

        if (moveDir.magnitude > 0.01f)
        {
            _rb.AddForce(moveDir * moveSpeed);

            if (_rb.linearVelocity.magnitude > maxMoveSpeed)
            {
                _rb.linearVelocity = Vector2.ClampMagnitude(_rb.linearVelocity, maxMoveSpeed);
            }
        }
        else
        {
            if (_rb.linearVelocity.magnitude > maxMoveSpeed)
            {
                // Dejo fluir la bola
            }
            else
            {
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, stopFriction * Time.fixedDeltaTime);
            }
        }
    }
    public void SetDead(bool state)
    {
        _isDead = state;
        if (_isDead)
        {
            _rb.linearVelocity = Vector2.zero; 
            _rb.angularVelocity = 0f;
        }
    }
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        _rb.linearVelocity = Vector2.zero; 
        _rb.angularVelocity = 0f;

        transform.position = position;
        transform.rotation = rotation;

    }
}