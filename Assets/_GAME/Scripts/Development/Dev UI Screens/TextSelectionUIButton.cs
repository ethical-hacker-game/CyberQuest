using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DevTools
{
    public class TextSelectionUIButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        private string id;

        public void SetupButton(string id)
        {
            this.id = id;
            label.text = id;
        }
        
        public void Click()
        {
            TextSelectionUI.Instance.SelectText(id);
        }
    }
}