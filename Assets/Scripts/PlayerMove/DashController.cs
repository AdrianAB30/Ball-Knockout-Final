using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class DashController : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private float dashForce = 25f;
    [SerializeField] private float dashCooldown = 1.0f; 

    [Header("Sensibilidad del Dedo")]
    [SerializeField] private float flickThreshold = 2000f; 

    private Rigidbody2D _rb;
    private PlayerInputHandler _input;
    private float _lastDashTime;

    private Vector2 _lastPointerPos;
    private float _lastTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _input = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner) return;

        if (Time.time < _lastDashTime + dashCooldown) return;

        bool shouldDash = false;
        Vector2 dashDirection = Vector2.zero;

        if (_input.DashPressedThisFrame)
        {
            shouldDash = true;
            dashDirection = _input.MoveDirection.normalized;
            if (dashDirection == Vector2.zero) shouldDash = false;
        }

        if (_input.IsPressing && (_input.CurrentScheme == "Touch" || _input.CurrentScheme == "KeyboardLeft"))
        {
            float deltaTime = Time.time - _lastTime;
            if (deltaTime > 0)
            {
                Vector2 pointerVelocity = (_input.PointerPosition - _lastPointerPos) / deltaTime;

                if (pointerVelocity.magnitude > flickThreshold)
                {
                    shouldDash = true;
                    dashDirection = pointerVelocity.normalized; 
                }
            }
        }

        _lastPointerPos = _input.PointerPosition;
        _lastTime = Time.time;

        if (shouldDash)
        {
            TriggerDash(dashDirection);
        }
    }

    private void TriggerDash(Vector2 direction)
    {
        _lastDashTime = Time.time;

        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer)
        {
            RequestDashServerRpc(direction); 
        }
        else
        {
            ApplyDashForce(direction); 
        }
    }

    [ServerRpc]
    private void RequestDashServerRpc(Vector2 direction)
    {
        ApplyDashForce(direction);
        // PlayDashEffectsClientRpc(); // Efectos visuales
    }

    private void ApplyDashForce(Vector2 dir)
    {
        _rb.linearVelocity = Vector2.zero; 
        _rb.AddForce(dir * dashForce, ForceMode2D.Impulse);
        Debug.Log("🔥 FLICK DASH!");
    }
}