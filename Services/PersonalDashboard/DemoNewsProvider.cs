using api.Dtos.PersonalDashboard;
using api.Interfaces;

namespace api.Services.PersonalDashboard
{
    public sealed class DemoNewsProvider : INewsProvider
    {
        public Task<IReadOnlyCollection<NewsArticleDto>> GetNewsAsync(
            NewsCategory category,
            int limit,
            CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var source = category switch
            {
                NewsCategory.Economy => "Banque de France",
                NewsCategory.ArtificialIntelligence => "INRIA",
                NewsCategory.ScienceTechnology => "CNRS",
                NewsCategory.World => "Toute l'Europe",
                _ => "Service public"
            };

            var items = Enumerable.Range(0, Math.Max(1, limit))
                .Select(index =>
                {
                    var publishedAt = now.AddMinutes(-(index + 1) * 19);
                    var url = ExternalUrlGuard.SafeHttpUrl($"https://example.org/life-demo/news/{category.ToString().ToLowerInvariant()}/{index + 1}")!;
                    return new NewsArticleDto(
                        $"{category}-{index + 1}",
                        BuildTitle(category, index),
                        "Donnée de démonstration destinée à valider la présentation du portlet. Remplacez le fournisseur Demo par un flux autorisé pour afficher de vraies actualités.",
                        source,
                        url,
                        null,
                        category.ToString(),
                        publishedAt,
                        now - publishedAt <= TimeSpan.FromHours(2));
                })
                .ToList();

            return Task.FromResult<IReadOnlyCollection<NewsArticleDto>>(items);
        }

        private static string BuildTitle(NewsCategory category, int index) =>
            category switch
            {
                NewsCategory.France => $"Point France #{index + 1}",
                NewsCategory.World => $"Repère international #{index + 1}",
                NewsCategory.Economy => $"Tendance économique #{index + 1}",
                NewsCategory.ScienceTechnology => $"Innovation scientifique #{index + 1}",
                NewsCategory.ArtificialIntelligence => $"Veille intelligence artificielle #{index + 1}",
                NewsCategory.Society => $"Fait de société #{index + 1}",
                NewsCategory.Health => $"Actualité santé #{index + 1}",
                NewsCategory.Culture => $"Sélection culture #{index + 1}",
                NewsCategory.Sport => $"Résultat sport #{index + 1}",
                _ => $"À la une #{index + 1}"
            };
    }
}
