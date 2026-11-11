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
    [SerializeField] private float minSize = 0.05f;
    [SerializeField] private float maxSize = 0.09f;
    [SerializeField] private float blinkSpeed = 2.4f;

    private ParticleSystem ps;
    private ParticleSystemRenderer psRenderer;
    private Material fireflyMaterial;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        psRenderer = GetComponent<ParticleSystemRenderer>();

        SetupFireflyMaterial();
        ConfigureParticleSystem();
    }

    private void SetupFireflyMaterial()
    {
        Shader shader = Shader.Find("PRANAN/SoftGlowParticle");
        if (shader != null)
        {
            fireflyMaterial = new Material(shader);
            fireflyMaterial.name = "M_FireflyGlow";
            fireflyMaterial.SetColor("_Color", glowColor);
            fireflyMaterial.SetFloat("_PulseSpeed", blinkSpeed);
            fireflyMaterial.SetFloat("_PulseAmount", 0.55f);
            psRenderer.sharedMaterial = fireflyMaterial;
        }
    }

    private void ConfigureParticleSystem()
    {
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

        // Turbulence untuk gerakan meliuk-liuk alami seperti serangga hidup
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

        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sortMode = ParticleSystemSortMode.Distance;
    }

    private void Update()
    {
        if (mode == FireflyBehaviorMode.AttractedToLight && lightTarget != null)
        {
            // Emitter mengikuti posisi lampu gantung
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
