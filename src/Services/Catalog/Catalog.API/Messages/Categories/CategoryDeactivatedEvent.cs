using Core.Messaging;

namespace Catalog.API.Messages.Categories;

[MessageKey("Catalog.CategoryDeactivatedEvent")]
public record CategoryDeactivatedEvent(Guid RootCategoryId, List<Guid> AllDeactivatedCategoryIds);
