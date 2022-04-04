using System;
using System.Collections;
using System.Collections.Generic;
using Battlehub.UIControls.Dialogs;
using Battlehub.UIControls.DockPanels;
using Battlehub.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public interface IDashboardListener {
    void Clear();
    void LoadHistory();
}

public class DashboardLayout : MonoBehaviour {
    public static DashboardLayout Instance { get; private set; }

    private void OnEnable() {
        Instance = this;
    }

    public DockPanel dockPanel;
    public DialogManager dialogManager;
    public GameObject danmuRingPrefab, danmuRollPrefab, superchatPrefab, freeGiftPrefab, paidGiftPrefab;

    public List<IDashboardListener> Listeners { get; } = new List<IDashboardListener>();

    private void Start() {
        LoadLayout();
    }

    private void OnApplicationQuit() {
        SaveLayout();
    }

    public void LoadHistory() {
        foreach (var lis in Listeners) {
            lis.LoadHistory();
        }
    }

    public void Clear() {
        foreach (var lis in Listeners) {
            lis.Clear();
        }
    }
    
    public void ResetLayout() {
        var root = new LayoutInfo(false, new LayoutInfo(false,
                new LayoutInfo(Instantiate(danmuRingPrefab).transform, "弹幕(列表)"),
                new LayoutInfo(Instantiate(danmuRollPrefab).transform, "弹幕(滚动)"), 0.7f)
            , new LayoutInfo(true, new LayoutInfo(Instantiate(superchatPrefab).transform, "Superchat"),
                new LayoutInfo(true, new LayoutInfo(Instantiate(paidGiftPrefab).transform, "付费礼物"),
                    new LayoutInfo(Instantiate(freeGiftPrefab).transform, "免费礼物"))), 0.75f);
        dockPanel.RootRegion.Build(root);
        SaveLayout();
    }

    public void LoadLayout() {
        var root = GetLayout();
        if (root == null) {
            ResetLayout();
        } else {
            dockPanel.RootRegion.Build(root);
        }
    }

    public void SaveLayout() {
        var layoutInfo = new PersistentLayoutInfo();
        ToPersistentLayout(dockPanel.RootRegion, layoutInfo);

        var serializedLayout = XmlUtility.ToXml(layoutInfo);
        PlayerPrefs.SetString("BliveLayout", serializedLayout);
        PlayerPrefs.Save();
    }

    public void OpenWindow(string n) {
        dockPanel.RootRegion.Add(null, n, Instantiate(GetTabPrefab(n)).transform, true);
    }
    
    private void ToPersistentLayout(Region region, PersistentLayoutInfo layoutInfo) {
        if (region.HasChildren) {
            Region childRegion0 = region.GetChild(0);
            Region childRegion1 = region.GetChild(1);

            RectTransform rt0 = (RectTransform) childRegion0.transform;
            RectTransform rt1 = (RectTransform) childRegion1.transform;

            Vector3 delta = rt0.localPosition - rt1.localPosition;
            layoutInfo.IsVertical = Mathf.Abs(delta.x) < Mathf.Abs(delta.y);

            if (layoutInfo.IsVertical) {
                float y0 = Mathf.Max(0.000000001f, rt0.sizeDelta.y - childRegion0.MinHeight);
                float y1 = Mathf.Max(0.000000001f, rt1.sizeDelta.y - childRegion1.MinHeight);

                layoutInfo.Ratio = y0 / (y0 + y1);
            } else {
                float x0 = Mathf.Max(0.000000001f, rt0.sizeDelta.x - childRegion0.MinWidth);
                float x1 = Mathf.Max(0.000000001f, rt1.sizeDelta.x - childRegion1.MinWidth);

                layoutInfo.Ratio = x0 / (x0 + x1);
            }

            layoutInfo.Child0 = new PersistentLayoutInfo();
            layoutInfo.Child1 = new PersistentLayoutInfo();

            ToPersistentLayout(childRegion0, layoutInfo.Child0);
            ToPersistentLayout(childRegion1, layoutInfo.Child1);
        } else {
            if (region.ContentPanel.childCount > 1) {
                layoutInfo.TabGroup = new PersistentLayoutInfo[region.ContentPanel.childCount];
                for (int i = 0; i < region.ContentPanel.childCount; ++i) {
                    Transform content = region.ContentPanel.GetChild(i);

                    PersistentLayoutInfo tabLayout = new PersistentLayoutInfo();
                    ToPersistentLayout(region, content, tabLayout);
                    layoutInfo.TabGroup[i] = tabLayout;
                }
            } else if (region.ContentPanel.childCount == 1) {
                Transform content = region.ContentPanel.GetChild(0);
                ToPersistentLayout(region, content, layoutInfo);
            }
        }
    }

    private void ToPersistentLayout(Region region, Transform content, PersistentLayoutInfo layoutInfo) {
        Tab tab = Region.FindTab(content);
        if (tab != null) {
            layoutInfo.WindowType = tab.Text;
            layoutInfo.CanDrag = tab.CanDrag;
            layoutInfo.CanClose = tab.CanClose;
        }
        layoutInfo.IsHeaderVisible = region.IsHeaderVisible;
    }

    private LayoutInfo GetLayout() {
        string serializedLayout = PlayerPrefs.GetString("BliveLayout");
        if (serializedLayout == null) {
            return null;
        }

        try {
            PersistentLayoutInfo persistentLayoutInfo = XmlUtility.FromXml<PersistentLayoutInfo>(serializedLayout);
            LayoutInfo layoutInfo = new LayoutInfo();
            ToLayout(persistentLayoutInfo, layoutInfo);
            return layoutInfo;
        } catch (Exception ex) {
            Debug.LogWarning("Restore layout failed: " + ex.Message);
            return null;
        }
    }

    private GameObject GetTabPrefab(string windowType) {
        return windowType switch {
            "弹幕(列表)" => danmuRingPrefab,
            "弹幕(滚动)" => danmuRollPrefab,
            "Superchat" => superchatPrefab,
            "付费礼物" => paidGiftPrefab,
            "免费礼物" => freeGiftPrefab,
            _ => null
        };
    }

    private void ToLayout(PersistentLayoutInfo persistentLayoutInfo, LayoutInfo layoutInfo) {
        if (!string.IsNullOrEmpty(persistentLayoutInfo.WindowType)) {
            var content = Instantiate(GetTabPrefab(persistentLayoutInfo.WindowType)).transform; 
            var tab = Instantiate(dockPanel.TabPrefab);
            tab.Text = persistentLayoutInfo.WindowType;
            tab.Icon = null;

            layoutInfo.Tab = tab;
            layoutInfo.Content = content.transform;
            layoutInfo.CanDrag = persistentLayoutInfo.CanDrag;
            layoutInfo.CanClose = persistentLayoutInfo.CanClose;
            layoutInfo.CanMaximize = persistentLayoutInfo.CanMaximize;
            layoutInfo.IsHeaderVisible = persistentLayoutInfo.IsHeaderVisible;
        } else {
            if (persistentLayoutInfo.TabGroup != null) {
                layoutInfo.TabGroup = new LayoutInfo[persistentLayoutInfo.TabGroup.Length];
                for (int i = 0; i < persistentLayoutInfo.TabGroup.Length; ++i) {
                    LayoutInfo tabLayoutInfo = new LayoutInfo();
                    ToLayout(persistentLayoutInfo.TabGroup[i], tabLayoutInfo);
                    layoutInfo.TabGroup[i] = tabLayoutInfo;
                }
            } else {
                layoutInfo.IsVertical = persistentLayoutInfo.IsVertical;
                layoutInfo.Child0 = new LayoutInfo();
                layoutInfo.Child1 = new LayoutInfo();
                layoutInfo.Ratio = persistentLayoutInfo.Ratio;

                ToLayout(persistentLayoutInfo.Child0, layoutInfo.Child0);
                ToLayout(persistentLayoutInfo.Child1, layoutInfo.Child1);
            }
        }
    }
}