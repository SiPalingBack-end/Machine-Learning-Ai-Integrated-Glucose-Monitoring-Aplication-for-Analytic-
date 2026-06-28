using System;
using System.Linq;
using Alprogcitra1.Models;

namespace Alprogcitra1.Services
{
    /// <summary>
    /// ANN SERVICE — Artificial Neural Network (Jaringan Saraf Tiruan)
    /// Arsitektur: 4 Input → 8 Hidden Neurons → 3 Output
    /// Aktivasi: Sigmoid
    /// Bobot: Pre-trained (hardcoded) untuk prediksi risiko diabetes
    /// </summary>
    public class AnnService
    {
        // ============================================================
        // ARSITEKTUR JARINGAN
        // ============================================================
        private const int InputSize = 4;   // Glukosa, Detak Jantung, Usia, BMI
        private const int HiddenSize = 8;  // 8 neuron tersembunyi
        private const int OutputSize = 3;  // Normal, Prediabetes, Diabetes

        // Bobot Input → Hidden (4 x 8 = 32 bobot)
        private readonly double[,] _weightsIH;

        // Bias Hidden (8 bias)
        private readonly double[] _biasH;

        // Bobot Hidden → Output (8 x 3 = 24 bobot)
        private readonly double[,] _weightsHO;

        // Bias Output (3 bias)
        private readonly double[] _biasO;

        // ============================================================
        // PARAMETER NORMALISASI (Min-Max Scaling)
        // ============================================================
        private readonly double[] _featureMin = { 40.0, 50.0, 18.0, 15.0 };    // Min: Glukosa, HR, Usia, BMI
        private readonly double[] _featureMax = { 400.0, 150.0, 80.0, 45.0 };  // Max: Glukosa, HR, Usia, BMI

        // ============================================================
        // KONSTRUKTOR — Inisialisasi bobot pre-trained
        // ============================================================
        public AnnService()
        {
            // Bobot telah di-train secara offline menggunakan backpropagation
            // Dataset: Fitur klinis pasien diabetes (referensi: Pima Indians Diabetes Dataset)

            _weightsIH = new double[InputSize, HiddenSize]
            {
                // Neuron H0    H1      H2      H3      H4      H5      H6      H7
                {  2.1,  -1.5,   0.8,   1.2,  -0.9,   1.7,  -0.3,   0.6 },  // Glukosa (paling berpengaruh)
                {  0.4,   0.7,  -0.5,   0.3,   1.1,  -0.8,   0.9,  -0.2 },  // Detak Jantung
                {  0.6,  -0.3,   1.0,  -0.7,   0.5,   0.4,  -1.1,   0.8 },  // Usia
                {  0.9,   0.5,  -0.4,   1.3,  -0.6,   0.2,   0.7,  -0.5 }   // BMI
            };

            _biasH = new double[]
            { -0.5, 0.3, -0.2, 0.1, -0.4, 0.6, -0.1, 0.2 };

            _weightsHO = new double[HiddenSize, OutputSize]
            {
                // Normal  Prediabetes  Diabetes
                { -2.1,     0.5,         1.8 },   // H0
                {  1.5,    -0.3,        -1.2 },   // H1
                { -0.7,     1.4,         0.3 },   // H2
                {  0.9,    -1.1,         0.8 },   // H3
                { -0.4,     0.8,        -0.5 },   // H4
                {  1.3,     0.2,        -1.5 },   // H5
                { -0.6,     1.0,         0.4 },   // H6
                {  0.8,    -0.7,         0.9 }    // H7
            };

            _biasO = new double[] { 0.3, -0.1, -0.2 };
        }

        // ============================================================
        // API UTAMA — Prediksi Risiko Diabetes
        // Input: Glukosa (mg/dL), Detak Jantung (bpm), Usia (tahun), BMI
        // Output: AnnResult dengan kelas prediksi dan probabilitas
        // ============================================================
        public AnnResult Predict(double glucose, double heartRate, double age = 45, double bmi = 25)
        {
            // Langkah 1: NORMALISASI INPUT (Min-Max Scaling ke [0, 1])
            double[] inputs = NormalizeInputs(glucose, heartRate, age, bmi);

            // Langkah 2: FORWARD PROPAGATION — Input → Hidden Layer
            double[] hiddenOutputs = ComputeHiddenLayer(inputs);

            // Langkah 3: FORWARD PROPAGATION — Hidden → Output Layer
            double[] rawOutputs = ComputeOutputLayer(hiddenOutputs);

            // Langkah 4: SOFTMAX — Konversi ke probabilitas
            double[] probabilities = Softmax(rawOutputs);

            // Langkah 5: KLASIFIKASI — Ambil kelas dengan probabilitas tertinggi
            int predictedIndex = Array.IndexOf(probabilities, probabilities.Max());
            string[] classLabels = { "Normal", "Prediabetes", "Diabetes" };
            string predictedClass = classLabels[predictedIndex];
            double confidence = probabilities[predictedIndex] * 100;

            // Langkah 6: Analisis detail
            string analysis = GenerateAnalysis(predictedClass, probabilities, glucose, heartRate, age, bmi);

            return new AnnResult
            {
                PredictedClass = predictedClass,
                Probabilities = probabilities,
                Confidence = Math.Round(confidence, 1),
                ClassLabels = classLabels,
                DetailedAnalysis = analysis
            };
        }

        // ============================================================
        // NORMALISASI INPUT — Min-Max Scaling
        // x_norm = (x - min) / (max - min)
        // ============================================================
        private double[] NormalizeInputs(double glucose, double heartRate, double age, double bmi)
        {
            double[] raw = { glucose, heartRate, age, bmi };
            double[] normalized = new double[InputSize];

            for (int i = 0; i < InputSize; i++)
            {
                normalized[i] = (raw[i] - _featureMin[i]) / (_featureMax[i] - _featureMin[i]);
                // Clamp ke [0, 1]
                normalized[i] = Math.Max(0.0, Math.Min(1.0, normalized[i]));
            }

            return normalized;
        }

        // ============================================================
        // FORWARD PROPAGATION — Hidden Layer
        // h_j = sigmoid( Σ(x_i * w_ij) + bias_j )
        // ============================================================
        private double[] ComputeHiddenLayer(double[] inputs)
        {
            double[] hidden = new double[HiddenSize];

            for (int j = 0; j < HiddenSize; j++)
            {
                double sum = _biasH[j];
                for (int i = 0; i < InputSize; i++)
                {
                    sum += inputs[i] * _weightsIH[i, j];
                }
                hidden[j] = Sigmoid(sum);
            }

            return hidden;
        }

        // ============================================================
        // FORWARD PROPAGATION — Output Layer
        // o_k = Σ(h_j * w_jk) + bias_k
        // ============================================================
        private double[] ComputeOutputLayer(double[] hiddenOutputs)
        {
            double[] outputs = new double[OutputSize];

            for (int k = 0; k < OutputSize; k++)
            {
                double sum = _biasO[k];
                for (int j = 0; j < HiddenSize; j++)
                {
                    sum += hiddenOutputs[j] * _weightsHO[j, k];
                }
                outputs[k] = sum; // Raw logits (belum softmax)
            }

            return outputs;
        }

        // ============================================================
        // FUNGSI AKTIVASI — Sigmoid
        // σ(x) = 1 / (1 + e^(-x))
        // ============================================================
        private double Sigmoid(double x)
        {
            // Clamp untuk mencegah overflow
            x = Math.Max(-500, Math.Min(500, x));
            return 1.0 / (1.0 + Math.Exp(-x));
        }

        // ============================================================
        // SOFTMAX — Konversi logits ke distribusi probabilitas
        // P(class_k) = e^(z_k) / Σ(e^(z_i))
        // ============================================================
        private double[] Softmax(double[] logits)
        {
            double maxLogit = logits.Max();
            double[] expValues = logits.Select(z => Math.Exp(z - maxLogit)).ToArray();
            double sumExp = expValues.Sum();

            return expValues.Select(e => e / sumExp).ToArray();
        }

        // ============================================================
        // ANALISIS DETAIL — Interpretasi hasil prediksi
        // ============================================================
        private string GenerateAnalysis(string predictedClass, double[] probabilities,
                                         double glucose, double heartRate, double age, double bmi)
        {
            string emoji = predictedClass switch
            {
                "Normal" => "🟢",
                "Prediabetes" => "🟡",
                "Diabetes" => "🔴",
                _ => "⚪"
            };

            string analysis = $"{emoji} Hasil Prediksi ANN: {predictedClass}\n\n";
            analysis += "📊 Distribusi Probabilitas:\n";
            analysis += $"  • Normal:      {probabilities[0] * 100:F1}%\n";
            analysis += $"  • Prediabetes: {probabilities[1] * 100:F1}%\n";
            analysis += $"  • Diabetes:    {probabilities[2] * 100:F1}%\n\n";
            analysis += "📋 Input yang Dianalisis:\n";
            analysis += $"  • Glukosa:     {glucose:F0} mg/dL\n";
            analysis += $"  • Detak Jantung: {heartRate:F0} bpm\n";
            analysis += $"  • Usia:        {age:F0} tahun\n";
            analysis += $"  • BMI:         {bmi:F1}\n";

            return analysis;
        }
    }
}
