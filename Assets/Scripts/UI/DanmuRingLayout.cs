using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DanmuRingLayout : MonoBehaviour, IDashboardListener {
    public enum DanmuRingMode {
        DownOnly,
        Ring
    }

    public GameObject danmuItemPrefab;

    private RectTransform _rectTransform;
    private readonly List<DanmuItemLayout> _items = new();
    private Vector2 _lastViewSize = Vector2.zero;

    public int columns = 2;
    public DanmuRingMode danmuRingMode = DanmuRingMode.Ring; 

    private Vector2 _currentItemPos = new Vector2();
    private Vector2 _lastItemPos = new Vector2();
    private int _rowCount = 0;
    private int CurrentItemIdx => (int) (_currentItemPos.x * _rowCount + _currentItemPos.y);
    private int LastItemIdx => (int) (_lastItemPos.x * _rowCount + _lastItemPos.y);
 
    private void Awake() {
        _rectTransform = GetComponent<RectTransform>();
        GetComponentInParent<Canvas>().pixelPerfect = true;
        BliveDanmuManager.Instance.DanmuEvent += OnDanmu;
        SettingManager.Settings.DanmuRingColumns += OnColumnChanged;
        SettingManager.Settings.DanmuRingMode += OnModeChanged;
        DashboardLayout.Instance.Listeners.Add(this);
    }

    private void OnDestroy() {
        BliveDanmuManager.Instance.DanmuEvent -= OnDanmu;
        SettingManager.Settings.DanmuRingColumns -= OnColumnChanged;
        SettingManager.Settings.DanmuRingMode -= OnModeChanged;
        DashboardLayout.Instance.Listeners.Remove(this);
    }

    private void OnColumnChanged(int value, bool changed) {
        columns = value + 1;
        if (changed)
            ResetLayout(true);
    }

    private void OnModeChanged(DanmuRingMode mode, bool changed) { 
        danmuRingMode = mode; 
        if (changed)
            ResetLayout(true);
    } 

    private void OnDanmu(Danmu danmu) {
        if (danmu.ShouldFilter())
            return;

        if (CurrentItemIdx >= _items.Count) {
            _currentItemPos = new Vector2();
        }

        if (_items.Count == 0)
            return;

        var reuse = _items.Where(c =>
            string.Equals(c.DanmuRaw, danmu.Content, StringComparison.CurrentCultureIgnoreCase));
        if (reuse.Any()) {
            reuse.First().AddUser(danmu.Username); 
        } else {
            _items[LastItemIdx].Latest = false;
            _items[CurrentItemIdx].Latest = true;
            _items[CurrentItemIdx].gameObject.SetActive(true);
            _items[CurrentItemIdx].SetContent(danmu);
            _lastItemPos = _currentItemPos;

            // Determine next position
            if (danmuRingMode == DanmuRingMode.Ring) {
                // All End
                if (_currentItemPos.x == columns - 1 &&
                    _currentItemPos.y == (_currentItemPos.x % 2 == 0 ? _rowCount - 1 : 0)) {
                    _currentItemPos = new Vector2();
                }
                // Column End
                else if (_currentItemPos.y == (_currentItemPos.x % 2 == 0 ? _rowCount - 1 : 0)) {
                    _currentItemPos = new Vector2(_currentItemPos.x + 1, _currentItemPos.y);
                } else {
                    _currentItemPos = new Vector2(_currentItemPos.x,
                        _currentItemPos.y + (_currentItemPos.x % 2 == 0 ? 1 : -1));
                }
            } else if (danmuRingMode == DanmuRingMode.DownOnly) {
                if (_currentItemPos.y == _rowCount - 1) {
                    _currentItemPos = new Vector2(_currentItemPos.x + 1, 0);
                } else {
                    _currentItemPos = new Vector2(_currentItemPos.x, _currentItemPos.y + 1);
                }
            }
        }
    }

    private void Start() {
        ResetLayout();
    }

    private void Update() {
        ResetLayout();
    }

    public void ResetLayout(bool forced = false) {
        if (_lastViewSize == _rectTransform.rect.size && !forced)
            return;
        _lastViewSize = _rectTransform.rect.size;

        var width = (_lastViewSize.x - 5 - 5 * columns) / columns;
        _rowCount = (int) Mathf.Floor((_lastViewSize.y - 5) / 85.0f);
        var height = (_lastViewSize.y - 5) / _rowCount - 5;
        var itemCount = _rowCount * columns;
        var delta = _items.Count - itemCount;
        if (itemCount < _items.Count) {
            for (var i = 0; i < delta; ++i) {
                var last = _items.Last();
                Destroy(last.gameObject);
                _items.Remove(last);
            }
            _currentItemPos = new Vector2();
            _lastItemPos = new Vector2();
        } else if (itemCount > _items.Count) {
            for (var i = 0; i < -delta; ++i) {
                var ins = Instantiate(danmuItemPrefab, transform);
                ins.SetActive(false);
                ins.GetComponent<Canvas>().pixelPerfect = true;
                var com = ins.GetComponent<DanmuItemLayout>();
                _items.Add(com);
            }
        }

        for (var c = 0; c < columns; ++c) {
            for (var i = 0; i < _rowCount; ++i) {
                var rt = _items[i + _rowCount * c].rectTransform;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(5 + width / 2 + c * (width + 5), -(height / 2 + 5) - i * (height + 5));
                rt.sizeDelta = new Vector2(width, height);
            }
        }

        var cur = Mathf.Clamp(CurrentItemIdx, 0, _items.Count);
        _currentItemPos = new Vector2(cur / _rowCount, cur % _rowCount);
        var lst = Mathf.Clamp(LastItemIdx, 0, _items.Count);
        _lastItemPos = new Vector2(lst / _rowCount, lst % _rowCount);
        foreach (var c in _items) {
            c.Latest = false;
        }
    }

    public void Clear() {
        foreach (var i in _items) {
            i.gameObject.SetActive(false);
        }
        _currentItemPos = new Vector2();
        _lastItemPos = new Vector2();
        ResetLayout(true);
    }

    public void LoadHistory() {
        
    }
}