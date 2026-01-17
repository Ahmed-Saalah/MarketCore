namespace Warehouse.API.Entities
{
    public class StockTransaction
    {
        public Guid Id { get; set; }
        public Guid StoreId { get; set; }
        public Guid InventoryId { get; set; }
        public Inventory Inventory { get; set; }

        public TransactionType Type { get; set; }

        public int QuantityChanged { get; set; }

        public string ReferenceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TransactionType
    {
        Restock, // Adding stock from supplier
        Sale, // Removing stock for an order
        Adjustment, // Correction (e.g., item broken/lost)
        Reservation, // Locking stock
    }
}
