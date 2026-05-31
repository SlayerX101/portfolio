using InventoryOrdersApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InventoryStore>();
builder.Services.AddSingleton<InventoryService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "Inventory Orders API",
    description = "Advanced inventory and order management API with stock rules, order totals, reservations, low-stock alerts, and sales reporting.",
    endpoints = new[]
    {
        "GET /products",
        "POST /products",
        "POST /products/{id}/stock",
        "POST /orders",
        "PATCH /orders/{id}/status",
        "GET /reports/low-stock",
        "GET /reports/sales"
    }
}));

var products = app.MapGroup("/products");

products.MapGet("/", (InventoryService service, string? category, bool? lowStock, string? search) =>
    Results.Ok(service.SearchProducts(new ProductQuery(category, lowStock, search))));

products.MapGet("/{id:int}", (InventoryService service, int id) =>
{
    var product = service.GetProduct(id);
    return product is null ? Results.NotFound(new { message = "Product not found." }) : Results.Ok(product);
});

products.MapPost("/", (InventoryService service, CreateProductRequest request) =>
{
    var result = service.CreateProduct(request);
    return result.IsSuccess ? Results.Created($"/products/{result.Value!.Id}", result.Value) : Results.BadRequest(new { errors = result.Errors });
});

products.MapPost("/{id:int}/stock", (InventoryService service, int id, AdjustStockRequest request) =>
{
    var result = service.AdjustStock(id, request);
    return ToHttpResult(result);
});

var orders = app.MapGroup("/orders");

orders.MapGet("/", (InventoryService service, string? status) => Results.Ok(service.SearchOrders(status)));

orders.MapGet("/{id:int}", (InventoryService service, int id) =>
{
    var order = service.GetOrder(id);
    return order is null ? Results.NotFound(new { message = "Order not found." }) : Results.Ok(order);
});

orders.MapPost("/", (InventoryService service, CreateOrderRequest request) =>
{
    var result = service.CreateOrder(request);
    return result.IsSuccess ? Results.Created($"/orders/{result.Value!.Id}", result.Value) : Results.BadRequest(new { errors = result.Errors });
});

orders.MapPatch("/{id:int}/status", (InventoryService service, int id, UpdateOrderStatusRequest request) =>
{
    var result = service.UpdateOrderStatus(id, request);
    return ToHttpResult(result);
});

app.MapGet("/reports/low-stock", (InventoryService service) => Results.Ok(service.GetLowStockReport()));
app.MapGet("/reports/sales", (InventoryService service) => Results.Ok(service.GetSalesReport()));

app.Run();

static IResult ToHttpResult<T>(OperationResult<T> result)
{
    if (result.IsSuccess)
    {
        return Results.Ok(result.Value);
    }

    return result.NotFound ? Results.NotFound(new { errors = result.Errors }) : Results.BadRequest(new { errors = result.Errors });
}
