using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class PrologueController : MonoBehaviour
{
    public static PrologueController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject blackPanel;
    [SerializeField] private GameObject titleObject;
    [SerializeField] private GameObject[] narrativeObjects;
    [SerializeField] private TextMeshProUGUI singlePrologueText;

    [Header("Timing & Speed")]
    [SerializeField] private float initialBlackDelay = 2.0f;
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float fadeOutDuration = 0.6f;
    [SerializeField] private float inputCooldown = 0.25f;

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
        if (isEnding) return;

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
        if (isEnding) return;

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

        if (isTyping)
        {
            CompleteTyping();
            return;
        }

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
            titleObject.SetActive(true);

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
            singlePrologueText.maxVisibleCharacters = 0;
            singlePrologueText.gameObject.SetActive(true);
            if (useTypewriterEffect)
                StartTypewriter(singlePrologueText);
        }
    }

    private void NextPage()
    {
        int totalPages = narrativeObjects != null && narrativeObjects.Length > 0 ? narrativeObjects.Length : 1;

        if (currentPageIndex + 1 < totalPages)
        {
            ShowPage(currentPageIndex + 1);
        }
        else
        {
            StartCoroutine(FadeOutAndEnd());
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

            yield return new WaitForSecondsRealtime(typingSpeed);
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

        gameObject.SetActive(false);
    }

    private void HideContent()
    {
        if (blackPanel != null) blackPanel.SetActive(true);
        if (titleObject != null) titleObject.SetActive(false);

        if (narrativeObjects != null)
        {
            foreach (var page in narrativeObjects)
            {
                if (page != null) page.SetActive(false);
            }
        }

        if (singlePrologueText != null)
            singlePrologueText.gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (blackPanel == null)
        {
            Transform panel = transform.Find("Black Panel");
            if (panel != null) blackPanel = panel.gameObject;
        }

        Transform searchRoot = blackPanel != null ? blackPanel.transform : transform;

        if (titleObject == null)
        {
            Transform t = searchRoot.Find("Prolog") ?? searchRoot.Find("Title");
            if (t != null) titleObject = t.gameObject;
        }

        if (narrativeObjects == null || narrativeObjects.Length == 0)
        {
            List<GameObject> pages = new List<GameObject>();
            for (int i = 0; i < searchRoot.childCount; i++)
            {
                Transform child = searchRoot.GetChild(i);
                if (child.name.ToLower().StartsWith("narasi"))
                    pages.Add(child.gameObject);
            }

            if (pages.Count > 0)
                narrativeObjects = pages.ToArray();
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
