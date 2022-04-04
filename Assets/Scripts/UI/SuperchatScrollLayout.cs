using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UI.Toggle;

public class SuperchatRecyclerLayout : RecyclerLayout<SuperchatItemLayout> { }

public class SuperchatScrollLayout : MonoBehaviour, SuperchatRecyclerLayout.IRecyclerAdapter, IDashboardListener {
    public GameObject prefab;

    private readonly List<Superchat> _items = new List<Superchat>();
    private SuperchatRecyclerLayout _recycler; 

    private RectTransform _rectTransform;
    private Vector2 _lastViewSize = Vector2.zero;
    
    private void Awake() {
        _recycler = gameObject.AddComponent<SuperchatRecyclerLayout>();
        _recycler.SetAdapter(this); 
        _rectTransform = GetComponent<RectTransform>();
        GetComponentInParent<Canvas>().pixelPerfect = true;
        BliveDanmuManager.Instance.SuperchatEvent += OnSuperchat;
        BliveDanmuManager.Instance.SuperchatDeleteEvent += OnSuperchatDelete;
        DashboardLayout.Instance.Listeners.Add(this); 
    }

    private void OnSuperchatDelete(SuperchatDelete del) { 
        _items.RemoveAll(a => del.SuperchatIds.Contains(a.Id));
        _recycler.NotifyDatasetChanged();
    }

    private void Update() {
        if (_lastViewSize == _rectTransform.rect.size)
            return;
        _lastViewSize = _rectTransform.rect.size;
        RefreshLayout();
    }

    private void RefreshLayout() {
        _recycler.NotifyDatasetChanged();
    }
    
    public void LoadHistory() {
        if (!SettingManager.Settings.SuperchatRestore)
            return;
        var notThanked = DatabaseManager.Instance.Room.Table<Superchat>().Where((sc) => !sc.Thanked).ToList();
        notThanked.Sort((a, b) => b.Time.CompareTo(a.Time)); 
        _items.AddRange(notThanked);
        _recycler.NotifyDatasetChanged();
    }

    public void Clear() {
        _items.Clear();
        _recycler.NotifyDatasetChanged();
    }

    private void OnDestroy() {
        BliveDanmuManager.Instance.SuperchatEvent -= OnSuperchat; 
        DashboardLayout.Instance.Listeners.Remove(this);
    }

    private void OnSuperchat(Superchat sc) {
        var exist = _items.FirstOrDefault(a => a.SuperchatId == sc.SuperchatId);
        if (exist == null) {
            _items.Insert(0, sc);
        } else {
            if (!string.IsNullOrWhiteSpace(sc.ContentJpn))
                exist.ContentJpn = sc.ContentJpn;
        }
        _recycler.NotifyDatasetChanged();
    }

    public SuperchatItemLayout OnCreateViewHolder() {
        var ins = Instantiate(prefab, _recycler.contentRect);
        var rect = ins.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.SetLeft(5);
        rect.SetRight(5);

        ins.GetComponent<Canvas>().pixelPerfect = true;
        var g = ins.GetComponent<SuperchatItemLayout>();
        g.onDoubleClick = (l) => {
            int idx = _recycler.GetViewHolderIndex(l);
            Debug.Log($"Double Click {idx}");
            _items[idx].Thanked = !_items[idx].Thanked;
            _recycler.NotifyDatasetChanged();
            DatabaseManager.Instance.Room.Update(_items[idx]);
        };
        g.onLongClick = l => { 
            int idx = _recycler.GetViewHolderIndex(l);
            Debug.Log($"Long Click {idx}"); 
            UniClipboard.SetText(_items[idx].Content);
            Toast.Instance.ShowToast("已复制到剪贴板");
        };
        return g;
    }

    public void OnBindViewHolder(SuperchatItemLayout holder, int i) {
        holder.SetContent(_items[i]);
    }

    public int GetItemCount() {
        return _items.Count;
    } 
}