using Core.Messaging;

namespace Catalog.API.Messages.Categories;

[MessageKey("Catalog.CategoryActivatedEvent")]
public sealed record CategoryActivatedEvent(
    Guid RootCategoryId,
    List<Guid> AllActivatedCategoryIds
);
