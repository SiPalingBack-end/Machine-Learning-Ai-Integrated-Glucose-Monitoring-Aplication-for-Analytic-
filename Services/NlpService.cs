using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Alprogcitra1.Models;

namespace Alprogcitra1.Services
{
    /// <summary>
    /// NLP SERVICE — Natural Language Processing untuk Chatbot Glukosa
    /// Melakukan: Tokenisasi, Ekstraksi Intent, Ekstraksi Entitas, Pencocokan Fuzzy Keyword
    /// </summary>
    public class NlpService
    {
        // ============================================================
        // DATABASE INTENT — Pola kata kunci untuk setiap intent
        // ============================================================
        private static readonly Dictionary<string, List<string>> IntentPatterns = new()
        {
            ["salam"] = new List<string>
            {
                "halo", "hai", "hi", "hello", "hey", "selamat pagi", "selamat siang",
                "selamat sore", "selamat malam", "assalamualaikum", "permisi", "hei"
            },
            ["tanya_glukosa"] = new List<string>
            {
                "glukosa", "gula darah", "kadar gula", "berapa glukosa", "level glukosa",
                "blood sugar", "glucose", "kadar glukosa", "gula", "berapa gula",
                "cek gula", "cek glukosa", "angka gula", "angka glukosa", "nilai glukosa",
                "persentase glukosa", "kadar", "level", "tingkat glukosa", "tingkat gula"
            },
            ["penyebab_tinggi"] = new List<string>
            {
                "penyebab tinggi", "kenapa tinggi", "mengapa tinggi", "apa penyebab",
                "penyebab naik", "kenapa naik", "gula tinggi kenapa", "gula naik",
                "apa yang menyebabkan", "penyebab gula tinggi", "penyebab glukosa tinggi",
                "kenapa gula darah tinggi", "faktor", "penyebab diabetes", "penyebab",
                "kenapa bisa tinggi", "apa sebab", "sebab tinggi", "pemicu"
            },
            ["cara_menurunkan"] = new List<string>
            {
                "menurunkan", "turunkan", "cara turunkan", "cara menurunkan",
                "bagaimana menurunkan", "gimana menurunkan", "tips menurunkan",
                "cara mengurangi", "mengurangi gula", "kurangi gula", "obat",
                "solusi", "cara mengobati", "cara mengatasi", "mengatasi", "terapi",
                "pengobatan", "cara menyembuhkan", "diet", "olahraga", "cara",
                "apa yang harus dilakukan", "bagaimana cara", "gimana cara", "regulasi",
                "mengatur", "stabilkan", "menstabilkan", "normalkan"
            },
            ["penyebab_rendah"] = new List<string>
            {
                "gula rendah", "glukosa rendah", "hipoglikemia", "terlalu rendah",
                "kenapa rendah", "penyebab rendah", "gula turun", "gula drop",
                "kadar rendah", "di bawah normal", "kurang gula", "kekurangan gula"
            },
            ["informasi_diabetes"] = new List<string>
            {
                "diabetes", "kencing manis", "apa itu diabetes", "tipe diabetes",
                "jenis diabetes", "diabetes mellitus", "dm", "info diabetes",
                "informasi diabetes", "tentang diabetes", "penjelasan diabetes",
                "diabetes tipe 1", "diabetes tipe 2", "insulin", "prediabetes"
            },
            ["rentang_normal"] = new List<string>
            {
                "normal", "berapa normal", "nilai normal", "rentang normal",
                "batas normal", "kadar normal", "gula normal", "glukosa normal",
                "standar", "sehat", "aman", "ideal", "batas aman"
            },
            ["gejala"] = new List<string>
            {
                "gejala", "tanda", "ciri", "symptom", "gejala diabetes",
                "tanda diabetes", "ciri diabetes", "indikasi", "keluhan",
                "apa gejalanya", "gimana gejalanya", "gejala gula tinggi"
            },
            ["makanan"] = new List<string>
            {
                "makanan", "makan", "diet", "nutrisi", "pantangan", "boleh makan",
                "tidak boleh makan", "makanan sehat", "menu", "sayur", "buah",
                "karbohidrat", "protein", "serat", "makanan diabetes", "makanan gula"
            },
            ["terima_kasih"] = new List<string>
            {
                "terima kasih", "makasih", "thanks", "thank you", "trims",
                "terimakasih", "thx", "tq"
            },
            ["bantuan"] = new List<string>
            {
                "bantuan", "help", "tolong", "bisa apa", "apa yang bisa",
                "fitur", "fungsi", "kemampuan", "menu", "perintah", "command"
            }
        };

        // ============================================================
        // API UTAMA — Analisis Teks Input Pengguna
        // ============================================================
        public NlpResult Analyze(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return new NlpResult
                {
                    Intent = "unknown",
                    Confidence = 0.0,
                    OriginalText = userInput ?? ""
                };
            }

            string normalized = NormalizeText(userInput);
            List<string> tokens = Tokenize(normalized);
            var entities = ExtractEntities(userInput);
            var (intent, confidence) = ClassifyIntent(normalized, tokens);

            return new NlpResult
            {
                Intent = intent,
                Confidence = confidence,
                Entities = entities,
                Keywords = tokens,
                OriginalText = userInput
            };
        }

        // ============================================================
        // NORMALISASI TEKS — Lowercase, hapus tanda baca
        // ============================================================
        private string NormalizeText(string text)
        {
            text = text.ToLowerInvariant().Trim();
            // Pertahankan angka dan spasi, hapus tanda baca
            text = Regex.Replace(text, @"[^\w\s]", " ");
            text = Regex.Replace(text, @"\s+", " ");
            return text;
        }

        // ============================================================
        // TOKENISASI — Pecah teks menjadi kata-kata
        // ============================================================
        private List<string> Tokenize(string text)
        {
            // Hapus stopword Bahasa Indonesia
            var stopwords = new HashSet<string>
            {
                "yang", "di", "ke", "dari", "dan", "atau", "ini", "itu",
                "untuk", "pada", "dengan", "adalah", "saya", "aku", "kamu",
                "dia", "mereka", "kita", "bisa", "akan", "sudah", "belum",
                "juga", "lagi", "masih", "ya", "tidak", "bukan", "ada",
                "sangat", "sekali", "apakah", "gimana", "bagaimana", "dong",
                "sih", "nih", "deh", "lah", "kan", "tuh", "mau"
            };

            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => !stopwords.Contains(t) && t.Length > 1)
                       .ToList();
        }

        // ============================================================
        // KLASIFIKASI INTENT — Cocokkan input dengan pola intent
        // Menggunakan Levenshtein Distance untuk fuzzy matching
        // ============================================================
        private (string intent, double confidence) ClassifyIntent(string normalizedText, List<string> tokens)
        {
            string bestIntent = "unknown";
            double bestScore = 0.0;

            foreach (var (intent, patterns) in IntentPatterns)
            {
                double intentScore = 0.0;
                int matchCount = 0;

                foreach (var pattern in patterns)
                {
                    // 1. Exact substring match (skor tinggi)
                    if (normalizedText.Contains(pattern))
                    {
                        double exactScore = (double)pattern.Length / normalizedText.Length;
                        intentScore += Math.Max(0.7, exactScore);
                        matchCount++;
                        continue;
                    }

                    // 2. Token-level fuzzy matching menggunakan Levenshtein
                    foreach (var token in tokens)
                    {
                        double similarity = CalculateSimilarity(token, pattern);
                        if (similarity > 0.7) // Threshold 70% kemiripan
                        {
                            intentScore += similarity * 0.5;
                            matchCount++;
                            break;
                        }
                    }

                    // 3. Bigram matching (dua kata berurutan)
                    var patternWords = pattern.Split(' ');
                    if (patternWords.Length > 1)
                    {
                        for (int i = 0; i < tokens.Count - 1; i++)
                        {
                            string bigram = tokens[i] + " " + tokens[i + 1];
                            double similarity = CalculateSimilarity(bigram, pattern);
                            if (similarity > 0.65)
                            {
                                intentScore += similarity * 0.6;
                                matchCount++;
                                break;
                            }
                        }
                    }
                }

                // Normalisasi skor berdasarkan jumlah match
                if (matchCount > 0)
                {
                    double normalizedScore = Math.Min(1.0, intentScore / Math.Max(1, matchCount) * Math.Min(matchCount, 3));
                    if (normalizedScore > bestScore)
                    {
                        bestScore = normalizedScore;
                        bestIntent = intent;
                    }
                }
            }

            // Clamp confidence antara 0 dan 1
            bestScore = Math.Min(1.0, Math.Max(0.0, bestScore));

            return (bestIntent, bestScore);
        }

        // ============================================================
        // EKSTRAKSI ENTITAS — Ambil angka dan kata kunci penting
        // ============================================================
        private Dictionary<string, string> ExtractEntities(string text)
        {
            var entities = new Dictionary<string, string>();

            // Ekstrak angka (kemungkinan nilai glukosa)
            var numberMatches = Regex.Matches(text, @"\b(\d{2,3})\b");
            if (numberMatches.Count > 0)
            {
                entities["glucose_value"] = numberMatches[0].Value;
            }

            // Ekstrak satuan
            if (text.Contains("mg/dl", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("mg/dL", StringComparison.OrdinalIgnoreCase))
            {
                entities["unit"] = "mg/dL";
            }

            // Ekstrak waktu pengukuran
            if (text.Contains("puasa", StringComparison.OrdinalIgnoreCase))
                entities["measurement_type"] = "puasa";
            else if (text.Contains("setelah makan", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("sesudah makan", StringComparison.OrdinalIgnoreCase))
                entities["measurement_type"] = "postprandial";

            return entities;
        }

        // ============================================================
        // LEVENSHTEIN DISTANCE — Untuk fuzzy string matching
        // ============================================================
        private double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return 0.0;

            int sourceLength = source.Length;
            int targetLength = target.Length;

            var matrix = new int[sourceLength + 1, targetLength + 1];

            for (int i = 0; i <= sourceLength; i++) matrix[i, 0] = i;
            for (int j = 0; j <= targetLength; j++) matrix[0, j] = j;

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost
                    );
                }
            }

            int distance = matrix[sourceLength, targetLength];
            int maxLength = Math.Max(sourceLength, targetLength);
            return 1.0 - (double)distance / maxLength;
        }
    }
}
