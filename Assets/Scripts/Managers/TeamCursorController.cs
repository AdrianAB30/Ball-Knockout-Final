using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TeamCursorController : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Image cursorImage;

    public int CurrentTeam { get; private set; } = 0; 
    public InputDevice Device { get; private set; }
    public int PlayerIndex { get; private set; }

    private TeamSelectionManager _manager;
    private PlayerInput _playerInput;
    private float _inputDelay = 0f;

    private bool _isInitialized = false;

    public void Setup(TeamSelectionManager manager, int index)
    {
        _manager = manager;
        _playerInput = GetComponent<PlayerInput>();
        PlayerIndex = index;

        if (_playerInput.devices.Count > 0)
            Device = _playerInput.devices[0];

        if (cursorImage != null)
            cursorImage.color = (index == 0) ? Color.white : new Color(0.8f, 0.8f, 0.8f);

        _isInitialized = true;
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!_isInitialized || !context.performed) return;

        if (Time.time < _inputDelay) return;

        Vector2 move = context.ReadValue<Vector2>();

        int newTeam = CurrentTeam;

        if (move.x < -0.5f) 
        {
            if (CurrentTeam == 0) newTeam = 1;      
            else if (CurrentTeam == 2) newTeam = 0; 
        }
        else if (move.x > 0.5f) 
        {
            if (CurrentTeam == 0) newTeam = 2;      
            else if (CurrentTeam == 1) newTeam = 0; 
        }

        if (newTeam != CurrentTeam)
        {
            MoveToTeam(newTeam);
            _inputDelay = Time.time + 0.2f;
        }
    }

    private void MoveToTeam(int newTeam)
    {
        Transform targetSlot = _manager.GetTargetSlot(PlayerIndex, newTeam);

        if (targetSlot != null)
        {
            transform.SetParent(targetSlot, false);
            transform.localPosition = Vector3.zero;
            CurrentTeam = newTeam;

            _manager.UpdatePlayerLight(PlayerIndex, newTeam);
            _manager.CheckReadyState();
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _manager.AttemptStartGame();
        }
    }
}