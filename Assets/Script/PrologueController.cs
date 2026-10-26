using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class PrologueController : MonoBehaviour, IPointerClickHandler
{
    private static PrologueController instance;
    public static PrologueController Instance => instance;

    [Header("1. Halaman Teks Prolog (Klik Bertahap Berganti Halaman)")]
    public string prologueTitle = "Prolog Cerita :";
    
    [Tooltip("Daftar halaman teks prolog cerita. Pemain akan mengklik mouse untuk membaca urutan halaman ini sampai selesai.")]
    [TextArea(4, 12)]
    public string[] prologuePages = new string[4]
    {
        // Narasi Pertama (Tampilan Awal)
        "Hutan hujan tak pernah benar-benar sunyi.\n\nAda jutaan bisikan kehidupan di balik lebatnya tajuk kanopi yang menahan terik dunia.",

        // Narasi Kedua (Klik Mouse Kedua)
        "Namun di puncak kemarau panjang tahun ini, keheningan asing merayap ke jantung Hutan Lindung Rano Towa.\n\nRatusan hektare pepohonan hilang tanpa jejak dalam hitungan minggu.",

        // Narasi Ketiga (Klik Mouse Ketiga)
        "Laporan satelit mendeteksi anomali tebangan liar di luar batas konsesi resmi. Jejak alat berat dan tunggul kayu bernomor fiktif tersembunyi di balik rawa gambut yang rapuh.",

        // Narasi Keempat (Klik Mouse Keempat - Terakhir)
        "Anda dikirim untuk mengumpulkan bukti materiil tak terbantahkan dan mengonfrontasi pihak pengelola sebelum kejahatan ini ditutup sepihak.\n\nKebenaran ada di antara pepohonan yang tersisa."
    };

    [Tooltip("Kosongkan jika tidak ingin menampilkan nama author pada cerita")]
    public string authorSignature = "";
    public string continuePrompt = "";
    public string finalPrompt = "";

    [Header("2. Mode Kustomisasi UI")]
    [Tooltip("Jika true, script akan otomatis mengisi teks di atas ke dalam komponen Text TMP Anda saat game dimulai.")]
    public bool overrideTextContent = true;

    [Tooltip("Jika true, script akan mengatur posisi RectTransform secara otomatis. Set FALSE jika Anda menata posisi UI sendiri.")]
    public bool autoGenerateLayout = false;

    [Header("3. Timing & Animasi")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.6f;
    public bool autoStartOnPlay = true;

    [Header("4. Referensi UI (CUKUP DRAG 1 OBJEK TEXT TMP ANDA KE SINI)")]
    [Tooltip("Tarik objek Text (TMP) Anda ke sini. Judul, isi cerita, dan tanda tangan akan langsung digabung secara rapi di 1 teks ini.")]
    public TextMeshProUGUI singlePrologueText;
    public CanvasGroup canvasGroup;
    public TMP_FontAsset preferredFont;

    [Header("5. Opsional (Hanya jika Anda memisahkan judul/prompt di objek berbeda)")]
    public TextMeshProUGUI separateTitleText;
    public TextMeshProUGUI separatePromptText;
    public Image avatarImage;
    public Sprite avatarSprite;

    private int currentPageIndex = 0;
    private float lastAdvanceTime = -1f;
    public float inputCooldown = 0.35f;
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

        // Pastikan EventSystem tersedia di scene
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Auto-find references dari child
        AutoDetectChildReferences();

        if (autoGenerateLayout)
        {
            SetupUIHierarchyIfMissing();
        }

        ApplyCurrentPageText();
    }

    private void Start()
    {
        currentPageIndex = 0;
        ApplyCurrentPageText();

        if (autoStartOnPlay && !isDismissed)
        {
            StartCoroutine(PlayPrologueSequence());
        }
    }

    private void Update()
    {
        if (isDismissed) return;

        // Pastikan kursor mouse selalu terlihat & tidak terkunci selama prolog tampil
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Jika user klik saat masih fade-in, langsung skip fade-in ke teks penuh
        if (isTransitioning)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                StopAllCoroutines();
                canvasGroup.alpha = 1f;
                isTransitioning = false;
                lastAdvanceTime = Time.unscaledTime;
            }
            return;
        }

        // Input untuk maju ke halaman berikutnya (Klik Kiri, Klik Kanan, Spasi, Enter)
        if (Input.GetMouseButtonDown(0) || 
            Input.GetMouseButtonDown(1) || 
            Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.E))
        {
            AdvanceToNextPage();
        }
    }

    // Mendeteksi klik langsung pada Canvas / UI Panel via EventSystem
    public void OnPointerClick(PointerEventData eventData)
    {
        AdvanceToNextPage();
    }

    public void AdvanceToNextPage()
    {
        if (isDismissed || isTransitioning) return;

        // Cegah eksekusi berulang kali dalam 1 klik (Debounce cooldown)
        if (Time.unscaledTime - lastAdvanceTime < inputCooldown) return;
        lastAdvanceTime = Time.unscaledTime;

        currentPageIndex++;
        Debug.Log($"[Prologue] Maju ke Halaman Indeks: {currentPageIndex + 1}");

        // Jika masih ada halaman berikutnya dalam daftar:
        if (prologuePages != null && currentPageIndex < prologuePages.Length)
        {
            ApplyCurrentPageText();
        }
        else
        {
            Debug.Log("[Prologue] Halaman terakhir selesai. Menutup prolog dan memulai game!");
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

        // Tampilkan kursor bersih dan bebas digerakkan
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Fade in cepat
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

        // Alihkan state ke Eksplorasi aktif & kunci kursor untuk gameplay first-person
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Exploration);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Nonaktifkan canvas agar menghemat draw call
        gameObject.SetActive(false);
    }

    private void AutoDetectChildReferences()
    {
        if (singlePrologueText == null)
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps != null && tmps.Length > 0)
            {
                singlePrologueText = tmps[0];
            }
        }

        if (avatarImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject != gameObject && !img.name.ToLower().Contains("panel"))
                {
                    avatarImage = img;
                    break;
                }
            }
        }
    }

    private void ApplyCurrentPageText()
    {
        if (prologuePages == null || prologuePages.Length == 0)
        {
            prologuePages = new string[4]
            {
                "Hutan hujan tak pernah benar-benar sunyi.\n\nAda jutaan bisikan kehidupan di balik lebatnya tajuk kanopi yang menahan terik dunia.",
                "Namun di puncak kemarau panjang tahun ini, keheningan asing merayap ke jantung Hutan Lindung Rano Towa.\n\nRatusan hektare pepohonan hilang tanpa jejak dalam hitungan minggu.",
                "Laporan satelit mendeteksi anomali tebangan liar di luar batas konsesi resmi. Jejak alat berat dan tunggul kayu bernomor fiktif tersembunyi di balik rawa gambut yang rapuh.",
                "Anda dikirim untuk mengumpulkan bukti materiil tak terbantahkan dan mengonfrontasi pihak pengelola sebelum kejahatan ini ditutup sepihak.\n\nKebenaran ada di antara pepohonan yang tersisa."
            };
        }

        if (singlePrologueText == null)
        {
            singlePrologueText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, prologuePages.Length - 1);
        string currentBody = prologuePages[currentPageIndex];
        bool isLastPage = (currentPageIndex >= prologuePages.Length - 1);
        string activePrompt = isLastPage ? finalPrompt : continuePrompt;

        // 1. Jika pengguna memakai 1 TextMeshPro untuk semuanya:
        if (singlePrologueText != null)
        {
            if (preferredFont != null) singlePrologueText.font = preferredFont;

            string titleFormatted = string.IsNullOrEmpty(prologueTitle) ? "" : $"{prologueTitle}\n\n";
            string bodyFormatted = $"{currentBody}";
            
            // Tanda tangan author (jika diisi)
            string authorFormatted = (isLastPage && !string.IsNullOrEmpty(authorSignature))
                ? $"\n\n\n<align=right>{authorSignature}</align>"
                : "";

            string promptFormatted = (separatePromptText == null && !string.IsNullOrEmpty(activePrompt)) 
                ? $"\n\n\n<align=center><size=75%><color=#999999>{activePrompt}</color></size></align>" 
                : "";

            singlePrologueText.text = titleFormatted + bodyFormatted + authorFormatted + promptFormatted;
            Debug.Log($"[Prologue] Text diperbarui ke Halaman {currentPageIndex + 1}/{prologuePages.Length}: {singlePrologueText.text.Substring(0, Mathf.Min(35, singlePrologueText.text.Length))}...");
        }

        // 2. Jika ada teks terpisah untuk judul
        if (separateTitleText != null && !string.IsNullOrEmpty(prologueTitle))
        {
            separateTitleText.text = prologueTitle;
            if (preferredFont != null) separateTitleText.font = preferredFont;
        }

        // 3. Jika ada teks terpisah untuk prompt
        if (separatePromptText != null && !string.IsNullOrEmpty(activePrompt))
        {
            separatePromptText.text = activePrompt;
            if (preferredFont != null) separatePromptText.font = preferredFont;
        }

        // 4. Avatar gambar jika ada
        if (avatarImage != null && avatarSprite != null)
        {
            avatarImage.gameObject.SetActive(isLastPage);
            avatarImage.sprite = avatarSprite;
        }
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

        // Single Unified Prologue Text (Judul + Isi + Signature dalam 1 TextMeshPro)
        if (singlePrologueText == null)
        {
            Transform existingText = panelObj.transform.Find("Text (TMP)");
            if (existingText != null) singlePrologueText = existingText.GetComponent<TextMeshProUGUI>();
        }

        if (singlePrologueText == null)
        {
            Transform existingBody = containerObj.transform.Find("PrologueText");
            if (existingBody != null) singlePrologueText = existingBody.GetComponent<TextMeshProUGUI>();
        }

        if (singlePrologueText == null)
        {
            GameObject textObj = new GameObject("PrologueText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(containerObj.transform, false);
            singlePrologueText = textObj.GetComponent<TextMeshProUGUI>();
        }

        RectTransform textRect = singlePrologueText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        singlePrologueText.fontSize = 22;
        singlePrologueText.lineSpacing = 26f;
        singlePrologueText.paragraphSpacing = 22f;
        singlePrologueText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        singlePrologueText.alignment = TextAlignmentOptions.TopLeft;
        singlePrologueText.enableWordWrapping = true;
        if (preferredFont != null) singlePrologueText.font = preferredFont;

        ApplyCurrentPageText();
    }
}
