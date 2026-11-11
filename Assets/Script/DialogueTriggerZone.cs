using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger zone serbaguna untuk memicu percakapan radio atau monolog
/// saat pemain memasuki area tertentu (misal: melewati portal/gerbang pos).
/// </summary>
[RequireComponent(typeof(Collider))]
public class DialogueTriggerZone : MonoBehaviour
{
    public enum TriggerType
    {
        Scene1GerbangPos,
        CustomMonologue,
        CustomEventOnly
    }

    [Header("Trigger Settings")]
    [Tooltip("Jenis dialog yang akan dipicu")]
    [SerializeField] private TriggerType triggerType = TriggerType.Scene1GerbangPos;

    [Tooltip("Hanya memicu dialog satu kali")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("Jeda waktu (detik) sebelum dialog dimulai setelah pemain menyentuh trigger")]
    [SerializeField] private float triggerDelay = 0.5f;

    [Header("Custom Monologue (Jika tipe CustomMonologue dipilih)")]
    [TextArea(2, 4)]
    [SerializeField] private string monologueText = "";
    [SerializeField] private float monologueDuration = 5f;

    [Header("Optional Events")]
    [SerializeField] private UnityEvent onPlayerEnter;

    [Header("Gizmo Visualizer")]
    [SerializeField] private Color gizmoColor = new Color(0f, 0.8f, 1f, 0.35f);
    [SerializeField] private Color gizmoWireColor = new Color(0f, 0.8f, 1f, 0.9f);

    private bool hasTriggered = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;
        if (!IsPlayer(other)) return;

        Debug.Log($"[DialogueTriggerZone] Pemain melewati trigger '{gameObject.name}'. Memulai dialog: {triggerType}");
        hasTriggered = true;
        StartCoroutine(ExecuteTriggerRoutine());
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null;
    }

    private IEnumerator ExecuteTriggerRoutine()
    {
        if (triggerDelay > 0f)
        {
            yield return new WaitForSeconds(triggerDelay);
        }

        switch (triggerType)
        {
            case TriggerType.Scene1GerbangPos:
                if (RadioDialogueManager.Instance != null)
                {
                    RadioDialogueManager.Instance.TryTriggerScene1();
                }
                else
                {
                    Debug.LogWarning("[DialogueTriggerZone] RadioDialogueManager.Instance tidak ditemukan di scene!");
                }
                break;

            case TriggerType.CustomMonologue:
                if (RadioDialogueManager.Instance != null && !string.IsNullOrEmpty(monologueText))
                {
                    RadioDialogueManager.Instance.ShowMonologue(monologueText, monologueDuration);
                }
                break;

            case TriggerType.CustomEventOnly:
                break;
        }

        onPlayerEnter?.Invoke();
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.color = gizmoColor;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = gizmoWireColor;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}

