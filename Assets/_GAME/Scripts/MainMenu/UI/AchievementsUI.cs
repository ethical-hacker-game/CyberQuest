using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MainMenu
{
    public class AchievementsUI : UI_Screen
    {
        [SerializeField] private Animator animator;

        public override void Back()
        {
            base.Back();
        }

        public override void ShowScreen()
        {
            base.ShowScreen();
            animator.Play("Instant In");
        }

        public override void HideScreen()
        {
            base.HideScreen();
        }

    }
}
