using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BlackToonManager
{


    class Program
    {
        private static HtmlDocument HtmlDocument { get; } = new HtmlDocument();
        private static HttpClient HttpClient { get; } = new HttpClient();
        private static string Domain { get; set; }

        static async Task<JsonDocument> GetJsonDocumentAsync(string fileName) //
        {
            var result = await HttpClient.GetStringAsync($"{Domain}data/{fileName}"); //Get Site address
            var index = result.IndexOf('=');
            result = result[(index + 1)..^1].Trim(); //Extract Json parts from Js file.
            return await Task.FromResult(JsonDocument.Parse(result));

        }
        static async Task<string> GetDomainAsync(string url)
        {
            var result = await (await HttpClient.GetAsync(url)).Content.ReadAsStringAsync();
            HtmlDocument.LoadHtml(result);
            var node = HtmlDocument.DocumentNode.SelectSingleNode("//a");
            var address = node.Attributes["href"].Value;

            return address;
        }

        static async Task Main()
        {

            var result = await HttpClient.GetStringAsync("https://blacktoon.net");
            HtmlDocument.LoadHtml(result);
            var node = HtmlDocument.DocumentNode.SelectSingleNode($"//main/div/div/a");
            result = await GetDomainAsync(node.Attributes["href"].Value); // capture bit.ly type redirect address
            HttpResponseMessage httpResponseMessage = await HttpClient.GetAsync(result);
            httpResponseMessage.EnsureSuccessStatusCode();
            Domain = httpResponseMessage.RequestMessage.RequestUri.ToString(); //redirected domin

            result = await HttpClient.GetStringAsync(Domain);
            HtmlDocument.LoadHtml(result);
            result = HtmlDocument.DocumentNode.SelectSingleNode($"//body/script[@type='text/javascript']").InnerText;
            var javaScriptFile = Regex.Matches(result, @"webtoon_\d.js\?v=_(\d+)");
            var javaScriptFiles = new List<string>();

            foreach (var element in javaScriptFile) //get javascript files
            {
                int i = javaScriptFile.ToString().IndexOf(element.ToString());
                javaScriptFiles.Add(element.ToString());
            }

            ServicePointManager.DefaultConnectionLimit = 120;
            //change httpclient default limit

            Console.WriteLine("Program Start\n");
            //ProGram Start


            Console.WriteLine("Please enter the webtoon name of Black Toon.");
            var bookName = Console.ReadLine();
            Console.Clear();

            JsonDocument json = default; //JsonDocument type variable
            string index = default;
            var webToonData = new List<(string title, string index)>(); //Webtoon title, index tuple

            foreach (var element in javaScriptFiles) //Search
            {
                json = await GetJsonDocumentAsync(element); //Convert javascript in jsondocuemnt

                foreach (var jsonElement in json.RootElement.EnumerateArray())
                    if (jsonElement.GetProperty("t").GetString().Replace(" ", "").IndexOf(bookName.Replace(" ", "")) != -1)
                        webToonData.Add((jsonElement.GetProperty("t").GetString(), jsonElement.GetProperty("x").GetString()));

                if (webToonData.Count == 0) //No matching
                {
                    Console.WriteLine("No matching webtoon.");
                    Console.WriteLine("Exit the program.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();
                    return;
                }
            }

            for (int i = 0; i < webToonData.Count; i++) //Print the matching Data.
                Console.WriteLine(i + ". " + webToonData[i]);

            Console.WriteLine("Please enter the number of the webtoon you want!");
            var input = Convert.ToInt32(Console.ReadLine());

            if (input >= webToonData.Count) //Exceptions
            {
                Console.WriteLine("You entered a number that does not exist."); ;
                Console.WriteLine("Exit the program.");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                return;
            }


            index = webToonData[input].index;

            var random = new Random();
            var randomValue = random.NextDouble().ToString();

            HttpClient.DefaultRequestHeaders.Add("referer", $"{Domain}webtoon/{index}.html");
            json = await GetJsonDocumentAsync($"toonlist/{index}.js?V+{randomValue}"); //convert jsondocument type 
            bookName = webToonData[input].title;
            Console.Clear();

            Console.WriteLine("Would you like to download it??");
            Console.WriteLine("If you want to exit in the middle of the download, press Ctrl + C.");
            Console.WriteLine("YES: 1");
            Console.WriteLine("No: Press any key to exit.\n");
            Console.WriteLine($"Current search term : {bookName}");

            input = Convert.ToInt32(Console.ReadLine());

            if (input == 1) //start download
            {
                Console.Clear();
                Console.WriteLine("Please select a download mode.");
                Console.WriteLine("1: Partial download");
                Console.WriteLine("2: Full download");
                Console.WriteLine("Enter any key to exit.");
                input = Convert.ToInt32(Console.ReadLine());

                int chapterCount = 1; //chpter index
                foreach (var element in json.RootElement.EnumerateArray())
                {
                    string url = default;
                    string chapterIndex = default;

                    if (input == 1)
                    {
                        Console.WriteLine("What chapter would you like to download?");
                        chapterIndex = Console.ReadLine();
                        foreach (var jsonelemnt in json.RootElement.EnumerateArray())
                        {
                            if (jsonelemnt.GetProperty("t").GetString().Replace(" ", "").IndexOf(chapterIndex) != -1)
                            {
                                url = jsonelemnt.GetProperty("u").GetString();
                                break;
                            }
                        }
                        if (url == null)
                        {
                            Console.WriteLine("does not exist matching chapter. Please check and try again.");
                            Console.WriteLine("Program exit.");
                            Console.WriteLine("Please press any key to exit.");
                            Console.ReadLine();
                            break;

                        }
                    }
                    else if (input == 2)
                    {
                        url = element.GetProperty("u").GetString(); // return example : '\webtoons\3658\172277.html'
                    }
                    else
                    {
                        Console.WriteLine("Program Exit");
                        return;
                    }

                    result = await HttpClient.GetStringAsync($"{Domain}{url}");
                    HtmlDocument.LoadHtml(result);
                    string htmlTag = "//div[@class='toon-viewer-warp']";

                    var nodes = HtmlDocument.DocumentNode.SelectNodes($"{htmlTag}/img") ?? HtmlDocument.DocumentNode.SelectNodes($"{htmlTag}/p/img") ?? HtmlDocument.DocumentNode.SelectNodes($"{htmlTag}/div/img")
                    ?? HtmlDocument.DocumentNode.SelectNodes($"{htmlTag}/div/p/img") ?? HtmlDocument.DocumentNode.SelectNodes($"{htmlTag}/div/a/img") ?? HtmlDocument.DocumentNode.SelectNodes($"{htmlTag}/div/div/img");

                    string chapterNumbers = input == 1 ? chapterIndex : chapterCount.ToString();

                    Directory.CreateDirectory(@$"D:\Webtoons/webtoons/{bookName}/{bookName}_{chapterNumbers}");

                    Console.WriteLine($"Downloading .. chapter : {chapterNumbers}");

                    try
                    {
                        var DownloadBooks = nodes.Select(async (node, idx) =>
                        {
                            using var httpClient = new HttpClient();
                            using var httpGetImageValue = await httpClient.GetAsync($"https://blacktoonimg.com/{node.Attributes["o_src"].Value}"); // image domain example : https://blacktoonimg.com/2020/1007/20201007111831643.jpg
                            using var fileStream = new FileStream(@$"D:\Webtoons/webtoons/{bookName}/{bookName}_{chapterNumbers}/{chapterNumbers}_{idx}.jpg", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                            await httpGetImageValue.Content.CopyToAsync(fileStream);
                        });

                        await Task.WhenAll(DownloadBooks);

                        Directory.CreateDirectory(@$"D:\Webtoons/filezips/{bookName}");
                        var path = @$"D:\Webtoons/filezips/{bookName}/{bookName}_{chapterNumbers}.zip";
                        if (!File.Exists(path))
                        {
                            ZipFile.CreateFromDirectory(@$"D:\Webtoons/webtoons/{bookName}/{bookName}_{chapterNumbers}", path);
                        }

                    }
                    catch (Exception e) //error catch . Usually tag problem
                    {
                        Console.WriteLine(e);
                    }

                    chapterCount++;

                    if (input == 1)
                        break;

                }
            }
            else { Console.WriteLine("Program Exit."); }

            Console.WriteLine("Program Exit ! Please Press any key to exit");
            Console.ReadLine();
        }
    }

}
