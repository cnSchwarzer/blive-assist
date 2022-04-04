using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class Knob : MonoBehaviour, IDragHandler,IPointerEnterHandler,IPointerExitHandler {
    public UnityEvent<PointerEventData> target;
    public CanvasGroup group;
    public static float ratio = 1;
    
    public RectTransform widthLimit;

    private void Update() {
        if(widthLimit != null) {
            if (widthLimit.rect.width < 100) {
                group.alpha = 0;
                group.interactable = false;
            } else {
                group.alpha = 0.3f;
                group.interactable = true;
            }
        }
    }
    
    public void OnDrag(PointerEventData eventData) { 
        target?.Invoke(eventData);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        group.DOFade(1, 0.2f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        group.DOFade(0.3f, 0.2f);
    }
}
