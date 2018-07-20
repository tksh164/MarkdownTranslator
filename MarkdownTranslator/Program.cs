using System;
using System.IO;
using System.Text;

namespace MarkdownTranslator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                return;
            }

            var sourceFilePath = args[0];
            var destinationFilePath = args[1];

            using (var stream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                MarkdownTranslator.TranslateToWriter(sourceFilePath, writer).Wait();
            }
        }
    }
}
