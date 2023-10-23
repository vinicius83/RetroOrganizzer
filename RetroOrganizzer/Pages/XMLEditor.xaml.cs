using CommunityToolkit.Maui.Views;
using RetroOrganizzer.Converters;
using System.Xml;

namespace RetroOrganizzer.Pages;

public partial class XMLEditor : ContentPage
{
    private static readonly int REQUEST_DIRECTORY = 1;

    public XMLEditor()
    {
        InitializeComponent();
    }

    List<string> nomesDeJogos = new List<string>();
    List<string> jogosNaoEncontrados = new List<string>();
    string xmlFilePath;

    private async void EscolherXML_Clicked(object sender, EventArgs e)
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
                        //string id = gameNode.Attributes["id"]?.Value;
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
                    listaDeJogos.ItemsSource = gamesInfo.OrderBy(x => x.Name);

                    // Adicione o manipulador de eventos para o evento ItemSelected
                    listaDeJogos.ItemSelected += listaDeJogos_ItemSelected;

                    lblPastaSelecionada.Text = $"XML carregado com sucesso: {xmlFilePath}";
                }
                else
                {
                    lblPastaSelecionada.Text = "O arquivo selecionado não é um arquivo XML.";
                }
            }
            else
            {
                lblPastaSelecionada.Text = "Nenhum arquivo selecionado.";
            }

            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
        catch (Exception ex)
        {
            // Lida com erros, se houver algum
            lblPastaSelecionada.Text = $"Erro: {ex.Message}";
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
    }

    private void LimparXML_Clicked(object sender, EventArgs e)
    {
        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;

        List<GameInfo> jogosSemPath = listaDeJogos.ItemsSource.Cast<GameInfo>().Where(x => x.IsGameNotFound == true).ToList();
        List<GameInfo> jogosComPath = listaDeJogos.ItemsSource.Cast<GameInfo>().Where(x => x.IsGameNotFound == false).ToList();

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

            if (LimparArquivosCheckBox.IsChecked)
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

            listaDeJogos.ItemsSource = listaDeJogos.ItemsSource.Cast<GameInfo>().Where(x => x.Path != game.Path).ToList();

            //Salva o arquivo XML
            xmlDoc.Save(xmlFilePath);
        }

        //Limpa os campos
        LimpaCampos();

        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;

        //Exibe mensagem de sucesso
        DisplayAlert("XML Limpo", "O arquivo XML foi limpo com sucesso.", "OK");
    }

    private void listaDeJogos_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        loadingIndicator.IsRunning = true;
        loadingIndicator.IsVisible = true;

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
                ReleaseDateEntry.Text = DateTime.ParseExact(releasedate, "yyyyMMddTHHmmss", null).ToString("dd/MM/yyyy");
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
                    DisplayAlert("Arquivo do Jogo", $"O arquivo do jogo {selectedGame.Path} não foi encontrado.", "OK");
                }
            }
        }

        loadingIndicator.IsRunning = false;
        loadingIndicator.IsVisible = false;
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

    public class GameInfo
    {
        public string GameID { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsGameNotFound { get; set; }
    }

}
