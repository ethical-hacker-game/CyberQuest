using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>One decode-tool button in the Decrypt minigame (label + click routing).</summary>
    public class DecryptMinigameUI_ToolButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI labelTMP;

        private DecryptStepType type;
        private Action<DecryptStepType> onClicked;

        public void Setup(DecryptStepType toolType, string label, Action<DecryptStepType> clickHandler)
        {
            type = toolType;
            onClicked = clickHandler;

            if (labelTMP != null)
                labelTMP.text = label;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(type));
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}
