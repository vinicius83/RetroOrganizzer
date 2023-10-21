using System.Globalization;

namespace RetroOrganizzer.Converters
{
    public class PathToBoolConverter : IValueConverter
    {
        private List<string> _jogosNaoEncontrados;

        public PathToBoolConverter(List<string> jogosNaoEncontrados)
        {
            _jogosNaoEncontrados = jogosNaoEncontrados;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = (string)value;
            return _jogosNaoEncontrados.Contains(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
