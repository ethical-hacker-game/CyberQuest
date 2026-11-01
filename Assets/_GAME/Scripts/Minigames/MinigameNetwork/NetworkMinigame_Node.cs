using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Minigames
{
    public class NetworkMinigame_Node : MonoBehaviour
    {
        [SerializeField] private float scaleWhenOld = 0.1f;
        [SerializeField] private float scaleWhenSatelite = 0.3f;
        [SerializeField] private float scaleWhenMain = 0.8f;


        public int NodeIndex { get; set; } = -1;
        public int DevicesCount { get; set; } = 0;
        public bool IsDestroying { get; set; } = false;

        private void Start()
        {
            transform.localScale = Vector3.zero;
            //UpdateScale(true);
        }

        private void Update()
        {
            UpdateScale();
        }

        private void UpdateScale(bool instant = false)
        {
            float tempScale = transform.localScale.x;
            float targetScale = 0;

            if (IsDestroying)
                targetScale = 0;
            else if (NodeIndex == -1)
                targetScale = scaleWhenMain;
            else if (NodeIndex == -2)
                targetScale = scaleWhenOld;
            else
                targetScale = scaleWhenSatelite;

            if (instant)
                tempScale = targetScale;
            else
                tempScale = Mathf.MoveTowards(tempScale, targetScale, Time.deltaTime * 0.6f);

            transform.localScale = new Vector3(tempScale, tempScale, tempScale);

            if (IsDestroying && tempScale <= 0)
                Destroy(gameObject);
        }

        private void OnMouseEnter()
        {
            if (NodeIndex == -1 || MinigamesController.isAnyMinigameActive == false)
                return;

            string hackText = DevicesCount > 0 ? $"Hack <b>{DevicesCount} devices</b>" : "Hack";
            Vector2 textPosition = NetworkMinigame.MinigameInstance.MinigameCamera.WorldToScreenPoint(transform.position);

            CursorHintsManager.Instance?.ShowHint(hackText, textPosition, this);
        }

        private void OnMouseExit()
        {
            if (NodeIndex == -1 || MinigamesController.isAnyMinigameActive == false)
                return;

            CursorHintsManager.Instance?.ClearHint(this);
        }

        private void OnMouseDown()
        {
            if (NodeIndex == -1)
                return;

            NetworkMinigame.MinigameInstance.HackNode(this);
            CursorHintsManager.Instance?.ClearHint(this);
        }

        public void BecomeMain()
        {
            NodeIndex = -1;

            int scoreToAdd = (int)MinigamesController.GetTimerPositive;

            if (DevicesCount > 0)
            {
                scoreToAdd *= DevicesCount;
                scoreToAdd /= 300;
            }

            if (NetworkMinigame.MinigameInstance.NetworkDistance == false)
                NetworkMinigame.MinigameInstance.ChangeScore(scoreToAdd);
        }

        public void DestroyNode()
        {
            NodeIndex = -1;
            IsDestroying = true;
        }
    }
}
