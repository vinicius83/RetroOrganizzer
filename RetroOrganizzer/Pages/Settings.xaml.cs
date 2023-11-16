using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RetroOrganizzer.Models;
using RetroOrganizzer.Services;
using static RetroOrganizzer.Pages.XMLEditor;

namespace RetroOrganizzer.Pages;

public partial class Settings : ContentPage
{
    public Settings()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
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

            List<string> pastasComGamelist = new();

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var result = await FolderPicker.PickAsync(cancellationToken);

            List<SystemInfo> lstSystemInfo = new();

            if (result != null)
            {
                Preferences.Default.Set("rootFolder", rootFolder);
                GetSystemData();
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

    private void GetSystemData()
    {
        var service = new ScreenScraperService();
        string SystemList = service.GetSystemList();
        JObject json = JObject.Parse(SystemList);

        //Keep only the "systemes" section in the config
        JToken jsonSystems = json["response"]["systemes"];

        foreach (JObject system in jsonSystems)
        {
            system.Remove("medias");
        }

        var appDirectory = AppContext.BaseDirectory;
        var filesDirectory = Path.Combine(appDirectory, "files");

        if (!Directory.Exists(filesDirectory))
        {
            Directory.CreateDirectory(filesDirectory);
        }

        string filePath = Path.Combine(filesDirectory, "systemList.json");
        File.WriteAllText(filePath, jsonSystems.ToString());

        Preferences.Default.Set("jsonSystems", filePath);
    }

}