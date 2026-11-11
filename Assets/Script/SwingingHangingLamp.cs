using UnityEngine;

/// <summary>
/// Mensimulasikan lampu bohlam gantung berayun (pendulum physics) di pos jaga mandor.
/// Menghasilkan bayangan dinamis lembut (dynamic soft shadow) yang bergerak lambat
/// di lantai dan dinding pos jaga untuk menghadirkan atmosfer sunyi dan mencekam.
/// </summary>
public class SwingingHangingLamp : MonoBehaviour
{
    [Header("Pendulum Physics (Fisika Ayunan)")]
    [Tooltip("Panjang tali/kabel gantung dari langit-langit ke bohlam (meter)")]
    [SerializeField] private float cableLength = 1.6f;

    [Tooltip("Percepatan gravitasi")]
    [SerializeField] private float gravity = 9.81f;

    [Tooltip("Koefisien peredaman gesekan udara (semakin tinggi, semakin cepat melambat)")]
    [SerializeField] private float damping = 0.12f;

    [Tooltip("Sudut simpangan awal saat scene baru dimulai (derajat)")]
    [SerializeField] private float initialImpulseAngle = 14f;

    [Header("Wind Turbulence (Angin Hutan Berhembus)")]
    [Tooltip("Aktifkan hembusan angin hutan agar lampu terus berayun lembut tanpa henti")]
    [SerializeField] private bool enableWindTurbulence = true;

    [Tooltip("Kekuatan dorongan angin")]
    [SerializeField] private float windForce = 3.8f;

    [Tooltip("Frekuensi pergantian arah hembusan angin")]
    [SerializeField] private float windFrequency = 0.35f;

    [Header("Light & Dynamic Shadow (Cahaya & Bayangan Bergerak)")]
    [SerializeField] private Light lampLight;
    [SerializeField] private Color lightColor = new Color(1.0f, 0.74f, 0.40f); // Bohlam pijar hangat (tungsten)
    [SerializeField] private float baseIntensity = 2.4f;
    [SerializeField] private float lightRange = 11f;
    [SerializeField] private LightShadows shadowType = LightShadows.Soft;

    [Header("Voltage Instability (Kedip Listrik Pos Hutan)")]
    [Tooltip("Simulasi tegangan generator pos jaga yang sedikit berfluktuasi")]
    [SerializeField] private bool enableVoltageFlicker = true;
    [SerializeField] private float flickerVariance = 0.08f;

    [Header("Visual Setup")]
    [Tooltip("Objek visual lampu kustom (jika kosong, komponen akan membuat bohlam & kabel gantung otomatis)")]
    [SerializeField] private Transform customLampFixture;
    [SerializeField] private Color shadeColor = new Color(0.18f, 0.18f, 0.20f); // Kap lampu besi hitam

    private Transform swingPivot;
    private Transform bulbHolder;
    private LineRenderer cableLine;
    private Material bulbEmissiveMaterial;

    private float angleX = 0f;
    private float angleZ = 0f;
    private float angularVelocityX = 0f;
    private float angularVelocityZ = 0f;

    private void Awake()
    {
        SetupHierarchy();
        SetupLight();

        // Berikan simpangan awal
        angleX = initialImpulseAngle;
        angleZ = initialImpulseAngle * 0.35f; // Memberikan sedikit ayunan elips alami
    }

    private void SetupHierarchy()
    {
        // 1. Buat titik tumpu ayunan (Swing Pivot) di posisi langit-langit
        GameObject pivotObj = new GameObject("SwingPivot");
        pivotObj.transform.SetParent(transform, false);
        pivotObj.transform.localPosition = Vector3.zero;
        pivotObj.transform.localRotation = Quaternion.identity;
        swingPivot = pivotObj.transform;

        if (customLampFixture != null)
        {
            customLampFixture.SetParent(swingPivot, true);
            bulbHolder = customLampFixture;
        }
        else
        {
            BuildProceduralLamp(swingPivot);
        }
    }

    private void BuildProceduralLamp(Transform pivot)
    {
        // Objek bohlam & kap lampu di ujung bawah kabel
        GameObject bulbObj = new GameObject("BulbAndFixture");
        bulbObj.transform.SetParent(pivot, false);
        bulbObj.transform.localPosition = new Vector3(0f, -cableLength, 0f);
        bulbHolder = bulbObj.transform;

        // Visual Kap Lampu (Industrial Shade)
        GameObject shadeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shadeObj.name = "LampShade";
        shadeObj.transform.SetParent(bulbHolder, false);
        shadeObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        shadeObj.transform.localScale = new Vector3(0.38f, 0.05f, 0.38f);
        var shadeCol = shadeObj.GetComponent<Collider>();
        if (shadeCol != null) Destroy(shadeCol);

        var shadeRenderer = shadeObj.GetComponent<MeshRenderer>();
        if (shadeRenderer != null)
        {
            Material shadeMat = new Material(Shader.Find("Standard"));
            shadeMat.color = shadeColor;
            shadeMat.SetFloat("_Metallic", 0.6f);
            shadeMat.SetFloat("_Glossiness", 0.4f);
            shadeRenderer.sharedMaterial = shadeMat;
        }

        // Visual Bohlam (Glowing Bulb)
        GameObject bulbMeshObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulbMeshObj.name = "BulbMesh";
        bulbMeshObj.transform.SetParent(bulbHolder, false);
        bulbMeshObj.transform.localPosition = Vector3.zero;
        bulbMeshObj.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
        var bulbCol = bulbMeshObj.GetComponent<Collider>();
        if (bulbCol != null) Destroy(bulbCol);

        var bulbRenderer = bulbMeshObj.GetComponent<MeshRenderer>();
        if (bulbRenderer != null)
        {
            bulbEmissiveMaterial = new Material(Shader.Find("Standard"));
            bulbEmissiveMaterial.color = lightColor;
            bulbEmissiveMaterial.EnableKeyword("_EMISSION");
            bulbEmissiveMaterial.SetColor("_EmissionColor", lightColor * 2.2f);
            bulbRenderer.sharedMaterial = bulbEmissiveMaterial;
        }

        // Kabel Gantung Menggunakan LineRenderer
        GameObject cableObj = new GameObject("CableLine");
        cableObj.transform.SetParent(transform, false);
        cableLine = cableObj.AddComponent<LineRenderer>();
        cableLine.useWorldSpace = true;
        cableLine.startWidth = 0.015f;
        cableLine.endWidth = 0.015f;
        cableLine.positionCount = 2;

        Material lineMat = new Material(Shader.Find("Standard"));
        lineMat.color = new Color(0.1f, 0.1f, 0.1f);
        cableLine.sharedMaterial = lineMat;
        cableLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void SetupLight()
    {
        if (lampLight == null)
        {
            Transform lightParent = bulbHolder != null ? bulbHolder : transform;
            GameObject lightObj = new GameObject("LampPointLight");
            lightObj.transform.SetParent(lightParent, false);
            lightObj.transform.localPosition = Vector3.zero;

            lampLight = lightObj.AddComponent<Light>();
        }

        lampLight.type = LightType.Point;
        lampLight.color = lightColor;
        lampLight.intensity = baseIntensity;
        lampLight.range = lightRange;
        lampLight.shadows = shadowType;
        lampLight.shadowStrength = 0.95f;
    }

    private void Update()
    {
        SimulatePendulumPhysics();
        UpdateVisualCable();

        if (enableVoltageFlicker)
        {
            UpdateVoltageFlicker();
        }
    }

    private void SimulatePendulumPhysics()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Hembusan angin hutan (Perlin noise untuk variasi organik)
        float windX = 0f;
        float windZ = 0f;

        if (enableWindTurbulence)
        {
            windX = (Mathf.PerlinNoise(Time.time * windFrequency, 0.25f) * 2f - 1f) * windForce;
            windZ = (Mathf.PerlinNoise(0.75f, Time.time * windFrequency * 0.9f) * 2f - 1f) * (windForce * 0.4f);
        }

        // Persamaan pendulum teredam:
        // theta'' = -(g / L) * sin(theta) - damping * theta' + wind
        float effectiveLength = Mathf.Max(0.2f, cableLength);
        float accelX = -(gravity / effectiveLength) * Mathf.Sin(angleX * Mathf.Deg2Rad) * Mathf.Rad2Deg 
                       - damping * angularVelocityX + windX;

        float accelZ = -(gravity / effectiveLength) * Mathf.Sin(angleZ * Mathf.Deg2Rad) * Mathf.Rad2Deg 
                       - damping * angularVelocityZ + windZ;

        angularVelocityX += accelX * dt;
        angularVelocityZ += accelZ * dt;

        angleX += angularVelocityX * dt;
        angleZ += angularVelocityZ * dt;

        // Batasi sudut simpangan agar stabil
        angleX = Mathf.Clamp(angleX, -60f, 60f);
        angleZ = Mathf.Clamp(angleZ, -60f, 60f);

        // Terapkan rotasi pada pivot
        if (swingPivot != null)
        {
            swingPivot.localRotation = Quaternion.Euler(angleX, 0f, angleZ);
        }
    }

    private void UpdateVisualCable()
    {
        if (cableLine != null && bulbHolder != null)
        {
            cableLine.SetPosition(0, transform.position);
            cableLine.SetPosition(1, bulbHolder.position);
        }
    }

    private void UpdateVoltageFlicker()
    {
        if (lampLight == null) return;

        // Fluktuasi tegangan mikro 50Hz + noise
        float microHum = Mathf.Sin(Time.time * 60f) * 0.02f;
        float noise = (Mathf.PerlinNoise(Time.time * 12f, 1.5f) - 0.5f) * flickerVariance;

        // Sesekali kedip voltase drop singkat khas genset pos hutan
        float rareDip = 0f;
        if (Random.value < 0.003f)
        {
            rareDip = -Random.Range(0.2f, 0.45f);
        }

        float currentIntensity = Mathf.Max(0.1f, baseIntensity + microHum + noise + rareDip);
        lampLight.intensity = currentIntensity;

        if (bulbEmissiveMaterial != null)
        {
            bulbEmissiveMaterial.SetColor("_EmissionColor", lightColor * (currentIntensity * 0.9f));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.08f);

        Vector3 bottomRestPos = transform.position + Vector3.down * cableLength;
        Gizmos.DrawLine(transform.position, bottomRestPos);
        Gizmos.DrawWireSphere(bottomRestPos, 0.15f);

        // Lingkar jangkauan cahaya
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.15f);
        Gizmos.DrawWireSphere(bottomRestPos, lightRange);
    }
}
