using System.Collections;
using UnityEngine;

/// <summary>
/// Mengontrol senter (flashlight) bergaya first-person investigative/horror (Pamali/Outlast):
/// - Spot light dengan hotspot tengah terang dan lingkar luar memudar alami
/// - Berkas volumetrik (volumetric cone beam) yang membelah kabut dan debu hutan
/// - Reaktif terhadap FogZone: Berkas cahaya semakin tebal dan padat saat berada di dalam kabut
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

    [Header("Volumetric Beam (Cahaya Menembus Kabut)")]
    [Tooltip("Aktifkan berkas cahaya volumetrik pada senter")]
    [SerializeField] private bool enableVolumetricBeam = true;
    [Range(0f, 3f)]
    [SerializeField] private float beamIntensity = 0.55f;
    [SerializeField] private float beamLength = 22f;
    [SerializeField] private Color beamColor = new Color(0.95f, 0.97f, 1f, 0.40f);

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

    // Volumetric Cone Runtime Elements
    private GameObject beamObject;
    private MeshRenderer beamRenderer;
    private MaterialPropertyBlock beamPropertyBlock;
    private float currentBeamFade = 0f;

    private static readonly int ColorProp = Shader.PropertyToID("_Color");
    private static readonly int IntensityProp = Shader.PropertyToID("_Intensity");
    private static readonly int FogMultiplierProp = Shader.PropertyToID("_FogMultiplier");

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
        SetupVolumetricBeam();
    }

    private void OnEnable()
    {
        FogZone.OnVolumetricMultiplierChanged += HandleFogMultiplierChanged;
    }

    private void OnDisable()
    {
        FogZone.OnVolumetricMultiplierChanged -= HandleFogMultiplierChanged;
    }

    private void Start()
    {
        SetFlashlightState(startsOn, playSound: false);
        currentBeamFade = startsOn ? 1f : 0f;
        UpdateBeamProperties();
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

        UpdateVolumetricBeamAnimation();
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

    private void SetupVolumetricBeam()
    {
        if (!enableVolumetricBeam) return;

        Transform parentTransform = flashlightLight != null ? flashlightLight.transform : transform;

        // Cari atau buat objek beam anak
        Transform existing = parentTransform.Find("FlashlightVolumetricBeam");
        if (existing != null)
        {
            beamObject = existing.gameObject;
        }
        else
        {
            beamObject = new GameObject("FlashlightVolumetricBeam");
            beamObject.transform.SetParent(parentTransform, false);
            beamObject.transform.localPosition = Vector3.zero;
            beamObject.transform.localRotation = Quaternion.identity;
        }

        MeshFilter mf = beamObject.GetComponent<MeshFilter>();
        if (mf == null) mf = beamObject.AddComponent<MeshFilter>();

        beamRenderer = beamObject.GetComponent<MeshRenderer>();
        if (beamRenderer == null) beamRenderer = beamObject.AddComponent<MeshRenderer>();

        Shader beamShader = Shader.Find("PRANAN/VolumetricLightBeam");
        if (beamShader != null)
        {
            Material mat = new Material(beamShader);
            mat.name = "M_FlashlightBeam";
            beamRenderer.sharedMaterial = mat;
        }

        beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beamRenderer.receiveShadows = false;
        beamRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

        // Bangun cone mesh senter
        mf.sharedMesh = GenerateFlashlightConeMesh(defaultSpotAngle, beamLength, 16);

        beamPropertyBlock = new MaterialPropertyBlock();

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.depthTextureMode |= DepthTextureMode.Depth;
        }
    }

    private Mesh GenerateFlashlightConeMesh(float angleDegrees, float length, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "FlashlightConeMesh";

        float radius = length * Mathf.Tan(angleDegrees * 0.5f * Mathf.Deg2Rad);
        float topRadius = 0.04f; // Pangkal lensa senter

        int vertCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        float angleStep = (2f * Mathf.PI) / segments;

        for (int i = 0; i <= segments; i++)
        {
            float a = i * angleStep;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            // Ring pangkal dekat lensa senter (Z = 0.05f)
            int topIdx = i;
            vertices[topIdx] = new Vector3(cos * topRadius, sin * topRadius, 0.05f);
            normals[topIdx] = new Vector3(cos, sin, 0f).normalized;
            uvs[topIdx] = new Vector2(0f, 0f);

            // Ring ujung beam jauh
            int botIdx = (segments + 1) + i;
            vertices[botIdx] = new Vector3(cos * radius, sin * radius, length);
            normals[botIdx] = new Vector3(cos, sin, 0f).normalized;
            uvs[botIdx] = new Vector2(1f, 1f);
        }

        int[] triangles = new int[segments * 6];
        int triIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            int topA = i;
            int topB = i + 1;
            int botA = (segments + 1) + i;
            int botB = (segments + 1) + i + 1;

            triangles[triIndex++] = topA;
            triangles[triIndex++] = botA;
            triangles[triIndex++] = topB;

            triangles[triIndex++] = topB;
            triangles[triIndex++] = botA;
            triangles[triIndex++] = botB;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        return mesh;
    }

    private void UpdateVolumetricBeamAnimation()
    {
        if (beamRenderer == null) return;

        float targetFade = (IsOn && (flashlightLight == null || flashlightLight.enabled)) ? 1f : 0f;
        currentBeamFade = Mathf.MoveTowards(currentBeamFade, targetFade, Time.deltaTime * 6.5f);

        bool shouldRender = currentBeamFade > 0.001f;
        if (beamRenderer.enabled != shouldRender)
        {
            beamRenderer.enabled = shouldRender;
        }

        if (shouldRender)
        {
            UpdateBeamProperties();
        }
    }

    private void HandleFogMultiplierChanged(float multiplier)
    {
        UpdateBeamProperties();
    }

    private void UpdateBeamProperties()
    {
        if (beamRenderer == null) return;
        if (beamPropertyBlock == null) beamPropertyBlock = new MaterialPropertyBlock();

        float effectiveFog = FogZone.CurrentVolumetricMultiplier;
        Color tintedColor = new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a * currentBeamFade);

        beamRenderer.GetPropertyBlock(beamPropertyBlock);
        beamPropertyBlock.SetColor(ColorProp, tintedColor);
        beamPropertyBlock.SetFloat(IntensityProp, beamIntensity);
        beamPropertyBlock.SetFloat(FogMultiplierProp, effectiveFog);
        beamRenderer.SetPropertyBlock(beamPropertyBlock);
    }

    private void UpdateSway()
    {
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
