using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UI.Toggle;

public class DanmuRecyclerLayout : RecyclerLayout<DanmuItemLayout> { }

public class DanmuScrollLayout : MonoBehaviour, DanmuRecyclerLayout.IRecyclerAdapter, IDashboardListener {
    public GameObject prefab;
    public GameObject highlightPrefab;

    public RectTransform highlightRoot;
    public RectTransform viewport;
    private List<DanmuHighlightLayout> _highlights = new();
    private List<DanmuHighlightLayout> _highlightsCache = new();
    
    private readonly List<Danmu> _items = new();
    private DanmuRecyclerLayout _recycler; 

    private RectTransform _rectTransform;
    private Vector2 _lastViewSize = Vector2.zero;

    public RectTransform split;

    private void Awake() {
        _recycler = gameObject.AddComponent<DanmuRecyclerLayout>();
        _recycler.SetAdapter(this); 
        _rectTransform = GetComponent<RectTransform>();
        GetComponentInParent<Canvas>().pixelPerfect = true;
        BliveDanmuManager.Instance.DanmuEvent += OnDanmu;
        DashboardLayout.Instance.Listeners.Add(this);
        SettingManager.Settings.DanmuRollShowRepeatOnly += (a, b) => {
            if (a) {  
                _recycler.NotifyDatasetChanged();
            }
        };
    }

    private void RefreshHighlight() {
        if (!_highlights.Any()) {
            split.gameObject.SetActive(false);
            highlightRoot.gameObject.SetActive(false);
            viewport.SetBottom(0);
        } else {
            float y = -5;
            highlightRoot.gameObject.SetActive(true);
            split.gameObject.SetActive(true);
            split.anchoredPosition = new Vector2(0, y);
            y -= split.sizeDelta.y + 5;
            if (SettingManager.Settings.DanmuShowRepeatSort) {
                _highlights.Sort((a, b) => { 
                    var c1 = a.count.CompareTo(b.count);
                    if (c1 == 0)
                        return a.Percent.CompareTo(b.Percent);
                    return c1;
                });
            }
            foreach (var highlight in _highlights) {
                var rect = highlight.rectTransform;
                rect.anchoredPosition = new Vector2(0, y - rect.sizeDelta.y / 2);
                y -= (rect.sizeDelta.y + 5);
            }
            highlightRoot.sizeDelta = new Vector2(highlightRoot.sizeDelta.x, -y);
            viewport.SetBottom(-y + 5);
        }
    }
    
    private void Update() { 
        foreach (var highlight in _highlights) {
            var slower = 1f - Mathf.Clamp(highlight.count / 50f, 0, 1) * 0.5f;
            highlight.Percent += Time.deltaTime / SettingManager.Settings.DanmuShowRepeatDuration * slower;
        }

        var removal = _highlights.Where(a => a.Percent >= 1).ToList();
        if (removal.Any()) {
            foreach (var r in removal) {
                r.gameObject.SetActive(false);
                r.Percent = 0;
            }
        
            _highlightsCache.AddRange(removal);
            _highlights.RemoveAll(a => removal.Contains(a));

            RefreshHighlight();
        }
        
        if (_lastViewSize == _rectTransform.rect.size)
            return;
        _lastViewSize = _rectTransform.rect.size;
        RefreshLayout();
    }

    private void RefreshLayout() {
        if (!SettingManager.Settings.DanmuRollShowRepeatOnly) {
            _recycler.NotifyDatasetChanged();
        }
    }
    
    public void LoadHistory() {
        
    }

    public void Clear() {
        _items.Clear();
        _highlightsCache.AddRange(_highlights);
        foreach (var h in _highlights) {
            h.gameObject.SetActive(false);
        }
        _highlights.Clear();  
        RefreshHighlight();
        if (!SettingManager.Settings.DanmuRollShowRepeatOnly) {
            _recycler.NotifyDatasetChanged();
        } 
    }
    
    private void OnDanmu(Danmu danmu) {
        if (danmu.ShouldFilter())
            return;

        _items.Insert(0, danmu);
        while (_items.Count > 1000) {
            _items.RemoveAt(_items.Count - 1);
        }
        if (!SettingManager.Settings.DanmuRollShowRepeatOnly) {
            _recycler.NotifyDatasetChanged();
        }

        foreach (var highlight in _highlights) {
            if (highlight.content.text.Equals(danmu.Content, StringComparison.OrdinalIgnoreCase)) {
                highlight.Add();
                if (SettingManager.Settings.DanmuShowRepeatSort) {
                    RefreshHighlight();
                }
                return;
            }
        }

        // Add new one
        var count = _items.Count(a => a.Content.Equals(danmu.Content, StringComparison.OrdinalIgnoreCase) 
                                      && DateTime.Now - a.Time < TimeSpan.FromMinutes(1));
        if (count >= SettingManager.Settings.DanmuShowRepeatThreshold) {
            DanmuHighlightLayout hl = null;
            if (_highlightsCache.Any()) {
                hl = _highlightsCache.First();
                _highlightsCache.RemoveAt(0);
            } else {
                hl = Instantiate(highlightPrefab, highlightRoot).GetComponent<DanmuHighlightLayout>();
            }
            var rect = hl.rectTransform;
            hl.GetComponent<Canvas>().pixelPerfect = true;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.SetLeft(5);
            rect.SetRight(5);
            hl.gameObject.SetActive(true);
            hl.Init(danmu.Content, count);
            _highlights.Add(hl);
            
            RefreshHighlight();
        }
    }

    private void OnDestroy() {
        BliveDanmuManager.Instance.DanmuEvent -= OnDanmu;
        DashboardLayout.Instance.Listeners.Remove(this);
    } 

    public DanmuItemLayout OnCreateViewHolder() {
        var ins = Instantiate(prefab, _recycler.contentRect);
        var rect = ins.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.SetLeft(5);
        rect.SetRight(5);

        ins.GetComponent<Canvas>().pixelPerfect = true;
        var g = ins.GetComponent<DanmuItemLayout>(); 
        g.onLongClick = l => { 
            int idx = _recycler.GetViewHolderIndex(l);
            Debug.Log($"Long Click {idx}"); 
            UniClipboard.SetText(_items[idx].Content);
            Toast.Instance.ShowToast("已复制到剪贴板");
        };
        return g;
    }

    public void OnBindViewHolder(DanmuItemLayout holder, int i) {
        holder.Latest = i == 0;
        holder.SetContent(_items[i]);
    }

    public int GetItemCount() {
        if (SettingManager.Settings.DanmuRollShowRepeatOnly) {
            return 0;
        } 
        return _items.Count;
    } 
}