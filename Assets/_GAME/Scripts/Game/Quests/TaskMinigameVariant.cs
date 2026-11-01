using Minigames;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskMinigameVariant : TaskBaseVariant, IValidateContent
{
    [JsonProperty]
    public MinigamesController.MinigameType minigameType = MinigamesController.MinigameType.None;
    public string minigameHeaderText;

    // --- General overrides (apply to every minigame) ---

    // Timer in seconds. 0 (or less) means "use the minigame's own default duration".
    public float durationOverride = 0f;

    // Difficulty knob. Default means "use the prefab values as-is".
    public MinigameDifficulty difficulty = MinigameDifficulty.Default;

    // --- Per-minigame settings (only the block matching minigameType is used) ---

    // Connections: optional override of the benign/malicious traffic lists.
    // Leave null to keep the lists baked into the prefab.
    public ConnectionsMinigameSettings connectionsSettings;

    // Waves: optional override of the match tolerance.
    public WavesMinigameSettings wavesSettings;

    // Network (Distance/Devices/Trace): optional override of the win-condition goals.
    public NetworkMinigameSettings networkSettings;

    // Firewall Ruleset: optional override of the traffic set, rule budget and policy.
    public FirewallMinigameSettings firewallSettings;

    // Trace the Intruder: optional override of the whole topology (nodes + links).
    public TraceMinigameSettings traceSettings;

    // Decrypt the Intercept: optional override of the scenario pool and attempt budget.
    public DecryptMinigameSettings decryptSettings;

    // Strength Forge: optional override of the token pool and the account rounds.
    public StrengthForgeMinigameSettings strengthForgeSettings;

    // Sequence Order: optional override of the scenario pool and check budget.
    public OrderMinigameSettings orderSettings;

    // Packet Rush: optional override of packet integrity, flow speed and hazard density.
    public RunnerMinigameSettings runnerSettings;

    // Data Rover: optional override of integrity, shard count, patrol speed, arena layout
    // and the per-unit story skin labels.
    public RoverMinigameSettings dataRoverSettings;

    // Patch Hunt: optional override of integrity, pacing, decoy density and the
    // per-unit story skin labels.
    public PatchHuntMinigameSettings patchHuntSettings;

    // Sonar Sweep: optional override of integrity, cache/echo counts, sweep speed,
    // afterglow and the per-unit story skin labels.
    public SonarMinigameSettings sonarSettings;

    // Camera Deck: stable facility/profile selection, common story skin and nested
    // mode-specific overrides. Numeric value -1 keeps the difficulty profile.
    public CameraDeckMinigameSettings cameraDeckSettings;

    public override bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();

        base.ValidateContent(out string baseIssues);

        if (baseIssues.IsNullOrEmpty() == false)
            issues.Add(baseIssues);

        if(minigameType == MinigamesController.MinigameType.None)
            issues.Add("Minigame type is not set.");

        if(minigameHeaderText.IsNullOrEmpty())
            issues.Add("Minigame header text is empty.");

        if(durationOverride < 0f)
            issues.Add("Duration override cannot be negative.");

        if (connectionsSettings != null)
        {
            bool hasPositive = connectionsSettings.positiveConnections.IsNullOrEmpty() == false;
            bool hasNegative = connectionsSettings.negativeConnections.IsNullOrEmpty() == false;

            if (hasPositive != hasNegative)
                issues.Add("Connections settings must define both the benign and the malicious list, or neither.");
        }

        if (wavesSettings != null && wavesSettings.tolerance < 0f)
            issues.Add("Waves tolerance cannot be negative.");

        if (networkSettings != null && (networkSettings.devicesToHack < 0 || networkSettings.reconMovesToWin < 0))
            issues.Add("Network settings goals cannot be negative.");

        if (firewallSettings != null)
        {
            if (firewallSettings.ruleBudget < 0)
                issues.Add("Firewall rule budget cannot be negative.");

            if (firewallSettings.traffic != null)
            {
                bool anyAllow = false;
                bool anyBlock = false;

                for (int t = 0; t < firewallSettings.traffic.Count; t++)
                {
                    FirewallTrafficEntry entry = firewallSettings.traffic[t];

                    if (entry.label.IsNullOrEmpty())
                        issues.Add($"Firewall traffic entry {t} has no label.");

                    if (entry.port < 0 || entry.port > 65535)
                        issues.Add($"Firewall traffic entry {t} has an out-of-range port ({entry.port}).");

                    anyAllow |= entry.shouldAllow;
                    anyBlock |= !entry.shouldAllow;
                }

                if (firewallSettings.traffic.Count > 0 && (!anyAllow || !anyBlock))
                    issues.Add("Firewall traffic should mix at least one allowed and one blocked request.");
            }
        }

        if (traceSettings != null)
        {
            if (traceSettings.attackerStepEvery < 0)
                issues.Add("Trace attackerStepEvery cannot be negative (0 = keep the prefab value).");

            if (traceSettings.scenarios != null)
            {
                for (int s = 0; s < traceSettings.scenarios.Count; s++)
                {
                    TraceScenario scenario = traceSettings.scenarios[s];

                    if (scenario == null || scenario.HasGraph == false)
                    {
                        issues.Add($"Trace scenario {s} must define both nodes and links.");
                        continue;
                    }

                    ValidateTraceGraph(scenario.nodes, scenario.links, $"Trace scenario {s}", issues);
                }
            }

            if (traceSettings.nodes.Count > 0 || traceSettings.links.Count > 0)
            {
                if (traceSettings.nodes.Count == 0 || traceSettings.links.Count == 0)
                    issues.Add("Trace settings must define both nodes and links, or neither.");
                else
                    ValidateTraceGraph(traceSettings.nodes, traceSettings.links, "Trace graph", issues);
            }
        }

        if (decryptSettings != null)
        {
            if (decryptSettings.wrongAttemptsAllowed < 0)
                issues.Add("Decrypt wrongAttemptsAllowed cannot be negative (0 = keep the prefab value).");

            if (decryptSettings.scenarios != null)
            {
                for (int s = 0; s < decryptSettings.scenarios.Count; s++)
                {
                    DecryptScenario scenario = decryptSettings.scenarios[s];

                    if (scenario == null || scenario.HasPuzzle == false)
                    {
                        issues.Add($"Decrypt scenario {s} must define a plaintext and at least one encoding step.");
                        continue;
                    }

                    for (int t = 0; t < scenario.steps.Count; t++)
                    {
                        DecryptStep step = scenario.steps[t];

                        if (step == null)
                        {
                            issues.Add($"Decrypt scenario {s} step {t} is empty.");
                            continue;
                        }

                        if (step.type == DecryptStepType.Caesar && (step.caesarShift < 1 || step.caesarShift > DecryptCipher.MaxCaesarShift))
                            issues.Add($"Decrypt scenario {s} step {t}: Caesar shift must be between 1 and {DecryptCipher.MaxCaesarShift}.");

                        if (step.type == DecryptStepType.Rot13)
                            issues.Add($"Decrypt scenario {s} step {t}: ROT13 is not supported (needs Caesar key 13, outside the 1-{DecryptCipher.MaxCaesarShift} picker).");
                    }

                    if (scenario.steps.Count > 4)
                        issues.Add($"Decrypt scenario {s} stacks more than 4 layers - unreasonable for the attempt budget.");
                }
            }
        }

        if (strengthForgeSettings != null)
        {
            if (strengthForgeSettings.tokens != null && strengthForgeSettings.tokens.Count > 0)
            {
                if (strengthForgeSettings.tokens.Count < 4)
                    issues.Add("Strength Forge needs at least 4 tokens to be a meaningful choice.");

                for (int t = 0; t < strengthForgeSettings.tokens.Count; t++)
                {
                    StrengthToken token = strengthForgeSettings.tokens[t];

                    if (token == null || token.text.IsNullOrEmpty())
                        issues.Add($"Strength Forge token {t} has no text.");

                    if (token != null && token.cost < 0)
                        issues.Add($"Strength Forge token {t} has a negative cost (0 = category default).");
                }
            }

            if (strengthForgeSettings.rounds != null)
            {
                for (int r = 0; r < strengthForgeSettings.rounds.Count; r++)
                {
                    StrengthForgeRound round = strengthForgeSettings.rounds[r];

                    if (round == null)
                    {
                        issues.Add($"Strength Forge round {r} is empty.");
                        continue;
                    }

                    if (round.label.IsNullOrEmpty())
                        issues.Add($"Strength Forge round {r} has no label.");

                    if (round.guessesPerSecond <= 0f)
                        issues.Add($"Strength Forge round {r} needs a positive guess rate.");

                    if (round.requiredCrackSeconds <= 0f)
                        issues.Add($"Strength Forge round {r} needs a positive required crack time.");

                    if (round.maxTokens < 1)
                        issues.Add($"Strength Forge round {r} must allow at least 1 token.");

                    if (round.memorabilityBudget < 1)
                        issues.Add($"Strength Forge round {r} needs a positive memorability budget.");

                    if (round.dictionaryAttack == false && round.hybridAttack == false && round.bruteForceAttack == false)
                        issues.Add($"Strength Forge round {r} has no attack enabled (it falls back to Hybrid at runtime).");
                }
            }
        }

        if (orderSettings != null)
        {
            if (orderSettings.checksAllowed < 0)
                issues.Add("Order checksAllowed cannot be negative (0 = keep the prefab value).");

            if (orderSettings.scenarios != null)
            {
                for (int s = 0; s < orderSettings.scenarios.Count; s++)
                {
                    OrderScenario scenario = orderSettings.scenarios[s];

                    if (scenario == null || scenario.HasSequence == false)
                    {
                        issues.Add($"Order scenario {s} must define at least 2 non-empty terms.");
                        continue;
                    }

                    if (scenario.headline.IsNullOrEmpty())
                        issues.Add($"Order scenario {s} has no headline - the player cannot know which sequence (and direction) to build.");

                    if (scenario.items.Count > 10)
                        issues.Add($"Order scenario {s} has more than 10 terms - it will not fit the board.");

                    HashSet<string> seen = new HashSet<string>();

                    for (int t = 0; t < scenario.items.Count; t++)
                    {
                        if (seen.Add(scenario.items[t].Trim().ToLowerInvariant()) == false)
                            issues.Add($"Order scenario {s} repeats the term '{scenario.items[t]}' - duplicate terms make the order ambiguous.");
                    }
                }
            }
        }

        if (runnerSettings != null)
        {
            if (runnerSettings.packetIntegrity < 0)
                issues.Add("Packet Rush packetIntegrity cannot be negative (0 = keep the prefab value).");

            if (runnerSettings.speedMultiplier < 0f)
                issues.Add("Packet Rush speedMultiplier cannot be negative (0 = keep the prefab value).");

            if (runnerSettings.spawnInterval < 0f)
                issues.Add("Packet Rush spawnInterval cannot be negative (0 = keep the prefab value).");

            if (runnerSettings.hazardChance < 0f || runnerSettings.hazardChance > 1f)
                issues.Add("Packet Rush hazardChance must be between 0 and 1 (0 = keep the prefab value).");
        }

        if (patchHuntSettings != null)
        {
            if (patchHuntSettings.packetIntegrity < 0)
                issues.Add("Patch Hunt packetIntegrity cannot be negative (0 = keep the prefab value).");

            if (patchHuntSettings.spawnInterval < 0f)
                issues.Add("Patch Hunt spawnInterval cannot be negative (0 = keep the prefab value).");

            if (patchHuntSettings.breachLifetime < 0f)
                issues.Add("Patch Hunt breachLifetime cannot be negative (0 = keep the prefab value).");

            if (patchHuntSettings.honeypotChance < 0f || patchHuntSettings.honeypotChance > 1f)
                issues.Add("Patch Hunt honeypotChance must be between 0 and 1 (0 = keep the prefab value).");
        }

        if (dataRoverSettings != null)
        {
            if (dataRoverSettings.packetIntegrity < 0)
                issues.Add("Data Rover packetIntegrity cannot be negative (0 = keep the prefab value).");

            if (dataRoverSettings.shardCount < 0)
                issues.Add("Data Rover shardCount cannot be negative (0 = keep the prefab value).");

            if (dataRoverSettings.patrolSpeedMultiplier < 0f)
                issues.Add("Data Rover patrolSpeedMultiplier cannot be negative (0 = keep the prefab value).");

            if (dataRoverSettings.layoutIndex < -1 || dataRoverSettings.layoutIndex >= RoverLayouts.BuiltIn.Length)
                issues.Add($"Data Rover layoutIndex must be -1 (random) or 0..{RoverLayouts.BuiltIn.Length - 1}.");

            if (dataRoverSettings.shardCount > 0)
            {
                // A specific layout is checked against its own capacity; a random layout
                // must fit the smallest arena in the pool.
                int capacity = dataRoverSettings.layoutIndex >= 0 && dataRoverSettings.layoutIndex < RoverLayouts.BuiltIn.Length
                    ? RoverLayouts.BuiltIn[dataRoverSettings.layoutIndex].ShardCapacity
                    : RoverLayouts.MinShardCapacity;

                if (dataRoverSettings.shardCount > capacity)
                    issues.Add($"Data Rover shardCount {dataRoverSettings.shardCount} exceeds the layout shard capacity ({capacity}).");
            }
        }

        if (sonarSettings != null)
        {
            if (sonarSettings.scanIntegrity < 0)
                issues.Add("Sonar Sweep scanIntegrity cannot be negative (0 = keep the prefab value).");

            if (sonarSettings.cacheCount < 0)
                issues.Add("Sonar Sweep cacheCount cannot be negative (0 = keep the prefab value).");

            if (sonarSettings.falseEchoCount < 0)
                issues.Add("Sonar Sweep falseEchoCount cannot be negative (0 = keep the prefab value).");

            if (sonarSettings.sweepSpeedMultiplier < 0f)
                issues.Add("Sonar Sweep sweepSpeedMultiplier cannot be negative (0 = keep the prefab value).");

            if (sonarSettings.afterglowSeconds < 0f)
                issues.Add("Sonar Sweep afterglowSeconds cannot be negative (0 = keep the prefab value).");

            if (sonarSettings.blipDriftMultiplier < 0f)
                issues.Add("Sonar Sweep blipDriftMultiplier cannot be negative (0 = keep the prefab value).");
        }

        if (cameraDeckSettings != null)
        {
            CameraDeckMode expectedMode = minigameType == MinigamesController.MinigameType.CamCleanDeskSweep
                ? CameraDeckMode.CleanDeskSweep
                : minigameType == MinigamesController.MinigameType.CamBlindRoute
                    ? CameraDeckMode.BlindRoute
                    : CameraDeckMode.BlindSpotAudit;

            CameraDeckFacilityDefinition facility;
            if (CameraDeckFacilityCatalog.TryGet(expectedMode, cameraDeckSettings.facilityId,
                cameraDeckSettings.profileId, out facility) == false)
            {
                issues.Add($"Camera Deck facility '{cameraDeckSettings.facilityId}' profile "
                    + $"'{cameraDeckSettings.profileId}' does not exist for {expectedMode}.");
            }

            if (cameraDeckSettings.integrity == 0 || cameraDeckSettings.integrity < -1)
                issues.Add("Camera Deck integrity must be -1 (difficulty profile) or at least 1.");

            if (cameraDeckSettings.audit != null)
            {
                if (cameraDeckSettings.audit.cameraBudget != -1
                    && (cameraDeckSettings.audit.cameraBudget < 1 || cameraDeckSettings.audit.cameraBudget > 3))
                    issues.Add("Camera Deck audit cameraBudget must be -1 or 1..3.");
                if (cameraDeckSettings.audit.passingRating != -1
                    && (cameraDeckSettings.audit.passingRating < 1 || cameraDeckSettings.audit.passingRating > 100))
                    issues.Add("Camera Deck audit passingRating must be -1 or 1..100.");
            }

            if (cameraDeckSettings.sweep != null)
            {
                if (cameraDeckSettings.sweep.violationCount < -1)
                    issues.Add("Camera Deck sweep violationCount must be -1 or non-negative.");
                if (cameraDeckSettings.sweep.decoyCount < -1)
                    issues.Add("Camera Deck sweep decoyCount must be -1 or non-negative.");
                if (cameraDeckSettings.sweep.switchDelaySeconds < -1f)
                    issues.Add("Camera Deck sweep switchDelaySeconds must be -1 or non-negative.");

                int violations = cameraDeckSettings.sweep.violationCount >= 0
                    ? cameraDeckSettings.sweep.violationCount : 6;
                int decoys = cameraDeckSettings.sweep.decoyCount >= 0
                    ? cameraDeckSettings.sweep.decoyCount : 4;
                int capacity = facility != null ? facility.InspectionAnchors.Length : 0;
                if (capacity > 0 && violations + decoys > capacity)
                    issues.Add($"Camera Deck sweep cases exceed facility capacity ({capacity}).");
            }

            if (cameraDeckSettings.route != null)
            {
                if (cameraDeckSettings.route.sweepSpeedMultiplier < -1f)
                    issues.Add("Camera Deck route sweepSpeedMultiplier must be -1 or non-negative.");
                if (cameraDeckSettings.route.playerSpeedMultiplier < -1f)
                    issues.Add("Camera Deck route playerSpeedMultiplier must be -1 or non-negative.");
            }
        }

        validationMessage = string.Join("\n - ", issues);

        return issues.Count == 0;
    }

    private static void ValidateTraceGraph(List<TraceNode> nodes, List<TraceLink> links, string context, List<string> issues)
    {
        int entries = 0;
        int targets = 0;
        HashSet<string> ids = new HashSet<string>();

        for (int n = 0; n < nodes.Count; n++)
        {
            TraceNode node = nodes[n];

            if (node.id.IsNullOrEmpty())
                issues.Add($"{context}: node {n} has an empty id.");
            else if (ids.Add(node.id) == false)
                issues.Add($"{context}: duplicate node id '{node.id}'.");

            if (node.role == TraceNodeRole.Entry) entries++;
            if (node.role == TraceNodeRole.Target) targets++;
        }

        if (entries != 1)
            issues.Add($"{context} needs exactly one Entry node.");

        if (targets != 1)
            issues.Add($"{context} needs exactly one Target node.");

        for (int l = 0; l < links.Count; l++)
        {
            TraceLink link = links[l];

            if (ids.Contains(link.fromId) == false || ids.Contains(link.toId) == false)
                issues.Add($"{context}: link {l} references an unknown node id ('{link.fromId}' - '{link.toId}').");
        }
    }
}
