var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var entries = new List<WeatherEntry>
{
    new(1, "Cape Town", DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), 21.5, 64, "Partly cloudy", "Good day for a short walk."),
    new(2, "Johannesburg", DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), 18.2, 58, "Clear", "Cool evening."),
    new(3, "Durban", DateOnly.FromDateTime(DateTime.Today), 26.8, 77, "Humid", "High humidity after rain.")
};

var nextId = entries.Max(entry => entry.Id) + 1;

app.MapGet("/", () => Results.Ok(new
{
    name = "Weather Journal API",
    description = "A Web API for logging weather observations by city and reviewing simple climate summaries.",
    endpoints = new[] { "GET /entries", "GET /entries/{id}", "POST /entries", "DELETE /entries/{id}", "GET /cities/{city}/summary" }
}));

app.MapGet("/entries", (string? city, DateOnly? from, DateOnly? to) =>
{
    var query = entries.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(city))
    {
        query = query.Where(entry => entry.City.Equals(city, StringComparison.OrdinalIgnoreCase));
    }

    if (from is not null)
    {
        query = query.Where(entry => entry.Date >= from);
    }

    if (to is not null)
    {
        query = query.Where(entry => entry.Date <= to);
    }

    return Results.Ok(query.OrderByDescending(entry => entry.Date));
});

app.MapGet("/entries/{id:int}", (int id) =>
{
    var entry = entries.FirstOrDefault(item => item.Id == id);
    return entry is null ? Results.NotFound() : Results.Ok(entry);
});

app.MapPost("/entries", (WeatherEntryCreateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Condition))
    {
        return Results.BadRequest("City and condition are required.");
    }

    if (request.HumidityPercent is < 0 or > 100)
    {
        return Results.BadRequest("Humidity must be between 0 and 100.");
    }

    var entry = new WeatherEntry(
        nextId++,
        request.City.Trim(),
        request.Date ?? DateOnly.FromDateTime(DateTime.Today),
        request.TemperatureC,
        request.HumidityPercent,
        request.Condition.Trim(),
        request.Notes?.Trim() ?? string.Empty);

    entries.Add(entry);
    return Results.Created($"/entries/{entry.Id}", entry);
});

app.MapDelete("/entries/{id:int}", (int id) =>
{
    var removed = entries.RemoveAll(entry => entry.Id == id);
    return removed == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapGet("/cities/{city}/summary", (string city) =>
{
    var cityEntries = entries
        .Where(entry => entry.City.Equals(city, StringComparison.OrdinalIgnoreCase))
        .OrderBy(entry => entry.Date)
        .ToList();

    if (cityEntries.Count == 0)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        city,
        entries = cityEntries.Count,
        firstLogged = cityEntries.First().Date,
        latestLogged = cityEntries.Last().Date,
        averageTemperatureC = Math.Round(cityEntries.Average(entry => entry.TemperatureC), 1),
        averageHumidity = Math.Round(cityEntries.Average(entry => entry.HumidityPercent), 1),
        warmest = cityEntries.MaxBy(entry => entry.TemperatureC)
    });
});

app.Run();

record WeatherEntry(int Id, string City, DateOnly Date, double TemperatureC, int HumidityPercent, string Condition, string Notes);
record WeatherEntryCreateRequest(string City, DateOnly? Date, double TemperatureC, int HumidityPercent, string Condition, string? Notes);
