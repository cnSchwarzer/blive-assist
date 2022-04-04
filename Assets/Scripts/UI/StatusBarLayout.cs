using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class StatusBarLayout : MonoBehaviour {

    public static StatusBarLayout Instance { get; private set; }

    public CanvasGroup windowList;
    
    private void OnEnable() {
        Instance = this;
    } 

    private const float speed = 0.3f;
    
    public void Hide(CanvasGroup g) { 
        g.DOFade(0, speed).OnComplete(() => g.gameObject.SetActive(false));
    }

    public void Show(CanvasGroup g) {
        if (!g.gameObject.activeSelf) {
            g.alpha = 0;
            g.gameObject.SetActive(true);
            g.DOFade(1, speed);
        } else Hide(g);
    }

    public void ToggleOpenWindowList() {
        if (windowList.gameObject.activeSelf) {
            windowList.alpha = 1;
            windowList.DOFade(0, 0.25f).OnComplete(() => windowList.gameObject.SetActive(false));
        } else {
            windowList.gameObject.SetActive(true);
            windowList.alpha = 0;
            windowList.DOFade(1, 0.25f);
        }
    }
    
    public void OpenWindow(string n) {
        ToggleOpenWindowList();
        DashboardLayout.Instance.OpenWindow(n);
        Toast.Instance.ShowToast("可以拖动左上角标签吸附到其他窗口");
    }
}
