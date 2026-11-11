using UnityEngine;
using System.Collections.Generic;

public class ClueInspector : MonoBehaviour
{
    private static ClueInspector instance;
    public static ClueInspector Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindObjectOfType<ClueInspector>();
                if (instance == null)
                {
                    GameObject go = new GameObject("InspectionStudio");
                    instance = go.AddComponent<ClueInspector>();
                }
            }
            return instance;
        }
    }

    [Header("Studio References")]
    public Camera inspectionCamera;
    public Transform studioPivot;
    public GameObject threePointLightingSetup;
    public InspectionUI inspectionUI;

    [Header("Rotation & Damping")]
    public float rotationSpeed = 5f;
    public float dampingRate = 4f;

    [Header("Zoom Settings")]
    public float zoomSensitivity = 0.5f;
    public float minZoomDistance = 0.8f;
    public float maxZoomDistance = 3.0f;
    public float defaultZoomDistance = 2.5f;

    [Header("Zoom Ratio Limits")]
    [Tooltip("Batas rasio zoom in terdekat terhadap ukuran ideal (0.45 = bisa perbesar 2.2x lebih dekat)")]
    public float minZoomRatio = 0.45f;
    [Tooltip("Batas rasio zoom out terjauh agar objek tetap proporsional & jelas terbaca (default: 1.12x)")]
    public float maxZoomRatio = 1.12f;

    [Header("Transition & Motion Settings")]
    [Tooltip("Durasi animasi gerak halus saat objek masuk ke mode periksa")]
    public float entranceDuration = 0.48f;
    [Tooltip("Durasi animasi transisi saat keluar dari mode periksa")]
    public float exitDuration = 0.26f;

    private InteractableObject activeClue;
    private GameObject spawnedInspectionModel;
    private Transform modelHolder;
    private List<AnomalyHotspot> currentHotspots = new List<AnomalyHotspot>();

    private Vector2 angularVelocity;
    private float currentZoom;
    private bool isDragging = false;
    private bool isInspecting = false;
    private bool isTransitioning = false;
    private bool isClosing = false;
    private Coroutine activeTransitionCoroutine = null;
    private readonly Color bgTargetColor = new Color(0.10f, 0.12f, 0.16f, 0.94f);

    private Camera cachedMainCamera;
    private UnityEngine.UI.RawImage backgroundRawImage;
    private RenderTexture blurredBackgroundRT;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(false);
        if (threePointLightingSetup != null) threePointLightingSetup.SetActive(false);
        currentZoom = defaultZoomDistance;
    }

    private void Update()
    {
        if (!isInspecting) return;

        HandleInput();
        if (!isTransitioning)
        {
            ApplyGyroscopicRotation();
            HandleZoom();
            EvaluateHotspots();
        }
    }

    public void StartInspection(InteractableObject clue)
    {
        if (clue == null) return;
        EnsureStudioReady();
        activeClue = clue;
        isInspecting = true;
        isClosing = false;

        if (modelHolder != null)
        {
            modelHolder.localRotation = Quaternion.identity;
            modelHolder.localPosition = Vector3.zero;
            modelHolder.localScale = Vector3.one;
        }

        if (studioPivot != null)
        {
            studioPivot.rotation = Quaternion.identity;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Inspection);
        }

        // Tangkap latar belakang alami game sebelum lampu studio dinyalakan agar tidak silau/terlalu terang
        CaptureBlurredBackground();

        if (Camera.main != null && Camera.main != inspectionCamera)
        {
            cachedMainCamera = Camera.main;
            cachedMainCamera.gameObject.SetActive(false);
        }

        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(true);
        if (threePointLightingSetup != null) threePointLightingSetup.SetActive(true);

        SetupInspectionModel(clue);

        angularVelocity = Vector2.zero;
        float targetZoom = CalculateIdealZoom(spawnedInspectionModel);

        if (inspectionUI != null)
        {
            inspectionUI.Show(clue);
            inspectionUI.SetAlpha(0f);
        }

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }
        activeTransitionCoroutine = StartCoroutine(EntranceMotionRoutine(targetZoom));
    }

    private float CalculateIdealZoom(GameObject targetModel)
    {
        if (targetModel == null) return defaultZoomDistance;

        Renderer[] renderers = targetModel.GetComponentsInChildren<Renderer>();
        Collider[] colliders = targetModel.GetComponentsInChildren<Collider>();

        Bounds combinedBounds = new Bounds(targetModel.transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (var r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            if (!hasBounds)
            {
                combinedBounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(r.bounds);
            }
        }

        if (!hasBounds)
        {
            foreach (var c in colliders)
            {
                if (!hasBounds)
                {
                    combinedBounds = c.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(c.bounds);
                }
            }
        }

        if (!hasBounds) return defaultZoomDistance;

        float maxDim = Mathf.Max(combinedBounds.size.x, Mathf.Max(combinedBounds.size.y, combinedBounds.size.z));
        float fov = (inspectionCamera != null) ? inspectionCamera.fieldOfView : 60f;
        float padding = 2.85f;

        float idealDist = (maxDim * 0.5f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * padding;
        minZoomDistance = Mathf.Max(0.4f, idealDist * minZoomRatio);
        maxZoomDistance = idealDist * maxZoomRatio;
        return idealDist;
    }

    private void EnsureStudioReady()
    {
        if (studioPivot == null)
        {
            GameObject pivotObj = new GameObject("StudioPivot");
            pivotObj.transform.SetParent(transform);
            pivotObj.transform.position = new Vector3(0, 1000f, 0);
            studioPivot = pivotObj.transform;
        }

        if (modelHolder == null)
        {
            GameObject holderObj = new GameObject("ModelHolder");
            holderObj.transform.SetParent(studioPivot);
            holderObj.transform.localPosition = Vector3.zero;
            holderObj.transform.localRotation = Quaternion.identity;
            modelHolder = holderObj.transform;
        }

        if (inspectionCamera == null)
        {
            GameObject camObj = new GameObject("InspectionCamera");
            camObj.transform.SetParent(studioPivot);
            camObj.transform.localPosition = new Vector3(0, 0, -defaultZoomDistance);
            camObj.transform.localRotation = Quaternion.identity;
            inspectionCamera = camObj.AddComponent<Camera>();
            inspectionCamera.clearFlags = CameraClearFlags.SolidColor;
            inspectionCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
            inspectionCamera.depth = 20f;
            inspectionCamera.nearClipPlane = 0.05f;
            inspectionCamera.farClipPlane = 100f;
        }

        if (backgroundRawImage == null)
        {
            GameObject bgCanvasObj = new GameObject("InspectionBackgroundCanvas");
            bgCanvasObj.transform.SetParent(studioPivot);
            Canvas bgCanvas = bgCanvasObj.AddComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            bgCanvas.worldCamera = inspectionCamera;
            bgCanvas.planeDistance = 30f;
            bgCanvas.sortingOrder = -50;
            bgCanvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

            GameObject bgImageObj = new GameObject("BlurredBackground");
            bgImageObj.transform.SetParent(bgCanvasObj.transform, false);
            var rect = bgImageObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            backgroundRawImage = bgImageObj.AddComponent<UnityEngine.UI.RawImage>();

            // Warna latar belakang redup/gelap agar serasi dengan suasana sore hutan dan tidak silau
            backgroundRawImage.color = new Color(0.10f, 0.12f, 0.16f, 0.94f);
        }

        if (threePointLightingSetup == null)
        {
            threePointLightingSetup = new GameObject("ThreePointLighting");
            threePointLightingSetup.transform.SetParent(studioPivot);
            threePointLightingSetup.transform.localPosition = Vector3.zero;

            GameObject keyLightObj = new GameObject("KeyLight");
            keyLightObj.transform.SetParent(threePointLightingSetup.transform);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.2f;
            keyLight.color = new Color(1f, 0.95f, 0.9f);
            keyLightObj.transform.rotation = Quaternion.Euler(45, 45, 0);

            GameObject fillLightObj = new GameObject("FillLight");
            fillLightObj.transform.SetParent(threePointLightingSetup.transform);
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = 0.7f;
            fillLight.color = new Color(0.8f, 0.9f, 1f);
            fillLightObj.transform.rotation = Quaternion.Euler(30, -135, 0);

            GameObject backLightObj = new GameObject("BackLight");
            backLightObj.transform.SetParent(threePointLightingSetup.transform);
            Light backLight = backLightObj.AddComponent<Light>();
            backLight.type = LightType.Directional;
            backLight.intensity = 0.9f;
            backLight.color = Color.white;
            backLightObj.transform.rotation = Quaternion.Euler(-30, 180, 0);
        }

        if (inspectionUI == null)
        {
            inspectionUI = Object.FindObjectOfType<InspectionUI>();
            if (inspectionUI == null)
            {
                inspectionUI = CreateFallbackUI();
            }
        }
    }

    private void CaptureBlurredBackground()
    {
        Camera cam = (cachedMainCamera != null && cachedMainCamera.gameObject.activeInHierarchy) ? cachedMainCamera : Camera.main;
        if (cam == null) return;

        int width = Mathf.Max(Screen.width / 4, 320);
        int height = Mathf.Max(Screen.height / 4, 180);

        RenderTexture rt1 = RenderTexture.GetTemporary(width, height, 16);
        RenderTexture rt2 = RenderTexture.GetTemporary(width / 2, height / 2, 0);

        RenderTexture prevTarget = cam.targetTexture;
        cam.targetTexture = rt1;
        cam.Render();
        cam.targetTexture = prevTarget;

        Graphics.Blit(rt1, rt2);
        if (blurredBackgroundRT != null)
        {
            blurredBackgroundRT.Release();
        }
        blurredBackgroundRT = new RenderTexture(width, height, 0);
        blurredBackgroundRT.filterMode = FilterMode.Bilinear;
        Graphics.Blit(rt2, blurredBackgroundRT);

        RenderTexture.ReleaseTemporary(rt1);
        RenderTexture.ReleaseTemporary(rt2);

        if (backgroundRawImage != null)
        {
            backgroundRawImage.texture = blurredBackgroundRT;
            backgroundRawImage.gameObject.SetActive(true);
        }
    }

    private InspectionUI CreateFallbackUI()
    {
        GameObject canvasObj = new GameObject("InspectionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject panel = new GameObject("InspectionPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.AddComponent<CanvasGroup>();

        InspectionUI ui = canvasObj.AddComponent<InspectionUI>();
        ui.inspectionPanel = panel;

        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -60);
        titleRect.sizeDelta = new Vector2(600, 50);
        var titleTmp = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
        titleTmp.fontSize = 28;
        titleTmp.fontStyle = TMPro.FontStyles.Bold;
        titleTmp.color = Color.white;
        ui.titleText = titleTmp;

        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(panel.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.5f, 1f);
        descRect.anchorMax = new Vector2(0.5f, 1f);
        descRect.anchoredPosition = new Vector2(0, -110);
        descRect.sizeDelta = new Vector2(700, 60);
        var descTmp = descObj.AddComponent<TMPro.TextMeshProUGUI>();
        descTmp.alignment = TMPro.TextAlignmentOptions.Center;
        descTmp.fontSize = 18;
        descTmp.color = new Color(0.85f, 0.85f, 0.85f);
        ui.descriptionText = descTmp;

        GameObject statusObj = new GameObject("StatusLogText");
        statusObj.transform.SetParent(panel.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0, 100);
        statusRect.sizeDelta = new Vector2(800, 60);
        var statusTmp = statusObj.AddComponent<TMPro.TextMeshProUGUI>();
        statusTmp.alignment = TMPro.TextAlignmentOptions.Center;
        statusTmp.fontSize = 18;
        statusTmp.color = Color.white;
        ui.statusLogText = statusTmp;

        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(panel.transform, false);
        var closeRect = closeObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0, 45);
        closeRect.sizeDelta = new Vector2(240, 45);
        var closeImg = closeObj.AddComponent<UnityEngine.UI.Image>();
        closeImg.color = new Color(0.2f, 0.2f, 0.25f, 0.85f);
        var closeBtn = closeObj.AddComponent<UnityEngine.UI.Button>();
        closeBtn.onClick.AddListener(CloseInspection);

        GameObject closeTextObj = new GameObject("Text");
        closeTextObj.transform.SetParent(closeObj.transform, false);
        var ctRect = closeTextObj.AddComponent<RectTransform>();
        ctRect.anchorMin = Vector2.zero;
        ctRect.anchorMax = Vector2.one;
        ctRect.offsetMin = Vector2.zero;
        ctRect.offsetMax = Vector2.zero;
        var closeTmp = closeTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        closeTmp.text = "[Q] Kembali";
        closeTmp.alignment = TMPro.TextAlignmentOptions.Center;
        closeTmp.fontSize = 18;
        closeTmp.color = Color.white;
        ui.closeButton = closeBtn;
        ui.closeButtonText = closeTmp;

        GameObject takeObj = new GameObject("CollectButton");
        takeObj.transform.SetParent(panel.transform, false);
        var takeRect = takeObj.AddComponent<RectTransform>();
        takeRect.anchorMin = new Vector2(0.5f, 0f);
        takeRect.anchorMax = new Vector2(0.5f, 0f);
        takeRect.anchoredPosition = new Vector2(110, 45);
        takeRect.sizeDelta = new Vector2(200, 45);
        var takeImg = takeObj.AddComponent<UnityEngine.UI.Image>();
        takeImg.color = new Color(0.18f, 0.40f, 0.25f, 0.9f);
        var takeBtn = takeObj.AddComponent<UnityEngine.UI.Button>();
        takeBtn.onClick.AddListener(TryCollectActiveClue);

        GameObject takeTextObj = new GameObject("Text");
        takeTextObj.transform.SetParent(takeObj.transform, false);
        var ttRect = takeTextObj.AddComponent<RectTransform>();
        ttRect.anchorMin = Vector2.zero;
        ttRect.anchorMax = Vector2.one;
        ttRect.offsetMin = Vector2.zero;
        ttRect.offsetMax = Vector2.zero;
        var takeTmp = takeTextObj.AddComponent<TMPro.TextMeshProUGUI>();
        takeTmp.text = "[F] Ambil";
        takeTmp.alignment = TMPro.TextAlignmentOptions.Center;
        takeTmp.fontSize = 18;
        takeTmp.color = Color.white;
        ui.collectButton = takeBtn;
        ui.collectButtonText = takeTmp;

        return ui;
    }

    private void SetupInspectionModel(InteractableObject clue)
    {

        if (spawnedInspectionModel != null)
        {
            Destroy(spawnedInspectionModel);
        }

        if (modelHolder != null)
        {
            modelHolder.localRotation = Quaternion.identity;
        }
        else if (studioPivot != null)
        {
            studioPivot.rotation = Quaternion.identity;
        }
        currentHotspots.Clear();

        Transform parentTransform = modelHolder != null ? modelHolder : (studioPivot != null ? studioPivot : transform);

        GameObject sourceObject = clue.inspectionModelPrefab != null ? clue.inspectionModelPrefab : clue.gameObject;
        spawnedInspectionModel = Instantiate(sourceObject, parentTransform.position, parentTransform.rotation, parentTransform);
        spawnedInspectionModel.transform.localPosition = Vector3.zero;
        spawnedInspectionModel.transform.localRotation = Quaternion.identity;

        // Pusatkan model secara otomatis agar titik putar (pivot) tepat berada di tengah objek (bukan di kaki tiang tanah)
        Renderer[] renderers = spawnedInspectionModel.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer) continue;
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 targetCenter = combinedBounds.center;
            if (clue.targetPivot != null)
            {
                Vector3 localPivot = clue.transform.InverseTransformPoint(clue.targetPivot.position);
                targetCenter = spawnedInspectionModel.transform.TransformPoint(localPivot);
            }

            Vector3 centerOffset = targetCenter - parentTransform.position;
            spawnedInspectionModel.transform.position = parentTransform.position - centerOffset;
        }

        var rigidbodies = spawnedInspectionModel.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var colliders = spawnedInspectionModel.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {

            if (!col.isTrigger) col.enabled = false;
        }

        var hotspots = spawnedInspectionModel.GetComponentsInChildren<AnomalyHotspot>();
        foreach (var hs in hotspots)
        {
            hs.ResetState();
            hs.OnDiscovered += OnHotspotDiscovered;
            hs.OnDwellProgressChanged += OnHotspotDwellProgress;
            currentHotspots.Add(hs);
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInspection();
            return;
        }

        if (isTransitioning) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryCollectActiveClue();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            angularVelocity = new Vector2(mouseX, mouseY) * (rotationSpeed * 2.5f);
        }
        else
        {
            angularVelocity = Vector2.Lerp(angularVelocity, Vector2.zero, Time.unscaledDeltaTime * dampingRate);
        }
    }

    private void ApplyGyroscopicRotation()
    {
        Transform targetToRotate = modelHolder != null ? modelHolder : (spawnedInspectionModel != null ? spawnedInspectionModel.transform : null);
        if (targetToRotate == null || angularVelocity.sqrMagnitude < 0.0001f) return;

        Camera cam = inspectionCamera != null ? inspectionCamera : Camera.main;
        if (cam == null) return;

        Vector3 camUp = cam.transform.up;
        Vector3 camRight = cam.transform.right;

        Quaternion rotY = Quaternion.AngleAxis(-angularVelocity.x, camUp);
        Quaternion rotX = Quaternion.AngleAxis(angularVelocity.y, camRight);

        targetToRotate.rotation = (rotX * rotY) * targetToRotate.rotation;
    }

    private void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom -= scroll * zoomSensitivity;
            currentZoom = Mathf.Clamp(currentZoom, minZoomDistance, maxZoomDistance);
            UpdateCameraPosition();
        }
    }

    private void UpdateCameraPosition()
    {
        if (inspectionCamera == null) return;
        Vector3 centerPos = studioPivot != null ? studioPivot.position : transform.position;
        inspectionCamera.transform.position = centerPos - (inspectionCamera.transform.forward * currentZoom);
    }

    private void EvaluateHotspots()
    {
        if (inspectionCamera == null || currentHotspots.Count == 0) return;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        bool anyFocused = false;

        foreach (var hs in currentHotspots)
        {
            if (hs.EvaluateObservation(inspectionCamera, screenCenter))
            {
                anyFocused = true;
                break;
            }
        }

        if (!anyFocused && inspectionUI != null)
        {
            inspectionUI.UpdateDwellProgress(0f);
        }
    }

    private void OnHotspotDwellProgress(float progress)
    {
        if (inspectionUI != null)
        {
            inspectionUI.UpdateDwellProgress(progress);
        }
    }

    private void OnHotspotDiscovered(AnomalyHotspot hotspot)
    {
        Debug.Log($"[ClueInspector] Anomali terdeteksi: {hotspot.anomalyTitle}");
        if (inspectionUI != null)
        {
            inspectionUI.OnHotspotIdentified(hotspot);
        }

        if (JournalManager.Instance != null && activeClue != null)
        {
            ClueEvidenceType effectiveType = hotspot.evidenceType != ClueEvidenceType.General
                ? hotspot.evidenceType
                : activeClue.GetEffectiveEvidenceType();

            JournalManager.Instance.RecordEvidence(
                activeClue.clueName,
                activeClue.category.ToString(),
                $"{hotspot.anomalyTitle}: {hotspot.anomalyObservationLog}",
                effectiveType,
                false
            );
        }
    }

    private System.Collections.IEnumerator EntranceMotionRoutine(float targetZoom)
    {
        isTransitioning = true;

        // Motion masuk: Objek meluncur halus dari bawah-depan, membesar sedikit, dan berotasi halus ke orientasi default
        Vector3 startPos = new Vector3(0f, -0.32f, 0.35f);
        Vector3 endPos = Vector3.zero;

        Vector3 startScale = Vector3.one * 0.72f;
        Vector3 endScale = Vector3.one;

        Quaternion startRot = Quaternion.Euler(14f, -22f, 3.5f);
        Quaternion endRot = Quaternion.identity;

        float startZoom = targetZoom * Mathf.Min(1.08f, maxZoomRatio);

        if (modelHolder != null)
        {
            modelHolder.localPosition = startPos;
            modelHolder.localScale = startScale;
            modelHolder.localRotation = startRot;
        }

        currentZoom = startZoom;
        UpdateCameraPosition();

        Color startBgColor = new Color(0.70f, 0.72f, 0.76f, 0.95f);
        if (backgroundRawImage != null)
        {
            backgroundRawImage.color = startBgColor;
            backgroundRawImage.gameObject.SetActive(true);
        }

        if (inspectionUI != null)
        {
            inspectionUI.SetAlpha(0f);
        }

        float elapsed = 0f;
        while (elapsed < entranceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / entranceDuration);

            // Ease-out cubic curve: gerakan responsif di awal dan melambat secara lembut saat mendarat
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);

            if (modelHolder != null)
            {
                modelHolder.localPosition = Vector3.Lerp(startPos, endPos, easeOut);
                modelHolder.localScale = Vector3.Lerp(startScale, endScale, easeOut);
                modelHolder.localRotation = Quaternion.Slerp(startRot, endRot, easeOut);
            }

            currentZoom = Mathf.Lerp(startZoom, targetZoom, easeOut);
            UpdateCameraPosition();

            if (backgroundRawImage != null)
            {
                backgroundRawImage.color = Color.Lerp(startBgColor, bgTargetColor, easeOut);
            }

            if (inspectionUI != null)
            {
                // UI memudar masuk sedikit tertunda (delay 15%) agar fokus awal tertuju ke objek 3D
                float uiProgress = Mathf.Clamp01((t - 0.15f) / 0.85f);
                float uiEase = 1f - Mathf.Pow(1f - uiProgress, 2f);
                inspectionUI.SetAlpha(uiEase);
            }

            yield return null;
        }

        if (modelHolder != null)
        {
            modelHolder.localPosition = endPos;
            modelHolder.localScale = endScale;
            modelHolder.localRotation = endRot;
        }
        currentZoom = targetZoom;
        UpdateCameraPosition();

        if (backgroundRawImage != null)
        {
            backgroundRawImage.color = bgTargetColor;
        }

        if (inspectionUI != null)
        {
            inspectionUI.SetAlpha(1f);
        }

        isTransitioning = false;
        activeTransitionCoroutine = null;
    }

    public void TryCollectActiveClue()
    {
        if (activeClue != null && activeClue.isCollectable)
        {
            InteractableObject clueToCollect = activeClue;
            if (activeTransitionCoroutine != null)
            {
                StopCoroutine(activeTransitionCoroutine);
            }
            FinalizeCloseInspection();
            clueToCollect.Collect();
        }
    }

    public void SaveAndClose()
    {
        if (JournalManager.Instance != null && activeClue != null)
        {
            JournalManager.Instance.RecordEvidence(
                activeClue.clueName,
                activeClue.category.ToString(),
                activeClue.clueDescription,
                activeClue.GetEffectiveEvidenceType(),
                false
            );
        }
        CloseInspection();
    }

    public void CloseInspection()
    {
        if (!isInspecting || isClosing) return;

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        activeTransitionCoroutine = StartCoroutine(ExitMotionRoutine());
    }

    private System.Collections.IEnumerator ExitMotionRoutine()
    {
        isClosing = true;
        isTransitioning = true;

        Vector3 startPos = modelHolder != null ? modelHolder.localPosition : Vector3.zero;
        Vector3 endPos = startPos + new Vector3(0f, -0.25f, 0.25f);

        Vector3 startScale = modelHolder != null ? modelHolder.localScale : Vector3.one;
        Vector3 endScale = startScale * 0.78f;

        float startZoom = currentZoom;
        float endZoom = currentZoom * 1.15f;

        Color startBgColor = backgroundRawImage != null ? backgroundRawImage.color : bgTargetColor;
        Color endBgColor = new Color(0.70f, 0.72f, 0.76f, 0f);

        float elapsed = 0f;
        while (elapsed < exitDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / exitDuration);

            // Ease-in quad curve
            float easeIn = t * t;

            if (modelHolder != null)
            {
                modelHolder.localPosition = Vector3.Lerp(startPos, endPos, easeIn);
                modelHolder.localScale = Vector3.Lerp(startScale, endScale, easeIn);
            }

            currentZoom = Mathf.Lerp(startZoom, endZoom, easeIn);
            UpdateCameraPosition();

            if (backgroundRawImage != null)
            {
                backgroundRawImage.color = Color.Lerp(startBgColor, endBgColor, easeIn);
            }

            if (inspectionUI != null)
            {
                inspectionUI.SetAlpha(1f - easeIn);
            }

            yield return null;
        }

        FinalizeCloseInspection();
        isClosing = false;
        isTransitioning = false;
        activeTransitionCoroutine = null;
    }

    private void FinalizeCloseInspection()
    {
        isInspecting = false;

        if (spawnedInspectionModel != null)
        {
            Destroy(spawnedInspectionModel);
        }

        if (modelHolder != null)
        {
            modelHolder.localPosition = Vector3.zero;
            modelHolder.localScale = Vector3.one;
            modelHolder.localRotation = Quaternion.identity;
        }

        if (studioPivot != null)
        {
            studioPivot.rotation = Quaternion.identity;
        }

        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(false);
        if (threePointLightingSetup != null) threePointLightingSetup.SetActive(false);
        if (inspectionUI != null) inspectionUI.Hide();

        if (RadioDialogueManager.Instance != null)
        {
            RadioDialogueManager.Instance.DismissDialogueImmediate();
        }

        if (backgroundRawImage != null)
        {
            backgroundRawImage.gameObject.SetActive(false);
            backgroundRawImage.texture = null;
        }

        if (blurredBackgroundRT != null)
        {
            blurredBackgroundRT.Release();
            Destroy(blurredBackgroundRT);
            blurredBackgroundRT = null;
        }

        if (cachedMainCamera != null)
        {
            cachedMainCamera.gameObject.SetActive(true);
            cachedMainCamera = null;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Exploration);
        }
    }
}
