using RuntimeInspectorNamespace;
using System;

namespace Minigames
{
    [Serializable, ExpandArray]
    public sealed class CameraDeckMinigameSettings
    {
        // Empty values select the mode's canonical facility and default scenario profile.
        public string facilityId;
        public string profileId;

        // -1 keeps the difficulty profile value. Zero is intentionally a valid override.
        public int integrity = -1;

        public string facilityLabel;
        public string targetLabel;
        public string decoyLabel;

        public CameraDeckAuditSettings audit = new CameraDeckAuditSettings();
        public CameraDeckSweepSettings sweep = new CameraDeckSweepSettings();
        public CameraDeckRouteSettings route = new CameraDeckRouteSettings();
    }

    [Serializable]
    public sealed class CameraDeckAuditSettings
    {
        public int cameraBudget = -1;
        public int passingRating = -1;
    }

    [Serializable]
    public sealed class CameraDeckSweepSettings
    {
        public int violationCount = -1;
        public int decoyCount = -1;
        public float switchDelaySeconds = -1f;
    }

    [Serializable]
    public sealed class CameraDeckRouteSettings
    {
        public float sweepSpeedMultiplier = -1f;
        public float playerSpeedMultiplier = -1f;
    }
}
