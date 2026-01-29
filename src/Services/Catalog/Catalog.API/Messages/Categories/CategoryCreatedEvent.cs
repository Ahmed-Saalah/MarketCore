using Core.Messaging;

namespace Catalog.API.Messages.Categories;

[MessageKey("Catalog.CategoryCreatedEvent")]
public sealed record CategoryCreatedEvent(
    Guid Id,
    string Name,
    string Description,
    string? ImageUrl,
    bool IsActive,
    Guid? ParentId,
    DateTime CreatedAt
);
