using System.Collections;
using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class MovementController : NetworkBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Configuración de Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;

    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    
    private Vector2 _moveInput;
    private Vector2 _dashDirection;
    private Vector2 _pushDirection;
    private float _pushSpeed;

    private bool _isDashing = false;
    private bool _isBeingPushed = false;

    public bool IsDashing => _isDashing;
    public bool IsBeingPushed => _isBeingPushed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
        // ... (Tu configuración de Rigidbody existente) ...
        _rb.gravityScale = 0; 
    }

    private void OnEnable() => _input.OnMoveInput += HandleMoveInput;
    private void OnDisable() => _input.OnMoveInput -= HandleMoveInput;

    private void HandleMoveInput(Vector2 input)
    {
        _moveInput = input;
    }

    private void FixedUpdate()
    {
        // CAMBIO CLAVE: 
        // Si está conectado a la red (IsSpawned) Y NO es mío, no lo muevo.
        // Si es Local (IsSpawned es false), SI lo muevo.
        if (IsSpawned && !IsOwner) return;

        Move();
    }

    private void Move()
    {
        Vector2 finalVelocity = Vector2.zero;

        if (_isDashing) finalVelocity = _dashDirection * dashSpeed;
        else if (_isBeingPushed) finalVelocity = _pushDirection * _pushSpeed;
        else finalVelocity = _moveInput * moveSpeed;

        _rb.linearVelocity = finalVelocity;
    }

    public void PerformDash(Vector2 direction)
    {
        if (_isDashing || _isBeingPushed) return;
        _dashDirection = direction.normalized;
        StartCoroutine(DashCoroutine());
    }

    // --- SISTEMA DE EMPUJE (HÍBRIDO) ---

    // 1. Método para recibir la orden desde el servidor (Solo Online)
    [ClientRpc]
    public void ApplyPushClientRpc(Vector2 direction, float force, float duration)
    {
        if (IsOwner) GetPushed(direction, force, duration);
    }

    // 2. Método local que aplica la fuerza
    public void GetPushed(Vector2 direction, float force, float duration)
    {
        if (_isBeingPushed) return;

        if (_isDashing)
        {
            StopAllCoroutines();
            _isDashing = false;
        }

        _pushDirection = direction;
        _pushSpeed = force;
        StartCoroutine(PushedCoroutine(duration));
    }

    private IEnumerator DashCoroutine()
    {
        _isDashing = true;
        yield return new WaitForSeconds(dashDuration);
        _isDashing = false;
    }

    private IEnumerator PushedCoroutine(float duration)
    {
        _isBeingPushed = true;
        yield return new WaitForSeconds(duration);
        _isBeingPushed = false;
    }
}