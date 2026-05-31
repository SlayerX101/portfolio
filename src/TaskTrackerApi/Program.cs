var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var tasks = new List<TaskItem>
{
    new(1, "Build portfolio API", "Create project endpoints and seed data.", "In Progress", "High", DateOnly.FromDateTime(DateTime.Today.AddDays(3))),
    new(2, "Update CV", "Add C# projects and measurable achievements.", "Todo", "Medium", DateOnly.FromDateTime(DateTime.Today.AddDays(5))),
    new(3, "Practice SQL joins", "Complete exercises with INNER and LEFT joins.", "Done", "Low", DateOnly.FromDateTime(DateTime.Today.AddDays(-1)))
};

var nextId = tasks.Max(task => task.Id) + 1;

app.MapGet("/", () => Results.Ok(new
{
    name = "Task Tracker API",
    description = "A junior-friendly C# Web API for managing tasks, priorities, statuses, and due dates.",
    endpoints = new[] { "GET /tasks", "GET /tasks/{id}", "POST /tasks", "PUT /tasks/{id}", "DELETE /tasks/{id}", "GET /tasks/summary" }
}));

app.MapGet("/tasks", (string? status, string? priority) =>
{
    var query = tasks.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(task => task.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

    if (!string.IsNullOrWhiteSpace(priority))
    {
        query = query.Where(task => task.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase));
    }

    return Results.Ok(query.OrderBy(task => task.DueDate).ThenBy(task => task.Priority));
});

app.MapGet("/tasks/{id:int}", (int id) =>
{
    var task = tasks.FirstOrDefault(item => item.Id == id);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

app.MapPost("/tasks", (TaskCreateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest("Title is required.");
    }

    var task = new TaskItem(
        nextId++,
        request.Title.Trim(),
        request.Description?.Trim() ?? string.Empty,
        request.Status?.Trim() ?? "Todo",
        request.Priority?.Trim() ?? "Medium",
        request.DueDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7)));

    tasks.Add(task);
    return Results.Created($"/tasks/{task.Id}", task);
});

app.MapPut("/tasks/{id:int}", (int id, TaskUpdateRequest request) =>
{
    var index = tasks.FindIndex(task => task.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    var current = tasks[index];
    var updated = current with
    {
        Title = string.IsNullOrWhiteSpace(request.Title) ? current.Title : request.Title.Trim(),
        Description = request.Description?.Trim() ?? current.Description,
        Status = request.Status?.Trim() ?? current.Status,
        Priority = request.Priority?.Trim() ?? current.Priority,
        DueDate = request.DueDate ?? current.DueDate
    };

    tasks[index] = updated;
    return Results.Ok(updated);
});

app.MapDelete("/tasks/{id:int}", (int id) =>
{
    var removed = tasks.RemoveAll(task => task.Id == id);
    return removed == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapGet("/tasks/summary", () =>
{
    var byStatus = tasks
        .GroupBy(task => task.Status)
        .Select(group => new { status = group.Key, count = group.Count() })
        .OrderBy(item => item.status);

    var overdue = tasks.Count(task => task.Status != "Done" && task.DueDate < DateOnly.FromDateTime(DateTime.Today));

    return Results.Ok(new { total = tasks.Count, overdue, byStatus });
});

app.Run();

record TaskItem(int Id, string Title, string Description, string Status, string Priority, DateOnly DueDate);
record TaskCreateRequest(string Title, string? Description, string? Status, string? Priority, DateOnly? DueDate);
record TaskUpdateRequest(string? Title, string? Description, string? Status, string? Priority, DateOnly? DueDate);
