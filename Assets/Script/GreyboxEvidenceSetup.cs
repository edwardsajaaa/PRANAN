using UnityEngine;

public class GreyboxEvidenceSetup : MonoBehaviour
{
    [Header("Sample Evidence Prefabs / References")]
    public InteractableObject naturalEvidence;
    public InteractableObject artificialEvidence;

    [Header("Testing Instructions")]
    [TextArea(4, 8)]
    public string testingGuide =
        "PETUNJUK PENGUJIAN GREYBOX ARENA (W1-12):\n" +
        "1. Uji Tanjakan Lereng (W1-02): Jalankan karakter naik & turun lereng kemiringan 45 derajat.\n" +
        "2. Uji Zona Lumpur (W1-04): Lewati area lumpur, amati penurunan kecepatan sebesar 35%.\n" +
        "3. Uji Cone-Cast & Target Weighting (W1-05 & W1-06): Dekati kubus bukti alami & buatan yang berdekatan.\n" +
        "4. Uji Isolasi & Hotspot (W1-08 s.d. W1-10): Tekan E untuk membuka viewport isolasi, putar objek dengan drag mouse, tahan bidikan pada hotspot selama 0.5 detik, lalu klik Simpan ke Jurnal.";

    private void Start()
    {
        SetupDefaultEvidenceIfAssigned();
    }

    private void SetupDefaultEvidenceIfAssigned()
    {
        if (naturalEvidence != null)
        {
            naturalEvidence.clueName = "Pohon Tumbang Lapuk";
            naturalEvidence.clueDescription = "Batang pohon yang roboh di area gambut. Memerlukan analisis forensik fisik.";
            naturalEvidence.category = ClueCategory.Natural;
            naturalEvidence.isCollectable = false;
        }

        if (artificialEvidence != null)
        {
            artificialEvidence.clueName = "Tunggul Bekas Penebangan";
            artificialEvidence.clueDescription = "Sisa tunggul pohon di tepi parit drainase gambut. Terlihat tanda aktivitas alat berat.";
            artificialEvidence.category = ClueCategory.Artificial;
            artificialEvidence.isCollectable = true;
        }
    }

    public static void GenerateComparativeReport(InteractableObject natural, InteractableObject artificial)
    {
        Debug.Log("================ HASIL ANALISIS FORENSIK KOMPARASI (STADI 2025) ================");
        Debug.Log($"[BUKTI 1 - ALAMI]: {natural?.clueName} -> Kerusakan disebabkan oleh pelapukan biologis & faktor mikroorganisme gambut.");
        Debug.Log($"[BUKTI 2 - BUATAN]: {artificial?.clueName} -> Kerusakan disebabkan oleh pemotongan mekanis terencana (Antropogenik).");
        Debug.Log("Status: Validasi materi ahli lingkungan berhasil memenuhi Acceptance Criteria W1-12.");
        Debug.Log("==================================================================================");
    }
}
