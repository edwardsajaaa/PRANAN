using UnityEngine;

/// <summary>
/// Menghasilkan partikel spora dan debu mikro hutan (Dust Motes) di sekitar pemain.
/// Partikel melayang lembut di udara dan secara otomatis memantulkan kilauan cahaya (glint/shimmer)
/// saat terkena sorotan kerucut lampu senter dari FlashlightController.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class AmbientDustMotes : MonoBehaviour
{
    [Header("Radius & Volume")]
    [Tooltip("Radius sebaran partikel debu di sekitar pemain/kamera (meter)")]
    [SerializeField] private float spawnRadius = 8.0f;

    [Tooltip("Jumlah maksimum partikel debu/spora")]
    [SerializeField] private int maxParticles = 140;

    [Tooltip("Kecepatan melayang partikel di udara (Brownian drift)")]
    [SerializeField] private float driftSpeed = 0.22f;

    [Header("Visual & Ukuran")]
    [SerializeField] private float minParticleSize = 0.02f;
    [SerializeField] private float maxParticleSize = 0.05f;
    [SerializeField] private Color baseAmbientColor = new Color(0.85f, 0.92f, 1.0f, 0.28f);
    [SerializeField] private Color flashlightLitColor = new Color(1.0f, 0.98f, 0.82f, 1.0f);

    [Header("Senter (Flashlight Integration)")]
    [Tooltip("Referensi ke FlashlightController. Jika kosong, script akan mencarinya otomatis.")]
    [SerializeField] private FlashlightController flashlight;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;
    private Material dustMaterial;
    private Transform targetFollow;

    private static readonly int FlashlightPosProp = Shader.PropertyToID("_FlashlightPos");
    private static readonly int FlashlightForwardProp = Shader.PropertyToID("_FlashlightForward");
    private static readonly int FlashlightCosInnerProp = Shader.PropertyToID("_FlashlightCosInner");
    private static readonly int FlashlightCosOuterProp = Shader.PropertyToID("_FlashlightCosOuter");
    private static readonly int FlashlightRangeProp = Shader.PropertyToID("_FlashlightRange");
    private static readonly int FlashlightActiveProp = Shader.PropertyToID("_FlashlightActive");
    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int LitColorProp = Shader.PropertyToID("_LitColor");

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();

        SetupDustMaterial();
        ConfigureParticleSystem();
    }

    private void Start()
    {
        FindPlayerOrCamera();
        if (flashlight == null)
        {
            flashlight = FindObjectOfType<FlashlightController>();
        }
    }

    private void FindPlayerOrCamera()
    {
        if (Camera.main != null)
        {
            targetFollow = Camera.main.transform;
        }
        else
        {
            PlayerMovement pm = FindObjectOfType<PlayerMovement>();
            if (pm != null) targetFollow = pm.transform;
        }
    }

    private void SetupDustMaterial()
    {
        Shader shader = Shader.Find("PRANAN/DustMoteParticle");
        if (shader != null)
        {
            dustMaterial = new Material(shader);
            dustMaterial.name = "M_AmbientDustMotes";
            dustMaterial.SetColor(BaseColorProp, baseAmbientColor);
            dustMaterial.SetColor(LitColorProp, flashlightLitColor);
            psRenderer.sharedMaterial = dustMaterial;
        }
    }

    private void ConfigureParticleSystem()
    {
        var main = ps.main;
        main.maxParticles = maxParticles;
        main.startLifetime = 12f;
        main.startSpeed = driftSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(minParticleSize, maxParticleSize);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var emission = ps.emission;
        emission.rateOverTime = maxParticles / 10f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = spawnRadius;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.25f;
        noise.scrollSpeed = 0.1f;
        noise.damping = true;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortMode = ParticleSystemSortMode.Distance;
    }

    private void Update()
    {
        if (targetFollow == null)
        {
            FindPlayerOrCamera();
            if (targetFollow == null) return;
        }

        // Pindahkan posisi emitter mengikuti player tanpa memindahkan partikel world-space yang sedang melayang
        transform.position = targetFollow.position;

        UpdateFlashlightUniforms();
    }

    private void UpdateFlashlightUniforms()
    {
        if (dustMaterial == null) return;

        bool active = false;
        Vector3 pos = Vector3.zero;
        Vector3 forward = Vector3.forward;
        float cosInner = 0.9f;
        float cosOuter = 0.7f;
        float range = 30f;

        if (flashlight != null)
        {
            active = flashlight.IsOn;
            pos = flashlight.transform.position;
            forward = flashlight.transform.forward;

            Light spotLight = flashlight.GetComponentInChildren<Light>();
            if (spotLight != null && spotLight.type == LightType.Spot)
            {
                cosInner = Mathf.Cos(spotLight.innerSpotAngle * 0.5f * Mathf.Deg2Rad);
                cosOuter = Mathf.Cos(spotLight.spotAngle * 0.5f * Mathf.Deg2Rad);
                range = spotLight.range;
            }
        }

        dustMaterial.SetVector(FlashlightPosProp, pos);
        dustMaterial.SetVector(FlashlightForwardProp, forward);
        dustMaterial.SetFloat(FlashlightCosInnerProp, cosInner);
        dustMaterial.SetFloat(FlashlightCosOuterProp, cosOuter);
        dustMaterial.SetFloat(FlashlightRangeProp, range);
        dustMaterial.SetFloat(FlashlightActiveProp, active ? 1.0f : 0.0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1.0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
