using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity & Smoothing")]
    public float mouseSensitivity = 2f;
    public float smoothing = 15f;
    public Transform playerBody;

    [Header("Clamping Limits")]
    [Tooltip("Pembatasan sudut vertikal kamera (-80 s.d. +80 derajat)")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float targetXRotation = 0f;
    private float targetYRotation = 0f;
    private float currentXRotation = 0f;

    void Awake()
    {
        if (GetComponent<CameraEffects>() == null)
        {
            gameObject.AddComponent<CameraEffects>();
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerBody != null)
        {
            targetYRotation = playerBody.eulerAngles.y;
        }
    }

    void Update()
    {

        if (GameStateManager.Instance != null && !GameStateManager.Instance.CanLookAround())
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        targetXRotation -= mouseY;
        targetXRotation = Mathf.Clamp(targetXRotation, minPitch, maxPitch);
        targetYRotation += mouseX;

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        currentXRotation = Mathf.Lerp(currentXRotation, targetXRotation, t);
        Quaternion targetBodyRotation = Quaternion.Euler(0f, targetYRotation, 0f);

        transform.localRotation = Quaternion.Euler(currentXRotation, 0f, 0f);
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetBodyRotation, t);
        }
    }
}
