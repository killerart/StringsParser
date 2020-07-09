using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace StringsParser
{
    class Program
    {
        const string APP_NAME = "VGFIT";
        const string APP_PATH = "https://localhost:5001";
        static void Main(string[] args) {
            var languages = Directory.GetDirectories("../../../localizations");
            var clientHandler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            };
            using (var client = new HttpClient(clientHandler)) {
                var response = client.PostAsync($"{APP_PATH}/api/app?name={APP_NAME}", new StringContent("")).Result;
                Console.WriteLine($"Post {APP_NAME} creation: {response.StatusCode}");
            }
            var keys = new HashSet<string>();
            foreach (var language in languages) {
                var ext = language.LastIndexOf(".lproj");
                var slash = language.LastIndexOf('/') + 1;
                if (ext != -1 && slash != -1) {
                    var name = language[slash..ext];
                    keys.UnionWith(ParseLanguage(name, language));
                }
            }
            clientHandler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            };
            using (var client = new HttpClient(clientHandler)) {
                var response = client.PutAsJsonAsync($"{APP_PATH}/api/app/{APP_NAME}", keys).Result;
                Console.WriteLine($"Put {APP_NAME}: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
            }
        }
        static HashSet<string> ParseLanguage(string langName, string directory) {
            var files = Directory.GetFiles(directory, "*.strings");
            var keys = new HashSet<string>();
            HttpClientHandler clientHandler;
            foreach (var file in files) {
                var translations = ParseStringsFile(file);
                keys.UnionWith(translations.Keys);
                clientHandler = new HttpClientHandler {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                };
                using (var client = new HttpClient(clientHandler)) {
                    var response = client.PostAsJsonAsync($"{APP_PATH}/api/locale", new { Name = langName, Translations = translations }).Result;
                    Console.WriteLine($"Post {langName} locale: {response.StatusCode} {response.Content.ReadAsStringAsync().Result}");
                }
            }
            return keys;
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
                    value = line[valueStart..valueEnd].Replace("\\n", "\n").Replace("\\", string.Empty) + end;
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
                        var newValue = line[0..valueEnd].Replace("\\n", "\n").Replace("\\", string.Empty) + "\n";
                        value += newValue;
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
