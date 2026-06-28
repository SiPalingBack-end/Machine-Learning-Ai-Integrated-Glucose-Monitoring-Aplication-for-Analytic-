using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Alprogcitra1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string user = EntryUsername.Text?.Trim();
            string pass = EntryPassword.Text;

            // Cek apakah username ada di database lokal
            if (!Preferences.ContainsKey($"user_pass_{user}"))
            {
                await DisplayAlert("Ditolak", "Username tidak ditemukan!", "OK");
                return;
            }

            // Cek apakah passwordnya cocok
            string savedPass = Preferences.Get($"user_pass_{user}", "");
            if (pass != savedPass)
            {
                await DisplayAlert("Ditolak", "Password salah!", "OK");
                return;
            }

            // Kalau benar, ambil role-nya dan pindah ke halaman Verifikasi Wajah
            string role = Preferences.Get($"user_role_{user}", "User");
            Preferences.Set("session_username", user);
            Preferences.Set("session_role", role);

            Application.Current.MainPage = new FaceVerificationPage(user, role);
        }

        // Fungsi untuk pindah ke halaman pendaftaran
        private void OnGoToRegisterClicked(object sender, EventArgs e)
        {
            Application.Current.MainPage = new RegisterPage();
        }
    }
}