using Microsoft.Maui.Media;
using System.IO;

namespace Alprogcitra1
{
    public partial class RegisterPage : ContentPage
    {
        private byte[] _faceData;

        public RegisterPage()
        {
            InitializeComponent();
            SetupRolePicker();
        }

        private void SetupRolePicker()
        {
            bool isSuperAdminTaken = Preferences.Get("has_super_admin", false);
            PickerRole.Items.Clear();
            PickerRole.Items.Add("Operator Biasa");
            if (!isSuperAdminTaken) PickerRole.Items.Add("Super Admin (Pemilik Sistem)");
        }

        private async void OnCaptureFaceClicked(object sender, EventArgs e)
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                using var stream = await photo.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                _faceData = memoryStream.ToArray();
                CameraPreview.Source = ImageSource.FromStream(() => new MemoryStream(_faceData));
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string user = EntryUsername.Text?.Trim();
            string pass = EntryPassword.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || PickerRole.SelectedIndex == -1 || _faceData == null)
            {
                await DisplayAlert("Gagal", "Semua data (termasuk foto wajah) wajib diisi!", "OK"); return;
            }

            string selectedRole = PickerRole.SelectedItem.ToString();
            Preferences.Set($"user_pass_{user}", pass);
            Preferences.Set($"user_role_{user}", selectedRole);
            if (selectedRole == "Super Admin (Pemilik Sistem)") Preferences.Set("has_super_admin", true);

            string imagePath = Path.Combine(FileSystem.AppDataDirectory, $"{user}_face.png");
            File.WriteAllBytes(imagePath, _faceData);

            await DisplayAlert("Sukses", $"User '{user}' berhasil didaftarkan!", "OK");
            Application.Current.MainPage = new MainPage();
        }

        private void OnBackToLoginClicked(object sender, EventArgs e) => Application.Current.MainPage = new MainPage();
    }
}