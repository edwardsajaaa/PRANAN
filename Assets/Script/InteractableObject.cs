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

    [Header("Monologue / Thought (Pamali / Firewatch)")]
    [Tooltip("Jika diisi, memicu monolog batin Rangga di bawah layar")]
    [TextArea(3, 6)]
    public string examinationMonologue;
    public bool playMonologueOnInteract = true;
    public bool trigger3DInspection = true;

    [Header("Proximity Monologue (Memicu Saat Mendekat)")]
    [Tooltip("Jika dicentang, monolog batin otomatis muncul satu kali saat player berjalan mendekati objek ini")]
    public bool triggerOnApproach = true;
    [Tooltip("Jarak deteksi pemain mendekati objek (dalam meter)")]
    public float approachDistance = 4.0f;
    [Tooltip("Durasi teks monolog tampil di layar (detik)")]
    public float monologueDuration = 7.5f;

    [Header("Orbit Requirement (Harus Mengitari Objek Terlebih Dahulu)")]
    [Tooltip("Jika dicentang, player harus berjalan mengelilingi objek ini terlebih dahulu sebelum bisa berinteraksi [E]")]
    public bool requireOrbitBeforeInteract = false;
    [Tooltip("Jarak maksimum dari pusat objek agar langkah player terhitung sedang mengitari")]
    public float orbitMaxDistance = 4.5f;
    [Tooltip("Persentase keliling lingkaran yang harus dilewati (0.5 = 180°, 0.75 = 270°, 1.0 = 360° penuh)")]
    [Range(0.25f, 1.0f)]
    public float requiredOrbitCoverage = 0.75f;

    private const int OrbitSectorCount = 12;
    private bool[] visitedOrbitSectors;
    private int visitedOrbitSectorCount = 0;
    private bool isOrbitUnlocked = false;
    private bool hasTriggeredApproach = false;

    public bool CanInteract => !requireOrbitBeforeInteract || isOrbitUnlocked;

    public float OrbitProgressNormalized
    {
        get
        {
            if (!requireOrbitBeforeInteract || isOrbitUnlocked) return 1f;
            int requiredSectors = Mathf.Max(1, Mathf.CeilToInt(OrbitSectorCount * requiredOrbitCoverage));
            return Mathf.Clamp01((float)visitedOrbitSectorCount / requiredSectors);
        }
    }

    private void Update()
    {
        if (requireOrbitBeforeInteract && !isOrbitUnlocked)
        {
            TrackPlayerOrbit();
        }

        if (triggerOnApproach && !hasTriggeredApproach && !string.IsNullOrEmpty(examinationMonologue))
        {
            CheckApproachTrigger();
        }
    }

    private void TrackPlayerOrbit()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Exploration)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 center = GetCenterPosition();
        Vector3 playerPos = cam.transform.position;
        Vector2 deltaXZ = new Vector2(playerPos.x - center.x, playerPos.z - center.z);

        if (deltaXZ.sqrMagnitude > orbitMaxDistance * orbitMaxDistance || deltaXZ.sqrMagnitude < 0.04f)
        {
            return;
        }

        if (visitedOrbitSectors == null || visitedOrbitSectors.Length != OrbitSectorCount)
        {
            visitedOrbitSectors = new bool[OrbitSectorCount];
        }

        float angleDeg = Mathf.Atan2(deltaXZ.y, deltaXZ.x) * Mathf.Rad2Deg;
        if (angleDeg < 0f) angleDeg += 360f;

        int sectorIndex = Mathf.Clamp(Mathf.FloorToInt(angleDeg / (360f / OrbitSectorCount)), 0, OrbitSectorCount - 1);
        if (!visitedOrbitSectors[sectorIndex])
        {
            visitedOrbitSectors[sectorIndex] = true;
            visitedOrbitSectorCount++;

            int requiredSectors = Mathf.Max(1, Mathf.CeilToInt(OrbitSectorCount * requiredOrbitCoverage));
            if (visitedOrbitSectorCount >= requiredSectors)
            {
                isOrbitUnlocked = true;
            }
        }
    }

    private void CheckApproachTrigger()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Exploration)
        {
            return;
        }

        Transform playerTransform = null;
        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
        }

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(GetCenterPosition(), playerTransform.position);
            if (dist <= approachDistance)
            {
                hasTriggeredApproach = true;
                if (RadioDialogueManager.Instance != null)
                {
                    RadioDialogueManager.Instance.ShowMonologue(examinationMonologue, monologueDuration);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = GetCenterPosition();
        if (triggerOnApproach)
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(center, approachDistance);
        }

        if (requireOrbitBeforeInteract)
        {
            Gizmos.color = new Color(1f, 0.78f, 0.2f, 0.55f);
            Gizmos.DrawWireSphere(center, orbitMaxDistance);
        }
    }

    private Vector3 GetCenterPosition()
    {
        if (targetPivot != null) return targetPivot.position;
        Collider col = GetComponent<Collider>();
        if (col != null) return col.bounds.center;
        return transform.position;
    }

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

        Vector3 closestToEye;
        Vector3 closestToBody;

        if (col is MeshCollider mc && !mc.convex)
        {
            closestToEye = col.bounds.ClosestPoint(playerEyePos);
            closestToBody = col.bounds.ClosestPoint(playerBodyPos);
        }
        else
        {
            closestToEye = col.ClosestPoint(playerEyePos);
            closestToBody = col.ClosestPoint(playerBodyPos);
        }

        float distEye = Vector3.Distance(playerEyePos, closestToEye);
        float distBody = Vector3.Distance(playerBodyPos, closestToBody);
        float distanceToSurface = Mathf.Min(distEye, distBody);

        float allowedRange = GetDynamicInteractionReach(col);
        return distanceToSurface <= allowedRange;
    }

    public virtual void Interact(Transform playerTransform)
    {
        if (!CanInteract) return;

        if (!string.IsNullOrEmpty(examinationMonologue) && playMonologueOnInteract)
        {
            if (RadioDialogueManager.Instance != null)
            {
                RadioDialogueManager.Instance.ShowMonologue(examinationMonologue, monologueDuration);
            }
        }

        if (trigger3DInspection && ClueInspector.Instance != null)
        {
            ClueInspector.Instance.StartInspection(this);
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
