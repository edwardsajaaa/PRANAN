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
    /// Mengecek apakah pemain berada dalam jarak jangkau fisik dari permukaan Collider objek ini.
    /// Mendukung segala jenis bentuk 3D: kotak, silinder, pohon tumbang panjang, maupun mesh kompleks.
    /// Memperhitungkan posisi mata dan kaki pemain sehingga saat berdiri di atas tanah, semua sisi objek langsung terjangkau.
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

        float tolerance = (surfaceProximityTolerance > 0.1f) ? surfaceProximityTolerance : 3.5f;
        float maxAllowed = (customInteractionRange > 0.05f) ? customInteractionRange : Mathf.Max(tolerance, 3.5f);

        return distanceToSurface <= maxAllowed;
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
