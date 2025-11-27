using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInputHandler : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;
    [SerializeField] private Camera _playerCamera;

    private PlayerInput _playerInput;
    private Rigidbody2D _rb;

    public Vector2 MoveDirection { get; private set; }
    public Vector2 PointerPosition { get; private set; } 
    public bool IsPressing { get; private set; } 
    public string CurrentScheme { get; private set; }

    private bool _dashTriggeredInternal = false;
    public bool DashPressedThisFrame
    {
        get
        {
            if (_dashTriggeredInternal)
            {
                _dashTriggeredInternal = false;
                return true;
            }
            return false;
        }
    }

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner)
        {
            enabled = false;
            return;
        }

        if (_playerCamera == null)
        {
            var cam = GetComponentInChildren<Camera>();
            _playerCamera = cam != null ? cam : Camera.main;
        }
    }
    private bool ValidateInput()
    {
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner)
            return false;
        return true;
    }
    public void OnControlsChanged(PlayerInput input)
    {
        if (!ValidateInput()) return;
        CurrentScheme = input.currentControlScheme;

        MoveDirection = Vector2.zero;
        IsPressing = false;
        _dashTriggeredInternal = false;
    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;

        if (CurrentScheme == "Gamepad" || CurrentScheme == "KeyboardLeft" || CurrentScheme == "KeyboardRight")
        {
            MoveDirection = context.ReadValue<Vector2>();
        }
    }

    public void OnDashInput(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;

        if (context.performed) _dashTriggeredInternal = true;
    }

    public void OnPointerPositionInput(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;

        PointerPosition = context.ReadValue<Vector2>();

        if ((CurrentScheme == "Touch" || CurrentScheme == "KeyboardLeft") && IsPressing)
        {
            CalculatePointerMovement();
        }
    }

    public void OnPointerPressInput(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;

        IsPressing = context.ReadValueAsButton();

        if (!IsPressing && CurrentScheme == "Touch")
        {
            MoveDirection = Vector2.zero;
        }
        else if (IsPressing)
        {
            CalculatePointerMovement();
        }
    }

    private void CalculatePointerMovement()
    {
        if (_playerCamera == null) return;

        Ray ray = _playerCamera.ScreenPointToRay(PointerPosition);

        Plane gameplayPlane = new Plane(Vector3.back, transform.position);

        if (gameplayPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 worldPoint = ray.GetPoint(enterDistance);

            Vector2 rawDirection = (Vector2)worldPoint - _rb.position;

            if (rawDirection.magnitude > 0.5f)
            {
                MoveDirection = rawDirection.normalized;
            }
            else
            {
                MoveDirection = Vector2.zero;
            }

            Debug.DrawLine(transform.position, worldPoint, Color.red);
        }
    }
}