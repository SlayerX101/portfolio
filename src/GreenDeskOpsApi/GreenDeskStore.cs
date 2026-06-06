namespace GreenDeskOpsApi;

public sealed class GreenDeskStore
{
    private int _nextTicketId = 4;
    private int _nextWorkLogId = 4;
    private int _nextAssetId = 5;
    private int _nextMaintenanceId = 4;
    private int _nextActivityId = 5;

    public List<SupportTicket> Tickets { get; } =
    [
        new(
            1,
            "VPN access fails after password reset",
            "Finance user cannot connect to the VPN after resetting their password.",
            "Finance",
            "Amina Jacobs",
            TicketPriority.High,
            TicketStatus.InProgress,
            "Oageng",
            DateTimeOffset.UtcNow.AddHours(-8),
            DateTimeOffset.UtcNow.AddHours(4),
            null,
            [new TicketWorkLog(1, "Oageng", "Confirmed account is active and reset VPN profile.", DateTimeOffset.UtcNow.AddHours(-4), 35)]),
        new(
            2,
            "Reception tablet battery swelling",
            "Front desk check-in tablet is overheating and needs replacement review.",
            "Front Desk",
            "Kefilewe",
            TicketPriority.Critical,
            TicketStatus.Assigned,
            "Infrastructure",
            DateTimeOffset.UtcNow.AddHours(-2),
            DateTimeOffset.UtcNow.AddHours(2),
            null,
            [new TicketWorkLog(2, "Infrastructure", "Device isolated and replacement asset checked.", DateTimeOffset.UtcNow.AddHours(-1), 25)]),
        new(
            3,
            "New starter laptop setup",
            "Prepare laptop, accounts, email, browser bookmarks, and security checklist.",
            "Operations",
            "Johan",
            TicketPriority.Medium,
            TicketStatus.Resolved,
            "Oageng",
            DateTimeOffset.UtcNow.AddDays(-2),
            DateTimeOffset.UtcNow.AddHours(-8),
            DateTimeOffset.UtcNow.AddHours(-10),
            [new TicketWorkLog(3, "Oageng", "Laptop configured and handover checklist completed.", DateTimeOffset.UtcNow.AddHours(-11), 80)])
    ];

    public List<HardwareAsset> Assets { get; } =
    [
        new(1, "GD-LTP-014", "Lenovo ThinkPad E14", "Laptop", "Finance Desk", "Pretoria East", AssetHealth.Healthy, DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), 14500m, 82m),
        new(2, "GD-TAB-003", "Samsung Check-in Tablet", "Tablet", "Reception", "Front Desk", AssetHealth.NeedsRepair, DateOnly.FromDateTime(DateTime.Today.AddYears(-3)), 6200m, 58m),
        new(3, "GD-RTR-002", "Ubiquiti Edge Router", "Network", "Infrastructure", "Server Room", AssetHealth.Watch, DateOnly.FromDateTime(DateTime.Today.AddYears(-2)), 3900m, 74m),
        new(4, "GD-MON-021", "Dell 24-inch Monitor", "Display", "Operations", "Training Room", AssetHealth.Healthy, DateOnly.FromDateTime(DateTime.Today.AddMonths(-9)), 3200m, 88m)
    ];

    public List<MaintenanceTask> MaintenanceTasks { get; } =
    [
        new(1, 2, "Samsung Check-in Tablet", "Replace swollen battery and run safety check", MaintenanceStatus.InProgress, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), "Infrastructure", "High"),
        new(2, 3, "Ubiquiti Edge Router", "Review firmware version and backup configuration", MaintenanceStatus.Planned, DateOnly.FromDateTime(DateTime.Today.AddDays(5)), "Oageng", "Medium"),
        new(3, 1, "Lenovo ThinkPad E14", "Quarterly patch and endpoint health check", MaintenanceStatus.Planned, DateOnly.FromDateTime(DateTime.Today.AddDays(12)), "Oageng", "Low")
    ];

    public List<ActivityEvent> Activity { get; } =
    [
        new(1, "Ticket", "Critical tablet issue assigned to Infrastructure.", DateTimeOffset.UtcNow.AddHours(-1)),
        new(2, "Asset", "Router health moved to Watch after packet loss review.", DateTimeOffset.UtcNow.AddHours(-3)),
        new(3, "Maintenance", "Battery replacement task moved to In Progress.", DateTimeOffset.UtcNow.AddHours(-4)),
        new(4, "Ticket", "New starter setup resolved with handover checklist.", DateTimeOffset.UtcNow.AddHours(-10))
    ];

    public int NextTicketId() => _nextTicketId++;
    public int NextWorkLogId() => _nextWorkLogId++;
    public int NextAssetId() => _nextAssetId++;
    public int NextMaintenanceId() => _nextMaintenanceId++;
    public int NextActivityId() => _nextActivityId++;
}
