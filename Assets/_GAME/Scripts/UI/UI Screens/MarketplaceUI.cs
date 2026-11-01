using Michsky.UI.Beam;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Marketplace screen: a horizontally scrolled list of skill cards (loaded from the
/// active unit's skill definitions) plus a details panel with an unlock button.
/// Replaces the old hand-built radial skill tree.
/// </summary>
public class MarketplaceUI : UI_Screen
{
    [Header("Skill Cards")]
    [SerializeField] private RectTransform cardsParent;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private float cardsSpacing = 30f;

    [Header("Skill Details")]
    [SerializeField] private LayoutGroupFix layoutRebuilder;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI descriptionTMP;
    [SerializeField] private TextMeshProUGUI statusTMP;
    [SerializeField] private ButtonManager unlockButton;

    private readonly List<MarketplaceUI_SkillCard> cards = new List<MarketplaceUI_SkillCard>();
    private SkillData selectedSkill;

    public override void ShowScreen()
    {
        base.ShowScreen();

        RebuildCards();
        SelectSkill(null);
    }

    public override void Back()
    {
        GameplayUIManager.ShowGameUI();
        base.Back();
    }

    public void UnlockSelected()
    {
        if (selectedSkill == null)
            return;

        if (SkillsManager.TryUnlock(selectedSkill) == false)
            return;

        RefreshCards();
        ShowSkillDetails();
    }

    private void OnEnable()
    {
        SkillsManager.OnSkillUnlocked += OnSkillUnlocked;
    }

    private void OnDisable()
    {
        SkillsManager.OnSkillUnlocked -= OnSkillUnlocked;
    }

    private void OnSkillUnlocked(SkillData skill)
    {
        RefreshCards();

        if (gameObject.activeInHierarchy)
            ShowSkillDetails();
    }

    private void SelectSkill(SkillData skill)
    {
        selectedSkill = skill;
        HighlightSelectedCard();
        ShowSkillDetails();
    }

    private void OnCardClicked(MarketplaceUI_SkillCard card)
    {
        SelectSkill(card.Skill);
    }

    private void RebuildCards()
    {
        EnsureCardsLayout();

        cards.Clear();

        for (int i = cardsParent.childCount - 1; i >= 0; i--)
        {
            cardsParent.GetChild(i).gameObject.Destroy();
        }

        List<SkillData> skills = SkillsManager.Skills;

        for (int i = 0; i < skills.Count; i++)
        {
            MarketplaceUI_SkillCard newCard = Instantiate(cardPrefab, cardsParent)
                .GetComponent<MarketplaceUI_SkillCard>();

            newCard.Setup(skills[i], OnCardClicked);
            cards.Add(newCard);
        }
    }

    private void RefreshCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].Refresh();
        }
    }

    private void HighlightSelectedCard()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            cards[i].ToggleSelection(selectedSkill != null
                && cards[i].Skill.skillID == selectedSkill.skillID);
        }
    }

    private void ShowSkillDetails()
    {
        if (selectedSkill == null)
        {
            titleTMP.text = "Select a skill";
            descriptionTMP.text = "Select a skill to view its details.";
            statusTMP.text = "-";
            UpdateUnlockButton(SkillsManager.SkillUnlockState.None);
            layoutRebuilder.FixLayout();
            return;
        }

        SkillsManager.SkillUnlockState state = SkillsManager.GetUnlockState(selectedSkill);

        titleTMP.text = selectedSkill.skillName;
        descriptionTMP.text = GetDescriptionText(state);
        statusTMP.text = SkillsManager.GetStateDescription(state);

        UpdateUnlockButton(state);
        layoutRebuilder.FixLayout();
    }

    private string GetDescriptionText(SkillsManager.SkillUnlockState state)
    {
        string description = selectedSkill.skillDescription;
        return description;
    }

    private void UpdateUnlockButton(SkillsManager.SkillUnlockState state)
    {
        if (unlockButton == null)
            return;

        bool showButton = state != SkillsManager.SkillUnlockState.None
            && state != SkillsManager.SkillUnlockState.Unlocked;

        unlockButton.gameObject.SetActiveOptimized(showButton);

        if (showButton == false)
            return;

        unlockButton.SetText($"Unlock");
        unlockButton.Interactable(state == SkillsManager.SkillUnlockState.Available);
    }

    private void EnsureCardsLayout()
    {
        ScrollRect scrollRect = cardsParent.GetComponentInParent<ScrollRect>(true);

        if (scrollRect != null)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            if (scrollRect.verticalScrollbar != null)
                scrollRect.verticalScrollbar.gameObject.SetActiveOptimized(false);
        }

        // Stretch the content vertically inside the viewport and let the layout
        // group + size fitter drive the horizontal size.
        cardsParent.anchorMin = new Vector2(0f, 0f);
        cardsParent.anchorMax = new Vector2(0f, 1f);
        cardsParent.pivot = new Vector2(0f, 0.5f);
        cardsParent.anchoredPosition = Vector2.zero;
        cardsParent.sizeDelta = new Vector2(cardsParent.sizeDelta.x, 0f);

        HorizontalLayoutGroup layoutGroup = cardsParent.GetComponent<HorizontalLayoutGroup>();

        if (layoutGroup == null)
            layoutGroup = cardsParent.gameObject.AddComponent<HorizontalLayoutGroup>();

        layoutGroup.spacing = cardsSpacing;
        layoutGroup.padding = new RectOffset((int)cardsSpacing, (int)cardsSpacing, 0, 0);
        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = cardsParent.GetComponent<ContentSizeFitter>();

        if (sizeFitter == null)
            sizeFitter = cardsParent.gameObject.AddComponent<ContentSizeFitter>();

        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }
}
