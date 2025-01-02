using Moq;
using Moq.Protected;
using SimpleParser.API;
using SimpleParser.Constants;
using System.Reflection;

namespace SimpleParser.Tests
{
    public class LjPostReaderTests
    {
        [Theory]
        [InlineData("<article class='b-singlepost-body'><p><b>03 октября (чт)</b>Some announcement content</p></article>", "2024-10-03", "Some announcement content")]
        [InlineData("<article class='b-singlepost-body'><p><b>02 октября (ср)</b>Some announcement content</p></article>", "2024-10-03", ServiceLines.NoAnnouncementsToday)]
        [InlineData("<article class='b-singlepost-body'><p>Some pre-content.<br><br><b>06 октября (вс)</b><br><a href=\"https://test.com/\">Sunday contennt</a><br><br><b>07 октября (пн)</b><br><a href=\"https://test.com/\">Monday content</a><br><br><b>08 октября (вт)</b><br><a href=\"https://test.com/\">Tuesday content</a></p></article>", "2024-10-07 10:30", "Monday content")]
        [InlineData("<article class='b-singlepost-body'><p><b>30 декабря (вт)</b><br><a href='https://example.com'>Announcement 1</a><br><br><b>02 января (сб)</b><br><a href='https://example.com'>Announcement 2</a></p></article>", "2024-12-30", "Announcement 1")]
        [InlineData("<article class='b-singlepost-body'><p><b>30 декабря (вт)</b><br><a href='https://example.com'>Announcement 1</a><br><br><b>02 января (сб)</b><br><a href='https://example.com'>Announcement 2</a></p></article>", "2025-01-02", "Announcement 2")]
        public async Task GetAnnounceAsync_ReturnsFormattedAnnouncement_WhenPostExists(string html, string date, string response)
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(html),
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var ljPostReader = new LjPostReader(DateTime.Parse(date));
            typeof(LjPostReader)
                .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(ljPostReader, httpClient);

            // Act
            var result = await ljPostReader.GetAnnounceAsync();

            // Assert
            Assert.Contains(response, result);
        }

        [Fact]
        public async Task GetAnnounceAsync_ReturnsFormattedAnnouncement_AfterYearShift()
        {
            // Arrange
            var html =
                """<article class=" b-singlepost-body entry-content e-content  " lj-sale-entry lj-discovery-tags lj-embed-resizer ><p>Копия поста выкладывается в <a href="https://www.livejournal.com/away?to=https%3A%2F%2Ft.me%2FWeekChgkSPB" rel="nofollow" rel="nofollow">Телеграм-канал</a>.<br /><br /><b>30 декабря (пн)</b><br /><a href="https://chgk-spb.livejournal.com/3208005.html">Кубок Курта Кобейна-3 - Coffee Land (13:00) 1650 р.</a><br /><a href="https://chgk-spb.livejournal.com/3208285.html">Ночь согревающего очага-2024 - Coffee Land (16:00) 1800 р.</a><br /><br /><b>03 января (пт)</b><br /><a href="https://chgk-spb.livejournal.com/3208998.html">Болтик в гаечку-3 - Coffee Land (13:00) 1800 р.</a><br /><a href="https://chgk-spb.livejournal.com/3211289.html">Кубок Оливье - Queens (15:30) 1650 р.</a><br /></article>""";
            var date = "2025-01-02";
            var response = "Болтик в гаечку-3";
            var notContains = "Кубок Курта Кобейна-3";
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(html),
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var ljPostReader = new LjPostReader(DateTime.Parse(date));
            typeof(LjPostReader)
                .GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(ljPostReader, httpClient);

            // Act
            var result = await ljPostReader.GetAnnounceAsync();

            // Assert
            Assert.Contains(response, result);
            Assert.DoesNotContain(notContains, result);
        }

        [Fact]
        public async Task GetAnnounceAsync_ReturnsError_WhenPostNotFound()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var ljPostReader = new LjPostReader();
            typeof(LjPostReader)
                .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ljPostReader, httpClient);

            // Act
            var result = await ljPostReader.GetAnnounceAsync();

            // Assert
            Assert.Equal(ServiceLines.ReceivingPostError, result);
        }

        [Theory]
        [InlineData("29 сентября (вс)", "29 сентября 2024")]
        [InlineData("blabla", null)]
        [InlineData("(вс)", null)]
        public void ExtractDate_ValidText_ReturnsDatePart(string input, string result)
        {
            // Arrange
            var methodInfo = typeof(LjPostReader)
                .GetMethod("ExtractDate", BindingFlags.NonPublic | BindingFlags.Static);

            // Act
            var output = methodInfo.Invoke(null, [input, DateTime.Parse("2024-01-01")]);

            // Assert
            Assert.Equal(result, output);
        }

        [Theory]
        [InlineData("2024-10-03", "2024-10-04", false)]
        [InlineData("2024-10-05", "2024-10-04", true)]
        [InlineData("2024-12-30", "2024-01-04", true)]
        public void CompareTwoDates_ValidDates_ReturnComparasion(string date1, string date2, bool result)
        {
            // Arrange
            var lineDate = DateTime.Parse(date1);
            var currentDate = DateTime.Parse(date2);
            var methodInfo = typeof(LjPostReader)
                .GetMethod("CompareTwoDates", BindingFlags.NonPublic | BindingFlags.Instance);
            var instance = new LjPostReader();

            // Act
            var outout = methodInfo.Invoke(instance, [lineDate, currentDate]);

            // Assert
            Assert.Equal(outout, result);
        }
    }
}
