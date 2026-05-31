using ServiceDeskProApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TicketStore>();
builder.Services.AddSingleton<TicketService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    name = "Service Desk Pro API",
    description = "Advanced ticketing API with SLA calculations, assignment workflow, comments, audit trail, search, and dashboard metrics.",
    endpoints = new[]
    {
        "GET /tickets",
        "GET /tickets/{id}",
        "POST /tickets",
        "PATCH /tickets/{id}/assign",
        "PATCH /tickets/{id}/status",
        "POST /tickets/{id}/comments",
        "GET /tickets/{id}/audit",
        "GET /dashboard"
    }
}));

var tickets = app.MapGroup("/tickets");

tickets.MapGet("/", (
    TicketService service,
    string? status,
    string? priority,
    string? assignedTo,
    string? search,
    bool? breached) =>
{
    var query = new TicketQuery(status, priority, assignedTo, search, breached);
    return Results.Ok(service.Search(query));
});

tickets.MapGet("/{id:int}", (TicketService service, int id) =>
{
    var ticket = service.Get(id);
    return ticket is null ? Results.NotFound(new { message = "Ticket not found." }) : Results.Ok(ticket);
});

tickets.MapPost("/", (TicketService service, CreateTicketRequest request) =>
{
    var result = service.Create(request);
    return result.IsSuccess
        ? Results.Created($"/tickets/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { errors = result.Errors });
});

tickets.MapPatch("/{id:int}/assign", (TicketService service, int id, AssignTicketRequest request) =>
{
    var result = service.Assign(id, request);
    return ToHttpResult(result);
});

tickets.MapPatch("/{id:int}/status", (TicketService service, int id, UpdateTicketStatusRequest request) =>
{
    var result = service.UpdateStatus(id, request);
    return ToHttpResult(result);
});

tickets.MapPost("/{id:int}/comments", (TicketService service, int id, AddTicketCommentRequest request) =>
{
    var result = service.AddComment(id, request);
    return ToHttpResult(result);
});

tickets.MapGet("/{id:int}/audit", (TicketService service, int id) =>
{
    var result = service.GetAuditTrail(id);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { errors = result.Errors });
});

app.MapGet("/dashboard", (TicketService service) => Results.Ok(service.GetDashboard()));

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
