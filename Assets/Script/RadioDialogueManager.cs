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

    [Header("UI References")]
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image backgroundImage;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip radioStaticSound;

    [Header("Colors & Styling")]
    [SerializeField] private Color nadiaColor = new Color(0.35f, 0.78f, 1f, 1f);
    [SerializeField] private Color ranggaColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color monologueColor = new Color(0.88f, 0.90f, 0.92f, 0.95f);
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Scene 1 Auto-Play")]
    [SerializeField] private bool autoPlayScene1OnExploration = true;
    [SerializeField] private float scene1StartDelay = 1.8f;

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

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 0f;
        }
    }

    private void Start()
    {
        GameStateManager.OnStateChanged += HandleStateChanged;

        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState == GameState.Exploration)
        {
            TryTriggerScene1();
        }
    }

    private void OnDestroy()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState newState)
    {
        if (newState == GameState.Exploration)
        {
            TryTriggerScene1();
        }
    }

    private void TryTriggerScene1()
    {
        if (autoPlayScene1OnExploration && !hasPlayedScene1)
        {
            hasPlayedScene1 = true;
            StartCoroutine(DelayedScene1Start(scene1StartDelay));
        }
    }

    private IEnumerator DelayedScene1Start(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayScene1GerbangPos();
    }

    public void PlayScene1GerbangPos()
    {
        List<RadioDialogueLine> lines = new List<RadioDialogueLine>
        {
            new RadioDialogueLine
            {
                speaker = "Nadia (Via Radio)",
                text = "Rangga, masuk. Gimana, udah sampai di pos perbatasan Rano-Tawa?",
                duration = 4.2f,
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
                speaker = "Nadia (Via Radio)",
                text = "Hati-hati, Ga. Dari pantauan satelit STADI 2025 di layar aku, titik hutan yang gundul itu posisinya cuma sekitar 300 meter di atas jalur kamu. Ingat ya alasan pihak pengelola kemarin: mereka ngotot hutan di atas rusak gara-gara kekeringan kemarau dan angin puting beliung. Coba kamu cek ke dalam, beneran faktor alam atau ada yang nggak beres.",
                duration = 9.0f,
                audioClip = radioStaticSound
            },
            new RadioDialogueLine
            {
                speaker = "Rangga",
                text = "Siap. Angin ribut nggak mungkin ngebotakin hutan seluas itu dalam dua minggu. Aku jalan masuk sekarang.",
                duration = 4.8f
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

            yield return new WaitForSeconds(line.duration);

            yield return StartCoroutine(FadeSubtitle(1f, 0f));
            yield return new WaitForSeconds(0.25f);
        }

        activeDialogueRoutine = null;
    }

    private void UpdateUIContent(RadioDialogueLine line)
    {
        if (speakerText != null)
        {
            speakerText.text = line.speaker;

            if (line.isMonologue)
                speakerText.color = monologueColor;
            else if (line.speaker.ToLower().Contains("nadia"))
                speakerText.color = nadiaColor;
            else
                speakerText.color = ranggaColor;
        }

        if (bodyText != null)
        {
            bodyText.text = line.isMonologue ? $"<i>\"{line.text}\"</i>" : $"\"{line.text}\"";
            bodyText.color = line.isMonologue ? monologueColor : Color.white;
        }
    }

    private IEnumerator FadeSubtitle(float fromAlpha, float toAlpha)
    {
        if (subtitleCanvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            subtitleCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / fadeDuration);
            yield return null;
        }
        subtitleCanvasGroup.alpha = toAlpha;
    }

    private void EnsureUIReferences()
    {
        if (subtitleCanvasGroup != null && speakerText != null && bodyText != null)
            return;

        Canvas targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null) return;

        Transform panel = targetCanvas.transform.Find("RadioSubtitlePanel");
        if (panel == null)
        {
            GameObject panelObj = new GameObject("RadioSubtitlePanel");
            panelObj.transform.SetParent(targetCanvas.transform, false);

            RectTransform rt = panelObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.08f);
            rt.anchorMax = new Vector2(0.8f, 0.22f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = panelObj.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.65f);
            backgroundImage = bg;

            subtitleCanvasGroup = panelObj.AddComponent<CanvasGroup>();
            subtitleCanvasGroup.blocksRaycasts = false;
            subtitleCanvasGroup.interactable = false;

            GameObject spkObj = new GameObject("SpeakerText");
            spkObj.transform.SetParent(panelObj.transform, false);
            RectTransform spkRt = spkObj.AddComponent<RectTransform>();
            spkRt.anchorMin = new Vector2(0.04f, 0.62f);
            spkRt.anchorMax = new Vector2(0.96f, 0.95f);
            spkRt.offsetMin = Vector2.zero;
            spkRt.offsetMax = Vector2.zero;
            speakerText = spkObj.AddComponent<TextMeshProUGUI>();
            speakerText.fontSize = 18;
            speakerText.fontStyle = FontStyles.Bold;
            speakerText.alignment = TextAlignmentOptions.Left;

            GameObject bodyObj = new GameObject("BodyText");
            bodyObj.transform.SetParent(panelObj.transform, false);
            RectTransform bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.04f, 0.08f);
            bodyRt.anchorMax = new Vector2(0.96f, 0.62f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;
            bodyText = bodyObj.AddComponent<TextMeshProUGUI>();
            bodyText.fontSize = 16;
            bodyText.enableWordWrapping = true;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
        }
        else
        {
            subtitleCanvasGroup = panel.GetComponent<CanvasGroup>();
            speakerText = panel.Find("SpeakerText")?.GetComponent<TextMeshProUGUI>();
            bodyText = panel.Find("BodyText")?.GetComponent<TextMeshProUGUI>();
        }
    }
}
