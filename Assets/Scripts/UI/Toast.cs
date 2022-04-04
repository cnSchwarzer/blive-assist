using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using DG.Tweening;

public class Toast : MonoBehaviour {
    public static Toast Instance { get; private set; }

    public RectTransform rect;
    public Text content;

    private Sequence _seq;
    private readonly ConcurrentQueue<string> _toastQueue = new ConcurrentQueue<string>();
    private bool _toastBusy = false;

    private void Start() {
        Instance = this;
    }

    private void Update() {
        if (!_toastQueue.IsEmpty && !_toastBusy && _toastQueue.TryDequeue(out string text)) {
            _toastBusy = true;
            float height = content.CalculateHeight(text);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 0);
            content.text = text;
            content.color = new Color(content.color.r, content.color.g, content.color.b, 0);

            _seq = DOTween.Sequence();
            _seq.SetRecyclable(true);
            _seq.Append(rect.DOSizeDelta(new Vector2(rect.sizeDelta.x, height + 50), 0.3f))
                .Join(content.DOFade(1, 0.3f))
                .AppendInterval(3f)
                .Append(rect.DOSizeDelta(new Vector2(rect.sizeDelta.x, 0), 0.3f))
                .Join(content.DOFade(0, 0.3f))
                .AppendInterval(0.5f)
                .OnComplete(() => { _toastBusy = false; })
                .Play();
        }
    }

    public void ShowToast(string text) {
        if (_toastQueue.Count < 100)
            _toastQueue.Enqueue(text);
    }
}