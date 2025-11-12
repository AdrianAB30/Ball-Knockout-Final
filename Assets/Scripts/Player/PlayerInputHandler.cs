using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputHandler : MonoBehaviour
{
    public event Action<Vector2> OnMoveInput;
    public event Action OnDashPressed; // ← Cambiar a Action simple

    private Vector2 currentMoveDirection;

    public void HandleMove(InputAction.CallbackContext context)
    {
        currentMoveDirection = context.ReadValue<Vector2>();
        OnMoveInput?.Invoke(currentMoveDirection);
    }

    public void HandleDash(InputAction.CallbackContext context)
    {
        if (context.performed && currentMoveDirection.sqrMagnitude > 0.1f)
        {
            OnDashPressed?.Invoke();
        }
    }
    
    public Vector2 GetCurrentMoveDirection()
    {
        return currentMoveDirection.normalized;
    }
}