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

    [Header("Interaction Settings")]
    [Tooltip("Titik fokus targeting (jika null, menggunakan posisi transform)")]
    public Transform targetPivot;

    public Vector3 TargetPosition => targetPivot != null ? targetPivot.position : transform.position;

    public virtual void Interact(Transform playerTransform)
    {
        // Membuka Sub-Viewport Clue Isolator (W1-08)
        if (ClueInspector.Instance != null)
        {
            ClueInspector.Instance.StartInspection(this);
        }
        else
        {
            Debug.Log($"[InteractableObject] Berinteraksi dengan {clueName}, namun ClueInspector belum terpasang.");
        }
    }
}
