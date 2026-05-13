using System.Net;
using System.Text;
using CineMatch.Infrastructure.Database.Configurations;
using CineMatch.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace CineMatch.Tests.Services;

[TestFixture]
public class TmdbServiceTests
{
    private ILogger<TmdbService> _loggerMock = null!;
    private TmdbSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = Substitute.For<ILogger<TmdbService>>();
        _settings = new TmdbSettings
        {
            ApiKey = "test-api-key",
            BaseUrl = "https://api.themoviedb.org/3/",
            DefaultLanguage = "en-US"
        };
    }

    private TmdbService CreateService(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(_settings.BaseUrl) };
        return new TmdbService(httpClient, Options.Create(_settings), _loggerMock);
    }

    [Test]
    public async Task GetMoviesByGenreAsync_ValidGenre_ReturnsMappedDtos()
    {
        // Arrange
        const string json = """
            {
                "results": [
                    {
                        "id": 550,
                        "title": "Fight Club",
                        "poster_path": "/test.jpg",
                        "overview": "A ticking-time-bomb insomniac.",
                        "release_date": "1999-10-15"
                    }
                ],
                "total_pages": 1
            }
            """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var service = CreateService(response);

        // Act
        var result = await service.GetMoviesByGenreAsync("action", 1, CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var dto = result[0];
        Assert.That(dto.TmdbId, Is.EqualTo(550));
        Assert.That(dto.Title, Is.EqualTo("Fight Club"));
        Assert.That(dto.PosterUrl, Is.EqualTo("https://image.tmdb.org/t/p/w500/test.jpg"));
        Assert.That(dto.Overview, Is.EqualTo("A ticking-time-bomb insomniac."));
        Assert.That(dto.ReleaseYear, Is.EqualTo(1999));
    }

    [Test]
    public void GetMoviesByGenreAsync_TmdbReturnsError_ThrowsHttpRequestException()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        var service = CreateService(response);

        // Act + Assert
        Assert.ThrowsAsync<HttpRequestException>(
            async () => await service.GetMoviesByGenreAsync("action", 1, CancellationToken.None));
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }
}
