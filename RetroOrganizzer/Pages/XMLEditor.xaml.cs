using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using System.Xml;

namespace RetroOrganizzer.Pages;

public partial class XMLEditor : ContentPage
{
    private static readonly int REQUEST_DIRECTORY = 1;
    //List<string> nomesDeJogos = new List<string>();
    //List<string> jogosNaoEncontrados = new List<string>();
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
            await ChooseFolder(rootFolder);
        }
        else
        {
            await DisplayAlert("Game Folder Select", $"Select your game folder in menu settings!", "OK");
        }
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

            List<SystemInfo> lstSystemInfo = new List<SystemInfo>();

            foreach (var folder in Directory.EnumerateDirectories(rootFolder))
            {
                SystemInfo systemInfo = new SystemInfo();
                systemInfo.Folder = folder;
                systemInfo.System = SystemByFolder(Path.GetFileName(folder));

                if (File.Exists(Path.Combine(folder, "gamelist.xml")))
                {
                    lstSystemInfo.Add(systemInfo);
                }
            }

            listFolders.ItemsSource = lstSystemInfo.OrderBy(x => x.System);

            Preferences.Default.Set("rootFolder", rootFolder);

            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
        catch (Exception ex)
        {
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
    }

    private async void ButtonChooseFolder_Clicked(object sender, EventArgs e)
    {
        string rootFolder = Preferences.Default.Get("rootFolder", "");
        await ChooseFolder(rootFolder);
    }

    private void System_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;

        //Clean listGames
        listGames.ItemsSource = null;

        if (e.SelectedItem != null)
        {
            LimpaCampos();
            SystemInfo selectedSystem = e.SelectedItem as SystemInfo;
            string selectedFolder = selectedSystem.Folder;

            string xmlFilePath = Path.Combine(selectedFolder, "gamelist.xml");

            if (File.Exists(xmlFilePath))
            {
                // Carregar o arquivo XML
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                string xmlDirectory = Path.GetDirectoryName(xmlFilePath);

                // Extrair os nomes dos jogos do XML
                XmlNodeList gameNodes = xmlDoc.SelectNodes("/gameList/game");

                List<GameInfo> gamesInfo = new List<GameInfo>();

                foreach (XmlNode gameNode in gameNodes)
                {
                    //string id = gameNode.Attributes["id"]?.Value;
                    string path = gameNode.SelectSingleNode("path")?.InnerText;
                    string name = gameNode.SelectSingleNode("name")?.InnerText;
                    string id = gameNode.Attributes["id"]?.Value;

                    if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(name))
                    {
                        GameInfo game = new GameInfo { Path = path, Name = name, GameID = id };

                        string gamePath = path.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
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
                //LabelSelectedFolder.Text = "The selected folder doesn't have a gamelist.xml file.";
            }
        }

        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;

    }

    private async void ButtonChooseGame_Clicked(object sender, EventArgs e)
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
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(xmlFilePath);

                    string xmlDirectory = Path.GetDirectoryName(xmlFilePath);

                    // Extrair os nomes dos jogos do XML
                    XmlNodeList gameNodes = xmlDoc.SelectNodes("/gameList/game");

                    List<GameInfo> gamesInfo = new List<GameInfo>();

                    foreach (XmlNode gameNode in gameNodes)
                    {
                        string path = gameNode.SelectSingleNode("path")?.InnerText;
                        string name = gameNode.SelectSingleNode("name")?.InnerText;
                        string id = gameNode.Attributes["id"]?.Value;

                        if (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(name))
                        {
                            GameInfo game = new GameInfo { Path = path, Name = name, GameID = id };

                            string gamePath = path.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
                            gamePath = Path.Combine(xmlDirectory, gamePath);
                            game.IsGameNotFound = File.Exists(gamePath) ? false : true;

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
            // Lida com erros, se houver algum
            //LabelSelectedFolder.Text = $"Erro: {ex.Message}";
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
    }

    private void Games_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem == null)
            return; // Item was deselected

        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;


        //Se listFolders estiver selecionado
        if (listFolders.SelectedItem != null)
        {
            SystemInfo systemInfo = listFolders.SelectedItem as SystemInfo;
            string selectedFolder = systemInfo.Folder;
            xmlFilePath = Path.Combine(selectedFolder, "gamelist.xml");
        }
        else
        {
            DisplayAlert("Game Select", $"Select a system first!.", "OK");
        }


        if (e.SelectedItem != null && !string.IsNullOrEmpty(xmlFilePath))
        {
            LimpaCampos();
            GameInfo selectedGame = e.SelectedItem as GameInfo;
            string selectedGamePath = selectedGame.Path;

            string xpathQuery = $"//game[path=\"{selectedGamePath}\"]"; ;

            XmlDocument xmlDoc = new XmlDocument();
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
                    imageValue = imageValue.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
                    imageValue = Path.Combine(xmlDirectory, imageValue);
                    if (File.Exists(imageValue))
                    {
                        ImageDisplay.Source = ImageSource.FromFile(imageValue);
                    }
                }

                if (marqueeValue != null)
                {
                    marqueeValue = marqueeValue.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
                    marqueeValue = Path.Combine(xmlDirectory, marqueeValue);
                    if (File.Exists(marqueeValue))
                    {
                        MarqueeDisplay.Source = ImageSource.FromFile(marqueeValue);
                    }
                }

                if (videoValue != null)
                {
                    videoValue = videoValue.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
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
        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;

        List<GameInfo> jogosSemPath = listGames.ItemsSource.Cast<GameInfo>().Where(x => x.IsGameNotFound == true).ToList();
        List<GameInfo> jogosComPath = listGames.ItemsSource.Cast<GameInfo>().Where(x => x.IsGameNotFound == false).ToList();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(xmlFilePath);

        //Busca os jogos que não possuem mais paths
        foreach (GameInfo game in jogosSemPath)
        {
            string xpathQuery = $"//game[path=\"{game.Path}\"]";
            XmlNode gameNode = xmlDoc.SelectSingleNode(xpathQuery);

            //Deleta os jogos que não possuem mais paths
            if (gameNode != null)
            {
                gameNode.ParentNode.RemoveChild(gameNode);
            }

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
                                imageValue = imageValue.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
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
                                marqueeValue = marqueeValue.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
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
                                videoValue = videoValue.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
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
        LimpaCampos();

        StackCleanGames.IsVisible = false;
        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;

        //Exibe mensagem de sucesso
        DisplayAlert("XML Limpo", "The xml file was successfully cleaned.", "OK");
    }

    private void LimpaCampos()
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
        LimpaCampos();

        StackLayoutSystemSelected.IsVisible = true;

        await StackLayoutSystems.TranslateTo(0, -StackLayoutSystems.Height, 300); // Move para cima
        StackLayoutSystems.IsVisible = false;

        StackLayoutGames.IsVisible = true;
        await StackLayoutGames.TranslateTo(0, 0, 300); // Move para baixo
    }

    private async void ShowStackLayoutSystems()
    {
        LimpaCampos();

        StackLayoutSystemSelected.IsVisible = false;

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

    public class SystemInfo
    {
        public string Folder { get; set; }
        public string System { get; set; }
    }

    static string SystemByFolder(string folder)
    {
        folder = folder.ToLower();

        switch (folder)
        {
            case "wswan":
                return "Wonderswan";
            case "wswanc":
                return "Wonderswan Color";
            case "x1":
                return "X1";
            case "x68000":
                return "Sharp X68000";
            case "xash3d_fwgs":
                return "Xash3D (FWGS)";
            case "zx81":
                return "ZX81";
            case "zxspectrum":
                return "ZX Spectrum";
            case "3do":
                return "3DO";
            case "amiga500":
                return "Amiga 500";
            case "amiga1200":
                return "Amiga 1200";
            case "amigacd32":
                return "Amiga CD32";
            case "amigacdtv":
                return "Amiga CDTV";
            case "amstradcpc":
                return "Amstrad CPC";
            case "anbernic":
                return "Anbernic";
            case "arcade":
                return "Arcade";
            case "atari800":
                return "Atari 800";
            case "atari2600":
                return "Atari 2600";
            case "atari5200":
                return "Atari 5200";
            case "atari7800":
                return "Atari 7800";
            case "atarist":
                return "Atari ST";
            case "atomiswave":
                return "Atomiswave";
            case "c20":
                return "Commodore 20";
            case "c64":
                return "Commodore 64";
            case "c128":
                return "Commodore 128";
            case "cannonball":
                return "Cannonball";
            case "cavestory":
                return "Cave Story";
            case "cgenius":
                return "Commander Genius";
            case "channelf":
                return "Channel F";
            case "colecovision":
                return "Colecovision";
            case "cplus4":
                return "Commodore Plus/4";
            case "cps1":
                return "CPS-1";
            case "cps2":
                return "CPS-2";
            case "cps3":
                return "CPS-3";
            case "daphne":
                return "Daphne";
            case "dcim":
                return "DCIM";
            case "devilutionx":
                return "DevilutionX";
            case "dos":
                return "DOS";
            case "dreamcast":
                return "Dreamcast";
            case "easyrpg":
                return "EasyRPG";
            case "fbneo":
                return "FinalBurn Neo";
            case "fds":
                return "Famicom Disk System";
            case "gameandwatch":
                return "Game & Watch";
            case "gamegear":
                return "Game Gear";
            case "gb":
                return "Game Boy";
            case "gb2players":
                return "Game Boy (2 Players)";
            case "gba":
                return "Game Boy Advance";
            case "gbc":
                return "Game Boy Color";
            case "gbc2players":
                return "Game Boy Color (2 Players)";
            case "gc":
                return "GameCube";
            case "gx4000":
                return "Amstrad GX4000";
            case "hbmame":
                return "HBMAME";
            case "intellivision":
                return "Intellivision";
            case "lightgun":
                return "Lightgun Games";
            case "lutro":
                return "Lutro";
            case "lynx":
                return "Atari Lynx";
            case "mame":
                return "MAME";
            case "mastersystem":
                return "Sega Master System";
            case "megadrive":
                return "Sega Mega Drive";
            case "mrboom":
                return "Mr. Boom";
            case "msx1":
                return "MSX";
            case "msx2":
                return "MSX2";
            case "msx2+":
                return "MSX2+";
            case "msxturbor":
                return "MSX Turbo R";
            case "n64":
                return "Nintendo 64";
            case "n64dd":
                return "Nintendo 64DD";
            case "naomi":
                return "Naomi";
            case "nds":
                return "Nintendo DS";
            case "neogeo":
                return "Neo Geo";
            case "neogeocd":
                return "Neo Geo CD";
            case "nes":
                return "Nintendo Entertainment System";
            case "ngp":
                return "Neo Geo Pocket";
            case "ngpc":
                return "Neo Geo Pocket Color";
            case "o2em":
                return "Odyssey 2 (O2EM)";
            case "openbor":
                return "OpenBOR";
            case "pc88":
                return "NEC PC-8801";
            case "pc98":
                return "NEC PC-9801";
            case "pcengine":
                return "PC Engine";
            case "pcenginecd":
                return "PC Engine CD";
            case "pcfx":
                return "PC-FX";
            case "pet":
                return "Commodore PET";
            case "pico8":
                return "PICO-8";
            case "pokemini":
                return "Pokemini";
            case "ports":
                return "Ports";
            case "prboom":
                return "PrBoom";
            case "ps2":
                return "PlayStation 2";
            case "psp":
                return "PlayStation Portable";
            case "psx":
                return "PlayStation";
            case "pygame":
                return "Pygame";
            case "satellaview":
                return "Satellaview";
            case "saturn":
                return "Sega Saturn";
            case "scummvm":
                return "ScummVM";
            case "sdlpop":
                return "SDL Pop";
            case "sega32x":
                return "Sega 32X";
            case "segacd":
                return "Sega CD";
            case "sg1000":
                return "SG-1000";
            case "snes":
                return "Super Nintendo";
            case "snes-msu1":
                return "Super Nintendo (MSU-1)";
            case "solarus":
                return "Solarus";
            case "sufami":
                return "Super Famicom";
            case "supergrafx":
                return "PC Engine SuperGrafx";
            case "thomson":
                return "Thomson";
            case "tic80":
                return "TIC-80";
            case "tyrquake":
                return "Tyrquake";
            case "varcade":
                return "Virtual Arcade";
            case "vectrex":
                return "Vectrex";
            case "virtualboy":
                return "Virtual Boy";
            default:
                return "Unknow System";
        }
    }
}
