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

    [Header("Ajustes Joystick Virtual")]
    [Tooltip("Cuánto debes arrastrar el dedo para alcanzar la velocidad máxima.")]
    [SerializeField] private float joystickRadius = 100f;

    [Tooltip("Distancia máxima en píxeles desde la pelota para que el toque sea válido.")]
    [SerializeField] private float interactionRadius = 200f;

    [Tooltip("Qué tan rápido debes mover el dedo (píxeles/seg) al soltar para que cuente como Dash.")]
    [SerializeField] private float minDashVelocity = 1000f;

    private PlayerInput _playerInput;
    private Rigidbody2D _rb;
    private Camera _mainCamera; 

    public Vector2 MoveDirection { get; private set; }
    public Vector2 PointerPosition { get; private set; }
    public bool IsPressing { get; private set; }
    public string CurrentScheme { get; private set; }

    private Vector2 _touchStartPosition;
    private float _touchStartTime;
    private bool _isTouching = false;
    private Vector2 _lastTouchPos;
    private Vector2 _currentTouchVelocity;

    private void Awake()
    {
        _playerInput = GetComponent<PlayerInput>();
        _rb = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main; // Cacheamos la cámara
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

        if (_mainCamera == null) _mainCamera = Camera.main;

        if (Touchscreen.current != null)
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

        MoveDirection = context.ReadValue<Vector2>();
        OnMoveInput?.Invoke(MoveDirection);
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
        Vector2 ballScreenPos = _mainCamera.WorldToScreenPoint(transform.position);

        if (touch.press.isPressed)
        {
            IsPressing = true;

            if (!_isTouching)
            {
                float distanceToBall = Vector2.Distance(currentTouchPos, ballScreenPos);
                if (distanceToBall > interactionRadius) return;

                _touchStartPosition = currentTouchPos;
                _lastTouchPos = currentTouchPos; 
                _isTouching = true;
                PointerPosition = currentTouchPos;
            }
            else
            {
                PointerPosition = currentTouchPos;

                Vector2 deltaMove = currentTouchPos - _lastTouchPos;
                _currentTouchVelocity = deltaMove / Time.deltaTime;

                _lastTouchPos = currentTouchPos;

                Vector2 directionToFinger = currentTouchPos - ballScreenPos;

                if (directionToFinger.magnitude > minMoveDistance)
                {
                    MoveDirection = directionToFinger.normalized;
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

            float fingerSpeed = _currentTouchVelocity.magnitude;

            if (fingerSpeed > minDashVelocity)
            {
                OnDashPressed?.Invoke(_currentTouchVelocity.normalized);
                Debug.Log($"🚀 FLICK DASH! Velocidad: {fingerSpeed:F0} px/s");
            }
        }
    }
    public Vector2 GetCurrentMoveDirection()
    {
        return MoveDirection.normalized;
    }
}