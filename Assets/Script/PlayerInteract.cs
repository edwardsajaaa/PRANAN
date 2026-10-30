using UnityEngine;
using System.Collections.Generic;

public class PlayerInteract : MonoBehaviour
{
    [Header("Scanner Cone Settings")]
    [Tooltip("Sudut total kerucut pandang deteksi")]
    public float coneAngle = 30f;
    [Tooltip("Jarak deteksi maksimum")]
    public float maxDistance = 3.5f;

    [Header("Line-of-Sight Occlusion Layer")]
    [Tooltip("Layer halangan (dinding, tebing, pohon) yang memblokir pandangan")]
    public LayerMask occlusionMask = ~0;

    [Header("Target Weighting")]
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
        InteractableObject bestCandidate = null;
        Ray centerRay = new Ray(camPos, camForward);
        RaycastHit[] centerHits = Physics.RaycastAll(centerRay, maxVisionDist, occlusionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(centerHits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in centerHits)
        {
            if (hit.collider.transform.root == transform.root) continue;

            InteractableObject directInteractable = hit.collider.GetComponentInParent<InteractableObject>();
            if (directInteractable != null)
            {
                if (directInteractable.IsInInteractionRange(hit.collider, camPos, transform.position))
                {
                    bestCandidate = directInteractable;
                }
            }
            break;
        }

        if (bestCandidate == null)
        {
            RaycastHit[] sphereHits = Physics.SphereCastAll(centerRay, 0.05f, maxVisionDist, occlusionMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in sphereHits)
            {
                if (hit.collider.transform.root == transform.root) continue;

                InteractableObject directInteractable = hit.collider.GetComponentInParent<InteractableObject>();
                if (directInteractable != null)
                {
                    if (directInteractable.IsInInteractionRange(hit.collider, camPos, transform.position))
                    {
                        bestCandidate = directInteractable;
                    }
                }
                break;
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
