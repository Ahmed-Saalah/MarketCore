using Core.Messaging;

namespace Catalog.API.Messages.Categories;

[MessageKey("Catalog.CategoryUpdatedEvent")]
public sealed record CategoryUpdatedEvent(
    Guid Id,
    string Name,
    string Description,
    string? ImageUrl,
    bool IsActive,
    Guid? ParentId,
    DateTime UpdatedAt
);
