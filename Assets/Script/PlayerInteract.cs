using UnityEngine;
using System.Collections.Generic;

public class PlayerInteract : MonoBehaviour
{
    [Header("Scanner Cone Settings (W1-05)")]
    [Tooltip("Sudut total kerucut pandang (30 derajat sesuai W1-05)")]
    public float coneAngle = 30f;
    [Tooltip("Jarak deteksi maksimum")]
    public float maxDistance = 3.5f;

    [Header("Line-of-Sight (LOS) Occlusion Layer")]
    [Tooltip("Layer halangan (dinding, tebing, pohon) yang memblokir pandangan")]
    public LayerMask occlusionMask = ~0;

    [Header("Target Weighting Weights (W1-06)")]
    [Range(0f, 1f)] public float angleWeight = 0.6f;     // 60% kelurusan pandang
    [Range(0f, 1f)] public float distanceWeight = 0.4f;  // 40% kedekatan jarak

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    private Camera mainCamera;
    private InteractableObject currentBestTarget;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Hanya aktif di state Exploration
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Exploration)
        {
            if (currentBestTarget != null)
            {
                currentBestTarget = null;
                if (ReticleUI.Instance != null) ReticleUI.Instance.ClearPrompt();
            }
            return;
        }

        ScanForTargets();
        HandleInteractionInput();
    }

    /// <summary>
    /// Cone-Cast spasial 30 derajat dan radius 2.5m dengan verifikasi Line-of-Sight (W1-05)
    /// serta kalkulasi skor pembobotan target (W1-06).
    /// </summary>
    private void ScanForTargets()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 camPos = mainCamera.transform.position;
        Vector3 camForward = mainCamera.transform.forward;
        float maxVisionDist = 20f; // Jarak pandang penglihatan kursor
        float effectiveConeAngle = Mathf.Max(coneAngle, 60f);
        float halfAngle = effectiveConeAngle * 0.5f;

        InteractableObject bestCandidate = null;

        // 1. Prioritas Utama: Kursor langsung menunjuk ke arah collider objek
        // Menggunakan SphereCast tebal 0.2m dari mata pemain
        Ray centerRay = new Ray(camPos, camForward);
        RaycastHit[] centerHits = Physics.SphereCastAll(centerRay, 0.2f, maxVisionDist, occlusionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(centerHits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in centerHits)
        {
            // Abaikan tubuh player sendiri
            if (hit.collider.transform.root == transform.root) continue;

            InteractableObject directInteractable = hit.collider.GetComponentInParent<InteractableObject>();
            if (directInteractable != null)
            {
                // Cek apakah posisi pemain berada dalam jangkauan fisik Collider objek tersebut
                if (directInteractable.IsInInteractionRange(hit.collider, camPos, transform.position))
                {
                    // Cek Line-of-Sight ke titik hit agar tidak tembus dinding tebal
                    Vector3 toHit = hit.point - camPos;
                    if (toHit.sqrMagnitude > 0.01f && Physics.Raycast(camPos, toHit.normalized, out RaycastHit losHit, toHit.magnitude * 0.95f, occlusionMask, QueryTriggerInteraction.Ignore))
                    {
                        if (losHit.collider.transform.root != transform.root && !losHit.collider.transform.IsChildOf(directInteractable.transform))
                        {
                            continue; // Terhalang rintangan lain
                        }
                    }

                    bestCandidate = directInteractable;
                    break;
                }
                else
                {
                    // Pemain melihat objek ini tetapi masih terlalu jauh dari fisik collider-nya
                    break;
                }
            }
            else
            {
                // Terhalang obstacle solid non-interactable (tanah / tebing / dinding)
                break;
            }
        }

        // 2. Jika kursor tidak mengarah persis, cari objek di sekitar pandangan pemain
        if (bestCandidate == null)
        {
            Collider[] hits = Physics.OverlapSphere(camPos, 8f, occlusionMask, QueryTriggerInteraction.Ignore);
            float highestScore = -1f;
            HashSet<InteractableObject> evaluated = new HashSet<InteractableObject>();

            foreach (var col in hits)
            {
                if (col.transform.root == transform.root) continue; // Abaikan player
                InteractableObject interactable = col.GetComponentInParent<InteractableObject>();
                if (interactable == null || evaluated.Contains(interactable)) continue;
                evaluated.Add(interactable);

                // Cek apakah pemain berada dalam jangkauan fisik collider objek ini
                if (!interactable.IsInInteractionRange(col, camPos, transform.position)) continue;

                Vector3 closestPoint = col.ClosestPoint(camPos);
                Vector3 toTarget = closestPoint - camPos;
                float distance = toTarget.magnitude;

                if (distance < 0.05f) continue;

                // Cek batas sudut kerucut pandang (Cone check)
                float angle = Vector3.Angle(camForward, toTarget);
                if (angle > halfAngle) continue;

                // Verifikasi Line-of-Sight (LOS) ke permukaan terdekat
                if (Physics.Raycast(camPos, toTarget.normalized, out RaycastHit losHit, distance * 0.95f, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    if (losHit.collider.transform.root != transform.root && !losHit.collider.transform.IsChildOf(interactable.transform))
                    {
                        continue; // Terhalang rintangan
                    }
                }

                // Algoritma Pembobotan Target (W1-06)
                float normalizedAngleScore = Mathf.Clamp01(1f - (angle / halfAngle));
                float maxDist = (interactable.customInteractionRange > 0.05f) ? interactable.customInteractionRange : interactable.surfaceProximityTolerance;
                float normalizedDistanceScore = Mathf.Clamp01(1f - (distance / Mathf.Max(maxDist, 1f)));
                float totalScore = (angleWeight * normalizedAngleScore) + (distanceWeight * normalizedDistanceScore);

                if (totalScore > highestScore)
                {
                    highestScore = totalScore;
                    bestCandidate = interactable;
                }
            }
        }

        // Perbarui target aktif dan Reticle UI (W1-07)
        if (bestCandidate != currentBestTarget)
        {
            currentBestTarget = bestCandidate;

            if (currentBestTarget != null)
            {
                if (ReticleUI.Instance != null)
                {
                    ReticleUI.Instance.ShowPrompt(currentBestTarget.clueName);
                }
            }
            else
            {
                if (ReticleUI.Instance != null)
                {
                    ReticleUI.Instance.ClearPrompt();
                }
            }
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentBestTarget != null)
        {
            currentBestTarget.Interact(transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(mainCamera.transform.position, maxDistance);
    }
}
