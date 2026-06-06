using GreenDeskOpsApi;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<GreenDeskStore>();
builder.Services.AddSingleton<GreenDeskService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "GreenDesk Ops API",
    description = "A dark-theme IT service desk and green asset operations API with tickets, SLA risk, assets, maintenance, activity, and dashboard analytics.",
    endpoints = new[]
    {
        "GET /dashboard",
        "GET /tickets",
        "POST /tickets",
        "PATCH /tickets/{id}/assign",
        "PATCH /tickets/{id}/status",
        "POST /tickets/{id}/worklogs",
        "GET /assets",
        "POST /assets",
        "PATCH /assets/{id}/health",
        "GET /maintenance",
        "POST /maintenance",
        "PATCH /maintenance/{id}/status"
    }
}));

app.MapGet("/dashboard", (GreenDeskService service) => Results.Ok(service.GetDashboard()));

var tickets = app.MapGroup("/tickets");

tickets.MapGet("/", (
    GreenDeskService service,
    string? status,
    string? priority,
    string? department,
    string? search,
    bool? slaRisk) =>
{
    var query = new TicketQuery(status, priority, department, search, slaRisk);
    return Results.Ok(service.SearchTickets(query));
});

tickets.MapGet("/{id:int}", (GreenDeskService service, int id) =>
{
    var ticket = service.GetTicket(id);
    return ticket is null ? Results.NotFound(new { message = "Ticket not found." }) : Results.Ok(ticket);
});

tickets.MapPost("/", (GreenDeskService service, CreateTicketRequest request) =>
{
    var result = service.CreateTicket(request);
    return result.IsSuccess
        ? Results.Created($"/tickets/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { errors = result.Errors });
});

tickets.MapPatch("/{id:int}/assign", (GreenDeskService service, int id, AssignTicketRequest request) =>
    ToHttpResult(service.AssignTicket(id, request)));

tickets.MapPatch("/{id:int}/status", (GreenDeskService service, int id, UpdateTicketStatusRequest request) =>
    ToHttpResult(service.UpdateTicketStatus(id, request)));

tickets.MapPost("/{id:int}/worklogs", (GreenDeskService service, int id, AddWorkLogRequest request) =>
    ToHttpResult(service.AddWorkLog(id, request)));

var assets = app.MapGroup("/assets");

assets.MapGet("/", (GreenDeskService service, string? health, string? category, string? search) =>
{
    var query = new AssetQuery(health, category, search);
    return Results.Ok(service.SearchAssets(query));
});

assets.MapGet("/{id:int}", (GreenDeskService service, int id) =>
{
    var asset = service.GetAsset(id);
    return asset is null ? Results.NotFound(new { message = "Asset not found." }) : Results.Ok(asset);
});

assets.MapPost("/", (GreenDeskService service, CreateAssetRequest request) =>
{
    var result = service.CreateAsset(request);
    return result.IsSuccess
        ? Results.Created($"/assets/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { errors = result.Errors });
});

assets.MapPatch("/{id:int}/health", (GreenDeskService service, int id, UpdateAssetHealthRequest request) =>
    ToHttpResult(service.UpdateAssetHealth(id, request)));

var maintenance = app.MapGroup("/maintenance");

maintenance.MapGet("/", (GreenDeskService service, string? status) =>
{
    Enum.TryParse(status, true, out MaintenanceStatus parsed);
    return Results.Ok(service.SearchMaintenance(string.IsNullOrWhiteSpace(status) ? null : parsed));
});

maintenance.MapPost("/", (GreenDeskService service, ScheduleMaintenanceRequest request) =>
{
    var result = service.ScheduleMaintenance(request);
    return result.IsSuccess
        ? Results.Created($"/maintenance/{result.Value!.Id}", result.Value)
        : result.NotFound
            ? Results.NotFound(new { errors = result.Errors })
            : Results.BadRequest(new { errors = result.Errors });
});

maintenance.MapPatch("/{id:int}/status", (GreenDeskService service, int id, UpdateMaintenanceStatusRequest request) =>
    ToHttpResult(service.UpdateMaintenanceStatus(id, request)));

app.Run();

static IResult ToHttpResult<T>(OperationResult<T> result)
{
    if (result.IsSuccess)
    {
        return Results.Ok(result.Value);
    }

    return result.NotFound
        ? Results.NotFound(new { errors = result.Errors })
        : Results.BadRequest(new { errors = result.Errors });
}
