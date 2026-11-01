using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// One editable rule: Action / Source / Port / Protocol dropdowns plus reorder and
    /// delete buttons. Edits are written straight into the backing <see cref="FirewallRule"/>;
    /// structural actions (move / delete) are raised back to the parent UI as callbacks.
    /// </summary>
    public class FirewallMinigameUI_RuleRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI orderLabel;
        [SerializeField] private TMP_Dropdown actionDropdown;
        [SerializeField] private TMP_Dropdown sourceDropdown;
        [SerializeField] private TMP_Dropdown portDropdown;
        [SerializeField] private TMP_Dropdown protocolDropdown;

        [Header("Buttons")]
        [SerializeField] private Button moveUpButton;
        [SerializeField] private Button moveDownButton;
        [SerializeField] private Button deleteButton;

        [Header("Action tint (optional)")]
        [SerializeField] private Image actionTint;
        [SerializeField] private Color allowColor = new Color(0.42f, 0.85f, 0.45f);
        [SerializeField] private Color denyColor = new Color(0.90f, 0.36f, 0.36f);

        private FirewallRule rule;
        private Action<FirewallRule, int> onMove;
        private Action<FirewallRule> onDelete;
        private bool initialised;

        public void Setup(
            FirewallRule rule,
            int displayIndex,
            List<string> sourceOptions,
            List<string> portOptions,
            Action<FirewallRule, int> onMove,
            Action<FirewallRule> onDelete)
        {
            this.rule = rule;
            this.onMove = onMove;
            this.onDelete = onDelete;

            if (orderLabel != null)
                orderLabel.text = (displayIndex + 1).ToString();

            PopulateDropdown(actionDropdown, new List<string> { "DENY", "ALLOW" });
            PopulateDropdown(sourceDropdown, sourceOptions);
            PopulateDropdown(portDropdown, portOptions);
            PopulateDropdown(protocolDropdown, new List<string> { "Any", "TCP", "UDP" });

            // Reflect the rule's current state without firing change callbacks.
            SetDropdownValue(actionDropdown, (int)rule.action);
            SetDropdownValue(sourceDropdown, Mathf.Clamp(rule.sourceIndex, 0, sourceOptions.Count - 1));
            SetDropdownValue(portDropdown, Mathf.Clamp(rule.portIndex, 0, portOptions.Count - 1));
            SetDropdownValue(protocolDropdown, (int)rule.protocol);

            if (initialised == false)
            {
                WireListeners();
                initialised = true;
            }

            UpdateActionTint();
        }

        private void WireListeners()
        {
            if (actionDropdown != null)
                actionDropdown.onValueChanged.AddListener(OnActionChanged);
            if (sourceDropdown != null)
                sourceDropdown.onValueChanged.AddListener(OnSourceChanged);
            if (portDropdown != null)
                portDropdown.onValueChanged.AddListener(OnPortChanged);
            if (protocolDropdown != null)
                protocolDropdown.onValueChanged.AddListener(OnProtocolChanged);

            if (moveUpButton != null)
                moveUpButton.onClick.AddListener(() => onMove?.Invoke(rule, -1));
            if (moveDownButton != null)
                moveDownButton.onClick.AddListener(() => onMove?.Invoke(rule, +1));
            if (deleteButton != null)
                deleteButton.onClick.AddListener(() => onDelete?.Invoke(rule));
        }

        private void OnActionChanged(int value)
        {
            if (rule == null) return;
            rule.action = (FirewallRuleAction)value;
            UpdateActionTint();
        }

        private void OnSourceChanged(int value)
        {
            if (rule == null) return;
            rule.sourceIndex = value;
        }

        private void OnPortChanged(int value)
        {
            if (rule == null) return;
            rule.portIndex = value;
        }

        private void OnProtocolChanged(int value)
        {
            if (rule == null) return;
            rule.protocol = (FirewallProtocol)value;
        }

        private void UpdateActionTint()
        {
            if (actionTint == null || rule == null)
                return;

            actionTint.color = rule.action == FirewallRuleAction.Allow ? allowColor : denyColor;
        }

        private static void PopulateDropdown(TMP_Dropdown dropdown, List<string> options)
        {
            if (dropdown == null)
                return;

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
        }

        private static void SetDropdownValue(TMP_Dropdown dropdown, int value)
        {
            if (dropdown == null)
                return;

            dropdown.SetValueWithoutNotify(Mathf.Max(0, value));
            dropdown.RefreshShownValue();
        }
    }
}
