using System.Text.Json;
using System.Text.Json.Serialization;
using CineMatch.Application.Features.Movies.Common.Dtos;
using CineMatch.Application.Interfaces.Services;
using CineMatch.Infrastructure.Database.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CineMatch.Infrastructure.Services;

public class TmdbService : ITmdbService
{
    private static readonly Dictionary<string, int> GenreIds = new(StringComparer.OrdinalIgnoreCase)
    {
        { "action",          28    },
        { "adventure",       12    },
        { "animation",       16    },
        { "comedy",          35    },
        { "crime",           80    },
        { "documentary",     99    },
        { "drama",           18    },
        { "fantasy",         14    },
        { "horror",          27    },
        { "romance",         10749 },
        { "science-fiction", 878   },
        { "thriller",        53    },
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string ImageBaseUrl = "https://image.tmdb.org/t/p/w500";

    private readonly HttpClient _httpClient;
    private readonly TmdbSettings _settings;
    private readonly ILogger<TmdbService> _logger;

    public TmdbService(HttpClient httpClient, IOptions<TmdbSettings> settings, ILogger<TmdbService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TmdbMovieDto>> GetMoviesByGenreAsync(
        string genre, int count, CancellationToken cancellationToken)
    {
        var results = new List<TmdbMovieDto>();
        var page = 1;

        while (results.Count < count)
        {
            var url = BuildUrl(genre, page);
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<TmdbPagedResponse>(content, JsonOptions)
                ?? throw new HttpRequestException("TMDB returned an empty response.");

            foreach (var movie in apiResponse.Results)
            {
                results.Add(MapToDto(movie));
                if (results.Count >= count) break;
            }

            if (page >= apiResponse.TotalPages || !apiResponse.Results.Any()) break;
            page++;
        }

        if (results.Count < count)
            _logger.LogWarning("TMDB returned {Actual} movies, fewer than the requested {Count}.", results.Count, count);

        return results;
    }

    private string BuildUrl(string genre, int page)
    {
        var language = _settings.DefaultLanguage;
        var apiKey = _settings.ApiKey;

        if (genre.Equals("popular", StringComparison.OrdinalIgnoreCase))
            return $"movie/popular?api_key={apiKey}&language={language}&page={page}";

        if (GenreIds.TryGetValue(genre, out var genreId))
            return $"discover/movie?api_key={apiKey}&language={language}&with_genres={genreId}&page={page}";

        // Fall back to popular for unknown genres rather than failing the party creation.
        _logger.LogWarning("Unknown genre '{Genre}', falling back to popular.", genre);
        return $"movie/popular?api_key={apiKey}&language={language}&page={page}";
    }

    private static TmdbMovieDto MapToDto(TmdbMovieResult movie)
    {
        var posterUrl = string.IsNullOrEmpty(movie.PosterPath)
            ? string.Empty
            : $"{ImageBaseUrl}{movie.PosterPath}";

        var releaseYear = int.TryParse(movie.ReleaseDate?.Split('-').FirstOrDefault(), out var year)
            ? year
            : 0;

        return new TmdbMovieDto(
            TmdbId: movie.Id,
            Title: movie.Title ?? string.Empty,
            PosterUrl: posterUrl,
            Overview: movie.Overview ?? string.Empty,
            ReleaseYear: releaseYear);
    }

    private record TmdbPagedResponse(
        [property: JsonPropertyName("results")] List<TmdbMovieResult> Results,
        [property: JsonPropertyName("total_pages")] int TotalPages);

    private record TmdbMovieResult(
        [property: JsonPropertyName("id")]           int Id,
        [property: JsonPropertyName("title")]        string? Title,
        [property: JsonPropertyName("poster_path")]  string? PosterPath,
        [property: JsonPropertyName("overview")]     string? Overview,
        [property: JsonPropertyName("release_date")] string? ReleaseDate);
}
