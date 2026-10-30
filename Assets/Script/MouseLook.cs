using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Sensitivity & Smoothing")]
    public float mouseSensitivity = 2f;
    [Tooltip("Aktifkan jika ingin kamera memiliki perlambatan halus (cinematic). Matikan untuk respon mouse instan 1:1 tanpa lag.")]
    public bool smoothCamera = false;
    [Tooltip("Tingkat kelancaran kamera jika smoothCamera aktif. Nilai tinggi = lebih responsif.")]
    public float smoothing = 25f;
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

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        targetXRotation -= mouseY;
        targetXRotation = Mathf.Clamp(targetXRotation, minPitch, maxPitch);
        targetYRotation += mouseX;

        if (!smoothCamera || smoothing <= 0f)
        {
            currentXRotation = targetXRotation;
            transform.localRotation = Quaternion.Euler(currentXRotation, 0f, 0f);

            if (playerBody != null)
            {
                playerBody.rotation = Quaternion.Euler(0f, targetYRotation, 0f);
            }
        }
        else
        {
            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            currentXRotation = Mathf.Lerp(currentXRotation, targetXRotation, t);
            transform.localRotation = Quaternion.Euler(currentXRotation, 0f, 0f);

            if (playerBody != null)
            {
                Quaternion targetBodyRotation = Quaternion.Euler(0f, targetYRotation, 0f);
                playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetBodyRotation, t);
            }
        }
    }
}
