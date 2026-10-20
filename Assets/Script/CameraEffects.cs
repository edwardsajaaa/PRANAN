using UnityEngine;

public class CameraEffects : MonoBehaviour
{
    [Header("Dependencies")]
    public PlayerMovement playerMovement;
    public Camera targetCamera;

    [Header("Cinematic FOV")]
    public float walkFOV = 66f;
    public float runFOV = 74f;
    public float fovTransitionSpeed = 4f;

    [Header("Grounded Walking Dynamics")]
    public float bobFrequencyWalk = 8.5f;
    public float bobFrequencyRun = 13.0f;
    public float bobAmountYWalk = 0.055f;
    public float bobAmountYRun = 0.085f;
    public float bobAmountXWalk = 0.030f;
    public float bobAmountXRun = 0.045f;
    public float bobSmoothSpeed = 10f;

    [Header("Cinematic Neck Nod & Lean")]
    public float bobPitchNod = 1.1f;
    public float bobRollSway = 1.3f;
    public float maxStrafeRoll = 1.8f;
    public float maxTurnRoll = 1.4f;
    public float rollSmoothing = 7f;

    [Header("Organic Idle Breathing")]
    public float breathingSpeed = 1.4f;
    public float breathingAmountY = 0.007f;
    public float breathingAmountRoll = 0.25f;

    [Header("Landing Impact Cushion")]
    public float landingImpactStrength = 0.04f;
    public float landingRecoverySpeed = 6f;

    private Vector3 originalLocalPos;
    private float stepTimer = 0f;
    private float idleTimer = 0f;
    private float currentRoll = 0f;
    private float targetRoll = 0f;
    private float currentBobPitch = 0f;
    private float currentBobRoll = 0f;
    private float currentLandingDip = 0f;
    private bool wasGroundedLastFrame = true;
    private float lastAirborneVelocity = 0f;

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
        HandleLandingImpact();
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

    private void HandleLandingImpact()
    {
        if (playerMovement == null) return;

        bool isGrounded = playerMovement.IsGrounded;
        float currentVertVel = playerMovement.CurrentVelocity.y;

        if (!wasGroundedLastFrame && isGrounded && lastAirborneVelocity < -1.5f)
        {
            float impactRatio = Mathf.Clamp01(Mathf.Abs(lastAirborneVelocity) / 10f);
            currentLandingDip = impactRatio * landingImpactStrength;
        }

        if (!isGrounded)
        {
            lastAirborneVelocity = currentVertVel;
        }

        wasGroundedLastFrame = isGrounded;
        currentLandingDip = Mathf.Lerp(currentLandingDip, 0f, Time.deltaTime * landingRecoverySpeed);
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
            float speedFactor = Mathf.Clamp(speed / Mathf.Max(playerMovement.walkSpeed, 0.1f), 0.6f, 1.8f);
            float baseFreq = playerMovement.IsRunning ? bobFrequencyRun : bobFrequencyWalk;
            stepTimer += Time.deltaTime * baseFreq * speedFactor;

            float curBobY = playerMovement.IsRunning ? bobAmountYRun : bobAmountYWalk;
            float curBobX = playerMovement.IsRunning ? bobAmountXRun : bobAmountXWalk;

            float footStepBounce = -Mathf.Abs(Mathf.Sin(stepTimer)) * curBobY;
            float lateralSway = Mathf.Cos(stepTimer * 0.5f) * curBobX;

            targetPos = originalLocalPos + new Vector3(lateralSway, footStepBounce - currentLandingDip, 0f);

            targetBobPitch = Mathf.Sin(stepTimer) * bobPitchNod;
            targetBobRoll = Mathf.Cos(stepTimer * 0.5f) * bobRollSway;
        }
        else
        {
            idleTimer += Time.deltaTime * breathingSpeed;
            float breathPrimary = Mathf.Sin(idleTimer) * breathingAmountY;
            float breathSecondary = Mathf.Sin(idleTimer * 0.6f + 0.8f) * (breathingAmountY * 0.4f);
            float breathX = Mathf.Cos(idleTimer * 0.5f) * (breathingAmountY * 0.35f);

            targetPos = originalLocalPos + new Vector3(breathX, breathPrimary + breathSecondary - currentLandingDip, 0f);

            targetBobRoll = Mathf.Cos(idleTimer * 0.5f) * breathingAmountRoll;
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
        currentLandingDip = 0f;
    }
}
