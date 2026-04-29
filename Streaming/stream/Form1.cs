using Microsoft.AspNetCore.SignalR.Client;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using SIPSorceryMedia.Windows;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace stream
{
    public partial class Form1 : Form
    {
        private RTCPeerConnection? _peerConnection;
        private WindowsVideoEndPoint? _videoEndPoint;
        private WindowsAudioEndPoint? _audioEndPoint;
        private HubConnection? _signalingConnection;

        // Đặt biến ở đây (Đổi thành IP Tailscale của bạn)
        private readonly string SIGNALING_URL = "http://100.x.x.x:5000/signaling";

        public Form1()
        {
            InitializeComponent();
            picLocalVideo.SizeMode = PictureBoxSizeMode.Zoom;
            picRemoteVideo.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await SetupSignalR();
            await InitializeMediaAndWebRTC();
        }

        // ==========================================
        // 1. SIGNALR: XỬ LÝ NHẬN TÍN HIỆU (OFFER, ANSWER, ICE)
        // ==========================================
        private async Task SetupSignalR()
        {
            _signalingConnection = new HubConnectionBuilder()
                .WithUrl(SIGNALING_URL)
                .Build();

            _signalingConnection.On<string>("ReceiveSignal", async (jsonMsg) =>
            {
                // Đưa luồng xử lý về Main UI Thread
                this.Invoke(new Action(async () =>
                {
                    try
                    {
                        // Nếu là SDP (Offer hoặc Answer)
                        if (jsonMsg.Contains("\"type\"") && jsonMsg.Contains("\"sdp\""))
                        {
                            if (RTCSessionDescriptionInit.TryParse(jsonMsg, out var sdpInit))
                            {
                                var result = _peerConnection.setRemoteDescription(sdpInit);

                                // Nếu nhận được Offer, tự động tạo Answer gửi lại
                                if (sdpInit.type == RTCSdpType.offer)
                                {
                                    var answer = _peerConnection.createAnswer(null);
                                    await _peerConnection.setLocalDescription(answer);
                                    await _signalingConnection.InvokeAsync("SendSignal", answer.toJSON());
                                }
                            }
                        }
                        // Nếu là ICE Candidate (Gói tin định tuyến)
                        else if (jsonMsg.Contains("\"candidate\""))
                        {
                            if (RTCIceCandidateInit.TryParse(jsonMsg, out var iceInit))
                            {
                                _peerConnection.addIceCandidate(iceInit);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Lỗi Parse Signal: " + ex.Message);
                    }
                }));
            });

            try
            {
                await _signalingConnection.StartAsync();
                this.Text = "P2P Video Call - Đã nối tới Server";
            }
            catch (Exception)
            {
                MessageBox.Show("Không thể kết nối Server. Vui lòng kiểm tra lại IP hoặc Tường lửa.");
            }
        }

        // ==========================================
        // 2. WEBRTC: KHỞI TẠO CAMERA, MIC VÀ P2P
        // ==========================================
        private async Task InitializeMediaAndWebRTC()
        {
            _videoEndPoint = new WindowsVideoEndPoint(new VpxVideoEncoder());
            _audioEndPoint = new WindowsAudioEndPoint(new AudioEncoder());

            // Lấy hình ảnh Camera của mình vẽ lên picLocalVideo
            _videoEndPoint.OnVideoSourceEncodedSample += (duration, buffer) =>
            {
                // TODO: Xử lý frame nội bộ nếu muốn, thường WindowsVideoEndPoint tự handle khá tốt
            };

            var config = new RTCConfiguration
            {
                iceServers = { new RTCIceServer { urls = "stun:stun.l.google.com:19302" } }
            };
            _peerConnection = new RTCPeerConnection(config);

            // Nạp Track Media
            _peerConnection.addTrack(new MediaStreamTrack(_videoEndPoint.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv));
            _peerConnection.addTrack(new MediaStreamTrack(_audioEndPoint.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv));

            // Nhận Video người kia
            _peerConnection.OnVideoFormatsNegotiated += (formats) => _videoEndPoint.SetVideoSinkFormat(formats[0]);
            _videoEndPoint.OnVideoSinkDecodedSample += (byte[] bmpBytes, uint width, uint height, int stride, VideoPixelFormatsEnum format) =>
            {
                RenderRemoteVideo(bmpBytes, (int)width, (int)height);
            };

            // Bắn ICE Candidate qua Server
            _peerConnection.onicecandidate += async (candidate) =>
            {
                if (_signalingConnection.State == HubConnectionState.Connected)
                {
                    await _signalingConnection.InvokeAsync("SendSignal", candidate.toJSON());
                }
            };

            // Bật thiết bị Media
            await _videoEndPoint.StartVideo();
            await _audioEndPoint.StartAudio();
        }

        // ==========================================
        // 3. NÚT GỌI: TẠO OFFER ĐỂ BẮT ĐẦU
        // ==========================================
        private async void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;

            // Tạo lời mời
            var offer = _peerConnection.createOffer(null);
            await _peerConnection.setLocalDescription(offer);

            // Bắn lời mời qua Server
            if (_signalingConnection.State == HubConnectionState.Connected)
            {
                await _signalingConnection.InvokeAsync("SendSignal", offer.toJSON());
            }
        }

        // ==========================================
        // 4. HÀM HIỂN THỊ VIDEO AN TOÀN RAM & THREAD
        // ==========================================
        private void RenderRemoteVideo(byte[] rawPixels, int width, int height)
        {
            if (picRemoteVideo.InvokeRequired)
            {
                picRemoteVideo.Invoke(new Action(() => RenderRemoteVideo(rawPixels, width, height)));
                return;
            }

            try
            {
                // SIPSorcery trả về BGR32 chuẩn trên Windows
                Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppRgb);
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);

                Marshal.Copy(rawPixels, 0, bmpData.Scan0, rawPixels.Length);
                bmp.UnlockBits(bmpData);

                var oldImage = picRemoteVideo.Image;
                picRemoteVideo.Image = bmp;
                oldImage?.Dispose(); // Tránh tràn RAM (Memory Leak)
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _peerConnection?.Close("Exit");
            _videoEndPoint?.PauseVideo();
            base.OnFormClosing(e);
        }
    }
}