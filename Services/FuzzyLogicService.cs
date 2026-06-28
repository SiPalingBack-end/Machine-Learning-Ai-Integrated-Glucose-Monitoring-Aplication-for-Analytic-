using System;
using System.Collections.Generic;
using Alprogcitra1.Models;

namespace Alprogcitra1.Services
{
    /// <summary>
    /// FUZZY LOGIC SERVICE — Sistem Inferensi Fuzzy Tsukamoto
    /// Mengklasifikasikan kadar glukosa darah menggunakan fungsi keanggotaan
    /// dan aturan fuzzy untuk menghasilkan skor risiko dan rekomendasi.
    /// </summary>
    public class FuzzyLogicService
    {
        // ============================================================
        // API UTAMA — Klasifikasi Kadar Glukosa
        // Input: Kadar glukosa darah dalam mg/dL
        // Output: FuzzyResult dengan kategori, derajat keanggotaan, skor risiko
        // ============================================================
        public FuzzyResult ClassifyGlucose(double glucoseLevel)
        {
            // Langkah 1: FUZZIFIKASI — Hitung derajat keanggotaan setiap kategori
            var memberships = CalculateMemberships(glucoseLevel);

            // Langkah 2: INFERENSI — Terapkan aturan fuzzy
            var ruleOutputs = ApplyFuzzyRules(memberships);

            // Langkah 3: DEFUZZIFIKASI — Hitung skor risiko akhir
            double riskScore = Defuzzify(ruleOutputs);

            // Langkah 4: Tentukan kategori dan rekomendasi
            string category = DetermineCategory(memberships);
            string riskLevel = DetermineRiskLevel(riskScore);
            string colorCode = GetColorCode(riskLevel);
            string recommendation = GenerateRecommendation(category, glucoseLevel, riskScore);

            return new FuzzyResult
            {
                Category = category,
                MembershipDegrees = memberships,
                RiskScore = Math.Round(riskScore, 2),
                Recommendation = recommendation,
                RiskLevel = riskLevel,
                ColorCode = colorCode
            };
        }

        // ============================================================
        // FUNGSI KEANGGOTAAN (MEMBERSHIP FUNCTIONS)
        // Menggunakan fungsi trapesium dan segitiga
        // ============================================================

        /// <summary>
        /// Hipoglikemia: Gula darah sangat rendah (< 70 mg/dL)
        /// Trapesium: [0, 0, 50, 70]
        /// </summary>
        private double MembershipHipoglikemia(double x)
        {
            if (x <= 50) return 1.0;
            if (x > 50 && x < 70) return (70 - x) / 20.0;
            return 0.0;
        }

        /// <summary>
        /// Normal: Gula darah dalam rentang sehat (70 - 100 mg/dL)
        /// Segitiga: [60, 85, 110]
        /// </summary>
        private double MembershipNormal(double x)
        {
            if (x <= 60 || x >= 110) return 0.0;
            if (x > 60 && x <= 85) return (x - 60) / 25.0;
            if (x > 85 && x < 110) return (110 - x) / 25.0;
            return 0.0;
        }

        /// <summary>
        /// Prediabetes: Gula darah mulai meninggi (100 - 126 mg/dL)
        /// Segitiga: [95, 113, 130]
        /// </summary>
        private double MembershipPrediabetes(double x)
        {
            if (x <= 95 || x >= 130) return 0.0;
            if (x > 95 && x <= 113) return (x - 95) / 18.0;
            if (x > 113 && x < 130) return (130 - x) / 17.0;
            return 0.0;
        }

        /// <summary>
        /// Diabetes: Gula darah tinggi (> 126 mg/dL)
        /// Segitiga: [120, 163, 200]
        /// </summary>
        private double MembershipDiabetes(double x)
        {
            if (x <= 120 || x >= 200) return 0.0;
            if (x > 120 && x <= 163) return (x - 120) / 43.0;
            if (x > 163 && x < 200) return (200 - x) / 37.0;
            return 0.0;
        }

        /// <summary>
        /// Hiperglikemia Kritis: Gula darah sangat tinggi (> 200 mg/dL)
        /// Trapesium: [180, 220, 400, 400]
        /// </summary>
        private double MembershipHiperglikemiaKritis(double x)
        {
            if (x <= 180) return 0.0;
            if (x > 180 && x < 220) return (x - 180) / 40.0;
            if (x >= 220) return 1.0;
            return 0.0;
        }

        // ============================================================
        // FUZZIFIKASI — Hitung semua derajat keanggotaan
        // ============================================================
        private Dictionary<string, double> CalculateMemberships(double glucose)
        {
            return new Dictionary<string, double>
            {
                ["Hipoglikemia"] = MembershipHipoglikemia(glucose),
                ["Normal"] = MembershipNormal(glucose),
                ["Prediabetes"] = MembershipPrediabetes(glucose),
                ["Diabetes"] = MembershipDiabetes(glucose),
                ["Hiperglikemia_Kritis"] = MembershipHiperglikemiaKritis(glucose)
            };
        }

        // ============================================================
        // ATURAN FUZZY (FUZZY RULES) — Inferensi Tsukamoto
        // Setiap aturan menghasilkan skor risiko berdasarkan derajat keanggotaan
        // ============================================================
        private List<(double alpha, double z)> ApplyFuzzyRules(Dictionary<string, double> memberships)
        {
            var rules = new List<(double alpha, double z)>();

            // Aturan 1: JIKA glukosa Hipoglikemia MAKA risiko = 75 (bahaya rendah gula)
            if (memberships["Hipoglikemia"] > 0)
                rules.Add((memberships["Hipoglikemia"], 75.0));

            // Aturan 2: JIKA glukosa Normal MAKA risiko = 10 (aman)
            if (memberships["Normal"] > 0)
                rules.Add((memberships["Normal"], 10.0));

            // Aturan 3: JIKA glukosa Prediabetes MAKA risiko = 50 (waspada)
            if (memberships["Prediabetes"] > 0)
                rules.Add((memberships["Prediabetes"], 50.0));

            // Aturan 4: JIKA glukosa Diabetes MAKA risiko = 80 (tinggi)
            if (memberships["Diabetes"] > 0)
                rules.Add((memberships["Diabetes"], 80.0));

            // Aturan 5: JIKA glukosa Hiperglikemia Kritis MAKA risiko = 100 (kritis)
            if (memberships["Hiperglikemia_Kritis"] > 0)
                rules.Add((memberships["Hiperglikemia_Kritis"], 100.0));

            return rules;
        }

        // ============================================================
        // DEFUZZIFIKASI — Metode Rata-rata Berbobot (Weighted Average)
        // z* = Σ(αi * zi) / Σ(αi)
        // ============================================================
        private double Defuzzify(List<(double alpha, double z)> rules)
        {
            if (rules.Count == 0) return 0.0;

            double numerator = 0.0;
            double denominator = 0.0;

            foreach (var (alpha, z) in rules)
            {
                numerator += alpha * z;
                denominator += alpha;
            }

            return denominator > 0 ? numerator / denominator : 0.0;
        }

        // ============================================================
        // PENENTUAN KATEGORI — Berdasarkan derajat keanggotaan tertinggi
        // ============================================================
        private string DetermineCategory(Dictionary<string, double> memberships)
        {
            string bestCategory = "Unknown";
            double bestDegree = -1;

            foreach (var (category, degree) in memberships)
            {
                if (degree > bestDegree)
                {
                    bestDegree = degree;
                    bestCategory = category;
                }
            }

            return bestCategory switch
            {
                "Hipoglikemia" => "Hipoglikemia (Gula Darah Rendah)",
                "Normal" => "Normal (Gula Darah Sehat)",
                "Prediabetes" => "Prediabetes (Waspada)",
                "Diabetes" => "Diabetes (Gula Darah Tinggi)",
                "Hiperglikemia_Kritis" => "Hiperglikemia Kritis (Darurat)",
                _ => "Tidak Terklasifikasi"
            };
        }

        // ============================================================
        // PENENTUAN LEVEL RISIKO
        // ============================================================
        private string DetermineRiskLevel(double riskScore)
        {
            if (riskScore < 20) return "Rendah";
            if (riskScore < 50) return "Sedang";
            if (riskScore < 80) return "Tinggi";
            return "Kritis";
        }

        private string GetColorCode(string riskLevel)
        {
            return riskLevel switch
            {
                "Rendah" => "#10B981",  // Hijau
                "Sedang" => "#F59E0B",  // Amber
                "Tinggi" => "#EF4444",  // Merah
                "Kritis" => "#DC2626",  // Merah Tua
                _ => "#64748B"          // Abu-abu
            };
        }

        // ============================================================
        // REKOMENDASI — Berdasarkan kategori dan skor risiko
        // ============================================================
        private string GenerateRecommendation(string category, double glucose, double riskScore)
        {
            if (category.Contains("Hipoglikemia"))
            {
                return "⚠️ PERINGATAN HIPOGLIKEMIA!\n" +
                       $"Kadar glukosa Anda ({glucose:F0} mg/dL) terlalu rendah.\n\n" +
                       "🔴 Tindakan Segera:\n" +
                       "• Konsumsi 15-20 gram karbohidrat cepat (jus buah, permen)\n" +
                       "• Tunggu 15 menit, lalu cek ulang gula darah\n" +
                       "• Jika masih < 70 mg/dL, ulangi langkah di atas\n" +
                       "• Hubungi dokter jika sering terjadi";
            }
            else if (category.Contains("Normal"))
            {
                return "✅ Kadar Glukosa Normal!\n" +
                       $"Kadar glukosa Anda ({glucose:F0} mg/dL) dalam rentang sehat.\n\n" +
                       "💚 Tips Menjaga:\n" +
                       "• Pertahankan pola makan seimbang\n" +
                       "• Olahraga teratur 30 menit/hari\n" +
                       "• Cukup tidur 7-8 jam\n" +
                       "• Rutin periksa kesehatan berkala";
            }
            else if (category.Contains("Prediabetes"))
            {
                return "⚡ Peringatan Prediabetes!\n" +
                       $"Kadar glukosa Anda ({glucose:F0} mg/dL) di atas normal.\n\n" +
                       "🟡 Rekomendasi:\n" +
                       "• Kurangi konsumsi gula dan karbohidrat olahan\n" +
                       "• Tingkatkan aktivitas fisik (150 menit/minggu)\n" +
                       "• Perbanyak serat (sayur, buah, whole grain)\n" +
                       "• Konsultasikan ke dokter untuk tes HbA1c\n" +
                       "• Pantau gula darah secara berkala";
            }
            else if (category.Contains("Diabetes"))
            {
                return "🔴 Peringatan Diabetes!\n" +
                       $"Kadar glukosa Anda ({glucose:F0} mg/dL) menunjukkan diabetes.\n\n" +
                       "🏥 Tindakan Diperlukan:\n" +
                       "• SEGERA konsultasi ke dokter/endokrinolog\n" +
                       "• Ikuti program diet diabetes yang direkomendasikan\n" +
                       "• Pantau gula darah secara rutin (minimal 2x/hari)\n" +
                       "• Olahraga teratur dengan intensitas sedang\n" +
                       "• Minum obat sesuai resep dokter jika diresepkan\n" +
                       "• Periksa HbA1c setiap 3 bulan";
            }
            else if (category.Contains("Kritis"))
            {
                return "🚨 DARURAT HIPERGLIKEMIA KRITIS!\n" +
                       $"Kadar glukosa Anda ({glucose:F0} mg/dL) SANGAT TINGGI!\n\n" +
                       "🆘 Tindakan Darurat:\n" +
                       "• SEGERA ke UGD/rumah sakit terdekat!\n" +
                       "• Minum banyak air putih\n" +
                       "• JANGAN olahraga berat\n" +
                       "• Bawa catatan obat dan riwayat medis\n" +
                       "• Kondisi ini bisa mengancam nyawa (Ketoasidosis Diabetik)";
            }

            return $"Kadar glukosa Anda: {glucose:F0} mg/dL. Skor risiko: {riskScore:F1}. " +
                   "Konsultasikan dengan dokter untuk evaluasi lebih lanjut.";
        }
    }
}
