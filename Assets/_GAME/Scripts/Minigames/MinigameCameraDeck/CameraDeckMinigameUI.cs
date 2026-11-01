using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Overlay for the three Camera Deck minigames: an opaque security-desk screen with the
    /// CCTV feed on the left, the live floor plan on the right, and a thin HUD. Both feeds
    /// are <see cref="RawImage"/>s showing the render textures the world creates at
    /// runtime; every pointer event inside a feed is converted to a 0..1 UV and forwarded
    /// to <see cref="CameraDeckMinigameWorld"/>, which owns all the picking math - this
    /// class stays a pure readout plus input relay.
    ///
    /// Camera-select buttons are generated procedurally (one per mount, no asset
    /// dependencies), and the mode decides the objective line, available controls and
    /// opening instruction. Timer and score are drawn by the shared MinigameMainUI.
    /// </summary>
    public class CameraDeckMinigameUI : MinigameUI
    {
        [Header("Feeds")]
        [SerializeField, Tooltip("Left view: the CCTV render texture.")]
        private RawImage cctvImage;
        [SerializeField, Tooltip("Right view: the floor-plan render texture.")]
        private RawImage planImage;
        [SerializeField, Tooltip("Static overlay flashed over the CCTV feed while switching cameras.")]
        private Image noiseOverlay;
        [SerializeField, Tooltip("'CAM-03 • SERVER ROOM  REC' strip over the CCTV feed.")]
        private TextMeshProUGUI camLabelTMP;
        [SerializeField, Tooltip("Spot description line shown while aiming at an inspectable spot (Clean Desk).")]
        private TextMeshProUGUI hoverTMP;

        [Header("HUD")]
        [SerializeField, Tooltip("Mode objective, e.g. 'COVERAGE 2/3  •  BUDGET 3/4'.")]
        private TextMeshProUGUI objectiveTMP;
        [SerializeField, Tooltip("Row the integrity pips are spawned into.")]
        private RectTransform integrityPipsRow;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField, Tooltip("Row the procedural camera-select buttons are spawned into.")]
        private RectTransform cameraButtonsRow;
        [SerializeField, Tooltip("RUN AUDIT button used only by Blind Spot Audit.")]
        private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonTMP;

        [Header("Colours")]
        [SerializeField] private Color pipFullColor = new Color(0.21f, 0.88f, 1f);
        [SerializeField] private Color pipEmptyColor = new Color(0.16f, 0.25f, 0.34f);
        [SerializeField] private Color infoColor = new Color(0.55f, 0.90f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.60f);
        [SerializeField] private Color buttonIdleColor = new Color(0.1f, 0.18f, 0.28f);
        [SerializeField] private Color buttonActiveColor = new Color(0.21f, 0.88f, 1f);

        private readonly List<Image> pips = new List<Image>();
        private readonly List<Image> cameraButtons = new List<Image>();
        private readonly List<TextMeshProUGUI> cameraButtonLabels = new List<TextMeshProUGUI>();

        private CameraDeckMinigameWorld world;
        private Canvas canvas;
        private RectTransform cameraButtonsParent;
        private RectTransform auditControlsRow;
        private Button auditInstallButton;
        private Button auditRotateLeftButton;
        private Button auditRotateRightButton;
        private TextMeshProUGUI auditInstallLabel;
        private Vector2 lastCanvasSize;
        private int lastPipsIntegrity = -1;
        private int lastPipsMax = -1;
        private bool initialised;

        private CameraDeckMinigame Minigame => CameraDeckMinigame.MinigameInstance;

        private void Start()
        {
            if (Minigame == null)
                return;

            // Loud reference audit - a broken hand-authored binding must never fail silently.
            ReportIfMissing(cctvImage, "cctvImage");
            ReportIfMissing(planImage, "planImage");
            ReportIfMissing(noiseOverlay, "noiseOverlay");
            ReportIfMissing(camLabelTMP, "camLabelTMP");
            ReportIfMissing(hoverTMP, "hoverTMP");
            ReportIfMissing(objectiveTMP, "objectiveTMP");
            ReportIfMissing(integrityPipsRow, "integrityPipsRow");
            ReportIfMissing(feedbackTMP, "feedbackTMP");
            ReportIfMissing(cameraButtonsRow, "cameraButtonsRow");
            ReportIfMissing(actionButton, "actionButton");
            ReportIfMissing(actionButtonTMP, "actionButtonTMP");

            world = Minigame.GetComponent<CameraDeckMinigameWorld>();
            canvas = GetComponentInParent<Canvas>();
            cameraButtonsParent = cameraButtonsRow;

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(() =>
                {
                    if (world != null)
                        world.TriggerAction();
                });
            }

            if (Minigame.Mode == CameraDeckMode.BlindRoute)
                ApplyPlanOnlyLayout();
            else if (Minigame.Mode == CameraDeckMode.BlindSpotAudit)
                ApplyAuditControlsLayout();

            initialised = true;
            ShowFeedback(OpeningInstruction(), false);
        }

        /// <summary>
        /// Blind Route has no CCTV feed: the plan takes the whole screen, the camera-select
        /// row goes away (there is nothing to select), and the HUD switches to the red-team
        /// palette that matches the world.
        /// </summary>
        private void ApplyPlanOnlyLayout()
        {
            if (cctvImage != null)
                cctvImage.gameObject.SetActive(false);

            if (cameraButtonsRow != null)
                cameraButtonsRow.gameObject.SetActive(false);

            if (planImage != null)
            {
                RectTransform rect = planImage.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, -6f);
            }

            RefreshPlanOnlySize();

            infoColor = new Color(1f, 0.82f, 0.45f);
            warnColor = new Color(1f, 0.42f, 0.3f);
            pipFullColor = new Color(1f, 0.6f, 0.25f);
            pipEmptyColor = new Color(0.32f, 0.2f, 0.14f);

            if (objectiveTMP != null)
                objectiveTMP.color = infoColor;

            if (camLabelTMP != null)
                camLabelTMP.gameObject.SetActive(false);

            if (hoverTMP != null)
                hoverTMP.gameObject.SetActive(false);
        }

        private void RefreshPlanOnlySize()
        {
            if (planImage == null || canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                return;

            Vector2 size = canvasRect.rect.size;
            if ((size - lastCanvasSize).sqrMagnitude < 0.25f)
                return;

            lastCanvasSize = size;
            const float horizontalMargin = 60f;
            const float topReserved = 145f;
            const float bottomReserved = 135f;
            float availableWidth = Mathf.Max(320f, size.x - horizontalMargin * 2f);
            float availableHeight = Mathf.Max(320f, size.y - topReserved - bottomReserved);
            float side = Mathf.Min(availableWidth, availableHeight);
            planImage.rectTransform.sizeDelta = new Vector2(side, side);
            planImage.rectTransform.anchoredPosition = new Vector2(0f, (bottomReserved - topReserved) * 0.5f);
        }

        private void ReportIfMissing(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogError($"[CameraDeck] UI reference '{fieldName}' is not assigned - the HUD will misbehave. Fix the CameraDeckMinigameUI prefab.");
        }

        private string OpeningInstruction()
        {
            switch (Minigame.Mode)
            {
                case CameraDeckMode.BlindSpotAudit:
                    return $"GOAL: reach rating {Minigame.AuditPassingRating}. Select a mount, install a camera, set its orientation, then run all threat routes.";
                case CameraDeckMode.CleanDeskSweep:
                    return $"GOAL: report all {Minigame.TotalViolations} {Minigame.TargetLabel}S. Inspect the facts and context before clicking a neutral marker.";
                default:
                    return "GOAL: reach the yellow asset unseen. Left-click a destination to move. Right-click or press Space to stop in cover.";
            }
        }

        private void Update()
        {
            if (initialised == false || Minigame == null || world == null)
                return;

            // The render textures are created in the world's Start, which may land a frame
            // after ours - keep polling until both feeds are wired.
            if (cctvImage != null && cctvImage.texture == null && world.CctvTexture != null)
                cctvImage.texture = world.CctvTexture;

            if (planImage != null && planImage.texture == null && world.PlanTexture != null)
                planImage.texture = world.PlanTexture;

            if (Minigame.Mode == CameraDeckMode.BlindRoute)
                RefreshPlanOnlySize();

            EnsureCameraButtons();
            RefreshHud();

            if (Minigame.IsPaused || Minigame.IsFinished)
                return;

            HandlePointer();
        }

        // ------------------------------------------------------------ pointer relay

        private void HandlePointer()
        {
            Vector2 mouse = Input.mousePosition;

            if (TryFeedUv(cctvImage, mouse, out Vector2 cctvUv))
            {
                world.HoverCctv(cctvUv);

                if (Input.GetMouseButtonDown(0))
                    world.ClickCctv(cctvUv);
            }
            else
            {
                world.HoverCctv(new Vector2(-1f, -1f));
            }

            if (TryFeedUv(planImage, mouse, out Vector2 planUv))
            {
                if (Input.GetMouseButtonDown(0))
                    world.ClickPlan(planUv);

                if (Input.GetMouseButtonDown(1))
                    world.RightClickPlan();
            }
        }

        private bool TryFeedUv(RawImage feed, Vector2 screenPoint, out Vector2 uv)
        {
            uv = Vector2.zero;

            if (feed == null)
                return false;

            RectTransform rect = feed.rectTransform;
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out Vector2 local) == false)
                return false;

            uv = Rect.PointToNormalized(rect.rect, local);
            return uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f;
        }

        // ------------------------------------------------------------------- HUD

        /// <summary>Event line pushed by the world (installs, reports, detections, runs).</summary>
        public void ShowFeedback(string message, bool isWarning)
        {
            if (feedbackTMP == null)
                return;

            feedbackTMP.text = message;
            feedbackTMP.color = isWarning ? warnColor : infoColor;
        }

        private void RefreshHud()
        {
            if (camLabelTMP != null)
            {
                // An uninstalled mount still shows its would-be view: that preview is how the
                // auditor judges a placement before spending budget on it.
                string offline = Minigame.Mode == CameraDeckMode.BlindSpotAudit
                    ? "  •  PREVIEW (no camera here)"
                    : "  •  OFFLINE TONIGHT";
                string state = world.SwitchNoise01 > 0.2f
                    ? "  •  SIGNAL..."
                    : world.ActiveCameraInstalled ? "  •  REC ●" : offline;
                camLabelTMP.text = $"CAM-{world.ActiveCameraIndex + 1:00}  {world.ActiveCameraLabel}{state}";
            }

            if (noiseOverlay != null)
            {
                Color color = noiseOverlay.color;
                color.a = world.SwitchNoise01 * 0.92f;
                noiseOverlay.color = color;
            }

            if (hoverTMP != null)
            {
                string hovered = world.HoveredSpotDescription;
                hoverTMP.text = string.IsNullOrEmpty(hovered) ? string.Empty : $"> {hovered}";
            }

            if (objectiveTMP != null)
                objectiveTMP.text = BuildObjectiveLine();

            if (actionButton != null)
            {
                bool wanted = Minigame.Mode == CameraDeckMode.BlindSpotAudit;
                actionButton.gameObject.SetActive(wanted);
                actionButton.interactable = world.ActionAvailable;

                if (actionButtonTMP != null)
                    actionButtonTMP.text = world.ActionLabel;
            }

            RefreshCameraButtons();
            RefreshAuditControls();
            RefreshIntegrityPips();
        }

        private string BuildObjectiveLine()
        {
            switch (Minigame.Mode)
            {
                case CameraDeckMode.BlindSpotAudit:
                    return $"{Minigame.FacilityLabel}   •   RATING {Minigame.AuditRating}/{Minigame.AuditPassingRating}   •   COVERAGE {Minigame.CoveredTargets}/{Minigame.TotalTargets}   •   CAMERAS {world.InstalledCount}/{Minigame.CameraBudget}";
                case CameraDeckMode.CleanDeskSweep:
                    return $"{Minigame.FacilityLabel}   •   {Minigame.TargetLabel}S {Minigame.Reported}/{Minigame.TotalViolations}   •   FALSE REPORTS {Minigame.FalseReports}";
                default:
                    return $"{Minigame.FacilityLabel}   •   LIVE MOVEMENT   •   ATTEMPTS {Minigame.CurrentIntegrity}/{Minigame.MaxIntegrity}";
            }
        }

        // ------------------------------------------------------- procedural widgets

        private void EnsureCameraButtons()
        {
            if (cameraButtonsRow == null || cameraButtonsRow.gameObject.activeSelf == false)
                return;

            for (int i = cameraButtons.Count; i < world.CameraCount; i++)
                CreateCameraSelectButton(i);

            if (Minigame.Mode == CameraDeckMode.BlindSpotAudit && auditControlsRow == null)
                CreateAuditControls();
        }

        private void RefreshCameraButtons()
        {
            for (int i = 0; i < cameraButtons.Count; i++)
            {
                bool isActive = i == world.ActiveCameraIndex;
                cameraButtons[i].color = isActive ? buttonActiveColor : buttonIdleColor;

                if (i < cameraButtonLabels.Count)
                    cameraButtonLabels[i].color = isActive ? Color.black : infoColor;
            }
        }

        private void RefreshAuditControls()
        {
            if (auditControlsRow == null)
                return;

            bool canEdit = Minigame.IsFinished == false && world.IsRunActive == false;
            bool installed = world.ActiveCameraInstalled;
            auditInstallLabel.text = installed ? "REMOVE CAMERA" : "INSTALL CAMERA";
            auditInstallButton.interactable = canEdit
                && (installed || world.InstalledCount < Minigame.CameraBudget);
            auditRotateLeftButton.interactable = canEdit;
            auditRotateRightButton.interactable = canEdit;
        }

        private void CreateCameraSelectButton(int index)
        {
            RectTransform parent = cameraButtonsParent != null ? cameraButtonsParent : cameraButtonsRow;
            GameObject buttonGo = new GameObject($"CamButton{index + 1}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.layer = parent.gameObject.layer;
            RectTransform buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.SetParent(parent, false);
            buttonRect.sizeDelta = new Vector2(52f, 40f);

            Image image = buttonGo.GetComponent<Image>();
            image.color = buttonIdleColor;
            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => world.SelectCamera(index));

            TextMeshProUGUI label = CreateRuntimeLabel(buttonRect, "Label", Vector2.zero, Vector2.one);
            label.text = (index + 1).ToString();
            label.fontSize = Minigame.Mode == CameraDeckMode.BlindSpotAudit ? 12f : 20f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;

            cameraButtons.Add(image);
            cameraButtonLabels.Add(label);
        }

        private void ApplyAuditControlsLayout()
        {
            if (cameraButtonsRow == null)
                return;

            cameraButtonsRow.SetParent(transform, false);
            cameraButtonsRow.anchorMin = new Vector2(0.645f, 0.085f);
            cameraButtonsRow.anchorMax = new Vector2(0.985f, 0.18f);
            cameraButtonsRow.anchoredPosition = Vector2.zero;
            cameraButtonsRow.sizeDelta = Vector2.zero;

            HorizontalLayoutGroup horizontal = cameraButtonsRow.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
                horizontal.enabled = false;

            GameObject gridGo = new GameObject("AuditCameraGrid",
                typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.layer = cameraButtonsRow.gameObject.layer;
            RectTransform gridRect = (RectTransform)gridGo.transform;
            gridRect.SetParent(cameraButtonsRow, false);
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = Vector2.zero;
            cameraButtonsParent = gridRect;

            GridLayoutGroup grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(32f, 24f);
            grid.spacing = new Vector2(6f, 4f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 7;
        }

        private void CreateAuditControls()
        {
            GameObject rowGo = new GameObject("AuditCameraControls", typeof(RectTransform));
            rowGo.layer = gameObject.layer;
            auditControlsRow = (RectTransform)rowGo.transform;
            auditControlsRow.SetParent(transform, false);
            auditControlsRow.anchorMin = new Vector2(0.015f, 0.095f);
            auditControlsRow.anchorMax = new Vector2(0.625f, 0.175f);
            auditControlsRow.anchoredPosition = Vector2.zero;
            auditControlsRow.sizeDelta = Vector2.zero;

            auditRotateLeftButton = CreateAuditControlButton("RotateLeft", "ROTATE LEFT",
                0.09f, 112f, () => world.RotateActiveCamera(-1), out _);
            auditInstallButton = CreateAuditControlButton("InstallCamera", "INSTALL CAMERA",
                0.5f, 138f, () => world.ToggleActiveCameraInstallation(), out auditInstallLabel);
            auditRotateRightButton = CreateAuditControlButton("RotateRight", "ROTATE RIGHT",
                0.91f, 112f, () => world.RotateActiveCamera(1), out _);
        }

        private Button CreateAuditControlButton(string objectName, string text,
            float normalizedX, float width, UnityEngine.Events.UnityAction action,
            out TextMeshProUGUI label)
        {
            GameObject buttonGo = new GameObject(objectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.layer = auditControlsRow.gameObject.layer;
            RectTransform buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.SetParent(auditControlsRow, false);
            buttonRect.anchorMin = new Vector2(normalizedX, 0.5f);
            buttonRect.anchorMax = new Vector2(normalizedX, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(width, 34f);

            Image image = buttonGo.GetComponent<Image>();
            image.color = buttonIdleColor;
            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);

            label = CreateRuntimeLabel(buttonRect, "Label", Vector2.zero, Vector2.one);
            label.text = text;
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private TextMeshProUGUI CreateRuntimeLabel(RectTransform parent, string objectName,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject labelGo = new GameObject(objectName,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.layer = parent.gameObject.layer;
            RectTransform labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(parent, false);
            labelRect.anchorMin = anchorMin;
            labelRect.anchorMax = anchorMax;
            labelRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.color = infoColor;
            label.raycastTarget = false;
            return label;
        }

        private void RefreshIntegrityPips()
        {
            int integrity = Minigame.CurrentIntegrity;
            int max = Minigame.MaxIntegrity;

            if (integrity == lastPipsIntegrity && max == lastPipsMax)
                return;

            lastPipsIntegrity = integrity;
            lastPipsMax = max;

            if (integrityPipsRow == null)
                return;

            // Pips are plain procedural Images - no asset dependencies.
            while (pips.Count < max)
            {
                GameObject pipGo = new GameObject("Pip",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pipGo.layer = integrityPipsRow.gameObject.layer;

                RectTransform pipRt = (RectTransform)pipGo.transform;
                pipRt.SetParent(integrityPipsRow, false);
                pipRt.sizeDelta = new Vector2(18f, 28f);

                Image pip = pipGo.GetComponent<Image>();
                pip.raycastTarget = false;
                pips.Add(pip);
            }

            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].gameObject.SetActive(i < max);
                pips[i].color = i < integrity ? pipFullColor : pipEmptyColor;
            }
        }

        public override void UpdateUI()
        {
            // Called once on the source prefab before the live copy exists; safe to no-op
            // until this instance has run its own Start.
            if (initialised)
                RefreshHud();
        }
    }
}
