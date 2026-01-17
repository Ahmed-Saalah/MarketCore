namespace Warehouse.API.Entities
{
    public class Inventory
    {
        public Guid Id { get; set; }
        public Guid StoreId { get; set; }
        public Guid ProductId { get; set; }
        public string Sku { get; set; }

        public int QuantityOnHand { get; set; }

        public int ReservedQuantity { get; set; }

        public int AvailableStock => QuantityOnHand - ReservedQuantity;

        public int ReorderLevel { get; set; }
    }
}
