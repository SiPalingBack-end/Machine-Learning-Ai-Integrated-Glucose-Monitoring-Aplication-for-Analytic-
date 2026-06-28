using Microsoft.Maui.Media;
using SkiaSharp;
using System.IO;

namespace Alprogcitra1
{
    public partial class FaceVerificationPage : ContentPage
    {
        private string _username;
        private string _dbPhotoPath;

        // INI DIA FUNGSI YANG DICARI OLEH MAIN PAGE! ERRORNYA PASTI HILANG!
        public FaceVerificationPage(string username, string role)
        {
            InitializeComponent();
            _username = username;
            LabelRole.Text = $"User: {username} | Role: {role}";
            _dbPhotoPath = Path.Combine(FileSystem.AppDataDirectory, $"{username}_face.png");
        }

        private async void OnVerifyClicked(object sender, EventArgs e)
        {
            if (!File.Exists(_dbPhotoPath)) { await DisplayAlert("Error Database", "Foto wajah untuk user ini tidak ditemukan!", "OK"); return; }

            try
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null) return;
                CameraPreview.Source = ImageSource.FromStream(() => photo.OpenReadAsync().Result);

                using var newStream = await photo.OpenReadAsync();
                using var newSkStream = new SKManagedStream(newStream);
                using var newBitmap = SKBitmap.Decode(newSkStream);

                using var dbStream = File.OpenRead(_dbPhotoPath);
                using var dbSkStream = new SKManagedStream(dbStream);
                using var dbBitmap = SKBitmap.Decode(dbSkStream);

                var info = new SKImageInfo(16, 16);
                using var resizedNew = newBitmap.Resize(info, SKFilterQuality.Medium);
                using var resizedDb = dbBitmap.Resize(info, SKFilterQuality.Medium);

                int totalPixels = 256;
                int matchedPixels = 0;

                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        var c1 = resizedNew.GetPixel(x, y);
                        var c2 = resizedDb.GetPixel(x, y);

                        int gray1 = (int)(0.3 * c1.Red + 0.59 * c1.Green + 0.11 * c1.Blue);
                        int gray2 = (int)(0.3 * c2.Red + 0.59 * c2.Green + 0.11 * c2.Blue);

                        if (Math.Abs(gray1 - gray2) < 50) matchedPixels++;
                    }
                }

                double similarity = (matchedPixels / (double)totalPixels) * 100;

                if (similarity >= 50.0)
                {
                    BtnLanjut.IsVisible = true;
                    await DisplayAlert("AKSES DITERIMA", $"Identitas Valid. Kemiripan: {similarity:F1}%", "OK");
                }
                else
                {
                    BtnLanjut.IsVisible = false;
                    await DisplayAlert("AKSES DITOLAK", $"Wajah Tidak Cocok! Kemiripan hanya: {similarity:F1}%", "OK");
                }
            }
            catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        }

        private void OnNextClicked(object sender, EventArgs e) => Application.Current.MainPage = new MainDashboardPage();
        private void OnCancelClicked(object sender, EventArgs e) => Application.Current.MainPage = new MainPage();
    }
}