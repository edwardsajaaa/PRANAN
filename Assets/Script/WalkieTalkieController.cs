using UnityEngine;

/// <summary>
/// Mengontrol kemunculan dan animasi prosedural walkie-talkie (PROP_Comms_WalkieTalkie)
/// di tangan player saat Rangga berkomunikasi dengan Nadia melalui RadioDialogueManager:
/// - Tersembunyi / turun ke bawah layar saat tidak ada komunikasi radio
/// - Muncul ke tangan saat komunikasi dimulai
/// - Mendekat ke kamera/mulut player saat Rangga berbicara
/// - Menjauh sedikit saat mendengarkan suara Nadia
/// - Memiliki ayunan tangan (hand sway) halus agar terasa alami
/// </summary>
public class WalkieTalkieController : MonoBehaviour
{
    [Header("Offset Posisi & Rotasi (Relatif terhadap Posisi Awal di Scene)")]
    [Tooltip("Penyesuaian tinggi keseluruhan saat walkie-talkie muncul (naikkan jika dirasa terlalu rendah)")]
    [SerializeField] private float verticalOffset = 0.04f;

    [Tooltip("Offset posisi saat walkie-talkie disimpan / tidak digunakan (turun ke bawah layar)")]
    [SerializeField] private Vector3 hiddenPositionOffset = new Vector3(0.04f, -0.30f, -0.02f);
    [SerializeField] private Vector3 hiddenRotationOffset = new Vector3(30f, 0f, -12f);

    [Tooltip("Offset posisi saat Rangga berbicara (mendekat ke kamera / mulut player)")]
    [SerializeField] private Vector3 speakingPositionOffset = new Vector3(-0.06f, 0.09f, -0.08f);
    [SerializeField] private Vector3 speakingRotationOffset = new Vector3(-8f, 14f, 6f);

    [Tooltip("Offset posisi saat mendengarkan suara Nadia (menjauh sedikit dari kamera)")]
    [SerializeField] private Vector3 listeningPositionOffset = new Vector3(0.01f, 0.02f, 0.04f);
    [SerializeField] private Vector3 listeningRotationOffset = new Vector3(4f, -6f, -4f);

    [Header("Kecepatan Transisi Animasi")]
    [Tooltip("Waktu perpindahan posisi saat mengangkat / mendekatkan / menjauhkan walkie-talkie")]
    [SerializeField] private float raiseSmoothTime = 0.22f;

    [Tooltip("Waktu perpindahan posisi saat menurunkan walkie-talkie setelah selesai bicara")]
    [SerializeField] private float lowerSmoothTime = 0.28f;

    [Tooltip("Kecepatan interpolasi rotasi walkie-talkie")]
    [SerializeField] private float rotationSmoothSpeed = 9f;

    [Header("Efek Tangan Alami (Hand Sway & Breathing)")]
    [SerializeField] private bool enableHandSway = true;
    [SerializeField] private float breathingSpeed = 1.6f;
    [SerializeField] private float breathingAmountY = 0.004f;
    [SerializeField] private float mouseSwayAmount = 1.2f;
    [SerializeField] private float maxMouseSwayAngle = 4.0f;

    [Header("Audio Opsional (Tombol PTT)")]
    [SerializeField] private AudioSource pttAudioSource;
    [SerializeField] private AudioClip pttPressSound;
    [SerializeField] private AudioClip pttReleaseSound;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;

    private Vector3 currentAnchorPos;
    private Quaternion currentAnchorRot;
    private Vector3 positionVelocity;

    private RadioSpeakerState currentState = RadioSpeakerState.None;
    private Renderer[] cachedRenderers;
    private bool renderersVisible = true;
    private bool isBlockedByGameState = false;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        // Mulai dalam keadaan tersembunyi di bawah layar sebelum ada komunikasi radio
        currentState = RadioSpeakerState.None;
        currentAnchorPos = baseLocalPosition + hiddenPositionOffset;
        currentAnchorRot = baseLocalRotation * Quaternion.Euler(hiddenRotationOffset);

        transform.localPosition = currentAnchorPos;
        transform.localRotation = currentAnchorRot;
        SetRenderersVisible(false);
    }

    private void OnEnable()
    {
        RadioDialogueManager.OnRadioStateChanged += HandleRadioStateChanged;
        GameStateManager.OnStateChanged += HandleGameStateChanged;

        if (RadioDialogueManager.Instance != null)
        {
            HandleRadioStateChanged(RadioDialogueManager.Instance.CurrentRadioState);
        }

        if (GameStateManager.Instance != null)
        {
            HandleGameStateChanged(GameStateManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        RadioDialogueManager.OnRadioStateChanged -= HandleRadioStateChanged;
        GameStateManager.OnStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newGameState)
    {
        isBlockedByGameState = newGameState == GameState.Inspection
                            || newGameState == GameState.Dialogue
                            || newGameState == GameState.Ending;

        if (isBlockedByGameState)
        {
            SetRenderersVisible(false);
        }
        else if (currentState != RadioSpeakerState.None)
        {
            SetRenderersVisible(true);
        }
    }

    private void HandleRadioStateChanged(RadioSpeakerState newState)
    {
        if (currentState == newState) return;

        RadioSpeakerState previousState = currentState;
        currentState = newState;

        if (currentState != RadioSpeakerState.None && !isBlockedByGameState)
        {
            SetRenderersVisible(true);
        }

        // Bunyi klik PTT opsional saat Rangga mulai atau selesai menekan tombol bicara
        if (pttAudioSource != null)
        {
            if (currentState == RadioSpeakerState.Rangga && pttPressSound != null)
            {
                pttAudioSource.PlayOneShot(pttPressSound, 0.5f);
            }
            else if (previousState == RadioSpeakerState.Rangga && pttReleaseSound != null)
            {
                pttAudioSource.PlayOneShot(pttReleaseSound, 0.45f);
            }
        }
    }

    private void LateUpdate()
    {
        if (isBlockedByGameState) return;

        GetTargetPose(currentState, out Vector3 targetPos, out Quaternion targetRot);

        float smoothTime = currentState == RadioSpeakerState.None ? lowerSmoothTime : raiseSmoothTime;
        float dt = Time.unscaledDeltaTime;

        currentAnchorPos = Vector3.SmoothDamp(currentAnchorPos, targetPos, ref positionVelocity, smoothTime, Mathf.Infinity, dt);
        currentAnchorRot = Quaternion.Slerp(currentAnchorRot, targetRot, 1f - Mathf.Exp(-rotationSmoothSpeed * dt));

        // Matikan renderer setelah animasi turun selesai agar hemat performa & tidak bocor ke kamera
        if (currentState == RadioSpeakerState.None)
        {
            if (renderersVisible && Vector3.Distance(currentAnchorPos, targetPos) < 0.012f)
            {
                SetRenderersVisible(false);
            }
        }

        Vector3 finalPos = currentAnchorPos;
        Quaternion finalRot = currentAnchorRot;

        if (enableHandSway && renderersVisible && currentState != RadioSpeakerState.None)
        {
            float swayMultiplier = currentState == RadioSpeakerState.Rangga ? 0.65f : 1f;
            float breathY = Mathf.Sin(Time.unscaledTime * breathingSpeed) * breathingAmountY * swayMultiplier;
            float breathX = Mathf.Cos(Time.unscaledTime * breathingSpeed * 0.5f) * (breathingAmountY * 0.5f) * swayMultiplier;
            finalPos += new Vector3(breathX, breathY, 0f);

            if (GameStateManager.Instance == null || GameStateManager.Instance.CurrentState == GameState.Exploration)
            {
                float mouseX = Mathf.Clamp(-Input.GetAxisRaw("Mouse X") * mouseSwayAmount, -maxMouseSwayAngle, maxMouseSwayAngle);
                float mouseY = Mathf.Clamp(-Input.GetAxisRaw("Mouse Y") * mouseSwayAmount, -maxMouseSwayAngle, maxMouseSwayAngle);
                finalRot *= Quaternion.Euler(mouseY, mouseX, -mouseX * 0.5f);
            }
        }

        transform.localPosition = finalPos;
        transform.localRotation = finalRot;
    }

    private void GetTargetPose(RadioSpeakerState state, out Vector3 targetPos, out Quaternion targetRot)
    {
        Vector3 heightAdj = new Vector3(0f, verticalOffset, 0f);

        switch (state)
        {
            case RadioSpeakerState.Rangga:
                targetPos = baseLocalPosition + speakingPositionOffset + heightAdj;
                targetRot = baseLocalRotation * Quaternion.Euler(speakingRotationOffset);
                break;

            case RadioSpeakerState.Nadia:
                targetPos = baseLocalPosition + listeningPositionOffset + heightAdj;
                targetRot = baseLocalRotation * Quaternion.Euler(listeningRotationOffset);
                break;

            default:
                targetPos = baseLocalPosition + hiddenPositionOffset;
                targetRot = baseLocalRotation * Quaternion.Euler(hiddenRotationOffset);
                break;
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        if (renderersVisible == visible) return;
        renderersVisible = visible;

        if (cachedRenderers == null) return;
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].enabled = visible;
            }
        }
    }
}

