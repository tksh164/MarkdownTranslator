using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MarkdownTranslator
{
    internal static class TranslatorClient
    {
        private static string GetCognitiveServicesKey()
        {
            return AppSettings.Default.CognitiveServicesKey;
        }

        public static async Task<string> Translate(string text, string translateTo, string translateFrom = null)
        {
            var uriBuilder = new TranslatorUriBuilder(translateFrom, translateTo);
            var body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = uriBuilder.GetUri();
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", GetCognitiveServicesKey());

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);

                return result[0].translations[0].text;
            }
        }
    }
}
