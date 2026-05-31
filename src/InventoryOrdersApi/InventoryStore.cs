namespace InventoryOrdersApi;

public sealed class InventoryStore
{
    private readonly List<Product> products;
    private readonly List<SalesOrder> orders;
    private int nextProductId = 5;
    private int nextOrderId = 3;
    private int nextMovementId = 7;

    public InventoryStore()
    {
        products =
        [
            new Product(1, "DEV-KEY-001", "Mechanical Keyboard", "Hardware", 1299.99m, 14, 5, true),
            new Product(2, "DEV-MSE-002", "Wireless Mouse", "Hardware", 449.99m, 4, 6, true),
            new Product(3, "CRS-CSHARP", "C# Web API Course", "Education", 699.00m, 100, 20, true),
            new Product(4, "HST-BASIC", "Portfolio Hosting Setup", "Service", 999.00m, 8, 3, true)
        ];

        var now = DateTimeOffset.UtcNow;
        orders =
        [
            new SalesOrder(
                1,
                "Amina Jacobs",
                "amina@example.com",
                OrderStatus.Shipped,
                now.AddDays(-4),
                1749.98m,
                262.50m,
                2012.48m,
                [new OrderLine(1, "DEV-KEY-001", "Mechanical Keyboard", 1, 1299.99m, 1299.99m),
                 new OrderLine(2, "DEV-MSE-002", "Wireless Mouse", 1, 449.99m, 449.99m)],
                []),
            new SalesOrder(
                2,
                "Sipho Mokoena",
                "sipho@example.com",
                OrderStatus.Confirmed,
                now.AddDays(-1),
                699.00m,
                104.85m,
                803.85m,
                [new OrderLine(3, "CRS-CSHARP", "C# Web API Course", 1, 699.00m, 699.00m)],
                [])
        ];
    }

    public IReadOnlyCollection<Product> Products => products;
    public IReadOnlyCollection<SalesOrder> Orders => orders;

    public int NextProductId() => nextProductId++;
    public int NextOrderId() => nextOrderId++;
    public int NextMovementId() => nextMovementId++;

    public void AddProduct(Product product) => products.Add(product);
    public void AddOrder(SalesOrder order) => orders.Add(order);
    public void ReplaceProduct(Product product)
    {
        var index = products.FindIndex(item => item.Id == product.Id);
        products[index] = product;
    }

    public void ReplaceOrder(SalesOrder order)
    {
        var index = orders.FindIndex(item => item.Id == order.Id);
        orders[index] = order;
    }
}
