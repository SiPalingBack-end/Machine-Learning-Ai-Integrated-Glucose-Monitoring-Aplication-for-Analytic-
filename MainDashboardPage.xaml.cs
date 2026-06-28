using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Alprogcitra1.Models;
using Alprogcitra1.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Alprogcitra1
{
    public partial class MainDashboardPage : ContentPage
    {
        private IDispatcherTimer _signalTimer;
        private IDispatcherTimer _mlTimer;
        private Random _random = new Random();

        // Simpan titik sinyal untuk grafik
        private List<double> _signalValues = new List<double>();
        private const int MaxPoints = 100;
        private double _tick = 0;

        // ============================================================
        // ML SERVICES — Fuzzy Logic + ANN (digunakan langsung di dashboard)
        // ============================================================
        private readonly FuzzyLogicService _fuzzyService;
        private readonly AnnService _annService;

        // ============================================================
        // CHATBOT — Service dan Data
        // ============================================================
        private readonly ChatbotService _chatbot;
        private readonly List<ChatMessage> _chatMessages = new();
        private bool _isChatOpen = false;

        public MainDashboardPage()
        {
            InitializeComponent();

            // Inisialisasi ML Services
            _fuzzyService = new FuzzyLogicService();
            _annService = new AnnService();

            // Inisialisasi Chatbot Service (juga menggunakan NLP + Fuzzy + ANN secara internal)
            _chatbot = new ChatbotService();

            // 1. Timer Sinyal Real-time (Berjalan cepat untuk efek gelombang, misal 40ms)
            _signalTimer = Dispatcher.CreateTimer();
            _signalTimer.Interval = TimeSpan.FromMilliseconds(40);
            _signalTimer.Tick += OnSignalTick;
            _signalTimer.Start();

            // 2. Timer ML & Metrik (Diperbarui setiap 3 detik seperti catatan Anda)
            _mlTimer = Dispatcher.CreateTimer();
            _mlTimer.Interval = TimeSpan.FromSeconds(3);
            _mlTimer.Tick += OnMlTick;
            _mlTimer.Start();

            // 3. Tampilkan pesan selamat datang di chatbot
            AddBotWelcomeMessage();
        }

        // --- 1. PROSES DRAWING SINYAL BIOMEDIS REAL-TIME ---
        private void OnSignalTick(object sender, EventArgs e)
        {
            _tick += 0.2;

            // Simulasi sinyal analog (Gabungan komponen denyut nadi utama + noise analog frekuensi tinggi)
            double baseSignal = Math.Sin(_tick);
            double noise = 0.2 * Math.Sin(_tick * 10);
            double rawSignal = baseSignal + noise;

            _signalValues.Add(rawSignal);

            if (_signalValues.Count > MaxPoints)
                _signalValues.RemoveAt(0);

            // Perintahkan GraphicsView untuk menggambar ulang (Redraw)
            SignalCanvas.Drawable = new SignalDrawable(_signalValues);
            SignalCanvas.Invalidate();
        }

        // --- 2. PROSES INFERENSI MACHINE LEARNING ---
        private void OnMlTick(object sender, EventArgs e)
        {
            // Simulasi ekstraksi fitur dari sinyal analog yang masuk
            int hr = _random.Next(72, 98);
            int bs = _random.Next(85, 175); // Variasi glukosa simulasi

            LabelHeartRate.Text = hr.ToString();
            LabelBloodSugar.Text = bs.ToString();

            // Update nilai chatbot dengan data terkini
            _chatbot.CurrentGlucose = bs;
            _chatbot.CurrentHeartRate = hr;

            // Jalankan klasifikasi menggunakan Fuzzy Logic + ANN
            RunPredictiveModel(hr, bs);
        }

        // ============================================================
        // PREDIKSI ML — Menggunakan FuzzyLogicService + AnnService
        // ============================================================
        private void RunPredictiveModel(int heartRate, int bloodSugar)
        {
            // ── Langkah 1: Analisis Fuzzy Logic ──
            FuzzyResult fuzzyResult = _fuzzyService.ClassifyGlucose(bloodSugar);

            // ── Langkah 2: Prediksi ANN ──
            AnnResult annResult = _annService.Predict(
                glucose: bloodSugar,
                heartRate: heartRate,
                age: _chatbot.PatientAge,
                bmi: _chatbot.PatientBmi
            );

            // ── Langkah 3: Update Warna Card berdasarkan Fuzzy Risk Level ──
            UpdateRiskCardColor(fuzzyResult.RiskLevel);

            // ── Langkah 4: Update Label Hasil Prediksi ML ──
            LabelMlResult.Text = $"{annResult.PredictedClass.ToUpper()} — {fuzzyResult.Category}";
            LabelConfidence.Text = $"{annResult.Confidence:F1}%";

            // ── Langkah 5: Update Deskripsi dengan rekomendasi dari Fuzzy ──
            string shortRecommendation = fuzzyResult.Recommendation;
            // Ambil hanya baris pertama rekomendasi untuk deskripsi singkat
            if (shortRecommendation.Contains('\n'))
                shortRecommendation = shortRecommendation.Split('\n')[0];
            LabelMlDesc.Text = shortRecommendation;

            // ── Langkah 6: Update detail Fuzzy Logic ──
            LabelFuzzyCategory.Text = fuzzyResult.Category;
            LabelFuzzyRiskScore.Text = $"{fuzzyResult.RiskScore:F1} / 100";
            LabelFuzzyRiskLevel.Text = fuzzyResult.RiskLevel;
            LabelFuzzyRiskLevel.TextColor = Color.FromArgb(fuzzyResult.ColorCode);

            // Derajat keanggotaan — tampilkan hanya yang > 0
            string membershipText = "";
            foreach (var (cat, degree) in fuzzyResult.MembershipDegrees)
            {
                if (degree > 0.01)
                {
                    string bar = new string('█', (int)(degree * 10));
                    string empty = new string('░', 10 - (int)(degree * 10));
                    membershipText += $"{cat}: {bar}{empty} {degree:F2}\n";
                }
            }
            LabelFuzzyMembership.Text = membershipText.TrimEnd();

            // ── Langkah 7: Update detail ANN ──
            LabelAnnPrediction.Text = annResult.PredictedClass;
            LabelAnnConfidence.Text = $"{annResult.Confidence:F1}%";
            LabelAnnNormal.Text = $"{annResult.Probabilities[0] * 100:F1}%";
            LabelAnnPrediabetes.Text = $"{annResult.Probabilities[1] * 100:F1}%";
            LabelAnnDiabetes.Text = $"{annResult.Probabilities[2] * 100:F1}%";
        }

        /// <summary>
        /// Update warna gradient card berdasarkan risk level dari Fuzzy Logic
        /// </summary>
        private void UpdateRiskCardColor(string riskLevel)
        {
            GradientStopCollection stops;
            switch (riskLevel)
            {
                case "Kritis":
                    stops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#7F1D1D"), 0.0f),
                        new GradientStop(Color.FromArgb("#DC2626"), 0.5f),
                        new GradientStop(Color.FromArgb("#EF4444"), 1.0f)
                    };
                    break;
                case "Tinggi":
                    stops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#DC2626"), 0.0f),
                        new GradientStop(Color.FromArgb("#EF4444"), 0.5f),
                        new GradientStop(Color.FromArgb("#F43F5E"), 1.0f)
                    };
                    break;
                case "Sedang":
                    stops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#D97706"), 0.0f),
                        new GradientStop(Color.FromArgb("#F59E0B"), 0.5f),
                        new GradientStop(Color.FromArgb("#FBBF24"), 1.0f)
                    };
                    break;
                default: // "Rendah"
                    stops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#059669"), 0.0f),
                        new GradientStop(Color.FromArgb("#10B981"), 0.5f),
                        new GradientStop(Color.FromArgb("#14B8A6"), 1.0f)
                    };
                    break;
            }

            MlRiskBorder.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = stops
            };
        }

        // ============================================================
        // CHATBOT — UI Event Handlers
        // ============================================================

        /// <summary>
        /// Toggle buka/tutup chatbot panel via FAB
        /// </summary>
        private void OnChatFabClicked(object sender, EventArgs e)
        {
            _isChatOpen = !_isChatOpen;
            ChatOverlay.IsVisible = _isChatOpen;
            ChatFab.IsVisible = !_isChatOpen;
        }

        /// <summary>
        /// Tutup chatbot panel
        /// </summary>
        private void OnCloseChatClicked(object sender, EventArgs e)
        {
            _isChatOpen = false;
            ChatOverlay.IsVisible = false;
            ChatFab.IsVisible = true;
        }

        /// <summary>
        /// Kirim pesan saat tombol kirim ditekan
        /// </summary>
        private void OnSendMessageClicked(object sender, EventArgs e)
        {
            SendMessage();
        }

        /// <summary>
        /// Kirim pesan saat user menekan Enter di Entry
        /// </summary>
        private void OnChatInputCompleted(object sender, EventArgs e)
        {
            SendMessage();
        }

        // ============================================================
        // CHATBOT — Core Logic
        // ============================================================

        /// <summary>
        /// Pesan selamat datang saat chatbot pertama kali dibuka
        /// </summary>
        private void AddBotWelcomeMessage()
        {
            var welcomeMsg = new ChatMessage
            {
                Text = "👋 Halo! Saya GlucoBot — asisten informasi glukosa darah Anda.\n\n" +
                       "Saya didukung oleh:\n" +
                       "🔹 NLP — Memahami pertanyaan Anda\n" +
                       "🔹 Fuzzy Logic — Klasifikasi glukosa\n" +
                       "🔹 ANN — Prediksi risiko diabetes\n\n" +
                       "Silakan bertanya! Ketik \"bantuan\" untuk melihat fitur.",
                IsBot = true,
                MessageType = "greeting"
            };
            _chatMessages.Add(welcomeMsg);
            AddChatBubbleToUI(welcomeMsg);
        }

        /// <summary>
        /// Proses dan kirim pesan
        /// </summary>
        private void SendMessage()
        {
            string userText = ChatInput?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(userText)) return;

            // 1. Tambah pesan user ke UI
            var userMsg = new ChatMessage
            {
                Text = userText,
                IsBot = false,
                MessageType = "user"
            };
            _chatMessages.Add(userMsg);
            AddChatBubbleToUI(userMsg);

            // 2. Kosongkan input
            ChatInput.Text = string.Empty;

            // 3. Proses dengan ChatbotService (NLP → Fuzzy → ANN)
            string botResponse = _chatbot.ProcessMessage(userText);

            // 4. Tambah respons bot ke UI
            var botMsg = new ChatMessage
            {
                Text = botResponse,
                IsBot = true,
                MessageType = "analysis"
            };
            _chatMessages.Add(botMsg);
            AddChatBubbleToUI(botMsg);

            // 5. Scroll ke bawah
            ScrollChatToBottom();
        }

        /// <summary>
        /// Buat dan tambahkan bubble chat ke UI secara dinamis
        /// </summary>
        private void AddChatBubbleToUI(ChatMessage message)
        {
            // Tentukan alignment dan warna berdasarkan pengirim
            var bubbleBorder = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = message.IsBot
                        ? new CornerRadius(4, 16, 16, 16)
                        : new CornerRadius(16, 4, 16, 16)
                },
                StrokeThickness = 0,
                Padding = new Thickness(14, 10),
                MaximumWidthRequest = 340,
                HorizontalOptions = message.IsBot ? LayoutOptions.Start : LayoutOptions.End,
            };

            if (message.IsBot)
            {
                // Bot bubble: Teal gradient
                bubbleBorder.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#0D4F4F"), 0.0f),
                        new GradientStop(Color.FromArgb("#115E59"), 1.0f)
                    }
                };
            }
            else
            {
                // User bubble: Purple gradient
                bubbleBorder.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb("#4C1D95"), 0.0f),
                        new GradientStop(Color.FromArgb("#6C3CE1"), 1.0f)
                    }
                };
            }

            // Konten bubble
            var content = new VerticalStackLayout { Spacing = 4 };

            // Label pengirim
            content.Children.Add(new Label
            {
                Text = message.SenderLabel,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = message.IsBot ? Color.FromArgb("#5EEAD4") : Color.FromArgb("#C4B5FD"),
            });

            // Teks pesan
            content.Children.Add(new Label
            {
                Text = message.Text,
                FontSize = 13,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.WordWrap,
            });

            // Timestamp
            content.Children.Add(new Label
            {
                Text = message.Timestamp.ToString("HH:mm"),
                FontSize = 9,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.End,
            });

            bubbleBorder.Content = content;
            ChatMessagesStack.Children.Add(bubbleBorder);
        }

        /// <summary>
        /// Scroll chat ke pesan terbaru
        /// </summary>
        private async void ScrollChatToBottom()
        {
            try
            {
                await Task.Delay(100);
                await ChatScrollView.ScrollToAsync(0, ChatMessagesStack.Height, true);
            }
            catch { /* Ignore scroll errors */ }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _signalTimer?.Stop();
            _mlTimer?.Stop();
        }
    }

    // --- KELAS KHUSUS UNTUK MENGGAMBAR GELOMBANG SINYAL ---
    public class SignalDrawable : IDrawable
    {
        private List<double> _points;

        public SignalDrawable(List<double> points)
        {
            _points = points;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_points == null || _points.Count < 2) return;

            // Grid lines (subtle)
            canvas.StrokeColor = Color.FromArgb("#1E3A5F");
            canvas.StrokeSize = 0.5f;
            for (int i = 1; i < 4; i++)
            {
                float gy = dirtyRect.Height * i / 4;
                canvas.DrawLine(0, gy, dirtyRect.Width, gy);
            }
            for (int i = 1; i < 8; i++)
            {
                float gx = dirtyRect.Width * i / 8;
                canvas.DrawLine(gx, 0, gx, dirtyRect.Height);
            }

            // Gambar glow (blur) di bawah sinyal utama
            canvas.StrokeColor = Color.FromArgb("#00FF6640");
            canvas.StrokeSize = 6;

            float midY = dirtyRect.Height / 2;
            float stepX = dirtyRect.Width / (_points.Count - 1);

            PathF glowPath = new PathF();
            for (int i = 0; i < _points.Count; i++)
            {
                float x = i * stepX;
                float y = midY - (float)(_points[i] * (dirtyRect.Height / 3));
                if (i == 0) glowPath.MoveTo(x, y);
                else glowPath.LineTo(x, y);
            }
            canvas.DrawPath(glowPath);

            // Set up brush/pen untuk sinyal utama (Warna Hijau Neon khas Monitor Medis)
            canvas.StrokeColor = Color.FromArgb("#00FF66");
            canvas.StrokeSize = 2.5f;

            PathF signalPath = new PathF();

            for (int i = 0; i < _points.Count; i++)
            {
                float x = i * stepX;
                // Penskalaan nilai amplitudo agar pas di tinggi canvas
                float y = midY - (float)(_points[i] * (dirtyRect.Height / 3));

                if (i == 0)
                    signalPath.MoveTo(x, y);
                else
                    signalPath.LineTo(x, y);
            }

            canvas.DrawPath(signalPath);

            // Titik terakhir (highlight)
            if (_points.Count > 0)
            {
                float lastX = (_points.Count - 1) * stepX;
                float lastY = midY - (float)(_points[_points.Count - 1] * (dirtyRect.Height / 3));
                canvas.FillColor = Color.FromArgb("#00FF66");
                canvas.FillCircle(lastX, lastY, 4);
            }
        }
    }
}