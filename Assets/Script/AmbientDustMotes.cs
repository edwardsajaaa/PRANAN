using UnityEngine;

/// <summary>
/// Menghasilkan partikel spora dan debu mikro hutan (Dust Motes) di sekitar pemain.
/// Partikel melayang lembut di udara dan secara otomatis memantulkan kilauan cahaya (glint/shimmer)
/// saat terkena sorotan kerucut lampu senter dari FlashlightController.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
public class AmbientDustMotes : MonoBehaviour
{
    [Header("Radius & Volume")]
    [Tooltip("Radius sebaran partikel debu di sekitar pemain/kamera (meter)")]
    [SerializeField] private float spawnRadius = 8.0f;

    [Tooltip("Jumlah maksimum partikel debu/spora")]
    [SerializeField] private int maxParticles = 140;

    [Tooltip("Kecepatan melayang partikel di udara (Brownian drift)")]
    [SerializeField] private float driftSpeed = 0.18f;

    [Header("Visual & Ukuran Mikro")]
    [SerializeField] private float minParticleSize = 0.015f;
    [SerializeField] private float maxParticleSize = 0.038f;
    [SerializeField] private Color baseAmbientColor = new Color(0.75f, 0.85f, 0.95f, 0.25f);
    [SerializeField] private Color flashlightLitColor = new Color(1.0f, 0.95f, 0.75f, 1.0f);

    [Header("Material")]
    [SerializeField] private Material customMaterial;

    [Header("Senter (Flashlight Integration)")]
    [Tooltip("Referensi ke FlashlightController. Jika kosong, script akan mencarinya otomatis.")]
    [SerializeField] private FlashlightController flashlight;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;
    private Material activeMaterial;
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
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
    }

    private void Reset()
    {
        Initialize();
    }

    private void OnValidate()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (ps == null) ps = GetComponent<ParticleSystem>();
        if (psRenderer == null) psRenderer = GetComponent<ParticleSystemRenderer>();

        SetupDustMaterial();
        ConfigureParticleSystem();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            FindPlayerOrCamera();
            if (flashlight == null)
            {
                flashlight = FindObjectOfType<FlashlightController>();
            }
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
        if (psRenderer == null) return;

        if (customMaterial != null)
        {
            activeMaterial = customMaterial;
            psRenderer.sharedMaterial = activeMaterial;
            return;
        }

        // Cari material permanen M_AmbientDustMotes di project
        if (activeMaterial == null)
        {
#if UNITY_EDITOR
            activeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_AmbientDustMotes.mat");
#endif
            if (activeMaterial == null)
            {
                Shader shader = Shader.Find("PRANAN/DustMoteParticle");
                if (shader != null)
                {
                    activeMaterial = new Material(shader);
                    activeMaterial.name = "M_AmbientDustMotes";
                }
            }
        }

        if (activeMaterial != null)
        {
            activeMaterial.SetColor(BaseColorProp, baseAmbientColor);
            activeMaterial.SetColor(LitColorProp, flashlightLitColor);
            psRenderer.sharedMaterial = activeMaterial;
        }
    }

    private void ConfigureParticleSystem()
    {
        if (ps == null) return;

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

        if (psRenderer != null)
        {
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            psRenderer.sortMode = ParticleSystemSortMode.Distance;
        }
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (targetFollow == null)
            {
                FindPlayerOrCamera();
                if (targetFollow == null) return;
            }

            transform.position = targetFollow.position;
            UpdateFlashlightUniforms();
        }
    }

    private void UpdateFlashlightUniforms()
    {
        if (activeMaterial == null) return;

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

        activeMaterial.SetVector(FlashlightPosProp, pos);
        activeMaterial.SetVector(FlashlightForwardProp, forward);
        activeMaterial.SetFloat(FlashlightCosInnerProp, cosInner);
        activeMaterial.SetFloat(FlashlightCosOuterProp, cosOuter);
        activeMaterial.SetFloat(FlashlightRangeProp, range);
        activeMaterial.SetFloat(FlashlightActiveProp, active ? 1.0f : 0.0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1.0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
