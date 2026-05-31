var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var applications = new List<JobApplication>
{
    new(1, "Junior C# Developer", "BrightApps", "Applied", DateOnly.FromDateTime(DateTime.Today.AddDays(-3)), DateOnly.FromDateTime(DateTime.Today.AddDays(4)), "Submitted portfolio link."),
    new(2, "Graduate Software Developer", "CloudBridge", "Interview", DateOnly.FromDateTime(DateTime.Today.AddDays(-9)), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), "Prepare Web API talking points."),
    new(3, "Intern Backend Developer", "DataNest", "Rejected", DateOnly.FromDateTime(DateTime.Today.AddDays(-20)), null, "Improve SQL examples.")
};

var nextId = applications.Max(application => application.Id) + 1;

app.MapGet("/", () => Results.Ok(new
{
    name = "Job Application Tracker API",
    description = "A job-search API for tracking roles, companies, statuses, and follow-up dates.",
    endpoints = new[] { "GET /applications", "GET /applications/{id}", "POST /applications", "PATCH /applications/{id}/status", "GET /applications/follow-ups", "GET /applications/stats" }
}));

app.MapGet("/applications", (string? status, string? company) =>
{
    var query = applications.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(application => application.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

    if (!string.IsNullOrWhiteSpace(company))
    {
        query = query.Where(application => application.Company.Contains(company, StringComparison.OrdinalIgnoreCase));
    }

    return Results.Ok(query.OrderByDescending(application => application.AppliedOn));
});

app.MapGet("/applications/{id:int}", (int id) =>
{
    var application = applications.FirstOrDefault(item => item.Id == id);
    return application is null ? Results.NotFound() : Results.Ok(application);
});

app.MapPost("/applications", (JobApplicationCreateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Role) || string.IsNullOrWhiteSpace(request.Company))
    {
        return Results.BadRequest("Role and company are required.");
    }

    var application = new JobApplication(
        nextId++,
        request.Role.Trim(),
        request.Company.Trim(),
        request.Status?.Trim() ?? "Applied",
        request.AppliedOn ?? DateOnly.FromDateTime(DateTime.Today),
        request.FollowUpOn,
        request.Notes?.Trim() ?? string.Empty);

    applications.Add(application);
    return Results.Created($"/applications/{application.Id}", application);
});

app.MapPatch("/applications/{id:int}/status", (int id, StatusUpdateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Status))
    {
        return Results.BadRequest("Status is required.");
    }

    var index = applications.FindIndex(application => application.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    applications[index] = applications[index] with
    {
        Status = request.Status.Trim(),
        FollowUpOn = request.FollowUpOn ?? applications[index].FollowUpOn,
        Notes = request.Notes?.Trim() ?? applications[index].Notes
    };

    return Results.Ok(applications[index]);
});

app.MapGet("/applications/follow-ups", () =>
{
    var upcoming = applications
        .Where(application => application.FollowUpOn is not null && application.Status != "Rejected")
        .OrderBy(application => application.FollowUpOn)
        .Select(application => new
        {
            application.Id,
            application.Role,
            application.Company,
            application.FollowUpOn,
            dueSoon = application.FollowUpOn <= DateOnly.FromDateTime(DateTime.Today.AddDays(2))
        });

    return Results.Ok(upcoming);
});

app.MapGet("/applications/stats", () =>
{
    var byStatus = applications
        .GroupBy(application => application.Status)
        .Select(group => new { status = group.Key, count = group.Count() })
        .OrderBy(item => item.status);

    return Results.Ok(new
    {
        total = applications.Count,
        active = applications.Count(application => application.Status is not "Rejected" and not "Offer"),
        byStatus
    });
});

app.Run();

record JobApplication(int Id, string Role, string Company, string Status, DateOnly AppliedOn, DateOnly? FollowUpOn, string Notes);
record JobApplicationCreateRequest(string Role, string Company, string? Status, DateOnly? AppliedOn, DateOnly? FollowUpOn, string? Notes);
record StatusUpdateRequest(string Status, DateOnly? FollowUpOn, string? Notes);
