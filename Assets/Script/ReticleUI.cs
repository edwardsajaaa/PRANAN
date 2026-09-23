using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReticleUI : MonoBehaviour
{
    public static ReticleUI Instance { get; private set; }

    [Header("Reticle Visual Elements")]
    public Image reticleDot;
    public TextMeshProUGUI promptText;
    public CanvasGroup canvasGroup;

    [Header("Exploration State")]
    public Color idleColor = new Color(1f, 1f, 1f, 0.4f); // 40% transparan (W1-07)
    public Vector3 idleScale = Vector3.one;

    [Header("Targeted State")]
    public Color targetedColor = new Color(1f, 0.85f, 0.1f, 1f); // Kuning cerah (W1-07)
    public Vector3 targetedScale = new Vector3(1.35f, 1.35f, 1f);

    [Header("Animation Settings")]
    public float transitionSpeed = 12f;

    private Color currentColor;
    private Vector3 currentScale;
    private bool isTargeting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentColor = idleColor;
        currentScale = idleScale;
        ClearPrompt();
    }

    private void Update()
    {
        // Sembunyikan reticle jika bukan di state Exploration
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameState.Exploration)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            return;
        }
        else
        {
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        // Animasi transisi warna & scale reticle secara mulus
        Color targetColor = isTargeting ? targetedColor : idleColor;
        Vector3 targetScale = isTargeting ? targetedScale : idleScale;

        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * transitionSpeed);
        currentScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * transitionSpeed);

        if (reticleDot != null)
        {
            reticleDot.color = currentColor;
            reticleDot.transform.localScale = currentScale;
        }
    }

    public void ShowPrompt(string clueName)
    {
        isTargeting = true;
        if (promptText != null)
        {
            promptText.text = $"[E] Periksa {clueName}";
            promptText.gameObject.SetActive(true);
        }
    }

    public void ClearPrompt()
    {
        isTargeting = false;
        if (promptText != null)
        {
            promptText.text = string.Empty;
            promptText.gameObject.SetActive(false);
        }
    }
}
