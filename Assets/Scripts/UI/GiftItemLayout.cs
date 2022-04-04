using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text; 
using JoshH.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class GiftItemLayout : InteractiveItemLayout, IRecyclerViewHolder {
    public RectTransform rectTransform { get; private set; }

    public Sprite[] guardImages;
    
    public Button button;
    public MedalLayout medal;
    public Text username;
    public TMP_Text content;
    public Text price;
    public RectTransform contentTextRect;
    public Image guard;
    public GameObject thanked;
    
    void OnEnable() {
        rectTransform = GetComponent<RectTransform>(); 
    }

    private void SetMedal(string n, int level, int guardLevel) {
        if (!string.IsNullOrWhiteSpace(n)) {
            medal.gameObject.SetActive(true);
            medal.SetMedal(n, level, guardLevel);
            username.rectTransform.SetLeft(medal.rectTransform.sizeDelta.x + 20);
        }
        else {
            medal.gameObject.SetActive(false);
            username.rectTransform.SetLeft(20);
        }
    } 

    public void SetContent(Gift gift) {
        //Debug.Log(gift.Id);
        content.text = $"{gift.Action} {gift.Name}{(gift.Combo > 1 ? $" x{gift.Combo}" : "")}";
        username.text = gift.Username;
        contentTextRect.SetLeft(gift.IsGuardBuy ? 100 : 20);
        guard.gameObject.SetActive(gift.IsGuardBuy);
        guard.sprite = guardImages[gift.GuardLevel];
        thanked.SetActive(gift.Thanked);

        var priceStr = "";
        switch (gift.Unit) {
            case "gold":
                priceStr = $"{gift.Currency / 1000 * gift.Combo}￥"; 
                break;
            case "silver":
                priceStr = gift.Currency > 0 ? $"{gift.Currency * gift.Combo} 银瓜子" : ""; 
                break; 
        }
        price.text = priceStr + " " + gift.Time.ToString("MM.dd HH:mm"); 

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, gift.IsGuardBuy ? 120 : (content.preferredHeight + 40));
        SetMedal(gift.MedalName, gift.MedalLevel, gift.GuardLevel); 
    }
}