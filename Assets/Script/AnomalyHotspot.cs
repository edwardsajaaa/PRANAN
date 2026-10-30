using System;
using UnityEngine;

public class AnomalyHotspot : MonoBehaviour
{
    [Header("Hotspot Details")]
    public string anomalyTitle = "Kejanggalan Forensik";
    [TextArea(2, 4)]
    public string anomalyObservationLog = "Ditemukan jejak pemotongan mekanik yang rata.";

    [Header("Detection Parameters")]
    [Tooltip("Radius toleransi bidikan kursor (dalam pixel layar atau unit jarak)")]
    public float screenRadius = 50f;

    [Tooltip("Waktu observasi minimal yang dibutuhkan")]
    public float requiredDwellTime = 0.5f;

    public float facingAngleDotThreshold = 0.3f;

    [Header("STADI 2025 Evidence Link")]
    public ClueEvidenceType evidenceType = ClueEvidenceType.General;

    [Header("State")]
    public bool isDiscovered = false;
    private float currentDwellTime = 0f;

    public float DwellProgress => Mathf.Clamp01(currentDwellTime / requiredDwellTime);

    public event Action<AnomalyHotspot> OnDiscovered;
    public event Action<float> OnDwellProgressChanged;

    public void ResetState()
    {
        isDiscovered = false;
        currentDwellTime = 0f;
    }

    public bool EvaluateObservation(Camera inspectionCam, Vector2 screenCenter)
    {
        if (isDiscovered) return true;

        Vector3 dirToCamera = (inspectionCam.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, dirToCamera);

        if (dotProduct < facingAngleDotThreshold)
        {

            DecreaseDwell();
            return false;
        }

        Vector3 screenPos = inspectionCam.WorldToScreenPoint(transform.position);
        if (screenPos.z < 0)
        {
            DecreaseDwell();
            return false;
        }

        float screenDistance = Vector2.Distance(screenCenter, new Vector2(screenPos.x, screenPos.y));
        if (screenDistance <= screenRadius)
        {

            currentDwellTime += Time.unscaledDeltaTime;
            OnDwellProgressChanged?.Invoke(DwellProgress);

            if (currentDwellTime >= requiredDwellTime)
            {
                isDiscovered = true;
                OnDiscovered?.Invoke(this);
            }
            return true;
        }
        else
        {
            DecreaseDwell();
            return false;
        }
    }

    private void DecreaseDwell()
    {
        if (currentDwellTime > 0f)
        {
            currentDwellTime = Mathf.Max(0f, currentDwellTime - Time.unscaledDeltaTime * 2f);
            OnDwellProgressChanged?.Invoke(DwellProgress);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isDiscovered ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.05f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.15f);
    }
}
