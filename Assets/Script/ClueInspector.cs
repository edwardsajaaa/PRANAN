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

    [Header("Inspection Studio Setup (W1-08)")]
    public Camera inspectionCamera;
    public Transform studioPivot;
    public GameObject threePointLightingSetup;
    public InspectionUI inspectionUI;

    [Header("Gyroscopic Drag & Damping Engine (W1-09)")]
    public float rotationSpeed = 5f;
    [Tooltip("Laju deselerasi putaran saat klik mouse dilepas (Damping)")]
    public float dampingRate = 4f;

    [Header("Zoom Settings")]
    public float zoomSensitivity = 0.5f;
    public float minZoomDistance = 0.8f;
    public float maxZoomDistance = 6.0f;
    public float defaultZoomDistance = 2.5f;

    private InteractableObject activeClue;
    private GameObject spawnedInspectionModel;
    private List<AnomalyHotspot> currentHotspots = new List<AnomalyHotspot>();

    private Vector2 angularVelocity;
    private float currentZoom;
    private bool isDragging = false;
    private bool isInspecting = false;
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
        ApplyGyroscopicRotation();
        HandleZoom();
        EvaluateHotspots();
    }

    public void StartInspection(InteractableObject clue)
    {
        if (clue == null) return;
        EnsureStudioReady();
        activeClue = clue;
        isInspecting = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Inspection);
        }

        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(true);
        if (threePointLightingSetup != null) threePointLightingSetup.SetActive(true);

        CaptureBlurredBackground();

        if (Camera.main != null && Camera.main != inspectionCamera)
        {
            cachedMainCamera = Camera.main;
            cachedMainCamera.gameObject.SetActive(false);
        }

        SetupInspectionModel(clue);

        angularVelocity = Vector2.zero;
        currentZoom = CalculateIdealZoom(spawnedInspectionModel);
        UpdateCameraPosition();

        if (inspectionUI != null)
        {
            inspectionUI.Show(clue);
        }
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
        minZoomDistance = Mathf.Max(0.5f, idealDist * 0.4f);
        maxZoomDistance = Mathf.Max(minZoomDistance + 1f, idealDist * 2.2f);
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

            backgroundRawImage.color = new Color(0.45f, 0.48f, 0.52f, 0.88f);
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
        currentHotspots.Clear();

        Transform parentTransform = studioPivot != null ? studioPivot : transform;

        GameObject sourceObject = clue.inspectionModelPrefab != null ? clue.inspectionModelPrefab : clue.gameObject;
        spawnedInspectionModel = Instantiate(sourceObject, parentTransform.position, parentTransform.rotation, parentTransform);
        spawnedInspectionModel.transform.localPosition = Vector3.zero;
        spawnedInspectionModel.transform.localRotation = Quaternion.identity;

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
            angularVelocity = new Vector2(mouseX, mouseY) * rotationSpeed;
        }
        else
        {

            angularVelocity = Vector2.Lerp(angularVelocity, Vector2.zero, Time.unscaledDeltaTime * dampingRate);
        }
    }

    private void ApplyGyroscopicRotation()
    {
        if (spawnedInspectionModel == null || angularVelocity.sqrMagnitude < 0.0001f) return;

        Camera cam = inspectionCamera != null ? inspectionCamera : Camera.main;
        if (cam == null) return;

        Vector3 camUp = cam.transform.up;
        Vector3 camRight = cam.transform.right;

        Quaternion rotY = Quaternion.AngleAxis(-angularVelocity.x, camUp);
        Quaternion rotX = Quaternion.AngleAxis(angularVelocity.y, camRight);

        spawnedInspectionModel.transform.rotation = (rotX * rotY) * spawnedInspectionModel.transform.rotation;
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
    }

    public void TryCollectActiveClue()
    {
        if (activeClue != null && activeClue.isCollectable)
        {
            InteractableObject clueToCollect = activeClue;
            CloseInspection();
            clueToCollect.Collect();
        }
    }

    public void SaveAndClose()
    {
        CloseInspection();
    }

    public void CloseInspection()
    {
        isInspecting = false;

        if (spawnedInspectionModel != null)
        {
            Destroy(spawnedInspectionModel);
        }

        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(false);
        if (threePointLightingSetup != null) threePointLightingSetup.SetActive(false);
        if (inspectionUI != null) inspectionUI.Hide();

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
