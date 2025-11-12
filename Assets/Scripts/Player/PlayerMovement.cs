using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Configuración de Dash")]
    [SerializeField] private float dashSpeed = 18f;
    [SerializeField] private float dashDuration = 0.15f;

    private Vector2 moveInput;
    private Vector2 dashDirection;
    private Vector2 pushDirection;
    private float pushSpeed;

    private bool isDashing = false;
    private bool isBeingPushed = false;

    public bool IsDashing => isDashing;
    public bool IsBeingPushed => isBeingPushed;
    
    private PlayerInputHandler inputHandler;
    private Rigidbody2D rb;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        rb = GetComponent<Rigidbody2D>();
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.angularDamping = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        inputHandler.OnMoveInput += HandleMoveInput;
    }

    private void OnDisable()
    {
        inputHandler.OnMoveInput -= HandleMoveInput;
    }

    private void HandleMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    private void FixedUpdate()
    {
        Vector2 finalVelocity = Vector2.zero;

        if (isDashing)
        {
            finalVelocity = dashDirection * dashSpeed;
        }
        else if (isBeingPushed)
        {
            finalVelocity = pushDirection * pushSpeed;
        }
        else
        {
            finalVelocity = moveInput * moveSpeed;
        }

        rb.linearVelocity = finalVelocity;
    }

    public void PerformDash(Vector2 direction)
    {
        if (isDashing || isBeingPushed) return;

        dashDirection = direction.normalized;
        StartCoroutine(DashCoroutine());
    }

    public void GetPushed(Vector2 direction, float force, float duration)
    {
        if (isBeingPushed) return;

        pushDirection = direction;
        pushSpeed = force;
        StartCoroutine(PushedCoroutine(duration));
    }

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
    }

    private IEnumerator PushedCoroutine(float duration)
    {
        isBeingPushed = true;
        
        if (isDashing)
        {
            StopAllCoroutines();
            isDashing = false;
        }

        yield return new WaitForSeconds(duration);
        isBeingPushed = false;
    }
}