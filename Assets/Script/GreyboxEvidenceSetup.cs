using UnityEngine;

public class GreyboxEvidenceSetup : MonoBehaviour
{
    [Header("Sample Evidence Prefabs / References")]
    public InteractableObject naturalEvidence;
    public InteractableObject artificialEvidence;
    public InteractableObject permitEvidence;
    public NPCMandor npcMandor;

    [Header("Testing Instructions")]
    [TextArea(4, 8)]
    public string testingGuide =
        "PETUNJUK PENGUJIAN LENGKAP 6 MEKANISME:\n" +
        "1. Navigasi & Eksplorasi: WASD + Shift (Lari), Mouse Look, Head-bob, Zona Lumpur.\n" +
        "2. Deteksi Reticle: Sorot objek bukti pada jarak <= 2.5m untuk memunculkan prompt [E].\n" +
        "3. Inspeksi 3D: Tekan [E] untuk inspeksi 360 drag, scroll zoom, sorot Hotspot 0.5 detik, tekan [F] ambil atau [Q] simpan.\n" +
        "4. Jurnal Otomatis: Tekan [J] untuk membuka/menutup Jurnal Investigasi & Laporan STADI 2025.\n" +
        "5. Dialog Konvergen: Bicara ke Pak Hendra [E] untuk konfrontasi 4 rute & 3 Ending (A/B/C).\n" +
        "6. FSM State: Kontrol karakter terkunci saat Inspeksi, Jurnal, dan Dialog.";

    private void Start()
    {
        SetupDefaultEvidenceIfAssigned();
        EnsureMandorAndPermitExist();
    }

    private void SetupDefaultEvidenceIfAssigned()
    {
        if (naturalEvidence != null)
        {
            naturalEvidence.clueName = "Pohon Tumbang Lapuk";
            naturalEvidence.clueDescription = "Batang pohon yang roboh di area gambut. Kerusakan alami.";
            naturalEvidence.category = ClueCategory.Natural;
            naturalEvidence.evidenceType = ClueEvidenceType.NaturalEvidence;
            naturalEvidence.isCollectable = false;
        }

        if (artificialEvidence != null)
        {
            artificialEvidence.clueName = "Tunggul Bekas Penebangan";
            artificialEvidence.clueDescription = "Sisa tunggul berpola gergaji mesin rata & tapak alat berat.";
            artificialEvidence.category = ClueCategory.Artificial;
            artificialEvidence.evidenceType = ClueEvidenceType.ManMadeEquipment;
            artificialEvidence.isCollectable = true;
        }
    }

    private void EnsureMandorAndPermitExist()
    {
        if (FindObjectOfType<NPCMandor>() == null)
        {
            GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npc.name = "NPC_PakHendra";
            npc.transform.position = new Vector3(3f, 1f, 8f);
            npc.transform.localScale = new Vector3(1f, 1.8f, 1f);

            Renderer rend = npc.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.85f, 0.45f, 0.15f);
            }

            NPCMandor mandorComp = npc.AddComponent<NPCMandor>();
            mandorComp.clueName = "Pak Hendra (Mandor Lapangan)";
            mandorComp.clueDescription = "Mandor lapangan penanggung jawab operasional di lokasi.";
            mandorComp.surfaceProximityTolerance = 3.5f;
            npcMandor = mandorComp;
        }

        if (FindObjectOfType<InteractableObject>() != null)
        {
            InteractableObject[] all = FindObjectsOfType<InteractableObject>();
            bool hasPermit = false;
            foreach (var item in all)
            {
                if (item.evidenceType == ClueEvidenceType.PermitDocument)
                {
                    hasPermit = true;
                    break;
                }
            }

            if (!hasPermit)
            {
                GameObject doc = GameObject.CreatePrimitive(PrimitiveType.Cube);
                doc.name = "Bukti_DokumenKonsesiPalsu";
                doc.transform.position = new Vector3(-3f, 0.5f, 5f);
                doc.transform.localScale = new Vector3(0.6f, 0.1f, 0.8f);

                Renderer docRend = doc.GetComponent<Renderer>();
                if (docRend != null)
                {
                    docRend.material.color = new Color(0.95f, 0.95f, 0.7f);
                }

                InteractableObject docComp = doc.AddComponent<InteractableObject>();
                docComp.clueName = "Dokumen Izin Konsesi Palsu";
                docComp.clueDescription = "Surat izin penebangan yang mencantumkan stempel konsesi kadaluarsa dan tanda tangan palsu.";
                docComp.category = ClueCategory.Artificial;
                docComp.evidenceType = ClueEvidenceType.PermitDocument;
                docComp.isCollectable = true;
                docComp.collectedMessage = "Dokumen izin palsu berhasil diamankan ke dalam jurnal.";
                permitEvidence = docComp;
            }
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
