using Core.Messaging;
using Customer.API.Features;
using MediatR;

namespace Customer.API.Handlers;

public class UserCreatedEventHandler
{
    public sealed record Event(
        int UserId,
        string Username,
        string Email,
        string? PhoneNumber,
        string DisplayName,
        string? Role
    );

    public class Handler(IMediator mediator) : IEventHandler<Event>
    {
        public async Task HandleAsync(Event @event, CancellationToken cancellationToken)
        {
            if (@event.Role is not null && @event.Role != "Customer")
            {
                return;
            }

            var command = new CreateCustomer.Command(
                @event.UserId,
                @event.Username,
                @event.Email,
                @event.PhoneNumber!,
                @event.DisplayName
            );

            await mediator.Send(command, cancellationToken);
        }
    }
}
