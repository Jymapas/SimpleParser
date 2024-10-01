﻿using HtmlAgilityPack;
using SimpleParser.Constants;
using System.Globalization;
using System.Text;
using System.Web;

namespace SimpleParser.API;

internal class LjPostReader
{
    private readonly DateTime CurrentDate;
    private static readonly HttpClient httpClient = new();
    private CultureInfo culture = new("ru-RU");
    private string format = "dd MMMM (ddd)";

    public LjPostReader()
    {
        CurrentDate = DateTime.Now;
    }

    internal async Task<string> GetAnnounceAsync()
    {
        try
        {
            // Fetch the HTML content asynchronously.
            var html = await httpClient.GetStringAsync(Paths.PostUri);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Select the main post content's <p> node.
            var postContent = doc.DocumentNode.SelectSingleNode("//article[contains(@class, 'b-singlepost-body')]/p");
            if (postContent == null)
                return ServiceLines.ReceivingPostError;

            // Extract relevant sections based on the current date.
            var relevantAnnouncements = ExtractAnnouncementsForToday(postContent);

            if (string.IsNullOrWhiteSpace(relevantAnnouncements))
                return ServiceLines.NoAnnouncementsToday; // Define this in ServiceLines as appropriate.

            // Clean up the HTML and format it for Telegram.
            var formattedAnnouncement = ReplaceBr(relevantAnnouncements);

            return formattedAnnouncement;
        }
        catch (Exception e)
        {
            return ServiceLines.ReceivingPostError;
        }
    }

    private string ExtractAnnouncementsForToday(HtmlNode postContent)
    {
        var announcements = new StringBuilder();
        var skipCheck = false;

        // Iterate through all child nodes of the <p> tag.
        foreach (var node in postContent.ChildNodes)
        {
            if (skipCheck 
                || (node.Name.Equals("b", StringComparison.OrdinalIgnoreCase) 
                && DateTime.TryParseExact(node.InnerText, format, culture, DateTimeStyles.None, out var lineDate)
                && lineDate >= CurrentDate))
            {
                skipCheck = true;
            }
            else
            {
                continue;
            }

            if (node.Name.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                var href = node.GetAttributeValue("href", string.Empty);
                if (!href.StartsWith(ServiceLines.RemovableString))
                {
                    announcements.Append(node.OuterHtml);
                    continue;
                }

                // clean up the href attribute.
                href = href.Replace(ServiceLines.RemovableString, "");
                href = HttpUtility.UrlDecode(href); // normalize encoded characters.
                node.SetAttributeValue("href", href);
            }

            announcements.Append(node.OuterHtml);
        }

        return announcements.ToString();
    }

    private static string ReplaceBr(string html) => html.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");
}
