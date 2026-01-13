namespace Catalog.API.Messages.Categories;

public record CategoryDeactivatedEvent(Guid RootCategoryId, List<Guid> AllDeactivatedCategoryIds);
