using UnityEngine;

/// <summary>
/// Komponen Volumetric Light Shaft (God Rays) menembus sela kanopi pohon.
/// Menghasilkan berkas cahaya volumetrik yang bereaksi secara dinamis dengan FogZone.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VolumetricLightShaft : MonoBehaviour
{
    [Header("Directional Sun Alignment")]
    [Tooltip("Otomatis sejajarkan arah berkas sinar dengan Directional Light (Matahari) di scene")]
    [SerializeField] private bool alignWithDirectionalLight = true;
    [SerializeField] private Light targetSunLight;

    [Header("Shaft Geometry")]
    [Tooltip("Panjang berkas cahaya menembus kanopi hingga menyentuh tanah/lantai hutan")]
    [SerializeField] private float length = 26f;
    [Tooltip("Radius pangkal berkas di sela dedaunan/kanopi")]
    [SerializeField] private float topRadius = 0.6f;
    [Tooltip("Radius sebaran cahaya di dasar tanah")]
    [SerializeField] private float bottomRadius = 4.8f;
    [Range(8, 32)]
    [SerializeField] private int radialSegments = 16;

    [Header("Visual & Color (Golden Dusk / Senja Hutan)")]
    [SerializeField] private Color shaftColor = new Color(1.0f, 0.88f, 0.65f, 0.45f);
    [Range(0f, 5f)]
    [SerializeField] private float baseIntensity = 1.2f;

    [Header("Noise & Micro Particles")]
    [SerializeField] private Vector2 dustSpeed = new Vector2(0.04f, 0.08f);
    [SerializeField] private float dustScale = 3.5f;
    [SerializeField] private float depthFadeDistance = 1.5f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material shaftMaterial;
    private MaterialPropertyBlock propertyBlock;

    private static readonly int ColorProp = Shader.PropertyToID("_Color");
    private static readonly int IntensityProp = Shader.PropertyToID("_Intensity");
    private static readonly int FogMultiplierProp = Shader.PropertyToID("_FogMultiplier");
    private static readonly int NoiseSpeedProp = Shader.PropertyToID("_NoiseSpeed");
    private static readonly int NoiseScaleProp = Shader.PropertyToID("_NoiseScale");
    private static readonly int DepthFadeProp = Shader.PropertyToID("_DepthFadeDistance");

    private void Awake()
    {
        InitializeComponents();
        EnsureDepthTextureMode();
    }

    private void OnEnable()
    {
        InitializeComponents();
        FogZone.OnVolumetricMultiplierChanged += HandleFogMultiplierChanged;
        UpdateMaterialProperties(FogZone.CurrentVolumetricMultiplier);
    }

    private void OnDisable()
    {
        FogZone.OnVolumetricMultiplierChanged -= HandleFogMultiplierChanged;
    }

    private void Start()
    {
        RebuildMesh();
        UpdateAlignment();
        UpdateMaterialProperties(FogZone.CurrentVolumetricMultiplier);
    }

    private void Update()
    {
        if (alignWithDirectionalLight)
        {
            UpdateAlignment();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateMaterialProperties(1.0f);
        }
#endif
    }

    private void InitializeComponents()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (shaftMaterial == null)
        {
            Shader beamShader = Shader.Find("PRANAN/VolumetricLightBeam");
            if (beamShader != null)
            {
                shaftMaterial = new Material(beamShader);
                shaftMaterial.name = "M_VolumetricSunShaft";
                meshRenderer.sharedMaterial = shaftMaterial;
            }
        }

        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
    }

    private void EnsureDepthTextureMode()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.depthTextureMode |= DepthTextureMode.Depth;
        }
    }

    private void UpdateAlignment()
    {
        if (targetSunLight == null)
        {
            if (RenderSettings.sun != null)
            {
                targetSunLight = RenderSettings.sun;
            }
            else
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (var l in lights)
                {
                    if (l.type == LightType.Directional)
                    {
                        targetSunLight = l;
                        break;
                    }
                }
            }
        }

        if (targetSunLight != null)
        {
            transform.rotation = targetSunLight.transform.rotation;
        }
    }

    [ContextMenu("Rebuild Shaft Mesh")]
    public void RebuildMesh()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.name = "VolumetricShaftCone";

        int vertCount = (radialSegments + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];

        float angleStep = (2f * Mathf.PI) / radialSegments;

        for (int i = 0; i <= radialSegments; i++)
        {
            float angle = i * angleStep;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Top ring (pangkal di kanopi, UV y = 0)
            int topIdx = i;
            vertices[topIdx] = new Vector3(cos * topRadius, sin * topRadius, 0f);
            normals[topIdx] = new Vector3(cos, sin, 0f).normalized;
            uvs[topIdx] = new Vector2(0f, 0f); // Core / pangkal

            // Bottom ring (dasar tanah, UV y = 1)
            int botIdx = (radialSegments + 1) + i;
            vertices[botIdx] = new Vector3(cos * bottomRadius, sin * bottomRadius, length);
            normals[botIdx] = new Vector3(cos, sin, 0f).normalized;
            uvs[botIdx] = new Vector2(1f, 1f); // Rim / dasar
        }

        int[] triangles = new int[radialSegments * 6];
        int triIndex = 0;

        for (int i = 0; i < radialSegments; i++)
        {
            int topA = i;
            int topB = i + 1;
            int botA = (radialSegments + 1) + i;
            int botB = (radialSegments + 1) + i + 1;

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

        meshFilter.sharedMesh = mesh;
    }

    private void HandleFogMultiplierChanged(float multiplier)
    {
        UpdateMaterialProperties(multiplier);
    }

    private void UpdateMaterialProperties(float fogMultiplier)
    {
        if (meshRenderer == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ColorProp, shaftColor);
        propertyBlock.SetFloat(IntensityProp, baseIntensity);
        propertyBlock.SetFloat(FogMultiplierProp, fogMultiplier);
        propertyBlock.SetVector(NoiseSpeedProp, new Vector4(dustSpeed.x, dustSpeed.y, 0f, 0f));
        propertyBlock.SetFloat(NoiseScaleProp, dustScale);
        propertyBlock.SetFloat(DepthFadeProp, depthFadeDistance);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(shaftColor.r, shaftColor.g, shaftColor.b, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 topCenter = Vector3.zero;
        Vector3 bottomCenter = new Vector3(0f, 0f, length);

        Gizmos.DrawWireSphere(topCenter, topRadius);
        Gizmos.DrawWireSphere(bottomCenter, bottomRadius);
        Gizmos.DrawLine(topCenter + Vector3.up * topRadius, bottomCenter + Vector3.up * bottomRadius);
        Gizmos.DrawLine(topCenter - Vector3.up * topRadius, bottomCenter - Vector3.up * bottomRadius);
        Gizmos.DrawLine(topCenter + Vector3.right * topRadius, bottomCenter + Vector3.right * bottomRadius);
        Gizmos.DrawLine(topCenter - Vector3.right * topRadius, bottomCenter - Vector3.right * bottomRadius);
    }
}
