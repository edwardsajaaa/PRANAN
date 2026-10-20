using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerMovement playerMovement;
    public Camera targetCamera;

    [Header("Dynamic FOV")]
    public float walkFOV = 65f;
    public float runFOV = 75f;
    public float fovTransitionSpeed = 5f;

    [Header("Human Walking Bounce & Sway")]
    public float bobFrequencyWalk = 9.0f;
    public float bobFrequencyRun = 13.5f;
    public float bobAmountYWalk = 0.06f;
    public float bobAmountYRun = 0.09f;
    public float bobAmountXWalk = 0.035f;
    public float bobAmountXRun = 0.05f;
    public float bobSmoothSpeed = 12f;

    [Header("Head Rotational Nod & Roll")]
    public float bobPitchNod = 1.2f;
    public float bobRollSway = 1.4f;
    public float maxStrafeRoll = 2.0f;
    public float maxTurnRoll = 1.5f;
    public float rollSmoothing = 8f;

    [Header("Idle Breathing")]
    public float idleBreathingFrequency = 1.8f;
    public float idleBreathingAmount = 0.008f;

    private Vector3 originalLocalPos;
    private float stepTimer = 0f;
    private float idleTimer = 0f;
    private float currentRoll = 0f;
    private float targetRoll = 0f;
    private float currentBobPitch = 0f;
    private float currentBobRoll = 0f;

    void Start()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (playerMovement == null) playerMovement = Object.FindObjectOfType<PlayerMovement>();

        originalLocalPos = transform.localPosition;
        if (targetCamera != null)
        {
            targetCamera.fieldOfView = walkFOV;
        }
    }

    void LateUpdate()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.CanLookAround())
        {
            ResetEffects();
            return;
        }

        HandleDynamicFOV();
        HandleHeadBobAndMotion();
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

    private void HandleHeadBobAndMotion()
    {
        if (playerMovement == null) return;

        float speed = playerMovement.CurrentSpeedMagnitude;
        bool isMovingOnGround = playerMovement.IsGrounded && speed > 0.2f;

        Vector3 targetPos = originalLocalPos;
        float targetBobPitch = 0f;
        float targetBobRoll = 0f;

        if (isMovingOnGround)
        {
            float speedFactor = Mathf.Clamp(speed / Mathf.Max(playerMovement.walkSpeed, 0.1f), 0.5f, 2.0f);
            float baseFreq = playerMovement.IsRunning ? bobFrequencyRun : bobFrequencyWalk;
            stepTimer += Time.deltaTime * baseFreq * speedFactor;

            float curBobY = playerMovement.IsRunning ? bobAmountYRun : bobAmountYWalk;
            float curBobX = playerMovement.IsRunning ? bobAmountXRun : bobAmountXWalk;

            float footStepBounce = -Mathf.Abs(Mathf.Sin(stepTimer)) * curBobY;
            float lateralSway = Mathf.Cos(stepTimer * 0.5f) * curBobX;

            targetPos = originalLocalPos + new Vector3(lateralSway, footStepBounce, 0f);

            targetBobPitch = Mathf.Sin(stepTimer) * bobPitchNod;
            targetBobRoll = Mathf.Cos(stepTimer * 0.5f) * bobRollSway;
        }
        else
        {
            idleTimer += Time.deltaTime * idleBreathingFrequency;
            float breathY = Mathf.Sin(idleTimer) * idleBreathingAmount;
            float breathX = Mathf.Cos(idleTimer * 0.5f) * (idleBreathingAmount * 0.5f);
            targetPos = originalLocalPos + new Vector3(breathX, breathY, 0f);

            stepTimer = Mathf.Lerp(stepTimer, 0f, Time.deltaTime * 2f);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * bobSmoothSpeed);

        currentBobPitch = Mathf.Lerp(currentBobPitch, targetBobPitch, Time.deltaTime * bobSmoothSpeed);
        currentBobRoll = Mathf.Lerp(currentBobRoll, targetBobRoll, Time.deltaTime * bobSmoothSpeed);
    }

    private void HandleRollTilt()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float mouseX = Input.GetAxis("Mouse X");

        targetRoll = (-horizontalInput * maxStrafeRoll) + (-mouseX * maxTurnRoll);
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmoothing);

        Quaternion motionTilt = Quaternion.Euler(currentBobPitch, 0f, currentRoll + currentBobRoll);
        transform.localRotation = transform.localRotation * motionTilt;
    }

    private void ResetEffects()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPos, Time.deltaTime * bobSmoothSpeed);
        currentRoll = Mathf.Lerp(currentRoll, 0f, Time.deltaTime * rollSmoothing);
        currentBobPitch = Mathf.Lerp(currentBobPitch, 0f, Time.deltaTime * rollSmoothing);
        currentBobRoll = Mathf.Lerp(currentBobRoll, 0f, Time.deltaTime * rollSmoothing);
    }
}
