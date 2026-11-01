using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextOverflowCursorHint : UICursorHint
{
    [SerializeField] private TextMeshProUGUI textMeshPro;

    public void ShowTextIfOverflow()
    {
        if(textMeshPro == null)
            return;

        if (textMeshPro.overflowMode != TextOverflowModes.Ellipsis)
            return;

        if (textMeshPro.isTextOverflowing == false)
            return;

        HintText = GetHiddenText();
        ShowHint();
    }

    public void HideTextHint()
    {
        HideHint();
    }

    protected override void Start()
    {
        base.Start();
        OnMouseEnter.RemoveAllListeners();
        OnMouseExit.RemoveAllListeners();
        OnMouseMove.RemoveAllListeners();
        OnMouseDown.RemoveAllListeners();
    }

    private string GetHiddenText()
    {
        textMeshPro.ForceMeshUpdate();

        var textInfo = textMeshPro.textInfo;
        int visibleCount = textInfo.characterCount;

        if (visibleCount <= 0 || visibleCount >= textMeshPro.text.Length)
            return string.Empty;

        int ellipsisLength = textMeshPro.overflowMode == TextOverflowModes.Ellipsis ? 3 : 0;

        int hiddenStartIndex = visibleCount - ellipsisLength + 1;
        hiddenStartIndex = Mathf.Clamp(hiddenStartIndex, 0, textMeshPro.text.Length);

        return "..." + textMeshPro.text.Substring(hiddenStartIndex);
    }
}
