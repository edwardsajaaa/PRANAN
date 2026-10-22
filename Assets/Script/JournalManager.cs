using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ClueEvidenceType
{
    General,
    PermitDocument,
    ManMadeEquipment,
    NaturalEvidence
}

[System.Serializable]
public class JournalEntry
{
    public string clueName;
    public string category;
    public string observationLog;
    public string timestamp;
    public ClueEvidenceType evidenceType;
    public bool isCollected;
}

public class JournalManager : MonoBehaviour
{
    private static JournalManager instance;
    public static JournalManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<JournalManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("JournalManager");
                    instance = go.AddComponent<JournalManager>();
                }
            }
            return instance;
        }
    }

    [Header("STADI 2025 Investigation Boolean Variables")]
    public bool hasPermitFraud = false;
    public bool hasManMadeClue = false;
    public bool hasNaturalClue = false;

    [Header("Journal Data")]
    public List<JournalEntry> recordedEntries = new List<JournalEntry>();

    [Header("Key Input")]
    public KeyCode toggleKey = KeyCode.J;
    public KeyCode closeKey = KeyCode.Q;

    [Header("UI Containers")]
    public GameObject journalCanvas;
    public GameObject journalPanel;
    public TextMeshProUGUI statusSummaryText;
    public TextMeshProUGUI clueListText;
    public TextMeshProUGUI stadiEducationalText;
    public Button closeButton;

    private bool isJournalOpen = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        EnsureJournalUI();
        if (journalPanel != null) journalPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (GameStateManager.Instance == null) return;

            if (GameStateManager.Instance.CurrentState == GameState.Exploration)
            {
                OpenJournal();
            }
            else if (GameStateManager.Instance.CurrentState == GameState.Journal)
            {
                CloseJournal();
            }
        }
        else if (isJournalOpen && (Input.GetKeyDown(closeKey) || Input.GetKeyDown(KeyCode.Escape)))
        {
            CloseJournal();
        }
    }

    public void RecordEvidence(string name, string category, string log, ClueEvidenceType type, bool collected)
    {
        switch (type)
        {
            case ClueEvidenceType.PermitDocument:
                hasPermitFraud = true;
                break;
            case ClueEvidenceType.ManMadeEquipment:
                hasManMadeClue = true;
                break;
            case ClueEvidenceType.NaturalEvidence:
                hasNaturalClue = true;
                break;
        }

        JournalEntry existing = recordedEntries.Find(e => e.clueName == name);
        if (existing != null)
        {
            if (!string.IsNullOrEmpty(log)) existing.observationLog = log;
            if (collected) existing.isCollected = true;
        }
        else
        {
            JournalEntry newEntry = new JournalEntry
            {
                clueName = name,
                category = category,
                observationLog = string.IsNullOrEmpty(log) ? "Barang bukti fisik telah diperiksa di lokasi." : log,
                timestamp = DateTime.Now.ToString("HH:mm:ss"),
                evidenceType = type,
                isCollected = collected
            };
            recordedEntries.Add(newEntry);
        }

        UpdateJournalDisplay();
        Debug.Log($"[JournalManager] Bukti dicatat: {name} (PermitFraud={hasPermitFraud}, ManMade={hasManMadeClue}, Natural={hasNaturalClue})");
    }

    public void OpenJournal()
    {
        EnsureJournalUI();
        isJournalOpen = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Journal);
        }

        UpdateJournalDisplay();
        if (journalPanel != null) journalPanel.SetActive(true);
    }

    public void CloseJournal()
    {
        isJournalOpen = false;
        if (journalPanel != null) journalPanel.SetActive(false);

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Exploration);
        }
    }

    public void UpdateJournalDisplay()
    {
        if (statusSummaryText != null)
        {
            string docStatus = hasPermitFraud ? "<color=#4CAF50>[TERVERIFIKASI]</color> Cacat Izin Konsesi" : "<color=#F44336>[BELUM DITEMUKAN]</color> Bukti Dokumen Izin";
            string trackStatus = hasManMadeClue ? "<color=#4CAF50>[TERVERIFIKASI]</color> Jejak Alat Berat / Gergaji" : "<color=#F44336>[BELUM DITEMUKAN]</color> Bukti Jejak Alat Berat";
            string natStatus = hasNaturalClue ? "<color=#4CAF50>[TERVERIFIKASI]</color> Bukti Lapukan Alami" : "<color=#9E9E9E>[OPSIONAL]</color> Bukti Pelapukan Alami";

            statusSummaryText.text =
                $"<b>STATUS BUKTI KUNCI INVESTIGASI:</b>\n" +
                $"• Dokumen Konsesi : {docStatus}\n" +
                $"• Jejak Fisik / Alat : {trackStatus}\n" +
                $"• Karakteristik Alami : {natStatus}";
        }

        if (clueListText != null)
        {
            if (recordedEntries.Count == 0)
            {
                clueListText.text = "<i>Belum ada barang bukti yang diinspeksi. Jelajahi area hutan dan dekati objek bukti untuk memulai penyelidikan...</i>";
            }
            else
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < recordedEntries.Count; i++)
                {
                    var e = recordedEntries[i];
                    string collectBadge = e.isCollected ? "<color=#81C784>[DIAMANKAN]</color>" : "<color=#64B5F6>[DIINSPEKSI]</color>";
                    sb.AppendLine($"<b>{i + 1}. {e.clueName}</b> {collectBadge}");
                    sb.AppendLine($"   Kategori: {e.category} | Jam: {e.timestamp}");
                    sb.AppendLine($"   Catatan: <i>{e.observationLog}</i>");
                    sb.AppendLine();
                }
                clueListText.text = sb.ToString();
            }
        }

        if (stadiEducationalText != null)
        {
            stadiEducationalText.text =
                "<b>ARSIP FORENSIK STADI 2025:</b>\n" +
                "Menurut laporan <i>Status Deforestasi Indonesia (STADI) 2025</i>, deforestasi antropogenik ilegal dicirikan oleh:\n" +
                "1. <b>Cacat Dokumen</b>: Manipulasi koordinat batas konsesi dan pemalsuan stempel verifikasi kehutanan.\n" +
                "2. <b>Jejak Mekanis</b>: Pola tebangan rata berbasis gergaji mesin dan tapak roda alat berat (ekskavator) di lahan gambut lindung.\n" +
                "3. <b>Kontras Alami vs Buatan</b>: Pohon tumbang alami roboh oleh dekomposisi biologis mikroba, berbeda dengan potongan terencana.";
        }
    }

    private void EnsureJournalUI()
    {
        if (journalPanel != null) return;

        GameObject canvasObj = new GameObject("JournalCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        journalCanvas = canvasObj;

        journalPanel = new GameObject("JournalPanel");
        journalPanel.transform.SetParent(canvasObj.transform, false);
        var pRect = journalPanel.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.08f, 0.08f);
        pRect.anchorMax = new Vector2(0.92f, 0.92f);
        pRect.offsetMin = Vector2.zero;
        pRect.offsetMax = Vector2.zero;

        var pImg = journalPanel.AddComponent<Image>();
        pImg.color = new Color(0.09f, 0.11f, 0.13f, 0.96f);

        GameObject headerObj = new GameObject("HeaderTitle");
        headerObj.transform.SetParent(journalPanel.transform, false);
        var hRect = headerObj.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.05f, 0.88f);
        hRect.anchorMax = new Vector2(0.95f, 0.98f);
        hRect.offsetMin = Vector2.zero;
        hRect.offsetMax = Vector2.zero;
        var hTmp = headerObj.AddComponent<TextMeshProUGUI>();
        hTmp.text = "JURNAL INVESTIGASI FORENSIK LINGKUNGAN (PRANAN)";
        hTmp.fontSize = 24;
        hTmp.fontStyle = FontStyles.Bold;
        hTmp.alignment = TextAlignmentOptions.Left;
        hTmp.color = new Color(0.95f, 0.85f, 0.55f);

        GameObject summaryObj = new GameObject("SummarySection");
        summaryObj.transform.SetParent(journalPanel.transform, false);
        var sRect = summaryObj.AddComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0.05f, 0.68f);
        sRect.anchorMax = new Vector2(0.95f, 0.87f);
        sRect.offsetMin = Vector2.zero;
        sRect.offsetMax = Vector2.zero;
        statusSummaryText = summaryObj.AddComponent<TextMeshProUGUI>();
        statusSummaryText.fontSize = 16;
        statusSummaryText.color = Color.white;

        GameObject listObj = new GameObject("ClueListSection");
        listObj.transform.SetParent(journalPanel.transform, false);
        var lRect = listObj.AddComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0.05f, 0.12f);
        lRect.anchorMax = new Vector2(0.55f, 0.66f);
        lRect.offsetMin = Vector2.zero;
        lRect.offsetMax = Vector2.zero;
        clueListText = listObj.AddComponent<TextMeshProUGUI>();
        clueListText.fontSize = 14;
        clueListText.color = new Color(0.9f, 0.9f, 0.9f);

        GameObject stadiObj = new GameObject("StadiSection");
        stadiObj.transform.SetParent(journalPanel.transform, false);
        var stRect = stadiObj.AddComponent<RectTransform>();
        stRect.anchorMin = new Vector2(0.58f, 0.12f);
        stRect.anchorMax = new Vector2(0.95f, 0.66f);
        stRect.offsetMin = Vector2.zero;
        stRect.offsetMax = Vector2.zero;
        stadiEducationalText = stadiObj.AddComponent<TextMeshProUGUI>();
        stadiEducationalText.fontSize = 13;
        stadiEducationalText.color = new Color(0.85f, 0.9f, 0.85f);

        GameObject btnObj = new GameObject("CloseButton");
        btnObj.transform.SetParent(journalPanel.transform, false);
        var bRect = btnObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0.02f);
        bRect.anchorMax = new Vector2(0.5f, 0.02f);
        bRect.anchoredPosition = new Vector2(0, 22);
        bRect.sizeDelta = new Vector2(220, 36);
        var bImg = btnObj.AddComponent<Image>();
        bImg.color = new Color(0.2f, 0.25f, 0.3f, 0.9f);
        closeButton = btnObj.AddComponent<Button>();
        closeButton.onClick.AddListener(CloseJournal);

        GameObject btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btRect = btnTextObj.AddComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.offsetMin = Vector2.zero;
        btRect.offsetMax = Vector2.zero;
        var btTmp = btnTextObj.AddComponent<TextMeshProUGUI>();
        btTmp.text = "[J / Q] Tutup Jurnal";
        btTmp.fontSize = 15;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.color = Color.white;

        UpdateJournalDisplay();
    }
}
