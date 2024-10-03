using HtmlAgilityPack;
using SimpleParser.Constants;
using System.Globalization;
using System.Text;

namespace SimpleParser.API;

internal class LjPostReader : IPostReader
{
    private readonly DateTime currentDate;
    private readonly HttpClient httpClient;
    private readonly CultureInfo culture = new("ru-RU");

    public LjPostReader(DateTime date)
    {
        httpClient = new();
        currentDate = date;
    }

    public LjPostReader() : this(DateTime.Now) { }

    public async Task<string> GetAnnounceAsync()
    {
        try
        {
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
            Console.WriteLine(e);
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
                && DateTime.TryParseExact(
                    ExtractDate(node.InnerText, currentDate),
                    Format.Day,
                    culture,
                    DateTimeStyles.None,
                    out var lineDate)
                && CompareTwoDates(lineDate, currentDate)))
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
                if (!href.StartsWith(Paths.Removable))
                {
                    announcements.Append(node.OuterHtml);
                    continue;
                }

                // clean up the href attribute.
                href = href.Replace(Paths.Removable, "");
                href = Uri.UnescapeDataString(href); // normalize encoded characters.
                node.SetAttributeValue("href", href);
            }

            announcements.Append(node.OuterHtml);
        }

        return announcements.ToString();
    }

    private static string ReplaceBr(string html) => html
        .Replace("<br>", "\n")
        .Replace("<br/>", "\n")
        .Replace("<br />", "\n");

    private static string ExtractDate(string text, DateTime currentDate)
    {
        // Example boldText: "29 сентября (вс)"
        var parts = text.Split('('); // Split at the day of the week.
        if (parts.Length <= 1 || !parts[0].Contains(' '))
        {
            return null;
        }

        var datePart = string.Concat(parts[0].Trim(), ' ', currentDate.Year); // "29 сентября 2024"

        return datePart.ToLower();
    }

    private bool CompareTwoDates(DateTime lineDate, DateTime currentDate)
    {
        if (currentDate.Month == 12 && currentDate.Month > lineDate.Month)
        {
            currentDate.AddYears(-1);
        }
        return lineDate >= currentDate;
    }
}
