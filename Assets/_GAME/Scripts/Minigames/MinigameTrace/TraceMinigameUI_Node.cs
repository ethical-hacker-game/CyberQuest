using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// One host in the graph: a clickable button with a hostname label and a coloured
    /// background whose colour is driven by <see cref="TraceMinigameUI"/> to reflect the
    /// node's role and its state in the traced path.
    /// </summary>
    public class TraceMinigameUI_Node : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI labelTMP;
        [SerializeField, Tooltip("Optional ring / outline shown when this node is the current path tip.")]
        private GameObject tipHighlight;

        private string nodeId;
        private Action<string> onClick;

        public string NodeId => nodeId;
        public RectTransform Rect => (RectTransform)transform;

        public void Setup(TraceNode node, Action<string> clickHandler)
        {
            nodeId = node.id;
            onClick = clickHandler;

            if (labelTMP != null)
                labelTMP.text = node.label;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(nodeId));
            }

            SetTip(false);
        }

        public void SetColor(Color color)
        {
            if (background != null)
                background.color = color;
        }

        public void SetTip(bool isTip)
        {
            if (tipHighlight != null)
                tipHighlight.SetActive(isTip);
        }
    }
}
