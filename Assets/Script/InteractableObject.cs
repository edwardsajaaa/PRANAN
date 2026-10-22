using UnityEngine;

public enum ClueCategory
{
    Natural,
    Artificial
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

    [Header("Collectable Settings")]
    [Tooltip("Apakah objek ini bisa diambil dan diamankan ke inventori bukti saat diinspeksi")]
    public bool isCollectable = false;
    public string collectedMessage = "Bukti berhasil diamankan.";
    public System.Action<InteractableObject> OnCollected;

    [Header("STADI 2025 Evidence Classification")]
    public ClueEvidenceType evidenceType = ClueEvidenceType.General;

    [Header("Interaction Settings")]
    [Tooltip("Titik fokus targeting (jika null, menggunakan posisi transform)")]
    public Transform targetPivot;

    public Vector3 TargetPosition => targetPivot != null ? targetPivot.position : transform.position;

    public float GetDynamicInteractionReach(Collider col)
    {
        if (customInteractionRange > 0.05f) return customInteractionRange;
        if (col == null) return 2.5f;

        Vector3 size = col.bounds.size;
        float maxDimension = Mathf.Max(size.x, Mathf.Max(size.y, size.z));

        float dynamicReach = Mathf.Clamp(1.6f + (maxDimension * 0.9f), 1.8f, 12f);
        return dynamicReach;
    }

    public bool IsInInteractionRange(Collider col, Vector3 playerEyePos, Vector3 playerBodyPos)
    {
        if (col == null) return false;

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

    public ClueEvidenceType GetEffectiveEvidenceType()
    {
        if (evidenceType != ClueEvidenceType.General) return evidenceType;
        string lower = (clueName + " " + clueDescription).ToLower();
        if (lower.Contains("dokumen") || lower.Contains("izin") || lower.Contains("surat") || lower.Contains("legal"))
            return ClueEvidenceType.PermitDocument;
        if (lower.Contains("tunggul") || lower.Contains("alat") || lower.Contains("mesin") || lower.Contains("kotak") || lower.Contains("gergaji"))
            return ClueEvidenceType.ManMadeEquipment;
        if (lower.Contains("pohon") || lower.Contains("gambut") || lower.Contains("alami") || lower.Contains("lapuk"))
            return ClueEvidenceType.NaturalEvidence;
        return ClueEvidenceType.General;
    }

    public virtual void Collect()
    {
        OnCollected?.Invoke(this);
        if (JournalManager.Instance != null)
        {
            JournalManager.Instance.RecordEvidence(clueName, category.ToString(), collectedMessage, GetEffectiveEvidenceType(), true);
        }
        Debug.Log($"[InteractableObject] {clueName} berhasil diambil: {collectedMessage}");
        gameObject.SetActive(false);
    }
}
