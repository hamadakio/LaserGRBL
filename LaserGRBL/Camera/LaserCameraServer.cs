using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QRCoder;

namespace LaserGRBL.Camera
{
    public static class LaserCameraServer
    {
        private static TcpListener mTcpListener;
        private static Thread mServerThread;
        private static volatile bool mIsRunning = false;
        private static int mPort = 8088;
        private static Timer mExternalPollTimer;
        private static string mExternalAppUrl = "";

        private static DateTime mLastClientActivity = DateTime.MinValue;
        private static string mLastClientIp = "";
        private static int mFramesCount = 0;
        private static readonly object mLogLock = new object();
        private static readonly List<string> mLogHistory = new List<string>();

        public static event Action<string> OnStatusChanged;
        public static event Action OnFrameReceived;
        public static event Action<string> OnLogMessage;

        public static bool IsRunning => mIsRunning;
        public static int Port => mPort;
        public static int FramesReceivedCount => mFramesCount;
        public static string ConnectedClientIP => mLastClientIp;
        public static bool IsClientConnected => (DateTime.UtcNow - mLastClientActivity).TotalSeconds < 8;
        public static string ServerUrl => GetLocalUrl();

        public static string ExternalAppUrl
        {
            get => mExternalAppUrl;
            set
            {
                mExternalAppUrl = value;
                ConfigureExternalPolling();
            }
        }

        public static List<string> GetLogs()
        {
            lock (mLogLock)
            {
                return new List<string>(mLogHistory);
            }
        }

        public static void ClearLogs()
        {
            lock (mLogLock)
            {
                mLogHistory.Clear();
            }
        }

        public static void Log(string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            lock (mLogLock)
            {
                mLogHistory.Add(entry);
                if (mLogHistory.Count > 300)
                    mLogHistory.RemoveAt(0);
            }
            try
            {
                OnLogMessage?.Invoke(entry);
            }
            catch { }
        }

        public static string GetLocalUrl()
        {
            string ip = GetLocalIPAddress();
            return $"http://{ip}:{mPort}/";
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    if (endPoint != null) return endPoint.Address.ToString();
                }
            }
            catch { }

            // Fallback
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                        return ip.ToString();
                }
            }
            catch { }

            return "127.0.0.1";
        }

        public static bool Start(int port = 8088)
        {
            if (mIsRunning) return true;

            mPort = port;
            try
            {
                mTcpListener = new TcpListener(IPAddress.Any, mPort);
                mTcpListener.Start();
            }
            catch (Exception ex)
            {
                Log($"❌ Falha ao iniciar na porta {mPort}: {ex.Message}");
                OnStatusChanged?.Invoke($"Erro: {ex.Message}");
                return false;
            }

            mIsRunning = true;
            mServerThread = new Thread(ListenLoop) { IsBackground = true, Name = "LaserGRBL_CameraServer" };
            mServerThread.Start();

            string localUrl = GetLocalUrl();
            Log($"🚀 Servidor TCP iniciado na porta {mPort} (Pronto para Wi-Fi)");
            Log($"📡 Endereço do Celular: {localUrl}");
            Log("📱 Aponte a câmera do celular para o QR Code para conectar...");
            OnStatusChanged?.Invoke($"Servidor ativo em {localUrl}");
            return true;
        }

        public static void Stop()
        {
            if (!mIsRunning) return;

            mIsRunning = false;
            try
            {
                mExternalPollTimer?.Dispose();
                mExternalPollTimer = null;

                mTcpListener?.Stop();
                mTcpListener = null;
            }
            catch { }

            Log("⏹ Servidor de câmera parado pelo usuário.");
            OnStatusChanged?.Invoke("Servidor desconectado");
        }

        private static void ListenLoop()
        {
            while (mIsRunning && mTcpListener != null)
            {
                try
                {
                    TcpClient client = mTcpListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(ProcessClient, client);
                }
                catch
                {
                    if (!mIsRunning) break;
                }
            }
        }

        private static void ProcessClient(object state)
        {
            TcpClient client = (TcpClient)state;
            if (client == null) return;

            string clientIp = "Desconhecido";
            try
            {
                var ep = client.Client.RemoteEndPoint as IPEndPoint;
                if (ep != null) clientIp = ep.Address.ToString();
            }
            catch { }

            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    stream.ReadTimeout = 20000;
                    stream.WriteTimeout = 20000;

                    MemoryStream headerMs = new MemoryStream();
                    byte[] buffer = new byte[4096];
                    int headerEndPos = -1;

                    while (headerEndPos == -1)
                    {
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        if (bytesRead <= 0) break;
                        headerMs.Write(buffer, 0, bytesRead);

                        byte[] currentBytes = headerMs.ToArray();
                        headerEndPos = FindHeaderEnd(currentBytes);
                        if (headerMs.Length > 65536) break;
                    }

                    if (headerEndPos == -1)
                    {
                        client.Close();
                        return;
                    }

                    byte[] fullBytes = headerMs.ToArray();
                    string headerString = Encoding.UTF8.GetString(fullBytes, 0, headerEndPos);
                    string[] lines = headerString.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    if (lines.Length == 0) return;

                    string requestLine = lines[0];
                    string[] reqParts = requestLine.Split(' ');
                    string method = reqParts.Length > 0 ? reqParts[0].ToUpper() : "GET";
                    string path = reqParts.Length > 1 ? reqParts[1] : "/";

                    int contentLength = 0;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            int.TryParse(line.Substring(15).Trim(), out contentLength);
                        }
                    }

                    mLastClientActivity = DateTime.UtcNow;
                    mLastClientIp = clientIp;

                    if (method == "OPTIONS")
                    {
                        SendHttpResponse(stream, 200, "OK", "text/plain", new byte[0]);
                        return;
                    }

                    if (path == "/" || path.StartsWith("/camera"))
                    {
                        Log($"📱 Celular conectado ({clientIp}) — Interface web aberta no navegador");
                        byte[] html = Encoding.UTF8.GetBytes(GetMobileHtmlPage());
                        SendHttpResponse(stream, 200, "OK", "text/html; charset=utf-8", html);
                    }
                    else if (path.StartsWith("/upload") && method == "POST")
                    {
                        int bodyStart = headerEndPos + 4;
                        int bodyBytesAlreadyRead = fullBytes.Length - bodyStart;

                        byte[] bodyData = null;
                        int totalRead = 0;

                        if (contentLength > 0 && contentLength < 35 * 1024 * 1024)
                        {
                            bodyData = new byte[contentLength];
                            int copyLen = Math.Min(bodyBytesAlreadyRead, contentLength);
                            if (copyLen > 0)
                            {
                                Buffer.BlockCopy(fullBytes, bodyStart, bodyData, 0, copyLen);
                                totalRead = copyLen;
                            }

                            while (totalRead < contentLength)
                            {
                                int r = stream.Read(bodyData, totalRead, contentLength - totalRead);
                                if (r <= 0) break;
                                totalRead += r;
                            }
                        }
                        else
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                if (bodyBytesAlreadyRead > 0)
                                {
                                    ms.Write(fullBytes, bodyStart, bodyBytesAlreadyRead);
                                }
                                byte[] readBuf = new byte[8192];
                                while (ms.Length < 35 * 1024 * 1024)
                                {
                                    if (!stream.DataAvailable && ms.Length > 0)
                                    {
                                        Thread.Sleep(50);
                                        if (!stream.DataAvailable) break;
                                    }
                                    int r = stream.Read(readBuf, 0, readBuf.Length);
                                    if (r <= 0) break;
                                    ms.Write(readBuf, 0, r);
                                }
                                bodyData = ms.ToArray();
                                totalRead = bodyData.Length;
                            }
                        }

                        if (totalRead > 0 && bodyData != null)
                        {
                            try
                            {
                                using (MemoryStream imgMs = new MemoryStream(bodyData, 0, totalRead))
                                using (Image rawImg = Image.FromStream(imgMs))
                                {
                                    using (Bitmap bmp = new Bitmap(rawImg.Width, rawImg.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                                    {
                                        using (Graphics g = Graphics.FromImage(bmp))
                                        {
                                            g.DrawImage(rawImg, 0, 0, rawImg.Width, rawImg.Height);
                                        }

                                        CameraOverlayManager.UpdateFrame(bmp);
                                        mFramesCount++;
                                        OnFrameReceived?.Invoke();

                                        string desc = totalRead > 80000 ? "📸 Foto HD" : "🔴 Stream Live";
                                        Log($"{desc} recebido de {clientIp} ({bmp.Width}x{bmp.Height}, {totalRead / 1024} KB) [Frame #{mFramesCount}]");
                                    }
                                }

                                byte[] ok = Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"frames\":" + mFramesCount + "}");
                                SendHttpResponse(stream, 200, "OK", "application/json", ok);
                            }
                            catch (Exception ex)
                            {
                                Log($"⚠️ Erro decodificando foto de {clientIp}: {ex.Message}");
                                byte[] err = Encoding.UTF8.GetBytes($"{{\"error\":\"{ex.Message}\"}}");
                                SendHttpResponse(stream, 400, "Bad Request", "application/json", err);
                            }
                        }
                    }
                    else if (path.StartsWith("/rotate"))
                    {
                        CameraOverlayManager.RotatePhoto(90);
                        OnFrameReceived?.Invoke();
                        Log($"🔄 Foto rotacionada pelo celular ({clientIp}) -> Ângulo atual: {CameraOverlayManager.RotationAngle}°");
                        byte[] rot = Encoding.UTF8.GetBytes("{\"status\":\"ok\",\"rotation\":" + CameraOverlayManager.RotationAngle + "}");
                        SendHttpResponse(stream, 200, "OK", "application/json", rot);
                    }
                    else if (path.StartsWith("/status"))
                    {
                        byte[] status = Encoding.UTF8.GetBytes("{\"status\":\"running\",\"app\":\"LaserGRBL\",\"frames\":" + mFramesCount + "}");
                        SendHttpResponse(stream, 200, "OK", "application/json", status);
                    }
                    else
                    {
                        SendHttpResponse(stream, 404, "Not Found", "text/plain", Encoding.UTF8.GetBytes("Not Found"));
                    }
                }
            }
            catch { }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        private static int FindHeaderEnd(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length - 3; i++)
            {
                if (bytes[i] == '\r' && bytes[i + 1] == '\n' && bytes[i + 2] == '\r' && bytes[i + 3] == '\n')
                {
                    return i;
                }
            }
            return -1;
        }

        private static void SendHttpResponse(NetworkStream stream, int statusCode, string statusMsg, string contentType, byte[] body)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"HTTP/1.1 {statusCode} {statusMsg}\r\n");
            sb.Append("Server: LaserGRBL-CameraServer/2.0\r\n");
            sb.Append("Access-Control-Allow-Origin: *\r\n");
            sb.Append("Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n");
            sb.Append("Access-Control-Allow-Headers: Content-Type\r\n");
            sb.Append($"Content-Type: {contentType}\r\n");
            sb.Append($"Content-Length: {body.Length}\r\n");
            sb.Append("Connection: close\r\n");
            sb.Append("\r\n");

            byte[] headerBytes = Encoding.UTF8.GetBytes(sb.ToString());
            stream.Write(headerBytes, 0, headerBytes.Length);
            if (body.Length > 0)
            {
                stream.Write(body, 0, body.Length);
            }
            stream.Flush();
        }

        private static void ConfigureExternalPolling()
        {
            mExternalPollTimer?.Dispose();
            mExternalPollTimer = null;

            if (string.IsNullOrWhiteSpace(mExternalAppUrl)) return;

            mExternalPollTimer = new Timer(async (s) =>
            {
                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(mExternalAppUrl.Trim());
                    req.Timeout = 2000;
                    using (var resp = await req.GetResponseAsync())
                    using (var stream = resp.GetResponseStream())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await stream.CopyToAsync(ms);
                        ms.Position = 0;
                        using (Bitmap bmp = new Bitmap(ms))
                        {
                            CameraOverlayManager.UpdateFrame(bmp);
                            mFramesCount++;
                            OnFrameReceived?.Invoke();
                        }
                    }
                }
                catch { }
            }, null, 500, 1000);
        }

        /// <summary>
        /// Generates a standard ISO/IEC 18004 compliant QR Code using QRCoder.
        /// Guaranteed readable by all smartphone cameras.
        /// </summary>
        public static Bitmap GenerateQRCode(string content, int size = 220)
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content ?? "", QRCodeGenerator.ECCLevel.M))
                    {
                        using (QRCode qrCode = new QRCode(qrCodeData))
                        {
                            using (Bitmap raw = qrCode.GetGraphic(8, Color.FromArgb(15, 23, 42), Color.White, true))
                            {
                                if (size > 0 && (raw.Width != size || raw.Height != size))
                                {
                                    Bitmap resized = new Bitmap(size, size);
                                    using (Graphics g = Graphics.FromImage(resized))
                                    {
                                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                        g.DrawImage(raw, 0, 0, size, size);
                                    }
                                    return resized;
                                }
                                return (Bitmap)raw.Clone();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Erro gerando QR Code: {ex.Message}");
                Bitmap fallback = new Bitmap(size, size);
                using (Graphics g = Graphics.FromImage(fallback))
                {
                    g.Clear(Color.White);
                    using (Font font = new Font("Arial", 8f))
                    {
                        g.DrawString(content, font, Brushes.Black, new RectangleF(10, 10, size - 20, size - 20));
                    }
                }
                return fallback;
            }
        }

        private static string GetMobileHtmlPage()
        {
            return @"<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <title>LaserGRBL Câmera</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            background: #0f172a;
            color: #f8fafc;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            display: flex;
            flex-direction: column;
            height: 100vh;
            overflow: hidden;
        }
        header {
            background: #1e293b;
            padding: 12px 16px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            box-shadow: 0 2px 8px rgba(0,0,0,0.4);
            z-index: 10;
        }
        header h1 { font-size: 1.05rem; font-weight: 700; color: #60a5fa; }
        .badge {
            font-size: 0.75rem;
            font-weight: 600;
            padding: 4px 10px;
            border-radius: 9999px;
            background: #22c55e;
            color: #0f172a;
            transition: all 0.2s;
        }
        .viewport {
            flex: 1;
            position: relative;
            background: #000;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow: hidden;
        }
        video {
            width: 100%;
            height: 100%;
            object-fit: cover;
            display: none;
        }
        #imgPreview {
            width: 100%;
            height: 100%;
            object-fit: contain;
            display: none;
            background: #000;
        }
        .reticle {
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            width: 78%;
            height: 78%;
            border: 2px dashed rgba(59, 130, 246, 0.7);
            border-radius: 8px;
            pointer-events: none;
            display: none;
        }
        .reticle::after {
            content: '';
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            width: 22px;
            height: 22px;
            border-left: 2px solid #ef4444;
            border-top: 2px solid #ef4444;
        }
        .hero-card {
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: rgba(30, 41, 59, 0.96);
            border: 1px solid #3b82f6;
            border-radius: 16px;
            padding: 24px 20px;
            width: 90%;
            max-width: 380px;
            text-align: center;
            box-shadow: 0 10px 30px rgba(0,0,0,0.6);
            z-index: 20;
        }
        .hero-card h2 {
            font-size: 1.25rem;
            color: #60a5fa;
            margin-bottom: 8px;
        }
        .hero-card p {
            font-size: 0.88rem;
            color: #cbd5e1;
            margin-bottom: 20px;
            line-height: 1.45;
        }
        .btn-hero-primary {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            width: 100%;
            background: #059669;
            color: #fff;
            padding: 15px;
            border-radius: 12px;
            font-size: 1rem;
            font-weight: 700;
            text-align: center;
            cursor: pointer;
            box-shadow: 0 4px 16px rgba(5, 150, 105, 0.4);
            border: none;
            transition: all 0.15s ease;
        }
        .btn-hero-primary:active { transform: scale(0.97); background: #047857; }
        .btn-hero-primary input { display: none; }
        .btn-hero-secondary {
            width: 100%;
            background: #2563eb;
            color: #fff;
            padding: 12px;
            border-radius: 10px;
            border: none;
            font-size: 0.92rem;
            font-weight: 600;
            cursor: pointer;
            margin-top: 10px;
        }
        .info-btn {
            background: none;
            border: none;
            color: #94a3b8;
            font-size: 0.8rem;
            margin-top: 14px;
            text-decoration: underline;
            cursor: pointer;
            display: inline-block;
        }
        .controls {
            background: #1e293b;
            padding: 12px 16px 18px 16px;
            display: flex;
            flex-direction: column;
            gap: 10px;
            z-index: 10;
        }
        .btn-row {
            display: flex;
            gap: 8px;
            justify-content: center;
        }
        button, .btn-action {
            border: none;
            cursor: pointer;
            border-radius: 10px;
            font-weight: 600;
            font-size: 0.9rem;
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 6px;
            transition: all 0.15s ease;
        }
        .btn-photo-main {
            flex: 2;
            background: #059669;
            color: #fff;
            padding: 14px 12px;
            box-shadow: 0 4px 14px rgba(5, 150, 105, 0.4);
            font-size: 0.95rem;
            font-weight: 700;
        }
        .btn-photo-main:active { transform: scale(0.97); background: #047857; }
        .btn-photo-main input { display: none; }
        .btn-stream {
            flex: 1.2;
            background: #334155;
            color: #e2e8f0;
            padding: 14px 10px;
        }
        .btn-stream.active {
            background: #dc2626;
            color: #fff;
            animation: pulse 1.5s infinite;
        }
        .btn-small {
            width: 46px;
            background: #334155;
            color: #fbbf24;
            font-size: 1.15rem;
        }
        @keyframes pulse {
            0%, 100% { opacity: 1; }
            50% { opacity: 0.7; }
        }
        .status-bar {
            text-align: center;
            font-size: 0.8rem;
            color: #94a3b8;
            min-height: 18px;
        }
        .toast {
            position: absolute;
            top: 70px;
            left: 50%;
            transform: translateX(-50%);
            background: #16a34a;
            color: white;
            padding: 10px 18px;
            border-radius: 20px;
            font-size: 0.88rem;
            font-weight: 600;
            box-shadow: 0 6px 18px rgba(0,0,0,0.5);
            opacity: 0;
            transition: opacity 0.3s;
            pointer-events: none;
            z-index: 30;
            text-align: center;
            white-space: nowrap;
        }
        .toast.show { opacity: 1; }
        /* Modal for Help / HTTPS info */
        .modal-bg {
            display: none;
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.8);
            z-index: 50;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .modal-card {
            background: #1e293b;
            border: 1px solid #475569;
            border-radius: 16px;
            padding: 22px;
            max-width: 420px;
            width: 100%;
            text-align: left;
        }
        .modal-card h3 {
            font-size: 1.1rem;
            color: #60a5fa;
            margin-bottom: 12px;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .modal-card p {
            font-size: 0.85rem;
            color: #cbd5e1;
            margin-bottom: 12px;
            line-height: 1.45;
        }
        .modal-card ul {
            font-size: 0.85rem;
            color: #cbd5e1;
            margin-left: 20px;
            margin-bottom: 16px;
            line-height: 1.5;
        }
        .btn-close-modal {
            width: 100%;
            background: #3b82f6;
            color: #fff;
            padding: 12px;
            border-radius: 10px;
            font-weight: 700;
            border: none;
            cursor: pointer;
        }
    </style>
</head>
<body>
    <header>
        <h1>LaserGRBL Câmera</h1>
        <span class='badge' id='badge'>CONECTADO</span>
    </header>

    <div class='viewport'>
        <video id='v' autoplay playsinline muted></video>
        <img id='imgPreview' alt='Foto da Mesa do Laser'>
        <div class='reticle' id='reticle'></div>
        <canvas id='c' style='display:none;'></canvas>
        <div class='toast' id='toast'>✅ Foto enviada para o LaserGRBL!</div>

        <!-- Hero Prompt Card -->
        <div class='hero-card' id='heroCard'>
            <div style='font-size:2.6rem; margin-bottom:10px;'>📷</div>
            <h2>Foto da Mesa de Laser</h2>
            <p>Aponte o celular no suporte para a máquina e tire uma foto nítida para alinhar seus trabalhos com precisão milimétrica.</p>
            <label class='btn-hero-primary'>
                📸 Tirar Foto da Mesa (Câmera Nativa)
                <input type='file' accept='image/*' capture='environment' onchange='handleNativeFile(event)'>
            </label>
            <button class='btn-hero-secondary' id='btnLiveCard' style='display:none;' onclick='initCamera()'>
                ▶ Transmitir Vídeo Ao Vivo
            </button>
            <button class='info-btn' onclick='openHelpModal()'>ℹ️ Por que transmissão de vídeo pode não abrir?</button>
        </div>
    </div>

    <div class='controls'>
        <div class='btn-row'>
            <label class='btn-action btn-photo-main' title='Tirar foto com a câmera do celular'>
                📸 Tirar Foto da Mesa
                <input type='file' accept='image/*' capture='environment' onchange='handleNativeFile(event)'>
            </label>
            <button class='btn-stream' id='btnStream' onclick='toggleStream()'>🔴 Live</button>
            <button class='btn-small' id='btnRotate' title='Girar Foto (90°)' onclick='rotatePhoto()'>🔄</button>
            <button class='btn-small' id='btnTorch' onclick='toggleTorch()'>💡</button>
            <button class='btn-small' onclick='openHelpModal()'>❓</button>
        </div>
        <div class='status-bar' id='status'>Toque em 'Tirar Foto da Mesa' para enviar</div>
    </div>

    <!-- Help Modal -->
    <div class='modal-bg' id='helpModal'>
        <div class='modal-card'>
            <h3>ℹ️ Dicas de Câmera & Vídeo</h3>
            <p><b>Por que o vídeo ao vivo contínuo às vezes não abre no celular?</b></p>
            <p>Os navegadores móveis (Chrome e Safari) restringem o acesso direto a vídeo contínuo (WebRTC) apenas a conexões criptografadas com <code>https://</code>.</p>
            <p><b>Opções disponíveis:</b></p>
            <ul>
                <li><b>📸 Câmera Nativa (Recomendado):</b> Funciona 100% no Wi-Fi com 1 toque! Tira uma foto em resolução total da sua câmera e projeta instantaneamente no LaserGRBL.</li>
                <li><b>App de Câmera IP:</b> Baixe o app gratuito <i>'IP Webcam'</i> ou <i>'DroidCam'</i> no celular e cole o endereço IP direto no LaserGRBL para vídeo contínuo em 30 FPS.</li>
                <li><b>Chrome Flags:</b> No Chrome do celular, abra <code>chrome://flags/#unsafely-treat-insecure-origin-as-secure</code>, adicione este endereço e ative.</li>
            </ul>
            <button class='btn-close-modal' onclick='closeHelpModal()'>Entendi</button>
        </div>
    </div>

    <script>
        const video = document.getElementById('v');
        const imgPreview = document.getElementById('imgPreview');
        const canvas = document.getElementById('c');
        const status = document.getElementById('status');
        const badge = document.getElementById('badge');
        const heroCard = document.getElementById('heroCard');
        const reticle = document.getElementById('reticle');
        const toast = document.getElementById('toast');
        const helpModal = document.getElementById('helpModal');
        const btnLiveCard = document.getElementById('btnLiveCard');

        let currentStream = null;
        let isStreaming = false;
        let streamTimer = null;
        let torchOn = false;
        let currentPreviewRot = 0;

        function showToast(msg) {
            toast.innerText = msg;
            toast.classList.add('show');
            setTimeout(() => toast.classList.remove('show'), 2600);
        }

        function openHelpModal() { helpModal.style.display = 'flex'; }
        function closeHelpModal() { helpModal.style.display = 'none'; }

        // Client-side downscaler & converter to 1080p JPEG
        function handleNativeFile(event) {
            const file = event.target.files && event.target.files[0];
            if (!file) return;

            badge.innerText = 'PROCESSANDO';
            badge.style.background = '#f59e0b';
            status.innerText = 'Processando foto (' + Math.round(file.size / 1024) + ' KB)...';

            const reader = new FileReader();
            reader.onerror = () => {
                badge.innerText = 'ERRO';
                badge.style.background = '#ef4444';
                status.innerText = 'Erro ao ler arquivo da câmera.';
            };
            reader.onload = (e) => {
                const img = new Image();
                img.onerror = () => {
                    badge.innerText = 'ERRO';
                    badge.style.background = '#ef4444';
                    status.innerText = 'Formato de imagem não reconhecido.';
                };
                img.onload = () => {
                    // Downscale to max 1920x1080 (HD) for instant 50ms upload and pristine GDI+ decoding
                    const maxDim = 1920;
                    let w = img.width;
                    let h = img.height;
                    if (w > maxDim || h > maxDim) {
                        if (w > h) {
                            h = Math.round(h * maxDim / w);
                            w = maxDim;
                        } else {
                            w = Math.round(w * maxDim / h);
                            h = maxDim;
                        }
                    }
                    canvas.width = w;
                    canvas.height = h;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, w, h);

                    // Immediate preview on phone screen!
                    canvas.toBlob(async (blob) => {
                        if (!blob) return;

                        // Display on phone screen
                        const previewUrl = URL.createObjectURL(blob);
                        imgPreview.src = previewUrl;
                        currentPreviewRot = 0;
                        imgPreview.style.transform = 'none';
                        imgPreview.style.display = 'block';
                        video.style.display = 'none';
                        heroCard.style.display = 'none';
                        reticle.style.display = 'block';

                        badge.innerText = 'ENVIANDO';
                        badge.style.background = '#3b82f6';
                        status.innerText = 'Enviando foto HD (' + Math.round(blob.size / 1024) + ' KB) para o LaserGRBL...';

                        try {
                            const resp = await fetch('/upload', {
                                method: 'POST',
                                headers: { 'Content-Type': 'image/jpeg' },
                                body: blob
                            });

                            if (resp.ok) {
                                badge.innerText = 'FOTO APLICADA';
                                badge.style.background = '#22c55e';
                                status.innerText = '✅ Foto HD aplicada na mesa do LaserGRBL com sucesso!';
                                showToast('✅ Foto aplicada com sucesso no LaserGRBL!');
                            } else {
                                throw new Error('Servidor retornou erro: ' + resp.status);
                            }
                        } catch (err) {
                            badge.innerText = 'ERRO';
                            badge.style.background = '#ef4444';
                            status.innerText = 'Erro ao enviar foto: ' + err.message;
                        }
                    }, 'image/jpeg', 0.88);
                };
                img.src = e.target.result;
            };
            reader.readAsDataURL(file);
        }

        async function rotatePhoto() {
            try {
                badge.innerText = 'GIRANDO';
                badge.style.background = '#3b82f6';
                const resp = await fetch('/rotate', { method: 'POST' });
                if (resp.ok) {
                    const data = await resp.json();
                    showToast('🔄 Foto rotacionada: ' + data.rotation + '°');
                    badge.innerText = 'GIRO ' + data.rotation + '°';
                    badge.style.background = '#22c55e';
                    status.innerText = 'Foto rotacionada no LaserGRBL para ' + data.rotation + '°';
                    currentPreviewRot = (currentPreviewRot + 90) % 360;
                    if (imgPreview.style.display !== 'none') {
                        imgPreview.style.transform = 'rotate(' + currentPreviewRot + 'deg)';
                    }
                }
            } catch (e) {
                status.innerText = 'Erro ao girar foto: ' + e.message;
            }
        }

        async function initCamera() {
            if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                openHelpModal();
                return;
            }

            status.innerText = 'Solicitando acesso à câmera...';
            badge.innerText = 'CONECTANDO...';
            badge.style.background = '#f59e0b';

            const constraintsList = [
                { video: { facingMode: { ideal: 'environment' }, width: { ideal: 1920 }, height: { ideal: 1080 } } },
                { video: { facingMode: 'environment' } },
                { video: true }
            ];

            let stream = null;
            let lastErr = null;

            for (const constraints of constraintsList) {
                try {
                    stream = await navigator.mediaDevices.getUserMedia(constraints);
                    if (stream) break;
                } catch (e) {
                    lastErr = e;
                }
            }

            if (!stream) {
                status.innerText = 'Câmera não permitida pelo navegador. Use o botão Tirar Foto!';
                badge.innerText = 'ERRO';
                badge.style.background = '#ef4444';
                openHelpModal();
                return;
            }

            currentStream = stream;
            video.srcObject = stream;
            video.style.display = 'block';
            imgPreview.style.display = 'none';
            try { await video.play(); } catch (e) { }

            heroCard.style.display = 'none';
            reticle.style.display = 'block';
            status.innerText = 'Câmera ao vivo conectada!';
            badge.innerText = 'AO VIVO';
            badge.style.background = '#22c55e';
            showToast('✅ Câmera ao vivo ativada!');

            setTimeout(() => uploadFrame(0.85), 600);
        }

        async function uploadFrame(quality = 0.80) {
            if (!video.videoWidth) return;
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0);

            canvas.toBlob(async (blob) => {
                if (!blob) return;
                try {
                    await fetch('/upload', { method: 'POST', headers: { 'Content-Type': 'image/jpeg' }, body: blob });
                } catch (e) {}
            }, 'image/jpeg', quality);
        }

        function toggleStream() {
            if (!currentStream) {
                initCamera();
                return;
            }

            const btn = document.getElementById('btnStream');
            isStreaming = !isStreaming;
            if (isStreaming) {
                btn.classList.add('active');
                btn.innerText = '⏹ Parar';
                badge.innerText = 'TRANSMITINDO';
                badge.style.background = '#ef4444';
                status.innerText = 'Transmitindo vídeo contínuo para o LaserGRBL...';
                streamTimer = setInterval(() => uploadFrame(0.65), 180);
                showToast('🔴 Transmissão contínua ligada!');
            } else {
                btn.classList.remove('active');
                btn.innerText = '🔴 Live';
                badge.innerText = 'AO VIVO';
                badge.style.background = '#22c55e';
                clearInterval(streamTimer);
                status.innerText = 'Transmissão contínua pausada';
            }
        }

        async function toggleTorch() {
            if (!currentStream) {
                alert('Ative a câmera ao vivo para acender a lanterna.');
                return;
            }
            const track = currentStream.getVideoTracks()[0];
            try {
                torchOn = !torchOn;
                await track.applyConstraints({ advanced: [{ torch: torchOn }] });
                document.getElementById('btnTorch').style.background = torchOn ? '#fbbf24' : '#334155';
            } catch (e) {
                alert('Lanterna não controlável neste navegador.');
            }
        }

        // Check mediaDevices on load
        window.addEventListener('load', () => {
            const hasMedia = !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
            if (hasMedia) {
                btnLiveCard.style.display = 'block';
            }
        });
    </script>
</body>
</html>";
        }
    }
}
