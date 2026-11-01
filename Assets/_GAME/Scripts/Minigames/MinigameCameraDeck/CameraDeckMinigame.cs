using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// Rules and scenario configuration for the three Camera Deck variants. Rendering,
    /// visibility, interaction and movement are delegated to dedicated runtime services.
    /// </summary>
    public sealed class CameraDeckMinigame : MinigameBehaviour
    {
        private static CameraDeckMinigame minigameInstance;

        [Header("Mode")]
        [SerializeField] private CameraDeckMode mode = CameraDeckMode.BlindSpotAudit;

        [Header("Facility")]
        [SerializeField] private string facilityId;
        [SerializeField] private string profileId;

        [Header("Integrity")]
        [SerializeField] private int integrity = 3;

        [Header("Cameras")]
        [SerializeField] private float visionRangeCells = 10.5f;
        [SerializeField] private float visionHalfAngleDegrees = 45f;
        [SerializeField] private float switchDelaySeconds = 0.35f;

        [Header("Blind Spot Audit")]
        [SerializeField] private int cameraBudget = 3;
        [SerializeField] private int passingRating = 80;
        [SerializeField] private float intruderSpeedCells = 5.2f;

        [Header("Clean Desk Sweep")]
        [SerializeField] private int violationCount = 6;
        [SerializeField] private int decoyCount = 4;

        [Header("Blind Route")]
        [SerializeField] private float sweepSpeedDegPerSec = 26f;
        [SerializeField] private float avatarSpeedCells = 2.6f;

        [Header("Scoring")]
        [SerializeField] private int pointsPerTarget = 120;
        [SerializeField] private int decoyPenalty = 90;
        [SerializeField] private int budgetBonusPerCamera = 80;
        [SerializeField] private int routePoints = 250;
        [SerializeField] private int timeBonusPerSecond = 10;

        [Header("Story skin")]
        [SerializeField] private string facilityLabel = "SECURITY SITE";
        [SerializeField] private string targetLabel = "TARGET";
        [SerializeField] private string decoyLabel = "COMPLIANT OBSERVATION";

        private float sweepSpeedMultiplier = 1f;
        private float playerSpeedMultiplier = 1f;
        private DeckLayout layout;
        private int maxIntegrity;
        private int currentIntegrity;
        private int resolvedViolations;
        private int resolvedDecoys;
        private bool finished;

        private int coveredTargets;
        private int totalTargets;
        private int auditRuns;
        private int auditDetectedRoutes;
        private int auditTotalRoutes;
        private int auditRating;
        private int reported;
        private int falseReports;
        private int timesSpotted;

        public static CameraDeckMinigame MinigameInstance
        {
            get
            {
                if (minigameInstance == null)
                    Debug.LogError("[CameraDeck] Active minigame instance does not exist.");
                return minigameInstance;
            }
        }

        public CameraDeckMode Mode => mode;
        public DeckLayout Layout => layout;
        public int MaxIntegrity => maxIntegrity;
        public int CurrentIntegrity => currentIntegrity;
        public bool IsFinished => finished;
        public float VisionRangeCells => visionRangeCells;
        public float VisionHalfAngleDegrees => visionHalfAngleDegrees;
        public float SwitchDelaySeconds => Mathf.Max(0f, switchDelaySeconds);
        public int CameraBudget => cameraBudget;
        public int AuditPassingRating => passingRating;
        public int AuditDetectedRoutes => auditDetectedRoutes;
        public int AuditTotalRoutes => auditTotalRoutes;
        public int AuditRating => auditRating;
        public float IntruderSpeedCells => intruderSpeedCells;
        public int CoveredTargets => coveredTargets;
        public int TotalTargets => totalTargets;
        public int TotalViolations => resolvedViolations;
        public int TotalDecoys => resolvedDecoys;
        public int Reported => reported;
        public int FalseReports => falseReports;
        public float SweepSpeedDegPerSec => sweepSpeedDegPerSec * sweepSpeedMultiplier;
        public float AvatarSpeedCells => avatarSpeedCells * playerSpeedMultiplier;
        public string FacilityLabel => string.IsNullOrWhiteSpace(facilityLabel) ? layout.name.ToUpperInvariant() : facilityLabel;
        public string TargetLabel => targetLabel;
        public string DecoyLabel => decoyLabel;
        public override string Summary => BuildSummary();

        private void Awake()
        {
            minigameInstance = this;
            ApplyDifficultyProfile();
            ResolveDerivedState();
        }

        private void OnDestroy()
        {
            if (minigameInstance == this)
                minigameInstance = null;
        }

        public override void ApplyConfig(TaskMinigameVariant config)
        {
            base.ApplyConfig(config);
            ResetTuningToPrefabDefaults();
            ApplyDifficultyProfile();

            CameraDeckMinigameSettings settings = config != null ? config.cameraDeckSettings : null;
            if (settings != null)
            {
                facilityId = settings.facilityId;
                profileId = settings.profileId;

                if (settings.integrity >= 0)
                    integrity = Mathf.Max(1, settings.integrity);

                if (string.IsNullOrWhiteSpace(settings.facilityLabel) == false)
                    facilityLabel = settings.facilityLabel;
                if (string.IsNullOrWhiteSpace(settings.targetLabel) == false)
                    targetLabel = settings.targetLabel;
                if (string.IsNullOrWhiteSpace(settings.decoyLabel) == false)
                    decoyLabel = settings.decoyLabel;

                ApplyModeOverrides(settings);
            }

            ResolveDerivedState();
        }

        private void ResetTuningToPrefabDefaults()
        {
            sweepSpeedMultiplier = 1f;
            playerSpeedMultiplier = 1f;
        }

        private void ApplyDifficultyProfile()
        {
            MinigameDifficulty resolved = Difficulty == MinigameDifficulty.Default
                ? MinigameDifficulty.Normal
                : Difficulty;

            switch (mode)
            {
                case CameraDeckMode.BlindSpotAudit:
                    integrity = resolved == MinigameDifficulty.Easy ? 4 : resolved == MinigameDifficulty.Hard ? 2 : 3;
                    cameraBudget = 3;
                    passingRating = resolved == MinigameDifficulty.Easy ? 75 : resolved == MinigameDifficulty.Hard ? 90 : 80;
                    visionRangeCells = 10.5f;
                    visionHalfAngleDegrees = 45f;
                    intruderSpeedCells = 5.2f;
                    break;

                case CameraDeckMode.CleanDeskSweep:
                    integrity = resolved == MinigameDifficulty.Easy ? 4 : resolved == MinigameDifficulty.Hard ? 2 : 3;
                    violationCount = resolved == MinigameDifficulty.Easy ? 5 : resolved == MinigameDifficulty.Hard ? 7 : 6;
                    decoyCount = resolved == MinigameDifficulty.Easy ? 3 : resolved == MinigameDifficulty.Hard ? 5 : 4;
                    visionRangeCells = 9f;
                    visionHalfAngleDegrees = 45f;
                    break;

                default:
                    integrity = resolved == MinigameDifficulty.Easy ? 4 : resolved == MinigameDifficulty.Hard ? 2 : 3;
                    visionRangeCells = resolved == MinigameDifficulty.Easy ? 8.5f : resolved == MinigameDifficulty.Hard ? 9.5f : 9f;
                    visionHalfAngleDegrees = resolved == MinigameDifficulty.Easy ? 34f : resolved == MinigameDifficulty.Hard ? 42f : 38f;
                    sweepSpeedMultiplier = resolved == MinigameDifficulty.Easy ? 0.8f : resolved == MinigameDifficulty.Hard ? 1.2f : 1f;
                    break;
            }
        }

        private void ApplyModeOverrides(CameraDeckMinigameSettings settings)
        {
            if (mode == CameraDeckMode.BlindSpotAudit && settings.audit != null)
            {
                if (settings.audit.cameraBudget >= 0)
                    cameraBudget = Mathf.Clamp(settings.audit.cameraBudget, 1, 3);
                if (settings.audit.passingRating >= 0)
                    passingRating = Mathf.Clamp(settings.audit.passingRating, 1, 100);
            }
            else if (mode == CameraDeckMode.CleanDeskSweep && settings.sweep != null)
            {
                if (settings.sweep.violationCount >= 0)
                    violationCount = settings.sweep.violationCount;
                if (settings.sweep.decoyCount >= 0)
                    decoyCount = settings.sweep.decoyCount;
                if (settings.sweep.switchDelaySeconds >= 0f)
                    switchDelaySeconds = settings.sweep.switchDelaySeconds;
            }
            else if (mode == CameraDeckMode.BlindRoute && settings.route != null)
            {
                if (settings.route.sweepSpeedMultiplier >= 0f)
                    sweepSpeedMultiplier = settings.route.sweepSpeedMultiplier;
                if (settings.route.playerSpeedMultiplier >= 0f)
                    playerSpeedMultiplier = settings.route.playerSpeedMultiplier;
            }
        }

        private void ResolveDerivedState()
        {
            CameraDeckFacilityDefinition facility;
            if (CameraDeckFacilityCatalog.TryGet(mode, facilityId, profileId, out facility) == false)
            {
                Debug.LogWarning($"[CameraDeck] Facility '{facilityId}' profile '{profileId}' is unavailable for {mode}; using the default.");
                facility = CameraDeckFacilityCatalog.GetDefault(mode);
            }

            layout = new DeckLayout(facility);
            maxIntegrity = Mathf.Max(1, integrity);
            currentIntegrity = maxIntegrity;
            resolvedViolations = Mathf.Clamp(violationCount, 1, Mathf.Max(1, layout.SpotCapacity));
            resolvedDecoys = Mathf.Clamp(decoyCount, 0, Mathf.Max(0, layout.SpotCapacity - resolvedViolations));
            totalTargets = layout.assetCells.Count + layout.entryCells.Count;
            coveredTargets = 0;
            finished = false;
            reported = 0;
            falseReports = 0;
            timesSpotted = 0;
            auditRuns = 0;
            auditDetectedRoutes = 0;
            auditTotalRoutes = layout.intrusionRoutes.Count;
            auditRating = 0;
        }

        public void SetCoverage(int covered)
        {
            coveredTargets = Mathf.Clamp(covered, 0, totalTargets);
        }

        public string CompleteAudit(int detectedRoutes, int routeCount, int camerasInstalled)
        {
            if (finished)
                return string.Empty;

            auditRuns++;
            auditDetectedRoutes = Mathf.Clamp(detectedRoutes, 0, routeCount);
            auditTotalRoutes = Mathf.Max(1, routeCount);

            float detectionRatio = auditDetectedRoutes / (float)auditTotalRoutes;
            float coverageRatio = totalTargets > 0 ? coveredTargets / (float)totalTargets : 0f;
            auditRating = Mathf.RoundToInt(detectionRatio * 80f + coverageRatio * 20f);
            if (auditRating >= passingRating)
            {
                int budgetBonus = Mathf.Max(0, cameraBudget - camerasInstalled) * budgetBonusPerCamera;
                int timeBonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive * timeBonusPerSecond);
                ChangeScore(auditRating * 10 + budgetBonus + timeBonus);
                finished = true;
                FinishMinigame(true, Score, Summary);
                return $"AUDIT PASSED — RATING {auditRating}/100. Routes {auditDetectedRoutes}/{auditTotalRoutes}, coverage {coveredTargets}/{totalTargets}.";
            }

            currentIntegrity--;
            if (currentIntegrity <= 0)
            {
                currentIntegrity = 0;
                finished = true;
                FinishMinigame(false, Score, Summary);
                return $"AUDIT REJECTED — RATING {auditRating}/100, required {passingRating}.";
            }

            return $"AUDIT RATING {auditRating}/100 — required {passingRating}. Review the route overlays. RUNS LEFT: {currentIntegrity}";
        }

        public string ReportSpot(bool genuine, string description)
        {
            if (finished)
                return string.Empty;

            if (genuine)
            {
                reported++;
                ChangeScore(pointsPerTarget);
                if (reported >= resolvedViolations)
                {
                    int bonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive * timeBonusPerSecond);
                    ChangeScore(bonus);
                    finished = true;
                    FinishMinigame(true, Score, Summary);
                    return $"All {targetLabel}S reported — sweep complete. Time bonus +{bonus}";
                }

                return $"{targetLabel} reported: {description} ({reported}/{resolvedViolations})";
            }

            falseReports++;
            currentIntegrity--;
            ChangeScore(-decoyPenalty);
            if (currentIntegrity <= 0)
            {
                currentIntegrity = 0;
                finished = true;
                FinishMinigame(false, Score, Summary);
                return "REPORT CREDIBILITY LOST — too many false reports.";
            }

            return $"{decoyLabel}: {description}. False report (-{decoyPenalty}).";
        }

        public string OnRunCaught(string cameraLabel)
        {
            if (finished)
                return string.Empty;

            timesSpotted++;
            currentIntegrity--;
            if (currentIntegrity <= 0)
            {
                currentIntegrity = 0;
                finished = true;
                FinishMinigame(false, Score, Summary);
                return $"CAUGHT by {cameraLabel} — the operation is blown.";
            }

            return $"Spotted by {cameraLabel}. Back to entry. ATTEMPTS LEFT: {currentIntegrity}";
        }

        public string OnRunReachedTarget()
        {
            if (finished)
                return string.Empty;

            int attemptBonus = Mathf.Max(0, currentIntegrity - 1) * routePoints;
            int timeBonus = Mathf.RoundToInt(MinigamesController.GetTimerPositive * timeBonusPerSecond);
            ChangeScore(routePoints + attemptBonus + timeBonus);
            finished = true;
            FinishMinigame(true, Score, Summary);
            return $"{targetLabel} reached unseen — clean infiltration.";
        }

        private string BuildSummary()
        {
            switch (mode)
            {
                case CameraDeckMode.BlindSpotAudit:
                    return "<b><color=#35E0FF>Audit report:</color></b>"
                        + $"\nRating: {auditRating}/100 • Routes detected: {auditDetectedRoutes}/{auditTotalRoutes}"
                        + $" • Critical coverage: {coveredTargets}/{totalTargets} • Runs: {auditRuns}"
                        + "\n\n<b>Lesson:</b> effective surveillance balances route interception and critical coverage. "
                        + "Test the complete threat model, then spend equipment where it changes the rating.";

                case CameraDeckMode.CleanDeskSweep:
                    return "<b><color=#35E0FF>Sweep report:</color></b>"
                        + $"\n{targetLabel}S reported: {reported}/{resolvedViolations} • False reports: {falseReports}"
                        + "\n\n<b>Lesson:</b> physical security depends on context. Judge the observed facts instead "
                        + "of reacting to an object or label alone.";

                default:
                    return "<b><color=#35E0FF>Infiltration report:</color></b>"
                        + $"\nTimes spotted: {timesSpotted} • Attempts left: {currentIntegrity}"
                        + "\n\n<b>Lesson:</b> predictable camera rhythms create movement windows. Observe, move between "
                        + "safe positions, and stop before entering the next sweep.";
            }
        }
    }
}
