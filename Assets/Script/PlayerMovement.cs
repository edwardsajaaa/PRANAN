using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float walkSpeed = 4.5f;
    public float runSpeed = 8.0f;
    public KeyCode runKey = KeyCode.LeftShift;

    [Header("Inertia & Acceleration Curves")]
    [Tooltip("Laju akselerasi karakter menuju kecepatan target")]
    public float accelerationRate = 12f;
    [Tooltip("Laju deselerasi saat input dilepas")]
    public float decelerationRate = 15f;

    [Header("Slope & Gravity Settings")]
    public float slopeLimit = 45f;
    public float stepOffset = 0.3f;
    public float gravity = -20f;
    public float groundedGravity = -5f;
    public float slopeStickForce = 8f;

    [Header("Ground & Surface Detection")]
    public LayerMask groundLayer = ~0;
    public float groundCheckDistance = 1.2f;

    private CharacterController characterController;
    private Vector3 currentHorizontalVelocity;
    private float verticalVelocity;
    private float currentDragMultiplier = 1f;
    private bool isRunning = false;

    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public bool IsRunning => isRunning;
    public float CurrentSpeedMagnitude => currentHorizontalVelocity.magnitude;
    public Vector3 CurrentVelocity => currentHorizontalVelocity + Vector3.up * verticalVelocity;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        characterController.slopeLimit = slopeLimit;
        characterController.stepOffset = stepOffset;
    }

    void Update()
    {

        if (GameStateManager.Instance != null && !GameStateManager.Instance.CanPlayerMove())
        {
            currentHorizontalVelocity = Vector3.zero;
            verticalVelocity = groundedGravity;
            return;
        }

        CheckSurfaceDrag();
        HandleHorizontalMovement();
        HandleVerticalMovement();

        Vector3 finalMovement = (currentHorizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
        characterController.Move(finalMovement);
    }

    private void CheckSurfaceDrag()
    {
        currentDragMultiplier = 1f;

        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Collide))
        {
            SurfaceModifier surface = hit.collider.GetComponent<SurfaceModifier>();
            if (surface != null)
            {
                currentDragMultiplier = surface.GetSpeedMultiplier();
            }
            else if (hit.collider.CompareTag("Mud"))
            {
                currentDragMultiplier = 0.65f;
            }
        }
    }

    private void HandleHorizontalMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = (transform.right * horizontal + transform.forward * vertical).normalized;

        isRunning = Input.GetKey(runKey) && inputDir.magnitude > 0.1f;
        float baseTargetSpeed = isRunning ? runSpeed : walkSpeed;
        float targetSpeed = baseTargetSpeed * currentDragMultiplier;

        Vector3 targetVelocity = inputDir * targetSpeed;

        float rate = (inputDir.magnitude > 0.1f)
            ? (accelerationRate * currentDragMultiplier)
            : decelerationRate;

        currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, rate * Time.deltaTime);
    }

    private void HandleVerticalMovement()
    {
        if (characterController.isGrounded)
        {

            verticalVelocity = -slopeStickForce;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<SurfaceModifier>(out var surface))
        {
            currentDragMultiplier = Mathf.Min(currentDragMultiplier, surface.GetSpeedMultiplier());
        }
        else if (other.CompareTag("Mud"))
        {
            currentDragMultiplier = Mathf.Min(currentDragMultiplier, 0.65f);
        }
    }
}
