using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System;

public class PlayerInputHandler : NetworkBehaviour
{
    public event Action<Vector2> OnMoveInput;
    public event Action<Vector2> OnDashPressed;

    [Header("Configuración")]
    [SerializeField] private GameConfigurationSO gameConfig;

    [Header("Configuración de Swipe (Móvil)")]
    [Tooltip("La distancia mínima en píxeles para registrar un swipe.")]
    [SerializeField] private float minSwipeDistance = 50f;
    [Tooltip("Tiempo máximo para que un gesto cuente como Dash.")]
    [SerializeField] private float maxDashTime = 0.3f;

    [Header("Configuración de Movimiento (Móvil)")]
    [Tooltip("Zona muerta: Mínimo movimiento del dedo para empezar a caminar.")]
    [SerializeField] private float minMoveDistance = 10f;

    private PlayerInput _playerInput;
    private Rigidbody2D _rb;

    public Vector2 MoveDirection { get; private set; }
    public Vector2 PointerPosition { get; private set; }
    public bool IsPressing { get; private set; }
    public string CurrentScheme { get; private set; }

    private Vector2 _touchStartPosition;
    private float _touchStartTime;
    private bool _isTouching = false;

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
    }

    private bool ValidateInput()
    {
        if (gameConfig != null && gameConfig.CurrentGameMode == GameModeType.OnlineMultiplayer && !IsOwner)
            return false;
        return true;
    }

    private void Update()
    {
        if (!ValidateInput()) return;

        if (CurrentScheme == "Touch")
        {
            HandleTouchInput();
        }
    }

    public void OnControlsChanged(PlayerInput input)
    {
        if (!ValidateInput()) return;
        CurrentScheme = input.currentControlScheme;

        MoveDirection = Vector2.zero;
        IsPressing = false;
        _isTouching = false;
        
        OnMoveInput?.Invoke(Vector2.zero);
    }

    public void OnMoveInputAction(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;

        if (CurrentScheme == "Gamepad" || CurrentScheme == "KeyboardLeft" || CurrentScheme == "KeyboardRight")
        {
            MoveDirection = context.ReadValue<Vector2>();
            OnMoveInput?.Invoke(MoveDirection);
        }
    }

    public void OnDashInput(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;

        if (context.performed)
        {
            Vector2 dashDirection = MoveDirection;
            
            if (dashDirection.sqrMagnitude < 0.1f)
            {
                dashDirection = Vector2.right;
            }

            OnDashPressed?.Invoke(dashDirection.normalized);
        }
    }

    public void OnPointerPositionInput(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;
        PointerPosition = context.ReadValue<Vector2>();
    }

    public void OnPointerPressInput(InputAction.CallbackContext context)
    {
        if (!ValidateInput()) return;
        IsPressing = context.ReadValueAsButton();
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        Vector2 currentTouchPos = touch.position.ReadValue();

        if (touch.press.isPressed)
        {
            IsPressing = true;

            if (!_isTouching)
            {
                _touchStartPosition = currentTouchPos;
                _touchStartTime = Time.time;
                _isTouching = true;
                PointerPosition = currentTouchPos;
            }
            else
            {
                PointerPosition = currentTouchPos;
                Vector2 moveVector = currentTouchPos - _touchStartPosition;

                if (moveVector.magnitude > minMoveDistance)
                {
                    MoveDirection = moveVector.normalized;
                    OnMoveInput?.Invoke(MoveDirection);
                }
                else
                {
                    MoveDirection = Vector2.zero;
                    OnMoveInput?.Invoke(Vector2.zero);
                }
            }
        }
        else if (_isTouching)
        {
            IsPressing = false;
            _isTouching = false;

            MoveDirection = Vector2.zero;
            OnMoveInput?.Invoke(Vector2.zero);

            float timeElapsed = Time.time - _touchStartTime;
            Vector2 swipeVector = currentTouchPos - _touchStartPosition;

            if (swipeVector.magnitude > minSwipeDistance && timeElapsed <= maxDashTime)
            {
                OnDashPressed?.Invoke(swipeVector.normalized);
                Debug.Log($"🔥 SWIPE DASH! Distancia: {swipeVector.magnitude:F1}px, Tiempo: {timeElapsed:F2}s");
            }
        }
    }

    public Vector2 GetCurrentMoveDirection()
    {
        return MoveDirection.normalized;
    }
}