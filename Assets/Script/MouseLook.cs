using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 2f; 
    public float smoothing = 15f; 
    public Transform playerBody;

    private float targetXRotation = 0f;
    private float targetYRotation = 0f;
    private float currentXRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        targetYRotation = playerBody.eulerAngles.y;
    }

    void Update()
    {
        // 1. Ambil input mouse (Tanpa Time.deltaTime karena Mouse X/Y sudah berupa delta jarak)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 2. Hitung target rotasi
        targetXRotation -= mouseY;
        targetXRotation = Mathf.Clamp(targetXRotation, -90f, 90f);
        targetYRotation += mouseX;

        // 3. Smooth transisi rotasi menggunakan Lerp agar tidak patah-patah
        currentXRotation = Mathf.Lerp(currentXRotation, targetXRotation, Time.deltaTime * smoothing);
        Quaternion targetBodyRotation = Quaternion.Euler(0f, targetYRotation, 0f);

        // 4. Aplikasikan rotasi
        transform.localRotation = Quaternion.Euler(currentXRotation, 0f, 0f); // Kamera Atas/Bawah
        playerBody.rotation = Quaternion.Slerp(playerBody.rotation, targetBodyRotation, Time.deltaTime * smoothing); // Karakter Kiri/Kanan
    }
}
