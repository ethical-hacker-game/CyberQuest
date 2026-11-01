using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Minigames
{
    /// <summary>
    /// One chip in the forge bar. Clicking it removes the token from the password.
    /// </summary>
    public class StrengthForgeMinigameUI_ForgedToken : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI labelTMP;

        private int index;
        private Action<int> onRemove;

        public void Setup(StrengthToken token, int index, Action<int> onRemove)
        {
            this.index = index;
            this.onRemove = onRemove;

            if (labelTMP != null)
                labelTMP.text = token.text;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => this.onRemove?.Invoke(this.index));
            }
        }
    }
}
