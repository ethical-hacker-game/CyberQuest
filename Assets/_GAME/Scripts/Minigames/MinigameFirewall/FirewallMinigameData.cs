using System;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    /// <summary>
    /// Transport protocol a rule / traffic request is tied to.
    /// <see cref="Any"/> on a rule matches every protocol.
    /// </summary>
    public enum FirewallProtocol
    {
        Any = 0,
        TCP = 1,
        UDP = 2,
    }

    /// <summary>
    /// What a firewall rule does with the traffic it matches.
    /// Deny is 0 so a freshly created / default rule leans safe.
    /// </summary>
    public enum FirewallRuleAction
    {
        Deny = 0,
        Allow = 1,
    }

    /// <summary>
    /// One authored connection request the player has to allow or block.
    /// <see cref="shouldAllow"/> is the ground truth used for scoring and is never
    /// shown to the player directly &mdash; they infer it from <see cref="label"/>.
    /// </summary>
    [Serializable]
    public class FirewallTrafficEntry
    {
        [Tooltip("Human-readable context shown to the player, e.g. \"HTTPS from the branch office\".")]
        public string label = "New request";

        [Tooltip("Source address of the request, e.g. \"203.0.113.5\".")]
        public string sourceIp = "0.0.0.0";

        [Tooltip("Destination port, e.g. 443.")]
        public int port = 0;

        public FirewallProtocol protocol = FirewallProtocol.TCP;

        [Tooltip("Ground truth: should this request be allowed through? Never shown to the player.")]
        public bool shouldAllow = false;

        [TextArea(1, 3)]
        [Tooltip("Short explanation shown on the result screen so the player learns why.")]
        public string rationale = "";

        public string Details => $"{sourceIp}:{port} / {protocol}";
    }

    /// <summary>
    /// A selectable source-address matcher shown in a rule row's dropdown.
    /// The pool is generated from the traffic set so the player only ever picks from
    /// addresses (and /24 wildcards) that are actually relevant to the scenario.
    /// </summary>
    public class FirewallSourceOption
    {
        public string Label { get; }
        private readonly string pattern; // "any", an exact IP, or a "a.b.c.*" wildcard.

        public FirewallSourceOption(string label, string pattern)
        {
            Label = label;
            this.pattern = pattern;
        }

        public bool Matches(string ip)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "any")
                return true;

            if (pattern.EndsWith("*"))
            {
                string prefix = pattern.Substring(0, pattern.Length - 1);
                return ip != null && ip.StartsWith(prefix);
            }

            return ip == pattern;
        }
    }

    /// <summary>
    /// A selectable port matcher shown in a rule row's dropdown. Supports "any",
    /// an exact port and inclusive ranges (used for the system / ephemeral presets).
    /// </summary>
    public class FirewallPortOption
    {
        public enum PortMode { Any, Exact, Range }

        public string Label { get; }
        private readonly PortMode mode;
        private readonly int min;
        private readonly int max;

        private FirewallPortOption(string label, PortMode mode, int min, int max)
        {
            Label = label;
            this.mode = mode;
            this.min = min;
            this.max = max;
        }

        public static FirewallPortOption Any(string label) => new FirewallPortOption(label, PortMode.Any, 0, 0);
        public static FirewallPortOption Exact(string label, int port) => new FirewallPortOption(label, PortMode.Exact, port, port);
        public static FirewallPortOption Range(string label, int min, int max) => new FirewallPortOption(label, PortMode.Range, min, max);

        public bool Matches(int port)
        {
            switch (mode)
            {
                case PortMode.Any: return true;
                case PortMode.Exact: return port == min;
                case PortMode.Range: return port >= min && port <= max;
                default: return false;
            }
        }
    }

    /// <summary>
    /// A single rule the player builds at runtime. The source / port conditions are
    /// stored as indices into the minigame's option pools so a rule stays a tiny,
    /// serialization-free value object that the UI can rebuild cheaply.
    /// </summary>
    public class FirewallRule
    {
        public FirewallRuleAction action = FirewallRuleAction.Allow;
        public int sourceIndex = 0;
        public int portIndex = 0;
        public FirewallProtocol protocol = FirewallProtocol.Any;
    }

    /// <summary>
    /// Result of running the current ruleset against every traffic request.
    /// </summary>
    public class FirewallEvaluation
    {
        public int Total;
        public int Correct;
        // One entry per traffic request, index-aligned with the traffic list.
        public List<bool> Decisions = new List<bool>();   // true = allowed by the ruleset
        public List<bool> Correctness = new List<bool>();  // true = decision matches ground truth

        public bool IsPerfect => Total > 0 && Correct == Total;
    }
}
