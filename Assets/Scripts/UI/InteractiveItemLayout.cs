using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InteractiveItemLayout : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Action<InteractiveItemLayout> onDoubleClick;
    public Action<InteractiveItemLayout> onLongClick;

    private float _doubleClickLastClickTime = 0;  
    private Coroutine _longClickCoroutine = null;

    public void OnPointerClick(PointerEventData eventData) { 
        float currentTime = Time.time;
        if (currentTime - _doubleClickLastClickTime < 0.3f) {
            onDoubleClick?.Invoke(this);
            _doubleClickLastClickTime = 0;
            return;
        }

        _doubleClickLastClickTime = currentTime; 
    }

    public void OnPointerDown(PointerEventData eventData) {
        _longClickCoroutine = StartCoroutine(LongClickCoroutine(Time.time));
    }

    private IEnumerator LongClickCoroutine(float start) {
        while (Time.time - start < 0.5f)
            yield return null; 
        onLongClick?.Invoke(this);
        _longClickCoroutine = null;
    }
    
    public void OnPointerUp(PointerEventData eventData) {
        if (_longClickCoroutine != null) {
            StopCoroutine(_longClickCoroutine);
        }
    }
}
