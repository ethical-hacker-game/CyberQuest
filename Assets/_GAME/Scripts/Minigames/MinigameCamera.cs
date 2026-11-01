using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Minigames
{
    public class MinigameCamera : MonoBehaviour
    {
        private void Awake()
        {
            PostProcessVolume postProcess = GetComponent<PostProcessVolume>();

            if (postProcess == null)
                return;

            postProcess.enabled = SettingsManager.SettingsInfo.qualitySettings.postProcessing;
        }
    }
}
