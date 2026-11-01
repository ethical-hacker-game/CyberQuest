using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MainMenu
{
    public class VersionLabel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI versionTMP;

        private void Start()
        {
            versionTMP.text = "v" + Application.version;
        }
    }
}
