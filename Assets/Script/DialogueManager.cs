using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum EndingType
{
    None,
    EndingA_Good,
    EndingB_Neutral,
    EndingC_Bad
}

public class DialogueManager : MonoBehaviour
{
    private static DialogueManager instance;
    public static DialogueManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DialogueManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DialogueManager");
                    instance = go.AddComponent<DialogueManager>();
                }
            }
            return instance;
        }
    }

    [Header("UI Containers")]
    public GameObject dialogueCanvas;
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueBodyText;
    public Transform optionsContainer;

    [Header("Ending Screen")]
    public GameObject endingPanel;
    public TextMeshProUGUI endingBadgeText;
    public TextMeshProUGUI endingTitleText;
    public TextMeshProUGUI endingSynopsisText;
    public Button restartButton;
    public Button mainMenuButton;

    private List<GameObject> activeOptionButtons = new List<GameObject>();

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
        EnsureDialogueUI();
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);
    }

    public void StartConfrontation(NPCMandor mandor)
    {
        EnsureDialogueUI();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Dialogue);
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (endingPanel != null) endingPanel.SetActive(false);

        EvaluateConvergingDecisionTree();
    }

    public void EvaluateConvergingDecisionTree()
    {
        bool docFraud = JournalManager.Instance != null && JournalManager.Instance.hasPermitFraud;
        bool manMade = JournalManager.Instance != null && JournalManager.Instance.hasManMadeClue;

        ClearOptions();

        if (speakerNameText != null) speakerNameText.text = "Pak Hendra (Mandor Lapangan)";

        if (docFraud && manMade)
        {
            if (dialogueBodyText != null)
            {
                dialogueBodyText.text =
                    "\"Siapa kamu?! Berani-beraninya masuk ke area konsesi kami tanpa izin resmi! " +
                    "Semua urusan di sini sudah ada yang mengatur, jangan ikut campur urusan orang lain!\"";
            }

            CreateDialogueOption(
                "Tuduh dengan Bukti Mutlak: Kami punya dokumen konsesi palsu dan bukti jejak pemotongan alat berat ilegal!",
                () => TriggerEnding(EndingType.EndingA_Good)
            );
        }
        else if (docFraud && !manMade)
        {
            if (dialogueBodyText != null)
            {
                dialogueBodyText.text =
                    "\"Mau apa kamu memeriksa lokasi ini? Perusahaan kami punya izin legal lengkap dari dinas terkait. " +
                    "Kamu tidak punya wewenang menggeledah tanpa surat tugas!\"";
            }

            CreateDialogueOption(
                "Gugat Cacat Administrasi: Stempel verifikasi izin kalian terbukti fiktif dan cacat hukum!",
                () => TriggerEnding(EndingType.EndingB_Neutral)
            );
        }
        else if (!docFraud && manMade)
        {
            if (dialogueBodyText != null)
            {
                dialogueBodyText.text =
                    "\"Anak muda, hutan gambut ini berbahaya. Lebih baik kamu putar balik sekarang juga " +
                    "sebelum kamu tersesat dan menuduh hal yang tidak-tidak!\"";
            }

            CreateDialogueOption(
                "Gertak Pelaku: Saya melihat pola tebangan mesin dan jalur ekskavator di gambut lindung. Mengaku saja!",
                () => TriggerEnding(EndingType.EndingB_Neutral)
            );

            CreateDialogueOption(
                "Mundur / Ragu: Maaf... saya hanya pendaki yang tidak sengaja tersesat di sini...",
                () => TriggerEnding(EndingType.EndingC_Bad)
            );
        }
        else
        {
            if (dialogueBodyText != null)
            {
                dialogueBodyText.text =
                    "\"Sedang apa kamu berkeliaran di sini tanpa tujuan? Jangan berani memfitnah tanpa selembar pun bukti! " +
                    "Pergi dari sini sebelum saya panggil penjaga!\"";
            }

            CreateDialogueOption(
                "Interogasi Kosong: Saya tahu kalian sedang melakukan pembalakan liar di hutan ini!",
                () => TriggerEnding(EndingType.EndingC_Bad)
            );
        }
    }

    private void CreateDialogueOption(string optionText, UnityEngine.Events.UnityAction onClickAction)
    {
        if (optionsContainer == null) return;

        GameObject btnObj = new GameObject("OptionBtn");
        btnObj.transform.SetParent(optionsContainer, false);
        var bRect = btnObj.AddComponent<RectTransform>();
        bRect.sizeDelta = new Vector2(850, 48);

        var bImg = btnObj.AddComponent<Image>();
        bImg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);

        var btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(onClickAction);

        var colors = btn.colors;
        colors.highlightedColor = new Color(0.28f, 0.38f, 0.48f, 1f);
        colors.pressedColor = new Color(0.12f, 0.16f, 0.2f, 1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var tRect = textObj.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.02f, 0f);
        tRect.anchorMax = new Vector2(0.98f, 1f);
        tRect.offsetMin = Vector2.zero;
        tRect.offsetMax = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = optionText;
        tmp.fontSize = 15;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;

        activeOptionButtons.Add(btnObj);
    }

    private void ClearOptions()
    {
        foreach (var obj in activeOptionButtons)
        {
            if (obj != null) Destroy(obj);
        }
        activeOptionButtons.Clear();
    }

    public void TriggerEnding(EndingType ending)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(true);

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Ending);
        }

        switch (ending)
        {
            case EndingType.EndingA_Good:
                if (endingBadgeText != null)
                {
                    endingBadgeText.text = "<color=#4CAF50>ENDING A: KEMENANGAN KEADILAN LINGKUNGAN (GOOD ENDING)</color>";
                }
                if (endingTitleText != null)
                {
                    endingTitleText.text = "Operasi Pembalakan Liar Dihancurkan Total";
                }
                if (endingSynopsisText != null)
                {
                    endingSynopsisText.text =
                        "Berkat perpaduan bukti mutlak dokumen konsesi fiktif dan jejak mekanis alat berat di lapangan, " +
                        "aparat penegak hukum berhasil membongkar skandal hingga ke tingkat korporasi dalang utama.\n\n" +
                        "Mandor dan seluruh pemodal ditangkap. Kawasan hutan lindung gambut diselamatkan sesuai rekomendasi " +
                        "laporan Status Deforestasi Indonesia (STADI) 2025. Investigasi forensik Anda sukses sempurna!";
                }
                break;

            case EndingType.EndingB_Neutral:
                if (endingBadgeText != null)
                {
                    endingBadgeText.text = "<color=#FFC107>ENDING B: SANKSI ADMINISTRATIF PARSIAL (NEUTRAL ENDING)</color>";
                }
                if (endingTitleText != null)
                {
                    endingTitleText.text = "Pekerja Lapangan Ditangkap, Dalang Utama Lolos";
                }
                if (endingSynopsisText != null)
                {
                    endingSynopsisText.text =
                        "Bukti yang Anda bawa berhasil menjerat mandor dan menyegel alat berat di lokasi.\n\n" +
                        "Namun akibat ketiadaan mata rantai bukti yang utuh antara dokumen dan jejak fisik, " +
                        "korporasi dalang utama berhasil mencuci tangan melalui celah hukum administratif. " +
                        "Hutan terselamatkan sementara, namun keadilan belum tuntas sepenuhnya.";
                }
                break;

            case EndingType.EndingC_Bad:
                if (endingBadgeText != null)
                {
                    endingBadgeText.text = "<color=#F44336>ENDING C: KEGAGALAN INVESTIGASI (BAD ENDING)</color>";
                }
                if (endingTitleText != null)
                {
                    endingTitleText.text = "Tuduhan Ditolak, Kasus Ditutup Tanpa Hasil";
                }
                if (endingSynopsisText != null)
                {
                    endingSynopsisText.text =
                        "Penyelidikan tidak didukung oleh bukti forensik yang memadai. " +
                        "Mandor menertawakan tuduhan Anda dan mengusir penyelidik dari lokasi.\n\n" +
                        "Tanpa bukti fisik yang terverifikasi, laporan investigasi dinyatakan gugur oleh pengadilan. " +
                        "Aktivitas perambahan hutan terus berlanjut tanpa hambatan...";
                }
                break;
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            RestartGame();
        }
    }

    private void EnsureDialogueUI()
    {
        if (dialoguePanel != null && endingPanel != null) return;

        GameObject canvasObj = new GameObject("DialogueCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        dialogueCanvas = canvasObj;

        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(canvasObj.transform, false);
        var pRect = dialoguePanel.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.05f, 0.05f);
        pRect.anchorMax = new Vector2(0.95f, 0.42f);
        pRect.offsetMin = Vector2.zero;
        pRect.offsetMax = Vector2.zero;

        var pImg = dialoguePanel.AddComponent<Image>();
        pImg.color = new Color(0.08f, 0.1f, 0.12f, 0.95f);

        GameObject speakerObj = new GameObject("SpeakerName");
        speakerObj.transform.SetParent(dialoguePanel.transform, false);
        var sRect = speakerObj.AddComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0.03f, 0.82f);
        sRect.anchorMax = new Vector2(0.5f, 0.96f);
        sRect.offsetMin = Vector2.zero;
        sRect.offsetMax = Vector2.zero;
        speakerNameText = speakerObj.AddComponent<TextMeshProUGUI>();
        speakerNameText.text = "Pak Hendra (Mandor Lapangan)";
        speakerNameText.fontSize = 18;
        speakerNameText.fontStyle = FontStyles.Bold;
        speakerNameText.color = new Color(0.95f, 0.75f, 0.35f);

        GameObject bodyObj = new GameObject("DialogueBody");
        bodyObj.transform.SetParent(dialoguePanel.transform, false);
        var bRect = bodyObj.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.03f, 0.52f);
        bRect.anchorMax = new Vector2(0.97f, 0.80f);
        bRect.offsetMin = Vector2.zero;
        bRect.offsetMax = Vector2.zero;
        dialogueBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        dialogueBodyText.fontSize = 15;
        dialogueBodyText.color = Color.white;

        GameObject optObj = new GameObject("OptionsContainer");
        optObj.transform.SetParent(dialoguePanel.transform, false);
        var oRect = optObj.AddComponent<RectTransform>();
        oRect.anchorMin = new Vector2(0.03f, 0.05f);
        oRect.anchorMax = new Vector2(0.97f, 0.48f);
        oRect.offsetMin = Vector2.zero;
        oRect.offsetMax = Vector2.zero;

        var vlg = optObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        optionsContainer = optObj.transform;

        endingPanel = new GameObject("EndingPanel");
        endingPanel.transform.SetParent(canvasObj.transform, false);
        var epRect = endingPanel.AddComponent<RectTransform>();
        epRect.anchorMin = new Vector2(0.12f, 0.12f);
        epRect.anchorMax = new Vector2(0.88f, 0.88f);
        epRect.offsetMin = Vector2.zero;
        epRect.offsetMax = Vector2.zero;

        var epImg = endingPanel.AddComponent<Image>();
        epImg.color = new Color(0.06f, 0.07f, 0.09f, 0.98f);

        GameObject badgeObj = new GameObject("EndingBadge");
        badgeObj.transform.SetParent(endingPanel.transform, false);
        var badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.05f, 0.86f);
        badgeRect.anchorMax = new Vector2(0.95f, 0.96f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;
        endingBadgeText = badgeObj.AddComponent<TextMeshProUGUI>();
        endingBadgeText.fontSize = 18;
        endingBadgeText.fontStyle = FontStyles.Bold;
        endingBadgeText.alignment = TextAlignmentOptions.Center;

        GameObject titleObj = new GameObject("EndingTitle");
        titleObj.transform.SetParent(endingPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.74f);
        titleRect.anchorMax = new Vector2(0.95f, 0.84f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        endingTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        endingTitleText.fontSize = 22;
        endingTitleText.fontStyle = FontStyles.Bold;
        endingTitleText.alignment = TextAlignmentOptions.Center;
        endingTitleText.color = Color.white;

        GameObject synObj = new GameObject("EndingSynopsis");
        synObj.transform.SetParent(endingPanel.transform, false);
        var synRect = synObj.AddComponent<RectTransform>();
        synRect.anchorMin = new Vector2(0.08f, 0.22f);
        synRect.anchorMax = new Vector2(0.92f, 0.70f);
        synRect.offsetMin = Vector2.zero;
        synRect.offsetMax = Vector2.zero;
        endingSynopsisText = synObj.AddComponent<TextMeshProUGUI>();
        endingSynopsisText.fontSize = 16;
        endingSynopsisText.alignment = TextAlignmentOptions.Center;
        endingSynopsisText.color = new Color(0.9f, 0.9f, 0.9f);

        GameObject restartBtnObj = new GameObject("RestartButton");
        restartBtnObj.transform.SetParent(endingPanel.transform, false);
        var rRect = restartBtnObj.AddComponent<RectTransform>();
        rRect.anchorMin = new Vector2(0.28f, 0.06f);
        rRect.anchorMax = new Vector2(0.48f, 0.14f);
        rRect.offsetMin = Vector2.zero;
        rRect.offsetMax = Vector2.zero;
        var rImg = restartBtnObj.AddComponent<Image>();
        rImg.color = new Color(0.2f, 0.35f, 0.22f, 0.95f);
        restartButton = restartBtnObj.AddComponent<Button>();
        restartButton.onClick.AddListener(RestartGame);

        GameObject rTextObj = new GameObject("Text");
        rTextObj.transform.SetParent(restartBtnObj.transform, false);
        var rtRect = rTextObj.AddComponent<RectTransform>();
        rtRect.anchorMin = Vector2.zero;
        rtRect.anchorMax = Vector2.one;
        rtRect.offsetMin = Vector2.zero;
        rtRect.offsetMax = Vector2.zero;
        var rtTmp = rTextObj.AddComponent<TextMeshProUGUI>();
        rtTmp.text = "Ulangi Investigasi";
        rtTmp.fontSize = 16;
        rtTmp.alignment = TextAlignmentOptions.Center;
        rtTmp.color = Color.white;

        GameObject menuBtnObj = new GameObject("MainMenuButton");
        menuBtnObj.transform.SetParent(endingPanel.transform, false);
        var mRect = menuBtnObj.AddComponent<RectTransform>();
        mRect.anchorMin = new Vector2(0.52f, 0.06f);
        mRect.anchorMax = new Vector2(0.72f, 0.14f);
        mRect.offsetMin = Vector2.zero;
        mRect.offsetMax = Vector2.zero;
        var mImg = menuBtnObj.AddComponent<Image>();
        mImg.color = new Color(0.22f, 0.26f, 0.32f, 0.95f);
        mainMenuButton = menuBtnObj.AddComponent<Button>();
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        GameObject mTextObj = new GameObject("Text");
        mTextObj.transform.SetParent(menuBtnObj.transform, false);
        var mtRect = mTextObj.AddComponent<RectTransform>();
        mtRect.anchorMin = Vector2.zero;
        mtRect.anchorMax = Vector2.one;
        mtRect.offsetMin = Vector2.zero;
        mtRect.offsetMax = Vector2.zero;
        var mtTmp = mTextObj.AddComponent<TextMeshProUGUI>();
        mtTmp.text = "Menu Utama";
        mtTmp.fontSize = 16;
        mtTmp.alignment = TextAlignmentOptions.Center;
        mtTmp.color = Color.white;
    }
}
