using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using System.Xml;
using static RetroOrganizzer.Pages.XMLEditor;

namespace RetroOrganizzer.Pages;

public partial class Settings : ContentPage
{
	public Settings()
	{
		InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        string rootFolder = Preferences.Default.Get("rootFolder", "");

        if (!string.IsNullOrEmpty(rootFolder))
        {
            LabelSelectedFolder.Text = $"Root Folder: {rootFolder}";
            ButtonChooseFolder.Text = "Change root folder";
        }
        else
        {
            ButtonChooseFolder.Text = "Select your root game folder";
            LabelSelectedFolder.Text = "Select a folder!.";
        }
    }

    private async void ButtonGameRootFolder_Clicked(object sender, EventArgs e)
    {
        string rootFolder = Preferences.Default.Get("rootFolder", "");
        await ChooseFolder(rootFolder);
    }

    private async Task ChooseFolder(string rootFolder)
    {
        try
        {
            loadingIndicator.IsRunning = true;
            loadingIndicator.IsVisible = true;

            List<string> pastasComGamelist = new List<string>();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var result = await FolderPicker.PickAsync(cancellationToken);

            List<SystemInfo> lstSystemInfo = new List<SystemInfo>();

            if (result != null)
            {
                Preferences.Default.Set("rootFolder", rootFolder);
            }
            else
            {
                LabelSelectedFolder.Text = "Select a folder!.";
            }

            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
        catch (Exception ex)
        {
            LabelSelectedFolder.Text = $"Erro: {ex.Message}";
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
    }


}