using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerMovement playerMovement;
    public Camera targetCamera;

    [Header("Dynamic FOV (W1-03)")]
    public float walkFOV = 65f;
    public float runFOV = 75f;
    public float fovTransitionSpeed = 5f;

    [Header("Procedural Head-Bob (Dual-Sine Curve)")]
    public float bobFrequencyWalk = 10f;
    public float bobFrequencyRun = 14f;
    public float bobAmountY = 0.04f;
    public float bobAmountX = 0.025f;
    public float bobSmoothSpeed = 10f;

    [Header("Roll Tilt on Strafe / Turn")]
    public float maxStrafeRoll = 1.8f;
    public float maxTurnRoll = 1.2f;
    public float rollSmoothing = 6f;

    private Vector3 originalLocalPos;
    private float timer = 0f;
    private float currentRoll = 0f;
    private float targetRoll = 0f;

    void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();

        originalLocalPos = transform.localPosition;
        if (targetCamera != null)
        {
            targetCamera.fieldOfView = walkFOV;
        }
    }

    void Update()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.CanLookAround())
        {
            ResetEffects();
            return;
        }

        HandleDynamicFOV();
        HandleHeadBob();
        HandleRollTilt();
    }

    private void HandleDynamicFOV()
    {
        if (targetCamera == null || playerMovement == null) return;

        float targetFOV = playerMovement.IsRunning && playerMovement.CurrentSpeedMagnitude > 0.5f
            ? runFOV
            : walkFOV;

        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
    }

    private void HandleHeadBob()
    {
        if (playerMovement == null) return;

        float speed = playerMovement.CurrentSpeedMagnitude;
        bool isMovingOnGround = playerMovement.IsGrounded && speed > 0.2f;

        Vector3 targetPos = originalLocalPos;

        if (isMovingOnGround)
        {
            float frequency = playerMovement.IsRunning ? bobFrequencyRun : bobFrequencyWalk;
            timer += Time.deltaTime * frequency;

            float bobY = Mathf.Sin(timer) * bobAmountY;
            float bobX = Mathf.Cos(timer * 0.5f) * bobAmountX;

            targetPos = originalLocalPos + new Vector3(bobX, bobY, 0f);
        }
        else
        {
            timer = 0f;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * bobSmoothSpeed);
    }

    private void HandleRollTilt()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float mouseX = Input.GetAxis("Mouse X");

        targetRoll = (-horizontalInput * maxStrafeRoll) + (-mouseX * maxTurnRoll);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmoothing);

        Vector3 currentEuler = transform.localEulerAngles;

        transform.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, currentRoll);
    }

    private void ResetEffects()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPos, Time.deltaTime * bobSmoothSpeed);
        currentRoll = Mathf.Lerp(currentRoll, 0f, Time.deltaTime * rollSmoothing);
    }
}
