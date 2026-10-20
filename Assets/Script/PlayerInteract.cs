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
    [Range(0f, 1f)] public float angleWeight = 0.6f;
    [Range(0f, 1f)] public float distanceWeight = 0.4f;

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

    private void ScanForTargets()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 camPos = mainCamera.transform.position;
        Vector3 camForward = mainCamera.transform.forward;
        float maxVisionDist = 20f;
        float effectiveConeAngle = Mathf.Max(coneAngle, 60f);
        float halfAngle = effectiveConeAngle * 0.5f;

        InteractableObject bestCandidate = null;

        Ray centerRay = new Ray(camPos, camForward);
        RaycastHit[] centerHits = Physics.SphereCastAll(centerRay, 0.2f, maxVisionDist, occlusionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(centerHits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in centerHits)
        {

            if (hit.collider.transform.root == transform.root) continue;

            InteractableObject directInteractable = hit.collider.GetComponentInParent<InteractableObject>();
            if (directInteractable != null)
            {

                if (directInteractable.IsInInteractionRange(hit.collider, camPos, transform.position))
                {

                    Vector3 toHit = hit.point - camPos;
                    if (toHit.sqrMagnitude > 0.01f && Physics.Raycast(camPos, toHit.normalized, out RaycastHit losHit, toHit.magnitude * 0.95f, occlusionMask, QueryTriggerInteraction.Ignore))
                    {
                        if (losHit.collider.transform.root != transform.root && !losHit.collider.transform.IsChildOf(directInteractable.transform))
                        {
                            continue;
                        }
                    }

                    bestCandidate = directInteractable;
                    break;
                }
                else
                {

                    break;
                }
            }
            else
            {

                break;
            }
        }

        if (bestCandidate == null)
        {
            Collider[] hits = Physics.OverlapSphere(camPos, 8f, occlusionMask, QueryTriggerInteraction.Ignore);
            float highestScore = -1f;
            HashSet<InteractableObject> evaluated = new HashSet<InteractableObject>();

            foreach (var col in hits)
            {
                if (col.transform.root == transform.root) continue;
                InteractableObject interactable = col.GetComponentInParent<InteractableObject>();
                if (interactable == null || evaluated.Contains(interactable)) continue;
                evaluated.Add(interactable);

                if (!interactable.IsInInteractionRange(col, camPos, transform.position)) continue;

                Vector3 closestPoint = col.ClosestPoint(camPos);
                Vector3 toTarget = closestPoint - camPos;
                float distance = toTarget.magnitude;

                if (distance < 0.05f) continue;

                float angle = Vector3.Angle(camForward, toTarget);
                if (angle > halfAngle) continue;

                if (Physics.Raycast(camPos, toTarget.normalized, out RaycastHit losHit, distance * 0.95f, occlusionMask, QueryTriggerInteraction.Ignore))
                {
                    if (losHit.collider.transform.root != transform.root && !losHit.collider.transform.IsChildOf(interactable.transform))
                    {
                        continue;
                    }
                }

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
