namespace Catalog.API.Messages.Categories;

public sealed record CategoryActivatedEvent(
    Guid RootCategoryId,
    List<Guid> AllActivatedCategoryIds
);
