using Microsoft.Maui.Controls;
using RetroOrganizzer.Helper;
using System.Security.Cryptography;

namespace RetroOrganizzer
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            //string key = Cryptography.GenerateKey();
            //string key = Cryptography.key();
            //string crip = Cryptography.EncryptString("", key);
            //string decrip = Cryptography.DecryptString(crip, key);
        }

        private string selectedRoute;

        public string SelectedRoute
        {
            get { return selectedRoute; }
            set
            {
                selectedRoute = value;
                OnPropertyChanged();
            }
        }

        async void OnMenuItemChanged(System.Object sender, CheckedChangedEventArgs e)
        {
            var radioButton = (RadioButton)sender;
            selectedRoute = (string)radioButton.Value;

            if (!String.IsNullOrEmpty(selectedRoute))
                await Shell.Current.GoToAsync($"//{selectedRoute}");
        }

        private void OnLogoutTapped(object sender, EventArgs e)
        {
            // Feche o aplicativo
            System.Environment.Exit(0);
        }

    }
}