using Core.Domain.Abstractions;
using Core.Domain.Errors;
using Core.Domain.Response;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MediatR;
using Search.API.Models;

namespace Search.API.Features;

public sealed class SearchProducts
{
    public sealed record Request(
        string? Q,
        int Page = 1,
        int PageSize = 10,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        string? Sort = null
    ) : IRequest<Response>;

    public sealed class Response : Result<ResponseDto>
    {
        public static implicit operator Response(ResponseDto dto) => new() { Value = dto };
    }

    public sealed record ResponseDto(
        long Total,
        int Page,
        int PageSize,
        IReadOnlyCollection<Product> Data
    );

    public sealed class Handler(ElasticsearchClient client) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var response = await client.SearchAsync<Product>(
                s =>
                    s.Index("products")
                        .From((request.Page - 1) * request.PageSize)
                        .Size(request.PageSize)
                        .Query(q =>
                            q.Bool(b =>
                            {
                                var mustClauses = new List<Action<QueryDescriptor<Product>>>();

                                mustClauses.Add(m =>
                                    m.Term(t => t.Field(f => f.IsActive).Value(true))
                                );

                                if (request.MinPrice.HasValue)
                                {
                                    mustClauses.Add(m =>
                                        m.Range(r =>
                                            r.NumberRange(n =>
                                                n.Field(f => f.Price)
                                                    .Gte((double)request.MinPrice.Value)
                                            )
                                        )
                                    );
                                }

                                if (request.MaxPrice.HasValue)
                                {
                                    mustClauses.Add(m =>
                                        m.Range(r =>
                                            r.NumberRange(n =>
                                                n.Field(f => f.Price)
                                                    .Lte((double)request.MaxPrice.Value)
                                            )
                                        )
                                    );
                                }

                                if (!string.IsNullOrWhiteSpace(request.Q))
                                {
                                    mustClauses.Add(m =>
                                        m.MultiMatch(mm =>
                                            mm.Query(request.Q)
                                                // "name^3" means Name is 3x more important than Description
                                                .Fields(new[] { "name^3", "description" })
                                                .Fuzziness(new Fuzziness("AUTO"))
                                        )
                                    );
                                }

                                b.Must(mustClauses.ToArray());
                            })
                        )
                        .Sort(sort =>
                        {
                            if (request.Sort?.ToLower() == "price_asc")
                                sort.Field(f => f.Price, c => c.Order(SortOrder.Asc));
                            else if (request.Sort?.ToLower() == "price_desc")
                                sort.Field(f => f.Price, c => c.Order(SortOrder.Desc));
                        }),
                cancellationToken
            );

            if (!response.IsValidResponse)
            {
                return new ResponseDto(0, request.Page, request.PageSize, []);
            }

            return new ResponseDto(
                response.Total,
                request.Page,
                request.PageSize,
                response.Documents
            );
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/search",
                    async ([AsParameters] Request query, IMediator mediator) =>
                    {
                        var result = await mediator.Send(query);
                        return result.ToHttpResult();
                    }
                )
                .WithTags("Search")
                .WithName("SearchProducts")
                .WithSummary("Search for products with filtering and sorting");
        }
    }
}
