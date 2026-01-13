namespace Catalog.API.Messages.Categories;

public sealed record CategoryCreatedEvent(
    Guid Id,
    string Name,
    string Description,
    string? ImageUrl,
    bool IsActive,
    Guid? ParentId,
    DateTime CreatedAt
);
