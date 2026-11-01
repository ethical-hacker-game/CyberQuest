using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// A visual network link: a thin rotated bar stretched between two node anchors, with
    /// an optional evidence label kept upright at its midpoint. Clickable so the player can
    /// inspect its evidence (Trace phase) or choose it as the containment cut (Contain phase).
    /// </summary>
    public class TraceMinigameUI_Link : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private Image lineImage;
        [SerializeField] private Button button;
        [SerializeField, Tooltip("Optional label showing the link evidence at the midpoint.")]
        private TextMeshProUGUI evidenceTMP;
        [SerializeField, Tooltip("Optional transform (the evidence label's parent) counter-rotated so text stays upright.")]
        private RectTransform evidenceHolder;
        [SerializeField] private float thickness = 6f;

        private TraceLink link;
        private Action<TraceLink> onClick;

        public TraceLink Link => link;

        private void Reset()
        {
            rect = (RectTransform)transform;
        }

        public void Setup(TraceLink linkData, Action<TraceLink> clickHandler)
        {
            link = linkData;
            onClick = clickHandler;

            if (rect == null)
                rect = (RectTransform)transform;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(link));
            }
        }

        /// <summary>Positions the bar between two anchored points in the graph area's local space.</summary>
        public void SetEndpoints(Vector2 a, Vector2 b)
        {
            if (rect == null)
                rect = (RectTransform)transform;

            Vector2 delta = b - a;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            rect.sizeDelta = new Vector2(length, thickness);
            rect.anchoredPosition = (a + b) * 0.5f;
            rect.localEulerAngles = new Vector3(0f, 0f, angle);

            // Keep the evidence text horizontal regardless of the link's angle.
            if (evidenceHolder != null)
                evidenceHolder.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        public void SetColor(Color color)
        {
            if (lineImage != null)
                lineImage.color = color;
        }

        public void SetEvidenceVisible(bool visible)
        {
            if (evidenceTMP == null)
                return;

            evidenceTMP.gameObject.SetActive(visible && string.IsNullOrEmpty(evidenceTMP.text) == false);
        }

        public void SetEvidenceText(string text)
        {
            if (evidenceTMP != null)
                evidenceTMP.text = text;
        }
    }
}
