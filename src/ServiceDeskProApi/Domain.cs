namespace ServiceDeskProApi;

public enum TicketStatus
{
    New,
    Assigned,
    InProgress,
    WaitingForCustomer,
    Resolved,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public sealed record SupportTicket(
    int Id,
    string Title,
    string Description,
    string CustomerName,
    string CustomerEmail,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt,
    DateTimeOffset? ResolvedAt,
    List<TicketComment> Comments,
    List<TicketAuditEvent> AuditTrail);

public sealed record TicketComment(int Id, string Author, string Message, DateTimeOffset CreatedAt, bool IsInternal);
public sealed record TicketAuditEvent(int Id, string Action, string Actor, string Details, DateTimeOffset CreatedAt);

public sealed record CreateTicketRequest(
    string Title,
    string Description,
    string CustomerName,
    string CustomerEmail,
    string Priority);

public sealed record AssignTicketRequest(string AgentName, string Actor);
public sealed record UpdateTicketStatusRequest(string Status, string Actor, string? ResolutionNote);
public sealed record AddTicketCommentRequest(string Author, string Message, bool IsInternal);
public sealed record TicketQuery(string? Status, string? Priority, string? AssignedTo, string? Search, bool? Breached);

public sealed record TicketSummary(
    int Id,
    string Title,
    string CustomerName,
    string Priority,
    string Status,
    string? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt,
    bool SlaBreached,
    int CommentCount);

public sealed record DashboardMetric(
    int TotalTickets,
    int OpenTickets,
    int BreachedTickets,
    double AverageResolutionHours,
    IReadOnlyCollection<StatusCount> ByStatus,
    IReadOnlyCollection<PriorityCount> ByPriority,
    IReadOnlyCollection<AgentLoad> AgentLoads);

public sealed record StatusCount(string Status, int Count);
public sealed record PriorityCount(string Priority, int Count);
public sealed record AgentLoad(string Agent, int OpenTickets);

public sealed class OperationResult<T>
{
    private OperationResult(T? value, IReadOnlyCollection<string> errors, bool notFound)
    {
        Value = value;
        Errors = errors;
        NotFound = notFound;
    }

    public T? Value { get; }
    public IReadOnlyCollection<string> Errors { get; }
    public bool NotFound { get; }
    public bool IsSuccess => Errors.Count == 0;

    public static OperationResult<T> Success(T value) => new(value, Array.Empty<string>(), false);
    public static OperationResult<T> Failure(params string[] errors) => new(default, errors, false);
    public static OperationResult<T> Missing(params string[] errors) => new(default, errors, true);
}
