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
            // Listar diret�rios e preencher a lista � esquerda
            List<string> directories = new List<string>(Directory.GetDirectories(directoryPath));
            DirectoriesListView.ItemsSource = directories;
        }
        else
        {
            DisplayAlert("Erro", "O diret�rio especificado n�o existe.", "OK");
        }
    }

    private void DirectoriesListView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        string selectedDirectory = e.Item as string;

        if (!string.IsNullOrEmpty(selectedDirectory))
        {
            // Listar conte�do do diret�rio selecionado e preencher a lista � direita
            List<string> contents = new List<string>(Directory.GetFiles(selectedDirectory));
            ContentsListView.ItemsSource = contents;
        }
    }



}
