using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UI.Toggle;

public class GiftRecyclerLayout : RecyclerLayout<GiftItemLayout> { }

public class GiftScrollLayout : MonoBehaviour, GiftRecyclerLayout.IRecyclerAdapter, IDashboardListener {
    public GameObject prefab;
    public bool isGold;

    private readonly List<Gift> _items = new List<Gift>();
    private GiftRecyclerLayout _recycler;

    private int _paidThreshold => SettingManager.Settings.GiftPaidThreshold;
    private bool _showJoinRoom => SettingManager.Settings.GiftShowEnterRoom;

    private void Awake() {
        _recycler = gameObject.AddComponent<GiftRecyclerLayout>();
        _recycler.SetAdapter(this); 
        GetComponentInParent<Canvas>().pixelPerfect = true;
        BliveDanmuManager.Instance.GiftEvent += OnGift;
        DashboardLayout.Instance.Listeners.Add(this); 
    } 

    public void LoadHistory() {  
        if (isGold) {
            if (SettingManager.Settings.GiftPaidRestore) {
                var notThanked = DatabaseManager.Instance.Room.Table<Gift>()
                    .Where((g) => g.Unit == "gold" && !g.Thanked).ToList();
                _items.AddRange(notThanked.Where(g => g.Price >= _paidThreshold));
            }
        } else {
            if (SettingManager.Settings.GiftFreeRestore) {
                var notThanked = DatabaseManager.Instance.Room.Table<Gift>()
                    .Where((g) => g.Unit != "gold" && !g.Thanked);
                _items.AddRange(notThanked);
            }
        }

        _recycler.NotifyDatasetChanged();
    }

    public void Clear() {
        _items.Clear();
        _recycler.NotifyDatasetChanged();
    }

    private void OnDestroy() {
        BliveDanmuManager.Instance.GiftEvent -= OnGift; 
        DashboardLayout.Instance.Listeners.Remove(this);
    }

    private void OnGift(Gift gift) {
        if ((isGold && gift.Unit != "gold") || (!isGold && gift.Unit == "gold"))
            return;

        if (isGold) {
            if (gift.Currency / 1000 * gift.Combo < _paidThreshold) {
                return;
            }
        }

        if (!isGold && !_showJoinRoom && gift.IsJoinRoom) {
            return;
        }
        
        // By combo id
        if (!string.IsNullOrWhiteSpace(gift.ComboId)) {
            var combo = _items.Where((b) => b.ComboId == gift.ComboId);
            if (combo.Any()) {
                var oldGift = combo.First();
                oldGift.Combo = gift.Combo;
                DatabaseManager.Instance.Room.Update(oldGift);
                DatabaseManager.Instance.RemoveGift(gift);
                _recycler.NotifyDatasetChanged();
                return;
            }
        }

        // By exist
        var exist = _items.Where(
            (b) =>
                DateTime.Now - b.Time < TimeSpan.FromMinutes(1) &&
                b.Name == gift.Name &&
                b.UserId == gift.UserId);
        if (exist.Any()) {
            var oldGift = exist.First();
            oldGift.Combo += gift.Combo;
            DatabaseManager.Instance.Room.Update(oldGift);
            DatabaseManager.Instance.RemoveGift(gift);
            _recycler.NotifyDatasetChanged();
            return;
        }

        _items.Insert(0, gift);
        _recycler.NotifyDatasetChanged();
    }

    public GiftItemLayout OnCreateViewHolder() {
        var ins = Instantiate(prefab, _recycler.contentRect);
        var rect = ins.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.SetLeft(5);
        rect.SetRight(5);

        ins.GetComponent<Canvas>().pixelPerfect = true;
        var g = ins.GetComponent<GiftItemLayout>();
        g.onDoubleClick = (l) => {
            int idx = _recycler.GetViewHolderIndex(l);
            Debug.Log($"Double Click {idx}");
            _items[idx].Thanked = !_items[idx].Thanked;
            _recycler.NotifyDatasetChanged();
            DatabaseManager.Instance.Room.Update(_items[idx]);
        }; 
        return g;
    }

    public void OnBindViewHolder(GiftItemLayout holder, int i) {
        holder.SetContent(_items[i]);
    }

    public int GetItemCount() {
        return _items.Count;
    } 
}