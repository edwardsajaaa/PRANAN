using System.Collections;
using UnityEngine;

/// <summary>
/// Menghasilkan tetesan air dari ujung atap seng pos jaga (M_Roof_CorrugatedZinc)
/// yang jatuh ke tanah berlumpur di bawahnya lengkap dengan partikel percikan (splash ripple)
/// dan audio 3D spasial tetesan air alami.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class RoofWaterDripEmitter : MonoBehaviour
{
    [Header("Eave Dimensions (Panjang Tepi Atap Seng)")]
    [Tooltip("Panjang bentangan tepi atap seng tempat air menetes (meter)")]
    [SerializeField] private float eaveLength = 4.5f;

    [Tooltip("Tinggi jatuhnya tetesan air dari atap seng ke tanah berlumpur (meter)")]
    [SerializeField] private float fallDistance = 2.8f;

    [Tooltip("Frekuensi rata-rata tetesan per detik")]
    [SerializeField] private float dripsPerSecond = 3.5f;

    [Header("Visual Tetesan Air (Droplet)")]
    [SerializeField] private Color dropletColor = new Color(0.85f, 0.92f, 1.0f, 0.75f);
    [SerializeField] private float dropletSize = 0.025f;

    [Header("Audio Tetesan Air 3D (Spasial)")]
    [Tooltip("Aktifkan suara tetesan air di bawah atap seng")]
    [SerializeField] private bool enableDripAudio = true;
    [SerializeField] private AudioClip customDripClip;
    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 0.45f;
    [SerializeField] private float minIntervalSeconds = 1.2f;
    [SerializeField] private float maxIntervalSeconds = 3.8f;

    private ParticleSystem dripPS;
    private ParticleSystemRenderer dripRenderer;
    private ParticleSystem splashPS;
    private AudioSource audioSource;

    private void Awake()
    {
        dripPS = GetComponent<ParticleSystem>();
        dripRenderer = GetComponent<ParticleSystemRenderer>();

        SetupDropletParticleSystem();
        SetupSplashSubEmitter();

        if (enableDripAudio)
        {
            SetupAudioSource();
        }
    }

    private void Start()
    {
        if (enableDripAudio)
        {
            StartCoroutine(DripAudioRoutine());
        }
    }

    private void SetupDropletParticleSystem()
    {
        var main = dripPS.main;
        main.startLifetime = Mathf.Sqrt(2f * fallDistance / 9.81f); // Waktu jatuh bebas t = sqrt(2h/g)
        main.startSpeed = 0.2f;
        main.startSize = dropletSize;
        main.startColor = dropletColor;
        main.gravityModifier = 1.0f; // Jatuh dengan percepatan gravitasi bumi
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var emission = dripPS.emission;
        emission.rateOverTime = dripsPerSecond;

        // Distribusi tetesan memanjang di sepanjang tepi atap seng (Garis/Edge)
        var shape = dripPS.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = eaveLength * 0.5f;
        shape.rotation = new Vector3(0f, 90f, 0f); // Sejajar dengan sumbu Z/X tepi atap

        // Tetesan air memanjang (stretched billboard vertikal) saat melesat jatuh
        dripRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        dripRenderer.cameraVelocityScale = 0f;
        dripRenderer.velocityScale = 0.06f;
        dripRenderer.lengthScale = 1.8f;

        Shader shader = Shader.Find("PRANAN/SoftGlowParticle");
        if (shader != null)
        {
            Material dropMat = new Material(shader);
            dropMat.name = "M_WaterDroplet";
            dropMat.SetColor("_Color", dropletColor);
            dropMat.SetFloat("_PulseAmount", 0f);
            dripRenderer.sharedMaterial = dropMat;
        }
    }

    private void SetupSplashSubEmitter()
    {
        // Buat objek anak untuk partikel cipratan air saat menyentuh tanah
        Transform splashObj = transform.Find("SplashParticleSystem");
        if (splashObj == null)
        {
            GameObject go = new GameObject("SplashParticleSystem");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, -fallDistance, 0f);
            splashPS = go.AddComponent<ParticleSystem>();
        }
        else
        {
            splashPS = splashObj.GetComponent<ParticleSystem>();
        }

        var main = splashPS.main;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.5f;
        main.startSize = 0.06f;
        main.startColor = new Color(dropletColor.r, dropletColor.g, dropletColor.b, 0.45f);
        main.gravityModifier = 0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = splashPS.emission;
        emission.rateOverTime = 0; // Triggered on collision / interval

        var shape = splashPS.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = eaveLength * 0.5f;

        var renderer = splashPS.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        Shader shader = Shader.Find("PRANAN/SoftGlowParticle");
        if (shader != null)
        {
            Material splashMat = new Material(shader);
            splashMat.name = "M_WaterSplash";
            splashMat.SetColor("_Color", dropletColor);
            splashMat.SetFloat("_PulseAmount", 0f);
            renderer.sharedMaterial = splashMat;
        }
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1.0f; // Full 3D spatial audio
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 12.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.playOnAwake = false;

        if (customDripClip == null)
        {
            customDripClip = GenerateProceduralWaterDripClip();
        }
    }

    /// <summary>
    /// Sintesis audio tetesan air alami berbasis gelombang sinus teredam eksponensial (zero external files).
    /// </summary>
    private AudioClip GenerateProceduralWaterDripClip()
    {
        int sampleRate = 44100;
        float duration = 0.12f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float baseFreq = 950f; // Frekuensi denting tetesan air
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Nada naik sedikit di awal khas tetesan air (bubble plip)
            float freq = baseFreq + (t * 800f);
            float envelope = Mathf.Exp(-t * 45f); // Redaman eksponensial cepat
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
        }

        AudioClip clip = AudioClip.Create("ProceduralWaterDrip", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private IEnumerator DripAudioRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minIntervalSeconds, maxIntervalSeconds);
            yield return new WaitForSeconds(waitTime);

            if (audioSource != null && customDripClip != null)
            {
                // Variasi pitch acak (0.85x s.d. 1.2x) agar suara terdengar organik
                audioSource.pitch = Random.Range(0.85f, 1.2f);
                audioSource.PlayOneShot(customDripClip, audioVolume * Random.Range(0.8f, 1.0f));

                if (splashPS != null)
                {
                    splashPS.Emit(3);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);

        Vector3 eaveA = transform.position - transform.right * (eaveLength * 0.5f);
        Vector3 eaveB = transform.position + transform.right * (eaveLength * 0.5f);
        Gizmos.DrawLine(eaveA, eaveB);

        // Garis batas jatuhnya tetesan air ke tanah
        Vector3 groundA = eaveA + Vector3.down * fallDistance;
        Vector3 groundB = eaveB + Vector3.down * fallDistance;
        Gizmos.color = new Color(0.2f, 0.5f, 0.9f, 0.35f);
        Gizmos.DrawLine(groundA, groundB);
        Gizmos.DrawLine(eaveA, groundA);
        Gizmos.DrawLine(eaveB, groundB);
    }
}
