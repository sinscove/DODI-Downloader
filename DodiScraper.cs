using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using DodiDownloader.Ui;

namespace DodiDownloader
{
    public static class DodiScraper
    {
        private static readonly HtmlParser HtmlParser;
        private static readonly HttpClient HttpClient;

        private static int _waitTime;

        static DodiScraper()
        {
            HtmlParser = new HtmlParser();
            HttpClient = new HttpClient();
            
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");
            HttpClient.DefaultRequestHeaders.Upgrade.ParseAdd("1");
        }

        public static async Task<Dictionary<string, GameInfo>> GetGameList()
        {
            string        htmlString = await HttpClient.GetStringWithRateLimitAsync("https://dodi-repacks.site/index.php/all-my-repacks-a-z/");
            IHtmlDocument titlePage  = await HtmlParser.ParseDocumentAsync(htmlString);

            return titlePage.GetElementById("primary")
                .GetElementsByTagName("a")
                .OfType<IHtmlAnchorElement>()
                .ToDictionary(a => a.Parent.TextContent, a => new GameInfo { PageUrl = a.Href });
        }

        public static async Task<GameInfo> GetGameInfo(string url)
        {
            string htmlString  = await HttpClient.GetStringWithRateLimitAsync(url);
            IHtmlDocument page = await HtmlParser.ParseDocumentAsync(htmlString);

            IHtmlCollection<IElement> content = page.GetElementById("primary").GetElementsByClassName("entry-content").First().Children;

            List<Mirror> mirrors = new List<Mirror>();
            string description   = null;

            foreach (IElement element in content)
            {
                if (description == null && Any(element.TextContent.ToLower().StartsWith, "game description", "game", "description"))
                {
                    description = element.TextContent.TrimAnyStart("game description:\n", "game:\n", "description:\n");
                }
                
                foreach (IHtmlAnchorElement anchor in element.GetElementsByTagName("a").OfType<IHtmlAnchorElement>())
                {
                    string mirrorName = anchor.ParentElement.ParentElement.TextContent.TrimAnyEnd("(", "[", "|", "–", "-");

                    if (string.IsNullOrWhiteSpace(mirrorName) || Any(anchor.Href.Contains, "youtube", "reddit", "dodi-repacks", "onehack"))
                    {
                        continue;
                    }

                    mirrors.Add(new Mirror
                    {
                        MirrorName = mirrorName,
                        MirrorUrl  = anchor.Href
                    });
                }
            }

            return new GameInfo
            {
                PageUrl     = url,
                Mirrors     = mirrors,
                Description = description ??= "\n=== NO DESCRIPTION ==="
            };
        }

        private static bool Any(Func<string, bool> selector, params string[] values) => values.Select(selector).Any(condition => condition);
        private static string TrimAnyStart(this string input, params string[] values) => input.Substring(values.FirstOrDefault(input.ToLower().StartsWith)?.Length ?? 0);
        private static string TrimAnyEnd(this string input, params string[] values) => (from value in values select input.IndexOf(value, StringComparison.Ordinal) into length where length != -1 select input.Substring(0, length)).FirstOrDefault();

        private static async Task<string> GetStringWithRateLimitAsync(this HttpClient client, string requestUri)
        {
            while (true)
            {
                try
                {
                    string result = await client.GetStringAsync(requestUri);
                    
                    RatelimitErrorDialog.End();

                    return result;
                }
                catch (HttpRequestException)
                {
                    _waitTime += 10;
                    RatelimitErrorDialog.Show(_waitTime);
                    await Task.Delay(TimeSpan.FromSeconds(_waitTime));
                }
            }
        }
    }
}