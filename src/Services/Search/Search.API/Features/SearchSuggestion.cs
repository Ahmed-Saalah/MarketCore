using Core.Domain.Abstractions;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Search.API.Models;

namespace Search.API.Features;

public sealed class SearchSuggestions
{
    public sealed record Request(string Query) : IRequest<List<string>>;

    public sealed class Handler(ElasticsearchClient client) : IRequestHandler<Request, List<string>>
    {
        public async Task<List<string>> Handle(Request request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return new List<string>();
            }

            var response = await client.SearchAsync<Product>(
                s =>
                    s.Index("products")
                        .Size(5)
                        .SourceIncludes(new[] { "name" })
                        .Query(q =>
                            q.Bool(b =>
                                b.Must(m => m.Term(t => t.Field(f => f.IsActive).Value(true)))
                                    .Must(m =>
                                        m.MatchBoolPrefix(p =>
                                            p.Field(f => f.Name).Query(request.Query)
                                        )
                                    )
                            )
                        ),
                cancellationToken
            );

            if (!response.IsValidResponse)
            {
                return new List<string>();
            }

            return response.Documents.Select(d => d.Name).Distinct().ToList();
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/search/suggestions",
                    async (string q, IMediator mediator) =>
                    {
                        var result = await mediator.Send(new Request(q));
                        return Results.Ok(result);
                    }
                )
                .WithTags("Search")
                .WithName("GetSearchSuggestions")
                .WithSummary("Get auto-complete suggestions based on product names");
        }
    }
}
