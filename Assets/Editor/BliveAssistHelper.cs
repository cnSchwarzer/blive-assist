using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro.EditorUtilities;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = System.Random;

[EditorWindowTitle(title = "Blive Assist Helper")]
public class BliveAssistHelper : EditorWindow {
    private EditorWindow _gameView;
    private CanvasScaler[] _canvases;
    private FieldInfo _targetSizeField; 
    private float scale;

    [MenuItem("Window/Blive Assist Helper")]
    static void Init() {
        // Get existing open window or if none, make a new one:
        BliveAssistHelper window = (BliveAssistHelper) GetWindow(typeof(BliveAssistHelper));
        window.Show(); 
    }

    private void OnEnable() {
        var assembly = typeof(UnityEditor.EditorWindow).Assembly;
        var gameViewType = assembly.GetType("UnityEditor.GameView");
        var playModeViewType = assembly.GetType("UnityEditor.PlayModeView"); 
        _targetSizeField = playModeViewType.GetField("m_TargetSize", BindingFlags.NonPublic | BindingFlags.Instance); 
        _gameView = GetWindow(gameViewType);
        _canvases = SceneManager.GetActiveScene().GetRootGameObjects().Where((g) => g.name.EndsWith("Canvas"))
            .Select((g) => g.GetComponent<CanvasScaler>()).ToArray();
    }

    private void Update() {
        if (_gameView && _canvases != null && !EditorApplication.isPlaying) {
            var size = (Vector2) _targetSizeField.GetValue(_gameView);
            var newScale = size.x * 0.7f / 1920;
            if (newScale != scale) {
                scale = newScale;
                foreach (var canvas in _canvases) {
                    canvas.scaleFactor = scale;
                }

                Repaint();
            }
        }
    }

    private int testSuperchatId = 0;
    void OnGUI() { 
        GUILayout.Label("Working: " + scale, EditorStyles.boldLabel);
        if (GUILayout.Button("Send Test Superchat")) {
            var sc = new Superchat {
                Id = testSuperchatId,
                Time = DateTime.Now,
                BackgroundColor = "#C69D23",
                HeaderColor = "#D2C6A3",
                Content = "测试 " + (testSuperchatId++),
                Price = 100,
                UserId = 1,
                Username = "用户名",
                MedalName = "小黑梓",
                MedalLevel = 40,
                GuardLevel = 1,
                Face = "https://i0.hdslb.com/bfs/face/22665dcb72e36606444b15c4dd9f5f59eab995f4.jpg",
                FaceFrame = null
            };
            BliveDanmuManager.Instance.DispatchSuperchatEvent(sc);
        }
        if (GUILayout.Button("Send Test Superchat Delete")) {
            var del = new SuperchatDelete() {
                SuperchatIds = new List<long>() {testSuperchatId - 1, testSuperchatId - 3, testSuperchatId - 4}
            };
            BliveDanmuManager.Instance.DispatchSuperchatDeleteEvent(del);
        }
        if (GUILayout.Button("Send Test Guard Buy")) {
            var gift = new Gift {
                Time = DateTime.Now,
                Name = "总督",
                Currency = 19998000,
                Unit = $"gold",
                UserId = new Random().Next(),
                Username = "陈睿",
                MedalName = null,
                GuardLevel = 1,
                Combo = 1,
                IsGuardBuy = true
            };
            BliveDanmuManager.Instance.DispatchGiftEvent(gift);
        }
        
        if (GUILayout.Button("Screenshot")) {
            var size = (Vector2)_targetSizeField.GetValue(_gameView);
            ScreenCapture.CaptureScreenshot($@"D:\Work\BililiveAssist\Apple\AppStore\{size.x}x{size.y}_{Time.frameCount}.png");
        }
    } 
}