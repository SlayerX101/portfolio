namespace InventoryOrdersApi;

public sealed class InventoryService(InventoryStore store)
{
    private const decimal VatRate = 0.15m;

    public IReadOnlyCollection<ProductSummary> SearchProducts(ProductQuery query)
    {
        var products = store.Products.Where(product => product.Active);

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            products = products.Where(product => product.Category.Equals(query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            products = products.Where(product =>
                product.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                product.Sku.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (query.LowStock is not null)
        {
            products = products.Where(product => IsLowStock(product) == query.LowStock);
        }

        return products.OrderBy(product => product.Category).ThenBy(product => product.Name).Select(ToSummary).ToList();
    }

    public Product? GetProduct(int id) => store.Products.FirstOrDefault(product => product.Id == id);
    public SalesOrder? GetOrder(int id) => store.Orders.FirstOrDefault(order => order.Id == id);

    public OperationResult<Product> CreateProduct(CreateProductRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Sku) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category))
        {
            errors.Add("SKU, name, and category are required.");
        }

        if (request.Price <= 0)
        {
            errors.Add("Price must be greater than zero.");
        }

        if (request.OpeningStock < 0 || request.ReorderLevel < 0)
        {
            errors.Add("Opening stock and reorder level cannot be negative.");
        }

        if (store.Products.Any(product => product.Sku.Equals(request.Sku, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("SKU already exists.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<Product>.Failure([.. errors]);
        }

        var product = new Product(
            store.NextProductId(),
            request.Sku.Trim().ToUpperInvariant(),
            request.Name.Trim(),
            request.Category.Trim(),
            decimal.Round(request.Price, 2),
            request.OpeningStock,
            request.ReorderLevel,
            true);

        store.AddProduct(product);
        return OperationResult<Product>.Success(product);
    }

    public OperationResult<Product> AdjustStock(int id, AdjustStockRequest request)
    {
        var product = GetProduct(id);
        if (product is null)
        {
            return OperationResult<Product>.Missing("Product not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return OperationResult<Product>.Failure("Stock adjustment reason is required.");
        }

        var newBalance = product.StockOnHand + request.QuantityChange;
        if (newBalance < 0)
        {
            return OperationResult<Product>.Failure("Stock balance cannot go below zero.");
        }

        var updated = product with { StockOnHand = newBalance };
        store.ReplaceProduct(updated);
        return OperationResult<Product>.Success(updated);
    }

    public OperationResult<SalesOrder> CreateOrder(CreateOrderRequest request)
    {
        var errors = ValidateOrder(request);
        if (errors.Count > 0)
        {
            return OperationResult<SalesOrder>.Failure([.. errors]);
        }

        var lines = new List<OrderLine>();
        var movements = new List<StockMovement>();

        foreach (var requestLine in request.Lines)
        {
            var product = GetProduct(requestLine.ProductId)!;
            var updatedStock = product.StockOnHand - requestLine.Quantity;
            store.ReplaceProduct(product with { StockOnHand = updatedStock });

            var lineTotal = decimal.Round(product.Price * requestLine.Quantity, 2);
            lines.Add(new OrderLine(product.Id, product.Sku, product.Name, requestLine.Quantity, product.Price, lineTotal));
            movements.Add(new StockMovement(store.NextMovementId(), product.Id, "Order reservation", -requestLine.Quantity, updatedStock, DateTimeOffset.UtcNow));
        }

        var subtotal = lines.Sum(line => line.LineTotal);
        var vat = decimal.Round(subtotal * VatRate, 2);
        var order = new SalesOrder(
            store.NextOrderId(),
            request.CustomerName.Trim(),
            request.CustomerEmail.Trim(),
            OrderStatus.Confirmed,
            DateTimeOffset.UtcNow,
            subtotal,
            vat,
            subtotal + vat,
            lines,
            movements);

        store.AddOrder(order);
        return OperationResult<SalesOrder>.Success(order);
    }

    public IReadOnlyCollection<SalesOrder> SearchOrders(string? status)
    {
        var orders = store.Orders.AsEnumerable();
        if (Enum.TryParse(status, true, out OrderStatus parsed))
        {
            orders = orders.Where(order => order.Status == parsed);
        }

        return orders.OrderByDescending(order => order.CreatedAt).ToList();
    }

    public OperationResult<SalesOrder> UpdateOrderStatus(int id, UpdateOrderStatusRequest request)
    {
        var order = GetOrder(id);
        if (order is null)
        {
            return OperationResult<SalesOrder>.Missing("Order not found.");
        }

        if (!Enum.TryParse(request.Status, true, out OrderStatus newStatus))
        {
            return OperationResult<SalesOrder>.Failure("Invalid order status.");
        }

        if (!CanTransition(order.Status, newStatus))
        {
            return OperationResult<SalesOrder>.Failure($"Cannot transition from {order.Status} to {newStatus}.");
        }

        if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
        {
            foreach (var line in order.Lines)
            {
                var product = GetProduct(line.ProductId)!;
                store.ReplaceProduct(product with { StockOnHand = product.StockOnHand + line.Quantity });
                order.Movements.Add(new StockMovement(store.NextMovementId(), line.ProductId, "Order cancelled - stock released", line.Quantity, product.StockOnHand + line.Quantity, DateTimeOffset.UtcNow));
            }
        }

        var updated = order with { Status = newStatus };
        store.ReplaceOrder(updated);
        return OperationResult<SalesOrder>.Success(updated);
    }

    public IReadOnlyCollection<LowStockItem> GetLowStockReport() =>
        store.Products
            .Where(IsLowStock)
            .OrderBy(product => product.StockOnHand)
            .Select(product => new LowStockItem(product.Id, product.Sku, product.Name, product.StockOnHand, product.ReorderLevel, Math.Max(product.ReorderLevel * 2 - product.StockOnHand, 1)))
            .ToList();

    public SalesReport GetSalesReport()
    {
        var activeOrders = store.Orders.Where(order => order.Status is OrderStatus.Confirmed or OrderStatus.Packed or OrderStatus.Shipped).ToList();
        var allLines = activeOrders.SelectMany(order => order.Lines).ToList();

        var topProducts = allLines
            .GroupBy(line => new { line.ProductId, line.ProductName })
            .Select(group => new TopProduct(group.Key.ProductId, group.Key.ProductName, group.Sum(line => line.Quantity), group.Sum(line => line.LineTotal)))
            .OrderByDescending(product => product.Revenue)
            .ToList();

        return new SalesReport(
            activeOrders.Count,
            activeOrders.Sum(order => order.Total),
            activeOrders.Sum(order => order.Vat),
            allLines.Sum(line => line.Quantity),
            topProducts);
    }

    private List<string> ValidateOrder(CreateOrderRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            errors.Add("Customer name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !request.CustomerEmail.Contains('@'))
        {
            errors.Add("A valid customer email is required.");
        }

        if (request.Lines.Count == 0)
        {
            errors.Add("At least one order line is required.");
        }

        foreach (var line in request.Lines)
        {
            var product = GetProduct(line.ProductId);
            if (product is null)
            {
                errors.Add($"Product {line.ProductId} does not exist.");
                continue;
            }

            if (line.Quantity <= 0)
            {
                errors.Add($"Quantity for {product.Name} must be greater than zero.");
            }

            if (product.StockOnHand < line.Quantity)
            {
                errors.Add($"Insufficient stock for {product.Name}. Available: {product.StockOnHand}.");
            }
        }

        return errors;
    }

    private static ProductSummary ToSummary(Product product) =>
        new(product.Id, product.Sku, product.Name, product.Category, product.Price, product.StockOnHand, product.ReorderLevel, IsLowStock(product));

    private static bool IsLowStock(Product product) => product.StockOnHand <= product.ReorderLevel;

    private static bool CanTransition(OrderStatus from, OrderStatus to) =>
        from == to ||
        (from, to) is
        (OrderStatus.Draft, OrderStatus.Confirmed) or
        (OrderStatus.Confirmed, OrderStatus.Packed) or
        (OrderStatus.Packed, OrderStatus.Shipped) or
        (OrderStatus.Confirmed, OrderStatus.Cancelled) or
        (OrderStatus.Packed, OrderStatus.Cancelled);
}
