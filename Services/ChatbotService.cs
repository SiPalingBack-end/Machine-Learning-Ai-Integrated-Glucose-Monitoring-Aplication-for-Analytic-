using System;
using System.Collections.Generic;
using Alprogcitra1.Models;

namespace Alprogcitra1.Services
{
    /// <summary>
    /// CHATBOT SERVICE — Orkestrasi NLP + Fuzzy Logic + ANN
    /// Mengkoordinasikan ketiga algoritma untuk menghasilkan respons chatbot
    /// yang informatif tentang kadar glukosa darah dan risiko diabetes.
    /// </summary>
    public class ChatbotService
    {
        private readonly NlpService _nlp;
        private readonly FuzzyLogicService _fuzzy;
        private readonly AnnService _ann;

        // Parameter pasien saat ini (bisa diperbarui dari dashboard)
        public double CurrentGlucose { get; set; } = 100;
        public double CurrentHeartRate { get; set; } = 78;
        public double PatientAge { get; set; } = 45;
        public double PatientBmi { get; set; } = 25;

        public ChatbotService()
        {
            _nlp = new NlpService();
            _fuzzy = new FuzzyLogicService();
            _ann = new AnnService();
        }

        // ============================================================
        // API UTAMA — Proses Pesan Pengguna
        // ============================================================
        public string ProcessMessage(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "💬 Silakan ketik pertanyaan Anda tentang glukosa darah atau diabetes.";

            // Langkah 1: Analisis NLP
            NlpResult nlpResult = _nlp.Analyze(userMessage);

            // Langkah 2: Cek apakah ada nilai glukosa yang disebutkan
            double glucoseToAnalyze = CurrentGlucose;
            if (nlpResult.Entities.ContainsKey("glucose_value"))
            {
                if (double.TryParse(nlpResult.Entities["glucose_value"], out double parsedGlucose))
                {
                    glucoseToAnalyze = parsedGlucose;
                }
            }

            // Langkah 3: Route ke handler berdasarkan intent
            string response = nlpResult.Intent switch
            {
                "salam" => HandleGreeting(),
                "tanya_glukosa" => HandleGlucoseQuery(glucoseToAnalyze),
                "penyebab_tinggi" => HandleHighGlucoseCauses(),
                "cara_menurunkan" => HandleLowerGlucose(),
                "penyebab_rendah" => HandleLowGlucoseCauses(),
                "informasi_diabetes" => HandleDiabetesInfo(),
                "rentang_normal" => HandleNormalRange(),
                "gejala" => HandleSymptoms(),
                "makanan" => HandleFoodAdvice(),
                "terima_kasih" => HandleThanks(),
                "bantuan" => HandleHelp(),
                _ => HandleUnknown(userMessage, glucoseToAnalyze)
            };

            return response;
        }

        // ============================================================
        // HANDLER: Salam / Greeting
        // ============================================================
        private string HandleGreeting()
        {
            return "👋 Halo! Saya GlucoBot — asisten informasi glukosa darah Anda.\n\n" +
                   "Saya bisa membantu Anda dengan:\n" +
                   "🩸 Informasi kadar glukosa darah\n" +
                   "📊 Analisis risiko diabetes (NLP + Fuzzy + ANN)\n" +
                   "💊 Penyebab & cara menurunkan gula darah\n" +
                   "🍎 Rekomendasi makanan & gaya hidup\n\n" +
                   "Coba tanyakan: \"Berapa kadar glukosa saya?\" atau \"Bagaimana cara menurunkan gula darah?\"";
        }

        // ============================================================
        // HANDLER: Query Glukosa — Menggunakan Fuzzy + ANN
        // ============================================================
        private string HandleGlucoseQuery(double glucose)
        {
            // Analisis Fuzzy Logic
            FuzzyResult fuzzyResult = _fuzzy.ClassifyGlucose(glucose);

            // Analisis ANN
            AnnResult annResult = _ann.Predict(glucose, CurrentHeartRate, PatientAge, PatientBmi);

            string response = $"🩸 **Analisis Kadar Glukosa: {glucose:F0} mg/dL**\n\n";

            // Hasil Fuzzy Logic
            response += "━━━ 🔬 Analisis Fuzzy Logic ━━━\n";
            response += $"📌 Kategori: {fuzzyResult.Category}\n";
            response += $"⚡ Skor Risiko: {fuzzyResult.RiskScore:F1}/100\n";
            response += $"🎯 Level Risiko: {fuzzyResult.RiskLevel}\n\n";

            // Derajat keanggotaan
            response += "📊 Derajat Keanggotaan:\n";
            foreach (var (cat, degree) in fuzzyResult.MembershipDegrees)
            {
                if (degree > 0.01)
                {
                    string bar = new string('█', (int)(degree * 10));
                    string empty = new string('░', 10 - (int)(degree * 10));
                    response += $"  {cat}: {bar}{empty} {degree:F2}\n";
                }
            }
            response += "\n";

            // Hasil ANN
            response += "━━━ 🧠 Analisis Neural Network ━━━\n";
            response += $"🎯 Prediksi: {annResult.PredictedClass}\n";
            response += $"📈 Confidence: {annResult.Confidence:F1}%\n";
            response += $"  Normal: {annResult.Probabilities[0] * 100:F1}%\n";
            response += $"  Prediabetes: {annResult.Probabilities[1] * 100:F1}%\n";
            response += $"  Diabetes: {annResult.Probabilities[2] * 100:F1}%\n\n";

            // Rekomendasi dari Fuzzy
            response += "━━━ 💡 Rekomendasi ━━━\n";
            response += fuzzyResult.Recommendation;

            return response;
        }

        // ============================================================
        // HANDLER: Penyebab Glukosa Tinggi
        // ============================================================
        private string HandleHighGlucoseCauses()
        {
            return "📋 **Penyebab Kadar Glukosa Darah Tinggi (Hiperglikemia)**\n\n" +
                   "🔴 **Penyebab Utama:**\n" +
                   "1. **Pola Makan Tidak Sehat** — Konsumsi berlebihan gula, karbohidrat olahan, makanan cepat saji\n" +
                   "2. **Kurang Aktivitas Fisik** — Gaya hidup sedentari/kurang gerak\n" +
                   "3. **Resistensi Insulin** — Sel tubuh tidak merespons insulin dengan baik\n" +
                   "4. **Produksi Insulin Kurang** — Pankreas tidak memproduksi cukup insulin\n\n" +
                   "🟡 **Penyebab Lainnya:**\n" +
                   "5. **Stres** — Hormon kortisol meningkatkan gula darah\n" +
                   "6. **Kurang Tidur** — Gangguan metabolisme glukosa\n" +
                   "7. **Dehidrasi** — Konsentrasi gula dalam darah meningkat\n" +
                   "8. **Efek Samping Obat** — Steroid, diuretik, beta-blocker\n" +
                   "9. **Infeksi/Sakit** — Tubuh melepaskan hormon stres\n" +
                   "10. **Dawn Phenomenon** — Lonjakan gula darah di pagi hari\n\n" +
                   "🔬 **Faktor Risiko:**\n" +
                   "• Riwayat keluarga diabetes\n" +
                   "• Obesitas (BMI > 30)\n" +
                   "• Usia > 45 tahun\n" +
                   "• Sindrom ovarium polikistik (PCOS)\n" +
                   "• Riwayat diabetes gestasional";
        }

        // ============================================================
        // HANDLER: Cara Menurunkan Glukosa
        // ============================================================
        private string HandleLowerGlucose()
        {
            return "💪 **Cara Menurunkan & Mengatur Kadar Glukosa Darah**\n\n" +
                   "🥗 **1. Pola Makan (Diet)**\n" +
                   "• Kurangi gula dan karbohidrat olahan (nasi putih, roti putih)\n" +
                   "• Ganti dengan karbohidrat kompleks (nasi merah, oatmeal, quinoa)\n" +
                   "• Perbanyak serat (sayuran hijau, kacang-kacangan, biji-bijian)\n" +
                   "• Konsumsi protein tanpa lemak (ikan, ayam tanpa kulit, tahu)\n" +
                   "• Makan dalam porsi kecil tapi sering (5-6x sehari)\n" +
                   "• Hindari minuman manis dan jus buah kemasan\n\n" +
                   "🏃 **2. Olahraga & Aktivitas Fisik**\n" +
                   "• Jalan cepat 30 menit/hari, minimal 5x seminggu\n" +
                   "• Latihan kekuatan 2-3x seminggu\n" +
                   "• Yoga dan meditasi untuk mengurangi stres\n" +
                   "• Hindari duduk terlalu lama (berdiri setiap 30 menit)\n\n" +
                   "💊 **3. Medis (Konsultasi Dokter)**\n" +
                   "• Metformin — Obat lini pertama untuk diabetes tipe 2\n" +
                   "• Insulin — Jika diperlukan (terutama diabetes tipe 1)\n" +
                   "• Monitor gula darah rutin dengan glucometer\n" +
                   "• Tes HbA1c setiap 3 bulan\n\n" +
                   "🌙 **4. Gaya Hidup**\n" +
                   "• Tidur cukup 7-8 jam per malam\n" +
                   "• Kelola stres dengan relaksasi\n" +
                   "• Minum air putih minimal 8 gelas/hari\n" +
                   "• Berhenti merokok dan batasi alkohol\n" +
                   "• Jaga berat badan ideal (BMI 18.5-24.9)";
        }

        // ============================================================
        // HANDLER: Penyebab Glukosa Rendah
        // ============================================================
        private string HandleLowGlucoseCauses()
        {
            return "📉 **Penyebab Kadar Glukosa Darah Rendah (Hipoglikemia)**\n" +
                   "Hipoglikemia = Gula darah < 70 mg/dL\n\n" +
                   "🔴 **Penyebab Utama:**\n" +
                   "1. **Dosis insulin berlebihan** — Terlalu banyak insulin disuntikkan\n" +
                   "2. **Melewatkan makan** — Tidak makan pada waktu yang seharusnya\n" +
                   "3. **Olahraga berlebihan** — Tanpa asupan karbohidrat yang cukup\n" +
                   "4. **Konsumsi alkohol berlebihan** — Mengganggu produksi glukosa hati\n\n" +
                   "🟡 **Penyebab Lainnya:**\n" +
                   "5. Efek samping obat diabetes tertentu (sulfonilurea)\n" +
                   "6. Gangguan kelenjar adrenal atau hipofisis\n" +
                   "7. Penyakit hati atau ginjal\n" +
                   "8. Tumor pankreas (insulinoma — jarang)\n\n" +
                   "⚠️ **Gejala Hipoglikemia:**\n" +
                   "• Gemetar, keringat dingin\n" +
                   "• Pusing, penglihatan kabur\n" +
                   "• Jantung berdebar, cemas\n" +
                   "• Kebingungan, sulit konsentrasi\n" +
                   "• Kasus berat: kejang, pingsan\n\n" +
                   "🆘 **Pertolongan Pertama:**\n" +
                   "Konsumsi 15-20g karbohidrat cepat (jus buah, 3-4 tablet glukosa), " +
                   "tunggu 15 menit, cek ulang. Jika masih rendah, ulangi.";
        }

        // ============================================================
        // HANDLER: Informasi Diabetes
        // ============================================================
        private string HandleDiabetesInfo()
        {
            return "📚 **Informasi Lengkap Tentang Diabetes Mellitus**\n\n" +
                   "Diabetes adalah kondisi kronis di mana tubuh tidak dapat mengatur kadar gula darah dengan baik.\n\n" +
                   "🔵 **Diabetes Tipe 1 (5-10% kasus)**\n" +
                   "• Autoimun — Sistem imun menyerang sel beta pankreas\n" +
                   "• Biasanya muncul sejak anak-anak atau remaja\n" +
                   "• HARUS menggunakan insulin seumur hidup\n\n" +
                   "🟢 **Diabetes Tipe 2 (90-95% kasus)**\n" +
                   "• Resistensi insulin — Sel tubuh tidak merespons insulin\n" +
                   "• Sangat dipengaruhi gaya hidup (diet, olahraga, berat badan)\n" +
                   "• Bisa dicegah dan dikelola dengan perubahan gaya hidup\n\n" +
                   "🟡 **Diabetes Gestasional**\n" +
                   "• Terjadi selama kehamilan\n" +
                   "• Biasanya hilang setelah melahirkan\n" +
                   "• Meningkatkan risiko diabetes tipe 2 di kemudian hari\n\n" +
                   "📊 **Kriteria Diagnosis (WHO/ADA):**\n" +
                   "• Normal: Gula darah puasa < 100 mg/dL\n" +
                   "• Prediabetes: 100-125 mg/dL\n" +
                   "• Diabetes: ≥ 126 mg/dL\n" +
                   "• HbA1c Normal: < 5.7%\n" +
                   "• HbA1c Diabetes: ≥ 6.5%";
        }

        // ============================================================
        // HANDLER: Rentang Normal
        // ============================================================
        private string HandleNormalRange()
        {
            return "📏 **Rentang Kadar Glukosa Darah Normal**\n\n" +
                   "🟢 **Gula Darah Puasa (GDP):**\n" +
                   "• Normal: 70 - 100 mg/dL\n" +
                   "• Prediabetes: 100 - 125 mg/dL\n" +
                   "• Diabetes: ≥ 126 mg/dL\n\n" +
                   "🔵 **2 Jam Setelah Makan (GD2PP):**\n" +
                   "• Normal: < 140 mg/dL\n" +
                   "• Prediabetes: 140 - 199 mg/dL\n" +
                   "• Diabetes: ≥ 200 mg/dL\n\n" +
                   "🟣 **HbA1c (Rata-rata 3 bulan):**\n" +
                   "• Normal: < 5.7%\n" +
                   "• Prediabetes: 5.7% - 6.4%\n" +
                   "• Diabetes: ≥ 6.5%\n\n" +
                   "⚠️ **Batas Bahaya:**\n" +
                   "• Hipoglikemia: < 70 mg/dL (terlalu rendah)\n" +
                   "• Hiperglikemia Kritis: > 250 mg/dL (darurat)\n" +
                   "• Ketoasidosis: > 300 mg/dL (mengancam nyawa)\n\n" +
                   $"📌 Kadar glukosa Anda saat ini: **{CurrentGlucose:F0} mg/dL**";
        }

        // ============================================================
        // HANDLER: Gejala Diabetes
        // ============================================================
        private string HandleSymptoms()
        {
            return "🏥 **Gejala dan Tanda-Tanda Diabetes**\n\n" +
                   "⚡ **Gejala Umum:**\n" +
                   "• Sering buang air kecil (poliuria)\n" +
                   "• Rasa haus berlebihan (polidipsia)\n" +
                   "• Lapar terus-menerus (polifagia)\n" +
                   "• Penurunan berat badan tanpa sebab\n" +
                   "• Kelelahan dan lemah\n" +
                   "• Penglihatan kabur\n\n" +
                   "🔴 **Gejala Lanjutan:**\n" +
                   "• Luka sulit sembuh\n" +
                   "• Infeksi berulang (kulit, gusi, saluran kemih)\n" +
                   "• Kesemutan atau mati rasa di tangan/kaki\n" +
                   "• Kulit kering dan gatal\n" +
                   "• Area kulit menghitam (acanthosis nigricans)\n\n" +
                   "⚠️ Jika Anda mengalami gejala-gejala ini, segera periksakan ke dokter!";
        }

        // ============================================================
        // HANDLER: Saran Makanan
        // ============================================================
        private string HandleFoodAdvice()
        {
            return "🍎 **Panduan Makanan untuk Mengatur Gula Darah**\n\n" +
                   "✅ **DIREKOMENDASIKAN:**\n" +
                   "• 🥦 Sayuran hijau (bayam, brokoli, kangkung)\n" +
                   "• 🐟 Ikan berlemak (salmon, sarden — omega-3)\n" +
                   "• 🥑 Alpukat, kacang almond, kenari\n" +
                   "• 🫘 Kacang-kacangan (kacang merah, kedelai)\n" +
                   "• 🍚 Nasi merah, oatmeal, quinoa\n" +
                   "• 🫐 Buah rendah gula (berry, apel, jeruk)\n" +
                   "• 🥚 Telur, tahu, tempe\n" +
                   "• 🧄 Bawang putih, kayu manis, kunyit\n\n" +
                   "❌ **HINDARI/BATASI:**\n" +
                   "• 🍰 Gula dan permen\n" +
                   "• 🥤 Minuman manis dan soda\n" +
                   "• 🍞 Roti putih, nasi putih berlebihan\n" +
                   "• 🍟 Makanan goreng-gorengan\n" +
                   "• 🥫 Makanan olahan dan kemasan\n" +
                   "• 🍌 Buah tinggi gula berlebihan (mangga, durian)\n\n" +
                   "💡 **Tips:** Gunakan metode piring diabetes — " +
                   "½ piring sayuran, ¼ protein, ¼ karbohidrat kompleks.";
        }

        // ============================================================
        // HANDLER: Terima Kasih
        // ============================================================
        private string HandleThanks()
        {
            var responses = new List<string>
            {
                "😊 Sama-sama! Jangan ragu bertanya lagi tentang kesehatan glukosa Anda.",
                "🙏 Terima kasih kembali! Semoga informasinya bermanfaat. Jaga kesehatan Anda!",
                "💚 Dengan senang hati! Ingat untuk rutin memantau gula darah Anda ya."
            };
            return responses[new Random().Next(responses.Count)];
        }

        // ============================================================
        // HANDLER: Bantuan
        // ============================================================
        private string HandleHelp()
        {
            return "🤖 **GlucoBot — Panduan Penggunaan**\n\n" +
                   "Saya adalah chatbot informasi glukosa darah yang didukung oleh:\n" +
                   "🔹 **NLP** — Memahami pertanyaan Anda\n" +
                   "🔹 **Fuzzy Logic** — Mengklasifikasikan kadar glukosa\n" +
                   "🔹 **Neural Network** — Memprediksi risiko diabetes\n\n" +
                   "📝 **Contoh Pertanyaan:**\n" +
                   "• \"Berapa kadar glukosa saya?\"\n" +
                   "• \"Apa penyebab gula darah tinggi?\"\n" +
                   "• \"Bagaimana cara menurunkan gula darah?\"\n" +
                   "• \"Apa saja gejala diabetes?\"\n" +
                   "• \"Makanan apa yang baik untuk diabetes?\"\n" +
                   "• \"Berapa nilai normal gula darah?\"\n" +
                   "• \"Analisis glukosa 150 mg/dL\"\n" +
                   "• \"Apa itu diabetes?\"\n\n" +
                   "💡 Anda juga bisa menyebutkan angka glukosa spesifik, misal: \"analisis 200\"";
        }

        // ============================================================
        // HANDLER: Intent Tidak Dikenal — Fallback dengan analisis
        // ============================================================
        private string HandleUnknown(string userMessage, double glucose)
        {
            // Jika ada angka, coba analisis sebagai nilai glukosa
            if (double.TryParse(userMessage.Trim(), out double directValue))
            {
                return HandleGlucoseQuery(directValue);
            }

            // Cek apakah ada angka di dalam teks
            var match = System.Text.RegularExpressions.Regex.Match(userMessage, @"\b(\d{2,3})\b");
            if (match.Success && double.TryParse(match.Value, out double extractedValue))
            {
                if (extractedValue >= 30 && extractedValue <= 500)
                {
                    return HandleGlucoseQuery(extractedValue);
                }
            }

            return "🤔 Maaf, saya kurang memahami pertanyaan Anda.\n\n" +
                   "Coba tanyakan hal-hal berikut:\n" +
                   "• \"Berapa kadar glukosa saya?\"\n" +
                   "• \"Penyebab gula darah tinggi\"\n" +
                   "• \"Cara menurunkan gula darah\"\n" +
                   "• \"Info diabetes\"\n" +
                   "• Atau ketik angka glukosa langsung (misal: \"150\")\n\n" +
                   "Ketik \"bantuan\" untuk melihat semua fitur saya.";
        }
    }
}
