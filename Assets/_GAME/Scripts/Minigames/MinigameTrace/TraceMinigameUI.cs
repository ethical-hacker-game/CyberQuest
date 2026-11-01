using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// Interactive surface for <see cref="TraceMinigame"/>. Lays out the topology inside a
    /// graph area (links behind nodes), routes node / link clicks into the minigame and
    /// repaints node + link states after every action. Fully UI-driven, like Firewall.
    /// </summary>
    public class TraceMinigameUI : MinigameUI
    {
        [Header("Graph layout")]
        [SerializeField, Tooltip("Rect the graph is laid out inside. Nodes are placed at (position - 0.5) * size.")]
        private RectTransform graphArea;
        [SerializeField, Tooltip("Full-stretch child of graphArea that holds the link bars (drawn behind the nodes).")]
        private RectTransform linksContainer;
        [SerializeField, Tooltip("Full-stretch child of graphArea that holds the node buttons (drawn in front).")]
        private RectTransform nodesContainer;
        [SerializeField, Tooltip("Used when graphArea has not been laid out yet (rect size ~0).")]
        private Vector2 fallbackAreaSize = new Vector2(900f, 520f);

        [Header("Prefabs")]
        [SerializeField] private TraceMinigameUI_Node nodePrefab;
        [SerializeField] private TraceMinigameUI_Link linkPrefab;

        [Header("Controls & readout")]
        [SerializeField] private Button resetButton;
        [SerializeField, Tooltip("Phase / instruction line, e.g. \"Trace the path\" or \"Cut the choke point\".")]
        private TextMeshProUGUI statusTMP;
        [SerializeField, Tooltip("Feedback on the last action (pivot accepted, dead end, etc.).")]
        private TextMeshProUGUI feedbackTMP;

        // Neon / cyberpunk palette matching the game's look. Semantics: magenta = the
        // attacker, cyan = the defender's action (cuttable links), neon yellow = the
        // asset to protect, deep blue = neutral infrastructure.
        [Header("Node colours")]
        [SerializeField] private Color entryColor = new Color(1f, 0.15f, 0.55f);
        [SerializeField] private Color targetColor = new Color(0.99f, 0.90f, 0.16f);
        [SerializeField] private Color pathColor = new Color(0.75f, 0.30f, 1f);
        [SerializeField] private Color normalColor = new Color(0.16f, 0.32f, 0.55f);
        [SerializeField, Tooltip("Host the live intruder is currently on (shown only once the traced path covers it).")]
        private Color attackerColor = new Color(1f, 0.10f, 0.25f);

        [Header("Link colours")]
        [SerializeField] private Color linkIdleColor = new Color(0.25f, 0.50f, 0.70f, 0.60f);
        [SerializeField] private Color linkPathColor = new Color(1f, 0.30f, 0.80f);
        [SerializeField] private Color linkCuttableColor = new Color(0.10f, 0.95f, 1f);

        private readonly Dictionary<string, TraceMinigameUI_Node> nodeViews = new Dictionary<string, TraceMinigameUI_Node>();
        private readonly List<TraceMinigameUI_Link> linkViews = new List<TraceMinigameUI_Link>();

        private TraceMinigame Minigame => TraceMinigame.MinigameInstance;
        private bool initialised;

        private void Start()
        {
            if (Minigame == null)
                return;

            BuildLinks();
            BuildNodes();

            if (resetButton != null)
                resetButton.onClick.AddListener(OnReset);

            initialised = true;
            RefreshAll();
        }

        private Vector2 GraphToAnchored(Vector2 normalised)
        {
            Vector2 size = graphArea != null ? graphArea.rect.size : fallbackAreaSize;
            if (size.x < 1f || size.y < 1f)
                size = fallbackAreaSize;

            return new Vector2((normalised.x - 0.5f) * size.x, (normalised.y - 0.5f) * size.y);
        }

        private void BuildLinks()
        {
            linkViews.Clear();

            if (linkPrefab == null || linksContainer == null)
                return;

            foreach (TraceLink link in Minigame.Links)
            {
                TraceNode from = Minigame.GetNode(link.fromId);
                TraceNode to = Minigame.GetNode(link.toId);
                if (from == null || to == null)
                    continue;

                TraceMinigameUI_Link view = Instantiate(linkPrefab, linksContainer);
                view.gameObject.SetActive(true);
                view.Setup(link, OnLinkClicked);
                view.SetEndpoints(GraphToAnchored(from.position), GraphToAnchored(to.position));
                view.SetEvidenceText(link.evidence);
                linkViews.Add(view);
            }
        }

        private void BuildNodes()
        {
            nodeViews.Clear();

            if (nodePrefab == null || nodesContainer == null)
                return;

            foreach (TraceNode node in Minigame.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.id))
                    continue;

                TraceMinigameUI_Node view = Instantiate(nodePrefab, nodesContainer);
                view.gameObject.SetActive(true);
                view.Setup(node, OnNodeClicked);
                view.Rect.anchoredPosition = GraphToAnchored(node.position);

                if (nodeViews.ContainsKey(node.id) == false)
                    nodeViews.Add(node.id, view);
            }
        }

        private void OnNodeClicked(string id)
        {
            // Inspect the link between the tip and the clicked node so the evidence shows
            // even when it starts hidden (Normal/Hard).
            RevealEvidenceFor(Minigame.TipId, id);

            TraceSelectResult result = Minigame.TrySelectNode(id);

            if (string.IsNullOrEmpty(result.message) == false)
                SetFeedback(result.message, result.IsRejection);

            RefreshAll();
        }

        private void OnLinkClicked(TraceLink link)
        {
            if (Minigame.Phase == TracePhase.Contain)
            {
                TraceCutResult cut = Minigame.TryCutLink(link);
                if (cut.handled)
                    SetFeedback(cut.message, cut.correct == false);

                RefreshAll();
                return;
            }

            // Trace phase: clicking a link just inspects its evidence.
            RevealEvidence(link);
            if (string.IsNullOrEmpty(link.evidence) == false)
                SetFeedback(link.evidence, false);
        }

        private void OnReset()
        {
            Minigame.ResetPath();
            SetFeedback("Path cleared - trace it again from the entry point.", false);
            RefreshAll();
        }

        private void RevealEvidenceFor(string a, string b)
        {
            if (a == null || b == null)
                return;

            TraceLink link = Minigame.GetLink(a, b);
            if (link != null)
                RevealEvidence(link);
        }

        private void RevealEvidence(TraceLink link)
        {
            foreach (TraceMinigameUI_Link view in linkViews)
            {
                if (view.Link == link)
                {
                    view.SetEvidenceVisible(true);
                    return;
                }
            }
        }

        private void RefreshAll()
        {
            RefreshNodes();
            RefreshLinks();
            RefreshStatus();
        }

        private void RefreshNodes()
        {
            foreach (KeyValuePair<string, TraceMinigameUI_Node> pair in nodeViews)
            {
                TraceNode node = Minigame.GetNode(pair.Key);
                TraceMinigameUI_Node view = pair.Value;

                Color color = normalColor;
                if (node != null && node.role == TraceNodeRole.Entry)
                    color = entryColor;
                else if (node != null && node.role == TraceNodeRole.Target)
                    color = targetColor;
                else if (Minigame.IsInPath(pair.Key))
                    color = pathColor;

                // The live intruder outranks every other tint - but only on hosts the
                // player has already traced (no free intel about the rest of the route).
                if (Minigame.AttackerVisible && pair.Key == Minigame.AttackerNodeId)
                    color = attackerColor;

                view.SetColor(color);
                view.SetTip(Minigame.IsTip(pair.Key) && Minigame.Phase == TracePhase.Trace);
            }
        }

        private void RefreshLinks()
        {
            bool contain = Minigame.Phase == TracePhase.Contain;

            foreach (TraceMinigameUI_Link view in linkViews)
            {
                TraceLink link = view.Link;
                Color color = linkIdleColor;

                if (Minigame.IsLinkOnPath(link))
                    color = linkPathColor;

                // In the contain phase, highlight cut candidates - but only when the
                // evidence is on show (Easy / prefab default). On Normal/Hard the player
                // has to decide from their own reading of the topology.
                if (contain && link.attackerCanTraverse && Minigame.EvidenceVisible)
                    color = linkCuttableColor;

                view.SetColor(color);

                if (Minigame.EvidenceVisible)
                    view.SetEvidenceVisible(true);
            }
        }

        private void RefreshStatus()
        {
            if (resetButton != null)
                resetButton.interactable = Minigame.Phase != TracePhase.Done;

            if (statusTMP == null)
                return;

            int hops = Minigame.AttackerHopsFromTarget;
            string intruder = hops == 1 ? "1 hop" : $"{hops} hops";

            switch (Minigame.Phase)
            {
                case TracePhase.Trace:
                    statusTMP.text = "Trace the path from the magenta entry to the yellow target. Inspecting links is free - every move is not." +
                                     $"   •   Intruder: {intruder} from the target.";
                    break;
                case TracePhase.Contain:
                    statusTMP.text = "Contain: cut a link AHEAD of the intruder to isolate the target." +
                                     $"   •   Intruder: {intruder} from the target.";
                    break;
                default:
                    statusTMP.text = "Investigation closed.";
                    break;
            }
        }

        private void SetFeedback(string message, bool isWarning)
        {
            if (feedbackTMP == null)
                return;

            feedbackTMP.text = message;
            feedbackTMP.color = isWarning
                ? new Color(1f, 0.35f, 0.60f)     // hot magenta - attacker/danger
                : new Color(0.55f, 0.90f, 1f);    // neon cyan - informational
        }

        public override void UpdateUI()
        {
            // Called once on the source prefab before the live copy exists; safe to no-op
            // until this instance has run its own Start.
            if (initialised)
                RefreshStatus();
        }
    }
}
