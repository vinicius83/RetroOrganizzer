using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using Microsoft.Extensions.Configuration;
using RetroOrganizzer.Models;
using RetroOrganizzer.Services;
using System.Text.Json;
using System.Xml;

namespace RetroOrganizzer.Pages;

public partial class XMLEditor : ContentPage
{
    string xmlFilePath;

    public XMLEditor()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        string rootFolder = Preferences.Default.Get("rootFolder", "");

        if (!string.IsNullOrEmpty(rootFolder))
        {
            await ListSystemsByFolder(rootFolder);
        }
        else
        {
            await DisplayAlert("Game Folder Select", $"Select your game folder in menu settings!", "OK");
        }
    }

    private async Task ListSystemsByFolder(string rootFolder)
    {
        try
        {
            loadingIndicator.IsRunning = true;
            loadingIndicator.IsVisible = true;

            CancellationTokenSource cancellationTokenSource = new();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            List<SystemInfo> lstSystemInfo = new();

            foreach (var folder in Directory.EnumerateDirectories(rootFolder))
            {
                SystemInfo systemInfo = new()
                {
                    Folder = folder,
                    System = SystemByFolder(Path.GetFileName(folder))
                };

                if (File.Exists(Path.Combine(folder, "gamelist.xml")))
                {
                    lstSystemInfo.Add(systemInfo);
                }
            }

            listSystems.ItemsSource = lstSystemInfo.OrderBy(x => x.System);

            Preferences.Default.Set("rootFolder", rootFolder);

            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
        catch (Exception ex)
        {
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private async void System_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;

        //Clean listGames
        listGames.ItemsSource = null;

        if (e.SelectedItem != null)
        {
            CleanInputs();

            SystemInfo selectedSystem = e.SelectedItem as SystemInfo;
            string selectedFolder = selectedSystem.Folder;

            await GetSystemData(selectedSystem);

            string xmlFilePath = Path.Combine(selectedFolder, "gamelist.xml");
            string xmlDirectory = Path.GetDirectoryName(xmlFilePath);

            if (File.Exists(xmlFilePath))
            {
                // Load configuration from XML file
                var config = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddXmlFile(xmlFilePath)
                 .Build();

                // Extract game names from configuration
                var gameSections = config.GetSection("game").GetChildren();

                // List to show in ListView
                List<GameInfo> gamesInfo = new();

                foreach (var gameSection in gameSections)
                {
                    string path = gameSection["path"];
                    string name = gameSection["name"];
                    string id = gameSection["id"];

                    if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(name))
                    {
                        GameInfo game = new() { Path = path, Name = name, GameID = id };

                        string gamePath = path[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                        gamePath = Path.Combine(xmlDirectory, gamePath);

                        if (File.Exists(gamePath))
                        {
                            game.IsGameNotFound = false;
                        }
                        else
                        {
                            game.IsGameNotFound = true;
                        }

                        gamesInfo.Add(game);
                    }
                }

                //Show option to clean games
                if (gamesInfo.Any(x => x.IsGameNotFound == true))
                {
                    StackCleanGames.IsVisible = true;
                }
                else
                {
                    StackCleanGames.IsVisible = false;
                }

                listGames.ItemsSource = gamesInfo.OrderBy(x => x.Name);
                LabelSelectedSystem.Text = $"{selectedSystem.System}";

                ShowStackLayoutGames();
            }
            else
            {
                await DisplayAlert("System Select", $"The selected system doesn't have a gamelist.xml file.", "OK");
            }
        }

        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;
    }

    private async Task GetSystemData(SystemInfo selectedSystem)
    {
        string jsonSystems = Preferences.Default.Get("jsonSystems", "");
        string selectedPlatform = Preferences.Get("platform", "");

        string json = File.ReadAllText(jsonSystems);

        List<Dictionary<string, object>> configSystems = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);

        string idSistema = "";
        foreach (var system in configSystems)
        {
            JsonElement noms = (JsonElement)system["noms"];

            if (noms.TryGetProperty("nom_" + selectedPlatform, out JsonElement nom_recalbox) && nom_recalbox.GetString() == Path.GetFileName(selectedSystem.Folder))
            {
                idSistema = system["id"].ToString();
                break;
            }
        }

        string appDirectory = AppContext.BaseDirectory;
        string filesDirectory = Path.Combine(appDirectory, "images");

        //Create the directory if it doesn't exist
        if (!Directory.Exists(filesDirectory))
        {
            Directory.CreateDirectory(filesDirectory);
        }

        await GetIcon(idSistema, filesDirectory);

        await GetLogo(idSistema, filesDirectory);

    }

    private async Task GetIcon(string idSistema, string filesDirectory)
    {
        string iconPath = Path.Combine(filesDirectory, idSistema + "_icon.png");

        //Check if images idSistema + "_icon.png" exists 
        if (File.Exists(iconPath))
        {
            IconSystem.Source = ImageSource.FromFile(iconPath);
        }
        else
        {
            // Download Image from screenScraper
            var service = new ScreenScraperService();
            byte[] imageIcon = await service.GetSystemImageAsync(idSistema, "icon");

            // Save the image to the local storage
            await File.WriteAllBytesAsync(Path.Combine(filesDirectory, idSistema + "_icon" + ".png"), imageIcon);

            if (imageIcon != null)
            {
                IconSystem.Source = ImageSource.FromStream(() => new MemoryStream(imageIcon));
            }
        }
    }

    private async Task GetLogo(string idSistema, string filesDirectory)
    {
        string logoPath = Path.Combine(filesDirectory, idSistema + "_logo-monochrome(wor).png");

        //Check if images idSistema + "_logo-monochrome(wor).png" exists 
        if (File.Exists(logoPath))
        {
            LogoSystem.Source = ImageSource.FromFile(logoPath);
        }
        else
        {
            // Download Image from screenScraper
            var service = new ScreenScraperService();
            byte[] imageLogo = await service.GetSystemImageAsync(idSistema, "logo-monochrome(wor)");

            // Save the image to the local storage
            await File.WriteAllBytesAsync(Path.Combine(filesDirectory, idSistema + "_logo-monochrome(wor)" + ".png"), imageLogo);

            if (imageLogo != null)
            {
                LogoSystem.Source = ImageSource.FromStream(() => new MemoryStream(imageLogo));
            }
        }
    }

    private async void ListFolders(object sender, EventArgs e)
    {
        try
        {
            loadingIndicator.IsRunning = true;
            loadingIndicator.IsVisible = true;

            var result = await FilePicker.PickAsync(PickOptions.Default);

            if (result != null)
            {
                xmlFilePath = result.FullPath;

                if (Path.GetExtension(xmlFilePath) == ".xml")
                {
                    // Carregar o arquivo XML
                    XmlDocument xmlDoc = new();
                    xmlDoc.Load(xmlFilePath);

                    string xmlDirectory = Path.GetDirectoryName(xmlFilePath);

                    // Extrair os nomes dos jogos do XML
                    XmlNodeList gameNodes = xmlDoc.SelectNodes("/gameList/game");

                    List<GameInfo> gamesInfo = new();

                    foreach (XmlNode gameNode in gameNodes)
                    {
                        string path = gameNode.SelectSingleNode("path")?.InnerText;
                        string name = gameNode.SelectSingleNode("name")?.InnerText;
                        string id = gameNode.Attributes["id"]?.Value;

                        if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(name))
                        {
                            GameInfo game = new() { Path = path, Name = name, GameID = id };
                            //GameInfo game = new GameInfo { Path = path, Name = name, GameID = id };



                            string gamePath = path[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                            gamePath = Path.Combine(xmlDirectory, gamePath);
                            game.IsGameNotFound = !File.Exists(gamePath);

                            gamesInfo.Add(game);
                        }

                    }

                    // Exibir os nomes dos jogos em uma lista (ListView)
                    listGames.ItemsSource = gamesInfo.OrderBy(x => x.Name);

                    // Adicione o manipulador de eventos para o evento ItemSelected
                    listGames.ItemSelected += Games_ItemSelected;

                    //LabelSelectedFolder.Text = $"XML carregado com sucesso: {xmlFilePath}";
                }
                else
                {
                    //LabelSelectedFolder.Text = "The selected file isn't a XML.";
                }
            }
            else
            {
                //LabelSelectedFolder.Text = "Select a XML file!.";
            }

            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
        catch (Exception ex)
        {
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private void Games_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem == null)
            return; // Item was deselected

        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;


        //Se listSystems estiver selecionado
        if (listSystems.SelectedItem != null)
        {
            SystemInfo systemInfo = listSystems.SelectedItem as SystemInfo;
            string selectedFolder = systemInfo.Folder;
            xmlFilePath = Path.Combine(selectedFolder, "gamelist.xml");
        }
        else
        {
            DisplayAlert("Game Select", $"Select a system first!.", "OK");
        }


        if (e.SelectedItem != null && !string.IsNullOrEmpty(xmlFilePath))
        {
            CleanInputs();
            GameInfo selectedGame = e.SelectedItem as GameInfo;
            string selectedGamePath = selectedGame.Path;

            string xpathQuery = $"//game[path=\"{selectedGamePath}\"]"; ;

            XmlDocument xmlDoc = new();
            xmlDoc.Load(xmlFilePath);

            XmlNode gameNode = xmlDoc.SelectSingleNode(xpathQuery);

            if (gameNode != null)
            {
                string xmlDirectory = Path.GetDirectoryName(xmlFilePath);

                string gameID = gameNode.Attributes["id"]?.Value;
                string path = gameNode.SelectSingleNode("path")?.InnerText;
                string name = gameNode.SelectSingleNode("name")?.InnerText;
                string desc = gameNode.SelectSingleNode("desc")?.InnerText;
                string releasedate = gameNode.SelectSingleNode("releasedate")?.InnerText;
                string developerValue = gameNode.SelectSingleNode("developer")?.InnerText;
                string publisherValue = gameNode.SelectSingleNode("publisher")?.InnerText;
                string genreValue = gameNode.SelectSingleNode("genre")?.InnerText;
                string playersValue = gameNode.SelectSingleNode("players")?.InnerText;
                string langValue = gameNode.SelectSingleNode("lang")?.InnerText;

                GameIDEntry.Text = gameID;
                PathEntry.Text = path;
                NameEntry.Text = name;
                DescEditor.Text = desc;
                if (!string.IsNullOrEmpty(releasedate))
                {
                    ReleaseDateEntry.Text = DateTime.ParseExact(releasedate, "yyyyMMddTHHmmss", null).ToString("dd/MM/yyyy");
                }
                DeveloperEntry.Text = developerValue;
                PublisherEntry.Text = publisherValue;
                GenreEntry.Text = genreValue;
                PlayersEntry.Text = playersValue;
                LangEntry.Text = langValue;

                string imageValue = gameNode.SelectSingleNode("image")?.InnerText;
                string marqueeValue = gameNode.SelectSingleNode("marquee")?.InnerText;
                string videoValue = gameNode.SelectSingleNode("video")?.InnerText;

                ImageEntry.Text = imageValue;
                MarqueeEntry.Text = marqueeValue;
                VideoEntry.Text = videoValue;

                if (imageValue != null)
                {
                    imageValue = imageValue[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                    imageValue = Path.Combine(xmlDirectory, imageValue);
                    if (File.Exists(imageValue))
                    {
                        ImageDisplay.Source = ImageSource.FromFile(imageValue);
                    }
                }

                if (marqueeValue != null)
                {
                    marqueeValue = marqueeValue[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                    marqueeValue = Path.Combine(xmlDirectory, marqueeValue);
                    if (File.Exists(marqueeValue))
                    {
                        MarqueeDisplay.Source = ImageSource.FromFile(marqueeValue);
                    }
                }

                if (videoValue != null)
                {
                    videoValue = videoValue[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                    videoValue = Path.Combine(xmlDirectory, videoValue);
                    if (File.Exists(videoValue))
                    {
                        VideoDisplay.Source = MediaSource.FromFile(videoValue);
                        VideoDisplay.Stop();
                        VideoDisplay.Play();
                    }
                }

                if (selectedGame.IsGameNotFound)
                {
                    DisplayAlert("Game not found", $"The game {selectedGame.Path} was not found!.", "OK");
                }
            }
        }

        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;
    }

    private void ButtonShowSystems_Clicked(object sender, EventArgs e)
    {
        ShowStackLayoutSystems();
    }

    private void ButtonCleanXML_Clicked(object sender, EventArgs e)
    {
        try
        {
            loadingIndicator.IsRunning = true;
            loadingIndicator.IsVisible = true;

            List<GameInfo> jogosSemPath = listGames.ItemsSource.Cast<GameInfo>().Where(x => x.IsGameNotFound == true).ToList();
            List<GameInfo> jogosComPath = listGames.ItemsSource.Cast<GameInfo>().Where(x => x.IsGameNotFound == false).ToList();

            XmlDocument xmlDoc = new();
            xmlDoc.Load(xmlFilePath);

            //Busca os jogos que não possuem mais paths
            foreach (GameInfo game in jogosSemPath)
            {
                string xpathQuery = $"//game[path=\"{game.Path}\"]";
                XmlNode gameNode = xmlDoc.SelectSingleNode(xpathQuery);

                //Deleta os jogos que não possuem mais paths
                gameNode?.ParentNode.RemoveChild(gameNode);

                if (CheckBoxCleanMediaFiles.IsChecked)
                {
                    //Consulta por outros nós com o mesmo id exceto o que está sendo deletado (removido acima)
                    List<XmlNode> MesmoJogoOutrosNodes = xmlDoc.SelectNodes($"//game[@id=\"{game.GameID}\"]")
                                                           .Cast<XmlNode>()
                                                           .ToList();

                    if (MesmoJogoOutrosNodes.Count > 0)
                    {
                        //Filtro a lista MesmoJogoOutrosNodes com a lista jogosComPath (que tenham path válidos)
                        List<XmlNode> nodesToRemoveWithMatchingName = MesmoJogoOutrosNodes
                                                            .Where(x => jogosComPath.Any(y => y.GameID == x.SelectSingleNode("id")?.InnerText))
                                                            .ToList();

                        if (nodesToRemoveWithMatchingName.Count > 0)
                        {
                            //Midia referente ao jogo deletado
                            string imageValue = gameNode.SelectSingleNode("image")?.InnerText;
                            string marqueeValue = gameNode.SelectSingleNode("marquee")?.InnerText;
                            string videoValue = gameNode.SelectSingleNode("video")?.InnerText;

                            if (!nodesToRemoveWithMatchingName.Any(node => node.SelectSingleNode("image")?.InnerText == imageValue))
                            {
                                if (imageValue != null)
                                {
                                    imageValue = imageValue[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                                    imageValue = Path.Combine(Path.GetDirectoryName(xmlFilePath), imageValue);
                                    if (File.Exists(imageValue))
                                    {
                                        File.Delete(imageValue);
                                    }
                                }
                            }

                            if (!nodesToRemoveWithMatchingName.Any(node => node.SelectSingleNode("marquee")?.InnerText == marqueeValue))
                            {
                                if (marqueeValue != null)
                                {
                                    marqueeValue = marqueeValue[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                                    marqueeValue = Path.Combine(Path.GetDirectoryName(xmlFilePath), marqueeValue);
                                    if (File.Exists(marqueeValue))
                                    {
                                        File.Delete(marqueeValue);
                                    }
                                }
                            }

                            if (!nodesToRemoveWithMatchingName.Any(node => node.SelectSingleNode("video")?.InnerText == videoValue))
                            {
                                if (videoValue != null)
                                {
                                    videoValue = videoValue[2..].Replace("/", Path.DirectorySeparatorChar.ToString());
                                    videoValue = Path.Combine(Path.GetDirectoryName(xmlFilePath), videoValue);
                                    if (File.Exists(videoValue))
                                    {
                                        File.Delete(videoValue);
                                    }
                                }
                            }
                        }
                    }
                }

                listGames.ItemsSource = listGames.ItemsSource.Cast<GameInfo>().Where(x => x.Path != game.Path).ToList();

                //Salva o arquivo XML
                xmlDoc.Save(xmlFilePath);
            }

            //Limpa os campos
            CleanInputs();

            StackCleanGames.IsVisible = false;
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;

            //Exibe mensagem de sucesso
            DisplayAlert("XML Limpo", "The xml file was successfully cleaned.", "OK");
        }
        catch (Exception ex)
        {
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
            DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private void CleanInputs()
    {
        GameIDEntry.Text = "";
        PathEntry.Text = "";
        NameEntry.Text = "";
        DescEditor.Text = "";
        ReleaseDateEntry.Text = "";
        DeveloperEntry.Text = "";
        PublisherEntry.Text = "";
        GenreEntry.Text = "";
        PlayersEntry.Text = "";
        LangEntry.Text = "";
        ImageEntry.Text = "";
        MarqueeEntry.Text = "";

        ImageDisplay.Source = null;
        MarqueeDisplay.Source = null;
        VideoDisplay.Source = null;
    }


    private async void ShowStackLayoutGames()
    {
        CleanInputs();

        GridSystemSelected.IsVisible = true;

        await StackLayoutSystems.TranslateTo(0, -StackLayoutSystems.Height, 300); // Move para cima
        StackLayoutSystems.IsVisible = false;

        StackLayoutGames.IsVisible = true;
        await StackLayoutGames.TranslateTo(0, 0, 300); // Move para baixo
    }

    private async void ShowStackLayoutSystems()
    {
        CleanInputs();

        GridSystemSelected.IsVisible = false;

        await StackLayoutGames.TranslateTo(0, -StackLayoutGames.Height, 300); // Move para cima
        StackLayoutGames.IsVisible = false;

        StackLayoutSystems.IsVisible = true;
        await StackLayoutSystems.TranslateTo(0, 0, 300); // Move para baixo
    }

    public class GameInfo
    {
        public string GameID { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsGameNotFound { get; set; }
    }

    static string SystemByFolder(string folder)
    {
        folder = folder.ToLower();

        return folder switch
        {
            "wswan" => "Wonderswan",
            "wswanc" => "Wonderswan Color",
            "x1" => "X1",
            "x68000" => "Sharp X68000",
            "xash3d_fwgs" => "Xash3D (FWGS)",
            "zx81" => "ZX81",
            "zxspectrum" => "ZX Spectrum",
            "3do" => "3DO",
            "amiga500" => "Amiga 500",
            "amiga1200" => "Amiga 1200",
            "amigacd32" => "Amiga CD32",
            "amigacdtv" => "Amiga CDTV",
            "amstradcpc" => "Amstrad CPC",
            "anbernic" => "Anbernic",
            "arcade" => "Arcade",
            "atari800" => "Atari 800",
            "atari2600" => "Atari 2600",
            "atari5200" => "Atari 5200",
            "atari7800" => "Atari 7800",
            "atarist" => "Atari ST",
            "atomiswave" => "Atomiswave",
            "c20" => "Commodore 20",
            "c64" => "Commodore 64",
            "c128" => "Commodore 128",
            "cannonball" => "Cannonball",
            "cavestory" => "Cave Story",
            "cgenius" => "Commander Genius",
            "channelf" => "Channel F",
            "colecovision" => "Colecovision",
            "cplus4" => "Commodore Plus/4",
            "cps1" => "CPS-1",
            "cps2" => "CPS-2",
            "cps3" => "CPS-3",
            "daphne" => "Daphne",
            "dcim" => "DCIM",
            "devilutionx" => "DevilutionX",
            "dos" => "DOS",
            "dreamcast" => "Dreamcast",
            "easyrpg" => "EasyRPG",
            "fbneo" => "FinalBurn Neo",
            "fds" => "Famicom Disk System",
            "gameandwatch" => "Game & Watch",
            "gamegear" => "Game Gear",
            "gb" => "Game Boy",
            "gb2players" => "Game Boy (2 Players)",
            "gba" => "Game Boy Advance",
            "gbc" => "Game Boy Color",
            "gbc2players" => "Game Boy Color (2 Players)",
            "gc" => "GameCube",
            "gx4000" => "Amstrad GX4000",
            "hbmame" => "HBMAME",
            "intellivision" => "Intellivision",
            "lightgun" => "Lightgun Games",
            "lutro" => "Lutro",
            "lynx" => "Atari Lynx",
            "mame" => "MAME",
            "mastersystem" => "Sega Master System",
            "megadrive" => "Sega Mega Drive",
            "mrboom" => "Mr. Boom",
            "msx1" => "MSX",
            "msx2" => "MSX2",
            "msx2+" => "MSX2+",
            "msxturbor" => "MSX Turbo R",
            "n64" => "Nintendo 64",
            "n64dd" => "Nintendo 64DD",
            "naomi" => "Naomi",
            "nds" => "Nintendo DS",
            "neogeo" => "Neo Geo",
            "neogeocd" => "Neo Geo CD",
            "nes" => "Nintendo Entertainment System",
            "ngp" => "Neo Geo Pocket",
            "ngpc" => "Neo Geo Pocket Color",
            "o2em" => "Odyssey 2 (O2EM)",
            "openbor" => "OpenBOR",
            "pc88" => "NEC PC-8801",
            "pc98" => "NEC PC-9801",
            "pcengine" => "PC Engine",
            "pcenginecd" => "PC Engine CD",
            "pcfx" => "PC-FX",
            "pet" => "Commodore PET",
            "pico8" => "PICO-8",
            "pokemini" => "Pokemini",
            "ports" => "Ports",
            "prboom" => "PrBoom",
            "ps2" => "PlayStation 2",
            "psp" => "PlayStation Portable",
            "psx" => "PlayStation",
            "pygame" => "Pygame",
            "satellaview" => "Satellaview",
            "saturn" => "Sega Saturn",
            "scummvm" => "ScummVM",
            "sdlpop" => "SDL Pop",
            "sega32x" => "Sega 32X",
            "segacd" => "Sega CD",
            "sg1000" => "SG-1000",
            "snes" => "Super Nintendo",
            "snes-msu1" => "Super Nintendo (MSU-1)",
            "solarus" => "Solarus",
            "sufami" => "Super Famicom",
            "supergrafx" => "PC Engine SuperGrafx",
            "thomson" => "Thomson",
            "tic80" => "TIC-80",
            "tyrquake" => "Tyrquake",
            "varcade" => "Virtual Arcade",
            "vectrex" => "Vectrex",
            "virtualboy" => "Virtual Boy",
            _ => "Unknow System",
        };
    }
}
