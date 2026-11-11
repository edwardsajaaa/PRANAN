using System.Collections;
using UnityEngine;

/// <summary>
/// Mengontrol senter (flashlight) bergaya first-person investigative/horror (Pamali/Outlast):
/// - Spot light dengan hotspot tengah terang dan lingkar luar memudar alami
/// - Viewmodel 3D senter di sudut kanan bawah layar
/// - Efek ayunan kamera (lag/sway) saat menoleh agar terasa realistis di tangan
/// - Efek bobbing halus saat karakter melangkah
/// - Suara klik sakelar dan fitur flicker (kedip-kedip horor) untuk momen cerita
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Komponen Utama")]
    [Tooltip("Lampu Spot Light senter")]
    [SerializeField] private Light flashlightLight;

    [Tooltip("Objek 3D visual senter (viewmodel tangan kanan bawah)")]
    [SerializeField] private GameObject flashlightModel;

    [Header("Pengaturan Kontrol")]
    [Tooltip("Tombol keyboard untuk menyalakan/mematikan senter (Default: F)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    [Tooltip("Apakah senter menyala saat game baru dimulai (default: mati/false)")]
    [SerializeField] private bool startsOn = false;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchOnSound;
    [SerializeField] private AudioClip switchOffSound;

    [Header("Efek Ayunan Senter (Sway & Lag)")]
    [Tooltip("Memberi bobot realistis pada senter saat player menggerakkan mouse")]
    [SerializeField] private bool enableSway = true;
    [SerializeField] private float swayAmount = 1.8f;
    [SerializeField] private float maxSwayAngle = 6.0f;
    [SerializeField] private float swaySmoothness = 7.0f;

    [Header("Efek Langkah Kaki (Bobbing)")]
    [Tooltip("Goyangan halus naik-turun saat karakter berjalan")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobFrequency = 4.8f;
    [SerializeField] private float bobAmountY = 0.012f;
    [SerializeField] private float bobAmountX = 0.008f;

    [Header("Pengaturan Cahaya (Preset Pamali/Horor)")]
    [SerializeField] private float defaultIntensity = 2.4f;
    [SerializeField] private float defaultRange = 30f;
    [SerializeField] private float defaultSpotAngle = 65f;
    [SerializeField] private float defaultInnerSpotAngle = 36f;
    [SerializeField] private Color lightColor = new Color(0.96f, 0.98f, 1f, 1f);

    public bool IsOn { get; private set; }

    private Quaternion initialLocalRotation;
    private Vector3 initialLocalPosition;
    private float bobTimer = 0f;
    private Coroutine flickerRoutine;

    private void Awake()
    {
        initialLocalRotation = transform.localRotation;
        initialLocalPosition = transform.localPosition;

        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ApplyLightPreset();
    }

    private void Start()
    {
        SetFlashlightState(startsOn, playSound: false);
    }

    private void Update()
    {
        HandleInput();

        if (enableSway)
        {
            UpdateSway();
        }

        if (enableBobbing)
        {
            UpdateBobbing();
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        SetFlashlightState(!IsOn, playSound: true);
    }

    public void SetFlashlightState(bool on, bool playSound = true)
    {
        IsOn = on;

        if (flashlightLight != null)
        {
            flashlightLight.enabled = IsOn;
        }

        if (flashlightModel != null)
        {
            // Model senter bisa tetap terlihat atau disembunyikan sesuai kebutuhan
            // Biasanya di game first person, modelnya tetap ada di tangan
            flashlightModel.SetActive(true);
        }

        if (playSound && audioSource != null)
        {
            AudioClip clip = IsOn ? switchOnSound : switchOffSound;
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    private void UpdateSway()
    {
        // Membaca pergerakan mouse untuk rotasi lag halus
        float mouseX = -Input.GetAxis("Mouse X") * swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * swayAmount;

        mouseX = Mathf.Clamp(mouseX, -maxSwayAngle, maxSwayAngle);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAngle, maxSwayAngle);

        Quaternion targetRotation = initialLocalRotation * Quaternion.Euler(mouseY, mouseX, 0f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * swaySmoothness);
    }

    private void UpdateBobbing()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float newY = initialLocalPosition.y + Mathf.Sin(bobTimer * 2f) * bobAmountY;
            float newX = initialLocalPosition.x + Mathf.Cos(bobTimer) * bobAmountX;
            transform.localPosition = Vector3.Lerp(transform.localPosition, new Vector3(newX, newY, initialLocalPosition.z), Time.deltaTime * 8f);
        }
        else
        {
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialLocalPosition, Time.deltaTime * 6f);
        }
    }

    public void ApplyLightPreset()
    {
        if (flashlightLight == null) return;

        flashlightLight.type = LightType.Spot;
        flashlightLight.range = defaultRange;
        flashlightLight.spotAngle = defaultSpotAngle;
        flashlightLight.innerSpotAngle = defaultInnerSpotAngle;
        flashlightLight.intensity = defaultIntensity;
        flashlightLight.color = lightColor;
        flashlightLight.shadows = LightShadows.Soft;
    }

    /// <summary>
    /// Efek flicker / kedip-kedip lampu untuk membangun ketegangan atmosfer.
    /// </summary>
    public void TriggerFlicker(float duration = 1.2f, int flickerCount = 5)
    {
        if (!IsOn || flickerRoutine != null) return;
        flickerRoutine = StartCoroutine(FlickerRoutine(duration, flickerCount));
    }

    private IEnumerator FlickerRoutine(float duration, int count)
    {
        float interval = duration / (count * 2);
        for (int i = 0; i < count; i++)
        {
            if (flashlightLight != null) flashlightLight.enabled = false;
            yield return new WaitForSeconds(interval * Random.Range(0.4f, 1.2f));
            if (flashlightLight != null) flashlightLight.enabled = true;
            yield return new WaitForSeconds(interval * Random.Range(0.4f, 1.2f));
        }

        if (flashlightLight != null) flashlightLight.enabled = IsOn;
        flickerRoutine = null;
    }
}

