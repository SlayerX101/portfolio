namespace GreenDeskOpsApi;

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TicketStatus
{
    New,
    Assigned,
    InProgress,
    WaitingForUser,
    Resolved,
    Closed
}

public enum AssetHealth
{
    Healthy,
    Watch,
    NeedsRepair,
    Retired
}

public enum MaintenanceStatus
{
    Planned,
    InProgress,
    Completed,
    Cancelled
}

public sealed record SupportTicket(
    int Id,
    string Title,
    string Description,
    string Department,
    string Requester,
    TicketPriority Priority,
    TicketStatus Status,
    string? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset SlaDueAt,
    DateTimeOffset? ResolvedAt,
    List<TicketWorkLog> WorkLogs);

public sealed record TicketWorkLog(
    int Id,
    string Actor,
    string Note,
    DateTimeOffset CreatedAt,
    int MinutesSpent);

public sealed record HardwareAsset(
    int Id,
    string AssetTag,
    string Name,
    string Category,
    string AssignedTo,
    string Location,
    AssetHealth Health,
    DateOnly PurchaseDate,
    decimal ReplacementCost,
    decimal EnergyRating);

public sealed record MaintenanceTask(
    int Id,
    int AssetId,
    string AssetName,
    string Task,
    MaintenanceStatus Status,
    DateOnly DueDate,
    string Owner,
    string Risk);

public sealed record ActivityEvent(
    int Id,
    string EventType,
    string Message,
    DateTimeOffset CreatedAt);

public sealed record CreateTicketRequest(
    string Title,
    string Description,
    string Department,
    string Requester,
    string Priority);

public sealed record AssignTicketRequest(string AgentName);

public sealed record UpdateTicketStatusRequest(string Status, string? ResolutionNote);

public sealed record AddWorkLogRequest(string Actor, string Note, int MinutesSpent);

public sealed record CreateAssetRequest(
    string AssetTag,
    string Name,
    string Category,
    string AssignedTo,
    string Location,
    decimal ReplacementCost,
    decimal EnergyRating);

public sealed record UpdateAssetHealthRequest(string Health);

public sealed record ScheduleMaintenanceRequest(
    int AssetId,
    string Task,
    DateOnly DueDate,
    string Owner,
    string Risk);

public sealed record UpdateMaintenanceStatusRequest(string Status);

public sealed record TicketQuery(string? Status, string? Priority, string? Department, string? Search, bool? SlaRisk);

public sealed record AssetQuery(string? Health, string? Category, string? Search);

public sealed record TicketSummary(
    int Id,
    string Title,
    string Department,
    string Priority,
    string Status,
    string? AssignedTo,
    DateTimeOffset SlaDueAt,
    bool SlaRisk,
    int WorkLogCount,
    int MinutesLogged);

public sealed record DashboardSummary(
    int OpenTickets,
    int SlaRiskTickets,
    int AssetsOnWatch,
    int PlannedMaintenance,
    decimal AssetValue,
    decimal EstimatedMonthlyEnergySaving,
    IReadOnlyCollection<StatusCount> TicketsByStatus,
    IReadOnlyCollection<DepartmentCount> TicketsByDepartment,
    IReadOnlyCollection<ActivityEvent> RecentActivity);

public sealed record StatusCount(string Status, int Count);

public sealed record DepartmentCount(string Department, int Count);

public sealed record OperationResult<T>(bool IsSuccess, T? Value, bool NotFound, IReadOnlyCollection<string> Errors)
{
    public static OperationResult<T> Success(T value) => new(true, value, false, []);
    public static OperationResult<T> Missing(string error) => new(false, default, true, [error]);
    public static OperationResult<T> Failure(params string[] errors) => new(false, default, false, errors);
    public static OperationResult<T> Failure(IReadOnlyCollection<string> errors) => new(false, default, false, errors);
}
