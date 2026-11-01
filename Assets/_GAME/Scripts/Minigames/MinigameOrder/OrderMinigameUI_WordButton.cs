using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// One clickable term in the Sequence Order minigame. Reused for both columns:
    /// pool entries (index = item) and sequence slots (index = slot) - the UI controller
    /// decides what the click means and how the button is painted.
    /// </summary>
    public class OrderMinigameUI_WordButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI labelTMP;

        private int index;
        private Action<int> onClicked;

        public void Setup(int buttonIndex, string label, Action<int> clickHandler)
        {
            index = buttonIndex;
            onClicked = clickHandler;

            if (labelTMP != null)
                labelTMP.text = label;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(index));
            }
        }

        public void SetLabel(string label)
        {
            if (labelTMP != null)
                labelTMP.text = label;
        }

        public void SetLabelAlpha(float alpha)
        {
            if (labelTMP == null)
                return;

            Color color = labelTMP.color;
            color.a = alpha;
            labelTMP.color = color;
        }

        public void SetBackground(Color color)
        {
            if (background != null)
                background.color = color;
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}
