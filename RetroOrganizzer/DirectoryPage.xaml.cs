
using System;
using System.Xml;
using System.Xml.Linq;

namespace RetroOrganizzer;

public partial class DirectoryPage : ContentPage
{
    private static readonly int REQUEST_DIRECTORY = 1;

    public DirectoryPage()
    {
        InitializeComponent();
    }

    List<string> nomesDeJogos = new List<string>();
    string xmlFilePath;

    private async void EscolherXML_Clicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickAsync(PickOptions.Default);

            if (result != null)
            {
                xmlFilePath = result.FullPath;

                if (Path.GetExtension(xmlFilePath) == ".xml")
                {
                    // Carregar o arquivo XML
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(xmlFilePath);

                    // Extrair os nomes dos jogos do XML
                    XmlNodeList gameNodes = xmlDoc.SelectNodes("/gameList/game");

                    List<GameInfo> gamesInfo = new List<GameInfo>();

                    foreach (XmlNode gameNode in gameNodes)
                    {
                        string id = gameNode.Attributes["id"]?.Value;
                        string name = gameNode.SelectSingleNode("name")?.InnerText;
                        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                        {
                            gamesInfo.Add(new GameInfo { ID = id, Name = name });
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
        }
        catch (Exception ex)
        {
            // Lida com erros, se houver algum
            lblPastaSelecionada.Text = $"Erro: {ex.Message}";
        }
    }

    private void listaDeJogos_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        if (e.SelectedItem != null && !string.IsNullOrEmpty(xmlFilePath))
        {
            GameInfo selectedGame = e.SelectedItem as GameInfo;
            string selectedGameID = selectedGame.ID;

            string xpathQuery = $"//game[@id='{selectedGameID}']";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            XmlNode gameNode = xmlDoc.SelectSingleNode(xpathQuery);

            if (gameNode != null)
            {
                string xmlDirectory = Path.GetDirectoryName(xmlFilePath);

               // string gameID = gameNode.SelectSingleNode("gameID")?.InnerText;
                string path = gameNode.SelectSingleNode("path")?.InnerText;
                string name = gameNode.SelectSingleNode("name")?.InnerText;
                string desc = gameNode.SelectSingleNode("desc")?.InnerText;
                string releaseDateValue = gameNode.SelectSingleNode("releaseDate")?.InnerText;
                string developerValue = gameNode.SelectSingleNode("developer")?.InnerText;
                string publisherValue = gameNode.SelectSingleNode("publisher")?.InnerText;
                string genreValue = gameNode.SelectSingleNode("genre")?.InnerText;
                string playersValue = gameNode.SelectSingleNode("players")?.InnerText;
                string langValue = gameNode.SelectSingleNode("lang")?.InnerText;

                PathEntry.Text = path;
                NameEntry.Text = name;
                DescEditor.Text = desc;
                ReleaseDateEntry.Text = releaseDateValue;
                DeveloperEntry.Text = developerValue;
                PublisherEntry.Text = publisherValue;
                GenreEntry.Text = genreValue;
                PlayersEntry.Text = playersValue;
                LangEntry.Text = langValue;

                string imageValue = gameNode.SelectSingleNode("image")?.InnerText;
                string marqueeValue = gameNode.SelectSingleNode("marquee")?.InnerText;

                ImageEntry.Text = imageValue;
                MarqueeEntry.Text = marqueeValue;

                if (imageValue != null)
                {
                    imageValue = imageValue.Substring(2).Replace("/", Path.DirectorySeparatorChar.ToString());
                    imageValue = Path.Combine(xmlDirectory, imageValue);
                    if (File.Exists(imageValue))
                    {
                        ImageDisplay.Source = ImageSource.FromFile(imageValue);
                    }
                    else
                    {
                        ImageDisplay.Source = null;
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
                    else
                    {
                        MarqueeDisplay.Source = null;
                    }
                }
            }
        }
    }

    public class GameInfo
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }

}
