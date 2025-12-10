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
    [SerializeField] private float absoluteMaxSpeed = 25f;

    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    private bool _isDashing = false;
    private bool _isDead = true;

    private Vector2 _networkInputDirection;

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

    private void Update()
    {
        if (IsOwner)
        {
            if (_input != null)
                _networkInputDirection = _input.MoveDirection;
        }
    }

    private void FixedUpdate()
    {
        if (_isDashing || _isDead) return;

        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {

            if (IsOwner)
            {
                SubmitInputServerRpc(_networkInputDirection);
            }

            if (IsServer)
            {

            }
        }
        else
        {
            ApplyPhysics(_input.MoveDirection);
        }

        if (_rb.linearVelocity.magnitude > absoluteMaxSpeed)
        {
            _rb.linearVelocity = Vector2.ClampMagnitude(_rb.linearVelocity, absoluteMaxSpeed);
        }
    }

    [ServerRpc]
    private void SubmitInputServerRpc(Vector2 inputDirection)
    {
        ApplyPhysics(inputDirection);
    }

    private void ApplyPhysics(Vector2 moveDir)
    {
        if (moveDir.magnitude > 0.01f)
        {
            Vector2 targetVelocity = moveDir * moveSpeed;
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 15f);
        }
        else
        {

            if (_rb.linearVelocity.magnitude > moveSpeed)
            {
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 0.5f);
            }
            else
            {
                _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 20f);
            }
        }
    }
    public void SetDead(bool state)
    {
        _isDead = state;
        if (_isDead && _rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
        transform.position = position;
        transform.rotation = rotation;
    }
}