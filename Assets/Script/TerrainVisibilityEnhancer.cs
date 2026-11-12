using UnityEngine;

/// <summary>
/// Mengoptimalkan jarak render pohon (Tree Distance & Billboard Distance) pada seluruh Terrain hutan.
/// Mencegah hutan terlihat botak/kosong saat dilihat dari kejauhan.
/// </summary>
[ExecuteAlways]
public class TerrainVisibilityEnhancer : MonoBehaviour
{
    [Header("Jarak Render Pohon (Tree Draw Distance)")]
    [Tooltip("Jarak maksimal pohon 3D tetap terlihat utuh sebelum beralih ke billboard (meter)")]
    [SerializeField] private float treeBillboardDistance = 500f;

    [Tooltip("Jumlah maksimum pohon 3D mesh penuh yang dirender sekaligus di layar")]
    [SerializeField] private int maxMeshTrees = 2500;

    [Tooltip("Jarak render total pohon secara keseluruhan")]
    [SerializeField] private float totalTreeDistance = 2000f;

    [Header("Jarak Render Rumput & Semak (Detail Distance)")]
    [SerializeField] private float detailDistance = 140f;

    private void Awake()
    {
        ApplyTerrainSettings();
    }

    private void OnEnable()
    {
        ApplyTerrainSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyTerrainSettings();
    }
#endif

    [ContextMenu("Terapkan ke Seluruh Terrain")]
    public void ApplyTerrainSettings()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
        {
            terrains = FindObjectsOfType<Terrain>();
        }

        foreach (var t in terrains)
        {
            if (t == null) continue;
            t.treeBillboardDistance = treeBillboardDistance;
            t.treeMaximumFullLODCount = maxMeshTrees;
            t.treeDistance = totalTreeDistance;
            t.detailObjectDistance = detailDistance;
        }
    }
}

