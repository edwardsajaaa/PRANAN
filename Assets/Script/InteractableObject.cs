using UnityEngine;

public enum ClueCategory
{
    Natural,   // Contoh: Pohon tumbang lapuk
    Artificial // Contoh: Tunggul bekas gergaji mesin
}

public class InteractableObject : MonoBehaviour
{
    [Header("Clue Information")]
    public string clueName = "Objek Bukti";
    [TextArea(2, 4)]
    public string clueDescription = "Bukti fisik di lokasi investigasi.";
    public ClueCategory category = ClueCategory.Natural;

    [Header("Inspection Model Setup")]
    [Tooltip("Prefab atau GameObject 3D yang akan diisolasi di studio inspection. Jika kosong, objek ini/mesh-nya yang digunakan.")]
    public GameObject inspectionModelPrefab;

    [Header("Dynamic Interaction Range")]
    [Tooltip("Toleransi jarak berdiri pemain dari permukaan fisik collider objek (default: 2.2 meter).")]
    public float surfaceProximityTolerance = 2.2f;

    [Tooltip("Jika diisi > 0, akan mengabaikan hitungan otomatis collider dan memakai angka ini.")]
    public float customInteractionRange = 0f;

    [Header("Interaction Settings")]
    [Tooltip("Titik fokus targeting (jika null, menggunakan posisi transform)")]
    public Transform targetPivot;

    public Vector3 TargetPosition => targetPivot != null ? targetPivot.position : transform.position;

    /// <summary>
    /// Menghitung jarak jangkauan interaksi secara dinamis dan otomatis berdasarkan ukuran fisik Collider objek ini.
    /// Objek kecil (alat, buku) otomatis membutuhkan jarak dekat,
    /// sedangkan objek sedang (kubus 1m) dan besar (pohon tumbang) jangkauannya otomatis meluas secara proporsional.
    /// </summary>
    public float GetDynamicInteractionReach(Collider col)
    {
        if (customInteractionRange > 0.05f) return customInteractionRange;
        if (col == null) return 2.5f;

        // Ambil dimensi terpanjang dari bounds collider objek ini
        Vector3 size = col.bounds.size;
        float maxDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        // Formula proporsional adaptif:
        // Ukuran 0.3m (benda kecil)  -> reach ~1.8m
        // Ukuran 1.0m (kubus sedang) -> reach ~2.5m
        // Ukuran 5.0m (benda besar)  -> reach ~5.0m
        // Ukuran 10m+ (pohon/mesin)  -> reach ~8.0m (maksimal clamped 12m)
        float dynamicReach = Mathf.Clamp(1.6f + (maxDimension * 0.9f), 1.8f, 12f);
        return dynamicReach;
    }

    /// <summary>
    /// Mengecek apakah pemain berada dalam jangkauan fisik dari permukaan Collider objek ini.
    /// Memperhitungkan posisi mata dan kaki pemain serta ukuran dinamis collider.
    /// </summary>
    public bool IsInInteractionRange(Collider col, Vector3 playerEyePos, Vector3 playerBodyPos)
    {
        if (col == null) return false;

        // Cari titik terdekat pada permukaan collider terhadap mata dan tubuh pemain
        Vector3 closestToEye = col.ClosestPoint(playerEyePos);
        Vector3 closestToBody = col.ClosestPoint(playerBodyPos);

        float distEye = Vector3.Distance(playerEyePos, closestToEye);
        float distBody = Vector3.Distance(playerBodyPos, closestToBody);
        float distanceToSurface = Mathf.Min(distEye, distBody);

        float allowedRange = GetDynamicInteractionReach(col);
        return distanceToSurface <= allowedRange;
    }

    public virtual void Interact(Transform playerTransform)
    {
        // Membuka Sub-Viewport Clue Isolator (W1-08)
        if (ClueInspector.Instance == null)
        {
            var inspector = Object.FindObjectOfType<ClueInspector>();
            if (inspector == null)
            {
                GameObject studioObj = new GameObject("InspectionStudio");
                inspector = studioObj.AddComponent<ClueInspector>();
            }
        }

        if (ClueInspector.Instance != null)
        {
            ClueInspector.Instance.StartInspection(this);
        }
        else
        {
            Debug.Log($"[InteractableObject] Berinteraksi dengan {clueName}, studio inspeksi sedang disiapkan.");
        }
    }
}
