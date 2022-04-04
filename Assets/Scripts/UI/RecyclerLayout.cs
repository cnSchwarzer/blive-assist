using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public interface IRecyclerViewHolder {
    GameObject gameObject { get; }
    RectTransform rectTransform { get; }
    float height => rectTransform.sizeDelta.y; 
    
    float startPosition {
        get => rectTransform.anchoredPosition.y + height * rectTransform.pivot.y;
        set => rectTransform.anchoredPosition =
            new Vector2(rectTransform.anchoredPosition.x, value - height * rectTransform.pivot.y);
    }

    float endPosition {
        get => startPosition - height;
        set => startPosition = value + height;
    }
}

[RequireComponent(typeof(ScrollRectLayout), typeof(RectTransform))]
public abstract class RecyclerLayout<VH> : MonoBehaviour
    where VH : IRecyclerViewHolder {
    public interface IRecyclerAdapter {
        VH OnCreateViewHolder();
        void OnBindViewHolder(VH holder, int i);
        int GetItemCount(); 
    }

    public ScrollRectLayout scrollRect;
    public RectTransform viewportRect => scrollRect.viewport;
    public RectTransform contentRect => scrollRect.content;
    
    private float _lastContentPos = 0;

    private float contentPos {
        get => -scrollRect.position;
        set => scrollRect.position = -value;
    }

    private int _dataStartIdx = 0;
    private int _dataEndIdx => _dataStartIdx + _presentViews.Count - 1;
    private List<VH> _presentViews = new List<VH>();
    private List<VH> _cachedViews = new List<VH>();
    private IRecyclerAdapter _adapter;
    private float _viewportHeight => viewportRect.rect.height;

    public float offsetWhenStartItemRecycle = 100;
    public float offsetWhenStartItemLoad = 50;
    public float offsetWhenEndItemRecycle = 100;
    public float offsetWhenEndItemLoad = 50;

    public float margin = 5; 

    private bool _datasetChanged = false;

    private void OnEnable() {
        scrollRect = GetComponent<ScrollRectLayout>();  
    }

    public void SetAdapter(IRecyclerAdapter adapter) {
        _adapter = adapter;
    }
 
    private void Update() {  
        if (_lastContentPos != contentPos
            || !_presentViews.Any()
            || _datasetChanged) {
            if (_datasetChanged) {
                OnDatasetChanged();
                _datasetChanged = false;
            }

            OnViewportMoved();
            _lastContentPos = contentPos;
        }
    }

    public void NotifyDatasetChanged() {
        _datasetChanged = true;
    } 
    
    private VH GetIdleViewHolder() {
        if (_cachedViews.Count == 0)
            return _adapter.OnCreateViewHolder();
        var ret = _cachedViews[0];
        ret.gameObject.SetActive(true);
        _cachedViews.RemoveAt(0);
        return ret;
    }

    private void BindViewHolder(VH view, int idx) { 
        _adapter.OnBindViewHolder(view, idx);
    }
    
    private void OnDatasetChanged() { 
        if (!_presentViews.Any())
            return;

        if (_adapter.GetItemCount() == 0) {
            foreach (var v in _presentViews) {
                v.gameObject.SetActive(false);
            }
            _cachedViews.AddRange(_presentViews);
            _presentViews.Clear();
            _dataStartIdx = 0;
            return;
        }
        
        // Maintain current index
        var idx = _dataStartIdx;
        if (idx > _adapter.GetItemCount() - 1) {
            var itemsToDelete = _adapter.GetItemCount() - idx + 1;
            for (var i = 0; i < itemsToDelete; ++i) {
                var v = _presentViews.Last();
                v.gameObject.SetActive(false);
                _cachedViews.Add(v);
                _presentViews.RemoveAt(_presentViews.Count - 1);
            }

            idx = _adapter.GetItemCount() - 1;
        }

        while (_presentViews.Count > _adapter.GetItemCount()) { 
            var v = _presentViews.Last();
            v.gameObject.SetActive(false);
            _cachedViews.Add(v);
            _presentViews.RemoveAt(_presentViews.Count - 1);
        }

        foreach (var view in _presentViews) {
            var oldPos = view.startPosition;
            BindViewHolder(view, idx++);
            view.startPosition = oldPos;
        }

        // Recalculate Heights
        if (_presentViews.Any()) {
            var pos = _presentViews.First().startPosition;
            for (var i = 1; i < _presentViews.Count; ++i) {
                pos -= _presentViews[i - 1].height + margin;
                _presentViews[i].startPosition = pos;
            }
        }
    }

    private void OnViewportMoved() {
        float viewportStartPos = contentPos;
        
        // Limit by bottom
        if (_presentViews.Any() && _dataEndIdx == _adapter.GetItemCount() - 1) {
            viewportStartPos = Mathf.Max(_presentViews.Last().endPosition + _viewportHeight - margin, contentPos);
        } 
        
        // First View
        if (!_presentViews.Any() && _adapter.GetItemCount() != 0) {
            var idle = GetIdleViewHolder();
            BindViewHolder(idle, 0);
            idle.startPosition = -margin;
            _presentViews.Add(idle);
            //Debug.Log($"First View");
        }

        // Recycle Top
        while (_presentViews.Count > 1) {
            var firstView = _presentViews.First();
            if (firstView.endPosition - viewportStartPos > offsetWhenStartItemRecycle) {
                firstView.gameObject.SetActive(false);
                _presentViews.Remove(firstView);
                _cachedViews.Add(firstView);
                _dataStartIdx++;
                //Debug.Log($"Recycle Top {_dataStartIdx}");
            }
            else break;
        }

        // Load Top
        while (_presentViews.Any() &&
               _presentViews.First().startPosition - viewportStartPos < offsetWhenStartItemLoad &&
               _dataStartIdx > 0) {
            var idle = GetIdleViewHolder();
            BindViewHolder(idle, --_dataStartIdx);
            idle.endPosition = margin + _presentViews.First().startPosition;
            _presentViews.Insert(0, idle);
            //Debug.Log($"Load Top {_dataStartIdx}");
        }

        // Limit by top
        if (_presentViews.Any() && _dataStartIdx == 0) {
            contentPos = Mathf.Min(_presentViews.First().startPosition + margin, contentPos); 
        }
        
        var viewportEndPos = -_viewportHeight + contentPos; 
        
        // Recycle Bottom
        while (_presentViews.Count > 1) {
            var lastView = _presentViews.Last();
            if (lastView.startPosition - viewportEndPos < -offsetWhenEndItemRecycle) {
                lastView.gameObject.SetActive(false);
                _presentViews.Remove(lastView);
                _cachedViews.Add(lastView);
                //Debug.Log($"Recycle Bottom {_dataEndIdx}");
            }
            else break;
        }

        // Load Bottom
        while (_presentViews.Any() && _presentViews.Last().endPosition - viewportEndPos > -offsetWhenEndItemLoad &&
               _dataEndIdx < _adapter.GetItemCount() - 1) {
            var idle = GetIdleViewHolder();
            BindViewHolder(idle, _dataEndIdx + 1);
            idle.startPosition = _presentViews.Last().endPosition - margin;
            _presentViews.Add(idle);
            //Debug.Log($"Load Bottom {_dataEndIdx}");
        }

        // Limit by bottom only when top != 0
        if (_presentViews.Any() && _dataEndIdx == _adapter.GetItemCount() - 1) {
            contentPos = Mathf.Max(_presentViews.Last().endPosition + _viewportHeight - margin, contentPos);
        }

        // Limit when not scrollable
        if (_presentViews.Any() &&
            -_presentViews.Last().endPosition + _presentViews.First().startPosition + margin * 2 < _viewportHeight &&
            _dataStartIdx == 0 && _dataEndIdx == _adapter.GetItemCount() - 1) {
            contentPos = _presentViews.First().startPosition + margin;
        }
    }

    public int GetViewHolderIndex(object holder) {
        return _presentViews.IndexOf((VH)holder) + _dataStartIdx;
    }
}