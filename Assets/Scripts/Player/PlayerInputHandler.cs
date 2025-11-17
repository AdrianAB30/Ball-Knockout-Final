using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputHandler : MonoBehaviour
{
    public event Action<Vector2> OnMoveInput;
    public event Action<Vector2> OnDashPressed; 

    [Header("Configuración de Swipe (Móvil)")]
    [SerializeField] private float minDashDistance = 100f;
    [SerializeField] private float maxDashTime = 0.25f;
    
    [Header("Configuración de Movimiento (Móvil)")]
    [SerializeField] private float minTouchMoveThreshold = 5f;

    private Vector2 currentMoveDirection;
    
    private Vector2 touchStartPosition;
    private Vector2 lastTouchPosition;
    private float touchStartTime;
    private bool isTouching = false;
    private bool isMovementDrag = false;
    
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        
        if (Touchscreen.current != null)
        {
            InputSystem.EnableDevice(Touchscreen.current);
        }
    }

    private void Update()
    {
        HandleTouchInput();
    }

    public void HandleMove(InputAction.CallbackContext context)
    {
        currentMoveDirection = context.ReadValue<Vector2>();
        OnMoveInput?.Invoke(currentMoveDirection);
    }

    public void HandleDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 dashDirection = GetCurrentMoveDirection();
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

        var primaryTouch = Touchscreen.current.primaryTouch;
        Vector2 touchPosition = primaryTouch.position.ReadValue();
        
        switch (primaryTouch.phase.ReadValue())
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                touchStartPosition = touchPosition;
                lastTouchPosition = touchPosition;
                touchStartTime = Time.time;
                isTouching = true;
                isMovementDrag = false;
                break;
            
            case UnityEngine.InputSystem.TouchPhase.Moved:
                if (!isTouching) return;

                float touchDuration = Time.time - touchStartTime;
                Vector2 frameDragVector = touchPosition - lastTouchPosition;

                if (touchDuration > maxDashTime || isMovementDrag)
                {
                    isMovementDrag = true;
                    
                    if (frameDragVector.magnitude > minTouchMoveThreshold)
                    {
                        Vector2 moveDirection = frameDragVector.normalized;
                        OnMoveInput?.Invoke(moveDirection);
                    }
                }
                
                lastTouchPosition = touchPosition;
                break;

            case UnityEngine.InputSystem.TouchPhase.Ended:
                if (!isTouching) return;
                
                isTouching = false;
                float finalTouchDuration = Time.time - touchStartTime;
                Vector2 finalSwipeVector = touchPosition - touchStartPosition;

                if (!isMovementDrag && 
                    finalTouchDuration <= maxDashTime && 
                    finalSwipeVector.magnitude > minDashDistance)
                {
                    OnDashPressed?.Invoke(finalSwipeVector.normalized);
                }

                OnMoveInput?.Invoke(Vector2.zero);
                isMovementDrag = false;
                break;

            case UnityEngine.InputSystem.TouchPhase.Canceled:
                isTouching = false;
                isMovementDrag = false;
                OnMoveInput?.Invoke(Vector2.zero);
                break;
        }
    }
}