using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDash : MonoBehaviour
{
    [Header("Configuración de Habilidad")]
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private float dashPushForce = 10f;
    [SerializeField] private float pushDuration = 0.2f;

    private bool canDash = true;
    private PlayerInputHandler inputHandler;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        inputHandler.OnDashPressed += HandleDashPressed;
    }

    private void OnDisable()
    {
        inputHandler.OnDashPressed -= HandleDashPressed;
    }

    private void HandleDashPressed()
    {
        if (canDash)
        {
            Vector2 direction = inputHandler.GetCurrentMoveDirection();
            
            if (direction.sqrMagnitude > 0.1f)
            {
                playerMovement.PerformDash(direction);
                StartCoroutine(DashCooldownCoroutine());
            }
        }
    }

    private IEnumerator DashCooldownCoroutine()
    {
        canDash = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!playerMovement.IsDashing || !collision.gameObject.CompareTag("Player"))
            return;

        PlayerMovement otherMovement = collision.gameObject.GetComponent<PlayerMovement>();

        if (otherMovement != null && !otherMovement.IsBeingPushed)
        {
            Debug.Log($"Empujando a {collision.gameObject.name}!");
            
            Vector2 pushDirection = (collision.transform.position - transform.position).normalized;
            otherMovement.GetPushed(pushDirection, dashPushForce, pushDuration);
        }
    }
}