using System.Collections;
using UnityEngine;

/// <summary>
/// Menghasilkan tetesan air dari ujung atap seng pos jaga (M_Roof_CorrugatedZinc)
/// yang jatuh ke tanah berlumpur di bawahnya lengkap dengan partikel percikan (splash ripple)
/// dan audio 3D spasial tetesan air alami.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
public class RoofWaterDripEmitter : MonoBehaviour
{
    [Header("Eave Dimensions (Panjang Tepi Atap Seng)")]
    [Tooltip("Panjang bentangan tepi atap seng tempat air menetes (meter)")]
    [SerializeField] private float eaveLength = 4.5f;

    [Tooltip("Tinggi jatuhnya tetesan air dari atap seng ke tanah berlumpur (meter)")]
    [SerializeField] private float fallDistance = 2.8f;

    [Tooltip("Frekuensi rata-rata tetesan per detik (nilai kecil = tetesan semakin sedikit dan jarang)")]
    [Range(0.05f, 6.0f)]
    [SerializeField] private float dripsPerSecond = 0.8f;

    [Header("Visual Tetesan Air (Droplet)")]
    [SerializeField] private Color dropletColor = new Color(0.85f, 0.92f, 1.0f, 0.75f);
    [SerializeField] private float dropletSize = 0.025f;

    [Header("Materials (Otomatis)")]
    [SerializeField] private Material customDropletMaterial;
    [SerializeField] private Material customSplashMaterial;

    [Header("Audio Tetesan Air 3D (Spasial)")]
    [Tooltip("Aktifkan suara tetesan air di bawah atap seng")]
    [SerializeField] private bool enableDripAudio = true;
    [SerializeField] private AudioClip customDripClip;
    [Range(0f, 1f)]
    [SerializeField] private float audioVolume = 0.45f;
    [Tooltip("Jeda waktu minimal antara tetesan bunyi air (detik)")]
    [SerializeField] private float minIntervalSeconds = 3.0f;
    [Tooltip("Jeda waktu maksimal antara tetesan bunyi air (detik)")]
    [SerializeField] private float maxIntervalSeconds = 7.5f;

    private ParticleSystem dripPS;
    private ParticleSystemRenderer dripRenderer;
    private ParticleSystem splashPS;
    private ParticleSystemRenderer splashRenderer;
    private AudioSource audioSource;
    private Coroutine audioRoutine;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        if (Application.isPlaying && enableDripAudio)
        {
            if (audioRoutine != null) StopCoroutine(audioRoutine);
            audioRoutine = StartCoroutine(DripAudioRoutine());
        }
    }

    private void OnDisable()
    {
        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
            audioRoutine = null;
        }
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
        if (dripPS == null) dripPS = GetComponent<ParticleSystem>();
        if (dripRenderer == null) dripRenderer = GetComponent<ParticleSystemRenderer>();

        SetupDropletParticleSystem();
        SetupSplashSubEmitter();

        if (Application.isPlaying && enableDripAudio)
        {
            SetupAudioSource();
        }
    }

    private void SetupDropletParticleSystem()
    {
        if (dripPS == null || dripRenderer == null) return;

        // Material Tetesan Air
        Material matToUse = customDropletMaterial;
        if (matToUse == null)
        {
#if UNITY_EDITOR
            matToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_WaterDroplet.mat");
#endif
            if (matToUse == null)
            {
                Shader shader = Shader.Find("PRANAN/SoftGlowParticle");
                if (shader != null)
                {
                    matToUse = new Material(shader);
                    matToUse.name = "M_WaterDroplet";
                }
            }
        }

        if (matToUse != null)
        {
            matToUse.SetColor("_Color", dropletColor);
            dripRenderer.sharedMaterial = matToUse;
        }

        var main = dripPS.main;
        float safeFall = Mathf.Max(0.5f, fallDistance);
        main.startLifetime = Mathf.Sqrt(2f * safeFall / 9.81f); // t = sqrt(2h/g)
        main.startSpeed = 0.1f;
        main.startSize = dropletSize;
        main.startColor = Color.white;
        main.gravityModifier = 1.0f; // Jatuh dengan gravitasi bumi
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;

        var emission = dripPS.emission;
        emission.rateOverTime = dripsPerSecond;

        // Distribusi tetesan sepanjang garis tepi atap seng (SingleSidedEdge)
        var shape = dripPS.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = eaveLength * 0.5f;
        shape.rotation = new Vector3(0f, 0f, 0f);

        // Stretched billboard vertikal agar air tampak menetes memanjang
        dripRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        dripRenderer.cameraVelocityScale = 0f;
        dripRenderer.velocityScale = 0.05f;
        dripRenderer.lengthScale = 1.6f;
    }

    private void SetupSplashSubEmitter()
    {
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
            splashObj.localPosition = new Vector3(0f, -fallDistance, 0f);
        }

        splashRenderer = splashPS.GetComponent<ParticleSystemRenderer>();

        Material splashMat = customSplashMaterial;
        if (splashMat == null)
        {
#if UNITY_EDITOR
            splashMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_WaterSplash.mat");
#endif
            if (splashMat == null)
            {
                Shader shader = Shader.Find("PRANAN/SoftGlowParticle");
                if (shader != null)
                {
                    splashMat = new Material(shader);
                    splashMat.name = "M_WaterSplash";
                }
            }
        }

        if (splashMat != null && splashRenderer != null)
        {
            splashMat.SetColor("_Color", new Color(dropletColor.r, dropletColor.g, dropletColor.b, 0.45f));
            splashRenderer.sharedMaterial = splashMat;
        }

        var main = splashPS.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 0.2f;
        main.startSize = 0.05f;
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = splashPS.emission;
        emission.rateOverTime = 0; // Triggered via script/splash

        var shape = splashPS.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = eaveLength * 0.5f;

        if (splashRenderer != null)
        {
            splashRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1.0f; // 3D spatial audio
        audioSource.minDistance = 1.5f;
        audioSource.maxDistance = 12.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.playOnAwake = false;

        if (customDripClip == null)
        {
            customDripClip = GenerateProceduralWaterDripClip();
        }
    }

    private AudioClip GenerateProceduralWaterDripClip()
    {
        int sampleRate = 44100;
        float duration = 0.12f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float baseFreq = 950f;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq = baseFreq + (t * 800f);
            float envelope = Mathf.Exp(-t * 45f);
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
                audioSource.pitch = Random.Range(0.85f, 1.2f);
                audioSource.PlayOneShot(customDripClip, audioVolume * Random.Range(0.8f, 1.0f));

                if (splashPS != null)
                {
                    splashPS.Emit(2);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.75f);

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
