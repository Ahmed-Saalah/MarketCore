//using Core.Domain.Abstractions;
//using FluentValidation;
//using MediatR;

//namespace Order.API.Features;

//public sealed class GetOrderById
//{
//    public sealed record Request(Guid Id) : IRequest<Entities.Order>;

//    public sealed class Validator : AbstractValidator<Request>
//    {
//        public Validator()
//        {
//            RuleFor(r => r.Id).NotEmpty().WithMessage("Order ID is required.");
//        }
//    }

//    public sealed class Endpoint : IEndpoint
//    {
//        public void Map(IEndpointRouteBuilder app)
//        {
//            app.MapGet(
//                "api/orders/{id}",
//                async (IMediator mediator, Guid id) =>
//                {
//                    var response = await mediator.Send(new Request(id));
//                    return response.ToHttpResult();
//                }
//            );
//        }
//    }
//}
