var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var expenses = new List<Expense>
{
    new(1, "Groceries", "Food", 785.60m, DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), "Weekly essentials"),
    new(2, "Bus pass", "Transport", 320.00m, DateOnly.FromDateTime(DateTime.Today.AddDays(-4)), "Commute"),
    new(3, "Online C# course", "Education", 249.99m, DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), "Developer learning")
};

var nextId = expenses.Max(expense => expense.Id) + 1;

app.MapGet("/", () => Results.Ok(new
{
    name = "Expense Tracker API",
    description = "A budgeting API that records expenses and returns category totals.",
    endpoints = new[] { "GET /expenses", "GET /expenses/{id}", "POST /expenses", "DELETE /expenses/{id}", "GET /reports/monthly", "GET /reports/categories" }
}));

app.MapGet("/expenses", (string? category, DateOnly? from, DateOnly? to) =>
{
    var query = expenses.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(expense => expense.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    if (from is not null)
    {
        query = query.Where(expense => expense.Date >= from);
    }

    if (to is not null)
    {
        query = query.Where(expense => expense.Date <= to);
    }

    return Results.Ok(query.OrderByDescending(expense => expense.Date));
});

app.MapGet("/expenses/{id:int}", (int id) =>
{
    var expense = expenses.FirstOrDefault(item => item.Id == id);
    return expense is null ? Results.NotFound() : Results.Ok(expense);
});

app.MapPost("/expenses", (ExpenseCreateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category))
    {
        return Results.BadRequest("Name and category are required.");
    }

    if (request.Amount <= 0)
    {
        return Results.BadRequest("Amount must be greater than zero.");
    }

    var expense = new Expense(
        nextId++,
        request.Name.Trim(),
        request.Category.Trim(),
        decimal.Round(request.Amount, 2),
        request.Date ?? DateOnly.FromDateTime(DateTime.Today),
        request.Notes?.Trim() ?? string.Empty);

    expenses.Add(expense);
    return Results.Created($"/expenses/{expense.Id}", expense);
});

app.MapDelete("/expenses/{id:int}", (int id) =>
{
    var removed = expenses.RemoveAll(expense => expense.Id == id);
    return removed == 0 ? Results.NotFound() : Results.NoContent();
});

app.MapGet("/reports/categories", () =>
{
    var report = expenses
        .GroupBy(expense => expense.Category)
        .Select(group => new
        {
            category = group.Key,
            total = group.Sum(expense => expense.Amount),
            count = group.Count()
        })
        .OrderByDescending(item => item.total);

    return Results.Ok(report);
});

app.MapGet("/reports/monthly", (int? year, int? month) =>
{
    var selectedYear = year ?? DateTime.Today.Year;
    var selectedMonth = month ?? DateTime.Today.Month;

    var monthlyExpenses = expenses
        .Where(expense => expense.Date.Year == selectedYear && expense.Date.Month == selectedMonth)
        .ToList();

    return Results.Ok(new
    {
        year = selectedYear,
        month = selectedMonth,
        total = monthlyExpenses.Sum(expense => expense.Amount),
        average = monthlyExpenses.Count == 0 ? 0 : monthlyExpenses.Average(expense => expense.Amount),
        count = monthlyExpenses.Count
    });
});

app.Run();

record Expense(int Id, string Name, string Category, decimal Amount, DateOnly Date, string Notes);
record ExpenseCreateRequest(string Name, string Category, decimal Amount, DateOnly? Date, string? Notes);
