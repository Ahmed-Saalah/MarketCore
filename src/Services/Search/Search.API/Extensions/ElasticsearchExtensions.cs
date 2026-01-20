using Elastic.Clients.Elasticsearch;

namespace Search.API.Extensions;

public static class ElasticsearchExtensions
{
    public static IServiceCollection AddElasticsearch(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var uriString = configuration["ElasticSettings:Uri"] ?? "http://localhost:9200";

            var settings = new ElasticsearchClientSettings(new Uri(uriString))
                .DefaultIndex("products")
                // .DisableDirectStreaming()
                .ThrowExceptions();

            return new ElasticsearchClient(settings);
        });

        return services;
    }
}
