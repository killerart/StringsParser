using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace StringsParser
{
    class Language
    {
        public string Name { get; set; }
        public Dictionary<string, string> Translations { get; set; }
    }
    class Program
    {
        const string APP_PATH = "https://localhost:5001";
        static void Main(string[] args) {
            var languages = Directory.GetDirectories("../../../localizations");
            foreach (var language in languages) {
                var ext = language.LastIndexOf(".lproj");
                var slash = language.LastIndexOf('/') + 1;
                if (ext != -1 && slash != -1)
                    ParseLanguage(language[slash..ext], language);
            }
        }
        static void ParseLanguage(string name, string directory) {
            var files = Directory.GetFiles(directory, "*.strings");
            foreach (var file in files) {
                var clientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                };
                var translations = ParseStringsFile(file);
                using var client = new HttpClient(clientHandler);
                var response = client.PutAsJsonAsync($"{APP_PATH}/api/locale/{name}", new { Name = name, Translations = translations }).Result;
                Console.WriteLine(response.StatusCode.ToString());
                //using (var client = new HttpClient(clientHandler)) {
                //    var response = client.GetAsync(APP_PATH + "/api/locale/ru").Result;
                //    var result = response.Content.ReadAsStringAsync().Result;
                //    var lang = JsonConvert.DeserializeObject<Language>(result);
                //    foreach (var translation in lang.Translations) {
                //        Console.WriteLine($"{translation.Key} = {translation.Value}\n");
                //    }
                //    Console.WriteLine(response.StatusCode.ToString());
                //}
            }
        }
        static Dictionary<string, string> ParseStringsFile(string path) {
            var text = File.ReadAllLines(path);
            var translations = new Dictionary<string, string>();
            string key = string.Empty, value = string.Empty;
            bool isComment = false;
            bool isMultiLineValue = false;
            for (int i = 0; i < text.Length; i++) {
                var line = text[i];
                if (isComment) {
                    if (line.EndsWith("*/")) {
                        isComment = false;
                    }
                    continue;
                }
                if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.StartsWith("/*")) {
                    isComment = true;
                    continue;
                }
                if (line.StartsWith('"') && !isMultiLineValue) {
                    int keyEnd = line.IndexOf('"', 1);
                    while (line[keyEnd - 1] == '\\') {
                        keyEnd = line.IndexOf('"', keyEnd + 1);
                    }
                    int valueStart = line.IndexOf('"', keyEnd + 1) + 1;
                    int valueEnd = line.IndexOf('"', valueStart);
                    while (valueEnd != -1 && line[valueEnd - 1] == '\\') {
                        valueEnd = line.IndexOf('"', valueEnd + 1);
                    }
                    string end = string.Empty;
                    key = line[1..keyEnd];
                    if (valueEnd == -1) {
                        valueEnd = line.Length;
                        end = "\n";
                        isMultiLineValue = true;
                    }
                    value = line[valueStart..valueEnd] + end;
                    if (!isMultiLineValue) {
                        if (!translations.ContainsKey(key))
                            translations.Add(key, value);
                        //Console.WriteLine($"{key} = {value}\n");
                        //Console.ReadKey(true);
                    }
                    continue;
                }
                if (isMultiLineValue) {
                    if (line != "\";") {
                        int valueEnd = line.LastIndexOf('"');
                        if (valueEnd != -1 && line[valueEnd - 1] == '\\') {
                            valueEnd = -1;
                        }
                        if (valueEnd == -1) {
                            valueEnd = line.Length;
                        } else {
                            isMultiLineValue = false;
                        }
                        value += string.Format($"{line[0..valueEnd]}\n");
                    } else {
                        isMultiLineValue = false;
                    }
                    if (!isMultiLineValue) {
                        if (!translations.ContainsKey(key))
                            translations.Add(key, value);
                        //Console.WriteLine($"{key} = {value}\n");
                        //Console.ReadKey(true);
                    }
                }
            }
            return translations;
        }
    }
}
