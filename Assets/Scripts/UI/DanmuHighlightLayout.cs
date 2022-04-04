using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DanmuHighlightLayout : MonoBehaviour {
    public RectTransform rectTransform;
    public RectTransform sliderMask;
    public RectTransform slider; 
    public TMP_Text content;
    public Gradient highlight;
    public TMP_Text comboText;
    public Image selfImage;
    public CanvasGroup canvasGroup;
    [HideInInspector]
    public int count;  

    public float Percent { get; set; }
    
    void Update() {
        var w = rectTransform.rect.width;
        slider.sizeDelta = new Vector2(w, 0);
        sliderMask.sizeDelta = new Vector2(w * Percent, 0);
        canvasGroup.alpha = Percent < 0.95f ? 1 : Mathf.Sin(Mathf.Clamp((1 - Percent) / 0.05f, 0, 1) * Mathf.PI / 2);
    }

    private void SetColor() { 
        selfImage.color = highlight.Evaluate(Mathf.Min((count - 1) / 50f, 1));
    }
    
    public void Init(string text, int count) {
        Percent = 0;
        content.text = text;
        this.count = count;
        comboText.text = this.count.ToString();
        SetColor();
    }
    
    public void Add() {
        Percent = 0;
        count++;
        comboText.text = count.ToString();
        SetColor();
    }
}
