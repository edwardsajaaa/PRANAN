using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FogZone : MonoBehaviour
{
    [Header("Target Fog Settings")]
    [Tooltip("Jarak kabut mulai muncul (mode Linear)")]
    [SerializeField] private float targetStartDistance = 2f;

    [Tooltip("Jarak kabut mencapai kerapatan penuh (mode Linear)")]
    [SerializeField] private float targetEndDistance = 20f;

    [Tooltip("Kerapatan kabut (mode Exponential)")]
    [SerializeField] private float targetDensity = 0.05f;

    [Tooltip("Centang jika ingin mengubah warna kabut di zona ini")]
    [SerializeField] private bool overrideColor = false;
    [SerializeField] private Color targetColor = new Color(0.55f, 0.40f, 0.28f, 1f);

    [Header("Transition Settings")]
    [Tooltip("Durasi transisi perubahan kabut dalam detik")]
    [SerializeField] private float transitionDuration = 2.5f;

    private static float defaultStart;
    private static float defaultEnd;
    private static float defaultDensity;
    private static Color defaultColor;
    private static bool defaultsCaptured;

    private static Coroutine activeTransition;
    private static MonoBehaviour transitionRunner;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        if (!defaultsCaptured)
        {
            defaultStart = RenderSettings.fogStartDistance;
            defaultEnd = RenderSettings.fogEndDistance;
            defaultDensity = RenderSettings.fogDensity;
            defaultColor = RenderSettings.fogColor;
            defaultsCaptured = true;
        }

        transitionRunner = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        Color col = overrideColor ? targetColor : RenderSettings.fogColor;
        StartFogTransition(targetStartDistance, targetEndDistance, targetDensity, col, transitionDuration);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        Color col = overrideColor ? defaultColor : RenderSettings.fogColor;
        StartFogTransition(defaultStart, defaultEnd, defaultDensity, col, transitionDuration);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
    }

    private void StartFogTransition(float toStart, float toEnd, float toDensity, Color toColor, float duration)
    {
        if (activeTransition != null && transitionRunner != null)
        {
            transitionRunner.StopCoroutine(activeTransition);
        }

        transitionRunner = this;
        activeTransition = StartCoroutine(TransitionRoutine(toStart, toEnd, toDensity, toColor, duration));
    }

    private IEnumerator TransitionRoutine(float toStart, float toEnd, float toDensity, Color toColor, float duration)
    {
        float fromStart = RenderSettings.fogStartDistance;
        float fromEnd = RenderSettings.fogEndDistance;
        float fromDensity = RenderSettings.fogDensity;
        Color fromColor = RenderSettings.fogColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            RenderSettings.fogStartDistance = Mathf.Lerp(fromStart, toStart, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(fromEnd, toEnd, t);
            RenderSettings.fogDensity = Mathf.Lerp(fromDensity, toDensity, t);
            RenderSettings.fogColor = Color.Lerp(fromColor, toColor, t);

            yield return null;
        }

        RenderSettings.fogStartDistance = toStart;
        RenderSettings.fogEndDistance = toEnd;
        RenderSettings.fogDensity = toDensity;
        RenderSettings.fogColor = toColor;
        activeTransition = null;
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        // Gambar visualisasi volume kabut di Scene View
        Color baseCol = overrideColor ? targetColor : new Color(0.85f, 0.85f, 0.85f, 1f);

        Gizmos.color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);

        Gizmos.color = new Color(baseCol.r, baseCol.g, baseCol.b, 0.7f);
        Gizmos.DrawWireCube(box.center, box.size);
    }
}

