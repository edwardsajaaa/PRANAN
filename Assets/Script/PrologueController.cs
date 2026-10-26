using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class PrologueController : MonoBehaviour
{
    private static PrologueController instance;
    public static PrologueController Instance => instance;

    [Header("Prologue Header & Content")]
    public string prologueTitle = "ABOUT THE GAME";
    
    [TextArea(8, 18)]
    public string prologueBody = 
        "Terima kasih telah memainkan PRANAN.\n\n" +
        "Game ini dirancang sebagai pengalaman investigasi forensik lingkungan interaktif yang mengangkat isu deforestasi dan pembalakan liar di kawasan gambut, berlandaskan data faktual Status Deforestasi Indonesia (STADI) 2025.\n\n" +
        "Anda bertindak sebagai penyidik independen yang diterjunkan ke area konsesi terpencil. Tugas Anda adalah menelusuri kejanggalan fisik medan, mengidentifikasi perbedaan pola penebangan mekanik vs pelapukan alami, mengamankan dokumen izin fiktif ke dalam jurnal, dan mengonfrontasi penanggung jawab lapangan.\n\n" +
        "Gunakan ketelitian observasi Anda untuk mengungkap fakta sebenarnya di balik kerusakan hutan ini.\n\n" +
        "Selamat bertugas dan selamat bermain!";

    [Header("Author Signature")]
    public string authorSignature = "EDWARD BENEDICT";
    public Sprite avatarSprite;

    [Header("Prompt Settings")]
    public string continuePrompt = "[ Tekan SPASI atau KLIK untuk Mulai ]";
    public float promptBlinkSpeed = 2.5f;

    [Header("Timing & Animation")]
    public float fadeInDuration = 1.0f;
    public float fadeOutDuration = 0.8f;
    public bool autoStartOnPlay = true;

    [Header("UI Element References (Auto-Created If Null)")]
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI titleTextComponent;
    public TextMeshProUGUI bodyTextComponent;
    public TextMeshProUGUI authorTextComponent;
    public Image avatarImageComponent;
    public TextMeshProUGUI promptTextComponent;
    public TMP_FontAsset preferredFont;

    private bool isTransitioning = false;
    private bool isDismissed = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetupUIHierarchyIfMissing();
    }

    private void Start()
    {
        if (autoStartOnPlay && !isDismissed)
        {
            StartCoroutine(PlayPrologueSequence());
        }
    }

    private void Update()
    {
        if (isDismissed || isTransitioning) return;

        // Efek kedip halus pada teks prompt bawah
        if (promptTextComponent != null)
        {
            float alpha = Mathf.PingPong(Time.unscaledTime * promptBlinkSpeed, 1f);
            Color c = promptTextComponent.color;
            c.a = Mathf.Lerp(0.25f, 1f, alpha);
            promptTextComponent.color = c;
        }

        // Input untuk melanjutkan
        if (Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.KeypadEnter) || 
            Input.GetMouseButtonDown(0))
        {
            DismissPrologue();
        }
    }

    public void DismissPrologue()
    {
        if (isDismissed || isTransitioning) return;
        StartCoroutine(FadeOutAndBeginGame());
    }

    private IEnumerator PlayPrologueSequence()
    {
        isTransitioning = true;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Kunci pergerakan pemain selama prolog tampil
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Menu);
        }

        // Tampilkan kursor bersih
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        isTransitioning = false;
    }

    private IEnumerator FadeOutAndBeginGame()
    {
        isTransitioning = true;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        isDismissed = true;
        isTransitioning = false;

        // Alihkan state ke Eksplorasi aktif
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Exploration);
        }

        // Nonaktifkan canvas agar menghemat draw call
        gameObject.SetActive(false);
    }

    public void SetupUIHierarchyIfMissing()
    {
        // Cari font FragmentMono jika preferredFont belum diisi
        if (preferredFont == null)
        {
            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in allFonts)
            {
                if (f.name.Contains("FragmentMono") || f.name.Contains("Fragment"))
                {
                    preferredFont = f;
                    break;
                }
            }
        }

        // Pastikan Canvas Scaler diatur dengan baik (1920x1080)
        CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Cari atau buat Background Black Panel
        Transform panelT = transform.Find("Black Panel");
        GameObject panelObj;
        if (panelT != null)
        {
            panelObj = panelT.gameObject;
        }
        else
        {
            panelObj = new GameObject("Black Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(transform, false);
        }

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.color = Color.black;
        }

        // Content Container untuk memusatkan teks seperti gambar referensi
        Transform containerT = panelObj.transform.Find("ContentContainer");
        GameObject containerObj;
        if (containerT != null)
        {
            containerObj = containerT.gameObject;
        }
        else
        {
            containerObj = new GameObject("ContentContainer", typeof(RectTransform));
            containerObj.transform.SetParent(panelObj.transform, false);
        }

        RectTransform containerRect = containerObj.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.12f, 0.12f);
        containerRect.anchorMax = new Vector2(0.88f, 0.88f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // 1. Title Text (ABOUT THE GAME)
        if (titleTextComponent == null)
        {
            Transform existingTitle = containerObj.transform.Find("TitleText");
            if (existingTitle != null) titleTextComponent = existingTitle.GetComponent<TextMeshProUGUI>();
        }
        if (titleTextComponent == null)
        {
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(containerObj.transform, false);
            titleTextComponent = titleObj.GetComponent<TextMeshProUGUI>();
        }

        RectTransform titleRect = titleTextComponent.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(0, 0);
        titleRect.sizeDelta = new Vector2(0, 50);

        titleTextComponent.text = prologueTitle;
        titleTextComponent.fontSize = 28;
        titleTextComponent.fontStyle = FontStyles.Bold;
        titleTextComponent.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        titleTextComponent.characterSpacing = 2.5f;
        titleTextComponent.alignment = TextAlignmentOptions.TopLeft;
        if (preferredFont != null) titleTextComponent.font = preferredFont;

        // 2. Body Text
        if (bodyTextComponent == null)
        {
            Transform existingBody = containerObj.transform.Find("BodyText");
            if (existingBody != null) bodyTextComponent = existingBody.GetComponent<TextMeshProUGUI>();
        }
        if (bodyTextComponent == null)
        {
            // Cek apakah ada Text (TMP) yang dibuat user sebelumnya
            TextMeshProUGUI userTmp = panelObj.GetComponentInChildren<TextMeshProUGUI>();
            if (userTmp != null && userTmp != titleTextComponent)
            {
                bodyTextComponent = userTmp;
                bodyTextComponent.transform.SetParent(containerObj.transform, false);
            }
            else
            {
                GameObject bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
                bodyObj.transform.SetParent(containerObj.transform, false);
                bodyTextComponent = bodyObj.GetComponent<TextMeshProUGUI>();
            }
        }

        RectTransform bodyRect = bodyTextComponent.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0.15f);
        bodyRect.anchorMax = new Vector2(1f, 0.90f);
        bodyRect.pivot = new Vector2(0f, 1f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;

        bodyTextComponent.text = prologueBody;
        bodyTextComponent.fontSize = 21;
        bodyTextComponent.lineSpacing = 28f;
        bodyTextComponent.paragraphSpacing = 20f;
        bodyTextComponent.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        bodyTextComponent.alignment = TextAlignmentOptions.TopLeft;
        bodyTextComponent.enableWordWrapping = true;
        if (preferredFont != null) bodyTextComponent.font = preferredFont;

        // 3. Signature & Avatar Section (Bottom Right)
        Transform sigGroupT = containerObj.transform.Find("SignatureGroup");
        GameObject sigGroupObj;
        if (sigGroupT != null)
        {
            sigGroupObj = sigGroupT.gameObject;
        }
        else
        {
            sigGroupObj = new GameObject("SignatureGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            sigGroupObj.transform.SetParent(containerObj.transform, false);
        }

        RectTransform sigRect = sigGroupObj.GetComponent<RectTransform>();
        sigRect.anchorMin = new Vector2(1f, 0f);
        sigRect.anchorMax = new Vector2(1f, 0f);
        sigRect.pivot = new Vector2(1f, 0f);
        sigRect.anchoredPosition = new Vector2(0, 50);
        sigRect.sizeDelta = new Vector2(300, 48);

        HorizontalLayoutGroup hlg = sigGroupObj.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.spacing = 14;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        // Avatar Image
        if (avatarImageComponent == null)
        {
            Transform existingAvatar = sigGroupObj.transform.Find("Avatar");
            if (existingAvatar != null) avatarImageComponent = existingAvatar.GetComponent<Image>();
        }
        if (avatarImageComponent == null)
        {
            GameObject avatarObj = new GameObject("Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            avatarObj.transform.SetParent(sigGroupObj.transform, false);
            avatarImageComponent = avatarObj.GetComponent<Image>();
        }
        RectTransform avRect = avatarImageComponent.GetComponent<RectTransform>();
        avRect.sizeDelta = new Vector2(40, 40);

        if (avatarSprite != null)
        {
            avatarImageComponent.sprite = avatarSprite;
            avatarImageComponent.color = Color.white;
        }
        else
        {
            // Warna aksen badge jika avatar belum dipasang
            avatarImageComponent.color = new Color(0.2f, 0.6f, 0.9f, 0.8f);
        }

        // Author Name Text
        if (authorTextComponent == null)
        {
            Transform existingAuthor = sigGroupObj.transform.Find("AuthorName");
            if (existingAuthor != null) authorTextComponent = existingAuthor.GetComponent<TextMeshProUGUI>();
        }
        if (authorTextComponent == null)
        {
            GameObject authorObj = new GameObject("AuthorName", typeof(RectTransform), typeof(TextMeshProUGUI));
            authorObj.transform.SetParent(sigGroupObj.transform, false);
            authorTextComponent = authorObj.GetComponent<TextMeshProUGUI>();
        }
        RectTransform authorRect = authorTextComponent.GetComponent<RectTransform>();
        authorRect.sizeDelta = new Vector2(180, 40);

        authorTextComponent.text = authorSignature;
        authorTextComponent.fontSize = 20;
        authorTextComponent.fontStyle = FontStyles.Bold;
        authorTextComponent.color = Color.white;
        authorTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
        if (preferredFont != null) authorTextComponent.font = preferredFont;

        // 4. Prompt Text (Bottom Center)
        if (promptTextComponent == null)
        {
            Transform existingPrompt = containerObj.transform.Find("PromptText");
            if (existingPrompt != null) promptTextComponent = existingPrompt.GetComponent<TextMeshProUGUI>();
        }
        if (promptTextComponent == null)
        {
            GameObject promptObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            promptObj.transform.SetParent(containerObj.transform, false);
            promptTextComponent = promptObj.GetComponent<TextMeshProUGUI>();
        }

        RectTransform promptRect = promptTextComponent.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0f, 0f);
        promptRect.anchorMax = new Vector2(0.6f, 0f);
        promptRect.pivot = new Vector2(0f, 0f);
        promptRect.anchoredPosition = new Vector2(0, 50);
        promptRect.sizeDelta = new Vector2(450, 40);

        promptTextComponent.text = continuePrompt;
        promptTextComponent.fontSize = 17;
        promptTextComponent.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
        promptTextComponent.alignment = TextAlignmentOptions.MidlineLeft;
        if (preferredFont != null) promptTextComponent.font = preferredFont;
    }
}
