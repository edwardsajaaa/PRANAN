using UnityEngine;
using System.Collections.Generic;

public class PlayerInteract : MonoBehaviour
{
    [Header("Scanner Cone Settings (W1-05)")]
    [Tooltip("Sudut total kerucut pandang (30 derajat sesuai W1-05)")]
    public float coneAngle = 30f;
    [Tooltip("Jarak deteksi maksimum (2.5 meter sesuai W1-05)")]
    public float maxDistance = 2.5f;

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
        float halfAngle = coneAngle * 0.5f;

        // Ambil semua collider dalam bola radius maxDistance
        Collider[] hits = Physics.OverlapSphere(camPos, maxDistance);
        InteractableObject bestCandidate = null;
        float highestScore = -1f;

        // Menggunakan set untuk menghindari duplikasi jika satu objek memiliki multiple collider
        HashSet<InteractableObject> evaluated = new HashSet<InteractableObject>();

        foreach (var col in hits)
        {
            InteractableObject interactable = col.GetComponentInParent<InteractableObject>();
            if (interactable == null || evaluated.Contains(interactable)) continue;
            evaluated.Add(interactable);

            Vector3 targetPos = interactable.TargetPosition;
            Vector3 toTarget = targetPos - camPos;
            float distance = toTarget.magnitude;

            if (distance > maxDistance || distance < 0.05f) continue;

            // 1. Cek batas sudut kerucut pandang (Cone check)
            float angle = Vector3.Angle(camForward, toTarget);
            if (angle > halfAngle) continue;

            // 2. Verifikasi Line-of-Sight (LOS) - Tidak boleh terhalang tebing/pohon/dinding
            Ray losRay = new Ray(camPos, toTarget.normalized);
            if (Physics.Raycast(losRay, out RaycastHit losHit, distance, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                // Jika membentur sesuatu yang bukan bagian dari interactable tersebut, maka terhalang
                if (!losHit.collider.transform.IsChildOf(interactable.transform))
                {
                    continue; // Terhalang rintangan
                }
            }

            // 3. Algoritma Pembobotan Target (W1-06): 60% Sudut Kelurusan + 40% Jarak
            float normalizedAngleScore = Mathf.Clamp01(1f - (angle / halfAngle));
            float normalizedDistanceScore = Mathf.Clamp01(1f - (distance / maxDistance));
            float totalScore = (angleWeight * normalizedAngleScore) + (distanceWeight * normalizedDistanceScore);

            if (totalScore > highestScore)
            {
                highestScore = totalScore;
                bestCandidate = interactable;
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
