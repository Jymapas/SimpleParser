using HtmlAgilityPack;
using SimpleParser.Constants;
using System.Web;

namespace SimpleParser.API
{
    internal class LjPostReader
    {
        internal async Task<string> GetAnnounce()
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
                    contentHtml = contentHtml[(firstBreakIndex)..];
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

                return postContent.InnerHtml;
            }
            else
            {
                return ServiceLines.ReceivingPostError;
            }
        }
    }
}
