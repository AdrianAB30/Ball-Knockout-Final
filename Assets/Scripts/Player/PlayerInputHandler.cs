using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputHandler : MonoBehaviour
{
    public event Action<Vector2> OnMoveInput;
    public event Action<Vector2> OnDashPressed;

    [Header("Configuración de Swipe (Móvil)")]
    [Tooltip("La distancia mínima en píxeles para registrar un swipe.")]
    [SerializeField] private float minSwipeDistance = 50f;
    [Tooltip("Tiempo máximo para que un gesto cuente como Dash (si tardas más, solo camina).")]
    [SerializeField] private float maxDashTime = 0.3f;

    [Header("Configuración de Movimiento (Móvil)")]
    [Tooltip("Zona muerta: Mínimo movimiento del dedo para empezar a caminar.")]
    [SerializeField] private float minMoveDistance = 10f;

    // Variables internas
    private Vector2 currentMoveDirection;
    private Vector2 touchStartPosition;
    private float touchStartTime;
    private bool isTouching = false;

    private void Awake()
    {
        if (Touchscreen.current != null)
        {
            InputSystem.EnableDevice(Touchscreen.current);
        }
    }

    private void Update()
    {
        HandleTouchInput();
    }

    // --- INPUT DE PC (TECLADO/MANDO) ---
    public void HandleMove(InputAction.CallbackContext context)
    {
        currentMoveDirection = context.ReadValue<Vector2>();

        if (!isTouching)
        {
            OnMoveInput?.Invoke(currentMoveDirection);
        }
    }

    public void HandleDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 dashDirection = GetCurrentMoveDirection();
            if (dashDirection == Vector2.zero) dashDirection = Vector2.right;

            if (dashDirection.sqrMagnitude > 0.1f)
            {
                OnDashPressed?.Invoke(dashDirection);
            }
        }
    }

    public Vector2 GetCurrentMoveDirection()
    {
        return currentMoveDirection.normalized;
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;

        Vector2 currentTouchPos = touch.position.ReadValue();

        if (touch.press.isPressed)
        {

            if (!isTouching)
            {
                touchStartPosition = currentTouchPos;
                touchStartTime = Time.time;
                isTouching = true;
            }
            else
            {
                Vector2 moveVector = currentTouchPos - touchStartPosition;

                if (moveVector.magnitude > minMoveDistance)
                {
                    OnMoveInput?.Invoke(moveVector.normalized);
                }
                else
                {
                    OnMoveInput?.Invoke(Vector2.zero);
                }
            }
        }
        else if (isTouching)
        {
            isTouching = false;

            OnMoveInput?.Invoke(Vector2.zero);

            float timeElapsed = Time.time - touchStartTime;
            Vector2 swipeVector = currentTouchPos - touchStartPosition;

            if (swipeVector.magnitude > minSwipeDistance && timeElapsed <= maxDashTime)
            {
                OnDashPressed?.Invoke(swipeVector.normalized);
            }

            if (currentMoveDirection != Vector2.zero)
            {
                OnMoveInput?.Invoke(currentMoveDirection);
            }
        }
    }
}