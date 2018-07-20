using System;
using System.Text;

namespace MarkdownTranslator
{
    internal sealed class TranslatorUriBuilder
    {
        public TranslatorUriBuilder(string translateTo)
        {
            TranslateTo = translateTo;
        }

        public TranslatorUriBuilder(string translateFrom, string translateTo)
        {
            TranslateFrom = translateFrom;
            TranslateTo = translateTo;
        }

        private const string protocol = "https";
        public string Protocol { get { return protocol; } }

        private const string host = "api.cognitive.microsofttranslator.com";
        public string Host { get { return host; } }

        private const string apiVersion = "3.0";
        public string ApiVersion { get { return apiVersion; } }


        public string TranslateFrom { get; set; }
        public string TranslateTo { get; set; }

        public Uri GetUri()
        {
            var uri = new StringBuilder(string.Format("{0}://{1}/translate?api-version={2}", Protocol, Host, ApiVersion));

            if (!string.IsNullOrWhiteSpace(TranslateFrom))
            {
                uri.AppendFormat("&from={0}", TranslateFrom);
            }

            if (!string.IsNullOrWhiteSpace(TranslateTo))
            {
                uri.AppendFormat("&to={0}", TranslateTo);
            }
            else
            {
                throw new InvalidOperationException(string.Format(@"The {0} property's value is invalid. The value was ""{1}"".", nameof(TranslateTo), TranslateTo == null ? "null" : TranslateTo.ToString()));
            }

            return new Uri(uri.ToString());
        }
    }
}
