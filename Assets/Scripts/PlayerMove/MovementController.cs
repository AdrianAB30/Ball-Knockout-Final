using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class MovementController : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private GameConfigurationSO gameConfig; 
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float smoothTime = 0.15f; 

    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    private Vector2 _currentVelocitySmooth;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
    }

    private void FixedUpdate()
    {
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner) return;

        Move();
    }

    private void Move()
    {
        Vector2 targetVelocity = _input.MoveDirection * maxSpeed;

        _rb.linearVelocity = Vector2.SmoothDamp( _rb.linearVelocity,targetVelocity, ref _currentVelocitySmooth,smoothTime);
    }
}