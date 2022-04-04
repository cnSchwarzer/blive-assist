using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollRectLayout : MonoBehaviour, IScrollHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler {
    public RectTransform content;
    public RectTransform viewport;

    public float position {
        get => content.anchoredPosition.y;
        set => content.anchoredPosition = new Vector2(content.anchoredPosition.x, value);
    }

    private bool interactive;
    
    public void OnScroll(PointerEventData eventData) {
        position -= eventData.scrollDelta.y * 20;
    } 

    public void OnDrag(PointerEventData eventData) {
        if (interactive)
            position += eventData.delta.y * 2;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        interactive = true;
    }

    public void OnPointerExit(PointerEventData eventData) {
        interactive = false;
    }
}
