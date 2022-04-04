using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DG.Tweening;
using JoshH.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DanmuItemLayout : InteractiveItemLayout, IRecyclerViewHolder {
    public RectTransform rectTransform { get; private set; }

    public Button button;
    public MedalLayout medal;
    public Text usernames;
    public GameObject latest;
    public TMP_Text repeatText;
    public TMP_Text content;

    public Image selfImage;

    public Gradient highlight;

    public bool Latest {
        set => latest.SetActive(value);
    }

    private readonly List<string> _usernameList = new List<string>();
    private readonly StringBuilder _usernameSb = new StringBuilder();
    private bool _usernameTruncated = false;
    private int _repeat = 0;

    public string DanmuRaw => content.text;

    private void Start() { 
        onLongClick = l => {  
            UniClipboard.SetText(content.text);
            Toast.Instance.ShowToast("已复制到剪贴板");
        };
    }

    private void OnEnable() {
        rectTransform = GetComponent<RectTransform>();
    }

    public void AddUser(string username) {
        _repeat++;
        repeatText.text = _repeat > 1 ? $"{_repeat}" : string.Empty;
        selfImage.color = highlight.Evaluate(Mathf.Min((_repeat - 1) / 10f, 1));
        repeatText.rectTransform.localScale = Vector3.one * 1.2f;
        repeatText.rectTransform.DOKill();
        repeatText.rectTransform.DOScale(Vector3.one, 0.3f);

        if (_usernameList.Contains(username)) return;
        _usernameList.Add(username);

        if (_usernameTruncated) return;

        var n = _usernameSb.Length == 0 ? username : $"、{username}";
        var length = usernames.CalculateWidth($"{_usernameSb}{n}");
        if (length > usernames.rectTransform.rect.width) {
            _usernameTruncated = true;
            _usernameSb.Clear();
            var truncatedText = "";
            foreach (var un in _usernameList) {
                _usernameSb.Append(_usernameSb.Length == 0 ? $"{un}" : $"、{un}");
                var tmpText = $"{_usernameSb} 等";
                if (usernames.CalculateWidth(tmpText) > usernames.rectTransform.rect.width) {
                    break;
                }

                truncatedText = tmpText;
            }

            usernames.text = truncatedText;
        } else {
            _usernameSb.Append(n);
            usernames.text = _usernameSb.ToString();
        }
    }

    private void SetMedal(string n, int level, int guardLevel) {
        if (!string.IsNullOrWhiteSpace(n)) {
            medal.gameObject.SetActive(true);
            medal.SetMedal(n, level, guardLevel);
            usernames.rectTransform.SetLeft(medal.rectTransform.sizeDelta.x + 20);
        } else {
            medal.gameObject.SetActive(false);
            usernames.rectTransform.SetLeft(15);
        }
    }

    public void SetContent(Danmu danmu) {
        _usernameSb.Clear();
        _usernameList.Clear();
        _usernameTruncated = false;
        _repeat = 0;

        content.text = danmu.Content;
        SetMedal(danmu.MedalName, danmu.MedalLevel, danmu.GuardLevel);
        AddUser(danmu.Username);
    }
}