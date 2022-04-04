using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonGroupLayout : MonoBehaviour {
    private List<GameObject> _selectionHint = new();

    public Action<int> OnSelectChanged;

    private bool _initialized;
    
    private void Initialize() {
        if (_initialized)
            return;

        for (var i = 0; i < transform.childCount; ++i) {
            var child = transform.GetChild(i);
            var btn = child.GetComponent<Button>();
            if (btn == null) continue;
            var idx = i;
            var sel = child.Find("Selected");
            if (sel == null) continue;
            btn.onClick.AddListener(() => {
                SetSelectedAndDispatch(idx);
            });
            _selectionHint.Add(sel.gameObject);
        }

        _initialized = true;
        Selected = 0;
    }

    private int _selected;
    public int Selected {
        get => _selected;
        set {
            Initialize();
            value = Mathf.Clamp(value, 0, _selectionHint.Count - 1);
            for (var i = 0; i < _selectionHint.Count; ++i) {
                _selectionHint[i].SetActive(i == value);
            }
            _selected = value;
        }
    }

    public void SetSelectedAndDispatch(int idx) {
        Initialize();
        Selected = idx;
        OnSelectChanged?.Invoke(_selected);
    }
    
    public void SetSelected(int idx) {
        Initialize();
        Selected = idx; 
    }
}