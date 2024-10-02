using Moq;
using Moq.Protected;
using SimpleParser.API;
using SimpleParser.Constants;

namespace SimpleParser.Tests
{
    public class LjPostReaderTests
    {
        [Theory]
        [InlineData("<article class='b-singlepost-body'><p><b>03 октября (чт)</b>Some announcement content</p></article>", "2024-10-03", "Some announcement content")]
        [InlineData("<article class='b-singlepost-body'><p><b>02 октября (чт)</b>Some announcement content</p></article>", "2024-10-03", ServiceLines.NoAnnouncementsToday)]
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
                .GetField("httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ljPostReader, httpClient);

            // Act
            var result = await ljPostReader.GetAnnounceAsync();

            // Assert
            Assert.Contains(response, result);
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
                .GetField("httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ljPostReader, httpClient);

            // Act
            var result = await ljPostReader.GetAnnounceAsync();

            // Assert
            Assert.Equal(ServiceLines.ReceivingPostError, result);
        }
    }
}
