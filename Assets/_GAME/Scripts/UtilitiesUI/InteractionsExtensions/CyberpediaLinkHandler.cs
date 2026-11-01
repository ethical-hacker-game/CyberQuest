using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public sealed class CyberpediaLinkHandler : UIInteractions
{
    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            textComponent,
            eventData.position,
            eventData.pressEventCamera
        );

        if (linkIndex < 0)
            return;

        string articleID = textComponent.textInfo.linkInfo[linkIndex].GetLinkID();

        if (string.IsNullOrWhiteSpace(articleID))
            return;

        if (CyberpediaManager.Instance == null)
        {
            Debug.LogWarning($"Cannot open Cyberpedia article before the manager is initialized: {articleID}");
            return;
        }

        CyberpediaManager.Instance.OpenArticle(articleID);
        base.OnPointerClick(eventData);
    }
}
