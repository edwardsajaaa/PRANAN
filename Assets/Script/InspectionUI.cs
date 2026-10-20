using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InspectionUI : MonoBehaviour
{
    [Header("UI Containers")]
    public GameObject inspectionPanel;

    [Header("Clue Information Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI categoryText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statusLogText;

    [Header("Hotspot / Dwell Feedback")]
    public Image dwellProgressBar;
    public GameObject hotspotIndicator;

    [Header("Action Buttons")]
    public Button saveToJournalButton;
    public Button closeButton;

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (saveToJournalButton != null)
        {
            saveToJournalButton.onClick.AddListener(OnSaveToJournalClicked);
        }

        Hide();
    }

    public void Show(InteractableObject clue)
    {
        if (inspectionPanel != null) inspectionPanel.SetActive(true);

        if (titleText != null) titleText.text = clue.clueName;
        if (categoryText != null) categoryText.text = $"Kategori: {clue.category}";
        if (descriptionText != null) descriptionText.text = clue.clueDescription;

        if (statusLogText != null)
        {
            statusLogText.text = "Periksa detail objek. Arahkan pandangan ke titik anomali fisik...";
            statusLogText.color = Color.white;
        }

        SetSaveButtonState(false);
        UpdateDwellProgress(0f);
    }

    public void Hide()
    {
        if (inspectionPanel != null) inspectionPanel.SetActive(false);
        UpdateDwellProgress(0f);
    }

    public void UpdateDwellProgress(float progress)
    {
        if (dwellProgressBar != null)
        {
            dwellProgressBar.fillAmount = progress;
            dwellProgressBar.gameObject.SetActive(progress > 0.01f);
        }
    }

    public void SetSaveButtonState(bool isUnlocked)
    {
        if (saveToJournalButton != null)
        {
            saveToJournalButton.interactable = isUnlocked;
        }
    }

    public void OnHotspotIdentified(AnomalyHotspot hotspot)
    {
        SetSaveButtonState(true);

        if (statusLogText != null)
        {
            statusLogText.text = $"[ANOMALI TERVERIFIKASI]\n{hotspot.anomalyTitle}: {hotspot.anomalyObservationLog}";
            statusLogText.color = Color.yellow;
        }
    }

    private void OnCloseClicked()
    {
        if (ClueInspector.Instance != null)
        {
            ClueInspector.Instance.CloseInspection();
        }
    }

    private void OnSaveToJournalClicked()
    {
        Debug.Log("[Jurnal Investigasi] Bukti dan anomali forensik berhasil dicatat ke dalam jurnal!");
        if (ClueInspector.Instance != null)
        {
            ClueInspector.Instance.SaveAndClose();
        }
    }
}
