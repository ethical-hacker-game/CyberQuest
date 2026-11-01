using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Minigames.MinigamesController;

namespace Minigames
{
    public class MinigameBehaviour : GameSystemComponent
    {
        [SerializeField] private new string name;
        [SerializeField, EnumToggleButtons] private MinigameType minigameType;
        [SerializeField, FoldoutGroup("Minigame Settings")] private string instructionsLabel;
        [SerializeField, FoldoutGroup("Minigame Settings")] private bool resultOnTimeout = false;
        [SerializeField, FoldoutGroup("Minigame Settings")] private float standardDuration = 30;
        [SerializeField, FoldoutGroup("Minigame Settings")] private MinigameUI minigameUIPrefab;
        [SerializeField, FoldoutGroup("Minigame Settings")] private Sprite descriptionImage;
        [SerializeField, FoldoutGroup("Minigame Settings")] private string descriptionHeader;
        [TextArea(3, 10)]
        [SerializeField, FoldoutGroup("Minigame Settings")] private string description;

        protected bool isPaused = false;

        // Task-authored overrides applied through ApplyConfig before the minigame starts.
        protected float durationOverride = 0f;
        protected MinigameDifficulty difficulty = MinigameDifficulty.Default;

        public int Score { get; protected set; } = 0;
        public string Name { get => name; }
        public string InstructionsLabel { get => instructionsLabel; }
        public bool ResultOnTimeout { get => resultOnTimeout; }
        public MinigameUI MinigameUIPrefab { get => minigameUIPrefab; }
        public MinigameType MinigameType { get => minigameType; }
        public Sprite DescriptionImage { get => descriptionImage; }
        public string DescriptionHeader { get => descriptionHeader; }
        public string Description { get => description; }
        public virtual string Summary { get => string.Empty; }

        public bool IsPaused => isPaused;
        public MinigameDifficulty Difficulty => difficulty;

        public virtual float GetMinigameDuration
        {
            get
            {
                return durationOverride > 0f ? durationOverride : standardDuration;
            }
        }

        /// <summary>
        /// Applies task-authored configuration to this minigame instance.
        /// Called by <see cref="MinigamesController"/> right after the prefab is
        /// instantiated (so it runs after Awake but before the duration/UI are read
        /// and before Start). A null config leaves all prefab values untouched.
        /// Override in a concrete minigame to consume type-specific settings.
        /// </summary>
        public virtual void ApplyConfig(TaskMinigameVariant config)
        {
            if (config == null)
                return;

            if (config.durationOverride > 0f)
                durationOverride = config.durationOverride;

            difficulty = config.difficulty;
        }

        public virtual void PauseMinigame() 
        {
            isPaused = true;
        }

        public virtual void ResumeMinigame() 
        {
            isPaused = false;
        }

        public virtual void FinishMinigame(bool result, int score, string summary) => Instance.FinishMinigame(result, score, summary);

        public virtual void ChangeScore(int change)
        {
            Score += change;

            if (Score < 0)
                Score = 0;

            MinigamesUIManager.Instance.MainUI.UpdateScore(Score, change > 0);
        }

        protected virtual void Start()
        {
            ChangeScore(0);
        }
    }
}