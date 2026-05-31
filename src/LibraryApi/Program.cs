var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var books = new List<Book>
{
    new(1, "Clean Code", "Robert C. Martin", "Programming", true),
    new(2, "Head First C#", "Andrew Stellman", "Programming", true),
    new(3, "The Pragmatic Programmer", "David Thomas", "Programming", false)
};

var members = new List<Member>
{
    new(1, "Amina Jacobs", "amina@example.com"),
    new(2, "Sipho Mokoena", "sipho@example.com")
};

var loans = new List<Loan>
{
    new(1, 3, 2, DateOnly.FromDateTime(DateTime.Today.AddDays(-6)), DateOnly.FromDateTime(DateTime.Today.AddDays(8)), null)
};

var nextBookId = books.Max(book => book.Id) + 1;
var nextMemberId = members.Max(member => member.Id) + 1;
var nextLoanId = loans.Max(loan => loan.Id) + 1;

app.MapGet("/", () => Results.Ok(new
{
    name = "Library API",
    description = "A small library management API with books, members, loans, and returns.",
    endpoints = new[] { "GET /books", "POST /books", "GET /members", "POST /members", "POST /loans", "POST /loans/{id}/return", "GET /loans/active" }
}));

app.MapGet("/books", (string? search, bool? available) =>
{
    var query = books.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        query = query.Where(book =>
            book.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            book.Author.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    if (available is not null)
    {
        query = query.Where(book => book.IsAvailable == available);
    }

    return Results.Ok(query.OrderBy(book => book.Title));
});

app.MapPost("/books", (BookCreateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Author))
    {
        return Results.BadRequest("Title and author are required.");
    }

    var book = new Book(nextBookId++, request.Title.Trim(), request.Author.Trim(), request.Genre?.Trim() ?? "General", true);
    books.Add(book);
    return Results.Created($"/books/{book.Id}", book);
});

app.MapGet("/members", () => Results.Ok(members.OrderBy(member => member.Name)));

app.MapPost("/members", (MemberCreateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest("Name and email are required.");
    }

    var member = new Member(nextMemberId++, request.Name.Trim(), request.Email.Trim());
    members.Add(member);
    return Results.Created($"/members/{member.Id}", member);
});

app.MapPost("/loans", (LoanCreateRequest request) =>
{
    var bookIndex = books.FindIndex(book => book.Id == request.BookId);
    var member = members.FirstOrDefault(item => item.Id == request.MemberId);

    if (bookIndex < 0 || member is null)
    {
        return Results.BadRequest("Book and member must exist.");
    }

    if (!books[bookIndex].IsAvailable)
    {
        return Results.Conflict("Book is already on loan.");
    }

    books[bookIndex] = books[bookIndex] with { IsAvailable = false };
    var loan = new Loan(
        nextLoanId++,
        request.BookId,
        request.MemberId,
        DateOnly.FromDateTime(DateTime.Today),
        request.DueDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
        null);

    loans.Add(loan);
    return Results.Created($"/loans/{loan.Id}", loan);
});

app.MapPost("/loans/{id:int}/return", (int id) =>
{
    var index = loans.FindIndex(loan => loan.Id == id);
    if (index < 0)
    {
        return Results.NotFound();
    }

    var loan = loans[index];
    if (loan.ReturnedOn is not null)
    {
        return Results.Conflict("Loan has already been returned.");
    }

    loans[index] = loan with { ReturnedOn = DateOnly.FromDateTime(DateTime.Today) };
    var bookIndex = books.FindIndex(book => book.Id == loan.BookId);
    if (bookIndex >= 0)
    {
        books[bookIndex] = books[bookIndex] with { IsAvailable = true };
    }

    return Results.Ok(loans[index]);
});

app.MapGet("/loans/active", () =>
{
    var activeLoans = loans
        .Where(loan => loan.ReturnedOn is null)
        .Select(loan => new
        {
            loan.Id,
            book = books.FirstOrDefault(book => book.Id == loan.BookId)?.Title,
            member = members.FirstOrDefault(member => member.Id == loan.MemberId)?.Name,
            loan.BorrowedOn,
            loan.DueDate,
            overdue = loan.DueDate < DateOnly.FromDateTime(DateTime.Today)
        });

    return Results.Ok(activeLoans);
});

app.Run();

record Book(int Id, string Title, string Author, string Genre, bool IsAvailable);
record Member(int Id, string Name, string Email);
record Loan(int Id, int BookId, int MemberId, DateOnly BorrowedOn, DateOnly DueDate, DateOnly? ReturnedOn);
record BookCreateRequest(string Title, string Author, string? Genre);
record MemberCreateRequest(string Name, string Email);
record LoanCreateRequest(int BookId, int MemberId, DateOnly? DueDate);
