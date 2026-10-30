using System.Collections;
using System.Collections.Generic;
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
        "Hutan hujan tak pernah benar-benar sunyi.\n\nAda jutaan bisikan kehidupan di balik lebatnya tajuk kanopi yang menahan terik dunia.",

        "Namun di puncak kemarau panjang tahun ini, keheningan asing merayap ke jantung Hutan Lindung Rano Towa.\n\nRatusan hektare pepohonan hilang tanpa jejak dalam hitungan minggu.",

        "Laporan satelit mendeteksi anomali tebangan liar di luar batas konsesi resmi. Jejak alat berat dan tunggul kayu bernomor fiktif tersembunyi di balik rawa gambut yang rapuh.",

        "Anda dikirim untuk mengumpulkan bukti materiil tak terbantahkan dan mengonfrontasi pihak pengelola sebelum kejahatan ini ditutup sepihak.\n\nKebenaran ada di antara pepohonan yang tersisa."
    };

    [Header("Opsi Mode Objek Terpisah (Jika Anda membuat objek Narasi, Narasi (1), dst)")]
    [Tooltip("Jika Anda membuat objek terpisah di Hierarchy (seperti Narasi, Narasi (1), dst), script otomatis mendeteksinya atau Anda bisa drag ke sini. Script akan menyalakan/mematikan objek satu per satu setiap klik!")]
    public GameObject[] narrativeObjects;

    [Tooltip("Kosongkan jika tidak ingin menampilkan nama author pada cerita")]
    public string authorSignature = "";
    public string continuePrompt = "";
    public string finalPrompt = "";

    [Header("2. Mode Kustomisasi UI")]
    [Tooltip("Jika true, script akan otomatis mengisi teks di atas ke dalam komponen Text TMP Anda saat game dimulai.")]
    public bool overrideTextContent = true;

    [Tooltip("Jika true, script akan mengatur posisi RectTransform secara otomatis. Set FALSE jika Anda menata posisi UI sendiri.")]
    public bool autoGenerateLayout = false;

    [Header("Timing Layar Hitam (Delay Awal)")]
    [Tooltip("Objek Black Panel (Layar Hitam). Jika kosong, otomatis mencari child bernama 'Black Panel'.")]
    public GameObject blackPanel;

    [Tooltip("Objek Judul Prolog. Jika kosong, otomatis mencari child bernama 'Prolog' atau 'Title'.")]
    public GameObject titleObject;

    [Tooltip("Durasi jeda (dalam detik) layar hitam polos muncul terlebih dahulu sebelum judul Prolog dan teks Narasi diaktifkan.")]
    public float initialBlackDelay = 2.0f;

    [Tooltip("Jika true, pemain dapat mengklik mouse untuk langsung memunculkan teks tanpa harus menunggu jeda detik selesai.")]
    public bool allowSkipDelayWithClick = true;

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

    [Header("6. Efek Ketik (Typewriter Transition)")]
    [Tooltip("Aktifkan animasi efek ketik (Typewriter) saat kata-kata prolog muncul.")]
    public bool useTypewriterEffect = true;

    [Tooltip("Kecepatan ketik (detik per karakter). Contoh: 0.035 = ~28 karakter per detik.")]
    public float typingSpeed = 0.035f;

    [Tooltip("Audio klip suara ketik opsional (misal suara 'tik' tuts mesin ketik). Kosongkan jika tidak perlu suara.")]
    public AudioClip typingSound;

    [Tooltip("AudioSource untuk memutar suara ketik. Jika kosong, script otomatis mencarinya.")]
    public AudioSource audioSource;

    private int currentPageIndex = 0;
    private float lastAdvanceTime = -1f;
    public float inputCooldown = 0.35f;
    private bool isTransitioning = false;
    private bool isWaitingInitialDelay = false;
    private bool isDismissed = false;

    // State untuk Typewriter
    private Coroutine typewriterCoroutine = null;
    private bool isTyping = false;
    private TextMeshProUGUI activeTypingTmp = null;
    private Text activeTypingLegacyText = null;
    private string activeOriginalLegacyText = "";

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

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && typingSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        AutoDetectChildReferences();

        if (autoGenerateLayout)
        {
            SetupUIHierarchyIfMissing();
        }

        // Sembunyikan semua teks/narasi di awal agar layar hitam polos tampil duluan
        HideAllPrologueContent();
    }

    private void Start()
    {
        currentPageIndex = 0;

        if (autoStartOnPlay && !isDismissed)
        {
            StartCoroutine(PlayPrologueSequence());
        }
    }

    private void Update()
    {
        if (isDismissed) return;

        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // 1. Jika sedang dalam jeda layar hitam polos (sebelum judul & narasi muncul)
        if (isWaitingInitialDelay)
        {
            if (allowSkipDelayWithClick && (
                Input.GetMouseButtonDown(0) || 
                Input.GetMouseButtonDown(1) || 
                Input.GetKeyDown(KeyCode.Space) || 
                Input.GetKeyDown(KeyCode.Return) || 
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.E)))
            {
                SkipInitialDelay();
            }
            return;
        }

        // 2. Jika efek ketik sedang berjalan dan pemain mengklik / tekan tombol: langsung selesaikan ketikan kalimat
        if (isTyping)
        {
            if (Input.GetMouseButtonDown(0) || 
                Input.GetMouseButtonDown(1) || 
                Input.GetKeyDown(KeyCode.Space) || 
                Input.GetKeyDown(KeyCode.Return) || 
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.E))
            {
                CompleteTypewriter();
            }
            return;
        }

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

        // 3. Input untuk maju ke halaman berikutnya
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isWaitingInitialDelay)
        {
            if (allowSkipDelayWithClick) SkipInitialDelay();
            return;
        }
        if (isTyping)
        {
            CompleteTypewriter();
            return;
        }
        AdvanceToNextPage();
    }

    public void SkipInitialDelay()
    {
        if (!isWaitingInitialDelay) return;
        Debug.Log("[Prologue] Pemain mengklik; langsung memunculkan judul Prolog dan narasi pertama!");
        ShowInitialPrologueContent();
    }

    private void HideAllPrologueContent()
    {
        StopTypewriter();

        if (titleObject != null)
        {
            titleObject.SetActive(false);
        }
        if (narrativeObjects != null)
        {
            foreach (var obj in narrativeObjects)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
        if (singlePrologueText != null && (narrativeObjects == null || narrativeObjects.Length == 0))
        {
            singlePrologueText.gameObject.SetActive(false);
        }
        if (avatarImage != null)
        {
            avatarImage.gameObject.SetActive(false);
        }
    }

    private void ShowInitialPrologueContent()
    {
        isWaitingInitialDelay = false;
        
        if (titleObject != null)
        {
            titleObject.SetActive(true);
        }
        if (singlePrologueText != null && (narrativeObjects == null || narrativeObjects.Length == 0))
        {
            singlePrologueText.gameObject.SetActive(true);
        }

        ApplyCurrentPageText();
        lastAdvanceTime = Time.unscaledTime;
    }

    public void AdvanceToNextPage()
    {
        if (isDismissed || isTransitioning || isWaitingInitialDelay) return;

        if (isTyping)
        {
            CompleteTypewriter();
            return;
        }

        if (Time.unscaledTime - lastAdvanceTime < inputCooldown) return;
        lastAdvanceTime = Time.unscaledTime;

        currentPageIndex++;

        int totalPages = (narrativeObjects != null && narrativeObjects.Length > 0) 
            ? narrativeObjects.Length 
            : (prologuePages != null ? prologuePages.Length : 1);

        Debug.Log($"[Prologue] Maju ke Halaman: {currentPageIndex + 1}/{totalPages}");

        if (currentPageIndex < totalPages)
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
        StopTypewriter();
        StartCoroutine(FadeOutAndBeginGame());
    }

    #region Typewriter Methods
    public void StartTypewriter(GameObject targetObj)
    {
        if (targetObj == null) return;
        StopTypewriter();
        if (!useTypewriterEffect) return;

        TextMeshProUGUI tmp = targetObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            typewriterCoroutine = StartCoroutine(TypewriterTMPRoutine(tmp));
            return;
        }

        Text legacy = targetObj.GetComponentInChildren<Text>(true);
        if (legacy != null)
        {
            typewriterCoroutine = StartCoroutine(TypewriterLegacyRoutine(legacy));
        }
    }

    public void StartTypewriter(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        StopTypewriter();
        if (!useTypewriterEffect) return;
        typewriterCoroutine = StartCoroutine(TypewriterTMPRoutine(tmp));
    }

    private IEnumerator TypewriterTMPRoutine(TextMeshProUGUI tmp)
    {
        isTyping = true;
        activeTypingTmp = tmp;

        tmp.ForceMeshUpdate();
        int totalChars = tmp.textInfo.characterCount;
        if (totalChars == 0 && !string.IsNullOrEmpty(tmp.text))
        {
            yield return null;
            tmp.ForceMeshUpdate();
            totalChars = tmp.textInfo.characterCount;
        }

        tmp.maxVisibleCharacters = 0;

        for (int i = 1; i <= totalChars; i++)
        {
            tmp.maxVisibleCharacters = i;

            if (typingSound != null && audioSource != null && (i % 2 == 0))
            {
                audioSource.PlayOneShot(typingSound, 0.4f);
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        tmp.maxVisibleCharacters = 99999;
        isTyping = false;
        activeTypingTmp = null;
        typewriterCoroutine = null;
    }

    private IEnumerator TypewriterLegacyRoutine(Text legacy)
    {
        isTyping = true;
        activeTypingLegacyText = legacy;
        activeOriginalLegacyText = legacy.text;

        int totalChars = activeOriginalLegacyText.Length;
        legacy.text = "";

        for (int i = 1; i <= totalChars; i++)
        {
            legacy.text = activeOriginalLegacyText.Substring(0, i);

            if (typingSound != null && audioSource != null && (i % 2 == 0))
            {
                audioSource.PlayOneShot(typingSound, 0.4f);
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        legacy.text = activeOriginalLegacyText;
        isTyping = false;
        activeTypingLegacyText = null;
        typewriterCoroutine = null;
    }

    public void CompleteTypewriter()
    {
        if (!isTyping) return;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (activeTypingTmp != null)
        {
            activeTypingTmp.maxVisibleCharacters = 99999;
            activeTypingTmp = null;
        }

        if (activeTypingLegacyText != null)
        {
            activeTypingLegacyText.text = activeOriginalLegacyText;
            activeTypingLegacyText = null;
        }

        isTyping = false;
        lastAdvanceTime = Time.unscaledTime;
    }

    public void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (activeTypingTmp != null)
        {
            activeTypingTmp.maxVisibleCharacters = 99999;
            activeTypingTmp = null;
        }

        if (activeTypingLegacyText != null)
        {
            activeTypingLegacyText.text = activeOriginalLegacyText;
            activeTypingLegacyText = null;
        }

        isTyping = false;
    }
    #endregion

    private IEnumerator PlayPrologueSequence()
    {
        // 1. Pastikan background panel hitam aktif
        if (blackPanel != null) blackPanel.SetActive(true);

        // 2. Sembunyikan semua teks/narasi agar layar hitam polos tampil duluan
        HideAllPrologueContent();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Menu);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 3. Jeda Layar Hitam Polos selama beberapa detik sesuai pengaturan
        if (initialBlackDelay > 0f)
        {
            isWaitingInitialDelay = true;
            float timer = 0f;
            while (timer < initialBlackDelay && isWaitingInitialDelay)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 4. Setelah selang waktu jeda selesai, aktifkan objek Prolog & Narasi pertama
        if (isWaitingInitialDelay || initialBlackDelay <= 0f)
        {
            ShowInitialPrologueContent();
        }
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

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Exploration);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        gameObject.SetActive(false);
    }

    private void AutoDetectChildReferences()
    {
        if (blackPanel == null)
        {
            Transform panel = transform.Find("Black Panel");
            if (panel != null) blackPanel = panel.gameObject;
        }

        Transform searchRoot = blackPanel != null ? blackPanel.transform : transform;

        if (titleObject == null)
        {
            Transform titleT = searchRoot.Find("Prolog");
            if (titleT == null) titleT = searchRoot.Find("Title");
            if (titleT == null)
            {
                for (int i = 0; i < searchRoot.childCount; i++)
                {
                    Transform c = searchRoot.GetChild(i);
                    string lower = c.name.ToLower();
                    if (lower.Contains("prolog") || lower.Contains("title") || lower.Contains("judul"))
                    {
                        titleT = c;
                        break;
                    }
                }
            }
            if (titleT != null)
            {
                titleObject = titleT.gameObject;
                Debug.Log($"[Prologue] Otomatis mendeteksi objek judul '{titleObject.name}'!");
            }
        }

        if (narrativeObjects == null || narrativeObjects.Length == 0)
        {
            List<GameObject> narasiList = new List<GameObject>();

            for (int i = 0; i < searchRoot.childCount; i++)
            {
                Transform child = searchRoot.GetChild(i);
                string lower = child.name.ToLower();
                if (lower.StartsWith("narasi"))
                {
                    narasiList.Add(child.gameObject);
                }
            }

            if (narasiList.Count > 1)
            {
                narrativeObjects = narasiList.ToArray();
                Debug.Log($"[Prologue] Otomatis mendeteksi {narrativeObjects.Length} objek narasi terpisah di Hierarchy!");
            }
        }

        if (singlePrologueText == null)
        {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                string lower = t.name.ToLower();
                if (lower.Contains("narasi") || lower.Contains("body") || lower.Contains("content"))
                {
                    singlePrologueText = t;
                    break;
                }
            }

            if (singlePrologueText == null && tmps != null && tmps.Length > 0)
            {
                foreach (var t in tmps)
                {
                    if (!t.name.ToLower().Contains("title") && !t.name.ToLower().Contains("prolog"))
                    {
                        singlePrologueText = t;
                        break;
                    }
                }
                if (singlePrologueText == null) singlePrologueText = tmps[0];
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
        if (narrativeObjects != null && narrativeObjects.Length > 0)
        {
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, narrativeObjects.Length - 1);
            for (int i = 0; i < narrativeObjects.Length; i++)
            {
                if (narrativeObjects[i] != null)
                {
                    bool isActive = (i == currentPageIndex);
                    narrativeObjects[i].SetActive(isActive);
                }
            }
            Debug.Log($"[Prologue] Mode Objek Terpisah: Menampilkan '{narrativeObjects[currentPageIndex]?.name}' (Aktif). Objek lainnya dimatikan.");

            if (useTypewriterEffect && narrativeObjects[currentPageIndex] != null)
            {
                StartTypewriter(narrativeObjects[currentPageIndex]);
            }
            return;
        }

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
            AutoDetectChildReferences();
        }

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, prologuePages.Length - 1);
        string currentBody = prologuePages[currentPageIndex];
        bool isLastPage = (currentPageIndex >= prologuePages.Length - 1);
        string activePrompt = isLastPage ? finalPrompt : continuePrompt;

        if (singlePrologueText != null)
        {
            if (preferredFont != null) singlePrologueText.font = preferredFont;

            string titleFormatted = string.IsNullOrEmpty(prologueTitle) ? "" : $"{prologueTitle}\n\n";
            string bodyFormatted = $"{currentBody}";
            
            string authorFormatted = (isLastPage && !string.IsNullOrEmpty(authorSignature))
                ? $"\n\n\n<align=right>{authorSignature}</align>"
                : "";

            string promptFormatted = (separatePromptText == null && !string.IsNullOrEmpty(activePrompt)) 
                ? $"\n\n\n<align=center><size=75%><color=#999999>{activePrompt}</color></size></align>" 
                : "";

            singlePrologueText.text = titleFormatted + bodyFormatted + authorFormatted + promptFormatted;
            Debug.Log($"[Prologue] Mode 1 Text: Mengubah isi teks pada '{singlePrologueText.name}' ke Halaman {currentPageIndex + 1}/{prologuePages.Length}");

            if (useTypewriterEffect)
            {
                StartTypewriter(singlePrologueText);
            }
        }

        if (separateTitleText != null && !string.IsNullOrEmpty(prologueTitle))
        {
            separateTitleText.text = prologueTitle;
            if (preferredFont != null) separateTitleText.font = preferredFont;
        }

        if (separatePromptText != null && !string.IsNullOrEmpty(activePrompt))
        {
            separatePromptText.text = activePrompt;
            if (preferredFont != null) separatePromptText.font = preferredFont;
        }

        if (avatarImage != null && avatarSprite != null)
        {
            avatarImage.gameObject.SetActive(isLastPage);
            avatarImage.sprite = avatarSprite;
        }
    }

    public void SetupUIHierarchyIfMissing()
    {
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

        CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

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
