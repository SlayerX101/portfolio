namespace GreenDeskOpsApi;

public sealed class GreenDeskService(GreenDeskStore store)
{
    public IReadOnlyCollection<TicketSummary> SearchTickets(TicketQuery query)
    {
        var tickets = store.Tickets.AsEnumerable();

        if (TryParse(query.Status, out TicketStatus status))
        {
            tickets = tickets.Where(ticket => ticket.Status == status);
        }

        if (TryParse(query.Priority, out TicketPriority priority))
        {
            tickets = tickets.Where(ticket => ticket.Priority == priority);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            tickets = tickets.Where(ticket => ticket.Department.Equals(query.Department, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            tickets = tickets.Where(ticket =>
                ticket.Title.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                ticket.Description.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                ticket.Requester.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        if (query.SlaRisk is not null)
        {
            tickets = tickets.Where(ticket => IsSlaRisk(ticket) == query.SlaRisk);
        }

        return tickets
            .OrderByDescending(ticket => ticket.Priority)
            .ThenBy(ticket => ticket.SlaDueAt)
            .Select(ToSummary)
            .ToList();
    }

    public SupportTicket? GetTicket(int id) => store.Tickets.FirstOrDefault(ticket => ticket.Id == id);

    public OperationResult<SupportTicket> CreateTicket(CreateTicketRequest request)
    {
        var errors = ValidateTicket(request);
        if (errors.Count > 0)
        {
            return OperationResult<SupportTicket>.Failure(errors);
        }

        var priority = Enum.Parse<TicketPriority>(request.Priority, true);
        var now = DateTimeOffset.UtcNow;
        var ticket = new SupportTicket(
            store.NextTicketId(),
            request.Title.Trim(),
            request.Description.Trim(),
            request.Department.Trim(),
            request.Requester.Trim(),
            priority,
            TicketStatus.New,
            null,
            now,
            now.Add(CalculateSla(priority)),
            null,
            []);

        store.Tickets.Add(ticket);
        AddActivity("Ticket", $"{priority} ticket opened by {ticket.Requester}: {ticket.Title}");
        return OperationResult<SupportTicket>.Success(ticket);
    }

    public OperationResult<SupportTicket> AssignTicket(int id, AssignTicketRequest request)
    {
        var ticket = GetTicket(id);
        if (ticket is null)
        {
            return OperationResult<SupportTicket>.Missing("Ticket not found.");
        }

        if (string.IsNullOrWhiteSpace(request.AgentName))
        {
            return OperationResult<SupportTicket>.Failure("Agent name is required.");
        }

        var updated = ticket with
        {
            AssignedTo = request.AgentName.Trim(),
            Status = ticket.Status == TicketStatus.New ? TicketStatus.Assigned : ticket.Status
        };

        ReplaceTicket(updated);
        AddActivity("Ticket", $"Ticket #{id} assigned to {updated.AssignedTo}.");
        return OperationResult<SupportTicket>.Success(updated);
    }

    public OperationResult<SupportTicket> UpdateTicketStatus(int id, UpdateTicketStatusRequest request)
    {
        var ticket = GetTicket(id);
        if (ticket is null)
        {
            return OperationResult<SupportTicket>.Missing("Ticket not found.");
        }

        if (!TryParse(request.Status, out TicketStatus status))
        {
            return OperationResult<SupportTicket>.Failure("Invalid ticket status.");
        }

        if (!CanTransition(ticket.Status, status))
        {
            return OperationResult<SupportTicket>.Failure($"Cannot transition from {ticket.Status} to {status}.");
        }

        if (status is TicketStatus.Resolved or TicketStatus.Closed && string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            return OperationResult<SupportTicket>.Failure("Resolution note is required when resolving or closing.");
        }

        var logs = ticket.WorkLogs;
        if (!string.IsNullOrWhiteSpace(request.ResolutionNote))
        {
            logs.Add(new TicketWorkLog(store.NextWorkLogId(), "system", request.ResolutionNote.Trim(), DateTimeOffset.UtcNow, 0));
        }

        var updated = ticket with
        {
            Status = status,
            ResolvedAt = status is TicketStatus.Resolved or TicketStatus.Closed ? DateTimeOffset.UtcNow : ticket.ResolvedAt,
            WorkLogs = logs
        };

        ReplaceTicket(updated);
        AddActivity("Ticket", $"Ticket #{id} moved from {ticket.Status} to {status}.");
        return OperationResult<SupportTicket>.Success(updated);
    }

    public OperationResult<SupportTicket> AddWorkLog(int id, AddWorkLogRequest request)
    {
        var ticket = GetTicket(id);
        if (ticket is null)
        {
            return OperationResult<SupportTicket>.Missing("Ticket not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Actor) || string.IsNullOrWhiteSpace(request.Note))
        {
            return OperationResult<SupportTicket>.Failure("Actor and note are required.");
        }

        if (request.MinutesSpent < 0)
        {
            return OperationResult<SupportTicket>.Failure("Minutes spent cannot be negative.");
        }

        ticket.WorkLogs.Add(new TicketWorkLog(store.NextWorkLogId(), request.Actor.Trim(), request.Note.Trim(), DateTimeOffset.UtcNow, request.MinutesSpent));
        AddActivity("Ticket", $"{request.Actor.Trim()} logged {request.MinutesSpent} minutes on ticket #{id}.");
        return OperationResult<SupportTicket>.Success(ticket);
    }

    public IReadOnlyCollection<HardwareAsset> SearchAssets(AssetQuery query)
    {
        var assets = store.Assets.AsEnumerable();

        if (TryParse(query.Health, out AssetHealth health))
        {
            assets = assets.Where(asset => asset.Health == health);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            assets = assets.Where(asset => asset.Category.Equals(query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            assets = assets.Where(asset =>
                asset.AssetTag.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                asset.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) ||
                asset.AssignedTo.Contains(query.Search, StringComparison.OrdinalIgnoreCase));
        }

        return assets.OrderBy(asset => asset.Health).ThenBy(asset => asset.AssetTag).ToList();
    }

    public HardwareAsset? GetAsset(int id) => store.Assets.FirstOrDefault(asset => asset.Id == id);

    public OperationResult<HardwareAsset> CreateAsset(CreateAssetRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.AssetTag) || string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Category))
        {
            errors.Add("Asset tag, name, and category are required.");
        }

        if (store.Assets.Any(asset => asset.AssetTag.Equals(request.AssetTag, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Asset tag already exists.");
        }

        if (request.ReplacementCost <= 0)
        {
            errors.Add("Replacement cost must be greater than zero.");
        }

        if (request.EnergyRating is < 0 or > 100)
        {
            errors.Add("Energy rating must be between 0 and 100.");
        }

        if (errors.Count > 0)
        {
            return OperationResult<HardwareAsset>.Failure(errors);
        }

        var asset = new HardwareAsset(
            store.NextAssetId(),
            request.AssetTag.Trim().ToUpperInvariant(),
            request.Name.Trim(),
            request.Category.Trim(),
            string.IsNullOrWhiteSpace(request.AssignedTo) ? "Unassigned" : request.AssignedTo.Trim(),
            string.IsNullOrWhiteSpace(request.Location) ? "Unassigned" : request.Location.Trim(),
            AssetHealth.Healthy,
            DateOnly.FromDateTime(DateTime.Today),
            decimal.Round(request.ReplacementCost, 2),
            decimal.Round(request.EnergyRating, 1));

        store.Assets.Add(asset);
        AddActivity("Asset", $"{asset.AssetTag} registered for {asset.AssignedTo}.");
        return OperationResult<HardwareAsset>.Success(asset);
    }

    public OperationResult<HardwareAsset> UpdateAssetHealth(int id, UpdateAssetHealthRequest request)
    {
        var asset = GetAsset(id);
        if (asset is null)
        {
            return OperationResult<HardwareAsset>.Missing("Asset not found.");
        }

        if (!TryParse(request.Health, out AssetHealth health))
        {
            return OperationResult<HardwareAsset>.Failure("Invalid asset health.");
        }

        var updated = asset with { Health = health };
        ReplaceAsset(updated);
        AddActivity("Asset", $"{asset.AssetTag} health changed to {health}.");
        return OperationResult<HardwareAsset>.Success(updated);
    }

    public IReadOnlyCollection<MaintenanceTask> SearchMaintenance(MaintenanceStatus? status) =>
        store.MaintenanceTasks
            .Where(task => status is null || task.Status == status)
            .OrderBy(task => task.DueDate)
            .ToList();

    public OperationResult<MaintenanceTask> ScheduleMaintenance(ScheduleMaintenanceRequest request)
    {
        var asset = GetAsset(request.AssetId);
        if (asset is null)
        {
            return OperationResult<MaintenanceTask>.Missing("Asset not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Task) || string.IsNullOrWhiteSpace(request.Owner))
        {
            return OperationResult<MaintenanceTask>.Failure("Task and owner are required.");
        }

        if (request.DueDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return OperationResult<MaintenanceTask>.Failure("Due date cannot be in the past.");
        }

        var task = new MaintenanceTask(
            store.NextMaintenanceId(),
            asset.Id,
            asset.Name,
            request.Task.Trim(),
            MaintenanceStatus.Planned,
            request.DueDate,
            request.Owner.Trim(),
            string.IsNullOrWhiteSpace(request.Risk) ? "Medium" : request.Risk.Trim());

        store.MaintenanceTasks.Add(task);
        AddActivity("Maintenance", $"Maintenance scheduled for {asset.AssetTag}: {task.Task}.");
        return OperationResult<MaintenanceTask>.Success(task);
    }

    public OperationResult<MaintenanceTask> UpdateMaintenanceStatus(int id, UpdateMaintenanceStatusRequest request)
    {
        var task = store.MaintenanceTasks.FirstOrDefault(item => item.Id == id);
        if (task is null)
        {
            return OperationResult<MaintenanceTask>.Missing("Maintenance task not found.");
        }

        if (!TryParse(request.Status, out MaintenanceStatus status))
        {
            return OperationResult<MaintenanceTask>.Failure("Invalid maintenance status.");
        }

        var updated = task with { Status = status };
        ReplaceMaintenance(updated);
        AddActivity("Maintenance", $"Maintenance task #{id} changed to {status}.");
        return OperationResult<MaintenanceTask>.Success(updated);
    }

    public DashboardSummary GetDashboard()
    {
        var openTickets = store.Tickets.Where(ticket => ticket.Status is not TicketStatus.Resolved and not TicketStatus.Closed).ToList();
        var assetsOnWatch = store.Assets.Count(asset => asset.Health is AssetHealth.Watch or AssetHealth.NeedsRepair);
        var plannedMaintenance = store.MaintenanceTasks.Count(task => task.Status is MaintenanceStatus.Planned or MaintenanceStatus.InProgress);
        var assetValue = store.Assets.Where(asset => asset.Health != AssetHealth.Retired).Sum(asset => asset.ReplacementCost);
        var estimatedMonthlyEnergySaving = store.Assets.Sum(asset => Math.Max(asset.EnergyRating - 60m, 0m) * 1.75m);

        return new DashboardSummary(
            openTickets.Count,
            openTickets.Count(IsSlaRisk),
            assetsOnWatch,
            plannedMaintenance,
            assetValue,
            decimal.Round(estimatedMonthlyEnergySaving, 2),
            store.Tickets.GroupBy(ticket => ticket.Status).Select(group => new StatusCount(group.Key.ToString(), group.Count())).ToList(),
            store.Tickets.GroupBy(ticket => ticket.Department).Select(group => new DepartmentCount(group.Key, group.Count())).OrderByDescending(group => group.Count).ToList(),
            store.Activity.OrderByDescending(activity => activity.CreatedAt).Take(8).ToList());
    }

    private static TicketSummary ToSummary(SupportTicket ticket) =>
        new(
            ticket.Id,
            ticket.Title,
            ticket.Department,
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.AssignedTo,
            ticket.SlaDueAt,
            IsSlaRisk(ticket),
            ticket.WorkLogs.Count,
            ticket.WorkLogs.Sum(log => log.MinutesSpent));

    private static bool IsSlaRisk(SupportTicket ticket) =>
        ticket.Status is not TicketStatus.Resolved and not TicketStatus.Closed &&
        DateTimeOffset.UtcNow.AddHours(6) >= ticket.SlaDueAt;

    private static TimeSpan CalculateSla(TicketPriority priority) =>
        priority switch
        {
            TicketPriority.Critical => TimeSpan.FromHours(4),
            TicketPriority.High => TimeSpan.FromHours(12),
            TicketPriority.Medium => TimeSpan.FromHours(48),
            _ => TimeSpan.FromDays(5)
        };

    private static bool CanTransition(TicketStatus from, TicketStatus to) =>
        from == to ||
        (from, to) is
        (TicketStatus.New, TicketStatus.Assigned) or
        (TicketStatus.New, TicketStatus.InProgress) or
        (TicketStatus.Assigned, TicketStatus.InProgress) or
        (TicketStatus.InProgress, TicketStatus.WaitingForUser) or
        (TicketStatus.WaitingForUser, TicketStatus.InProgress) or
        (TicketStatus.InProgress, TicketStatus.Resolved) or
        (TicketStatus.WaitingForUser, TicketStatus.Resolved) or
        (TicketStatus.Resolved, TicketStatus.Closed);

    private static List<string> ValidateTicket(CreateTicketRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("Description is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Department))
        {
            errors.Add("Department is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Requester))
        {
            errors.Add("Requester is required.");
        }

        if (!TryParse(request.Priority, out TicketPriority _))
        {
            errors.Add("Priority must be Low, Medium, High, or Critical.");
        }

        return errors;
    }

    private static bool TryParse<T>(string? value, out T parsed)
        where T : struct, Enum =>
        Enum.TryParse(value, true, out parsed);

    private void ReplaceTicket(SupportTicket updated)
    {
        var index = store.Tickets.FindIndex(ticket => ticket.Id == updated.Id);
        store.Tickets[index] = updated;
    }

    private void ReplaceAsset(HardwareAsset updated)
    {
        var index = store.Assets.FindIndex(asset => asset.Id == updated.Id);
        store.Assets[index] = updated;
    }

    private void ReplaceMaintenance(MaintenanceTask updated)
    {
        var index = store.MaintenanceTasks.FindIndex(task => task.Id == updated.Id);
        store.MaintenanceTasks[index] = updated;
    }

    private void AddActivity(string eventType, string message) =>
        store.Activity.Add(new ActivityEvent(store.NextActivityId(), eventType, message, DateTimeOffset.UtcNow));
}
