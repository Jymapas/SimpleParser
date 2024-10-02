using Moq;
using Moq.Protected;
using SimpleParser.API;
using SimpleParser.Constants;

namespace SimpleParser.Tests
{
    public class LjPostReaderTests
    {
        [Fact]
        public async Task GetAnnounceAsync_ReturnsFormattedAnnouncement_WhenPostExists()
        {
            // Arrange
            var html = "<article class='b-singlepost-body'><p><b>03 ������� (��)</b>Some announcement content</p></article>";
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
            var ljPostReader = new LjPostReader(DateTime.Parse("2024-10-03"));
            typeof(LjPostReader)
                .GetField("httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ljPostReader, httpClient);

            // Act
            var result = await ljPostReader.GetAnnounceAsync();

            // Assert
            Assert.Contains("Some announcement content", result);
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
