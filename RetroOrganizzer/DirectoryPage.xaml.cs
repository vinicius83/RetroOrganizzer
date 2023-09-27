namespace RetroOrganizzer;

public partial class DirectoryPage : ContentPage
{
    private static readonly int REQUEST_DIRECTORY = 1;

    public DirectoryPage()
	{
		InitializeComponent();
	}

    private void ListDirectories_Clicked(object sender, EventArgs e)
    {
        string directoryPath = DirectoryEntry.Text;

        if (Directory.Exists(directoryPath))
        {
            // Listar diretórios e preencher a lista à esquerda
            List<string> directories = new List<string>(Directory.GetDirectories(directoryPath));
            DirectoriesListView.ItemsSource = directories;
        }
        else
        {
            DisplayAlert("Erro", "O diretório especificado não existe.", "OK");
        }
    }

    private void DirectoriesListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        string selectedDirectory = e.Item as string;

        if (!string.IsNullOrEmpty(selectedDirectory))
        {
            // Listar conteúdo do diretório selecionado e preencher a lista à direita
            List<string> contents = new List<string>(Directory.GetFiles(selectedDirectory));
            ContentsListView.ItemsSource = contents;
        }
    }



}
