using System.Runtime.Serialization;
using HtmlAgilityPack;
using System.Web;
using SimpleParser.Constants;

namespace SimpleParser
{
    internal class Program
    {
        private async Task Main(string[] args)
        {
            var html = await new HttpClient().GetStringAsync(Paths.PostUri);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var postContent = doc.DocumentNode.SelectSingleNode("//article[contains(@class, 'b-singlepost-body')]");
            if (postContent != null)
            {
                var contentHtml = postContent.InnerHtml;
                var firstBreakIndex = contentHtml.IndexOf("<br><br>");
                if (firstBreakIndex != -1)
                {
                    contentHtml = contentHtml[(firstBreakIndex + 8)..]; // +8 to remove the first "<br><br>"
                }

                postContent.InnerHtml = contentHtml;

                foreach (var link in postContent.SelectNodes(".//a[@href]"))
                {
                    var href = link.GetAttributeValue("href", string.Empty);
                    if (!href.StartsWith("https://www.livejournal.com/away?to=")) 
                        continue;
                    href = href.Replace("https://www.livejournal.com/away?to=", "");
                    href = HttpUtility.UrlDecode(href); // Normalize encoded characters
                    link.SetAttributeValue("href", href);
                }

                Console.WriteLine(postContent.InnerHtml);
            }
            else
            {
                Console.WriteLine("Post content not found.");
            }
        }

    }
}
