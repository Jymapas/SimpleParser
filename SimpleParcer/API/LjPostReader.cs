using System.Globalization;
using System.Text;
using HtmlAgilityPack;
using SimpleParser.Constants;

namespace SimpleParser.API;

internal class LjPostReader : IPostReader
{
    private readonly CultureInfo _culture = new("ru-RU");
    private readonly DateTime _currentDate;
    private readonly HttpClient _httpClient = new();
    private bool _yearShiftChecked = false;

    public LjPostReader(DateTime date)
    {
        _currentDate = date;
    }

    public LjPostReader() : this(DateTime.Now) { }

    public async Task<string> GetAnnounceAsync()
    {
        try
        {
            var html = await _httpClient.GetStringAsync(Paths.PostUri);

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Select the main post content's <p> node.
            var postContent = doc.DocumentNode.SelectSingleNode("//article[contains(@class, 'b-singlepost-body')]/p");
            if (postContent == null)
            {
                return ServiceLines.ReceivingPostError;
            }

            // Extract relevant sections based on the current date.
            var relevantAnnouncements = ExtractAnnouncementsForToday(postContent);

            if (string.IsNullOrWhiteSpace(relevantAnnouncements))
            {
                return ServiceLines.NoAnnouncementsToday; // Define this in ServiceLines as appropriate.
            }

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
                        ExtractDate(node.InnerText),
                        Format.Day,
                        _culture,
                        DateTimeStyles.None,
                        out var lineDate)
                    && CompareTwoDates(lineDate, _currentDate)))
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

    private string ExtractDate(string text)
    {
        // Example boldText: "29 сентября (вс)"
        var parts = text.Split('('); // Split at the day of the week.
        if (parts.Length <= 1 || !parts[0].Contains(' '))
        {
            return null;
        }

        // TODO: extract adding year to separate function; Add check year and add year minus 1
        var datePart = string.Concat(parts[0].Trim(), ' ', _currentDate.Year); // "29 сентября 2024"

        return AddYear(parts[0].Trim());
    }

    private string AddYear(string datePart)
    {
        if (_yearShiftChecked)
        {
            return string.Concat(datePart, ' ', _currentDate.Year);
        }

        if (datePart.Contains("декаб") && _currentDate.Month == 1)
        {
            return string.Concat(datePart, ' ', (_currentDate.Year - 1));
        }

        _yearShiftChecked = true;
        return string.Concat(datePart, ' ', _currentDate.Year);
    }

    private bool CompareTwoDates(DateTime lineDate, DateTime currentDate)
    {
        var currentDateWithoutTime = currentDate.Date;
        var lineDateWithoutTime = lineDate.Date;

        if (currentDate.Month == 12 && currentDate.Month > lineDate.Month)
        {
            currentDateWithoutTime = currentDateWithoutTime.AddYears(-1);
        }

        return lineDateWithoutTime >= currentDateWithoutTime;
    }
}