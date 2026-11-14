using UnityEngine;

/// <summary>
/// Mengontrol kemunculan dan animasi prosedural walkie-talkie (PROP_Comms_WalkieTalkie)
/// di tangan player saat Rangga berkomunikasi dengan Nadia melalui RadioDialogueManager:
/// - Tersembunyi / turun ke bawah layar saat tidak ada komunikasi radio
/// - Muncul ke tangan saat komunikasi dimulai
/// - Mendekat ke kamera/mulut player saat Rangga berbicara
/// - Menjauh sedikit saat mendengarkan suara Nadia
///
/// Cara Setup Visual (WYSIWYG) di Unity Inspector:
/// 1. Pasang script ini di PROP_Comms_WalkieTalkie (anak dari Main Camera).
/// 2. Atur posisi walkie-talkie untuk mendengarkan Nadia di Scene View -> klik titik tiga (⋮) komponen ini -> "Simpan Posisi Ini -> Pose Mendengarkan (Nadia)".
/// 3. Geser walkie-talkie lebih dekat ke kamera untuk Rangga berbicara -> klik titik tiga (⋮) -> "Simpan Posisi Ini -> Pose Berbicara (Rangga)".
/// 4. (Opsional) Geser ke bawah layar -> klik titik tiga (⋮) -> "Simpan Posisi Ini -> Pose Tersembunyi".
/// </summary>
public class WalkieTalkieController : MonoBehaviour
{
    [Header("Pose Tersimpan (Atur lewat menu titik tiga ⋮ di kanan atas komponen ini)")]
    [SerializeField] private bool useCustomListeningPose = false;
    [SerializeField] private Vector3 customListeningPos;
    [SerializeField] private Vector3 customListeningEuler;

    [SerializeField] private bool useCustomSpeakingPose = false;
    [SerializeField] private Vector3 customSpeakingPos;
    [SerializeField] private Vector3 customSpeakingEuler;

    [SerializeField] private bool useCustomHiddenPose = false;
    [SerializeField] private Vector3 customHiddenPos;
    [SerializeField] private Vector3 customHiddenEuler;

    [Header("Anchor Transform (Opsional - jika ingin memakai Empty GameObject)")]
    [SerializeField] private Transform speakingPoseAnchor;
    [SerializeField] private Transform listeningPoseAnchor;
    [SerializeField] private Transform hiddenPoseAnchor;

    [Header("Offset Fallback (Jika Pose Tersimpan & Anchor belum diatur)")]
    [SerializeField] private Vector3 hiddenPositionOffset = new Vector3(0.05f, -0.55f, -0.05f);
    [SerializeField] private Vector3 hiddenRotationOffset = new Vector3(30f, 0f, -12f);

    [SerializeField] private Vector3 speakingPositionOffset = new Vector3(-0.14f, 0.03f, -0.25f);
    [SerializeField] private Vector3 speakingRotationOffset = new Vector3(-6f, 12f, 5f);

    [SerializeField] private Vector3 listeningPositionOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 listeningRotationOffset = new Vector3(0f, 0f, 0f);

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
        GetTargetPose(RadioSpeakerState.None, out currentAnchorPos, out currentAnchorRot);

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

        if (currentState == RadioSpeakerState.None)
        {
            if (renderersVisible && Vector3.Distance(currentAnchorPos, targetPos) < 0.015f)
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
        switch (state)
        {
            case RadioSpeakerState.Rangga:
                if (speakingPoseAnchor != null)
                {
                    GetAnchorLocalPose(speakingPoseAnchor, out targetPos, out targetRot);
                }
                else if (useCustomSpeakingPose)
                {
                    targetPos = customSpeakingPos;
                    targetRot = Quaternion.Euler(customSpeakingEuler);
                }
                else
                {
                    targetPos = baseLocalPosition + speakingPositionOffset;
                    targetRot = baseLocalRotation * Quaternion.Euler(speakingRotationOffset);
                }
                break;

            case RadioSpeakerState.Nadia:
                if (listeningPoseAnchor != null)
                {
                    GetAnchorLocalPose(listeningPoseAnchor, out targetPos, out targetRot);
                }
                else if (useCustomListeningPose)
                {
                    targetPos = customListeningPos;
                    targetRot = Quaternion.Euler(customListeningEuler);
                }
                else
                {
                    targetPos = baseLocalPosition + listeningPositionOffset;
                    targetRot = baseLocalRotation * Quaternion.Euler(listeningRotationOffset);
                }
                break;

            default:
                if (hiddenPoseAnchor != null)
                {
                    GetAnchorLocalPose(hiddenPoseAnchor, out targetPos, out targetRot);
                }
                else if (useCustomHiddenPose)
                {
                    targetPos = customHiddenPos;
                    targetRot = Quaternion.Euler(customHiddenEuler);
                }
                else
                {
                    Vector3 refPos = useCustomListeningPose ? customListeningPos : baseLocalPosition;
                    Quaternion refRot = useCustomListeningPose ? Quaternion.Euler(customListeningEuler) : baseLocalRotation;
                    targetPos = refPos + hiddenPositionOffset;
                    targetRot = refRot * Quaternion.Euler(hiddenRotationOffset);
                }
                break;
        }
    }

    private void GetAnchorLocalPose(Transform anchor, out Vector3 localPos, out Quaternion localRot)
    {
        if (transform.parent != null)
        {
            localPos = transform.parent.InverseTransformPoint(anchor.position);
            localRot = Quaternion.Inverse(transform.parent.rotation) * anchor.rotation;
        }
        else
        {
            localPos = anchor.position;
            localRot = anchor.rotation;
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

#if UNITY_EDITOR
    [ContextMenu("Simpan Posisi Ini -> Pose Mendengarkan (Nadia)")]
    private void SaveListeningPose()
    {
        useCustomListeningPose = true;
        customListeningPos = transform.localPosition;
        customListeningEuler = transform.localEulerAngles;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[WalkieTalkieController] Pose Mendengarkan (Nadia) disimpan: {customListeningPos}");
    }

    [ContextMenu("Simpan Posisi Ini -> Pose Berbicara (Rangga)")]
    private void SaveSpeakingPose()
    {
        useCustomSpeakingPose = true;
        customSpeakingPos = transform.localPosition;
        customSpeakingEuler = transform.localEulerAngles;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[WalkieTalkieController] Pose Berbicara (Rangga) disimpan: {customSpeakingPos}");
    }

    [ContextMenu("Simpan Posisi Ini -> Pose Tersembunyi")]
    private void SaveHiddenPose()
    {
        useCustomHiddenPose = true;
        customHiddenPos = transform.localPosition;
        customHiddenEuler = transform.localEulerAngles;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[WalkieTalkieController] Pose Tersembunyi disimpan: {customHiddenPos}");
    }

    [ContextMenu("Preview -> Pindah ke Pose Mendengarkan")]
    private void PreviewListeningPose()
    {
        if (!useCustomListeningPose) return;
        transform.localPosition = customListeningPos;
        transform.localEulerAngles = customListeningEuler;
        UnityEditor.EditorUtility.SetDirty(transform);
    }

    [ContextMenu("Preview -> Pindah ke Pose Berbicara")]
    private void PreviewSpeakingPose()
    {
        if (!useCustomSpeakingPose) return;
        transform.localPosition = customSpeakingPos;
        transform.localEulerAngles = customSpeakingEuler;
        UnityEditor.EditorUtility.SetDirty(transform);
    }
#endif
}
