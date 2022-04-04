using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Newtonsoft.Json;
using SQLite4Unity3d;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MainManager : MonoBehaviour {
    public Version AppVersion => Version.Parse(Application.version);

    public static MainManager Instance { get; private set; }

    private void OnEnable() {
        Instance = this; 
        DOTween.Init();
        SettingManager.Settings.SystemLayoutScaleRatio += (v, b) => {
            SetCanvasRatio();
        };
        timeText.text = DateTime.Now.ToString("HH:mm:ss");
        roomInput.text = SettingManager.Settings.RoomId.ToString();

        if (Application.platform == RuntimePlatform.Android) {
            if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
    }

    public BliveDanmuManager danmu;
    public DashboardLayout dashboard;
    public int room;
    public RectTransform mainRoot;
    
    public CanvasScaler[] canvases;

    public Text timeText, heatText, followText, roomText;

    public InputField roomInput;
    public RectTransform roomInputPanel;
    private const float RoomInputAnimDuration = 0.3f;

    private ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();

    public Text messageBoxText;
    public CanvasGroup messageBoxCanvasGroup;

    public Text versionText;

    public float zoomRatio = 1;

    public Button switchFullscreenButton;
    public Button switchDisplayButton;
    public GameObject screenSetting;
    public GameObject updateButton;

    public CanvasGroup helpCanvasGroup;

    private Vector2 _lastScreenSize;
    private DeviceOrientation _lastOrientation;

    private void Start() {
        versionText.text = AppVersion.ToString();
        StartCoroutine(CheckUpdate());
        if (Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.OSXPlayer) {
            switchFullscreenButton.gameObject.SetActive(false);
            switchDisplayButton.gameObject.SetActive(false);
            screenSetting.SetActive(false);
        }
        SettingManager.Settings.SystemFramerate += (i, b) => {
            var frameRate = i switch {
                0 => 15,
                1 => 30,
                2 => 60,
                3 => 120,
                _ => 60
            };
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = frameRate;
        };
        _lastScreenSize = new Vector2(Screen.width, Screen.height);
    }

    public void EnterRoom(InputField roomField) {
        if (!int.TryParse(roomField.text, out room))
            return;
        SettingManager.Settings.RoomId.Value = roomField.text;
        roomText.text = room.ToString();
        heatText.text = "热度";
        followText.text = "关注";

        roomInputPanel.DOAnchorPosY(2000, RoomInputAnimDuration);
        roomInputPanel.GetComponent<Image>().DOFade(0, RoomInputAnimDuration).OnComplete(() => {
            DatabaseManager.Instance.InitRoom();
            danmu.Connect(room);
            dashboard.LoadHistory();
            roomInputPanel.gameObject.SetActive(false);
        });
 
        if (!SettingManager.Tutorials.FirstShowHelp) {
            SettingManager.Tutorials.FirstShowHelp.Value = true;
            StatusBarLayout.Instance.Show(helpCanvasGroup);
            Toast.Instance.ShowToast("欢迎使用，请先阅读帮助~");
        }
    }

    public void ExitRoom() {
        danmu.Disconnect();
        DatabaseManager.Instance.ExitRoom();
        dashboard.Clear();

        PlayerPrefs.Save();

        roomInputPanel.gameObject.SetActive(true);
        roomInputPanel.DOAnchorPosY(0, RoomInputAnimDuration);
        roomInputPanel.GetComponent<Image>().DOFade(1, RoomInputAnimDuration);
    }

    public void Quit() {
        Application.Quit();
    }

    private void Update() {
        var screenSize = new Vector2(Screen.width, Screen.height);
        var orientation = Input.deviceOrientation;
        if (_lastScreenSize != screenSize || _lastOrientation != orientation) {
            SetCanvasRatio();
            _lastScreenSize = screenSize;
            _lastOrientation = orientation;
        }
        
        timeText.text = DateTime.Now.ToString("HH:mm:ss");
        while (actions.TryDequeue(out Action action)) {
            action();
        }
    }

    public void SetCanvasRatio() {
        var ratio = SettingManager.Settings.SystemLayoutScaleRatio.Value; 
        var factor = Screen.width * 0.7f / 1920;
        zoomRatio = ratio;
        foreach (var canvas in canvases) {
            canvas.scaleFactor = factor * ratio;
        } 
        mainRoot.SetLeft(Screen.safeArea.xMin);
        mainRoot.SetRight(Screen.width - Screen.safeArea.xMax); 
    }

    public void RunInMainThread(Action action) {
        actions.Enqueue(action);
    }

    public void OpenUrl(string url) {
        Application.OpenURL(url);
    }

    public void SwitchFullscreen() {
        var resolution = Screen.currentResolution;
        if (Screen.fullScreen) {
            Screen.SetResolution(Mathf.RoundToInt(resolution.width * 0.7f), Mathf.RoundToInt(resolution.height * 0.7f),
                FullScreenMode.Windowed);
        } else {
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
        }
        SetCanvasRatio();
    }

    public void SwitchDisplay() {
        StartCoroutine(SwitchDisplayCoroutine());
        switchDisplayButton.interactable = false;
    }

    private IEnumerator SwitchDisplayCoroutine() {
        var displays = new List<DisplayInfo>();
        Screen.GetDisplayLayout(displays);
        var idx = PlayerPrefs.GetInt("UnitySelectMonitor", 0) + 1;
        idx %= displays.Count;
        PlayerPrefs.SetInt("UnitySelectMonitor", idx);

        var (oldWidth, oldHeight) = (Screen.width, Screen.height);
        yield return Screen.MoveMainWindowTo(displays[idx], Vector2Int.zero);

        var resolutions = Screen.resolutions;
        var resolution = resolutions[^1];
        if (!Screen.fullScreen) {
            Screen.SetResolution(oldWidth, oldHeight, Screen.fullScreenMode);
        } else {
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
        }
        switchDisplayButton.interactable = true;
        SetCanvasRatio();
    }

    private IEnumerator CheckUpdate() {
        using var req = UnityWebRequest.Get("https://api.schwarzer.wang/blive/version");
        yield return req.SendWebRequest();
        try {
            if (req.result == UnityWebRequest.Result.Success) {
                var t = req.downloadHandler.text;
                var j = JsonConvert.DeserializeObject<BliveVersion>(t);
                var v = Version.Parse(j.version);
                if (v > AppVersion || !string.IsNullOrWhiteSpace(j.message)) {
                    var str = "<size=50>通知</size>\n";
                    if (v > AppVersion) {
                        str += $"有<color=red>新版本</color>（{j.version}），请\n";
                        updateButton.SetActive(true);
                    }
                    if (!string.IsNullOrWhiteSpace(j.message)) {
                        str += $"{j.message}";
                    }

                    messageBoxText.text = str;
                    if (SettingManager.Settings.SystemLastCloudMessage != j.message || v > AppVersion) {
                        StatusBarLayout.Instance.Show(messageBoxCanvasGroup);
                        SettingManager.Settings.SystemLastCloudMessage.Value = j.message;
                    }
                }
            }
        } catch (Exception ex) {
            Toast.Instance.ShowToast("检查更新失败");
            Toast.Instance.ShowToast(ex.ToString());
        }
    }
}

public class BliveVersion {
    public string version;
    public string message;
}