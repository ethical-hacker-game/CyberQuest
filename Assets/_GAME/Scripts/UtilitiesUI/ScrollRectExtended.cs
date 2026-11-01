using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollRectExtended : ScrollRect
{
    public bool scrollByDrag = false;
    public bool scrollToTopOnEnable = false;
    public bool scrollToLeftOnEnable = false;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (scrollByDrag)
            base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (scrollByDrag)
            base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (scrollByDrag)
            base.OnEndDrag(eventData);
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (scrollToTopOnEnable)
            verticalNormalizedPosition = 0;

        if(scrollToLeftOnEnable)
            horizontalNormalizedPosition = 0;
    }
}
