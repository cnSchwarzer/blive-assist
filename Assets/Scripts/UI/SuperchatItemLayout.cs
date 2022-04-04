using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text; 
using JoshH.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Utility;

[RequireComponent(typeof(RectTransform))]
public class SuperchatItemLayout : InteractiveItemLayout, IRecyclerViewHolder {
    public RectTransform rectTransform { get; private set; }  

    public Button button;
    public MedalLayout medal;
    public Text username;
    public TMP_Text content;
    public Image backgroundImage, headerImage;
    public Image faceImage, faceFrameImage;
    public Text price;
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

    private Coroutine _faceCoroutine;
    private string _currentFace;

    private void SetFace(string face, string frame) {
        if (_currentFace != face + frame) {
            if (_faceCoroutine != null)
                StopCoroutine(_faceCoroutine);
            _currentFace = face + frame;
            _faceCoroutine = StartCoroutine(SetFaceCoroutine(face, frame));
        }
    }
    
    private IEnumerator SetFaceCoroutine(string face, string frame) { 
        yield return WebSpriteUtility.Load(face, faceImage);

        if (!string.IsNullOrWhiteSpace(frame)) {
            faceFrameImage.enabled = true;
            yield return WebSpriteUtility.Load(frame, faceFrameImage);
        }
        else {
            faceFrameImage.enabled = false;
        }

        _faceCoroutine = null;
    }

    private Color ParseColorString(string str) {
        str = str.Replace("#", "");

        if (str.Length == 6) {
            var r = str[0..2];
            var g = str[2..4];
            var b = str[4..6];
            return new Color(int.Parse(r, NumberStyles.HexNumber) / 255f,
                int.Parse(g, NumberStyles.HexNumber) / 255f,
                int.Parse(b, NumberStyles.HexNumber) / 255f);
        } 
        return Color.black;
    }

    public void SetContent(Superchat sc) {
        //Debug.Log(sc.Id);
        content.text = sc.Content;
        if (!string.IsNullOrWhiteSpace(sc.ContentJpn)) {
            content.text += $"\n<size=25>{sc.ContentJpn}</size>";
        }
        username.text = sc.Username;
        price.text = sc.Price + "￥ " + sc.Time.ToString("MM.dd HH:mm");
        thanked.SetActive(sc.Thanked);
        
        backgroundImage.color = ParseColorString(sc.BackgroundColor);
        headerImage.color = ParseColorString(sc.HeaderColor);

        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, Math.Max(content.preferredHeight + 40, 120));
        SetMedal(sc.MedalName, sc.MedalLevel, sc.GuardLevel); 
        SetFace(sc.Face, sc.FaceFrame);
    }
}