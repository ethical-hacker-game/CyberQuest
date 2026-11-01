using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single skill card shown on the Marketplace horizontal scroll list.
/// Instantiated and driven by MarketplaceUI.
/// </summary>
public class MarketplaceUI_SkillCard : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI priceLabel;
    [SerializeField] private GameObject unlockedBG;
    [SerializeField] private GameObject selectionFrame;
    [SerializeField] private Button button;

    public SkillData Skill { get; private set; }

    public void Setup(SkillData skill, Action<MarketplaceUI_SkillCard> onClick)
    {
        Skill = skill;

        nameLabel.text = skill.skillName;
        iconImage.sprite = PicturesManager.GetPicture(skill.skillID, PicturesManager.PictureType.Skill);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(this));

        Refresh();
        ToggleSelection(false);
    }

    public void Refresh()
    {
        if (Skill == null)
            return;

        bool unlocked = SkillsManager.IsUnlocked(Skill.skillID);

        unlockedBG.SetActive(unlocked);

        if (unlocked)
            priceLabel.text = "UNLOCKED";
        else
            priceLabel.text = Skill.price.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
    }

    public void ToggleSelection(bool selected)
    {
        selectionFrame.SetActive(selected);
    }
}
