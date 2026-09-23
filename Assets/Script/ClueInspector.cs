using UnityEngine;
using System.Collections.Generic;

public class ClueInspector : MonoBehaviour
{
    public static ClueInspector Instance { get; private set; }

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
    public float minZoomDistance = 0.6f;
    public float maxZoomDistance = 2.5f;
    public float defaultZoomDistance = 1.2f;

    private InteractableObject activeClue;
    private GameObject spawnedInspectionModel;
    private List<AnomalyHotspot> currentHotspots = new List<AnomalyHotspot>();

    private Vector2 angularVelocity;
    private float currentZoom;
    private bool isDragging = false;
    private bool isInspecting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
        activeClue = clue;
        isInspecting = true;

        // 1. Ganti Game State ke Inspection & Freeze Time (W1-08 & W1-11)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Inspection);
        }

        // 2. Aktifkan Studio Kamera & Pencahayaan 3 Titik (Key, Fill, Back light)
        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(true);
        if (threePointLightingSetup != null) threePointLightingSetup.SetActive(true);

        // 3. Spawn model 3D di studio pivot
        SetupInspectionModel(clue);

        // 4. Reset rotasi dan zoom
        angularVelocity = Vector2.zero;
        currentZoom = defaultZoomDistance;
        UpdateCameraPosition();

        // 5. Tampilkan UI
        if (inspectionUI != null)
        {
            inspectionUI.Show(clue);
        }
    }

    private void SetupInspectionModel(InteractableObject clue)
    {
        // Bersihkan model sebelumnya jika ada
        if (spawnedInspectionModel != null)
        {
            Destroy(spawnedInspectionModel);
        }
        currentHotspots.Clear();

        Transform parentTransform = studioPivot != null ? studioPivot : transform;

        // Gunakan prefab model inspeksi jika disediakan, atau salinan dari objek dunia
        GameObject sourceObject = clue.inspectionModelPrefab != null ? clue.inspectionModelPrefab : clue.gameObject;
        spawnedInspectionModel = Instantiate(sourceObject, parentTransform.position, parentTransform.rotation, parentTransform);
        spawnedInspectionModel.transform.localPosition = Vector3.zero;
        spawnedInspectionModel.transform.localRotation = Quaternion.identity;

        // Nonaktifkan skrip interaksi atau collider fisik pada model studio agar tidak bentrok
        var colliders = spawnedInspectionModel.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            // Pertahankan trigger jika digunakan untuk hotspot, disable collider penghalang raycast
            if (!col.isTrigger) col.enabled = false;
        }

        // Cari semua AnomalyHotspot pada model inspeksi
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
        // Tombol Escape untuk menutup inspeksi
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInspection();
            return;
        }

        // Drag klik kiri mouse untuk memutar model
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
            // Deselerasi putaran lambat saat klik dilepas (Damping W1-09)
            angularVelocity = Vector2.Lerp(angularVelocity, Vector2.zero, Time.unscaledDeltaTime * dampingRate);
        }
    }

    /// <summary>
    /// Rotasi 360 derajat simultan pada sumbu X & Y berbasis Quaternion untuk mencegah gimbal lock (W1-09)
    /// </summary>
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

        // Kembalikan ke state Exploration (W1-11)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetState(GameState.Exploration);
        }
    }
}
