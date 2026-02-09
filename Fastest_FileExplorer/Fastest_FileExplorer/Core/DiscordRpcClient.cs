using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace Fastest_FileExplorer.Core
{
    public class DiscordRpcClient : IDisposable
    {
        private readonly string _clientId;
        private NamedPipeClientStream _pipe;
        private bool _isConnected;
        private bool _isDisposed;
        private readonly object _lock = new object();
        private int _nonce;
        private DateTime _startTime;

        public bool IsConnected => _isConnected;
        public event EventHandler<string> OnError;
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;

        public DiscordRpcClient(string clientId)
        {
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _startTime = DateTime.UtcNow;
        }

        public bool Connect()
        {
            if (_isConnected) return true;

            for (int pipeNum = 0; pipeNum < 10; pipeNum++)
            {
                try
                {
                    var pipeName = $"discord-ipc-{pipeNum}";
                    _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    _pipe.Connect(1000);

                    if (_pipe.IsConnected)
                    {
                        _isConnected = true;
                        Handshake();
                        OnConnected?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                }
                catch (TimeoutException)
                {
                    _pipe?.Dispose();
                    continue;
                }
                catch (IOException)
                {
                    _pipe?.Dispose();
                    continue;
                }
                catch (Exception ex)
                {
                    _pipe?.Dispose();
                    OnError?.Invoke(this, ex.Message);
                    continue;
                }
            }

            return false;
        }

        private void Handshake()
        {
            var handshake = $"{{\"v\":1,\"client_id\":\"{_clientId}\"}}";
            SendPacket(0, handshake);
            ReadResponse();
        }

        public void SetPresence(DiscordPresence presence)
        {
            if (!_isConnected || _pipe == null) return;

            try
            {
                var pid = Process.GetCurrentProcess().Id;
                var nonceVal = Interlocked.Increment(ref _nonce);
                var startTimestamp = ((DateTimeOffset)_startTime).ToUnixTimeSeconds();

                var details = EscapeJson(presence.Details ?? "Browsing files");
                var state = EscapeJson(presence.State ?? "");
                var largeImage = EscapeJson(presence.LargeImageKey ?? "file_explorer");
                var largeText = EscapeJson(presence.LargeImageText ?? "Fastest File Explorer");
                var smallImage = EscapeJson(presence.SmallImageKey ?? "Folder");
                var smallText = EscapeJson(presence.SmallImageText ?? "Developed by PradaFit");

                // Build activity JSON with all fields
                var assetsJson = $"\"large_image\":\"{largeImage}\",\"large_text\":\"{largeText}\"";
                assetsJson += $",\"small_image\":\"{smallImage}\",\"small_text\":\"{smallText}\"";

                var activityJson = presence.ShowTimestamp
                    ? $"{{\"details\":\"{details}\",\"state\":\"{state}\",\"timestamps\":{{\"start\":{startTimestamp}}},\"assets\":{{{assetsJson}}}}}"
                    : $"{{\"details\":\"{details}\",\"state\":\"{state}\",\"assets\":{{{assetsJson}}}}}";

                var payload = $"{{\"cmd\":\"SET_ACTIVITY\",\"args\":{{\"pid\":{pid},\"activity\":{activityJson}}},\"nonce\":\"{nonceVal}\"}}";

                SendPacket(1, payload);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex.Message);
                Disconnect();
            }
        }

        public void ClearPresence()
        {
            if (!_isConnected || _pipe == null) return;

            try
            {
                var pid = Process.GetCurrentProcess().Id;
                var nonceVal = Interlocked.Increment(ref _nonce);
                var payload = $"{{\"cmd\":\"SET_ACTIVITY\",\"args\":{{\"pid\":{pid},\"activity\":null}},\"nonce\":\"{nonceVal}\"}}";

                SendPacket(1, payload);
            }
            catch { }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        private void SendPacket(int opcode, string json)
        {
            lock (_lock)
            {
                if (_pipe == null || !_pipe.IsConnected) return;

                var jsonBytes = Encoding.UTF8.GetBytes(json);
                var packet = new byte[8 + jsonBytes.Length];

                BitConverter.GetBytes(opcode).CopyTo(packet, 0);
                BitConverter.GetBytes(jsonBytes.Length).CopyTo(packet, 4);
                jsonBytes.CopyTo(packet, 8);

                _pipe.Write(packet, 0, packet.Length);
                _pipe.Flush();
            }
        }

        private string ReadResponse()
        {
            try
            {
                var header = new byte[8];
                var bytesRead = _pipe.Read(header, 0, 8);
                
                if (bytesRead < 8) return null;

                var length = BitConverter.ToInt32(header, 4);
                var data = new byte[length];
                _pipe.Read(data, 0, length);

                return Encoding.UTF8.GetString(data);
            }
            catch
            {
                return null;
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            try
            {
                ClearPresence();
                _pipe?.Close();
                _pipe?.Dispose();
            }
            catch { }
            finally
            {
                _isConnected = false;
                _pipe = null;
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            Disconnect();
        }
    }

    public class DiscordPresence
    {
        public string Details { get; set; }
        public string State { get; set; }
        public string LargeImageKey { get; set; }
        public string LargeImageText { get; set; }
        public string SmallImageKey { get; set; }
        public string SmallImageText { get; set; }
        public bool ShowTimestamp { get; set; } = true;
    }
}