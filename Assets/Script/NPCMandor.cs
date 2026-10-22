using UnityEngine;

public class NPCMandor : InteractableObject
{
    private void Reset()
    {
        clueName = "Pak Hendra (Mandor Lapangan)";
        clueDescription = "Mandor lapangan penanggung jawab aktivitas penebangan di area konsesi.";
        category = ClueCategory.Artificial;
        isCollectable = false;
        surfaceProximityTolerance = 3.0f;
    }

    public override void Interact(Transform playerTransform)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartConfrontation(this);
        }
        else
        {
            Debug.LogWarning("[NPCMandor] DialogueManager tidak ditemukan di scene!");
        }
    }
}
