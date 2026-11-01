using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DevTools
{
    public class PictureSelectionUIButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image image;

        private string id;

        public void SetupButton(string id, Sprite sprite)
        {
            this.id = id;

            label.text = id;
            image.sprite = sprite;
        }
        
        public void Click()
        {
            PictureSelectionUI.Instance.SelectPicture(id);
        }
    }
}