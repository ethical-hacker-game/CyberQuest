using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WPM;

public class StickerQuest : MonoBehaviour
{
    [Header("Label UI")]
    [SerializeField] private Transform stickerUI;
    [SerializeField] private TextMeshProUGUI questLabel;
    [Header("State UI")]
    [SerializeField] private GameObject lockedSticker;
    [SerializeField] private GameObject unlockedSticker;
    [SerializeField] private GameObject activeSticker;
    [SerializeField] private GameObject doneSticker;
    [SerializeField] private TextMeshProUGUI questNumberLabel;
    [SerializeField] private Image quectIcon1;
    [SerializeField] private Image quectIcon2;

    [Header("Sticker settings")]
    [SerializeField] private float stickerScalingSpeed = 8;
    [SerializeField] private float blinkingSpeed = 3;

    private QuestReferences myQuestRef;
    private Quest MyQuestData => myQuestRef.data;
    private QuestInfo MyQuestInfo => myQuestRef.info;
    private STICKER_STATUS stickerStatus;
    private WorldMapGlobe globe;

    private float targetUIScale = 0;
    private Color targetUIColor;

    public void SetupSticker(QuestReferences questRef)
    {
        myQuestRef = questRef;
        questNumberLabel.text = (GameController.SelectedUnit.questIds.IndexOf(MyQuestData.questID) + 1).ToString();
        quectIcon1.sprite = MyQuestData.GetIcon();
        quectIcon2.sprite = MyQuestData.GetIcon();
        UpdateSticker();
    }

    public void ClickSticker()
    {
        if (stickerStatus == STICKER_STATUS.Locked)
            return;

        PlayerController.Instance.OpenQuest(MyQuestData.questID);
    }

    public void UpdateSticker()
    {
        if(MyQuestInfo == null)
        {
            stickerStatus = STICKER_STATUS.Locked;
            lockedSticker.SetActiveOptimized(true);
            unlockedSticker.SetActiveOptimized(false);
            activeSticker.SetActiveOptimized(false);
            doneSticker.SetActiveOptimized(false);
            return;
        }

        questLabel.text = MyQuestData.GetQuestName(MyQuestInfo.persona);

        if (MyQuestInfo.IsDone)
        {
            stickerStatus = STICKER_STATUS.Done;
            lockedSticker.SetActiveOptimized(false);
            unlockedSticker.SetActiveOptimized(false);
            activeSticker.SetActiveOptimized(false);
            doneSticker.SetActiveOptimized(true);
        }
        else if(MyQuestInfo.timerStarted == false)
        {
            stickerStatus = STICKER_STATUS.Locked;
            lockedSticker.SetActiveOptimized(true);
            unlockedSticker.SetActiveOptimized(false);
            activeSticker.SetActiveOptimized(false);
            doneSticker.SetActiveOptimized(false);
            //stickerStatus = STICKER_STATUS.Unlocked;
            //lockedSticker.SetActiveOptimized(false);
            //unlockedSticker.SetActiveOptimized(true);
            //activeSticker.SetActiveOptimized(false);
            //doneSticker.SetActiveOptimized(false);
        }
        else if(MyQuestInfo.IsDone == false)
        {
            stickerStatus = STICKER_STATUS.Active;
            lockedSticker.SetActiveOptimized(false);
            unlockedSticker.SetActiveOptimized(false);
            activeSticker.SetActiveOptimized(true);
            doneSticker.SetActiveOptimized(false);
        }
    }

    public void SetUIScale(float value)
    {
        if (stickerStatus != STICKER_STATUS.Locked)
            targetUIScale = value;
    }

    private void Update()
    {
        //if (stickerStatus == STICKER_STATUS.Done)
        //{
        //    doneImage.fillAmount = Mathf.MoveTowards(doneImage.fillAmount, 1, Time.deltaTime * fillSpeed);
        //}
        //else if (stickerStatus == STICKER_STATUS.Active)
        //{
        //    float pingPongTime = Mathf.PingPong(Time.timeSinceLevelLoad * blinkingSpeed, 1);
        //    ringOutline.color = Color.Lerp(colorLocked, colorActive, pingPongTime);

        //    activeImage.color = stickerStatus == STICKER_STATUS.Done ? Color.clear : ringOutline.color;
        //}

        float tempScale = Mathf.MoveTowards(stickerUI.localScale.x, targetUIScale, Time.deltaTime * stickerScalingSpeed);
        stickerUI.localScale = new Vector3(tempScale, tempScale, tempScale);
    }

    private enum STICKER_STATUS
    {
        Locked,
        Unlocked,
        Active,
        Done
    }
}
