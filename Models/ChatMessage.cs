using System;
using System.Collections.Generic;

namespace Alprogcitra1.Models
{
    // ============================================================
    // MODEL PESAN CHATBOT
    // ============================================================
    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public bool IsBot { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string MessageType { get; set; } = "info"; // info, warning, analysis, greeting
        public string BubbleColor => IsBot ? "#14B8A6" : "#6C3CE1";
        public string TextDisplayColor => "White";
        public string AlignmentSide => IsBot ? "Start" : "End";
        public string SenderLabel => IsBot ? "🤖 GlucoBot" : "👤 Anda";
    }

    // ============================================================
    // HASIL ANALISIS NLP
    // ============================================================
    public class NlpResult
    {
        public string Intent { get; set; } = "unknown";
        public double Confidence { get; set; } = 0.0;
        public Dictionary<string, string> Entities { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public string OriginalText { get; set; } = string.Empty;
    }

    // ============================================================
    // HASIL ANALISIS FUZZY LOGIC
    // ============================================================
    public class FuzzyResult
    {
        public string Category { get; set; } = "Unknown";
        public Dictionary<string, double> MembershipDegrees { get; set; } = new();
        public double RiskScore { get; set; } = 0.0;
        public string Recommendation { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Unknown"; // Rendah, Sedang, Tinggi, Kritis
        public string ColorCode { get; set; } = "#64748B";
    }

    // ============================================================
    // HASIL ANALISIS ANN (ARTIFICIAL NEURAL NETWORK)
    // ============================================================
    public class AnnResult
    {
        public string PredictedClass { get; set; } = "Unknown";
        public double[] Probabilities { get; set; } = new double[3];
        public double Confidence { get; set; } = 0.0;
        public string[] ClassLabels { get; set; } = { "Normal", "Prediabetes", "Diabetes" };
        public string DetailedAnalysis { get; set; } = string.Empty;
    }
}
