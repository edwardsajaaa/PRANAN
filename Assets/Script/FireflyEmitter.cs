using UnityEngine;

public enum FireflyBehaviorMode
{
    ForestBushes,       // Melayang acak di semak-semak dan dasar hutan
    AttractedToLight    // Berputar dan tertarik di sekitar lampu pos jaga
}

/// <summary>
/// Menghasilkan partikel serangga / kunang-kunang malam bercahaya kuning-kehijauan.
/// Dapat ditaruh di semak-semak hutan atau diatur agar mengerumuni lampu bohlam pos jaga.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
public class FireflyEmitter : MonoBehaviour
{
    [Header("Behavior Mode")]
    [Tooltip("Pola pergerakan kunang-kunang")]
    [SerializeField] private FireflyBehaviorMode mode = FireflyBehaviorMode.ForestBushes;

    [Tooltip("Target cahaya yang dikerumuni (khusus mode AttractedToLight, misal Lampu_Gantung_PosJaga)")]
    [SerializeField] private Transform lightTarget;

    [Header("Area & Kepadatan")]
    [Tooltip("Radius sebaran terbang kunang-kunang")]
    [SerializeField] private float areaRadius = 4.5f;

    [Tooltip("Jumlah kunang-kunang aktif")]
    [SerializeField] private int fireflyCount = 20;

    [Tooltip("Kecepatan terbang mengembara")]
    [SerializeField] private float flightSpeed = 0.5f;

    [Header("Bioluminescence (Cahaya Kunang-kunang)")]
    [SerializeField] private Color glowColor = new Color(0.78f, 0.98f, 0.28f, 1.0f); // Kuning-kehijauan
    [SerializeField] private float minSize = 0.04f;
    [SerializeField] private float maxSize = 0.08f;
    [SerializeField] private float blinkSpeed = 2.4f;

    [Header("Material")]
    [SerializeField] private Material customMaterial;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;
    private Material activeMaterial;

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

        SetupFireflyMaterial();
        ConfigureParticleSystem();
    }

    private void SetupFireflyMaterial()
    {
        if (psRenderer == null) return;

        if (customMaterial != null)
        {
            activeMaterial = customMaterial;
            psRenderer.sharedMaterial = activeMaterial;
            return;
        }

        if (activeMaterial == null)
        {
#if UNITY_EDITOR
            activeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_FireflyGlow.mat");
#endif
            if (activeMaterial == null)
            {
                Shader shader = Shader.Find("PRANAN/SoftGlowParticle");
                if (shader != null)
                {
                    activeMaterial = new Material(shader);
                    activeMaterial.name = "M_FireflyGlow";
                }
            }
        }

        if (activeMaterial != null)
        {
            activeMaterial.SetColor("_Color", glowColor);
            activeMaterial.SetFloat("_PulseSpeed", blinkSpeed);
            activeMaterial.SetFloat("_PulseAmount", 0.55f);
            psRenderer.sharedMaterial = activeMaterial;
        }
    }

    private void ConfigureParticleSystem()
    {
        if (ps == null) return;

        var main = ps.main;
        main.maxParticles = fireflyCount;
        main.startLifetime = 14f;
        main.startSpeed = flightSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var emission = ps.emission;
        emission.rateOverTime = fireflyCount / 5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = areaRadius;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.65f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) }
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
        if (mode == FireflyBehaviorMode.AttractedToLight && lightTarget != null)
        {
            transform.position = lightTarget.position;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.78f, 0.98f, 0.28f, 0.35f);
        Vector3 center = (mode == FireflyBehaviorMode.AttractedToLight && lightTarget != null) ? lightTarget.position : transform.position;
        Gizmos.DrawWireSphere(center, areaRadius);

        if (mode == FireflyBehaviorMode.AttractedToLight && lightTarget != null)
        {
            Gizmos.DrawLine(transform.position, lightTarget.position);
        }
    }
}
