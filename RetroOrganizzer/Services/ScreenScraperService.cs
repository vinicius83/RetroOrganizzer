using RetroOrganizzer.Helper;
using System.IO.Compression;

namespace RetroOrganizzer.Services
{
    public class ScreenScraperService
    {
        private static readonly HttpClient _client = new();
        private readonly string baseUri = "https://www.screenscraper.fr/api2";
        private readonly string softname = "RetroOrganizzer";

        public static async Task<string> GetAsync(string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip")); //Compression gzip

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var contentStream = await response.Content.ReadAsStreamAsync();

            // Unzip if response is gzipped
            if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                var decompressionStream = new GZipStream(contentStream, CompressionMode.Decompress);
                var streamReader = new StreamReader(decompressionStream);
                return await streamReader.ReadToEndAsync();
            }
            else
            {
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> BuildUriAsync(string endpoint)
        {
            string devId = "vinicius83";
            string devPassword = Cryptography.DecryptString(AppData.Password(), Cryptography.key());

            return $"{baseUri}/{endpoint}?devid={devId}&devpassword={devPassword}&softname={softname}&output=json";
        }

        public async Task<string> GetSystemListAsync()
        {
            string uri = await BuildUriAsync("systemesListe.php");
            return await ScreenScraperService.GetAsync(uri);
        }

    }
}
