using RetroOrganizzer.Helper;
using System.IO.Compression;

namespace RetroOrganizzer.Services
{
    public class ScreenScraperService
    {
        private static readonly HttpClient _client = new();
        private readonly string baseUri = "https://www.screenscraper.fr/api2";
        private readonly string softname = "RetroOrganizzer";

        public static async Task<string> GetDataAsync(string uri)
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

        public static async Task<byte[]> GetImageAsync(string uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var contentStream = await response.Content.ReadAsStreamAsync();

            // Unzip if response is gzipped
            if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                using var decompressionStream = new GZipStream(contentStream, CompressionMode.Decompress);
                using var memoryStream = new MemoryStream();
                await decompressionStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            else
            {
                using var memoryStream = new MemoryStream();
                await contentStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        public Task<string> BuildUriAsync(string endpoint, string[] parameters = null)
        {
            string devId = AppData.Login();
            string devPassword = Cryptography.DecryptString(AppData.Password(), Cryptography.key());
            string URL = $"{baseUri}/{endpoint}?devid={devId}&devpassword={devPassword}&softname={softname}&output=json";

            foreach (string parameter in parameters)
            {
                URL += $"&{parameter}";
            }

            return Task.FromResult(URL);
        }

        public async Task<string> GetSystemListAsync()
        {
            string uri = await BuildUriAsync("systemesListe.php");
            return await ScreenScraperService.GetDataAsync(uri);
        }

        public async Task<byte[]> GetSystemImageAsync(string systemId, string media)
        {
            string[] param = new string[2];
            param[0] = $"systemeid={systemId}";
            param[1] = $"media={media}";

            string uri = await BuildUriAsync("mediaSysteme.php", param);

            return await ScreenScraperService.GetImageAsync(uri);
        }

    }
}
