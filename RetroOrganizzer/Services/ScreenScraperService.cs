using RetroOrganizzer.Helper;
using System.IO.Compression;
using System.Net;

namespace RetroOrganizzer.Services
{
    public class ScreenScraperService
    {
        private static readonly HttpClient _client = new();
        private readonly string baseUri = "https://www.screenscraper.fr/api2";
        private readonly string softname = "RetroOrganizzer";

        public static string GetData(string uri)
        {
            try
            {
                var attempt = 0;
                const int maxAttempts = 3;

                while (attempt < maxAttempts)
                {
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

                        HttpResponseMessage response = _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
                        response.EnsureSuccessStatusCode();

                        var contentStream = response.Content.ReadAsStreamAsync().Result;

                        // Unzip if response is gzipped
                        if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                        {
                            var decompressionStream = new GZipStream(contentStream, CompressionMode.Decompress);
                            var streamReader = new StreamReader(decompressionStream);
                            return streamReader.ReadToEnd();
                        }
                        else
                        {
                            return response.Content.ReadAsStringAsync().Result;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Console.WriteLine($"Too Many Requests. Retrying after {Math.Pow(2, attempt)} seconds.");
                            Thread.Sleep((int)Math.Pow(2, attempt) * 1000);
                            attempt++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                Console.WriteLine("Maximum number of attempts exceeded.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        public static byte[] GetImage(string uri)
        {
            try
            {
                var attempt = 0;
                const int maxAttempts = 3;

                while (attempt < maxAttempts)
                {
                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Get, uri);
                        request.Headers.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));

                        HttpResponseMessage response = _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result;
                        response.EnsureSuccessStatusCode();

                        var contentStream = response.Content.ReadAsStreamAsync().Result;

                        // Unzip if response is gzipped
                        if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                        {
                            using var decompressionStream = new GZipStream(contentStream, CompressionMode.Decompress);
                            using var memoryStream = new MemoryStream();
                            decompressionStream.CopyTo(memoryStream);
                            return memoryStream.ToArray();
                        }
                        else
                        {
                            using var memoryStream = new MemoryStream();
                            contentStream.CopyTo(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Console.WriteLine($"Too Many Requests. Retrying after {Math.Pow(2, attempt)} seconds.");
                            Thread.Sleep((int)Math.Pow(2, attempt) * 1000);
                            attempt++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                Console.WriteLine("Maximum number of attempts exceeded.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }


        public string BuildUriAsync(string endpoint, string[] parameters = null)
        {
            string devId = AppData.Login();
            string devPassword = Cryptography.DecryptString(AppData.Password(), Cryptography.key());
            string URL = $"{baseUri}/{endpoint}?devid={devId}&devpassword={devPassword}&softname={softname}&output=json";

            foreach (string parameter in parameters)
            {
                URL += $"&{parameter}";
            }

            return URL;
        }

        public string GetSystemList()
        {
            string uri = BuildUriAsync("systemesListe.php");
            return ScreenScraperService.GetData(uri);
        }

        public byte[] GetSystemImage(string systemId, string media)
        {
            string[] param = new string[2];
            param[0] = $"systemeid={systemId}";
            param[1] = $"media={media}";

            string uri = BuildUriAsync("mediaSysteme.php", param);

            return ScreenScraperService.GetImage(uri);
        }

    }
}
