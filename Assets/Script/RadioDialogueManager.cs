using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class RadioDialogueLine
{
    public string speaker;
    [TextArea(2, 4)]
    public string text;
    public float duration = 4.5f;
    public AudioClip audioClip;
    public bool isMonologue = false;
}

public class RadioDialogueManager : MonoBehaviour
{
    public static RadioDialogueManager Instance { get; private set; }

    [Header("UI References (Optional - will auto-create if empty)")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image backgroundImage;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip radioStaticSound;

    [Header("Colors & Styling")]
    [SerializeField] private bool useMinimalistStyle = true;
    [SerializeField] private TMP_FontAsset customFont;
    [SerializeField] private TMP_FontAsset monologueFont;
    [SerializeField] private Color nadiaColor = new Color(0.35f, 0.85f, 1f, 1f);
    [SerializeField] private Color ranggaColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color monologueColor = new Color(0.85f, 0.90f, 0.95f, 0.95f);
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Scene 1 Auto-Play")]
    [SerializeField] private bool autoPlayScene1OnExploration = true;
    [SerializeField] private float scene1StartDelay = 1.5f;

    private Coroutine activeDialogueRoutine;
    private bool hasPlayedScene1 = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        EnsureUIReferences();
    }

    private void Start()
    {
        GameStateManager.OnStateChanged += HandleStateChanged;
        PrologueController.OnPrologueFinished += HandlePrologueFinished;

        if (PrologueController.IsPrologueRunning)
        {
            Debug.Log("[RadioDialogue] Prolog sedang aktif. Dialog Radio Scene 1 menunggu prolog selesai...");
            return;
        }

        if (autoPlayScene1OnExploration && !hasPlayedScene1)
        {
            TryTriggerScene1();
        }
    }

    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;
        PrologueController.OnPrologueFinished -= HandlePrologueFinished;
    }

    private void HandlePrologueFinished()
    {
        Debug.Log("[RadioDialogue] Prolog selesai! Memulai radio dialogue Scene 1...");
        if (autoPlayScene1OnExploration && !hasPlayedScene1)
        {
            TryTriggerScene1();
        }
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.Exploration && !hasPlayedScene1 && !PrologueController.IsPrologueRunning)
        {
            TryTriggerScene1();
        }
    }

    public void TryTriggerScene1()
    {
        if (!hasPlayedScene1)
        {
            hasPlayedScene1 = true;
            Debug.Log("[RadioDialogue] Memulai timer Scene 1 Gerbang Pos...");
            StartCoroutine(DelayedScene1Start(scene1StartDelay));
        }
    }

    private IEnumerator DelayedScene1Start(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        PlayScene1GerbangPos();
    }

    public void PlayScene1GerbangPos()
    {
        Debug.Log("[RadioDialogue] Memulai sekuens dialog Radio Scene 1...");

        List<RadioDialogueLine> lines = new List<RadioDialogueLine>
        {
            new RadioDialogueLine
            {
                speaker = "Nadia (Radio)",
                text = "Rangga, masuk. Gimana, udah sampai di pos perbatasan Rano-Tawa?",
                duration = 4.0f,
                audioClip = radioStaticSound
            },
            new RadioDialogueLine
            {
                speaker = "Rangga",
                text = "Udah, Nad. Baru aja lewatin palang pos luar. Sepi banget di sini, pos jaganya kosong. Mana udaranya gerah banget lagi, kabut asap tipis udah mulai turun ke lembah.",
                duration = 6.0f
            },
            new RadioDialogueLine
            {
                speaker = "Nadia (Radio)",
                text = "Hati-hati, Ga. Dari pantauan satelit STADI 2025 di layar aku, titik hutan yang gundul itu posisinya cuma sekitar 300 meter di atas jalur kamu. Ingat alasan pihak pengelola kemarin: mereka ngotot hutan rusak gara-gara kemarau dan angin puting beliung. Coba kamu cek ke dalam, beneran faktor alam atau ada yang nggak beres.",
                duration = 8.5f,
                audioClip = radioStaticSound
            },
            new RadioDialogueLine
            {
                speaker = "Rangga",
                text = "Siap. Angin ribut nggak mungkin ngebotakin hutan seluas itu dalam dua minggu. Aku jalan masuk sekarang.",
                duration = 4.5f
            }
        };

        StartDialogueSequence(lines);
    }

    public void ShowMonologue(string monologueText, float duration = 5.0f)
    {
        RadioDialogueLine line = new RadioDialogueLine
        {
            speaker = "Rangga (Batin)",
            text = monologueText,
            duration = duration,
            isMonologue = true
        };

        StartDialogueSequence(new List<RadioDialogueLine> { line });
    }

    public void StartDialogueSequence(List<RadioDialogueLine> lines)
    {
        EnsureUIReferences();

        if (activeDialogueRoutine != null)
        {
            StopCoroutine(activeDialogueRoutine);
        }

        activeDialogueRoutine = StartCoroutine(DialogueSequenceRoutine(lines));
    }

    private IEnumerator DialogueSequenceRoutine(List<RadioDialogueLine> lines)
    {
        foreach (var line in lines)
        {
            if (line == null) continue;

            if (line.audioClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(line.audioClip, 0.7f);
            }

            UpdateUIContent(line);
            yield return StartCoroutine(FadeSubtitle(0f, 1f));

            yield return new WaitForSecondsRealtime(line.duration);

            yield return StartCoroutine(FadeSubtitle(1f, 0f));
            yield return new WaitForSecondsRealtime(0.2f);
        }

        activeDialogueRoutine = null;
    }

    private void UpdateUIContent(RadioDialogueLine line)
    {
        if (speakerText != null)
        {
            if (customFont != null) speakerText.font = customFont;

            string speakerName = line.speaker;
            if (!string.IsNullOrEmpty(speakerName))
            {
                // Format ala Pamali: "Salsa:" / "Rangga:"
                if (!speakerName.EndsWith(":"))
                {
                    speakerName += ":";
                }
                speakerText.text = speakerName;
                speakerText.gameObject.SetActive(true);
            }
            else
            {
                speakerText.gameObject.SetActive(false);
            }

            if (useMinimalistStyle)
            {
                speakerText.color = Color.white;
            }
            else
            {
                if (line.isMonologue)
                    speakerText.color = monologueColor;
                else if (line.speaker.ToLower().Contains("nadia"))
                    speakerText.color = nadiaColor;
                else
                    speakerText.color = ranggaColor;
            }
        }

        if (bodyText != null)
        {
            if (line.isMonologue && monologueFont != null)
            {
                bodyText.font = monologueFont;
                bodyText.text = line.text;
            }
            else
            {
                if (customFont != null) bodyText.font = customFont;
                bodyText.text = line.isMonologue ? $"<i>{line.text}</i>" : line.text;
            }

            bodyText.color = (useMinimalistStyle || !line.isMonologue) ? Color.white : monologueColor;
        }

        Debug.Log($"[RadioDialogue] {line.speaker}: {line.text}");
    }

    private IEnumerator FadeSubtitle(float fromAlpha, float toAlpha)
    {
        if (subtitleCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            subtitleCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / fadeDuration);
            yield return null;
        }
        subtitleCanvasGroup.alpha = toAlpha;
    }

    private void EnsureUIReferences()
    {
        if (subtitleCanvasGroup != null && speakerText != null && bodyText != null)
            return;

        // Cari atau buat Canvas khusus dialog (Screen Space Overlay)
        if (dialogueCanvas == null)
        {
            GameObject canvasObj = GameObject.Find("RadioDialogueCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("RadioDialogueCanvas");
                dialogueCanvas = canvasObj.AddComponent<Canvas>();
                dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                dialogueCanvas.sortingOrder = 200; // Selalu di atas UI gameplay lainnya

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                dialogueCanvas = canvasObj.GetComponent<Canvas>();
            }
        }

        // Panel Subtitle gaya Pamali (Minimalis, di tengah bawah layar tanpa kotak hitam)
        Transform panel = dialogueCanvas.transform.Find("RadioSubtitlePanel");
        if (panel != null)
        {
            // Jika ada panel versi kotak lama, perbarui / hapus agar berganti ke format Pamali
            if (Application.isPlaying)
                Destroy(panel.gameObject);
            else
                DestroyImmediate(panel.gameObject);
            panel = null;
        }

        if (panel == null)
        {
            GameObject panelObj = new GameObject("RadioSubtitlePanel");
            panelObj.transform.SetParent(dialogueCanvas.transform, false);

            RectTransform rt = panelObj.AddComponent<RectTransform>();
            // Anchor di tengah bawah layar (responsif terhadap semua rasio layar)
            rt.anchorMin = new Vector2(0.12f, 0.08f);
            rt.anchorMax = new Vector2(0.88f, 0.28f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            subtitleCanvasGroup = panelObj.AddComponent<CanvasGroup>();
            subtitleCanvasGroup.blocksRaycasts = false;
            subtitleCanvasGroup.interactable = false;
            subtitleCanvasGroup.alpha = 0f;

            // Speaker Text (Nama Pembicara - di tengah, gaya Pamali)
            GameObject spkObj = new GameObject("SpeakerText");
            spkObj.transform.SetParent(panelObj.transform, false);
            RectTransform spkRt = spkObj.AddComponent<RectTransform>();
            spkRt.anchorMin = new Vector2(0f, 0.58f);
            spkRt.anchorMax = new Vector2(1f, 1f);
            spkRt.pivot = new Vector2(0.5f, 1f);
            spkRt.offsetMin = Vector2.zero;
            spkRt.offsetMax = Vector2.zero;

            speakerText = spkObj.AddComponent<TextMeshProUGUI>();
            speakerText.fontSize = 25;
            speakerText.fontStyle = FontStyles.Normal;
            speakerText.alignment = TextAlignmentOptions.Center;
            speakerText.color = Color.white;

            // Efek bayangan halus agar mudah dibaca di latar apapun
            Shadow spkShadow = spkObj.AddComponent<Shadow>();
            spkShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            spkShadow.effectDistance = new Vector2(1.8f, -1.8f);

            // Body Text (Teks Dialog - di tengah, gaya Pamali)
            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(panelObj.transform, false);
            RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 0.58f);
            bodyRt.pivot = new Vector2(0.5f, 0.5f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;

            bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyText.fontSize = 27;
            bodyText.fontStyle = FontStyles.Normal;
            bodyText.enableWordWrapping = true;
            bodyText.alignment = TextAlignmentOptions.Top;
            bodyText.color = Color.white;
            bodyText.lineSpacing = 16;

            // Efek bayangan halus
            Shadow bodyShadow = bodyObj.AddComponent<Shadow>();
            bodyShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            bodyShadow.effectDistance = new Vector2(2f, -2f);
        }
        else
        {
            subtitleCanvasGroup = panel.GetComponent<CanvasGroup>();
            speakerText = panel.Find("SpeakerText")?.GetComponent<TextMeshProUGUI>();
            bodyText = panel.Find("BodyText")?.GetComponent<TextMeshProUGUI>();
        }

#if UNITY_EDITOR
        if (customFont == null)
        {
            customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/FragmentMono-Regular SDF.asset");
        }
        if (monologueFont == null)
        {
            monologueFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/FragmentMono-Italic SDF.asset");
        }
#endif

        if (customFont != null)
        {
            if (speakerText != null) speakerText.font = customFont;
            if (bodyText != null) bodyText.font = customFont;
        }

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 0f;
        }
    }
}
