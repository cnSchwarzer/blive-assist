using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers; 
using DG.Tweening;
using Extension;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage; 
using HttpStatusCode = System.Net.HttpStatusCode;
 
public class SuperchatDelete {
    [JsonProperty("ids")]
    public List<long> SuperchatIds { get; set; } 
}

public delegate void DanmuCallback(Danmu danmu);

public delegate void SuperchatCallback(Superchat sc);

public delegate void GiftCallback(Gift gift);
public delegate void SuperchatDeleteCallback(SuperchatDelete del);

public class BliveDanmuManager : MonoBehaviour {
    public static BliveDanmuManager Instance { get; private set; }

    public BliveDanmuManager() {
        Instance = this;
    }

    public event DanmuCallback DanmuEvent;
    public event SuperchatCallback SuperchatEvent;
    public event GiftCallback GiftEvent;
    public event SuperchatDeleteCallback SuperchatDeleteEvent;

    private float _dispatchDelay => (50 - Mathf.Clamp(_danmus.Count, 0, 50)) / 1000f;
    private float _dispatchDelayCounter;

    private readonly ConcurrentQueue<Danmu> _danmus = new ConcurrentQueue<Danmu>();
    private readonly ConcurrentQueue<Superchat> _superchats = new ConcurrentQueue<Superchat>();
    private readonly ConcurrentQueue<Gift> _gifts = new ConcurrentQueue<Gift>();
    private readonly ConcurrentQueue<SuperchatDelete> _scDeletes = new ConcurrentQueue<SuperchatDelete>();

    private void Start() {  
        _heartbeat.Elapsed += (sender, e) => HeartbeatHandler();
        _heartbeat.AutoReset = true;
        _heartbeat.Start();
    }

    public Color pausedColor;
    public ProceduralImage pauseButton;
    private bool _pause = false;
    public void TogglePause() {
        _pause = !_pause;
        pauseButton.color = _pause ? pausedColor : Color.white;
        Toast.Instance.ShowToast(_pause ? "已暂停消息加载" : "恢复消息加载");
    }

    public void DispatchDanmuEvent(Danmu danmu) { 
        DanmuEvent?.Invoke(danmu); 
    }

    public void DispatchSuperchatEvent(Superchat sc) {
        SuperchatEvent?.Invoke(sc);
    }

    public void DispatchGiftEvent(Gift gift) {
        GiftEvent?.Invoke(gift);
    }
    
    public void DispatchSuperchatDeleteEvent(SuperchatDelete gift) {
        SuperchatDeleteEvent?.Invoke(gift);
    }


    private void Update() {
        if (!_pause) { 
            _dispatchDelayCounter += Time.deltaTime;
            if (_dispatchDelayCounter > _dispatchDelay) {
                if (_danmus.TryDequeue(out var danmu)) {
                    DispatchDanmuEvent(danmu);
                    _dispatchDelayCounter = 0;
                }
            }

            if (_superchats.TryDequeue(out var sc)) {
                DispatchSuperchatEvent(sc);
            }

            if (_gifts.TryDequeue(out var gift)) {
                DispatchGiftEvent(gift);
            }
            
            if (_scDeletes.TryDequeue(out var del)) {
                DispatchSuperchatDeleteEvent(del);
            }
        }
    }

    private WebSocketClient _ws;
    private readonly Timer _heartbeat = new (30000);
    private int _room = 510;
    private int _uid;

    private async void HeartbeatHandler() {
        if (_ws is {IsAlive: true}) {
            await _ws.SendAsync(BliveUtility.EncodeHeartbeat()); 
            MainManager.Instance.RunInMainThread(() => {
                StartCoroutine(QueryFollowerCoroutine());
            });
        }
    }

    private IEnumerator QueryFollowerCoroutine() {
        Debug.Log($"查关注: {_uid}");
        if (_uid == 0)
            yield break;
        using var web = UnityWebRequest.Get($"https://api.bilibili.com/x/relation/stat?vmid={_uid}");
        yield return web.SendWebRequest();
        if (web.result != UnityWebRequest.Result.Success)
            yield break;

        var j = JObject.Parse(web.downloadHandler.text);
        MainManager.Instance.followText.text = j["data"]["follower"].Value<int>().ToString();
    }

    private async void WsResponseHandler(WebSocketMessageType _, byte[] msg) {
        try {
            foreach ((string str, BliveOp op) in BliveUtility.Decode(msg)) {
                try {
                    switch (op) {
                        case BliveOp.ConnectSucceed:
                            Toast.Instance.ShowToast("弹幕服务器连接成功");
                            await _ws.SendAsync(BliveUtility.EncodeHeartbeat());
                            break;
                        case BliveOp.HeartbeatReply:
                            Debug.Log($"Heat: {str}");
                            MainManager.Instance.RunInMainThread(() => { MainManager.Instance.heatText.text = str; });
                            break;
                        case BliveOp.Message: {
                            var m = JObject.Parse(str);
                            var cmd = m["cmd"].Value<string>();
                            try {
                                switch (cmd) {
                                    case "DANMU_MSG": {
                                        var info = m["info"];
                                        // Extract from app.js
                                        var danmu = new Danmu {
                                            Time = DateTime.Now,
                                            MedalName = !info[3].Any() ? null : info[3][1].Value<string>(),
                                            MedalLevel = !info[3].Any() ? 0 : info[3][0].Value<int>(),
                                            UserId = info[2][0].Value<int>(),
                                            Username = info[2][1].Value<string>(),
                                            Content = info[1].Value<string>(),
                                            GuardLevel = info[7].Value<int>()
                                        };
                                        //DatabaseManager.Instance.AddDanmu(danmu);
                                        _danmus.Enqueue(danmu);
                                        break;
                                    }
                                    case "SUPER_CHAT_MESSAGE":
                                    case "SUPER_CHAT_MESSAGE_JPN":  {
                                        var data = m["data"];

                                        var sc = new Superchat {
                                            Time = DateTime.Now,
                                            SuperchatId = data["id"].Value<int>(),
                                            BackgroundColor = data["background_bottom_color"].Value<string>(),
                                            HeaderColor = data["background_color"].Value<string>(),
                                            Content = data["message"].Value<string>(),
                                            Price = data["price"].Value<int>(),
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["user_info"]["uname"].Value<string>(),
                                            MedalName = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["user_info"].HasValues
                                                ? data["user_info"]["guard_level"].Value<int>()
                                                : 0,
                                            Face = data["user_info"]["face"].Value<string>(),
                                            FaceFrame = data["user_info"]["face_frame"].Value<string>(),
                                            Thanked = false
                                        };
                                        DatabaseManager.Instance.AddSuperchat(sc);
                                        _superchats.Enqueue(sc);
                                        break;
                                    }
                                    case "SEND_GIFT": {
                                        var data = m["data"];

                                        var combo = data["super_batch_gift_num"].Value<int>();
                                        if (combo == 0 ||
                                            string.IsNullOrWhiteSpace(data["batch_combo_id"].Value<string>()))
                                            combo = data["num"].Value<int>();

                                        var gift = new Gift {
                                            Time = DateTime.Now,
                                            Action = data["action"].Value<string>(),
                                            Name = data["giftName"].Value<string>(),
                                            Currency = data["discount_price"].Value<float>(),
                                            Unit = data["coin_type"].Value<string>(),
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["uname"].Value<string>(),
                                            MedalName = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["guard_level"].Value<int>()
                                                : 0,
                                            ComboId = data["batch_combo_id"].Value<string>(),
                                            Combo = combo
                                        }; 
                                        DatabaseManager.Instance.AddGift(gift);
                                        _gifts.Enqueue(gift);
                                        break;
                                    }
                                    case "COMBO_SEND": {
                                        var data = m["data"]; 

                                        var gift = new Gift {
                                            IsComboSend = true,
                                            Time = DateTime.Now,
                                            Action = data["action"].Value<string>(),
                                            Name = data["gift_name"].Value<string>(), 
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["uname"].Value<string>(),
                                            MedalName = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["medal_info"].HasValues
                                                ? data["medal_info"]["guard_level"].Value<int>()
                                                : 0,
                                            ComboId = data["batch_combo_id"].Value<string>(),
                                            Combo = data["batch_combo_num"].Value<int>()
                                        }; 
                                        DatabaseManager.Instance.AddGift(gift);
                                        _gifts.Enqueue(gift);
                                        break;
                                    }
                                    case "USER_TOAST_MSG": {
                                        var data = m["data"];
                                        var price = data["price"].Value<float>();
                                        var gift = new Gift {
                                            Time = DateTime.Now,
                                            Name = data["role_name"].Value<string>(),
                                            Currency = price,
                                            Unit = $"gold",
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["username"].Value<string>(),
                                            MedalName = null,
                                            GuardLevel = data["guard_level"].Value<int>(),
                                            Combo = 1,
                                            IsGuardBuy = true
                                        };
                                        DatabaseManager.Instance.AddGift(gift);
                                        _gifts.Enqueue(gift);
                                        break;
                                    }
                                    case "INTERACT_WORD": {
                                        var data = m["data"];
                                        var gift = new Gift {
                                            Time = DateTime.Now,
                                            Name = "进入直播间",
                                            Currency = 0,
                                            Unit = $"silver",
                                            UserId = data["uid"].Value<int>(),
                                            Username = data["uname"].Value<string>(),
                                            MedalName = data["fans_medal"].HasValues
                                                ? data["fans_medal"]["medal_name"].Value<string>()
                                                : null,
                                            MedalLevel = data["fans_medal"].HasValues
                                                ? data["fans_medal"]["medal_level"].Value<int>()
                                                : 0,
                                            GuardLevel = data["fans_medal"].HasValues
                                                ? data["fans_medal"]["guard_level"].Value<int>()
                                                : 0,
                                            Combo = 1,
                                            IsJoinRoom = true
                                        };
                                        DatabaseManager.Instance.AddGift(gift);
                                        _gifts.Enqueue(gift);
                                        break;
                                    } 
                                    case "SUPER_CHAT_MESSAGE_DELETE": {
                                        var data = m["data"];
                                        var delete = data.ToObject<SuperchatDelete>();
                                        _scDeletes.Enqueue(delete);
                                        break;
                                    }
                                }
                            } catch (Exception ex) { 
                                Toast.Instance.ShowToast($"解析事件出错：{cmd}");
                                Debug.LogException(ex);
                                Debug.LogError(m);
                            } 
                            break;
                        }
                    }
                } catch (Exception ex) {
                    Debug.LogException(ex);
                    Debug.Log(op);
                    Debug.Log(str);
                }
            }
        } catch (Exception ex) {
            Debug.LogWarning(ex);
        }
    } 

    private void WsErrorHandler(Exception ex) { 
        Debug.LogError("与弹幕服务器连接出错");
        Debug.LogException(ex);
    }

    private void WsCloseHandler(WebSocketCloseStatus? status, string reason) {
        Debug.LogError($"与弹幕服务器连接关闭");
        Debug.LogWarning($"{status} {reason}");
    }

    private bool _reconnecting;
    private void WsReconnect() { 
        Debug.LogWarning($"弹幕服务器重连");
        MainManager.Instance.RunInMainThread(() => {
            if (_reconnecting)
                return;
            _reconnecting = true;
            StartCoroutine(WsReconnectCoroutine());
        });
    }

    private IEnumerator WsReconnectCoroutine() {
        yield return new WaitForSeconds(2);
        Connect(_room);
        _reconnecting = false;
    }

    private bool _connecting = false;
    public void Connect(int roomId) {
        if (_connecting) 
            return;
        Debug.Log($"连接弹幕服务器 {roomId}");
        _room = roomId;
        _connecting = true;
        Disconnect(); 
        StartCoroutine(ConnectAsync(roomId));
    } 

    private IEnumerator ConnectAsync(int roomId) {
        var req =
            UnityWebRequest.Get($"https://api.live.bilibili.com/room/v1/Room/get_info_by_id?ids[]={roomId}");
        req.SetRequestHeader("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36");
        req.SetRequestHeader("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        req.timeout = 3; 
        yield return req.SendWebRequest();

        if (req.responseCode != 200) {
            Debug.LogWarning($"获取房间信息错误 ({req.responseCode})");
            WsReconnect();
            req.Dispose();
            _connecting = false;
            yield break;
        }

        var roomInfoStr = req.downloadHandler.text;
        var realRoomId = 0;
        try {
            var roomInfo = JObject.Parse(roomInfoStr);
            var first = ((JObject) roomInfo["data"]).Properties().First();
            _uid = int.Parse(first.Value["uid"].Value<string>());
            StartCoroutine(QueryFollowerCoroutine());
            realRoomId = int.Parse(first.Name);
            Debug.Log($"房间号：{realRoomId}");
        } catch (Exception ex) { 
            Debug.LogException(ex);
            Debug.LogWarning($"解析房间信息异常：{ex}");
            WsReconnect();
            _connecting = false;
            yield break;
        } finally {
            req.Dispose();
        } 

        req = UnityWebRequest.Get(
            $"https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={realRoomId}");
        req.SetRequestHeader("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/93.0.4577.63 Safari/537.36");
        req.SetRequestHeader("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        req.timeout = 3; 
        yield return req.SendWebRequest();

        if (req.responseCode != 200) {
            Debug.LogWarning($"获取弹幕信息错误 ({req.responseCode})");
            req.Dispose();
            WsReconnect();
            _connecting = false;
            yield break;
        }

        Debug.Log(req.downloadHandler.text);

        string wssUrl;
        string token;
        try {
            var danmuInfo = JObject.Parse(req.downloadHandler.text);
            token = danmuInfo["data"]["token"].Value<string>();
            var danmuServerList = danmuInfo["data"]["host_list"].ToList();
            var danmuServer = danmuServerList[new System.Random().Next(0, danmuServerList.Count)];
            wssUrl = $"wss://{danmuServer["host"].Value<string>()}:{danmuServer["wss_port"].Value<int>()}/sub";
            Debug.Log($"地址：{wssUrl}");

        } catch (Exception ex) { 
            Debug.LogException(ex);
            Debug.LogWarning($"解析弹幕信息异常：{ex}"); 
            WsReconnect();
            _connecting = false;
            yield break;
        } finally {
            req.Dispose();
        }
        
        _ws = new WebSocketClient();
        _ws.OnReceive += WsResponseHandler;
        _ws.OnException += WsErrorHandler;
        _ws.OnClose += WsCloseHandler;
        _ws.OnConnectionLost += WsReconnect; 
        var task = _ws.ConnectAsync(wssUrl).AsCoroutine();
        yield return task;
        if (task.Error) { 
            Toast.Instance.ShowToast($"连接WS异常：{task.Task.Exception}"); 
            WsReconnect();
            _connecting = false;
            yield break;
        }
        task = _ws.SendAsync(BliveUtility.EncodeUserAuthentication(realRoomId, token)).AsCoroutine();
        yield return task;
        if (task.Error) { 
            Toast.Instance.ShowToast($"连接WS异常：{task.Task.Exception}"); 
            WsReconnect();
            _connecting = false;
            yield break;
        }
        Debug.Log($"连接弹幕服务器成功");
        _connecting = false;
    }

    public void Disconnect() {
        if (_ws != null) {
            try {
                _ws.CloseAsync().AsTask().Wait(TimeSpan.FromSeconds(1));
            } catch (Exception ex) {
                Debug.LogException(ex);
            }
            _ws = null;
        } 
    }

    private void OnApplicationQuit() {
        Disconnect();
        _heartbeat.Stop();
        _heartbeat.Close();
        _heartbeat.Dispose();
    } 
}