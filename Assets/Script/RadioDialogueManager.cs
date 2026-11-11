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

    [Header("UI References (Manual Setup)")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;

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
    [Tooltip("Jika dicentang, dialog Scene 1 langsung dimulai saat game/prolog selesai. Jika ingin lewat trigger portal, matikan opsi ini.")]
    [SerializeField] private bool autoPlayScene1OnExploration = false;
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

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 0f;
        }
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
        if (autoPlayScene1OnExploration && newState == GameState.Exploration && !hasPlayedScene1 && !PrologueController.IsPrologueRunning)
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

    /// <summary>
    /// Menghentikan dan langsung menghilangkan teks dialog/monolog yang sedang aktif di layar.
    /// </summary>
    public void DismissDialogueImmediate()
    {
        if (activeDialogueRoutine != null)
        {
            StopCoroutine(activeDialogueRoutine);
            activeDialogueRoutine = null;
        }

        if (subtitleCanvasGroup != null)
        {
            subtitleCanvasGroup.alpha = 0f;
        }

        if (speakerText != null) speakerText.text = "";
        if (bodyText != null) bodyText.text = "";
    }

    public void StartDialogueSequence(List<RadioDialogueLine> lines)
    {
        if (subtitleCanvasGroup == null || speakerText == null || bodyText == null)
        {
            Debug.LogWarning("[RadioDialogueManager] UI References belum di-assign lengkap di Inspector! Pastikan Subtitle Canvas Group, Speaker Text, dan Body Text sudah ditarik.");
            return;
        }

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
}
