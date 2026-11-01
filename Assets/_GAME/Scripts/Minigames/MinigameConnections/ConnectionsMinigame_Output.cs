using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Minigames
{
    public class ConnectionsMinigame_Output : MonoBehaviour
    {
        [SerializeField] private float initialScale = 0.3f;
        [SerializeField] private float targetScale = 1.5f;
        [SerializeField, Range(float.Epsilon, 3)] private float scalingTime = 0.5f;
        [SerializeField] private AnimationCurve alphaOverLifetime;
        [SerializeField] private CanvasGroup canvasGroup;

        private float timer = 0;

        private void Start()
        {
            if(gameObject.name.Contains("Clone", System.StringComparison.OrdinalIgnoreCase) == false)
            {
                gameObject.SetActiveOptimized(false);
                return;
            }

            Destroy(gameObject, scalingTime);
        }

        private void Update()
        {
            timer += Time.deltaTime;

            float timeRatio = timer / scalingTime;
            float alpha = alphaOverLifetime.Evaluate(timeRatio);
            float scale = Mathf.MoveTowards(initialScale, targetScale, timeRatio);
            canvasGroup.alpha = alpha;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}