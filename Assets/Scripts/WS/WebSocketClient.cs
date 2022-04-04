using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// WebSocket Client using .NET / .NET Core System.Net.WebSockets.ClientWebSocket
/// </summary>
public class WebSocketClient {
    public WebSocketClient() {
        
    }

    public WebSocketClient(Action<ClientWebSocket> options) {
        this.options = options;
    }
    
    public Uri Uri { get; private set; }
    private ClientWebSocket _ws;
    private Action<ClientWebSocket> options;
    private readonly byte[] _rxBuf = new byte[4096];
    private CancellationTokenSource _wsCts = null;
    private bool _closing = false;
    
    public bool IsAlive => _ws is {State: WebSocketState.Open};

    public Action<WebSocketCloseStatus?, string> OnClose;
    public Action<Exception> OnException;
    public Action OnConnect;
    public Action<WebSocketMessageType, byte[]> OnReceive;
    public Action OnConnectionLost;

    public async ValueTask ConnectAsync(string uri) {
        await ConnectAsync(new Uri(uri)).ConfigureAwait(false);
    }

    public async ValueTask ConnectAsync(Uri uri) {
        Uri = uri;

        Debug.WriteLine("Websocket 连接");
        // 释放上一个Websocket
        try {
            await CloseAsync().ConfigureAwait(false);
        } catch (Exception ex) {
            Debug.WriteLine(ex);
        }

        try {
            _ws = new ClientWebSocket();
            options?.Invoke(_ws);
            _wsCts = new CancellationTokenSource();
            await _ws.ConnectAsync(Uri, _wsCts.Token).ConfigureAwait(false);

            OnConnect?.Invoke();
            BeginReceive();
        } catch (Exception ex) {
            OnException?.Invoke(ex);
            OnConnectionLost.Invoke();
        }
    }

    private void BeginReceive() {
#pragma warning disable CS4014
        Task.Run(ReceiveAsync, _wsCts.Token);
#pragma warning restore CS4014
    }

    private async Task ReceiveAsync() {
        var cancellationToken = _wsCts.Token;
        WebSocketReceiveResult result = null;
        try {
            var ms = new MemoryStream();

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                result = await _ws.ReceiveAsync(_rxBuf, cancellationToken)
                    .ConfigureAwait(false);

                if (result.Count > 0) {
                    ms.Write(_rxBuf, 0, result.Count);
                }

                if (result.EndOfMessage)
                    break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (result.MessageType != WebSocketMessageType.Close) {
                var bytes = ms.ToArray();
                if (bytes.Length > 0) {
                    OnReceive?.Invoke(result.MessageType, bytes);
                }
            } 
        } catch (TaskCanceledException x) {
            Debug.WriteLine(x);
        } catch (Exception ex) {
            OnException?.Invoke(ex);
        } finally {
            // 被取消则不触发事件
            if (!cancellationToken.IsCancellationRequested) {
                if (_ws.State == WebSocketState.Open) {
                    BeginReceive();
                } else {
                    if (!_closing)
                        OnConnectionLost.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="jsonData"></param>
    /// <param name="bytes"></param>
    /// <exception cref="Exception"></exception>
    public async Task SendAsync(byte[] bytes) {
        if (_ws == null || _wsCts == null) {
            return;
        }

        Debug.WriteLine($"Send {bytes.Length} bytes");
        
        if (!_wsCts.IsCancellationRequested &&
            _ws.State is not WebSocketState.Open) {
            OnConnectionLost.Invoke();
            return;
        }

        try {
            await _ws.SendAsync(bytes, WebSocketMessageType.Binary, true,
                    _wsCts.Token)
                .ConfigureAwait(false);
        } catch (TaskCanceledException x) {
            Debug.WriteLine(x);
        } catch (Exception ex) {
            OnException?.Invoke(ex);
        }
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="jsonData"></param>
    /// <param name="bytes"></param>
    /// <exception cref="Exception"></exception>
    public async Task SendAsync(string text) {
        if (_ws == null || _wsCts == null) {
            return;
        }

        if (!_wsCts.IsCancellationRequested &&
            _ws.State is not WebSocketState.Open) {
            OnConnectionLost.Invoke();
            return;
        }

        try {
            await _ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true,
                    _wsCts.Token)
                .ConfigureAwait(false);
        } catch (TaskCanceledException x) {
            Debug.WriteLine(x);
        } catch (Exception ex) {
            OnException?.Invoke(ex);
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public async ValueTask CloseAsync(WebSocketCloseStatus status = WebSocketCloseStatus.NormalClosure,
        string reason = null) {
        if (_ws is not null) {
            try {
                _closing = true;
                await _ws.CloseAsync(status, reason ?? "", _wsCts.Token).ConfigureAwait(false);
                _wsCts.Cancel();
                _wsCts.Dispose();
                _ws.Abort();
            } catch (Exception ex) {
                Debug.WriteLine(ex);
            }

            _wsCts = null;
            _ws = null; 
            
            OnClose.Invoke(status, reason);
            _closing = false;
        }
    }
}