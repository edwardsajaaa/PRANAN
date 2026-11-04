using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class PrologueController : MonoBehaviour
{
    public static PrologueController Instance { get; private set; }
    public static event System.Action OnPrologueFinished;
    public static bool IsPrologueRunning => Instance != null && Instance.gameObject.activeInHierarchy && !Instance.isEnding;

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject blackPanel;
    [SerializeField] private GameObject titleObject;
    [SerializeField] private GameObject gameTitleObject;
    [SerializeField] private GameObject gameSubtitleObject;
    [SerializeField] private GameObject[] narrativeObjects;
    [SerializeField] private TextMeshProUGUI singlePrologueText;
    [SerializeField] private GameObject playerCanvas;

    [Header("Timing - Narasi & Typewriter")]
    [SerializeField] private float initialBlackDelay = 2.0f;
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float fastTypingSpeed = 0.007f;
    [SerializeField] private float textFadeOutDuration = 0.4f;
    [SerializeField] private float inputCooldown = 0.25f;

    [Header("Timing - Game Title Card (Cinematic)")]
    [SerializeField] private float postNarrationBlackDelay = 2.0f;
    [SerializeField] private float gameTitleFadeInDuration = 1.8f;
    [SerializeField] private float gameSubtitleDelay = 0.5f;
    [SerializeField] private float gameSubtitleFadeInDuration = 1.5f;
    [SerializeField] private float gameTitleDisplayDuration = 2.5f;
    [SerializeField] private float gameTitleFadeOutDuration = 1.2f;
    [SerializeField] private float postTitleBlackDelay = 3.0f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("Typewriter & Audio")]
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private bool lockTextAlignmentToTop = true;
    [SerializeField] private AudioClip typingSound;
    [SerializeField] private AudioSource audioSource;

    private int currentPageIndex;
    private float lastInputTime;
    private int lastInputFrame = -1;
    private bool isWaitingDelay;
    private bool isTyping;
    private bool isPageTransitioning;
    private bool isEnding;
    private Coroutine typingRoutine;
    private TMP_Text currentTypingText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (audioSource == null && typingSound != null)
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        ResolveReferences();
        HideContent();
    }

    private void Start()
    {
        StartCoroutine(PrologueSequence());
    }

    private void Update()
    {
        if (isEnding || isPageTransitioning) return;

        EnsureCursorVisible();

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || 
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E))
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        if (isEnding || isPageTransitioning) return;

        // Cegah eksekusi ganda dalam frame yang sama
        if (Time.frameCount == lastInputFrame) return;
        lastInputFrame = Time.frameCount;

        // Cooldown debouncing antara aksi klik pemain
        if (Time.unscaledTime - lastInputTime < inputCooldown) return;

        if (isWaitingDelay)
        {
            StopAllCoroutines();
            isWaitingDelay = false;
            lastInputTime = Time.unscaledTime;
            ShowContent();
            return;
        }

        // Jika teks sedang diketik, abaikan klik instan (pemain harus menahan klik untuk mempercepat)
        if (isTyping) return;

        lastInputTime = Time.unscaledTime;
        NextPage();
    }

    private IEnumerator PrologueSequence()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.SetState(GameState.Menu);

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (initialBlackDelay > 0f)
        {
            isWaitingDelay = true;
            yield return new WaitForSecondsRealtime(initialBlackDelay);
            isWaitingDelay = false;
        }

        ShowContent();
    }

    private void ShowContent()
    {
        if (titleObject != null)
        {
            SetObjectAlpha(titleObject, 1f);
            titleObject.SetActive(true);
        }

        ShowPage(0);
    }

    private void ShowPage(int index)
    {
        currentPageIndex = index;

        if (narrativeObjects != null && narrativeObjects.Length > 0)
        {
            for (int i = 0; i < narrativeObjects.Length; i++)
            {
                if (narrativeObjects[i] == null) continue;
                bool isCurrent = (i == index);

                if (isCurrent)
                {
                    SetObjectAlpha(narrativeObjects[i], 1f);

                    TMP_Text tmp = narrativeObjects[i].GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null)
                    {
                        tmp.maxVisibleCharacters = 0;
                    }

                    narrativeObjects[i].SetActive(true);

                    if (useTypewriterEffect && tmp != null)
                    {
                        StartTypewriter(tmp);
                    }
                }
                else
                {
                    narrativeObjects[i].SetActive(false);
                }
            }
        }
        else if (singlePrologueText != null)
        {
            SetObjectAlpha(singlePrologueText.gameObject, 1f);
            singlePrologueText.maxVisibleCharacters = 0;
            singlePrologueText.gameObject.SetActive(true);
            if (useTypewriterEffect)
                StartTypewriter(singlePrologueText);
        }
    }

    private void NextPage()
    {
        if (isEnding || isPageTransitioning) return;
        StartCoroutine(TransitionToNextPage());
    }

    private IEnumerator TransitionToNextPage()
    {
        isPageTransitioning = true;
        StopTypewriter();

        // Cari objek narasi saat ini
        GameObject currentPageObj = null;
        if (narrativeObjects != null && currentPageIndex >= 0 && currentPageIndex < narrativeObjects.Length)
        {
            currentPageObj = narrativeObjects[currentPageIndex];
        }
        else if (singlePrologueText != null)
        {
            currentPageObj = singlePrologueText.gameObject;
        }

        int totalPages = narrativeObjects != null && narrativeObjects.Length > 0 ? narrativeObjects.Length : 1;
        bool isLastPage = (currentPageIndex + 1 >= totalPages);

        // Animasi fade out untuk teks narasi (dan judul prolog jika halaman terakhir)
        if (currentPageObj != null && currentPageObj.activeSelf)
        {
            if (isLastPage && titleObject != null && titleObject.activeSelf)
            {
                StartCoroutine(FadeOutObjectRoutine(titleObject, textFadeOutDuration));
            }

            yield return StartCoroutine(FadeOutObjectRoutine(currentPageObj, textFadeOutDuration));
        }

        if (!isLastPage)
        {
            isPageTransitioning = false;
            ShowPage(currentPageIndex + 1);
        }
        else
        {
            // Selesai seluruh narasi! Jalankan sekuens sinematik Title & Subtitle Game
            StartCoroutine(GameTitleSequence());
        }
    }

    private IEnumerator GameTitleSequence()
    {
        isEnding = true; // Kunci input player, seluruh proses title card berjalan otomatis
        isPageTransitioning = false;

        // 1. Setelah narasi selesai, berikan jeda 2 detik di layar hitam
        if (postNarrationBlackDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(postNarrationBlackDelay);
        }

        // 2. Fade in secara pelan pada Title Game
        if (gameTitleObject != null)
        {
            yield return StartCoroutine(FadeInObjectRoutine(gameTitleObject, gameTitleFadeInDuration));
        }

        // 3. Jeda sebelum subtitle menyusul
        if (gameSubtitleDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(gameSubtitleDelay);
        }

        // 4. Subtitle Game menyusul fade in
        if (gameSubtitleObject != null)
        {
            yield return StartCoroutine(FadeInObjectRoutine(gameSubtitleObject, gameSubtitleFadeInDuration));
        }

        // 5. Tampilkan kedua judul dan subtitle selama beberapa detik
        if (gameTitleDisplayDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(gameTitleDisplayDuration);
        }

        // 6. Fade out Title Game & Subtitle Game bersamaan
        Coroutine fadeTitle = null;
        Coroutine fadeSubtitle = null;

        if (gameTitleObject != null && gameTitleObject.activeSelf)
            fadeTitle = StartCoroutine(FadeOutObjectRoutine(gameTitleObject, gameTitleFadeOutDuration));

        if (gameSubtitleObject != null && gameSubtitleObject.activeSelf)
            fadeSubtitle = StartCoroutine(FadeOutObjectRoutine(gameSubtitleObject, gameTitleFadeOutDuration));

        if (fadeTitle != null) yield return fadeTitle;
        if (fadeSubtitle != null) yield return fadeSubtitle;

        // 7. Jeda black screen lumayan lama (3 detik)
        if (postTitleBlackDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(postTitleBlackDelay);
        }

        // 8. Fade out black screen ke gameplay
        yield return StartCoroutine(FadeOutAndEnd());
    }

    private IEnumerator FadeInObjectRoutine(GameObject targetObj, float duration)
    {
        if (targetObj == null) yield break;

        SetObjectAlpha(targetObj, 0f);
        targetObj.SetActive(true);

        CanvasGroup cg = targetObj.GetComponent<CanvasGroup>();
        TMP_Text[] texts = targetObj.GetComponentsInChildren<TMP_Text>(true);

        // Jangan gunakan animasi ketik untuk title / subtitle
        if (texts != null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].maxVisibleCharacters = int.MaxValue;
                    texts[i].ForceMeshUpdate();
                }
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(0f, 1f, t);

            if (cg != null)
            {
                cg.alpha = a;
            }
            else if (texts != null)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null)
                    {
                        Color c = texts[i].color;
                        c.a = a;
                        texts[i].color = c;
                    }
                }
            }

            yield return null;
        }

        SetObjectAlpha(targetObj, 1f);
    }

    private IEnumerator FadeOutObjectRoutine(GameObject targetObj, float duration)
    {
        if (targetObj == null || !targetObj.activeSelf) yield break;

        CanvasGroup cg = targetObj.GetComponent<CanvasGroup>();
        TMP_Text[] texts = cg == null ? targetObj.GetComponentsInChildren<TMP_Text>(true) : null;

        float startAlpha = cg != null ? cg.alpha : (texts != null && texts.Length > 0 && texts[0] != null ? texts[0].color.a : 1f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(startAlpha, 0f, t);

            if (cg != null)
            {
                cg.alpha = a;
            }
            else if (texts != null)
            {
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null)
                    {
                        Color c = texts[i].color;
                        c.a = a;
                        texts[i].color = c;
                    }
                }
            }

            yield return null;
        }

        SetObjectAlpha(targetObj, 0f);
        targetObj.SetActive(false);
        SetObjectAlpha(targetObj, 1f);
    }

    private void SetObjectAlpha(GameObject targetObj, float alpha)
    {
        if (targetObj == null) return;

        CanvasGroup cg = targetObj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = alpha;
        }

        TMP_Text[] texts = targetObj.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
            {
                Color c = texts[i].color;
                c.a = alpha;
                texts[i].color = c;
            }
        }
    }

    private void StartTypewriter(TMP_Text textComponent)
    {
        StopTypewriter();
        if (lockTextAlignmentToTop && textComponent != null)
        {
            textComponent.verticalAlignment = VerticalAlignmentOptions.Top;
        }
        typingRoutine = StartCoroutine(TypewriterRoutine(textComponent));
    }

    private IEnumerator TypewriterRoutine(TMP_Text textComponent)
    {
        isTyping = true;
        currentTypingText = textComponent;

        textComponent.maxVisibleCharacters = 0;
        textComponent.ForceMeshUpdate();

        int totalChars = textComponent.textInfo.characterCount;
        if (totalChars == 0 && !string.IsNullOrEmpty(textComponent.text))
        {
            yield return null;
            textComponent.ForceMeshUpdate();
            totalChars = textComponent.textInfo.characterCount;
        }

        for (int i = 1; i <= totalChars; i++)
        {
            textComponent.maxVisibleCharacters = i;

            if (typingSound != null && audioSource != null && i % 2 == 0)
                audioSource.PlayOneShot(typingSound, 0.35f);

            bool isHoldingSpeedUp = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
            float currentDelay = isHoldingSpeedUp ? fastTypingSpeed : typingSpeed;

            yield return new WaitForSecondsRealtime(currentDelay);
        }

        CompleteTyping();
    }

    private void CompleteTyping()
    {
        StopTypewriter();
        if (currentTypingText != null)
        {
            currentTypingText.maxVisibleCharacters = int.MaxValue;
            currentTypingText = null;
        }
        isTyping = false;
        lastInputTime = Time.unscaledTime;
    }

    private void StopTypewriter()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
        isTyping = false;
    }

    private IEnumerator FadeOutAndEnd()
    {
        isEnding = true;
        StopTypewriter();

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Exploration);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerCanvas != null)
        {
            playerCanvas.SetActive(true);
        }

        OnPrologueFinished?.Invoke();

        gameObject.SetActive(false);
    }

    private void HideContent()
    {
        if (blackPanel != null) blackPanel.SetActive(true);

        if (titleObject != null)
        {
            SetObjectAlpha(titleObject, 1f);
            titleObject.SetActive(false);
        }

        if (gameTitleObject != null)
        {
            SetObjectAlpha(gameTitleObject, 0f);
            gameTitleObject.SetActive(false);
        }

        if (gameSubtitleObject != null)
        {
            SetObjectAlpha(gameSubtitleObject, 0f);
            gameSubtitleObject.SetActive(false);
        }

        if (playerCanvas != null) playerCanvas.SetActive(false);

        if (narrativeObjects != null)
        {
            foreach (var page in narrativeObjects)
            {
                if (page != null)
                {
                    SetObjectAlpha(page, 1f);
                    page.SetActive(false);
                }
            }
        }

        if (singlePrologueText != null)
        {
            SetObjectAlpha(singlePrologueText.gameObject, 1f);
            singlePrologueText.gameObject.SetActive(false);
        }
    }

    private void ResolveReferences()
    {
        if (blackPanel == null)
        {
            Transform panel = transform.Find("Black Panel");
            if (panel != null) blackPanel = panel.gameObject;
        }

        Transform searchRoot = blackPanel != null ? blackPanel.transform : transform;

        // Header teks prolog (misal "Prolog", "Prolog Cerita :")
        if (titleObject == null)
        {
            Transform t = searchRoot.Find("Prolog") ?? searchRoot.Find("Title");
            if (t != null) titleObject = t.gameObject;
        }

        // Title Game (Judul game utama, misal "Title Game")
        if (gameTitleObject == null)
        {
            Transform gt = searchRoot.Find("Title Game") ?? searchRoot.Find("Game Title");
            if (gt != null) gameTitleObject = gt.gameObject;
        }

        // Subtitle Game (Subjudul game, misal "Subtitle Game")
        if (gameSubtitleObject == null)
        {
            Transform gs = searchRoot.Find("Subtitle Game") ?? searchRoot.Find("Game Subtitle") ?? searchRoot.Find("Subtitle");
            if (gs != null) gameSubtitleObject = gs.gameObject;
        }

        if (narrativeObjects == null || narrativeObjects.Length == 0)
        {
            List<GameObject> pages = new List<GameObject>();
            for (int i = 0; i < searchRoot.childCount; i++)
            {
                Transform child = searchRoot.GetChild(i);
                string childName = child.name.ToLower();
                // Filter hanya objek narasi murni, jangan sertakan Title Game, Subtitle Game, ataupun header Prolog
                if (childName.StartsWith("narasi") && 
                    child.gameObject != gameTitleObject && 
                    child.gameObject != gameSubtitleObject && 
                    child.gameObject != titleObject)
                {
                    pages.Add(child.gameObject);
                }
            }

            if (pages.Count > 0)
                narrativeObjects = pages.ToArray();
        }

        if (playerCanvas == null)
        {
            GameObject player = GameObject.Find("Player") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                Canvas[] canvases = player.GetComponentsInChildren<Canvas>(true);
                if (canvases.Length > 0)
                {
                    playerCanvas = canvases[0].gameObject;
                }
            }
        }
    }

    private void EnsureCursorVisible()
    {
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
