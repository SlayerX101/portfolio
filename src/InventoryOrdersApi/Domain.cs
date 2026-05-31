namespace InventoryOrdersApi;

public enum OrderStatus
{
    Draft,
    Confirmed,
    Packed,
    Shipped,
    Cancelled
}

public sealed record Product(
    int Id,
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int StockOnHand,
    int ReorderLevel,
    bool Active);

public sealed record SalesOrder(
    int Id,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    decimal Subtotal,
    decimal Vat,
    decimal Total,
    List<OrderLine> Lines,
    List<StockMovement> Movements);

public sealed record OrderLine(int ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record StockMovement(int Id, int ProductId, string Reason, int QuantityChange, int BalanceAfter, DateTimeOffset CreatedAt);

public sealed record ProductQuery(string? Category, bool? LowStock, string? Search);
public sealed record CreateProductRequest(string Sku, string Name, string Category, decimal Price, int OpeningStock, int ReorderLevel);
public sealed record AdjustStockRequest(int QuantityChange, string Reason);
public sealed record CreateOrderRequest(string CustomerName, string CustomerEmail, IReadOnlyCollection<CreateOrderLineRequest> Lines);
public sealed record CreateOrderLineRequest(int ProductId, int Quantity);
public sealed record UpdateOrderStatusRequest(string Status);

public sealed record ProductSummary(int Id, string Sku, string Name, string Category, decimal Price, int StockOnHand, int ReorderLevel, bool LowStock);
public sealed record LowStockItem(int ProductId, string Sku, string Name, int StockOnHand, int ReorderLevel, int SuggestedReorderQuantity);
public sealed record SalesReport(int TotalOrders, decimal Revenue, decimal VatCollected, int UnitsSold, IReadOnlyCollection<TopProduct> TopProducts);
public sealed record TopProduct(int ProductId, string Name, int UnitsSold, decimal Revenue);

public sealed class OperationResult<T>
{
    private OperationResult(T? value, IReadOnlyCollection<string> errors, bool notFound)
    {
        Value = value;
        Errors = errors;
        NotFound = notFound;
    }

    public T? Value { get; }
    public IReadOnlyCollection<string> Errors { get; }
    public bool NotFound { get; }
    public bool IsSuccess => Errors.Count == 0;

    public static OperationResult<T> Success(T value) => new(value, Array.Empty<string>(), false);
    public static OperationResult<T> Failure(params string[] errors) => new(default, errors, false);
    public static OperationResult<T> Missing(params string[] errors) => new(default, errors, true);
}
